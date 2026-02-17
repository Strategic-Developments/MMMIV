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
using VRage.Game.ModAPI; // Mod API (IMyCubeGrid, IMySlimBlock, IMyInventory, etc.)
using VRage.ModAPI;      // IMyEntity
using VRage.Utils;
using VRageMath;
using VRage.Game.ModAPI.Ingame.Utilities; // MyIni, MyIniParseResult
using VRage.ObjectBuilders;

namespace Meridian.Economy
{
    [MySessionComponentDescriptor(MyUpdateOrder.AfterSimulation)]
    public class WarBountyPayouts : MySessionComponentBase
    {
        private const int PayoutIntervalTicks = 1 * 60;
        private const int PayoutIntervalCombatEndTicks = 30 * 60;
        private const float PAYOUT_RATIO = 1.025f;
        private const int WAR_REPUTATION_THRESHOLD = 500;

        // Config
        private const string IniSection = "WarBountyPayouts";
        private const string DefaultRewardFactionTag = "SIGIL";
        private string RewardFactionTag = DefaultRewardFactionTag;

        private struct RewardItem
        {
            public MyDefinitionId Id;
            public int Amount;
            public RewardItem(MyDefinitionId id, int amount)
            {
                Id = id;
                Amount = amount;
            }
        }

        private static readonly Dictionary<string, Type> TypeMap = new Dictionary<string, Type>(StringComparer.OrdinalIgnoreCase)
        {
            { "Component", typeof(MyObjectBuilder_Component) },
            { "Ingot", typeof(MyObjectBuilder_Ingot) },
        };

        private readonly List<RewardItem> _rewardItems = new List<RewardItem>();

        private bool _registered;

        private static readonly MyStringHash Damage_Deformation = MyStringHash.GetOrCompute("Deformation");
        private static readonly MyStringHash Damage_Grinding = MyStringHash.GetOrCompute("Grinding");

        // Currency aggregation
        private readonly Dictionary<long, long> _pending = new Dictionary<long, long>();

        // Shared "last hit" tracking for both currency and loot
        private readonly Dictionary<long, int> _pendingLastHit = new Dictionary<long, int>();

        // Loot aggregation: identityId -> (definitionId -> total amount)
        private readonly Dictionary<long, Dictionary<MyDefinitionId, int>> _pendingLoot = new Dictionary<long, Dictionary<MyDefinitionId, int>>(64);

        // Player cache to reduce repeated scans
        private readonly Dictionary<long, IMyPlayer> _playerCache = new Dictionary<long, IMyPlayer>(64);

        // Grid cache per identity (validated on use)
        private readonly Dictionary<long, IMyCubeGrid> _gridCache = new Dictionary<long, IMyCubeGrid>(64);

        // Reusable buffers to avoid per-call allocations
        private static readonly List<IMySlimBlock> _blockBuffer = new List<IMySlimBlock>(256);
        private readonly List<IMyPlayer> _playerQueryBuffer = new List<IMyPlayer>(8);
        private readonly List<long> _toClearList = new List<long>(64);

        public override void BeforeStart()
        {
            if (MyAPIGateway.Multiplayer != null && MyAPIGateway.Multiplayer.IsServer)
            {
                LoadConfiguration();
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
                PayAggregatedRewards(); // pay currency + loot together after combat lull
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
            // Defensive null check to avoid NREs when RaiseDestroyed is invoked with a null target
            if (target == null)
                return;

            // Skip common non-combat damage types
            if (info.Type == Damage_Deformation || info.Type == Damage_Grinding)
                return;

            // Character killed
            var ch = target as IMyCharacter;
            if (ch != null)
                return;

            // Block destroyed
            var slim = target as IMySlimBlock;
            if (slim == null || slim.CubeGrid == null)
                return;

            long defenderId = GetPrimaryOwnerIdentity(slim.CubeGrid);
            if (defenderId == 0)
                return;

            long attackerId;
            if (!TryResolveAttackerIdentity(info.AttackerId, out attackerId) || attackerId == 0)
                return;

            // Faction subsystem must be available
            var factions = MyAPIGateway.Session?.Factions;
            if (factions == null)
                return;

            var atkFac = factions.TryGetPlayerFaction(attackerId);
            var vicFac = factions.TryGetPlayerFaction(defenderId);

            // Defensive null check: players may not be in a faction in Space Engineers
            if (atkFac == null || vicFac == null)
                return;

            // Check if factions are at war (enemies OR reputation <= 500)
            bool atWar = factions.AreFactionsEnemies(atkFac.FactionId, vicFac.FactionId) ||
                         IsAtWarByReputation(factions, atkFac.FactionId, vicFac.FactionId);

            if (!atWar)
                return;

            // Defensive checks for block definition and price lookup chain
            var blockDef = slim.BlockDefinition;
            var priceChanger = PriceChanger.Instance;
            var costs = priceChanger?.Costs;
            var allCosts = costs?.AllBlockCosts;
            if (blockDef == null || allCosts == null)
                return;

            MyFixedPoint price;
            if (allCosts.TryGetValue(blockDef.Id, out price))
            {
                price += GetHydrogenBonusByLiters(slim, priceChanger);

                if (price > 0)
                    QueueCurrencyPayout(attackerId, (long)(price * PAYOUT_RATIO));
            }

            // Accumulate loot if the destroyed block's owner faction tag matches RewardFactionTag
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
            if (_pendingLastHit.Count == 0)
                return;

            var session = MyAPIGateway.Session;
            if (session == null)
                return;

            int now = session.GameplayFrameCounter;

            _toClearList.Clear();

            // Iterate only over identities with tracked combat activity
            foreach (var kv in _pendingLastHit)
            {
                long identityId = kv.Key;
                int lastHit = kv.Value;

                if (now - lastHit <= PayoutIntervalCombatEndTicks)
                    continue; // still in combat window

                // Try to resolve player (online only for currency payout)
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

                // 1) Pay currency if any and if player is online
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

                // 2) Pay loot in lump sum (to current/cached grid inventories)
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

                            // Only Components and Ingots are allowed
                            if (!IsAllowedLootType(id))
                            {
                                remainingMap[id] = amt; // keep ignored items pending
                                continue;
                            }

                            int remaining = AddItemToGridInventories(grid, id, amt);
                            int added = amt - remaining;
                            totalAdded += added;

                            if (added > 0)
                                payoutSummary.Add($"{id.SubtypeName} x{added}");

                            if (remaining > 0)
                                remainingMap[id] = remaining;
                        }

                        // Update pending loot with remaining (if any)
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
                            // No items could be added (no cargo space or invalid types)
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
                        // Player has no current grid; keep loot pending
                        // Consider adding a timeout or alternate delivery if desired
                    }
                }

                // Clear timing if both currency and loot are fully settled or empty
                long curAfter;
                _pending.TryGetValue(identityId, out curAfter);
                bool hasLoot = _pendingLoot.ContainsKey(identityId);

                if ((curAfter <= 0) && !hasLoot)
                {
                    _toClearList.Add(identityId);
                }
            }

            // Cleanup cleared identities
            for (int i = 0; i < _toClearList.Count; i++)
            {
                long id = _toClearList[i];
                _pending.Remove(id);
                _pendingLastHit.Remove(id);
                _gridCache.Remove(id);
            }
            _toClearList.Clear();
        }

        private static string GetPlayerName(long identityId)
        {
            if (identityId == 0 || MyAPIGateway.Players == null)
                return "Unknown";
            var list = new List<IMyPlayer>();
            MyAPIGateway.Players.GetPlayers(list, p => p != null && p.IdentityId == identityId);
            return (list.Count > 0 && list[0] != null) ? (list[0].DisplayName ?? "Unknown") : "Unknown";
        }

        private static MyFixedPoint GetHydrogenBonusByLiters(IMySlimBlock slim, PriceChanger priceChanger)
        {
            var tank = slim.FatBlock as IMyGasTank;
            if (tank == null || priceChanger == null || priceChanger.Costs == null || priceChanger.Costs.GasCosts == null)
                return 0;

            double liters = tank.Capacity * tank.FilledRatio;
            if (liters <= 0)
                return 0;

            var def = tank.SlimBlock.BlockDefinition as MyGasTankDefinition;
            // Fix: MyDefinitionId.TypeId is not nullable; do not compare to null
            if (def == null || string.IsNullOrEmpty(def.StoredGasId.SubtypeName))
                return 0;

            double pricePerLiter;
            if (priceChanger.Costs.GasCosts.TryGetValue(def.StoredGasId.SubtypeName, out pricePerLiter))
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

        // ---------------------
        // Configuration loading
        // ---------------------
        private void LoadConfiguration()
        {
            RewardFactionTag = DefaultRewardFactionTag;
            _rewardItems.Clear();

            try
            {
                var reader = MyAPIGateway.Utilities.ReadFileInWorldStorage("WarBountyPayouts.ini", typeof(WarBountyPayouts));
                if (reader != null)
                {
                    var content = reader.ReadToEnd();
                    reader.Close();

                    var ini = new MyIni();
                    MyIniParseResult result;
                    if (ini.TryParse(content, out result))
                    {
                        RewardFactionTag = ini.Get(IniSection, "RewardFactionTag").ToString(RewardFactionTag);
                        string items = ini.Get(IniSection, "RewardItems").ToString("");
                        if (!string.IsNullOrWhiteSpace(items))
                            ParseRewardItems(items);
                    }
                }
            }
            catch
            {
                // Swallow; will use defaults
            }

            // Defaults if none provided
            if (_rewardItems.Count == 0)
            {
                // Only Components/Ingots allowed
                _rewardItems.Add(new RewardItem(new MyDefinitionId(typeof(MyObjectBuilder_Component), "PrototechCapacitor"), 1));
                _rewardItems.Add(new RewardItem(new MyDefinitionId(typeof(MyObjectBuilder_Ingot), "Platinum"), 1));
            }

            // Filter any invalid types defensively
            for (int i = _rewardItems.Count - 1; i >= 0; i--)
            {
                if (!IsAllowedLootType(_rewardItems[i].Id))
                    _rewardItems.RemoveAt(i);
            }
        }

        private void ParseRewardItems(string itemsSpec)
        {
            var entries = itemsSpec.Split(new[] { ',', ';', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var raw in entries)
            {
                var entry = raw.Trim();
                var parts = entry.Split(':');
                if (parts.Length != 2)
                    continue;

                int amount;
                if (!int.TryParse(parts[1], out amount) || amount <= 0)
                    continue;

                var typeSubtype = parts[0].Split('/');
                if (typeSubtype.Length != 2)
                    continue;

                var typeName = typeSubtype[0].Trim();
                var subtype = typeSubtype[1].Trim();

                Type t;
                if (!TypeMap.TryGetValue(typeName, out t))
                    continue;

                var id = new MyDefinitionId(t, subtype);
                if (IsAllowedLootType(id))
                    _rewardItems.Add(new RewardItem(id, amount));
            }
        }

        private static bool IsAllowedLootType(MyDefinitionId id)
        {
            return id.TypeId == typeof(MyObjectBuilder_Component) || id.TypeId == typeof(MyObjectBuilder_Ingot);
        }

        // ---------------------
        // Loot awarding (aggregation)
        // ---------------------
        private void AwardLootIfApplicable(long attackerIdentityId, IMyFaction attackerFaction, IMyFaction victimFaction)
        {
            if (victimFaction == null)
                return;

            // Match victim's faction tag against configured tag
            if (!string.Equals(victimFaction.Tag, RewardFactionTag, StringComparison.OrdinalIgnoreCase))
                return;

            // Aggregate loot for attacker
            Dictionary<MyDefinitionId, int> bag;
            if (!_pendingLoot.TryGetValue(attackerIdentityId, out bag))
            {
                bag = new Dictionary<MyDefinitionId, int>(MyDefinitionId.Comparer);
                _pendingLoot[attackerIdentityId] = bag;
            }

            for (int i = 0; i < _rewardItems.Count; i++)
            {
                var ri = _rewardItems[i];
                if (!IsAllowedLootType(ri.Id))
                    continue;

                int existing;
                if (bag.TryGetValue(ri.Id, out existing))
                    bag[ri.Id] = existing + ri.Amount;
                else
                    bag[ri.Id] = ri.Amount;
            }

            // Mark/start aggregation window
            EnsureAggregationStart(attackerIdentityId);
        }

        private IMyCubeGrid GetCurrentOrCachedGridForPlayer(long identityId)
        {
            IMyCubeGrid cached;
            if (_gridCache.TryGetValue(identityId, out cached) && cached != null && !cached.MarkedForClose)
                return cached;

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
            if (!IsAllowedLootType(defId) || grid == null)
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

                    // Binary search for the max amount that fits [0..remaining]
                    int lo = 0, hi = remaining;
                    while (lo < hi)
                    {
                        int mid = (lo + hi + 1) >> 1; // upper mid to prevent infinite loop
                        MyFixedPoint test = (MyFixedPoint)mid;
                        if (inv.CanItemsBeAdded(test, defId))
                            lo = mid;
                        else
                            hi = mid - 1;
                    }

                    if (lo > 0)
                    {
                        inv.AddItems((MyFixedPoint)lo, phys);
                        remaining -= lo;
                    }
                }
            }

            return remaining;
        }
    }
}