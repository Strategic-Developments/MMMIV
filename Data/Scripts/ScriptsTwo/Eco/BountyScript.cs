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

namespace Meridian.Economy
{
    [MySessionComponentDescriptor(MyUpdateOrder.AfterSimulation)]
    public class WarBountyPayouts : MySessionComponentBase
    {
        private const int PayoutIntervalTicks = 1 * 60;
        private const int PayoutIntervalCombatEndTicks = 30 * 60;
        private const long PlayerKillBounty = 50000;
        private const float PAYOUT_RATIO = 0.75f;

        private bool _registered;

        private static readonly MyStringHash Damage_Deformation = MyStringHash.GetOrCompute("Deformation");
        private static readonly MyStringHash Damage_Grinding = MyStringHash.GetOrCompute("Grinding");

        private readonly Dictionary<long, long> _pending = new Dictionary<long, long>();
        private readonly Dictionary<long, long> _pendingLastHit = new Dictionary<long, long>();
        private static readonly Dictionary<string, int> _tmpMissing = new Dictionary<string, int>();

        // Player cache to reduce repeated scans
        private readonly Dictionary<long, IMyPlayer> _playerCache = new Dictionary<long, IMyPlayer>();
        private readonly List<IMyPlayer> _playerQueryBuffer = new List<IMyPlayer>(8);
        private readonly List<long> _longDumpList = new List<long>();
        public override void BeforeStart()
        {
            if (MyAPIGateway.Multiplayer != null && MyAPIGateway.Multiplayer.IsServer)
            {
                TryRegisterDamageHooks();
            }
        }

        protected override void UnloadData()
        {
            _pending.Clear();
            _tmpMissing.Clear();

            _playerCache.Clear();
            _registered = false;
        }

        public override void UpdateAfterSimulation()
        {
            if (MyAPIGateway.Multiplayer == null || !MyAPIGateway.Multiplayer.IsServer)
                return;

            if (!_registered)
                TryRegisterDamageHooks();


            if (MyAPIGateway.Session.GameplayFrameCounter % PayoutIntervalTicks == 0)
            {
                PayAggregatedBounties();
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
            // Skip common non-combat damage types
            if (info.Type == Damage_Deformation || info.Type == Damage_Grinding)
                return;

            // Character killed
            var ch = target as IMyCharacter;
            if (ch != null)
            {
                HandleCharacterDeath(ch, info);
                return;
            }

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

            if (MyAPIGateway.Session?.Factions == null)
                return;

            var atkFac = MyAPIGateway.Session.Factions.TryGetPlayerFaction(attackerId);
            var vicFac = MyAPIGateway.Session.Factions.TryGetPlayerFaction(defenderId);
            if (!MyAPIGateway.Session.Factions.AreFactionsEnemies(atkFac.FactionId, vicFac.FactionId))
                return;

            MyFixedPoint price;
            if (PriceChanger.Instance.Costs.AllBlockCosts.TryGetValue(slim.BlockDefinition.Id, out price))
            {
                price += GetHydrogenBonusByLiters(slim);

                if (price > 0)
                    QueuePayout(attackerId, (long)(price * PAYOUT_RATIO));
            }


        }

        private void HandleCharacterDeath(IMyCharacter ch, MyDamageInformation info)
        {
            if (MyAPIGateway.Session == null)
                return;

            long victimId = 0;
            var victimPlayer = MyAPIGateway.Players != null ? MyAPIGateway.Players.GetPlayerControllingEntity(ch) : null;
            if (victimPlayer != null)
                victimId = victimPlayer.IdentityId;
            if (victimId == 0)
                return;

            long killerId;
            if (!TryResolveAttackerIdentity(info.AttackerId, out killerId) || killerId == 0)
                return;

            if (killerId == victimId)
                return;

            if (MyAPIGateway.Session?.Factions == null)
                return;

            var atkFac = MyAPIGateway.Session.Factions.TryGetPlayerFaction(killerId);
            var vicFac = MyAPIGateway.Session.Factions.TryGetPlayerFaction(victimId);
            if (!MyAPIGateway.Session.Factions.AreFactionsEnemies(atkFac.FactionId, vicFac.FactionId))
                return;

            if (PlayerKillBounty > 0)
                QueuePayout(killerId, PlayerKillBounty);
        }

        private void QueuePayout(long identityId, long amount)
        {
            long existing;
            if (_pending.TryGetValue(identityId, out existing))
                _pending[identityId] = existing + amount;
            else
                _pending[identityId] = amount;

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

            _pendingLastHit[identityId] = MyAPIGateway.Session.GameplayFrameCounter;
        }

        private void PayAggregatedBounties()
        {
            if (_pending.Count == 0)
                return;


            for (int i = 0; i < _pending.Count; i++)
            {
                var kv = _pending.ElementAt(i);

                long identityId = kv.Key;
                long amount = kv.Value;

                if (amount <= 0)
                {
                    _longDumpList.Add(identityId);
                    continue;
                }

                long lasthit;
                if (!_pendingLastHit.TryGetValue(identityId, out lasthit))
                {
                    continue;
                }
                if (MyAPIGateway.Session.GameplayFrameCounter - lasthit <= PayoutIntervalCombatEndTicks)
                {
                    continue;
                }

                IMyPlayer player;
                if (!_playerCache.TryGetValue(identityId, out player) || player == null)
                {
                    _playerQueryBuffer.Clear();
                    MyAPIGateway.Players.GetPlayers(_playerQueryBuffer, p => p != null && p.IdentityId == identityId);
                    if (_playerQueryBuffer.Count == 0)
                    {
                        continue; // Player not online; keep pending
                    }
                    player = _playerQueryBuffer[0];
                    _playerCache[identityId] = player;
                }

                player.RequestChangeBalance(amount);

                MyVisualScriptLogicProvider.SendChatMessageColored(
                    $"Combat completed, you've received {amount:n0} SC in aggregated bounties.",
                    new Color(0, 122, 255),
                    "Conflict Commissariat",
                    identityId,
                    "Blue"
                );
                _longDumpList.Add(identityId);
                _pending[identityId] = 0;
            }

            foreach (var l in _longDumpList)
            {
                _pendingLastHit.Remove(l);
                _pending.Remove(l);
            }
            _longDumpList.Clear();
        }

        private static string GetPlayerName(long identityId)
        {
            if (identityId == 0 || MyAPIGateway.Players == null)
                return "Unknown";
            var list = new List<IMyPlayer>();
            MyAPIGateway.Players.GetPlayers(list, p => p != null && p.IdentityId == identityId);
            return (list.Count > 0 && list[0] != null) ? (list[0].DisplayName ?? "Unknown") : "Unknown";
        }

        private static MyFixedPoint GetHydrogenBonusByLiters(IMySlimBlock slim)
        {
            var tank = slim.FatBlock as IMyGasTank;
            if (tank == null)
                return 0;

            double liters = tank.Capacity * tank.FilledRatio;
            if (liters <= 0)
                return 0;

            MyGasTankDefinition def = tank.SlimBlock.BlockDefinition as MyGasTankDefinition;

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
    }
}