using static Scripts.Structure.WeaponDefinition;
using static Scripts.Structure.WeaponDefinition.AmmoDef;
using static Scripts.Structure.WeaponDefinition.AmmoDef.EjectionDef;
using static Scripts.Structure.WeaponDefinition.AmmoDef.EjectionDef.SpawnType;
using static Scripts.Structure.WeaponDefinition.AmmoDef.ShapeDef.Shapes;
using static Scripts.Structure.WeaponDefinition.AmmoDef.DamageScaleDef.CustomScalesDef.SkipMode;
using static Scripts.Structure.WeaponDefinition.AmmoDef.GraphicDef;
using static Scripts.Structure.WeaponDefinition.AmmoDef.FragmentDef;
using static Scripts.Structure.WeaponDefinition.AmmoDef.PatternDef.PatternModes;
using static Scripts.Structure.WeaponDefinition.AmmoDef.FragmentDef.TimedSpawnDef.PointTypes;
using static Scripts.Structure.WeaponDefinition.AmmoDef.TrajectoryDef;
using static Scripts.Structure.WeaponDefinition.AmmoDef.TrajectoryDef.ApproachDef.Conditions;
using static Scripts.Structure.WeaponDefinition.AmmoDef.TrajectoryDef.ApproachDef.UpRelativeTo;
using static Scripts.Structure.WeaponDefinition.AmmoDef.TrajectoryDef.ApproachDef.FwdRelativeTo;
using static Scripts.Structure.WeaponDefinition.AmmoDef.TrajectoryDef.ApproachDef.ReInitCondition;
using static Scripts.Structure.WeaponDefinition.AmmoDef.TrajectoryDef.ApproachDef.RelativeTo;
using static Scripts.Structure.WeaponDefinition.AmmoDef.TrajectoryDef.ApproachDef.ConditionOperators;
using static Scripts.Structure.WeaponDefinition.AmmoDef.TrajectoryDef.ApproachDef.StageEvents;
using static Scripts.Structure.WeaponDefinition.AmmoDef.TrajectoryDef.ApproachDef;
using static Scripts.Structure.WeaponDefinition.AmmoDef.TrajectoryDef.GuidanceType;
using static Scripts.Structure.WeaponDefinition.AmmoDef.DamageScaleDef;
using static Scripts.Structure.WeaponDefinition.AmmoDef.DamageScaleDef.ShieldDef.ShieldType;
using static Scripts.Structure.WeaponDefinition.AmmoDef.DamageScaleDef.DeformDef.DeformTypes;
using static Scripts.Structure.WeaponDefinition.AmmoDef.AreaOfDamageDef;
using static Scripts.Structure.WeaponDefinition.AmmoDef.AreaOfDamageDef.Falloff;
using static Scripts.Structure.WeaponDefinition.AmmoDef.AreaOfDamageDef.AoeShape;
using static Scripts.Structure.WeaponDefinition.AmmoDef.EwarDef;
using static Scripts.Structure.WeaponDefinition.AmmoDef.EwarDef.EwarMode;
using static Scripts.Structure.WeaponDefinition.AmmoDef.EwarDef.EwarType;
using static Scripts.Structure.WeaponDefinition.AmmoDef.EwarDef.PushPullDef.Force;
using static Scripts.Structure.WeaponDefinition.AmmoDef.GraphicDef.LineDef;
using static Scripts.Structure.WeaponDefinition.AmmoDef.GraphicDef.LineDef.FactionColor;
using static Scripts.Structure.WeaponDefinition.AmmoDef.GraphicDef.LineDef.TracerBaseDef;
using static Scripts.Structure.WeaponDefinition.AmmoDef.GraphicDef.LineDef.Texture;
using static Scripts.Structure.WeaponDefinition.AmmoDef.GraphicDef.DecalDef;
using static Scripts.Structure.WeaponDefinition.AmmoDef.DamageScaleDef.DamageTypes.Damage;
using static Scripts.Structure;
using static Scripts.Structure.ArmorDefinition.ArmorType;
using static Scripts.Structure.WeaponDefinition.TargetingDef.BlockTypes;
using VRageMath;
using System.Collections.Generic;
using System.Linq;
using System;

namespace Scripts
{
    partial class Parts
    {
        const bool DEBUG_MODE = false;
        const float C = 299792458;

        public const float S1_DAMAGE_MULTIPLIER = 1 / 2f;
        public const float S2_DAMAGE_MULTIPLIER = 1 / 3f;
        public const float S3_DAMAGE_MULTIPLIER = 1 / 4f;
        public const float S5_DAMAGE_MULTIPLIER = 1 / 5f;
        public const float S7_DAMAGE_MULTIPLIER = 1 / 6f;

        const float PD_SLOW_VELOCITY = 2500;
        const float PD_VELOCITY = 5000;
        const float CANNON_VELOCITY = 2500;
        const float RAILGUN_VELOCITY = 15000f;
        const float MISSILE_START_VELOCITY = 200f;
        const float MISSILE_VELOCITY = 1250f;

        const float JD_INTEGRITY = 16000;

        const float INV_FILL_AMOUNT = 0.95f;
        const float INV_LOW_AMOUNT = 0.6f;

        const float VERYCLOSE_RANGE = 2500;
        const float CLOSE_RANGE = 5000;
        const float MEDIUM_RANGE = 10000;
        const float LONG_RANGE = 20000;

        const float ROTATE_CONSTANT = ((float)Math.PI / 180f) / 60f;
        const float PD_ROTATE_SPEED = 1800f * ROTATE_CONSTANT;
        const float FAST_ROTATE_SPEED = 120f * ROTATE_CONSTANT;
        const float MEDIUM_ROTATE_SPEED = 60f * ROTATE_CONSTANT;
        const float SLOW_ROTATE_SPEED = 30f * ROTATE_CONSTANT;
        const float VERYSLOW_ROTATE_SPEED = 15f * ROTATE_CONSTANT;

        public readonly TargetingDef.BlockTypes[] SUBSYSTEMS_TARGETING = new[]
        {
            Jumping, Thrust, TargetingDef.BlockTypes.Offense, Power, Utility, Any,
        };
        public readonly Vector4 PROJECTILE_SMOKE_TRAIL_COLOR = new Vector4(0.7f, 0.7f, 0.7f, 0.7f) * 1f;
        #region MISSILE TRACERS
        internal LineDef FX_ROCKET => GenericTracer(
            ShellColor: Color(red: 1f, green: 0.5f, blue: 0.3f, alpha: 0.04f) * 15f,
            ShellLength: 35f,
            ShellWidth: 0.6f,
            TrailColor: PROJECTILE_SMOKE_TRAIL_COLOR,
            TrailWidth: 0.5f,
            TrailLength: 25,
            UseColorFade: true
            );


        internal LineDef FX_MISSILE_SMALL => GenericTracer(
            ShellColor: Color(red: 0.05f, green: 0.25f, blue: 1f, alpha: 0.04f) * 15f,
            ShellLength: 20f,
            ShellWidth: 1f,
            TrailColor: PROJECTILE_SMOKE_TRAIL_COLOR,
            TrailWidth: 2.0f,
            TrailLength: 30,
            UseColorFade: true
            );

        internal LineDef FX_MISSILE_MEDIUM => GenericTracer(
            ShellColor: Color(red: 0.4f, green: 0.5f, blue: 1f, alpha: 0.04f) * 15f,
            ShellLength: 20f,
            ShellWidth: 1f,
            TrailColor: PROJECTILE_SMOKE_TRAIL_COLOR,
            TrailWidth: 2.5f,
            TrailLength: 40,
            UseColorFade: true
            );

        internal LineDef FX_MISSILE_LARGE => GenericTracer(
            ShellColor: Color(red: 0.7f, green: 0.85f, blue: 1f, alpha: 0.04f) * 15f,
            ShellLength: 20f,
            ShellWidth: 1.5f,
            TrailColor: PROJECTILE_SMOKE_TRAIL_COLOR,
            TrailWidth: 3f,
            TrailLength: 50,
            UseColorFade: true
            );

        #endregion

        #region CANNON TRACERS
        internal LineDef FX_CANNON_TINY => GenericTracer(
            ShellColor: Color(red: 1f, green: 0.8f, blue: 0.44f, alpha: 0.04f) * 15f,
            ShellLength: 25f,
            ShellWidth: 0.5f,
            TrailColor: PROJECTILE_SMOKE_TRAIL_COLOR,
            TrailWidth: 0f,
            TrailLength: 15
            );

        internal LineDef FX_CANNON_SMALL => GenericTracer(
            ShellColor: Color(1f, 0.8f, 0.5f, 0.04f) * 15f,
            ShellLength: 40f,
            ShellWidth: 0.6f,
            TrailColor: PROJECTILE_SMOKE_TRAIL_COLOR,
            TrailWidth: 0.5f,
            TrailLength: 30
            );

        internal LineDef FX_CANNON_MEDIUM => GenericTracer(
            ShellColor: Color(1f, 0.8f, 0.6f, 0.04f) * 15f,
            ShellLength: 50f,
            ShellWidth: 0.6f,
            TrailColor: PROJECTILE_SMOKE_TRAIL_COLOR,
            TrailWidth: 0.6f,
            TrailLength: 36
            );

        internal LineDef FX_CANNON_LARGE => GenericTracer(
            ShellColor: Color(1f, 0.92f, 0.7f, 0.04f) * 17f,
            ShellLength: 60f,
            ShellWidth: 0.8f,
            TrailColor: PROJECTILE_SMOKE_TRAIL_COLOR,
            TrailWidth: 0.8f,
            TrailLength: 42
            );
        #endregion
        #region RAILGUN TRACERS

        internal LineDef FX_RAILGUN_SHRAPNEL => GenericTracer(
            ShellColor: Color(0.4f, 0.5f, 1f, 0.03f) * 10f,
            ShellLength: 25f,
            ShellWidth: 0.4f,
            TrailColor: Color(0.2f, 0.4f, 1f, 0.01f) * 8f, //Color(0.4f, 0.5f, 1f, 0.01f) * 15f,
            TrailWidth: 0.4f,
            TrailLength: 8
            );

        internal LineDef FX_RAILGUN_TINY => GenericTracer(
            ShellColor: Color(0.4f, 0.5f, 1f, 0.03f) * 15f,
            ShellLength: 40f,
            ShellWidth: 0.6f,
            TrailColor: Color(0.2f, 0.3f, 1f, 0.02f) * 13f, //Color(0.4f, 0.5f, 1f, 0.01f) * 15f,
            TrailWidth: 0.5f,
            TrailLength: 10
            );
        internal LineDef FX_RAILGUN_SMALL => GenericTracer(
            ShellColor: Color(0.5f, 0.6f, 1f, 0.03f) * 20f,
            ShellLength: 50f,
            ShellWidth: 0.8f,
            TrailColor: Color(0.3f, 0.4f, 1f, 0.02f) * 16f, //Color(0.4f, 0.5f, 1f, 0.01f) * 15f,
            TrailWidth: 0.7f,
            TrailLength: 12
            );

        internal LineDef FX_RAILGUN_MEDIUM => GenericTracer(
            ShellColor: Color(0.6f, 0.7f, 1f, 0.025f) * 30f,
            ShellLength: 75f,
            ShellWidth: 1.0f,
            TrailColor: Color(0.4f, 0.5f, 1f, 0.02f) * 16f, //Color(0.4f, 0.5f, 1f, 0.01f) * 15f,
            TrailWidth: 0.9f,
            TrailLength: 20
            );

        internal LineDef FX_RAILGUN_LARGE => GenericTracer(
            ShellColor: Color(0.7f, 0.8f, 1f, 0.015f) * 40f,
            ShellLength: 125f,
            ShellWidth: 1.2f,
            TrailColor: Color(0.5f, 0.6f, 1f, 0.02f) * 20f, //Color(0.4f, 0.5f, 1f, 0.01f) * 15f,
            TrailWidth: 1.0f,
            TrailLength: 24
            );
        #endregion
    }
}
