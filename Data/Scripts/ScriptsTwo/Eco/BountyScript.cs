using Meridian.Economy;
using Sandbox.Definitions;
using Sandbox.Game;
using Sandbox.Game.Entities;
using Sandbox.Game.Multiplayer;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using VRage;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.Entity;
using VRage.Game.ModAPI; 
using VRage.ModAPI;      
using VRage.Utils;
using VRageMath;
using VRage.Game.ModAPI.Ingame.Utilities; 
using VRage.ObjectBuilders;
using Meridian.Utilities;

namespace Meridian.Economy
{
    [MySessionComponentDescriptor(MyUpdateOrder.AfterSimulation)]
    public class WarBountyPayouts : MySessionComponentBase
    {
        private const int PayoutIntervalTicks = 1 * 60;
        private const int PayoutIntervalCombatEndTicks = 30 * 60;
        private const int WAR_REPUTATION_THRESHOLD = 500;

        public UserConfig UserConfig;
        const string ConfigFileName = "WarBountyPayout.cfg";

        private readonly List<RewardItem> _rewardItems = new List<RewardItem>();

        private bool _registered;

        private static readonly MyStringHash Damage_Deformation = MyStringHash.GetOrCompute("Deformation");
        private static readonly MyStringHash Damage_Grinding = MyStringHash.GetOrCompute("Grinding");

        
        private readonly Dictionary<long, long> _pending = new Dictionary<long, long>();

        
        private readonly Dictionary<long, int> _pendingLastHit = new Dictionary<long, int>();

        
        private readonly Dictionary<long, Dictionary<MyDefinitionId, int>> _pendingLoot = new Dictionary<long, Dictionary<MyDefinitionId, int>>(64);

        
        private readonly Dictionary<long, IMyPlayer> _playerCache = new Dictionary<long, IMyPlayer>(32);

        
        private readonly Dictionary<long, IMyCubeGrid> _gridCache = new Dictionary<long, IMyCubeGrid>(64);

        
        private readonly List<IMySlimBlock> _blockBuffer = new List<IMySlimBlock>(256);
        private readonly List<IMyPlayer> _playerQueryBuffer = new List<IMyPlayer>(8);
        private readonly List<long> _toClearList = new List<long>(64);

        
        private readonly List<long> _pendingLastHitSnapshot = new List<long>(64);

        public override void LoadData()
        {
            if (MyAPIGateway.Multiplayer.IsServer)
            {
                string s = FileManager.GetTextFileWorldStorage(ConfigFileName);
                UserConfig cfg = null;
                bool needsSave = true;
                if (s != null && s != "")
                {
                    try
                    {
                        cfg = MyAPIGateway.Utilities.SerializeFromXML<UserConfig>(s);
                        needsSave = false;
                    }
                    catch (Exception e)
                    {
                        MyLog.Default.WriteLine($"War bounty payout config problem, resetting to defaults: {e}");
                        cfg = null;
                    }
                }
                else
                {
                    MyLog.Default.WriteLine("s is null or empty");
                }

                if (cfg == null)
                {
                    cfg = new UserConfig(1f, "SIGIL", new List<RewardItemSerializable>()
                    {
                        new RewardItemSerializable("Ingot/PrototechScrap", 2),
                        new RewardItemSerializable("Ingot/Platinum", 1)
                    }, 2f);
                    MyLog.Default.WriteLine("Resetting war bounty payout config.");
                }

                UserConfig = cfg;

                if (needsSave)
                {
                    Save();
                }
                
                foreach (var item in cfg.NPCRewardItems)
                {
                    _rewardItems.Add(new RewardItem(item));
                }
            }
            
        }

        public void Save()
        {
            FileManager.SaveXMLFileWorldStorage(UserConfig, ConfigFileName);
        }

        public override void BeforeStart()
        {
            if (MyAPIGateway.Multiplayer.IsServer)
            {
                TryRegisterDamageHooks();
            }
        }

        protected override void UnloadData()
        {
            _pending.Clear();
            _pendingLoot.Clear();
            _pendingLastHit.Clear();

            _playerCache.Clear();
            _gridCache.Clear();

            _toClearList.Clear();
            _blockBuffer.Clear();
            _pendingLastHitSnapshot.Clear();

            _registered = false;
            _rewardItems.Clear();
        }

        public override void UpdateAfterSimulation()
        {
            if (MyAPIGateway.Multiplayer == null || !MyAPIGateway.Multiplayer.IsServer)
                return;

            if (!_registered)
                TryRegisterDamageHooks();

            var session = MyAPIGateway.Session;
            if (session == null)
                return;

            if (session.GameplayFrameCounter % PayoutIntervalTicks == 0)
            {
                PayAggregatedRewards(); 
            }
        }

        private void TryRegisterDamageHooks()
        {
            if (_registered)
                return;
            if (MyAPIGateway.Session?.DamageSystem == null)
                return;

            MyAPIGateway.Session.DamageSystem.RegisterDestroyHandler(0, OnDestroyed);
            _registered = true;
        }

        public static bool IsNPCFaction(IMyFaction faction) => faction.IsEveryoneNpc();

        private void OnDestroyed(object target, MyDamageInformation info)
{
    if (target == null)
        return;

    if (info.Type == Damage_Deformation || info.Type == Damage_Grinding)
        return;

    var ch = target as IMyCharacter;
    if (ch != null)
        return;

    var slim = target as IMySlimBlock;
    if (slim == null || slim.CubeGrid == null)
        return;

    long defenderId = GetPrimaryOwnerIdentity(slim.CubeGrid);
    if (defenderId == 0)
        return;

    long attackerId;
    if (!TryResolveAttackerIdentity(info.AttackerId, out attackerId) || attackerId == 0)
        return;

    // --- SELF-FARM GUARD ---
    // Attacker cannot be the same player as the block owner
    if (attackerId == defenderId)
        return;

    var factions = MyAPIGateway.Session?.Factions;
    if (factions == null)
        return;

    var atkFac = factions.TryGetPlayerFaction(attackerId);
    var vicFac = factions.TryGetPlayerFaction(defenderId);

    if (atkFac == null || vicFac == null)
        return;

    
    if (atkFac.FactionId == vicFac.FactionId)
        return;

    bool atWar = factions.AreFactionsEnemies(atkFac.FactionId, vicFac.FactionId) ||
                 IsAtWarByReputation(factions, atkFac.FactionId, vicFac.FactionId);

    if (!atWar)
        return;

    var blockDef = slim.BlockDefinition;
    var costs = PriceChanger.Instance.Costs;
    var allCosts = costs?.AllBlockCosts;
    if (blockDef == null || allCosts == null)
        return;

    bool NPCFac = string.Equals(vicFac.Tag, UserConfig.NPCFactionStr, StringComparison.OrdinalIgnoreCase);

    MyFixedPoint price;
    if (allCosts.TryGetValue(blockDef.Id, out price))
    {
        price += GetHydrogenBonusByLiters(slim);

        if (price > 0)
            QueueCurrencyPayout(attackerId, (long)(price * UserConfig.BountyPayoutMultiplier * (NPCFac ? UserConfig.NPCPayoutMultiplier : 1f)));
    }

    if (NPCFac)
        AwardLootIfApplicable(attackerId, atkFac, vicFac);
}

        private static bool IsAtWarByReputation(IMyFactionCollection factions, long factionId1, long factionId2)
        {
            int reputation = factions.GetReputationBetweenFactions(factionId1, factionId2);
            return reputation <= WAR_REPUTATION_THRESHOLD;
        }

        private void EnsureAggregationStart(long identityId)
        {
            var session = MyAPIGateway.Session;
            if (session == null)
                return;

            if (!_pendingLastHit.ContainsKey(identityId))
            {
                MyVisualScriptLogicProvider.SendChatMessageColored(
                    $"Combat detected, beginning bounty aggregation.",
                    new Color(0, 122, 255),
                    "Conflict Commissariat",
                    identityId,
                    "Blue"
                );
            }
            _pendingLastHit[identityId] = session.GameplayFrameCounter;
        }

        private void QueueCurrencyPayout(long identityId, long amount)
        {
            long existing;
            if (_pending.TryGetValue(identityId, out existing))
                _pending[identityId] = existing + amount;
            else
                _pending[identityId] = amount;

            EnsureAggregationStart(identityId);
        }

        private void PayAggregatedRewards()
        {
            
            _playerCache.Clear();

            if (_pendingLastHit.Count == 0)
                return;

            var session = MyAPIGateway.Session;
            if (session == null)
                return;

            int now = session.GameplayFrameCounter;

            _toClearList.Clear();

            
            _pendingLastHitSnapshot.Clear();
            foreach (var key in _pendingLastHit.Keys)
                _pendingLastHitSnapshot.Add(key);

            for (int si = 0; si < _pendingLastHitSnapshot.Count; si++)
            {
                long identityId = _pendingLastHitSnapshot[si];

                int lastHit;
                if (!_pendingLastHit.TryGetValue(identityId, out lastHit))
                    continue;

                if (now - lastHit <= PayoutIntervalCombatEndTicks)
                    continue; 

                
                IMyPlayer player;
                if (!_playerCache.TryGetValue(identityId, out player) || player == null)
                {
                    _playerQueryBuffer.Clear();
                    MyAPIGateway.Players?.GetPlayers(_playerQueryBuffer, p => p != null && p.IdentityId == identityId);
                    if (_playerQueryBuffer.Count > 0)
                    {
                        player = _playerQueryBuffer[0];
                        _playerCache[identityId] = player;
                    }
                }

               
                long currency = 0;
                _pending.TryGetValue(identityId, out currency);
                if (currency > 0 && player != null)
                {
                    player.RequestChangeBalance(currency);
                    MyVisualScriptLogicProvider.SendChatMessageColored(
                        $"Combat completed, you've received {currency:n0} SC in aggregated bounties.",
                        new Color(0, 122, 255),
                        "Conflict Commissariat",
                        identityId,
                        "Blue"
                    );
                    _pending[identityId] = 0;
                }

               
                Dictionary<MyDefinitionId, int> loot;
                if (_pendingLoot.TryGetValue(identityId, out loot) && loot != null && loot.Count > 0)
                {
                    IMyCubeGrid grid = GetCurrentOrCachedGridForPlayer(identityId);
                    if (grid != null)
                    {
                        int totalAdded = 0;
                        var payoutSummary = new List<string>(loot.Count);
                        var remainingMap = new Dictionary<MyDefinitionId, int>(loot.Count);

                        foreach (var item in loot)
                        {
                            var id = item.Key;
                            var amt = item.Value;

                            int remaining = AddItemToGridInventories(grid, id, amt);
                            int added = amt - remaining;
                            totalAdded += added;

                            if (added > 0)
                                payoutSummary.Add($"{id.SubtypeName} x{added}");

                            if (remaining > 0)
                                remainingMap[id] = remaining;
                        }

                        
                        if (remainingMap.Count > 0)
                            _pendingLoot[identityId] = remainingMap;
                        else
                            _pendingLoot.Remove(identityId);

                        if (totalAdded > 0)
                        {
                            MyVisualScriptLogicProvider.SendChatMessageColored(
                                $"You recovered the following loot: {string.Join(", ", payoutSummary)}",
                                new Color(255, 215, 0),
                                "Conflict Commissariat",
                                identityId,
                                "Yellow"
                            );
                        }
                        else
                        {
                            
                            MyVisualScriptLogicProvider.SendChatMessageColored(
                                $"War loot payout could not be delivered. Ensure your current grid has cargo space.",
                                new Color(255, 100, 0),
                                "Conflict Commissariat",
                                identityId,
                                "Orange"
                            );
                        }
                    }
                    else
                    {
                        
                        _pendingLastHit[identityId] = now - PayoutIntervalCombatEndTicks; 
                        continue; 
                    }
                }

                
                long curAfter;
                _pending.TryGetValue(identityId, out curAfter);
                bool hasLoot = _pendingLoot.ContainsKey(identityId);

                if ((curAfter <= 0) && !hasLoot)
                {
                    _toClearList.Add(identityId);
                }
            }

            
            for (int i = 0; i < _toClearList.Count; i++)
            {
                long id = _toClearList[i];
                _pending.Remove(id);
                _pendingLastHit.Remove(id);
                _gridCache.Remove(id);
            }
            _toClearList.Clear();
        }

        private static MyFixedPoint GetHydrogenBonusByLiters(IMySlimBlock slim)
        {
            var tank = slim.FatBlock as IMyGasTank;
            if (tank == null || PriceChanger.Instance == null || PriceChanger.Instance.Costs == null || PriceChanger.Instance.Costs.GasCosts == null)
                return 0;

            double liters = tank.Capacity * tank.FilledRatio;
            if (liters <= 0)
                return 0;

            var def = tank.SlimBlock.BlockDefinition as MyGasTankDefinition;
            
            if (def == null || string.IsNullOrEmpty(def.StoredGasId.SubtypeName))
                return 0;

            double pricePerLiter;
            if (PriceChanger.Instance.Costs.GasCosts.TryGetValue(def.StoredGasId.SubtypeName, out pricePerLiter))
            {
                return (MyFixedPoint)(liters * pricePerLiter);
            }
            return 0;
        }

        private static long GetPrimaryOwnerIdentity(IMyCubeGrid grid)
        {
            if (grid.BigOwners != null && grid.BigOwners.Count > 0 && grid.BigOwners[0] != 0)
                return grid.BigOwners[0];
            if (grid.SmallOwners != null && grid.SmallOwners.Count > 0 && grid.SmallOwners[0] != 0)
                return grid.SmallOwners[0];
            return 0;
        }

        private static bool TryResolveAttackerIdentity(long attackerEntityId, out long identityId)
        {
            identityId = 0;
            IMyEntity ent;
            if (!MyAPIGateway.Entities.TryGetEntityById(attackerEntityId, out ent) || ent == null)
                return false;

            var top = ent.GetTopMostParent();

            var player = MyAPIGateway.Players != null ? MyAPIGateway.Players.GetPlayerControllingEntity(top) : null;
            if (player != null)
            {
                identityId = player.IdentityId;
                if (identityId != 0)
                    return true;
            }

            var block = top as IMyCubeBlock;
            if (block != null && block.OwnerId != 0)
            {
                identityId = block.OwnerId;
                return true;
            }

            var grid = top as IMyCubeGrid;
            if (grid != null && grid.BigOwners != null && grid.BigOwners.Count > 0 && grid.BigOwners[0] != 0)
            {
                identityId = grid.BigOwners[0];
                return true;
            }

            return false;
        }


        private void AwardLootIfApplicable(long attackerIdentityId, IMyFaction attackerFaction, IMyFaction victimFaction)
        {
            
            if (!string.Equals(victimFaction.Tag, UserConfig.NPCFactionStr, StringComparison.OrdinalIgnoreCase))
                return;

            
            Dictionary<MyDefinitionId, int> bag;
            if (!_pendingLoot.TryGetValue(attackerIdentityId, out bag))
            {
                bag = new Dictionary<MyDefinitionId, int>(MyDefinitionId.Comparer);
                _pendingLoot[attackerIdentityId] = bag;
            }

            for (int i = 0; i < _rewardItems.Count; i++)
            {
                var ri = _rewardItems[i];

                int existing;
                if (bag.TryGetValue(ri.Id, out existing))
                    bag[ri.Id] = existing + ri.Amount;
                else
                    bag[ri.Id] = ri.Amount;
            }

            
            EnsureAggregationStart(attackerIdentityId);
        }

        private IMyCubeGrid GetCurrentOrCachedGridForPlayer(long identityId)
        {
            
            _gridCache.Remove(identityId);

            IMyPlayer p;
            if (!_playerCache.TryGetValue(identityId, out p) || p == null)
            {
                _playerQueryBuffer.Clear();
                MyAPIGateway.Players?.GetPlayers(_playerQueryBuffer, pl => pl != null && pl.IdentityId == identityId);
                if (_playerQueryBuffer.Count > 0)
                {
                    p = _playerQueryBuffer[0];
                    _playerCache[identityId] = p;
                }
            }

            if (p == null)
                return null;

           
            if (p.Controller == null)
            {
                _playerCache.Remove(identityId);
                return null;
            }

            var controlled = p.Controller?.ControlledEntity?.Entity;
            var top = controlled?.GetTopMostParent();

            var asGrid = top as IMyCubeGrid;
            if (asGrid != null && !asGrid.MarkedForClose)
            {
                _gridCache[identityId] = asGrid;
                return asGrid;
            }

            var asBlock = top as IMyCubeBlock;
            if (asBlock != null && asBlock.CubeGrid != null && !asBlock.CubeGrid.MarkedForClose)
            {
                _gridCache[identityId] = asBlock.CubeGrid;
                return asBlock.CubeGrid;
            }

            return null;
        }

        private static MyObjectBuilder_PhysicalObject CreatePhysicalObject(MyDefinitionId defId)
        {
            // Create properly initialized physical objects via serializer so inventories accept them
            if (defId.TypeId == typeof(MyObjectBuilder_Component) ||
                defId.TypeId == typeof(MyObjectBuilder_Ingot))
            {
                var baseObj = MyObjectBuilderSerializer.CreateNewObject(defId.TypeId, defId.SubtypeName);
                return baseObj as MyObjectBuilder_PhysicalObject;
            }

            // Not allowed; prevent accidental ammo/tool spawns
            return null;
        }

        private int AddItemToGridInventories(IMyCubeGrid grid, MyDefinitionId defId, int amount)
        {
            if (grid == null)
                return amount;

            int remaining = amount;

            _blockBuffer.Clear();
            grid.GetBlocks(_blockBuffer, b => b != null && b.FatBlock != null && b.FatBlock.HasInventory);

            // Simple heuristic: iterate all inventories; binary search for fit
            for (int bi = 0; bi < _blockBuffer.Count && remaining > 0; bi++)
            {
                var block = _blockBuffer[bi].FatBlock as IMyCubeBlock;
                if (block == null || !block.HasInventory)
                    continue;

                int invCount = block.InventoryCount;
                for (int i = 0; i < invCount && remaining > 0; i++)
                {
                    IMyInventory inv = block.GetInventory(i);
                    if (inv == null)
                        continue;

                    var phys = CreatePhysicalObject(defId);
                    if (phys == null)
                        continue;

                    // Try full add first
                    MyFixedPoint full = (MyFixedPoint)remaining;
                    if (inv.CanItemsBeAdded(full, defId))
                    {
                        inv.AddItems(full, phys);
                        remaining = 0;
                        break;
                    }

                    var volume = ((MyPhysicalItemDefinition)MyDefinitionManager.Static.GetDefinition(defId)).Volume * 1000;
                    int maxItems = (int)Math.Floor((double)(inv.MaxVolume - inv.CurrentVolume) * 1000 / volume);

                    if (inv.CanItemsBeAdded(maxItems, defId))
                    {
                        inv.AddItems((MyFixedPoint)maxItems, phys);
                        remaining -= maxItems;
                    }
                }
            }

            return remaining;
        }
    }
}