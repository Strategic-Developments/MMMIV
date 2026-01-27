using Sandbox.Definitions;
using Sandbox.Game;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.Entity;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRage.Utils;
using VRageMath;



[MySessionComponentDescriptor(MyUpdateOrder.AfterSimulation)]
public class DeclareWarWatcher : MySessionComponentBase
{
    private readonly Dictionary<string, MyRelationsBetweenFactions> _rel = new Dictionary<string, MyRelationsBetweenFactions>();
    private int _tick;
    private bool _initialized;

    public override void LoadData()
    {
        if (MyAPIGateway.Session == null || !MyAPIGateway.Multiplayer.IsServer)
            return;

        var facs = MyAPIGateway.Session.Factions;
        foreach (var a in facs.Factions)
        {
            foreach (var b in facs.Factions)
            {
                if (a.Key >= b.Key) continue;
                var key = PairKey(a.Key, b.Key);
                var rel = facs.GetRelationBetweenFactions(a.Key, b.Key);
                _rel[key] = rel;
            }
        }

        _initialized = true;
    }

    public override void BeforeStart()
    {
        if (_initialized && MyAPIGateway.Multiplayer.IsServer)
        {
            MyVisualScriptLogicProvider.SendChatMessageColored(
                message: "Declare War Watcher initialized successfully.",
                color: new Color(0, 122, 255),
                author: "Conflict Commissariat",
                playerId: 0,
                font: "Blue"
            );
        }
    }

    public override void UpdateAfterSimulation()
    {
        if (MyAPIGateway.Session == null || !MyAPIGateway.Multiplayer.IsServer)
            return;

        _tick++;
        if (_tick % 300 != 0)
            return;

        var facs = MyAPIGateway.Session.Factions;
        foreach (var a in facs.Factions)
        {
            foreach (var b in facs.Factions)
            {
                if (a.Key >= b.Key) continue;

                var key = PairKey(a.Key, b.Key);
                var now = facs.GetRelationBetweenFactions(a.Key, b.Key);

                MyRelationsBetweenFactions before;
                if (!_rel.TryGetValue(key, out before))
                {
                    _rel[key] = now;
                    continue;
                }

                if (before != MyRelationsBetweenFactions.Enemies && now == MyRelationsBetweenFactions.Enemies)
                {
                    AnnounceWar(a.Value, b.Value);
                }

                if (before == MyRelationsBetweenFactions.Enemies && now != MyRelationsBetweenFactions.Enemies)
                {
                    AnnouncePeace(a.Value, b.Value, now);
                }

                if (before != now)
                    _rel[key] = now;
            }
        }
    }

    protected override void UnloadData()
    {
        _rel.Clear();
    }

    private static string PairKey(long a, long b)
    {
        return a < b ? a.ToString() + ":" + b.ToString() : b.ToString() + ":" + a.ToString();
    }

    private static void AnnounceWar(IMyFaction fa, IMyFaction fb)
    {
        var nameA = $"{fa.Tag} ({fa.Name})";
        var nameB = $"{fb.Tag} ({fb.Name})";

        MyVisualScriptLogicProvider.SendChatMessageColored(
            message: $"War declared between {nameA} and {nameB}.",
            color: new Color(0, 122, 255),
            author: "Conflict Commissariat",
            playerId: 0,
            font: "Blue"
        );
    }

    private static void AnnouncePeace(IMyFaction fa, IMyFaction fb, MyRelationsBetweenFactions newRel)
    {
        var nameA = $"{fa.Tag} ({fa.Name})";
        var nameB = $"{fb.Tag} ({fb.Name})";

        var status = newRel == MyRelationsBetweenFactions.Friends ? "Peace accepted (Allied)" : "Peace accepted";
        MyVisualScriptLogicProvider.SendChatMessageColored(
            message: $"{status} between {nameA} and {nameB}.",
            color: new Color(0, 122, 255),
            author: "Conflict Commissariat",
            playerId: 0,
            font: "Blue"
        );
    }
}



[MySessionComponentDescriptor(MyUpdateOrder.AfterSimulation)]
public class WarBountyPayouts : MySessionComponentBase
{
    private const int PayoutIntervalSeconds = 1;

    private const long DefaultPayoutLargeGrid = 2000;
    private const long DefaultPayoutSmallGrid = 1000;

    private static readonly Dictionary<string, long> PayoutBySubtype = new Dictionary<string, long>()
    {
        { "LargeBlockLargeTurret", 25000 },
        { "SmallGatlingGun",        8000 },
        { "LargeBlockBatteryBlock", 12000 },
    };

    private static readonly Dictionary<string, long> BonusByComponentSubtypeUpper = new Dictionary<string, long>()
    {
        { "THRUST",         50000 },
        { "SUPERCONDUCTOR", 10000 },
        { "POWERCELL",       7500 },
    };

    private const double HydrogenTankCreditsPerLiter = 2.0;
    private const long PlayerKillBounty = 50000;

    private int _payoutIntervalTicks;
    private bool _registered;
    private int _tick;

    // Proximity detection (unused currently, left for potential future logic)
    private readonly BoundingSphereD _detSphere = new BoundingSphereD();
    private readonly List<MyEntity> _detEntities = new List<MyEntity>(64);

    private static readonly MyStringHash Damage_Deformation = MyStringHash.GetOrCompute("Deformation");
    private static readonly MyStringHash Damage_Grinding = MyStringHash.GetOrCompute("Grinding");

    private readonly Dictionary<long, long> _pending = new Dictionary<long, long>();
    private static readonly Dictionary<string, int> _tmpMissing = new Dictionary<string, int>();

    // Player cache to reduce repeated scans
    private readonly Dictionary<long, IMyPlayer> _playerCache = new Dictionary<long, IMyPlayer>();
    private readonly List<IMyPlayer> _playerQueryBuffer = new List<IMyPlayer>(8);

    // Per-tick faction enemy relation cache
    private readonly Dictionary<string, bool> _enemyRelCache = new Dictionary<string, bool>(64);
    private int _enemyRelCacheTick;

    private static readonly Type TYPEOF_IMyCubeGrid = typeof(IMyCubeGrid);
    private const string TypeIdPrefix = "MyObjectBuilder_";

    public override void BeforeStart()
    {
        if (MyAPIGateway.Multiplayer != null && MyAPIGateway.Multiplayer.IsServer)
        {
            _payoutIntervalTicks = PayoutIntervalSeconds * 60;
            TryRegisterDamageHooks();
        }
    }

    protected override void UnloadData()
    {
        _pending.Clear();
        _tmpMissing.Clear();

        _playerCache.Clear();
        _enemyRelCache.Clear();
        _registered = false;
    }

    public override void UpdateAfterSimulation()
    {
        if (MyAPIGateway.Multiplayer == null || !MyAPIGateway.Multiplayer.IsServer)
            return;

        if (!_registered)
            TryRegisterDamageHooks();

        _tick++;

        // Reset relation cache each tick to bound memory and ensure freshness
        if (_enemyRelCacheTick != _tick)
        {
            _enemyRelCache.Clear();
            _enemyRelCacheTick = _tick;
        }

        if (_tick >= _payoutIntervalTicks)
        {
            _tick = 0;
            PayAggregatedBounties();
        }
    }

    private void TryRegisterDamageHooks()
    {
        if (_registered) return;
        if (MyAPIGateway.Session?.DamageSystem == null) return;

        MyAPIGateway.Session.DamageSystem.RegisterDestroyHandler(0, OnDestroyed);
        _registered = true;
    }

    public static bool IsNPCFaction(IMyFaction faction) => faction.IsEveryoneNpc();

    private void OnDestroyed(object target, MyDamageInformation info)
    {
        // Skip common non-combat damage types
        if (info.Type == Damage_Deformation || info.Type == Damage_Grinding) return;

        // Character killed
        var ch = target as IMyCharacter;
        if (ch != null)
        {
            HandleCharacterDeath(ch, info);
            return;
        }

        // Block destroyed
        var slim = target as IMySlimBlock;
        if (slim == null || slim.CubeGrid == null) return;

        long defenderId = GetPrimaryOwnerIdentity(slim.CubeGrid);
        if (defenderId == 0) return;

        long attackerId;
        if (!TryResolveAttackerIdentity(info.AttackerId, out attackerId) || attackerId == 0)
            return;

        var facs = MyAPIGateway.Session.Factions;
        if (facs == null) return;

        var atkFac = facs.TryGetPlayerFaction(attackerId);
        var defFac = facs.TryGetPlayerFaction(defenderId);
        if (!AreEnemyFactions(facs, atkFac, defFac)) return;

        string subtypeRaw = GetTrueSubtypeID(slim);

        long payout;
        if (!PayoutBySubtype.TryGetValue(subtypeRaw, out payout))
        {
            payout = (slim.CubeGrid.GridSizeEnum == VRage.Game.MyCubeSize.Large)
                ? DefaultPayoutLargeGrid
                : DefaultPayoutSmallGrid;
        }

        payout += GetComponentBonuses(slim);
        payout += GetHydrogenBonusByLiters(slim, subtypeRaw);

        if (payout > 0)
            QueuePayout(attackerId, payout);
    }

    private void HandleCharacterDeath(IMyCharacter ch, MyDamageInformation info)
    {
        if (MyAPIGateway.Session == null) return;

        long victimId = 0;
        var victimPlayer = MyAPIGateway.Players != null ? MyAPIGateway.Players.GetPlayerControllingEntity(ch) : null;
        if (victimPlayer != null)
            victimId = victimPlayer.IdentityId;
        if (victimId == 0) return;

        long killerId;
        if (!TryResolveAttackerIdentity(info.AttackerId, out killerId) || killerId == 0)
            return;

        if (killerId == victimId) return;

        var facs = MyAPIGateway.Session.Factions;
        if (facs == null) return;

        var atkFac = facs.TryGetPlayerFaction(killerId);
        var vicFac = facs.TryGetPlayerFaction(victimId);
        if (!AreEnemyFactions(facs, atkFac, vicFac)) return;

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
    }

    private void PayAggregatedBounties()
    {
        if (_pending.Count == 0) return;

        // Snapshot to avoid modifying while iterating
        var snapshot = new List<KeyValuePair<long, long>>(_pending);
        foreach (var kv in snapshot)
        {
            long identityId = kv.Key;
            long amount = kv.Value;
            if (amount <= 0) continue;

            IMyPlayer player;
            if (!_playerCache.TryGetValue(identityId, out player) || player == null)
            {
                _playerQueryBuffer.Clear();
                MyAPIGateway.Players.GetPlayers(_playerQueryBuffer, p => p != null && p.IdentityId == identityId);
                if (_playerQueryBuffer.Count == 0) continue; // Player not online; keep pending
                player = _playerQueryBuffer[0];
                _playerCache[identityId] = player;
            }

            player.RequestChangeBalance(amount);

            MyVisualScriptLogicProvider.SendChatMessageColored(
                string.Format("You've received {0:n0}c in aggregated bounties.", amount),
                new Color(0, 122, 255),
                "Conflict Commissariat",
                identityId,
                "Blue"
            );

            _pending[identityId] = 0;
        }

        // Remove zeroed entries
        foreach (var kv in snapshot)
        {
            long cur;
            if (_pending.TryGetValue(kv.Key, out cur) && cur == 0)
                _pending.Remove(kv.Key);
        }
    }

    public static string GetTrueSubtypeID(IMySlimBlock block)
    {
        string subtype = block.BlockDefinition.Id.SubtypeName;
        if (!string.IsNullOrEmpty(subtype))
            return subtype;

        // Fallback: derive from TypeId safely (avoid magic number 16)
        string typeIdStr = block.BlockDefinition.Id.TypeId.ToString();
        if (!string.IsNullOrEmpty(typeIdStr) && typeIdStr.StartsWith(TypeIdPrefix))
            return typeIdStr.Substring(TypeIdPrefix.Length);

        return typeIdStr ?? string.Empty;
    }

    private static string GetPlayerName(long identityId)
    {
        if (identityId == 0 || MyAPIGateway.Players == null) return "Unknown";
        var list = new List<IMyPlayer>();
        MyAPIGateway.Players.GetPlayers(list, p => p != null && p.IdentityId == identityId);
        return (list.Count > 0 && list[0] != null) ? (list[0].DisplayName ?? "Unknown") : "Unknown";
    }

    private static long GetComponentBonuses(IMySlimBlock slim)
    {
        var def = MyDefinitionManager.Static.GetCubeBlockDefinition(slim.BlockDefinition.Id);
        if (def == null || def.Components == null) return 0;

        _tmpMissing.Clear();
        slim.GetMissingComponents(_tmpMissing);

        long total = 0;
        for (int i = 0; i < def.Components.Length; i++)
        {
            var compDef = def.Components[i];
            var subtype = compDef.Definition.Id.SubtypeName;
            if (string.IsNullOrEmpty(subtype)) continue;

            long perUnitBonus;
            if (!BonusByComponentSubtypeUpper.TryGetValue(subtype.ToUpperInvariant(), out perUnitBonus))
                continue;

            int required = compDef.Count;
            int missing;
            if (!_tmpMissing.TryGetValue(subtype, out missing))
                missing = 0;

            int present = required - missing;
            if (present > 0)
                total += perUnitBonus * (long)present;
        }

        _tmpMissing.Clear();
        return total;
    }

    private static long GetHydrogenBonusByLiters(IMySlimBlock slim, string subtypeRaw)
    {
        var tank = slim.FatBlock as Sandbox.ModAPI.IMyGasTank;
        if (tank == null) return 0;

        string up = (subtypeRaw ?? "").ToUpperInvariant();
        if (up.IndexOf("HYDROGEN", StringComparison.Ordinal) < 0) return 0;

        double liters = tank.Capacity * tank.FilledRatio;
        if (liters <= 0) return 0;

        long bonus = (long)Math.Round(liters * HydrogenTankCreditsPerLiter);
        return bonus > 0 ? bonus : 0;
    }

    private static long GetPrimaryOwnerIdentity(IMyCubeGrid grid)
    {
        if (grid.BigOwners != null && grid.BigOwners.Count > 0 && grid.BigOwners[0] != 0) return grid.BigOwners[0];
        if (grid.SmallOwners != null && grid.SmallOwners.Count > 0 && grid.SmallOwners[0] != 0) return grid.SmallOwners[0];
        return 0;
    }

    private static bool TryResolveAttackerIdentity(long attackerEntityId, out long identityId)
    {
        identityId = 0;
        IMyEntity ent;
        if (!MyAPIGateway.Entities.TryGetEntityById(attackerEntityId, out ent) || ent == null) return false;

        var top = ent.GetTopMostParent();

        var player = MyAPIGateway.Players != null ? MyAPIGateway.Players.GetPlayerControllingEntity(top) : null;
        if (player != null)
        {
            identityId = player.IdentityId;
            if (identityId != 0) return true;
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

    private bool AreEnemyFactions(IMyFactionCollection facs, IMyFaction atkFac, IMyFaction defFac)
    {
        if (facs == null || atkFac == null || defFac == null) return false;

        // Cache results per tick keyed by pair
        string key = atkFac.FactionId < defFac.FactionId
            ? atkFac.FactionId.ToString() + ":" + defFac.FactionId.ToString()
            : defFac.FactionId.ToString() + ":" + atkFac.FactionId.ToString();

        bool cached;
        if (_enemyRelCache.TryGetValue(key, out cached))
            return cached;

        bool res = facs.AreFactionsEnemies(atkFac.FactionId, defFac.FactionId);
        _enemyRelCache[key] = res;
        return res;
    }
}