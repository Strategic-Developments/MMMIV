using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sandbox.Definitions;
using Sandbox.ModAPI;
using VRage.Game;
using VRage.Utils;
using VRageMath;
using static Scripts.Structure;
using static Scripts.Structure.ArmorDefinition.ArmorType;
using static Scripts.Structure.WeaponDefinition;
using static Scripts.Structure.WeaponDefinition.AmmoDef;
using static Scripts.Structure.WeaponDefinition.AmmoDef.AreaOfDamageDef;
using static Scripts.Structure.WeaponDefinition.AmmoDef.AreaOfDamageDef.AoeShape;
using static Scripts.Structure.WeaponDefinition.AmmoDef.AreaOfDamageDef.Falloff;
using static Scripts.Structure.WeaponDefinition.AmmoDef.DamageScaleDef;
using static Scripts.Structure.WeaponDefinition.AmmoDef.DamageScaleDef.CustomScalesDef.SkipMode;
using static Scripts.Structure.WeaponDefinition.AmmoDef.DamageScaleDef.DamageTypes.Damage;
using static Scripts.Structure.WeaponDefinition.AmmoDef.DamageScaleDef.DeformDef.DeformTypes;
using static Scripts.Structure.WeaponDefinition.AmmoDef.DamageScaleDef.ShieldDef.ShieldType;
using static Scripts.Structure.WeaponDefinition.AmmoDef.EjectionDef;
using static Scripts.Structure.WeaponDefinition.AmmoDef.EjectionDef.SpawnType;
using static Scripts.Structure.WeaponDefinition.AmmoDef.EwarDef;
using static Scripts.Structure.WeaponDefinition.AmmoDef.EwarDef.EwarMode;
using static Scripts.Structure.WeaponDefinition.AmmoDef.EwarDef.EwarType;
using static Scripts.Structure.WeaponDefinition.AmmoDef.EwarDef.PushPullDef.Force;
using static Scripts.Structure.WeaponDefinition.AmmoDef.FragmentDef;
using static Scripts.Structure.WeaponDefinition.AmmoDef.FragmentDef.TimedSpawnDef.PointTypes;
using static Scripts.Structure.WeaponDefinition.AmmoDef.GraphicDef;
using static Scripts.Structure.WeaponDefinition.AmmoDef.GraphicDef.DecalDef;
using static Scripts.Structure.WeaponDefinition.AmmoDef.GraphicDef.LineDef;
using static Scripts.Structure.WeaponDefinition.AmmoDef.GraphicDef.LineDef.FactionColor;
using static Scripts.Structure.WeaponDefinition.AmmoDef.GraphicDef.LineDef.Texture;
using static Scripts.Structure.WeaponDefinition.AmmoDef.GraphicDef.LineDef.TracerBaseDef;
using static Scripts.Structure.WeaponDefinition.AmmoDef.PatternDef.PatternModes;
using static Scripts.Structure.WeaponDefinition.AmmoDef.ShapeDef.Shapes;
using static Scripts.Structure.WeaponDefinition.AmmoDef.TrajectoryDef;
using static Scripts.Structure.WeaponDefinition.AmmoDef.TrajectoryDef.ApproachDef;
using static Scripts.Structure.WeaponDefinition.AmmoDef.TrajectoryDef.ApproachDef.ConditionOperators;
using static Scripts.Structure.WeaponDefinition.AmmoDef.TrajectoryDef.ApproachDef.Conditions;
using static Scripts.Structure.WeaponDefinition.AmmoDef.TrajectoryDef.ApproachDef.FwdRelativeTo;
using static Scripts.Structure.WeaponDefinition.AmmoDef.TrajectoryDef.ApproachDef.ReInitCondition;
using static Scripts.Structure.WeaponDefinition.AmmoDef.TrajectoryDef.ApproachDef.RelativeTo;
using static Scripts.Structure.WeaponDefinition.AmmoDef.TrajectoryDef.ApproachDef.StageEvents;
using static Scripts.Structure.WeaponDefinition.AmmoDef.TrajectoryDef.ApproachDef.UpRelativeTo;
using static Scripts.Structure.WeaponDefinition.AmmoDef.TrajectoryDef.GuidanceType;
using static Scripts.Structure.WeaponDefinition.AnimationDef;
using static Scripts.Structure.WeaponDefinition.AnimationDef.PartAnimationSetDef.EventTriggers;
using static Scripts.Structure.WeaponDefinition.AnimationDef.RelMove;
using static Scripts.Structure.WeaponDefinition.AnimationDef.RelMove.MoveType;

namespace Scripts
{
    partial class Parts
    {
        bool InitDefinitions = false;
        VRage.Collections.DictionaryValuesReader<MyDefinitionId, MyDefinitionBase>? Definitions;

        internal static ArmorDefinition CreateFromWeapons(
            IEnumerable<WeaponDefinition> weps,
            ArmorDefinition.ArmorType type,
            float EnergyDamageMult,
            float KineticDamageMult
        )
        {
            HashSet<string> ids = new HashSet<string>();

            foreach (var weapondef in weps)
            {
                foreach (var block in weapondef.Assignments.MountPoints)
                {
                    ids.Add(block.SubtypeId);
                }
            }
            ArmorDefinition ret = new ArmorDefinition
            {
                EnergeticResistance = EnergyDamageMult,
                KineticResistance = KineticDamageMult,
                Kind = type,
                SubtypeIds = ids.ToArray(),
            };
            return ret;
        }

        internal IEnumerable<T> ToEnumerable<T>(params T[] defs)
        {
            return defs;
        }

        internal IEnumerable<T> Compile<T>(params IEnumerable<T>[] defs)
        {
            foreach (var arr in defs)
            foreach (var def in arr)
                yield return def;
        }

        internal IEnumerable<ArmorDefinition> CreateThrusterDefinitions()
        {
            if (!InitDefinitions)
            {
                Definitions = MyDefinitionManager.Static.GetAllDefinitions();
                InitDefinitions = true;
            }

            foreach (var def in Definitions)
            {
                if (def is MyThrustDefinition)
                {
                    var thrustdef = def as MyThrustDefinition;

                    if (
                        thrustdef.Id.SubtypeName == "SmallBlockPrototechThruster"
                        || thrustdef.Id.SubtypeName == "LargeBlockPrototechThruster"
                    )
                    {
                        yield return new ArmorDefinition
                        {
                            EnergeticResistance = 0.75f,
                            KineticResistance = 0.75f,
                            Kind = NonArmor,
                            SubtypeIds = new[] { thrustdef.Id.SubtypeName },
                        };
                        continue;
                    }

                    var volume = thrustdef.Size.Volume();

                    // yeah yeah this could be more efficient data size wise but idc
                    yield return new ArmorDefinition
                    {
                        EnergeticResistance = Math.Pow(volume, 1 / 3f),
                        KineticResistance = 1f,
                        Kind = NonArmor,
                        SubtypeIds = new[] { thrustdef.Id.SubtypeName },
                    };
                }
            }
        }

        internal float RotateSpeed(float Deg)
        {
            return Deg * ROTATE_CONSTANT;
        }

        internal string[] Copy(string subtype, int amount)
        {
            string[] strs = new string[amount];

            for (int i = 0; i < amount; i++)
                strs[i] = subtype;

            return strs;
        }

        internal string[] CopyWithInitial(string initial, string subtype, int amount)
        {
            string[] strs = new string[amount + 1];

            strs[0] = initial;
            for (int i = 0; i < amount; i++)
                strs[i + 1] = subtype;

            return strs;
        }

        internal string[] CopyWithEnd(string end, string subtype, int amount)
        {
            string[] strs = new string[amount + 1];

            strs[amount] = end;
            for (int i = 0; i < amount; i++)
                strs[i] = subtype;

            return strs;
        }

        internal LineDef Laser(Vector4 color, float width)
        {
            return new LineDef
            {
                TracerMaterial = "WeaponLaser",
                ColorVariance = Random(start: 1f, end: 1f), // multiply the color by random values within range.
                WidthVariance = Random(start: 0f, end: 0f), // adds random value to default width (negatives shrinks width)
                DropParentVelocity = true, // If set to true will not take on the parents (grid/player) initial velocity when rendering.
                Tracer = new TracerBaseDef
                {
                    Enable = true,
                    Length = 1, //
                    Width = width, //
                    Color = color, // RBG 255 is Neon Glowing, 100 is Quite Bright.
                    FactionColor = DontUse, // DontUse, Foreground, Background.
                    VisualFadeStart = 0, // Number of ticks the weapon has been firing before projectiles begin to fade their color
                    VisualFadeEnd = 0, // How many ticks after fade began before it will be invisible.
                    AlwaysDraw = false, // Prevents this tracer from being culled.  Only use if you have a reason too (very long tracers/trails).
                    Textures = new[]
                    { // WeaponLaser, ProjectileTrailLine, WarpBubble, etc..
                        "WeaponLaser", // Please always have this Line set, if this Section is enabled.
                    },
                    TextureMode = Normal, // Normal, Cycle, Chaos, Wave
                    Segmentation = new SegmentDef
                    {
                        Enable = false, // If true Tracer TextureMode is ignored
                    },
                },
            };
        }

        internal LineDef GenericTracer(
            Vector4 ShellColor,
            float ShellLength,
            float ShellWidth,
            Vector4 TrailColor,
            float TrailWidth,
            int TrailLength,
            bool UseColorFade = false
        )
        {
            return new LineDef
            {
                ColorVariance = Random(start: 1f, end: 1f), // multiply the color by random values within range.
                WidthVariance = Random(start: 0f, end: 0f), // adds random value to default width (negatives shrinks width)
                DropParentVelocity = true, // If set to true will not take on the parents (grid/player) initial velocity when rendering.
                Tracer = new TracerBaseDef
                {
                    Enable = true,
                    Length = ShellLength, //
                    Width = ShellWidth, //
                    Color = ShellColor, // RBG 255 is Neon Glowing, 100 is Quite Bright.
                    FactionColor = DontUse, // DontUse, Foreground, Background.
                    VisualFadeStart = 0, // Number of ticks the weapon has been firing before projectiles begin to fade their color
                    VisualFadeEnd = 0, // How many ticks after fade began before it will be invisible.
                    AlwaysDraw = false, // Prevents this tracer from being culled.  Only use if you have a reason too (very long tracers/trails).
                    Textures = new[]
                    { // WeaponLaser, ProjectileTrailLine, WarpBubble, etc..
                        "ProjectileTrailLine", // Please always have this Line set, if this Section is enabled.
                    },
                    TextureMode = Normal, // Normal, Cycle, Chaos, Wave
                    Segmentation = new SegmentDef
                    {
                        Enable = false, // If true Tracer TextureMode is ignored
                        Textures = new[]
                        {
                            "", // Please always have this Line set, if this Section is enabled.
                        },
                        SegmentLength = 0f, // Uses the values below.
                        SegmentGap = 0f, // Uses Tracer textures and values
                        Speed = 1f, // meters per second
                        Color = Color(red: 1, green: 2, blue: 2.5f, alpha: 1),
                        FactionColor = DontUse, // DontUse, Foreground, Background.
                        WidthMultiplier = 1f,
                        Reverse = false,
                        UseLineVariance = true,
                        WidthVariance = Random(start: 0f, end: 0f),
                        ColorVariance = Random(start: 0f, end: 0f),
                    },
                },
                Trail = new TrailDef
                {
                    Enable = TrailWidth != 0,
                    AlwaysDraw = false, // Prevents this tracer from being culled.  Only use if you have a reason too (very long tracers/trails).
                    Textures = new[]
                    {
                        "WeaponLaser", // Please always have this Line set, if this Section is enabled.
                    },
                    TextureMode = Normal,
                    DecayTime = TrailLength, // In Ticks. 1 = 1 Additional Tracer generated per motion, 33 is 33 lines drawn per projectile. Keep this number low.
                    Color = TrailColor,
                    FactionColor = DontUse, // DontUse, Foreground, Background.
                    Back = false,
                    CustomWidth = TrailWidth,
                    UseWidthVariance = false,
                    UseColorFade = UseColorFade,
                },
                OffsetEffect = new OffsetEffectDef
                {
                    MaxOffset = 0, // 0 offset value disables this effect
                    MinLength = 0.2f,
                    MaxLength = 3,
                },
            };
        }

        internal LineDef Rocket(
            Vector4 ShellColor,
            float ShellLength,
            float ShellWidth,
            Vector4 TrailColor,
            float TrailWidth,
            int TrailLength,
            float MaxOffset,
            float MinLength,
            float MaxLength
        )
        {
            return new LineDef
            {
                TracerMaterial = "WeaponLaser",
                ColorVariance = Random(start: 1f, end: 1f), // multiply the color by random values within range.
                WidthVariance = Random(start: 0f, end: 0f), // adds random value to default width (negatives shrinks width)
                DropParentVelocity = true, // If set to true will not take on the parents (grid/player) initial velocity when rendering.

                Tracer = new TracerBaseDef
                {
                    Enable = true,
                    Length = ShellLength, //
                    Width = ShellWidth, //
                    Color = ShellColor, // RBG 255 is Neon Glowing, 100 is Quite Bright.
                    FactionColor = DontUse, // DontUse, Foreground, Background.
                    VisualFadeStart = 0, // Number of ticks the weapon has been firing before projectiles begin to fade their color
                    VisualFadeEnd = 0, // How many ticks after fade began before it will be invisible.
                    AlwaysDraw = false, // Prevents this tracer from being culled.  Only use if you have a reason too (very long tracers/trails).
                    Textures = new[]
                    { // WeaponLaser, ProjectileTrailLine, WarpBubble, etc..
                        "ProjectileTrailLine", // Please always have this Line set, if this Section is enabled.
                    },
                    TextureMode = Normal, // Normal, Cycle, Chaos, Wave
                    Segmentation = new SegmentDef
                    {
                        Enable = false, // If true Tracer TextureMode is ignored
                        Textures = new[]
                        {
                            "", // Please always have this Line set, if this Section is enabled.
                        },
                        SegmentLength = 0f, // Uses the values below.
                        SegmentGap = 0f, // Uses Tracer textures and values
                        Speed = 1f, // meters per second
                        Color = Color(red: 1, green: 2, blue: 2.5f, alpha: 1),
                        FactionColor = DontUse, // DontUse, Foreground, Background.
                        WidthMultiplier = 1f,
                        Reverse = false,
                        UseLineVariance = true,
                        WidthVariance = Random(start: 0f, end: 0f),
                        ColorVariance = Random(start: 0f, end: 0f),
                    },
                },
                Trail = new TrailDef
                {
                    Enable = true,
                    AlwaysDraw = false, // Prevents this tracer from being culled.  Only use if you have a reason too (very long tracers/trails).
                    Textures = new[]
                    {
                        "WeaponLaser", // Please always have this Line set, if this Section is enabled.
                    },
                    TextureMode = Normal,
                    DecayTime = TrailLength, // In Ticks. 1 = 1 Additional Tracer generated per motion, 33 is 33 lines drawn per projectile. Keep this number low.
                    Color = TrailColor,
                    FactionColor = DontUse, // DontUse, Foreground, Background.
                    Back = false,
                    CustomWidth = TrailWidth,
                    UseWidthVariance = false,
                    UseColorFade = false,
                },
                OffsetEffect = new OffsetEffectDef
                {
                    MaxOffset = MaxOffset, // 0 offset value disables this effect
                    MinLength = MinLength,
                    MaxLength = MaxLength,
                },
            };
        }

        internal WeaponDefinition[] CompileWeapons(params IEnumerable<WeaponDefinition>[] weapons)
        {
            var AllWeapons = Compile(weapons);
            var modBlocks = new Dictionary<string, MyCubeBlockDefinition>();
            StringBuilder stringBuilder = new StringBuilder();

            if (!InitDefinitions)
            {
                Definitions = MyDefinitionManager.Static.GetAllDefinitions();
                InitDefinitions = true;
            }

            foreach (var definition in Definitions)
            {
                var def = definition as MyCubeBlockDefinition;

                if (def != null && def.Context.ModId == Session.I.ModContext.ModId)
                {
                    modBlocks.Add(
                        def.Id.SubtypeName == ""
                            ? def.Id.TypeId.ToString().Remove(0, 16)
                            : def.Id.SubtypeName,
                        def
                    );
                }
            }

            foreach (var weapon in AllWeapons)
            {
                if (
                    weapon.HardPoint.HardWare.Type
                    != HardPointDef.HardwareDef.HardwareType.BlockWeapon
                )
                    continue;

                foreach (var wep in weapon.Assignments.MountPoints)
                {
                    if (wep.SubtypeId.Contains("khalkeus"))
                        continue;

                    MyCubeBlockDefinition keendef;
                    if (modBlocks.TryGetValue(wep.SubtypeId, out keendef))
                    {
                        MyLog.Default.WriteLineAndConsole($"{wep.SubtypeId}: {weapon.HardPoint.PartName}");

                        if (keendef is MyLargeTurretBaseDefinition)
                        {
                            var turretdef = keendef as MyLargeTurretBaseDefinition;

                            var aziSpeed = Math.Min(weapon.HardPoint.HardWare.RotateRate / ROTATE_CONSTANT, FAST_ROTATE_SPEED);
                            var eleSpeed = Math.Min(weapon.HardPoint.HardWare.ElevateRate / ROTATE_CONSTANT, FAST_ROTATE_SPEED);

                            turretdef.RotationSpeed = aziSpeed / 55003.3333333f; // hello keen
                            turretdef.ElevationSpeed = eleSpeed / 55003.3333333f;
                        }

                        float AIRange = 0;
                        foreach (var ammo in weapon.Ammos)
                        {
                            if (AIRange < ammo.Trajectory.MaxTrajectory && ammo.HardPointUsable)
                                AIRange = ammo.Trajectory.MaxTrajectory;
                        }
                        var hardpoint = weapon.HardPoint;
                        var loading = hardpoint.Loading;
                        var targeting = weapon.Targeting;
                        stringBuilder.Append($"\n\nBASIC WEAPON STATS FOR '{hardpoint.PartName}':");

                        if (hardpoint.Ui.RateOfFire)
                            stringBuilder.Append(
                                $"\n{GetTabs(1)}ROF: {loading.RateOfFire * hardpoint.Ui.RateOfFireMin}-{loading.RateOfFire}RPM"
                            );
                        else
                            stringBuilder.Append($"\n{GetTabs(1)}ROF: {loading.RateOfFire}RPM");

                        if (loading.ReloadTime > 0)
                            stringBuilder.Append(
                                $"\n{GetTabs(1)}Reload Speed: {loading.ReloadTime / 60f:#.###}s"
                            );
                        if (loading.MagsToLoad > 0 && loading.MagsToLoad > 0)
                            stringBuilder.Append(
                                $"\n{GetTabs(1)}Mags Per Reload: {loading.MagsToLoad}"
                            );
                        if (loading.ReloadTime > 0 && loading.RateOfFire > 0)
                            stringBuilder.Append(
                                $"\n{GetTabs(1)}Shots In Burst: {loading.ShotsInBurst}"
                            );
                        if (loading.DelayAfterBurst > 0 && loading.RateOfFire > 0)
                            stringBuilder.Append(
                                $"\n{GetTabs(1)}Delay After Burst: {loading.DelayAfterBurst / 60f:#.###}s"
                            );
                        if (loading.HeatPerShot > 1)
                            stringBuilder.Append(
                                $"\n{GetTabs(1)}Heat Per Shot: {loading.HeatPerShot}"
                            );
                        stringBuilder.Append(
                            $"\n{GetTabs(1)}Range: {(targeting.MaxTargetDistance != 0 && hardpoint.Ai.TurretAttached ? targeting.MaxTargetDistance : AIRange)}m"
                        );
                        stringBuilder.Append(
                            $"\n{GetTabs(1)}Deviation: {hardpoint.DeviateShotAngle}°"
                        );
                        if (hardpoint.Ai.TurretAttached)
                        {
                            stringBuilder.Append(
                                $"\n{GetTabs(1)}Azimuth: {hardpoint.HardWare.MinAzimuth}° to {hardpoint.HardWare.MaxAzimuth}° "
                                    + $"@ {MathHelper.ToDegrees(hardpoint.HardWare.RotateRate) * 60f}°/s"
                            );
                            stringBuilder.Append(
                                $"\n{GetTabs(1)}Elevation: {hardpoint.HardWare.MinElevation}° to {hardpoint.HardWare.MaxElevation}° "
                                    + $"@ {MathHelper.ToDegrees(hardpoint.HardWare.ElevateRate) * 60f}°/s"
                            );
                        }

                        stringBuilder.Append($"\n\nAMMO STATS FOR '{hardpoint.PartName}'");
                        var templist = new List<string>();
                        foreach (var ammo in weapon.Ammos)
                        {
                            if (!ammo.HardPointUsable)
                            {
                                continue;
                            }

                            AppendAmmoStats(
                                stringBuilder,
                                ammo,
                                weapon.Ammos,
                                templist,
                                0,
                                keendef.CubeSize == MyCubeSize.Large
                            );
                            templist.Clear();
                        }

                        keendef.DescriptionString += stringBuilder.ToString();
                        stringBuilder.Clear();
                    }
                }

                // Scaler for energy per shot (EnergyCost * BaseDamage * (RateOfFire / 3600) * BarrelsPerShot * TrajectilesPerBarrel). Uses EffectStrength instead of BaseDamage if EWAR my ass
                foreach (var ammo in weapon.Ammos)
                {
                    if (
                        (
                            ammo.AmmoMagazine == "Energy"
                            || ammo.AmmoMagazine == ""
                            || ammo.HybridRound
                        )
                        && ammo.EnergyCost > 0
                    )
                    {
                        float mult = ammo.Ewar.Enable
                            ? ammo.Ewar.Strength
                            : ammo.BaseDamage
                                * (weapon.HardPoint.Loading.RateOfFire / 3600f)
                                * weapon.HardPoint.Loading.BarrelsPerShot
                                * weapon.HardPoint.Loading.TrajectilesPerBarrel;

                        if (mult != 0)
                        {
                            ammo.EnergyCost /= mult;
                        }
                    }
                    // obliterate this desync causer and I don't want to go through everything
                    // In kilograms; how much force the impact will apply to the target.
                }
            }

            return AllWeapons.ToArray();
        }

        private string GetTabs(int numTabs)
        {
            return new string(' ', numTabs * 2);
        }

        private void AppendAmmoStats(
            StringBuilder stringBuilder,
            AmmoDef ammo,
            AmmoDef[] ammos,
            List<string> visitedAmmos,
            int numTabs = 0,
            bool large = true
        )
        {
            visitedAmmos.Add(ammo.AmmoRound);

            bool IsEnergy =
                (ammo.AmmoMagazine == "Energy" || ammo.AmmoMagazine == "" || ammo.HybridRound)
                && ammo.EnergyCost > 0
                && ammo.HardPointUsable;

            stringBuilder.Append(
                $"\n{GetTabs(numTabs)}{(ammo.TerminalName == null || ammo.TerminalName == "" ? ammo.AmmoMagazine : ammo.TerminalName)}{(IsEnergy ? $" ({ammo.EnergyCost}MW)" : "")}\n{GetTabs(numTabs)}{{"
            );
            numTabs++;
            bool shouldShowScales = false;
            var gridScales = ammo.DamageScales;

            if (ammo.Health > 0)
            {
                stringBuilder.Append(
                       $"\n{GetTabs(numTabs)}Health: {ammo.Health}"
                   );
            }
            if (ammo.BaseDamage > 1 && !ammo.Ewar.Enable)
            {
                stringBuilder.Append(
                    $"\n{GetTabs(numTabs)}Pen Damage: {ammo.BaseDamage}, Dmg Type: {(ammo.NoGridOrArmorScaling ? Kinetic : gridScales.DamageType.Base)}"
                );
                shouldShowScales = true;
            }
            if (ammo.DamageScales.HealthHitModifier > 0 && !ammo.Ewar.Enable)
            {
                stringBuilder.Append(
                    $"\n{GetTabs(numTabs)}Dmg to projectiles: {ammo.DamageScales.HealthHitModifier}"
                );
            }
            if (ammo.BaseDamageCutoff > 0 && !ammo.Ewar.Enable)
            {
                stringBuilder.Append(
                    $"\n{GetTabs(numTabs)}Pen Damage Cutoff: {ammo.BaseDamageCutoff}"
                );
            }
            if (ammo.ObjectsHit.MaxObjectsHit > 0)
            {
                stringBuilder.Append(
                    $"\n{GetTabs(numTabs)}Max Blocks Penned: {ammo.ObjectsHit.MaxObjectsHit}"
                );
            }

            var AOE = ammo.AreaOfDamage;
            if (AOE.ByBlockHit.Enable && !ammo.Ewar.Enable)
            {
                stringBuilder.Append(
                    $"\n{GetTabs(numTabs)}BBH Expl Dmg: {AOE.ByBlockHit.Damage} over {AOE.ByBlockHit.Radius}m w/ {AOE.ByBlockHit.Depth}m depth"
                        + $"{GetTabs(numTabs)}Falloff: {AOE.ByBlockHit.Falloff}, Shape: {AOE.ByBlockHit.Shape}, Dmg Type: {(ammo.NoGridOrArmorScaling ? Energy : gridScales.DamageType.AreaEffect)}"
                );
                shouldShowScales = true;
            }
            if (AOE.EndOfLife.Enable && !ammo.Ewar.Enable)
            {
                stringBuilder.Append(
                    $"\n{GetTabs(numTabs)}EOL Expl Dmg: {AOE.EndOfLife.Damage} over {AOE.EndOfLife.Radius}m w/ {AOE.EndOfLife.Depth}m depth, "
                        + $"Falloff: {AOE.EndOfLife.Falloff}, Shape: {AOE.EndOfLife.Shape}, Dmg Type: {(ammo.NoGridOrArmorScaling ? Energy : gridScales.DamageType.Detonation)}"
                );
                shouldShowScales = true;
            }

            stringBuilder.Append(
                $"\n{GetTabs(numTabs)}Shield Dmg Type: {(ammo.NoGridOrArmorScaling ? Energy : gridScales.DamageType.Shield)}"
            );

            if (shouldShowScales)
            {
                var falloff = gridScales.FallOff;

                if (
                    falloff.Distance < ammo.Trajectory.MaxTrajectory
                    && falloff.MinMultipler > 0.0001f
                    && falloff.MinMultipler < 1f
                )
                    stringBuilder.Append(
                        $"\n{GetTabs(numTabs)}Falloff: Linearly past {falloff.Distance}m to {falloff.MinMultipler}x at {ammo.Trajectory.MaxTrajectory}m"
                    );

                if (gridScales.MaxIntegrity > 0)
                    stringBuilder.Append(
                        $"\n{GetTabs(numTabs)}Max Damagable Integrity: {gridScales.MaxIntegrity}"
                    );

                var grids = gridScales.Grids;
                var armor = gridScales.Armor;
                var shields = gridScales.Shields;
                if (!ammo.NoGridOrArmorScaling)
                {
                    stringBuilder.Append(
                        $"\n{GetTabs(numTabs)}Grid Dmg Multipliers: {(grids.Large == -1 ? large ? 1 : 4 : grids.Large)}x vs LG, {(grids.Small == -1 ? large ? 0.25f : 1 : grids.Small)}x vs SG"
                    );
                    stringBuilder.Append(
                        $"\n{GetTabs(numTabs)}Armor Multipliers: {DefaultValue(armor.Armor, -1, 1)}x vs armor, {DefaultValue(armor.Light, -1, 1)}x vs LA, "
                            + $"{DefaultValue(armor.Heavy, -1, 1)}x vs HA, {DefaultValue(armor.NonArmor, -1, 1)}x vs functional, {DefaultValue(gridScales.Characters, -1, 1)}x vs characters"
                    );
                    stringBuilder.Append(
                        $"\n{GetTabs(numTabs)}Shield Multipliers: {(shields.Type == Heal ? -1 : 1) * DefaultValue(shields.Modifier, -1, 1)}x vs shields"
                    );
                }
                if (shields.BypassModifier > -1)
                {
                    stringBuilder.Append(
                        $", bypasses {(1 - shields.BypassModifier) * 100f:#0.##}%"
                    );
                }
                if (shields.HeatModifier != 1)
                    stringBuilder.Append($", {shields.HeatModifier}x dmg converted to heat");

                if (gridScales.SelfDamage)
                    stringBuilder.Append($"\n{GetTabs(numTabs)}DAMAGES OWNG GRID!");

                if (gridScales.DamageVoxels)
                    stringBuilder.Append(
                        $"\n{GetTabs(numTabs)}AOE Damages voxels @ {gridScales.VoxelHitModifier}x radius"
                    );

                if (gridScales.HealthHitModifier > 0)
                    stringBuilder.Append(
                        $"\n{GetTabs(numTabs)}{gridScales.HealthHitModifier} dmg to projectiles."
                    );
            }

            stringBuilder.Append($"\n{GetTabs(numTabs)}Range: {ammo.Trajectory.MaxTrajectory}m");
            stringBuilder.Append(
                $"\n{GetTabs(numTabs)}Max Lifetime: {ammo.Trajectory.MaxLifeTime / 60f}s"
            );
            stringBuilder.Append(
                $"\n{GetTabs(numTabs)}Velocity: {(ammo.Beams.Enable ? "C" : ammo.Trajectory.DesiredSpeed.ToString() + "m/s")}"
            );
            stringBuilder.Append(
                $"\n{GetTabs(numTabs)}Guided: {(ammo.Trajectory.Guidance == Smart ? ammo.Trajectory.Smarts.Aggressiveness.ToString() : "None")}"
            );
            if (
                ammo.Fragment.AmmoRound != null
                && ammo.Fragment.AmmoRound != ""
                && !visitedAmmos.Contains(ammo.Fragment.AmmoRound)
                && !ammo.Fragment.AmmoRound.StartsWith("mss_jddt_")
            )
            {
                stringBuilder.Append(
                    $"\n\tFires {ammo.Fragment.Fragments}x Shrapnel @ {ammo.Fragment.Radial}-{ammo.Fragment.Degrees + ammo.Fragment.Radial}° ({ammo.Fragment.AmmoRound}):"
                );
                foreach (var a in ammos)
                {
                    if (a.AmmoRound == ammo.Fragment.AmmoRound)
                    {
                        AppendAmmoStats(stringBuilder, a, ammos, visitedAmmos, numTabs, large);
                    }
                }
            }
            else if (
                ammo.Fragment.AmmoRound != null
                && ammo.Fragment.AmmoRound != ""
                && ammo.Fragment.AmmoRound.Contains("jddt_")
            ) // cope hardoded exception
            {
                float strength;
                if (float.TryParse(ammo.Fragment.AmmoRound.Substring(9), out strength))
                {
                    stringBuilder.Append(
                        $"\n{GetTabs(numTabs)}Jump Knock: {strength * 100 / (JD_INTEGRITY * 2):##.##}%/hit ({strength})"
                    );
                }
            }
            stringBuilder.Append($"\n{GetTabs(numTabs)}}}");
        }

        internal float DefaultValue(float val, float valToDefault, float def)
        {
            if (val == valToDefault)
                return def;
            return val;
        }

        internal AmmoDef ComputeJDDTFragment(
            float strength,
            Vector4 TrailValue = default(Vector4),
            float TrailWidth = 0f,
            int TrailLength = 0
        )
        {
            return new AmmoDef // Your ID, for slotting into the Weapon CS
            {
                AmmoMagazine = "Energy", // SubtypeId of physical ammo magazine. Use "Energy" for weapons without physical ammo.
                AmmoRound = $"mss_jddt_{strength}", // Unique name used in server overrides and in the terminal (default).  Should be different for each ammoDef used by the same weapon.  Referred to for Shrapnel.
                TerminalName = "", // Optional terminal name for this ammo type, used when picking ammo/cycling consumables.  Safe to have duplicates across different ammo defs.
                HybridRound = false, // Use both a physical ammo magazine and energy per shot.
                EnergyCost = strength == 0 ? 0f : 1f, // Modified in MasterConfig here, actually just the power requirement needed to fire at 1.0 ROF scalar
                BaseDamage = strength == 0 ? 0f : 1f, // Direct damage; one steel plate is worth 100.
                BaseDamageCutoff = 0, // Maximum amount of pen damage to apply per block hit.  Deducts from BaseDamage and uses DamageScales modifiers
                // Optional penetration mechanic to apply damage to blocks beyond the first hit, without requiring the block to be destroyed.
                // Overwrites normal damage behavior of requiring a block to be destroyed before damage can continue.  0 disables.
                // To limit max # of blocks hit, set MaxObjectsHit to desired # and ensure CountBlocks = true in ObjectsHit, otherwise it will continue until BaseDamage depletes
                Mass = 0f, // Disabled here. Too lazy to set this to 0 everywhere so MasterConfig does it
                Health = 0, // How much damage the projectile can take from other projectiles (base of 1 per hit) before dying; 0 disables this and makes the projectile untargetable.
                BackKickForce = 0f, // Recoil. This is applied to the Parent Grid.
                DecayPerShot = 0f, // Damage to the firing weapon itself.
                //float.MaxValue will drop the weapon to the first build state and destroy all components used for construction
                //If greater than cube integrity it will remove the cube upon firing, without causing deformation (makes it look like the whole "block" flew away)
                HardPointUsable = false, // Whether this is a primary ammo type fired directly by the turret. Set to false if this is a shrapnel ammoType and you don't want the turret to be able to select it directly.
                EnergyMagazineSize = 1, // For energy weapons, how many shots to fire before reloading.
                IgnoreWater = false, // Whether the projectile should be able to penetrate water when using WaterMod.
                IgnoreVoxels = false, // Whether the projectile should be able to penetrate voxels.
                HeatModifier = -1f, // Allows this ammo to modify the amount of heat the weapon produces per shot.
                NpcSafe = false, // This is you tell npc moders that your ammo was designed with them in mind, if they tell you otherwise set this to false.
                NoGridOrArmorScaling = false, // If you enable this you can remove the damagescale section entirely.
                Sync = new SynchronizeDef
                {
                    Full = false, // Do not use - still in progress
                    PointDefense = false, // Server will inform clients of what projectiles have died by PD defense and will trigger destruction.
                    OnHitDeath = false, // Server will inform clients when projectiles die due to them hitting something and will trigger destruction.
                },
                Shape = new ShapeDef // Defines the collision shape of the projectile, defaults to LineShape and uses the visual Line Length if set to 0.
                {
                    Shape = LineShape, // LineShape or SphereShape. Do not use SphereShape for fast moving projectiles if you care about precision.
                    Diameter = 1, // For SphereShape this is diameter.
                    // For LineShape it is total length (double this value when setting up MaximumDiameter for weapon targeting).
                    // Defaults to 1 if left zero or deleted.
                },
                ObjectsHit = new ObjectsHitDef
                {
                    MaxObjectsHit = 0, // Limits the number of grids or projectiles that damage can be applied to, useful to limit overpenetration; 0 = unlimited.
                    CountBlocks = false, // Counts individual blocks, not just entities hit.  Note that every block touched by primary damage hits will count toward MaxObjectsHit
                    SkipBlocksForAOE = false, //If CountBlocks = true this will determine if AOE hits are counted against MaxObjectsHit.  Set true to skip counting for AOE
                },
                Fragment = new FragmentDef // Formerly known as Shrapnel. Spawns specified ammo fragments on projectile death (via hit or detonation).
                {
                    AmmoRound = "", // AmmoRound field of the ammo to spawn.
                    Fragments = 1, // Number of projectiles to spawn.
                    Degrees = 0, // Cone in which to randomize direction of spawned projectiles.
                    Reverse = false, // Spawn projectiles backward instead of forward.
                    DropVelocity = false, // fragments will not inherit velocity from parent.
                    Offset = 0f, // Offsets the fragment spawn by this amount, in meters (positive forward, negative for backwards), value is read from parent ammo type.
                    Radial = 0f, // Determines starting angle for Degrees of spread above.  IE, 0 degrees and 90 radial goes perpendicular to travel path
                    MaxChildren = 0, // number of maximum branches for fragments from the roots point of view, 0 is unlimited
                    IgnoreArming = true, // If true, ignore ArmOnHit or MinArmingTime in EndOfLife definitions
                    ArmWhenHit = false, // Setting this to true will arm the projectile when its shot by other projectiles.
                    AdvOffset = Vector(x: 0, y: 0, z: 0), // advanced offsets the fragment by xyz coordinates relative to parent, value is read from fragment ammo type.
                    TimedSpawns = new TimedSpawnDef // disables FragOnEnd in favor of info specified below, unless ArmWhenHit or Eol ArmOnlyOnHit is set to true then both kinds of frags are active
                    {
                        Enable = false, // Enables TimedSpawns mechanism
                        Interval = 0, // Time between spawning fragments, in ticks, 0 means every tick, 1 means every other
                        StartTime = 0, // Time delay to start spawning fragments, in ticks, of total projectile life
                        MaxSpawns = 1, // Max number of fragment children to spawn
                        Proximity = 1000, // Starting distance from target bounding sphere to start spawning fragments, 0 disables this feature.  No spawning outside this distance
                        ParentDies = true, // Parent dies once after it spawns its last child.
                        PointAtTarget = true, // Start fragment direction pointing at Target
                        PointType = Predict, // Point accuracy, Direct (straight forward), Lead (always fire), Predict (only fire if it can hit)
                        DirectAimCone = 0f, //Aim cone used for Direct fire, in degrees
                        GroupSize = 5, // Number of spawns in each group
                        GroupDelay = 120, // Delay between each group.
                    },
                },
                Pattern = new PatternDef
                {
                    Patterns = new[]
                    { // If enabled, set of multiple ammos to fire in order instead of the main ammo.
                        "",
                    },
                    Mode = Fragment, // Select when to activate this pattern, options: Never, Weapon, Fragment, Both
                    TriggerChance = 1f, // This is %
                    Random = false, // This randomizes the number spawned at once, NOT the list order.
                    RandomMin = 1,
                    RandomMax = 1,
                    SkipParent = false, // Skip the Ammo itself, in the list
                    PatternSteps = 1, // Number of Ammos activated per round, will progress in order and loop. Ignored if Random = true.
                },
                DamageScales = new DamageScaleDef
                {
                    MaxIntegrity = 0f, // Blocks with integrity higher than this value will be immune to damage from this projectile; 0 = disabled.
                    DamageVoxels = false, // Whether to damage voxels.
                    SelfDamage = false, // Whether to damage the weapon's own grid.
                    HealthHitModifier = 0.5, // How much Health to subtract from another projectile on hit; defaults to 1 if zero or less.
                    VoxelHitModifier = 1, // Voxel damage multiplier; defaults to 1 if zero or less.
                    Characters = -1f, // Character damage multiplier; defaults to 1 if zero or less.
                    // For the following modifier values: -1 = disabled (higher performance), 0 = no damage, 0.01f = 1% damage, 2 = 200% damage.
                    FallOff = new FallOffDef
                    {
                        Distance = 0f, // Distance at which damage begins falling off.
                        MinMultipler = 1f, // Value from 0.0001f to 1f where 0.1f would be a min damage of 10% of base damage.
                    },
                    Grids = new GridSizeDef //If both of these values are -1, a 4x buff to SG weapons firing at LG and 0.25x debuff to LG weapons firing at SG will apply
                    {
                        Large = 1f, // Multiplier for damage against large grids.
                        Small = 1f, // Multiplier for damage against small grids.
                    },
                    Armor = new ArmorDef
                    {
                        Armor = 1f, // Multiplier for damage against all armor. This is multiplied with the specific armor type multiplier (light, heavy).
                        Light = 1f, // Multiplier for damage against light armor.
                        Heavy = 1f, // Multiplier for damage against heavy armor.
                        NonArmor = 1f, // Multiplier for damage against every else.
                    },
                    Shields = new ShieldDef
                    {
                        Modifier = 1f, // Multiplier for damage against shields.
                        Type = Default, // Damage vs healing against shields; Default, Heal
                        BypassModifier = -1f, // 0-1 will bypass shields and apply that damage amount as a scaled %.  -1 is disabled.  -2 to -1 will alter the chance of penning a damaged shield, with -2 being a 100% reduction
                        HeatModifier = 1, // scales how much of the damage is converted to heat, negative values subtract heat.
                    },
                    DamageType = new DamageTypes // Damage type of each element of the projectile's damage; Kinetic, Energy
                    {
                        Base = Energy, // Base Damage uses this
                        AreaEffect = Energy,
                        Detonation = Energy,
                        Shield = Energy, // Damage against shields is currently all of one type per projectile. Shield Bypass Weapons, always Deal Energy regardless of this line
                    },
                    Deform = new DeformDef
                    {
                        DeformType = NoDeform, // HitBlock- applies deformation to the block that was hit
                        // AllDamagedBlocks- applies deformation to all blocks damaged (for AOE)
                        // NoDeform- applies no deformation
                        DeformDelay = 0, // Time in ticks to wait before applying another deformation event (prevents excess calls for deformation every tick or from multiple sources)
                    },
                    Custom = new CustomScalesDef
                    {
                        SkipOthers = NoSkip, // Controls how projectile interacts with other blocks in relation to those defined here, NoSkip, Exclusive, Inclusive.
                        Types = new[] // List of blocks to apply custom damage multipliers to.
                        {
                            new CustomBlocksDef { SubTypeId = "Test1", Modifier = -1f },
                            new CustomBlocksDef { SubTypeId = "Test2", Modifier = -1f },
                        },
                    },
                },
                AreaOfDamage = new AreaOfDamageDef // Note AOE is only applied to the Player/Grid it hit (and nearby projectiles) not nearby grids/players.
                {
                    ByBlockHit = new ByBlockHitDef
                    {
                        Enable = false,
                        Radius = 5f, // Meters
                        Damage = 5f,
                        Depth = 1f, // Max depth of AOE effect, in meters. 0=disabled, and AOE effect will reach to a depth of the radius value
                        MaxAbsorb = 0f, // Soft cutoff for damage (total, against shields or grids), except for pooled falloff.  If pooled falloff, limits max damage per block.
                        Falloff = Pooled, //.NoFalloff applies the same damage to all blocks in radius
                        //.Linear drops evenly by distance from center out to max radius
                        //.Curve drops off damage sharply as it approaches the max radius
                        //.InvCurve drops off sharply from the middle and tapers to max radius
                        //.Squeeze does little damage to the middle, but rapidly increases damage toward max radius
                        //.Pooled damage behaves in a pooled manner that once exhausted damage ceases.
                        //.Exponential drops off exponentially.  Does not scale to max radius
                        Shape = Diamond, // Round or Diamond shape.  Diamond is more performance friendly.
                    },
                    EndOfLife = new EndOfLifeDef
                    {
                        Enable = strength != 0,
                        Radius = 7f, // Radius of AOE effect, in meters.
                        Damage = 3000f,
                        Depth = 0f, // Max depth of AOE effect, in meters. 0=disabled, and AOE effect will reach to a depth of the radius value
                        MaxAbsorb = 0, // Soft cutoff for damage (total, against shields or grids), except for pooled falloff.  If pooled falloff, limits max damage per block.
                        Falloff = Falloff.Linear, //.NoFalloff applies the same damage to all blocks in radius
                        //.Linear drops evenly by distance from center out to max radius
                        //.Curve drops off damage sharply as it approaches the max radius
                        //.InvCurve drops off sharply from the middle and tapers to max radius
                        //.Squeeze does little damage to the middle, but rapidly increases damage toward max radius
                        //.Pooled damage behaves in a pooled manner that once exhausted damage ceases.
                        //.Exponential drops off exponentially.  Does not scale to max radius
                        ArmOnlyOnHit = true, // Detonation only is available, After it hits something, when this is true. IE, if shot down, it won't explode.
                        MinArmingTime = 3, // In ticks, before the Ammo is allowed to explode, detonate or similar; This affects shrapnel spawning.
                        NoVisuals = false,
                        NoSound = false,
                        ParticleScale = 1,
                        CustomParticle = "Explosion_Missile", // Particle SubtypeID, from your Particle SBC
                        // If you need to set a custom offset, specify it in the "Hit" particle
                        CustomSound = "soundName", // SubtypeID from your Audio SBC, not a filename
                        Shape = Diamond, // Round or Diamond shape.  Diamond is more performance friendly.
                    },
                },
                Ewar = new EwarDef
                {
                    Enable = strength > 0, // Enables EWAR effects AND DISABLES BASE DAMAGE AND AOE DAMAGE!!
                    Type = JumpNull, // EnergySink, Emp, Offense, Nav, Dot, AntiSmart, JumpNull, Anchor, Tractor, Pull, Push,
                    Mode = Effect, // Effect , Field
                    Strength = strength,
                    Radius = 250f, // Meters
                    Duration = 5 * 60, // In Ticks
                    StackDuration = false, // Combined Durations
                    Depletable = false,
                    MaxStacks = 1, // Max Debuffs at once
                    NoHitParticle = true,
                    /*
                    EnergySink : Targets & Shutdowns Power Supplies, such as Batteries & Reactor
                    Emp : Targets & Shutdown any Block capable of being powered
                    Offense : Targets & Shutdowns Weaponry
                    Nav : Targets & Shutdown Gyros or Locks them down
                    Dot : Deals Damage to Blocks in radius
                    AntiSmart : Effects & Scrambles the Targeting List of Affected Missiles
                    AntiSmartv2 : Effects & Scrambles the Targeting List of Affected Missiles.  See ewar section of wiki for specific differences
                    JumpNull : Shutdown & Stops any Active Jumps, or JumpDrive Units in radius
                    Tractor : Affects target with Physics
                    Pull : Affects target with Physics
                    Push : Affects target with Physics
                    Anchor : Targets & Shutdowns Thrusters

                    */
                    Force = new PushPullDef
                    {
                        ForceFrom = ProjectileLastPosition, // ProjectileLastPosition, ProjectileOrigin, HitPosition, TargetCenter, TargetCenterOfMass
                        ForceTo = HitPosition, // ProjectileLastPosition, ProjectileOrigin, HitPosition, TargetCenter, TargetCenterOfMass
                        Position = TargetCenterOfMass, // ProjectileLastPosition, ProjectileOrigin, HitPosition, TargetCenter, TargetCenterOfMass
                        DisableRelativeMass = false,
                        TractorRange = 0,
                        ShooterFeelsForce = false,
                    },
                    Field = new FieldDef
                    {
                        Interval = 0, // Time between each pulse, in game ticks (60 == 1 second), starts at 0 (59 == tick 60).
                        PulseChance = 100, // Chance from 0 - 100 that an entity in the field will be hit by any given pulse.
                        GrowTime = 0, // How many ticks it should take the field to grow to full size.
                        HideModel = false, // Hide the default bubble, or other model if specified.
                        ShowParticle = true, // Show Block damage effect.
                        TriggerRange = 250f, //range at which fields are triggered
                        Particle = new ParticleDef // Particle effect to generate at the field's position.
                        {
                            Name = "", // SubtypeId of field particle effect.
                            Extras = new ParticleOptionDef
                            {
                                Scale = 1, // Scale of effect.
                            },
                        },
                    },
                },
                Beams = new BeamDef
                {
                    Enable = true, // Enable beam behaviour. Please have 3600 RPM, when this Setting is enabled. Please do not fire Beams into Voxels.
                    VirtualBeams = strength == 0, // Only one damaging beam, but with the effectiveness of the visual beams combined (better performance).  If you are patterning a damage beam, ensure this is off for the non-AV beam
                    ConvergeBeams = false, // When using virtual beams, converge the visual beams to the location of the real beam.
                    RotateRealBeam = false, // The real beam is rotated between all visual beams, instead of centered between them.
                    OneParticle = false, // Only spawn one particle hit per beam weapon.
                    FakeVoxelHitTicks = 0, // If this beam hits/misses a voxel it assumes it will continue to do so for this many ticks at the same hit length and not extend further within this window.  This can save up to n times worth of cpu.
                },
                Trajectory = new TrajectoryDef
                {
                    Guidance = None, // None, Remote, TravelTo, Smart, DetectTravelTo, DetectSmart, DetectFixed
                    TargetLossDegree = 80f, // Degrees, Is pointed forward
                    TargetLossTime = 0, // 0 is disabled, Measured in game ticks (6 = 100ms, 60 = 1 seconds, etc..).
                    MaxLifeTime = 900, // 0 is disabled, Measured in game ticks (6 = 100ms, 60 = 1 seconds, etc..). time begins at 0 and time must EXCEED this value to trigger "time > maxValue". Please have a value for this, It stops Bad things.
                    AccelPerSec = 0f, // Acceleration in Meters Per Second. Projectile starts on tick 0 at its parents (weapon/other projectiles) travel velocity.
                    DesiredSpeed = 2000, // voxel phasing if you go above 5100
                    MaxTrajectory = 75, // Max Distance the projectile or beam can Travel.
                    DeaccelTime = 0, // EWAR & Mines only- time to spend slowing down to stop at end of trajectory.  0 is instant stop
                    GravityMultiplier = 1f, // Gravity multiplier, influences the trajectory of the projectile, value greater than 0 to enable. Natural Gravity Only.
                    SpeedVariance = Random(start: 0, end: 0), // subtracts value from DesiredSpeed. Be warned, you can make your projectile go backwards.
                    RangeVariance = Random(start: 0, end: 0), // subtracts value from MaxTrajectory
                    MaxTrajectoryTime = 0, // How long the weapon must fire before it reaches MaxTrajectory.
                    TotalAcceleration = 0, // 0 means no limit, something to do due with a thing called delta and something called v.
                    DragPerSecond = 0f, // Amount of drag (m/s) deducted from the projectile's speed, multiplied by age.  Will not go below zero/negative.  Note that turrets will not be able to reliably account for this with non-smart ammo.
                    DragMinSpeed = 0f, // If DragPerSecond is used, the projectiles speed will never go below this value in m/s
                    Smarts = new SmartsDef
                    {
                        SteeringLimit = 0, // 0 means no limit, value is in degrees, good starting is 150.  This enable advanced smart "control", cost of 3 on a scale of 1-5, 0 being basic smart.
                        Inaccuracy = 0f, // 0 is perfect, hit accuracy will be a random num of meters between 0 and this value.
                        Aggressiveness = 1f, // controls how responsive tracking is, recommended value 3-5.
                        MaxLateralThrust = 0.75, // controls how sharp the projectile may turn, this is the cheaper but less realistic version of SteeringLimit, cost of 2 on a scale of 1-5, 0 being basic smart.
                        NavAcceleration = 0, // helps influence how the projectile steers, 0 defaults to 1/2 Aggressiveness value or 0 if its 0, a value less than 0 disables this feature.
                        TrackingDelay = 0, // Measured in Shape diameter units traveled.
                        AccelClearance = false, // Setting this to true will prevent smart acceleration until it is clear of the grid and tracking delay has been met (free fall).
                        MaxChaseTime = 0, // Measured in game ticks (6 = 100ms, 60 = 1 seconds, etc..).
                        OverideTarget = true, // when set to true ammo picks its own target, does not use hardpoint's.
                        CheckFutureIntersection = false, // Utilize obstacle avoidance for drones/smarts
                        FutureIntersectionRange = 0, // Range in front of the projectile at which it will detect obstacle.  If set to zero it defaults to DesiredSpeed + Shape Diameter
                        MaxTargets = 0, // Number of targets allowed before ending, 0 = unlimited, 1 = stay with first target when fired
                        NoTargetExpire = false, // Expire without ever having a target at TargetLossTime
                        Roam = false, // Roam current area after target loss
                        KeepAliveAfterTargetLoss = false, // Whether to stop early death of projectile on target loss
                        OffsetRatio = 0.05f, // The ratio to offset the random direction (0 to 1)
                        OffsetTime = 60, // how often to offset degree, measured in game ticks (6 = 100ms, 60 = 1 seconds, etc..)
                        OffsetMinRange = 0, // The range from target at which offsets are no longer active
                        FocusOnly = false, // Only target the HUD or AI focused target (this includes changes to the hud-selected target.  Set MaxTargets = 1 to keep it from switching (aka fire and forget)
                        FocusEviction = false, // If FocusOnly and this to true will force smarts to lose target when there is no focus target (IE you must keep the target selected or the projectile will lose the target)
                        ScanRange = 0, // 0 disables projectile screening, the max range that this projectile will be seen at by defending grids (adds this projectile to defenders lookup database).
                        NoSteering = false, // this disables target follow and instead travel straight ahead (but will respect offsets).
                        MinTurnSpeed = 0, // set this to a reasonable value to avoid projectiles from spinning in place or being too aggressive turing at slow speeds
                        NoTargetApproach = false, // If true approaches can begin prior to the projectile ever having had a target.
                        AltNavigation = false, // If true this will swap the default navigation algorithm from ProNav to ZeroEffort Miss.  Zero effort is more direct/precise but less cinematic
                        IgnoreAntiSmarts = false, // If true, this projectiles targeting cannot be interfered with by anti smart EWAR effects
                    },
                },
                AmmoGraphics = new GraphicDef
                {
                    ModelName = "", // Model Path goes here.  "\\Models\\Ammo\\Starcore_Arrow_Missile_Large"
                    VisualProbability = 1f, // 0-1 % chance of AV appearing (controls all audio AND visual)
                    ShieldHitDraw = false,

                    Lines = new LineDef
                    {
                        ColorVariance = Random(start: 1f, end: 1f), // multiply the color by random values within range.
                        WidthVariance = Random(start: 0f, end: 0f), // adds random value to default width (negatives shrinks width)
                        DropParentVelocity = true, // If set to true will not take on the parents (grid/player) initial velocity when rendering.
                        Tracer = new TracerBaseDef
                        {
                            Enable = TrailValue != Vector4.Zero,
                            Length = TrailWidth, //
                            Width = TrailWidth, //
                            Color = TrailValue, // RBG 255 is Neon Glowing, 100 is Quite Bright.
                            FactionColor = DontUse, // DontUse, Foreground, Background.
                            VisualFadeStart = 0, // Number of ticks the weapon has been firing before projectiles begin to fade their color
                            VisualFadeEnd = 0, // How many ticks after fade began before it will be invisible.
                            AlwaysDraw = false, // Prevents this tracer from being culled.  Only use if you have a reason too (very long tracers/trails).
                            Textures = new[]
                            { // WeaponLaser, ProjectileTrailLine, WarpBubble, etc..
                                "WeaponLaser", // Please always have this Line set, if this Section is enabled.
                            },
                            TextureMode = Normal, // Normal, Cycle, Chaos, Wave
                            Segmentation = new SegmentDef
                            {
                                Enable = false, // If true Tracer TextureMode is ignored
                                Textures = new[]
                                {
                                    "", // Please always have this Line set, if this Section is enabled.
                                },
                                SegmentLength = 0f, // Uses the values below.
                                SegmentGap = 0f, // Uses Tracer textures and values
                                Speed = 1f, // meters per second
                                Color = Color(red: 1, green: 2, blue: 2.5f, alpha: 1),
                                FactionColor = DontUse, // DontUse, Foreground, Background.
                                WidthMultiplier = 1f,
                                Reverse = false,
                                UseLineVariance = true,
                                WidthVariance = Random(start: 0f, end: 0f),
                                ColorVariance = Random(start: 0f, end: 0f),
                            },
                        },
                        Trail = new TrailDef
                        {
                            Enable = TrailValue != Vector4.Zero,
                            AlwaysDraw = false, // Prevents this tracer from being culled.  Only use if you have a reason too (very long tracers/trails).
                            Textures = new[]
                            {
                                "WeaponLaser", // Please always have this Line set, if this Section is enabled.
                            },
                            TextureMode = Normal,
                            DecayTime = TrailLength, // In Ticks. 1 = 1 Additional Tracer generated per motion, 33 is 33 lines drawn per projectile. Keep this number low.
                            Color = TrailValue,
                            FactionColor = DontUse, // DontUse, Foreground, Background.
                            Back = false,
                            CustomWidth = TrailWidth,
                            UseWidthVariance = false,
                            UseColorFade = false,
                        },
                        OffsetEffect = new OffsetEffectDef
                        {
                            MaxOffset = 0, // 0 offset value disables this effect
                            MinLength = 0.2f,
                            MaxLength = 3,
                        },
                    },
                },
                AmmoAudio = new AmmoAudioDef
                {
                    TravelSound = "", // SubtypeID for your Sound File. Travel is sound generated around your projectile in flight
                    HitSound = "", // Default hit sound, used unless optional hit sounds below are populated.  MUST HAVE A VALUE FOR ANY HIT SOUND TO WORK!
                    ShotSound = "", // Sound when fired
                    ShieldHitSound = "", // Shield hit
                    PlayerHitSound = "", // Player character hit
                    VoxelHitSound = "", // Voxel hit
                    FloatingHitSound = "", // Floating object hit (IE components floating in space)
                    WaterHitSound = "", // Water hit sound, if Water Mod is present
                    HitPlayChance = 0f, //0-1% chance for any hit sound to play
                    HitPlayShield = false, //Including chance above, determines if the ShieldHitSound (or if ShieldHitSound is blank, default HitSound) will play for shield hits
                },
            };
        }

        internal AnimationDef GenerateMissileAnimation(string subtype, uint reloadTime)
        {
            return new AnimationDef
            {
                AnimationSets = new[]
                {
                    new PartAnimationSetDef()
                    {
                        SubpartId = Names(subtype),
                        BarrelId = "Any", //only used for firing, use "Any" for all muzzles
                        StartupFireDelay = 0,
                        AnimationDelays = Delays(
                            FiringDelay: 0,
                            ReloadingDelay: 0,
                            OverheatedDelay: 0,
                            TrackingDelay: 0,
                            LockedDelay: 0,
                            OnDelay: 0,
                            OffDelay: 0,
                            BurstReloadDelay: 0,
                            OutOfAmmoDelay: 0,
                            PreFireDelay: 0,
                            StopFiringDelay: 0,
                            StopTrackingDelay: 0,
                            InitDelay: 0
                        ), //Delay before animation starts
                        Reverse = Events(),
                        Loop = Events(),
                        EventMoveSets = new Dictionary<PartAnimationSetDef.EventTriggers, RelMove[]>
                        {
                            // Reloading, Firing, Tracking, Overheated, TurnOn, TurnOff, BurstReload, NoMagsToLoad, PreFire, EmptyOnGameLoad, StopFiring, StopTracking, LockDelay, Init
                            [NoMagsToLoad] = new[]
                            {
                                new RelMove
                                {
                                    CenterEmpty = "",
                                    TicksToMove = 1, //number of ticks to complete motion, 60 = 1 second
                                    MovementType = Hide, //Linear,ExpoDecay,ExpoGrowth,Delay,Show, //instant or fade Hide, //instant or fade
                                    LinearPoints = new[]
                                    {
                                        Transformation(0, 0, 0), //linear movement
                                    },
                                    Rotation = Transformation(0, 0, 0), //degrees
                                    RotAroundCenter = Transformation(0, 0, 0), //degrees
                                },
                            },
                            [Reloading] = new[]
                            {
                                new RelMove
                                {
                                    CenterEmpty = "",
                                    TicksToMove = 1, //number of ticks to complete motion, 60 = 1 second
                                    MovementType = Hide, //Linear,ExpoDecay,ExpoGrowth,Delay,Show, //instant or fade Hide, //instant or fade
                                    LinearPoints = new[]
                                    {
                                        Transformation(0, 0, 0), //linear movement
                                    },
                                    Rotation = Transformation(0, 0, 0), //degrees
                                    RotAroundCenter = Transformation(0, 0, 0), //degrees
                                },
                                new RelMove
                                {
                                    CenterEmpty = "",
                                    TicksToMove = reloadTime, //number of ticks to complete motion, 60 = 1 second
                                    MovementType = Delay, //Linear,ExpoDecay,ExpoGrowth,Delay,Show, //instant or fade Hide, //instant or fade
                                    LinearPoints = new[]
                                    {
                                        Transformation(0, 0, 0), //linear movement
                                    },
                                    Rotation = Transformation(0, 0, 0), //degrees
                                    RotAroundCenter = Transformation(0, 0, 0), //degrees
                                },
                                new RelMove
                                {
                                    CenterEmpty = "",
                                    TicksToMove = 1, //number of ticks to complete motion, 60 = 1 second
                                    MovementType = Show,
                                    LinearPoints = new XYZ[0],
                                    Rotation = Transformation(0, 0, 0), //degrees
                                    RotAroundCenter = Transformation(0, 0, 0), //degrees
                                },
                            },
                        },
                    },
                },
            };
        }
    }

    public static class AmmoHelper
    {
        public static AmmoDef SetJDDTFragment(this AmmoDef def, float strength)
        {
            def.Fragment = new FragmentDef // Formerly known as Shrapnel. Spawns specified ammo fragments on projectile death (via hit or detonation).
            {
                AmmoRound = $"mss_jddt_{strength}", // AmmoRound field of the ammo to spawn.
                Fragments = 1, // Number of projectiles to spawn.
                Degrees = 0, // Cone in which to randomize direction of spawned projectiles.
                Reverse = false, // Spawn projectiles backward instead of forward.
                DropVelocity = true, // fragments will not inherit velocity from parent.
                Offset = 0f, // Offsets the fragment spawn by this amount, in meters (positive forward, negative for backwards), value is read from parent ammo type.
                Radial = 0f, // Determines starting angle for Degrees of spread above.  IE, 0 degrees and 90 radial goes perpendicular to travel path
                MaxChildren = 0, // number of maximum branches for fragments from the roots point of view, 0 is unlimited
                IgnoreArming = true, // If true, ignore ArmOnHit or MinArmingTime in EndOfLife definitions
                ArmWhenHit = false, // Setting this to true will arm the projectile when its shot by other projectiles.
                AdvOffset = def.Fragment.AdvOffset, // advanced offsets the fragment by xyz coordinates relative to parent, value is read from fragment ammo type.
                AdvRotationOffset = def.Fragment.AdvRotationOffset,
                TimedSpawns = new TimedSpawnDef // disables FragOnEnd in favor of info specified below, unless ArmWhenHit or Eol ArmOnlyOnHit is set to true then both kinds of frags are active
                {
                    Enable = false, // Enables TimedSpawns mechanism
                    Interval = 0, // Time between spawning fragments, in ticks, 0 means every tick, 1 means every other
                    StartTime = 0, // Time delay to start spawning fragments, in ticks, of total projectile life
                    MaxSpawns = 1, // Max number of fragment children to spawn
                    Proximity = 1000, // Starting distance from target bounding sphere to start spawning fragments, 0 disables this feature.  No spawning outside this distance
                    ParentDies = true, // Parent dies once after it spawns its last child.
                    PointAtTarget = true, // Start fragment direction pointing at Target
                    PointType = Predict, // Point accuracy, Direct (straight forward), Lead (always fire), Predict (only fire if it can hit)
                    DirectAimCone = 0f, //Aim cone used for Direct fire, in degrees
                    GroupSize = 5, // Number of spawns in each group
                    GroupDelay = 120, // Delay between each group.
                },
            };
            return def;
        }

        public static AmmoDef MakeSolid(this AmmoDef def)
        {
            def.AmmoRound = $"{def.AmmoRound}_solid";
            def.TerminalName = def.TerminalName.Replace("Fragmentation", "Solid");
            def.BaseDamage *= def.Fragment.Fragments + 1;
            def.HeatModifier = 1f;

            def.Fragment.AmmoRound = "";

            def.ObjectsHit.CountBlocks = true;
            def.ObjectsHit.SkipBlocksForAOE = true;

            return def;
        }

        public static AmmoDef DamageMultiplier(this AmmoDef def, float mult)
        {
            def.BaseDamage *= mult;
            return def;
        }

        public static AmmoDef GiveOffset(this AmmoDef def, string newName, string newTerminalName)
        {
            def.AmmoRound = newName;
            def.TerminalName = newTerminalName;

            def.Trajectory.Guidance = Smart;
            def.Trajectory.Smarts.NoSteering = true;
            return def;
        }
    }
}
