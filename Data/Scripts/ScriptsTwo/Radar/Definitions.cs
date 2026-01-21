using ProtoBuf;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using ShipClass = VRage.MyTuple<int, string>;

namespace NerdRadar.Definitions
{
    public static class DefConstants
    {
        public const long MessageHandlerId = 3244679803;
    }
    /// <summary>
    /// Various options for radar line of sight interactions with watermod.
    /// <para>
    /// Values:
    /// <list type="table">
    /// <item>Ignore - Default value, no behavior change.</item>
    /// <item>Underwater - Only detects target if the LOS check starts and ends underwater.</item>
    /// <item>Abovewater - Only detects target if the LOS check starts and ends abovewater.</item>
    /// <item>EntersWater - Only detects target if the LOS check starts abovewater and ends underwater.</item>
    /// <item>ExitsWater - Only detects target if the LOS check starts underwater and ends abovewater.
    /// (according to a rough approximation of a sphere-line check w/ the water's radius).</item>
    /// <item>IntersectsWater - Set to true to have the radar only detect targets if the LOS start and end points are abovewater, but the line goes below water at some point.
    /// (according to a rough approximation of a sphere-line check w/ the water's radius).</item>
    /// </list>
    /// </para>
    /// </summary>
    [Serializable]
    [Flags]
    public enum WatermodLOSCheck : byte
    {
        /// <summary>
        /// Default value, no behavior change.
        /// </summary>
        Ignore = 0,
        /// <summary>
        /// Only detects target if the LOS check starts and ends underwater.
        /// </summary>
        Underwater = 1,
        /// <summary>
        /// Only detects target if the LOS check is completely abovewater. (according to a rough approximation of a sphere-line check w/ the water's radius).
        /// </summary>
        Abovewater = 2,
        /// <summary>
        /// Only detects target if the LOS check starts abovewater and ends underwater.
        /// </summary>
        EntersWater = 4,
        /// <summary>
        /// Only detects target if the LOS check starts underwater and ends abovewater.
        /// </summary>
        ExitsWater = 8,
        /// <summary>
        /// Only detects targets if the LOS start and end points are abovewater, but the line goes below water at some point. (according to a rough approximation of a sphere-line check w/ the water's radius).
        /// </summary>
        IntersectsWater = 16,
    }
    /// <summary>
    /// Parent class, ignore.
    /// </summary>
    [ProtoContract]
    [ProtoInclude(600, typeof(ClassConfig))]
    [ProtoInclude(601, typeof(BlockConfig))]
    public class EWARDefinition
    {

        public EWARDefinition()
        {
        }
    }
    /// <summary>
    /// Config for classifications.
    /// </summary>
    [ProtoContract]
    public class ClassConfig : EWARDefinition
    {
        [ProtoMember(1)] public bool ReplaceClasses;
        [ProtoMember(2)] public List<ShipClass> StationClasses;
        [ProtoMember(3)] public List<ShipClass> LargeGridClasses;
        [ProtoMember(4)] public List<ShipClass> SmallGridClasses;
    }
    /// <summary>
    /// Config for actual blocks
    /// </summary>
    [ProtoContract]
    public class BlockConfig : EWARDefinition
    {
        [ProtoMember(1)] public Dictionary<string, RadarStat> RadarStats;
        [ProtoMember(2)] public Dictionary<string, JammerStat> JammerStats;
        [ProtoMember(3)] public Dictionary<string, UpgradeBlockStat> UpgradeBlockStats;
        [ProtoMember(4)] public Dictionary<string, IFFBlockStat> IFFBlockStats;
    }
    /// <summary>
    /// Class containing various multipliers and addons to all radars, and the grid RCS its mounted on.
    /// </summary>
    [ProtoContract]
    public class UpgradeBlockStat
    {
        /// <summary>
        /// Sensitivity multiplier for ALL radars on the grid this is mounted on. Multipliers are calculated BEFORE addons.
        /// <para>
        /// Units: Unitless
        /// </para>
        /// <para>
        /// Requirements: <c>Value is a real number</c>
        /// </para>
        /// </summary>
        [ProtoMember(1)] public float SensitivityMultiplier = 1;
        /// <summary>
        /// Sensitivity addon for ALL radars on the grid this is mounted on. Addons are calculated AFTER multipliers.
        /// <para>
        /// Units: Decibels
        /// </para>
        /// <para>
        /// Requirements: <c>Value is a real number</c>
        /// </para>
        /// </summary>
        [ProtoMember(2)] public float SensitivityAddon = 0;

        /// <summary>
        /// Noise filter multiplier for ALL radars on the grid this is mounted on. Multipliers are calculated BEFORE addons.
        /// <para>
        /// Units: Unitless
        /// </para>
        /// <para>
        /// Requirements: <c>Value is a real number</c>
        /// </para>
        /// </summary>
        [ProtoMember(3)] public float NoiseFilterMultiplier = 1;
        /// <summary>
        /// Noise filter addon for ALL radars on the grid this is mounted on. Addons are calculated AFTER multipliers.
        /// <para>
        /// Units: Decibels
        /// </para>
        /// <para>
        /// Requirements: <c>Value is a real number</c>
        /// </para>
        /// </summary>
        [ProtoMember(4)] public float NoiseFilterAddon = 0;

        /// <summary>
        /// Positional error multiplier for ALL radars on the grid this is mounted on. Multipliers are calculated BEFORE addons.
        /// <para>
        /// Units: Unitless
        /// </para>
        /// <para>
        /// Requirements: <c>Value is a real number</c>
        /// </para>
        /// </summary>
        [ProtoMember(5)] public float PositionalErrorMultiplier = 1;
        /// <summary>
        /// Positional error addon for ALL radars on the grid this is mounted on. Addons are calculated AFTER multipliers.
        /// <para>
        /// Units: Meters
        /// </para>
        /// <para>
        /// Requirements: <c>Value is a real number</c>
        /// </para>
        /// </summary>
        [ProtoMember(6)] public float PositionalErrorAddon = 0;

        /// <summary>
        /// Velocity error multiplier for ALL radars on the grid this is mounted on. Multipliers are calculated BEFORE addons.
        /// <para>
        /// Units: Unitless
        /// </para>
        /// <para>
        /// Requirements: <c>Value is a real number</c>
        /// </para>
        /// </summary>
        [ProtoMember(7)] public float VelocityErrorMultiplier = 1;
        /// <summary>
        /// Velocity error addon for ALL radars on the grid this is mounted on. Addons are calculated AFTER multipliers.
        /// <para>
        /// Units: Meters per second
        /// </para>
        /// <para>
        /// Requirements: <c>Value is a real number</c>
        /// </para>
        /// </summary>
        [ProtoMember(8)] public float VelocityErrorAddon = 0;

        /// <summary>
        /// RCS multiplier for the grid this is mounted on. Multipliers are calculated BEFORE addons.
        /// <para>
        /// Units: Unitless
        /// </para>
        /// <para>
        /// Requirements: <c>Value is a real number</c>
        /// </para>
        /// </summary>
        [ProtoMember(9)] public float RCSMultiplier = 1;
        /// <summary>
        /// RCS addon for the grid this is mounted on. Addons are calculated AFTER multipliers.
        /// <para>
        /// Units: Meters per second
        /// </para>
        /// <para>
        /// Requirements: <c>Value is a real number</c>
        /// </para>
        /// </summary>
        [ProtoMember(10)] public float RCSAddon = 0;

        /// <summary>
        /// Make multipliers apply only if the block is functional and on.
        /// <br>Mutually exclusive with ApplyOnlyWhenFiring</br>
        /// </summary>
        [ProtoMember(11)] public bool ApplyOnlyWhenOn = false;

        /// <summary>
        /// Make multipliers apply only if the block is a weapon and is firing. :)
        /// <br>Mutually exclusive with ApplyOnlywhenOn</br>
        /// </summary>
        [ProtoMember(12)] public bool ApplyOnlyWhenFiring = false;

        /// <summary>
        /// If there are multiple definitions with the same subtype ID (like say someone is adjusting stats of another's mod), then the definition with the highest priority will be loaded.
        /// <para>For people making their own mod, its recommended to leave this at zero.</para>
        /// <para>For people MODIFYING other people's mod, its recommended to set this at anything greater than zero.</para>
        /// <para>This effectively allows mod adjuster-like behavior without relying on mod load order, although the entire definition must be copied for it to work properly. Those modifying stats can just have the definitions in their place w/o copying any models, sbc files, or sounds to the modified mod.</para>
        /// <para>
        /// Units: Unitless
        /// </para>
        /// <para>
        /// Requirements: <c>Value is an integer</c>
        /// </para>
        /// </summary>
        [ProtoMember(13)] public int DefinitionPriority;

        /// <summary>
        /// A string to be added onto the class or IFF beacon when this block is present. Useful if you want radar to detect the presence of a certain block and display it on other HUDs like a nuclear warhead or something. Not realistic but can be useful for balancing.
        /// <para>Only one will be displayed, use <c>NameAddonPriority</c> to determine which blocks are shown first</para>
        /// </summary>
        [ProtoMember(14)] public string NameAddon;

        /// <summary>
        /// Priority to determine which upgrade blocks with a specified <c>NameAddon</c> are shown if multiple are present. Higher priorities will be shown. Blocks with the same priority will be shown in order of first processed.
        /// <para>
        /// Units: Unitless
        /// </para>
        /// <para>
        /// Requirements: <c>Value is an integer</c>
        /// </para>
        /// </summary>
        [ProtoMember(15)] public int NameAddonPriority;

        /// <summary>
        /// If true, <c>NameAddon</c> will be applied regardless of whether <c>ApplyOnlyWhenOn</c> or <c>ApplyOnlyWhenFiring</c> is true and conditions are meant.
        /// </summary>
        [ProtoMember(16)] public bool NameAddonIgnoresFunctional;

        /// <summary>
        /// If greater than zero, overrides the block's power requirement to this value in MW.
        /// <para>
        /// Units: MW
        /// </para>
        /// <para>
        /// Requirements: <c>Value is greater than or equal to 0</c>
        /// </para>
        /// </summary>
        [ProtoMember(17)] public float PowerRequirementOverride;
    }
    /// <summary>
    /// Class containing all the stats for radar. Most stats can be found here: http://nebfltcom.wikidot.com/mechanics:radar
    /// </summary>
    [ProtoContract]
    public class RadarStat
    {
        /// <summary>
        /// Maximum radiated power of the radar.
        /// <para>
        /// Units: Kilowatts (kW)
        /// </para>
        /// <para>
        /// Requirements: <c>Value > 0</c>
        /// </para>
        /// </summary>
        [ProtoMember(1)] public float MaxRadiatedPower;
        /// <summary>
        /// Gain of the radar system.
        /// <para>
        /// Units: Decibels (dB)
        /// </para>
        /// <para>
        /// Requirements: <c>Value > 0</c>
        /// </para>
        /// </summary>
        [ProtoMember(2)] public float Gain;
        /// <summary>
        /// Maximum possible search range of the radar. Radar will not detect any targets past this range, even if they could otherwise.
        /// <para>
        /// Units: Meters (m)
        /// </para>
        /// <para>
        /// Requirements: <c>Value > 0</c>
        /// </para>
        /// </summary>
        [ProtoMember(3)] public float MaxSearchRange;
        /// <summary>
        /// Aperture Size of the radar.
        /// <para>
        /// Units: Meters Squared (m^2)
        /// </para>
        /// <para>
        /// Requirements: <c>Value > 0</c>
        /// </para>
        /// </summary>
        [ProtoMember(4)] public float ApertureSize;
        /// <summary>
        /// Noise filtering of the radar system. lower is better.<list type="bullet">
        /// </list>
        /// <para>
        /// Units: Decibels (dB)
        /// </para>
        /// <para>
        /// Requirements: <c>Value is a real number</c>
        /// </para>
        /// </summary>
        [ProtoMember(5)]
        public double NoiseFilter;
        /// <summary>
        /// Minimum required ratio of returned signal to noise; 
        /// <para>
        /// Units: None
        /// </para>
        /// <para>
        /// Requirements: <c>Value > 0</c>
        /// </para>
        /// </summary>
        [ProtoMember(6)] public float SignalToNoiseRatio;
        /// <summary>
        /// Sensitivity of the radar
        /// <para>
        /// Units: Decibels (dB)
        /// </para>
        /// <para>
        /// Requirements: <c>Value is a real number</c>
        /// </para>
        /// </summary>
        [ProtoMember(7)] public double Sensitivity;

        /// <summary>
        /// Position error of the radar. Note, position and velocity error of 0 designates locked targets, regardless of whether the target is actually locked.
        /// <para>
        /// Units: Meters (m)
        /// </para>
        /// <para>
        /// Requirements: <c>Value >= 0</c>
        /// </para>
        /// </summary>
        [ProtoMember(8)] public float PositionError;
        /// <summary>
        /// Velocity error of the radar. Does nothing currently, come back later when I figure out how to draw arbitrary lines on screen. Note, position and velocity error of 0 designates locked targets, regardless of whether the target is actually locked.
        /// <para>
        /// Units: Meters (m)
        /// </para>
        /// <para>
        /// Requirements: <c>Value >= 0</c>
        /// </para>
        /// </summary>
        [ProtoMember(9)] public float VelocityError;

        /// <summary>
        /// Determines whether or not the radar system can target lock
        /// <para>
        /// Units: N/A
        /// </para>
        /// <para>
        /// Requirements: <c>true</c> or <c>false</c>
        /// </para>
        /// </summary>
        [ProtoMember(10)] public bool CanTargetLock;
        /// <summary>
        /// Determines whether or not the radar system needs to be externally mounted by making the LOS check fail if the parent grid is in the way.
        /// <para>
        /// Units: N/A
        /// </para>
        /// <para>
        /// Requirements: <c>true</c> or <c>false</c>
        /// </para>
        /// </summary>
        [ProtoMember(11)] public bool LOSCheckIncludesParentGrid;

        /// <summary>
        /// Multiplier for the RCS of a target grid in stealth from the mod https://steamcommunity.com/sharedfiles/filedetails/?id=2805859069 (Stealth Drive by Ash Like Snow)
        /// <para>
        /// Units: None
        /// </para>
        /// <para>
        /// Requirements: <c>Value >= 0</c>
        /// </para>
        /// </summary>
        [ProtoMember(12)] public float StealthMultiplier;

        /// <summary>
        /// Determines if the radar can detect and show any jumps caused by any track visible to the radar.
        /// <br>Mutually exclusive with CanDetectLockedJumps</br>
        /// </summary>
        [ProtoMember(13)] public bool CanDetectAllJumps;

        /// <summary>
        /// Determines if the radar can detect and show any jumps caused by the tracked locked by the radar.
        /// <br>Mutually exclusive with CanDetectAllJumps</br>
        /// </summary>
        [ProtoMember(14)] public bool CanDetectLockedJumps;

        /// <summary>
        /// If the radar is a turret and is aiming at a target grid/block, then automatically lock whatever grid its firing it.
        /// </summary>
        [ProtoMember(15)] public bool AutoLockTurretTarget = false;

        /// <summary>
        /// Various options for radar line of sight interactions with watermod.
        /// <para>
        /// Values:
        /// <list type="table">
        /// <item>Ignore - Default value, no behavior change.</item>
        /// <item>Underwater - Only detects target if the LOS check starts and ends underwater.</item>
        /// <item>Abovewater - Only detects target if the LOS check starts and ends abovewater.</item>
        /// <item>EntersWater - Only detects target if the LOS check starts abovewater and ends underwater.</item>
        /// <item>ExitsWater - Only detects target if the LOS check starts underwater and ends abovewater.
        /// (according to a rough approximation of a sphere-line check w/ the water's radius).</item>
        /// <item>IntersectsWater - Set to true to have the radar only detect targets if the LOS start and end points are abovewater, but the line goes below water at some point.
        /// (according to a rough approximation of a sphere-line check w/ the water's radius).</item>
        /// </list>
        /// </para>
        /// </summary>
        [DefaultValue(0)]
        [ProtoMember(16)] private WatermodLOSCheck Mod_Watermod_LOSCheckOptions;

        /// <summary>
        /// Set to true if no other Mod_Watermod_LOSCheck____ booleans are set to true
        /// </summary>
        [ProtoIgnore]
        public bool Mod_Watermod_LOSCheckIgnore
        {
            get
            {
                return Mod_Watermod_LOSCheckOptions == 0;
            }
            set
            {
                if (value)
                    Mod_Watermod_LOSCheckOptions |= WatermodLOSCheck.Ignore;

                else
                    Mod_Watermod_LOSCheckOptions &= ~WatermodLOSCheck.Ignore;
            }
        }

        /// <summary>
        /// Set to true to have the radar only detect targets if the LOS check endpoints are underwater.
        /// </summary>
        [ProtoIgnore]
        public bool Mod_Watermod_LOSCheck_Underwater
        {
            get
            {
                return Mod_Watermod_LOSCheckOptions.HasFlag(WatermodLOSCheck.Underwater);
            }
            set
            {
                if (value)
                    Mod_Watermod_LOSCheckOptions |= WatermodLOSCheck.Underwater;

                else
                    Mod_Watermod_LOSCheckOptions &= ~WatermodLOSCheck.Underwater;
            }
        }

        /// <summary>
        /// Set to true to have the radar only detect targets if the LOS check is completely abovewater.
        /// (according to a rough approximation of a sphere-line check w/ the water's radius)
        /// </summary>
        [ProtoIgnore]
        public bool Mod_Watermod_LOSCheck_Abovewater
        {
            get
            {
                return Mod_Watermod_LOSCheckOptions.HasFlag(WatermodLOSCheck.Abovewater);
            }
            set
            {
                if (value)
                    Mod_Watermod_LOSCheckOptions |= WatermodLOSCheck.Abovewater;

                else
                    Mod_Watermod_LOSCheckOptions &= ~WatermodLOSCheck.Abovewater;
            }
        }
        /// <summary>
        /// Set to true to have the radar only detect targets if the LOS start and end points are abovewater, but the line goes below water at some point.
        /// (according to a rough approximation of a sphere-line check w/ the water's radius)
        /// </summary>
        [ProtoIgnore]
        public bool Mod_Watermod_LOSCheck_IntersectsWater
        {
            get
            {
                return Mod_Watermod_LOSCheckOptions.HasFlag(WatermodLOSCheck.IntersectsWater);
            }
            set
            {
                if (value)
                    Mod_Watermod_LOSCheckOptions |= WatermodLOSCheck.IntersectsWater;

                else
                    Mod_Watermod_LOSCheckOptions &= ~WatermodLOSCheck.IntersectsWater;
            }
        }

        /// <summary>
        /// Set to true to have the radar only detect targets if the LOS check starts abovewater and ends underwater.
        /// </summary>
        [ProtoIgnore]
        public bool Mod_Watermod_LOSCheck_EntersWater
        {
            get
            {
                return Mod_Watermod_LOSCheckOptions.HasFlag(WatermodLOSCheck.EntersWater);
            }
            set
            {
                if (value)
                    Mod_Watermod_LOSCheckOptions |= WatermodLOSCheck.EntersWater;

                else
                    Mod_Watermod_LOSCheckOptions &= ~WatermodLOSCheck.EntersWater;
            }
        }

        /// <summary>
        /// Set to true to have the radar only detect targets if the LOS check starts underwater and ends abovewater.
        /// </summary>
        [ProtoIgnore]
        public bool Mod_Watermod_LOSCheck_ExitsWater
        {
            get
            {
                return Mod_Watermod_LOSCheckOptions.HasFlag(WatermodLOSCheck.ExitsWater);
            }
            set
            {
                if (value)
                    Mod_Watermod_LOSCheckOptions |= WatermodLOSCheck.ExitsWater;

                else
                    Mod_Watermod_LOSCheckOptions &= ~WatermodLOSCheck.ExitsWater;
            }
        }

        /// <summary>
        /// If there are multiple definitions with the same subtype ID (like say someone is adjusting stats of another's  mod), then the definition with the highest priority will be loaded.
        /// <para>For people making their own  mod, its recommended to leave this at zero.</para>
        /// <para>For people MODIFYING other people's mod, its recommended to set this at anything greater than zero.</para>
        /// <para>This effectively allows mod adjuster-like behavior without relying on mod load order, although the entire definition must be copied for it to work properly. Those modifying  stats can just have the  definitions in their place w/o copying any models, sbc files, or sounds to the modified mod.</para>
        /// <para>
        /// Units: Unitless
        /// </para>
        /// <para>
        /// Requirements: <c>Value is an integer</c>
        /// </para>
        /// </summary>
        [ProtoMember(17)] public int DefinitionPriority;

        /// <summary>
        /// If greater than zero, overrides the block's power requirement to this value in MW.
        /// <para>
        /// Units: MW
        /// </para>
        /// <para>
        /// Requirements: <c>Value is greater than or equal to 0</c>
        /// </para>
        /// </summary>
        [ProtoMember(18)] public float PowerRequirementOverride;

        public RadarStat()
        {
        }
    }
    /// <summary>
    /// Class containing all the stats for jammers.
    /// </summary>
    [ProtoContract]
    public class JammerStat
    {
        /// <summary>
        /// Maximum radiated power of the jammer turret.
        /// <para>
        /// Units: Kilowatts (kW)
        /// </para>
        /// <para>
        /// Requirements: <c>Value > 0</c>
        /// </para>
        /// </summary>
        [ProtoMember(1)] public float MaxRadiatedPower;
        /// <summary>
        /// Gain of the jammer turret.
        /// <para>
        /// Units: Decibels (dB)
        /// </para>
        /// <para>
        /// Requirements: <c>Value > 0</c>
        /// </para>
        /// </summary>
        [ProtoMember(2)] public float Gain;
        /// <summary>
        /// Maximum possible search range of the jammer. The jammer will only affect radars within this range.
        /// <para>
        /// Units: Meters (m)
        /// </para>
        /// <para>
        /// Requirements: <c>Value > 0</c>
        /// </para>
        /// </summary>
        [ProtoMember(3)] public float MaxSearchRange;
        /// <summary>
        /// Determines whether or not the jammer system needs to be externally mounted by failing if the parent grid is in the way.
        /// <para>
        /// Units: N/A
        /// </para>
        /// <para>
        /// Requirements: <c>true</c> or <c>false</c>
        /// </para>
        /// </summary>
        [ProtoMember(4)] public bool LOSCheckIncludesParentGrid;
        /// <summary>
        /// Angle of the jamming cone.
        /// <para>
        /// Units: Radians (Use <c>MathHelperD.ToRadians([value]),</c> in place of a number to set value in degrees)
        /// </para>
        /// <para>
        /// Requirements: <c>Value > 0</c>
        /// </para>
        /// </summary>
        [ProtoMember(5)] public double AngleRadians;
        /// <summary>
        /// Area effect ratio of the jammer. Best explained here http://nebfltcom.wikidot.com/mechanics:electronic-warfare, although here it is a cylinder rather than a rectanglular prism.
        /// <para>
        /// Units: None
        /// </para>
        /// <para>
        /// Requirements: <c>Value > 0</c>
        /// </para>
        /// </summary>
        [ProtoMember(6)] public float AreaEffectRatio;
        /// <summary>
        /// Maximum heat buildup before the jammer automatically turns off. Set to -1 to disable. Jammers will gain 1 heat per tick no matter what.
        /// <para>
        /// Units: Proprietary Heat Measurement Unit™
        /// </para>
        /// <para>
        /// Requirements: <c>Value > 0</c> or <c>-1</c>
        /// </para>
        /// </summary>
        [ProtoMember(7)] public float MaxHeat;
        /// <summary>
        /// Amount of heat dissapated every tick the jammer is off. 
        /// <para>
        /// Units: Proprietary Heat Measurement Unit™ per tick
        /// </para>
        /// <para>
        /// Requirements: <c>Value > 0</c>
        /// </para>
        /// </summary>
        [ProtoMember(8)] public float HeatDrainPerTick;

        /// <summary>
        /// If there are multiple definitions with the same subtype ID (like say someone is adjusting stats of another's  mod), then the definition with the highest priority will be loaded.
        /// <para>For people making their own  mod, its recommended to leave this at zero.</para>
        /// <para>For people MODIFYING other people's mod, its recommended to set this at anything greater than zero.</para>
        /// <para>This effectively allows mod adjuster-like behavior without relying on mod load order, although the entire definition must be copied for it to work properly. Those modifying  stats can just have the  definitions in their place w/o copying any models, sbc files, or sounds to the modified mod.</para>
        /// <para>
        /// Units: Unitless
        /// </para>
        /// <para>
        /// Requirements: <c>Value is an integer</c>
        /// </para>
        /// </summary>
        [ProtoMember(9)] public int DefinitionPriority;

        /// <summary>
        /// Speed in degrees per second to rotate the azimuth subpart. Defaults to interior turret sbc values if the jammer is an interior turret (Ignores this setting if so).
        /// <para>
        /// Units: Deg/s
        /// </para>
        /// <para>
        /// Requirements: <c>Value > 0</c>
        /// </para>
        /// </summary>
        [ProtoMember(10)] public float AzimuthSpeed;
        /// <summary>
        /// Speed in degrees per second to rotate the elevation subpart. Defaults to interior turret sbc values if the jammer is an interior turret (Ignores this setting if so).
        /// <para>
        /// Units: Deg/s
        /// </para>
        /// <para>
        /// Requirements: <c>Value > 0</c>
        /// </para>
        /// </summary>
        [ProtoMember(11)] public float ElevationSpeed;

        /// <summary>
        /// Angle from grid forward to allow the subpart to go to left. -180 & 180 with MaxAzimuth means unlimited. Defaults to interior turret sbc values if the jammer is an interior turret (Ignores this setting if so).
        /// <para>
        /// Units: Degrees
        /// </para>
        /// <para>
        /// Requirements: <c>-180 &lt;= value &lt;= 180</c>
        /// </para>
        /// </summary>
        [ProtoMember(12)] public float MinAzimuth;
        /// <summary>
        /// Angle from grid forward to allow the subpart to go to right. 180 & -180 with MinAzimuth means unlimited. Defaults to interior turret sbc values if the jammer is an interior turret (Ignores this setting if so).
        /// <para>
        /// Units: Degrees
        /// </para>
        /// <para>
        /// Requirements: <c>-180 &lt;= value &lt;= 180</c>
        /// </para>
        /// </summary>
        [ProtoMember(13)] public float MaxAzimuth;

        /// <summary>
        /// Angle from grid forward to allow the subpart to go to down. Defaults to interior turret sbc values if the jammer is an interior turret (Ignores this setting if so).
        /// <para>
        /// Units: Degrees
        /// </para>
        /// <para>
        /// Requirements: <c>-90 &lt;= value &lt;= 90</c>
        /// </para>
        /// </summary>
        [ProtoMember(14)] public float MinElevation;
        /// <summary>
        /// Angle from grid forward to allow the subpart to go to up. Defaults to interior turret sbc values if the jammer is an interior turret (Ignores this setting if so).
        /// <para>
        /// Units: Degrees
        /// </para>
        /// <para>
        /// Requirements: <c>-90 &lt;= value &lt;= 90</c>
        /// </para>
        /// </summary>
        [ProtoMember(15)] public float MaxElevation;

        /// <summary>
        /// Azimuth subpart name. Should not contain the `subpart_` prefix. Defaults to interior turret sbc values if the jammer is an interior turret (Ignores this setting if so).
        /// <para>
        /// Units: N/A
        /// </para>
        /// <para>
        /// Requirements: <c>String</c>
        /// </para>
        /// </summary>
        [ProtoMember(16)] public string AzimuthSubpartName;
        /// <summary>
        /// Elevation subpart name. Should not contain the `subpart_` prefix. Defaults to interior turret sbc values if the jammer is an interior turret (Ignores this setting if so).
        /// <para>
        /// Units: N/A
        /// </para>
        /// <para>
        /// Requirements: <c>String</c>
        /// </para>
        /// </summary>
        [ProtoMember(17)] public string ElevationSubpartName;
        /// <summary>
        /// Empty where the jammer cone comes from. Defaults to block + block.Up*0.5 if the jammer is an interior turret (Ignores this setting if so).
        /// <para>
        /// Units: N/A
        /// </para>
        /// <para>
        /// Requirements: <c>String</c>
        /// </para>
        /// </summary>
        [ProtoMember(18)] public string MuzzleEmptyName;

        /// <summary>
        /// If greater than zero, overrides the block's power requirement to this value in MW.
        /// <para>
        /// Units: MW
        /// </para>
        /// <para>
        /// Requirements: <c>Value is greater than or equal to 0</c>
        /// </para>
        /// </summary>
        [ProtoMember(19)] public float PowerRequirementOverride;

        public JammerStat()
        {
        }
    }
    /// <summary>
    /// Class for containing all the stats for an IFF block - a beacon which will overwrite the default name listed on its radar track.
    /// </summary>
    [ProtoContract]
    public class IFFBlockStat
    {
        /// <summary>
        /// Maximum characters the IFF beacon is allowed to render on the radar track name. Set to zero to disable.
        /// </summary>
        [ProtoMember(1)] public int MaxCharacters;
        /// <summary>
        /// Whether or not to include the classification on the IFF name. 
        /// </summary>
        [ProtoMember(2)] public bool ShowClass;

        /// <summary>
        /// If there are multiple definitions with the same subtype ID (like say someone is adjusting stats of another's  mod), then the definition with the highest priority will be loaded.
        /// <para>For people making their own  mod, its recommended to leave this at zero.</para>
        /// <para>For people MODIFYING other people's mod, its recommended to set this at anything greater than zero.</para>
        /// <para>This effectively allows mod adjuster-like behavior without relying on mod load order, although the entire definition must be copied for it to work properly. Those modifying  stats can just have the  definitions in their place w/o copying any models, sbc files, or sounds to the modified mod.</para>
        /// <para>
        /// Units: Unitless
        /// </para>
        /// <para>
        /// Requirements: <c>Value is an integer</c>
        /// </para>
        /// </summary>
        [ProtoMember(3)] public int DefinitionPriority;
    }
}