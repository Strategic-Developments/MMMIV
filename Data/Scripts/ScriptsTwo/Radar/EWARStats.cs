using System.Collections.Generic;
using VRage.Game;
using VRage.Game.Components;
using NerdRadar.Definitions;
using Sandbox.ModAPI;
using VRageMath;
using System;
using System.Linq.Expressions;

namespace SRRadarStats
{
    [MySessionComponentDescriptor(MyUpdateOrder.NoUpdate)]
    public class EWARStats : MySessionComponentBase
    {
        BlockConfig cfg => new BlockConfig()
        {
            RadarStats = new Dictionary<string, RadarStat>()
            {
                ["Nerd_Radar"] = new RadarStat()
                {
                    // If there are multiple definitions with the same subtype ID (like say someone is adjusting stats of another's mod), then the definition with the highest priority will be loaded.
                    // For people making their own mod, its recommended to leave this at zero.
                    // For people MODIFYING other people's mod, its recommended to set this at anything greater than zero.
                    // This effectively allows mod adjuster-like behavior without relying on mod load order, although the entire definition must be copied for it to work properly.
                    //  - those modifying stats can just have the definitions in their place w/o copying any models, sbc files, or sounds to the modified mod.
                    DefinitionPriority = 1,

                    MaxRadiatedPower = 0, // radiated power of the radar, in kilowatts
                    Gain = 0, // gain of the radar, in decibels. Must be above 0
                    Sensitivity = 0, // sensitivity of the radar, in decibels
                    MaxSearchRange = 1, // maximum range radar will return targets, regardless of other settings, in meters

                    ApertureSize = 40, // aperture size of the radar, in meters^2
                    NoiseFilter = 0, // noise filter of the radar, in decibels
                    SignalToNoiseRatio = 1, // ratio of return signal to noise required for the radar to detect targets

                    PositionError = 70, // maximum error of position in any given direction the radar returns in meters
                    VelocityError = 5, // maximum error in the velocity vector in any given direction the radar returns (velocity indicator coming soonTM)

                    CanTargetLock = true, // determines whether or not the radar can lock. Locked targets have no velocity and position error, and will have the radar detected icon turn red from yellow.
                                          // When a guided missile from Vanilla+ fired from a grid with a radar locking another grid lacks a target, Neb. Radar will provide it with the radar's target lock, assuming a valid block exists on the target exists. Should multiple targets be locked by one grid, the missile will choose the closest one.
                    LOSCheckIncludesParentGrid = false,
                    // determines whether the radar's LOS check will include the grid it is on. Useful for radar paneling and such. Subgrids attached to the main grid count as the main grid in this case.
                    StealthMultiplier = 1, // if the target is cloaked via the Stealth Drive mod (https://steamcommunity.com/sharedfiles/filedetails/?id=2805859069), then the target's RCS is multiplied by this when the radar scans.

                    CanDetectAllJumps = true, // Determines if the radar can detect and show any jumps caused by any track visible to the radar.
                    CanDetectLockedJumps = false, // Determines if the radar can detect and show any jumps caused by the tracked locked by the radar.

                    AutoLockTurretTarget = false, // If the radar is a turret and is aiming at a target grid/block, then automatically radar lock whatever grid its firing it.

                    // Watermod LOS interactions. Governs whether or not radar will only detect targets in, out, or between water. They operate on an "or" basis - if one of the set conditions is true the radar will detect target.
                    // Ex: Setting EntersWater and ExitsWater to true will means that the radar will only detect targets opposite of it; underwater if its above and above if its under, rather than nothing at all.
                    Mod_Watermod_LOSCheckIgnore = true, // Set to true if no other Mod_Watermod_LOSCheck____ booleans are set to true
                    Mod_Watermod_LOSCheck_Abovewater = false, // Set to true to have the radar only detect targets if the LOS check starts and ends underwater.
                    Mod_Watermod_LOSCheck_Underwater = false, // Set to true to have the radar only detect targets if the LOS check starts and ends abovewater.
                    Mod_Watermod_LOSCheck_EntersWater = false, // Set to true to have the radar only detect targets if the LOS check starts abovewater and ends underwater.
                    Mod_Watermod_LOSCheck_ExitsWater = false, // Set to true to have the radar only detect targets if the LOS check starts underwater and ends abovewater.
                
                    PowerRequirementOverride = 50f,
                },
                ["Nerd_Radar_Basic"] = new RadarStat()
                {
                    // If there are multiple definitions with the same subtype ID (like say someone is adjusting stats of another's mod), then the definition with the highest priority will be loaded.
                    // For people making their own mod, its recommended to leave this at zero.
                    // For people MODIFYING other people's mod, its recommended to set this at anything greater than zero.
                    // This effectively allows mod adjuster-like behavior without relying on mod load order, although the entire definition must be copied for it to work properly.
                    //  - those modifying stats can just have the definitions in their place w/o copying any models, sbc files, or sounds to the modified mod.
                    DefinitionPriority = 1,

                    MaxRadiatedPower = 50000, // radiated power of the radar, in kilowatts
                    Gain = 175, // gain of the radar, in decibels. Must be above 0
                    Sensitivity = -39, // sensitivity of the radar, in decibels
                    MaxSearchRange = 300000, // maximum range radar will return targets, regardless of other settings, in meters

                    ApertureSize = 100, // aperture size of the radar, in meters^2
                    NoiseFilter = 0, // noise filter of the radar, in decibels
                    SignalToNoiseRatio = 1, // ratio of return signal to noise required for the radar to detect targets

                    PositionError = 100, // maximum error of position in any given direction the radar returns in meters
                    VelocityError = 5, // maximum error in the velocity vector in any given direction the radar returns (velocity indicator coming soonTM)

                    CanTargetLock = true, // determines whether or not the radar can lock. Locked targets have no velocity and position error, and will have the radar detected icon turn red from yellow.
                                          // When a guided missile from Vanilla+ fired from a grid with a radar locking another grid lacks a target, Neb. Radar will provide it with the radar's target lock, assuming a valid block exists on the target exists. Should multiple targets be locked by one grid, the missile will choose the closest one.
                    LOSCheckIncludesParentGrid = false,
                    // determines whether the radar's LOS check will include the grid it is on. Useful for radar paneling and such. Subgrids attached to the main grid count as the main grid in this case.
                    StealthMultiplier = 0.1f, // if the target is cloaked via the Stealth Drive mod (https://steamcommunity.com/sharedfiles/filedetails/?id=2805859069), then the target's RCS is multiplied by this when the radar scans.

                    CanDetectAllJumps = true, // Determines if the radar can detect and show any jumps caused by any track visible to the radar.
                    CanDetectLockedJumps = false, // Determines if the radar can detect and show any jumps caused by the tracked locked by the radar.

                    AutoLockTurretTarget = false, // If the radar is a turret and is aiming at a target grid/block, then automatically radar lock whatever grid its firing it.

                    // Watermod LOS interactions. Governs whether or not radar will only detect targets in, out, or between water. They operate on an "or" basis - if one of the set conditions is true the radar will detect target.
                    // Ex: Setting EntersWater and ExitsWater to true will means that the radar will only detect targets opposite of it; underwater if its above and above if its under, rather than nothing at all.
                    Mod_Watermod_LOSCheckIgnore = true, // Set to true if no other Mod_Watermod_LOSCheck____ booleans are set to true
                    Mod_Watermod_LOSCheck_Abovewater = false, // Set to true to have the radar only detect targets if the LOS check starts and ends underwater.
                    Mod_Watermod_LOSCheck_Underwater = false, // Set to true to have the radar only detect targets if the LOS check starts and ends abovewater.
                    Mod_Watermod_LOSCheck_EntersWater = false, // Set to true to have the radar only detect targets if the LOS check starts abovewater and ends underwater.
                    Mod_Watermod_LOSCheck_ExitsWater = false, // Set to true to have the radar only detect targets if the LOS check starts underwater and ends abovewater.

                    PowerRequirementOverride = 50f,
                },
                ["Nerd_Radar_SG"] = new RadarStat()
                {
                    // If there are multiple definitions with the same subtype ID (like say someone is adjusting stats of another's mod), then the definition with the highest priority will be loaded.
                    // For people making their own mod, its recommended to leave this at zero.
                    // For people MODIFYING other people's mod, its recommended to set this at anything greater than zero.
                    // This effectively allows mod adjuster-like behavior without relying on mod load order, although the entire definition must be copied for it to work properly.
                    //  - those modifying stats can just have the definitions in their place w/o copying any models, sbc files, or sounds to the modified mod.
                    DefinitionPriority = 1,

                    MaxRadiatedPower = 10000, // radiated power of the radar, in kilowatts
                    Gain = 175, // gain of the radar, in decibels. Must be above 0
                    Sensitivity = -39, // sensitivity of the radar, in decibels
                    MaxSearchRange = 250000, // maximum range radar will return targets, regardless of other settings, in meters

                    ApertureSize = 100, // aperture size of the radar, in meters^2
                    NoiseFilter = 0, // noise filter of the radar, in decibels
                    SignalToNoiseRatio = 1, // ratio of return signal to noise required for the radar to detect targets

                    PositionError = 100, // maximum error of position in any given direction the radar returns in meters
                    VelocityError = 5, // maximum error in the velocity vector in any given direction the radar returns (velocity indicator coming soonTM)

                    CanTargetLock = true, // determines whether or not the radar can lock. Locked targets have no velocity and position error, and will have the radar detected icon turn red from yellow.
                                          // When a guided missile from Vanilla+ fired from a grid with a radar locking another grid lacks a target, Neb. Radar will provide it with the radar's target lock, assuming a valid block exists on the target exists. Should multiple targets be locked by one grid, the missile will choose the closest one.
                    LOSCheckIncludesParentGrid = false,
                    // determines whether the radar's LOS check will include the grid it is on. Useful for radar paneling and such. Subgrids attached to the main grid count as the main grid in this case.
                    StealthMultiplier = 0.1f, // if the target is cloaked via the Stealth Drive mod (https://steamcommunity.com/sharedfiles/filedetails/?id=2805859069), then the target's RCS is multiplied by this when the radar scans.

                    CanDetectAllJumps = true, // Determines if the radar can detect and show any jumps caused by any track visible to the radar.
                    CanDetectLockedJumps = false, // Determines if the radar can detect and show any jumps caused by the tracked locked by the radar.

                    AutoLockTurretTarget = false, // If the radar is a turret and is aiming at a target grid/block, then automatically radar lock whatever grid its firing it.

                    // Watermod LOS interactions. Governs whether or not radar will only detect targets in, out, or between water. They operate on an "or" basis - if one of the set conditions is true the radar will detect target.
                    // Ex: Setting EntersWater and ExitsWater to true will means that the radar will only detect targets opposite of it; underwater if its above and above if its under, rather than nothing at all.
                    Mod_Watermod_LOSCheckIgnore = true, // Set to true if no other Mod_Watermod_LOSCheck____ booleans are set to true
                    Mod_Watermod_LOSCheck_Abovewater = false, // Set to true to have the radar only detect targets if the LOS check starts and ends underwater.
                    Mod_Watermod_LOSCheck_Underwater = false, // Set to true to have the radar only detect targets if the LOS check starts and ends abovewater.
                    Mod_Watermod_LOSCheck_EntersWater = false, // Set to true to have the radar only detect targets if the LOS check starts abovewater and ends underwater.
                    Mod_Watermod_LOSCheck_ExitsWater = false, // Set to true to have the radar only detect targets if the LOS check starts underwater and ends abovewater.

                    PowerRequirementOverride = 10f,
                },
                ["sr_sg_awacs_radar"] = new RadarStat()
                {
                    // If there are multiple definitions with the same subtype ID (like say someone is adjusting stats of another's mod), then the definition with the highest priority will be loaded.
                    // For people making their own mod, its recommended to leave this at zero.
                    // For people MODIFYING other people's mod, its recommended to set this at anything greater than zero.
                    // This effectively allows mod adjuster-like behavior without relying on mod load order, although the entire definition must be copied for it to work properly.
                    //  - those modifying stats can just have the definitions in their place w/o copying any models, sbc files, or sounds to the modified mod.
                    DefinitionPriority = 1,

                    MaxRadiatedPower = 0, // radiated power of the radar, in kilowatts
                    Gain = 0, // gain of the radar, in decibels. Must be above 0
                    Sensitivity = 0, // sensitivity of the radar, in decibels
                    MaxSearchRange = 1, // maximum range radar will return targets, regardless of other settings, in meters

                    ApertureSize = 40, // aperture size of the radar, in meters^2
                    NoiseFilter = 0, // noise filter of the radar, in decibels
                    SignalToNoiseRatio = 1, // ratio of return signal to noise required for the radar to detect targets

                    PositionError = 70, // maximum error of position in any given direction the radar returns in meters
                    VelocityError = 5, // maximum error in the velocity vector in any given direction the radar returns (velocity indicator coming soonTM)

                    CanTargetLock = true, // determines whether or not the radar can lock. Locked targets have no velocity and position error, and will have the radar detected icon turn red from yellow.
                                          // When a guided missile from Vanilla+ fired from a grid with a radar locking another grid lacks a target, Neb. Radar will provide it with the radar's target lock, assuming a valid block exists on the target exists. Should multiple targets be locked by one grid, the missile will choose the closest one.
                    LOSCheckIncludesParentGrid = false,
                    // determines whether the radar's LOS check will include the grid it is on. Useful for radar paneling and such. Subgrids attached to the main grid count as the main grid in this case.
                    StealthMultiplier = 1, // if the target is cloaked via the Stealth Drive mod (https://steamcommunity.com/sharedfiles/filedetails/?id=2805859069), then the target's RCS is multiplied by this when the radar scans.

                    CanDetectAllJumps = true, // Determines if the radar can detect and show any jumps caused by any track visible to the radar.
                    CanDetectLockedJumps = false, // Determines if the radar can detect and show any jumps caused by the tracked locked by the radar.

                    AutoLockTurretTarget = false, // If the radar is a turret and is aiming at a target grid/block, then automatically radar lock whatever grid its firing it.

                    // Watermod LOS interactions. Governs whether or not radar will only detect targets in, out, or between water. They operate on an "or" basis - if one of the set conditions is true the radar will detect target.
                    // Ex: Setting EntersWater and ExitsWater to true will means that the radar will only detect targets opposite of it; underwater if its above and above if its under, rather than nothing at all.
                    Mod_Watermod_LOSCheckIgnore = true, // Set to true if no other Mod_Watermod_LOSCheck____ booleans are set to true
                    Mod_Watermod_LOSCheck_Abovewater = false, // Set to true to have the radar only detect targets if the LOS check starts and ends underwater.
                    Mod_Watermod_LOSCheck_Underwater = false, // Set to true to have the radar only detect targets if the LOS check starts and ends abovewater.
                    Mod_Watermod_LOSCheck_EntersWater = false, // Set to true to have the radar only detect targets if the LOS check starts abovewater and ends underwater.
                    Mod_Watermod_LOSCheck_ExitsWater = false, // Set to true to have the radar only detect targets if the LOS check starts underwater and ends abovewater.

                    PowerRequirementOverride = 50f,
                },
                ["EyeOfSauron"] = new RadarStat()
                {
                    // If there are multiple definitions with the same subtype ID (like say someone is adjusting stats of another's mod), then the definition with the highest priority will be loaded.
                    // For people making their own mod, its recommended to leave this at zero.
                    // For people MODIFYING other people's mod, its recommended to set this at anything greater than zero.
                    // This effectively allows mod adjuster-like behavior without relying on mod load order, although the entire definition must be copied for it to work properly.
                    //  - those modifying stats can just have the definitions in their place w/o copying any models, sbc files, or sounds to the modified mod.
                    DefinitionPriority = 1,

                    MaxRadiatedPower = 1, // radiated power of the radar, in kilowatts
                    Gain = 0, // gain of the radar, in decibels. Must be above 0
                    Sensitivity = 0, // sensitivity of the radar, in decibels
                    MaxSearchRange = 1, // maximum range radar will return targets, regardless of other settings, in meters

                    ApertureSize = 40, // aperture size of the radar, in meters^2
                    NoiseFilter = 0, // noise filter of the radar, in decibels
                    SignalToNoiseRatio = 1, // ratio of return signal to noise required for the radar to detect targets

                    PositionError = 70, // maximum error of position in any given direction the radar returns in meters
                    VelocityError = 5, // maximum error in the velocity vector in any given direction the radar returns (velocity indicator coming soonTM)

                    CanTargetLock = false, // determines whether or not the radar can lock. Locked targets have no velocity and position error, and will have the radar detected icon turn red from yellow.
                                          // When a guided missile from Vanilla+ fired from a grid with a radar locking another grid lacks a target, Neb. Radar will provide it with the radar's target lock, assuming a valid block exists on the target exists. Should multiple targets be locked by one grid, the missile will choose the closest one.
                    LOSCheckIncludesParentGrid = false,
                    // determines whether the radar's LOS check will include the grid it is on. Useful for radar paneling and such. Subgrids attached to the main grid count as the main grid in this case.
                    StealthMultiplier = 1, // if the target is cloaked via the Stealth Drive mod (https://steamcommunity.com/sharedfiles/filedetails/?id=2805859069), then the target's RCS is multiplied by this when the radar scans.

                    CanDetectAllJumps = false, // Determines if the radar can detect and show any jumps caused by any track visible to the radar.
                    CanDetectLockedJumps = false, // Determines if the radar can detect and show any jumps caused by the tracked locked by the radar.

                    AutoLockTurretTarget = false, // If the radar is a turret and is aiming at a target grid/block, then automatically radar lock whatever grid its firing it.

                    // Watermod LOS interactions. Governs whether or not radar will only detect targets in, out, or between water. They operate on an "or" basis - if one of the set conditions is true the radar will detect target.
                    // Ex: Setting EntersWater and ExitsWater to true will means that the radar will only detect targets opposite of it; underwater if its above and above if its under, rather than nothing at all.
                    Mod_Watermod_LOSCheckIgnore = true, // Set to true if no other Mod_Watermod_LOSCheck____ booleans are set to true
                    Mod_Watermod_LOSCheck_Abovewater = false, // Set to true to have the radar only detect targets if the LOS check starts and ends underwater.
                    Mod_Watermod_LOSCheck_Underwater = false, // Set to true to have the radar only detect targets if the LOS check starts and ends abovewater.
                    Mod_Watermod_LOSCheck_EntersWater = false, // Set to true to have the radar only detect targets if the LOS check starts abovewater and ends underwater.
                    Mod_Watermod_LOSCheck_ExitsWater = false, // Set to true to have the radar only detect targets if the LOS check starts underwater and ends abovewater.

                    PowerRequirementOverride = 50f,
                },
            },
            JammerStats = new Dictionary<string, JammerStat>()
            {
                ["Nerd_JammingBlock"] = new JammerStat()
                {
                    // If there are multiple definitions with the same subtype ID (like say someone is adjusting stats of another's mod), then the definition with the highest priority will be loaded.
                    // For people making their own mod, its recommended to leave this at zero.
                    // For people MODIFYING other people's mod, its recommended to set this at anything greater than zero.
                    // This effectively allows mod adjuster-like behavior without relying on mod load order, although the entire definition must be copied for it to work properly.
                    //  - those modifying stats can just have the definitions in their place w/o copying any models, sbc files, or sounds to the modified mod.
                    DefinitionPriority = 1,

                    MaxRadiatedPower = 100, // max radiated power in kilowatts
                    Gain = 30, // gain of the jammer in decibels
                    MaxSearchRange = 300000, // maximum range of the jammer in meters, overriding ALL other stats

                    AreaEffectRatio = 0.4f, // Area effect ratio of the jammer. Best explained in the interaction wiki above.
                                            // Essentually, when a radar is jammed, a cylinder of height =2*(the distance from the jammer) and radius of AreaEffectRatio*length centered on the jammer, with the top and bottom being on the line between the radar and jammer

                    AngleRadians = MathHelperD.ToRadians(25), // angle from the center muzzle of the jammer that ships within are jammed, in radians (keep the MathHelper.ToRadians(value) call if you want to use degrees)
                                                              // why didn't I just make this in degrees - Nerd, 2025
                    LOSCheckIncludesParentGrid = true, // Determines whether the jammer can jam through its own grid.

                    MaxHeat = 0, // maximum heat the jammer can handle before shutting off. Heat will always increase by 1 every tick. Measured in ProprietaryHeatUnit™
                                       // set to -1 to disable
                                       // heat on a GUI somewhere coming soon™
                    HeatDrainPerTick = 0f, // Amount of heat that is dissapated per tick


                    // Below values are only needed if the block is not an interior turret
                    AzimuthSubpartName = "InteriorTurretBase1", // Azimuth subpart for the model. Should be parented by the main model. Do not include the prefix 'subpart_' Not used if the jammer is an interior turret.
                    ElevationSubpartName = "InteriorTurretBase2", // Elevation subpart for the model. Should be parented by the azimuth subpart. Do not include the prefix 'subpart_' Not used if the jammer is an interior turret.
                    MuzzleEmptyName = "muzzle_projectile_01",  // Empty where the jammer cone comes from. Defaults to block + block.Up*0.5 if the jammer is an interior turret.

                    AzimuthSpeed = 25, // Speed in degrees per second to rotate the azimuth subpart. Not used if the jammer is an interior turret.
                    ElevationSpeed = 25, // Speed in degrees per second to rotate the elevation subpart. Not used if the jammer is an interior turret.
                    // -180 to 180 means it can rotate fully. Make sure min < max lmao
                    MinAzimuth = -180, // Angle from grid forward to allow the subpart to go to left. -180 & 180 with MaxAzimuth means unlimited. Not used if the jammer is an interior turret.
                    MaxAzimuth = 180, // Angle from grid forward to allow the subpart to go to right. 180 & -180 with MinAzimuth means unlimited. Not used if the jammer is an interior turret.
                    // -90 to 90 means it can rotate fully. Make sure min < max lmao
                    MinElevation = -45, // Angle in degrees from grid forward to allow the subpart to go to down. Not used if the jammer is an interior turret.
                    MaxElevation = 90, // Angle in degrees from grid forward to allow the subpart to go to up. Not used if the jammer is an interior turret.

                    PowerRequirementOverride = 75f,
                },
                ["sr_sg_t_jammerpod_base"] = new JammerStat()
                {
                    // If there are multiple definitions with the same subtype ID (like say someone is adjusting stats of another's mod), then the definition with the highest priority will be loaded.
                    // For people making their own mod, its recommended to leave this at zero.
                    // For people MODIFYING other people's mod, its recommended to set this at anything greater than zero.
                    // This effectively allows mod adjuster-like behavior without relying on mod load order, although the entire definition must be copied for it to work properly.
                    //  - those modifying stats can just have the definitions in their place w/o copying any models, sbc files, or sounds to the modified mod.
                    DefinitionPriority = 1,

                    MaxRadiatedPower = 0.01f, // max radiated power in kilowatts
                    Gain = 0, // gain of the jammer in decibels
                    MaxSearchRange = 1, // maximum range of the jammer in meters, overriding ALL other stats

                    AreaEffectRatio = 0.4f, // Area effect ratio of the jammer. Best explained in the interaction wiki above.
                                            // Essentually, when a radar is jammed, a cylinder of height =2*(the distance from the jammer) and radius of AreaEffectRatio*length centered on the jammer, with the top and bottom being on the line between the radar and jammer

                    AngleRadians = MathHelperD.ToRadians(1), // angle from the center muzzle of the jammer that ships within are jammed, in radians (keep the MathHelper.ToRadians(value) call if you want to use degrees)
                                                              // why didn't I just make this in degrees - Nerd, 2025
                    LOSCheckIncludesParentGrid = true, // Determines whether the jammer can jam through its own grid.

                    MaxHeat = 90 * 60, // maximum heat the jammer can handle before shutting off. Heat will always increase by 1 every tick. Measured in ProprietaryHeatUnit™
                                       // set to -1 to disable
                                       // heat on a GUI somewhere coming soon™
                    HeatDrainPerTick = 0f, // Amount of heat that is dissapated per tick


                    // Below values are only needed if the block is not an interior turret
                    AzimuthSubpartName = "InteriorTurretBase1", // Azimuth subpart for the model. Should be parented by the main model. Do not include the prefix 'subpart_' Not used if the jammer is an interior turret.
                    ElevationSubpartName = "InteriorTurretBase2", // Elevation subpart for the model. Should be parented by the azimuth subpart. Do not include the prefix 'subpart_' Not used if the jammer is an interior turret.
                    MuzzleEmptyName = "muzzle_projectile_01",  // Empty where the jammer cone comes from. Defaults to block + block.Up*0.5 if the jammer is an interior turret.

                    AzimuthSpeed = 25, // Speed in degrees per second to rotate the azimuth subpart. Not used if the jammer is an interior turret.
                    ElevationSpeed = 25, // Speed in degrees per second to rotate the elevation subpart. Not used if the jammer is an interior turret.
                    // -180 to 180 means it can rotate fully. Make sure min < max lmao
                    MinAzimuth = -5, // Angle from grid forward to allow the subpart to go to left. -180 & 180 with MaxAzimuth means unlimited. Not used if the jammer is an interior turret.
                    MaxAzimuth = 5, // Angle from grid forward to allow the subpart to go to right. 180 & -180 with MinAzimuth means unlimited. Not used if the jammer is an interior turret.
                    // -90 to 90 means it can rotate fully. Make sure min < max lmao
                    MinElevation = -5, // Angle in degrees from grid forward to allow the subpart to go to down. Not used if the jammer is an interior turret.
                    MaxElevation = 5, // Angle in degrees from grid forward to allow the subpart to go to up. Not used if the jammer is an interior turret.

                    PowerRequirementOverride = 50f,
                }
            },

            IFFBlockStats = new Dictionary<string, IFFBlockStat>()
            {
                ["LargeBlockIFFBeacon"] = new IFFBlockStat()
                {
                    // If there are multiple definitions with the same subtype ID (like say someone is adjusting stats of another's mod), then the definition with the highest priority will be loaded.
                    // For people making their own mod, its recommended to leave this at zero.
                    // For people MODIFYING other people's mod, its recommended to set this at anything greater than zero.
                    // This effectively allows mod adjuster-like behavior without relying on mod load order, although the entire definition must be copied for it to work properly.
                    //  - those modifying stats can just have the definitions in their place w/o copying any models, sbc files, or sounds to the modified mod.
                    DefinitionPriority = 1,

                    MaxCharacters = 0, // maximum characters the IFF beacon will use
                    ShowClass = false, // whether or not the IFF beacon name change will completely replace (false) or only add its name after the class name (true)
                },
                ["SmallBlockIFFBeacon"] = new IFFBlockStat()
                {
                    // If there are multiple definitions with the same subtype ID (like say someone is adjusting stats of another's mod), then the definition with the highest priority will be loaded.
                    // For people making their own mod, its recommended to leave this at zero.
                    // For people MODIFYING other people's mod, its recommended to set this at anything greater than zero.
                    // This effectively allows mod adjuster-like behavior without relying on mod load order, although the entire definition must be copied for it to work properly.
                    //  - those modifying stats can just have the definitions in their place w/o copying any models, sbc files, or sounds to the modified mod.
                    DefinitionPriority = 1,

                    MaxCharacters = 0, // maximum characters the IFF beacon will use
                    ShowClass = false, // whether or not the IFF beacon name change will completely replace (false) or only add its name after the class name (true)
                },
            },

            UpgradeBlockStats = new Dictionary<string, UpgradeBlockStat>()
            {
                ["ARYLNX_RAIDER_Epstein_Drive"] = DRIVE_3x3,
                ["ARYLNX_Epstein_Drive"] = DRIVE_3x3,
                ["ARYLNX_MUNR_Epstein_Drive"] = DRIVE_3x3,
                ["ARYLNX_PNDR_Epstein_Drive"] = DRIVE_3x3,

                ["ARYLNX_ROCI_Epstein_Drive"] = DRIVE_5x5,
                ["ARYLNX_DRUMMER_Epstein_Drive"] = DRIVE_5x5,
                ["ARYLYNX_SILVERSMITH_DRIVE"] = new UpgradeBlockStat()
                {
                    DefinitionPriority = 1,
                    // these two are incompatible with eachother.
                    ApplyOnlyWhenFiring = false, // WEAPON BLOCKS ONLY. Makes all of the addons/multipliers apply only when the weapon is firing.
                    ApplyOnlyWhenOn = false, // FUNCTIONAL BLOCKS ONLY. Makes all of the addons/multipliers apply only if the block is functional.

                    PositionalErrorMultiplier = 1, // Positional error multiplier for ALL radars on the grid this is mounted on. Multipliers are calculated BEFORE addons.
                    PositionalErrorAddon = 0, // Positional error addon for ALL radars on the grid this is mounted on. Addons are calculated AFTER multipliers.

                    VelocityErrorMultiplier = 1, // Velocity error multiplier for ALL radars on the grid this is mounted on. Multipliers are calculated BEFORE addons.
                    VelocityErrorAddon = 0, // Velocity error addon for ALL radars on the grid this is mounted on. Addons are calculated AFTER multipliers.

                    NoiseFilterMultiplier = 1, // Noise filter multiplier for ALL radars on the grid this is mounted on. Multipliers are calculated BEFORE addons.
                    NoiseFilterAddon = 0, // Noise filter addon for ALL radars on the grid this is mounted on. Addons are calculated AFTER multipliers.

                    RCSMultiplier = 2f, // RCS multiplier for the grid this is mounted on. Multipliers are calculated BEFORE addons.
                    RCSAddon = 0, //  RCS addon for the grid this is mounted on. Addons are calculated AFTER multipliers.

                    SensitivityMultiplier = 1, // Sensitivity multiplier for ALL radars on the grid this is mounted on. Multipliers are calculated BEFORE addons.
                    SensitivityAddon = 0, // Sensitivity addon for ALL radars on the grid this is mounted on. Addons are calculated AFTER multipliers.
                },

                ["ARYLNX_SCIRCOCCO_Epstein_Drive"] = new UpgradeBlockStat()
                {
                    DefinitionPriority = 1,
                    // these two are incompatible with eachother.
                    ApplyOnlyWhenFiring = false, // WEAPON BLOCKS ONLY. Makes all of the addons/multipliers apply only when the weapon is firing.
                    ApplyOnlyWhenOn = false, // FUNCTIONAL BLOCKS ONLY. Makes all of the addons/multipliers apply only if the block is functional.

                    PositionalErrorMultiplier = 1, // Positional error multiplier for ALL radars on the grid this is mounted on. Multipliers are calculated BEFORE addons.
                    PositionalErrorAddon = 0, // Positional error addon for ALL radars on the grid this is mounted on. Addons are calculated AFTER multipliers.

                    VelocityErrorMultiplier = 1, // Velocity error multiplier for ALL radars on the grid this is mounted on. Multipliers are calculated BEFORE addons.
                    VelocityErrorAddon = 0, // Velocity error addon for ALL radars on the grid this is mounted on. Addons are calculated AFTER multipliers.

                    NoiseFilterMultiplier = 1, // Noise filter multiplier for ALL radars on the grid this is mounted on. Multipliers are calculated BEFORE addons.
                    NoiseFilterAddon = 0, // Noise filter addon for ALL radars on the grid this is mounted on. Addons are calculated AFTER multipliers.

                    RCSMultiplier = 4.3f, // RCS multiplier for the grid this is mounted on. Multipliers are calculated BEFORE addons.
                    RCSAddon = 0, //  RCS addon for the grid this is mounted on. Addons are calculated AFTER multipliers.

                    SensitivityMultiplier = 1, // Sensitivity multiplier for ALL radars on the grid this is mounted on. Multipliers are calculated BEFORE addons.
                    SensitivityAddon = 0, // Sensitivity addon for ALL radars on the grid this is mounted on. Addons are calculated AFTER multipliers.
                },

                ["ARYLNX_Mega_Epstein_Drive"] = new UpgradeBlockStat()
                {
                    DefinitionPriority = 1,
                    // these two are incompatible with eachother.
                    ApplyOnlyWhenFiring = false, // WEAPON BLOCKS ONLY. Makes all of the addons/multipliers apply only when the weapon is firing.
                    ApplyOnlyWhenOn = false, // FUNCTIONAL BLOCKS ONLY. Makes all of the addons/multipliers apply only if the block is functional.

                    PositionalErrorMultiplier = 1, // Positional error multiplier for ALL radars on the grid this is mounted on. Multipliers are calculated BEFORE addons.
                    PositionalErrorAddon = 0, // Positional error addon for ALL radars on the grid this is mounted on. Addons are calculated AFTER multipliers.

                    VelocityErrorMultiplier = 1, // Velocity error multiplier for ALL radars on the grid this is mounted on. Multipliers are calculated BEFORE addons.
                    VelocityErrorAddon = 0, // Velocity error addon for ALL radars on the grid this is mounted on. Addons are calculated AFTER multipliers.

                    NoiseFilterMultiplier = 1, // Noise filter multiplier for ALL radars on the grid this is mounted on. Multipliers are calculated BEFORE addons.
                    NoiseFilterAddon = 0, // Noise filter addon for ALL radars on the grid this is mounted on. Addons are calculated AFTER multipliers.

                    RCSMultiplier = 7f, // RCS multiplier for the grid this is mounted on. Multipliers are calculated BEFORE addons.
                    RCSAddon = 0, //  RCS addon for the grid this is mounted on. Addons are calculated AFTER multipliers.

                    SensitivityMultiplier = 1, // Sensitivity multiplier for ALL radars on the grid this is mounted on. Multipliers are calculated BEFORE addons.
                    SensitivityAddon = 0, // Sensitivity addon for ALL radars on the grid this is mounted on. Addons are calculated AFTER multipliers.
                },
            }
        };

        static UpgradeBlockStat DRIVE_3x3 = new UpgradeBlockStat()
        {
            DefinitionPriority = 1,
            // these two are incompatible with eachother.
            ApplyOnlyWhenFiring = false, // WEAPON BLOCKS ONLY. Makes all of the addons/multipliers apply only when the weapon is firing.
            ApplyOnlyWhenOn = false, // FUNCTIONAL BLOCKS ONLY. Makes all of the addons/multipliers apply only if the block is functional.

            PositionalErrorMultiplier = 1, // Positional error multiplier for ALL radars on the grid this is mounted on. Multipliers are calculated BEFORE addons.
            PositionalErrorAddon = 0, // Positional error addon for ALL radars on the grid this is mounted on. Addons are calculated AFTER multipliers.

            VelocityErrorMultiplier = 1, // Velocity error multiplier for ALL radars on the grid this is mounted on. Multipliers are calculated BEFORE addons.
            VelocityErrorAddon = 0, // Velocity error addon for ALL radars on the grid this is mounted on. Addons are calculated AFTER multipliers.

            NoiseFilterMultiplier = 1, // Noise filter multiplier for ALL radars on the grid this is mounted on. Multipliers are calculated BEFORE addons.
            NoiseFilterAddon = 0, // Noise filter addon for ALL radars on the grid this is mounted on. Addons are calculated AFTER multipliers.

            RCSMultiplier = 1.8f, // RCS multiplier for the grid this is mounted on. Multipliers are calculated BEFORE addons.
            RCSAddon = 0, //  RCS addon for the grid this is mounted on. Addons are calculated AFTER multipliers.

            SensitivityMultiplier = 1, // Sensitivity multiplier for ALL radars on the grid this is mounted on. Multipliers are calculated BEFORE addons.
            SensitivityAddon = 0, // Sensitivity addon for ALL radars on the grid this is mounted on. Addons are calculated AFTER multipliers.
        };

        static UpgradeBlockStat DRIVE_5x5 = new UpgradeBlockStat()
        {
            DefinitionPriority = 1,
            // these two are incompatible with eachother.
            ApplyOnlyWhenFiring = false, // WEAPON BLOCKS ONLY. Makes all of the addons/multipliers apply only when the weapon is firing.
            ApplyOnlyWhenOn = false, // FUNCTIONAL BLOCKS ONLY. Makes all of the addons/multipliers apply only if the block is functional.

            PositionalErrorMultiplier = 1, // Positional error multiplier for ALL radars on the grid this is mounted on. Multipliers are calculated BEFORE addons.
            PositionalErrorAddon = 0, // Positional error addon for ALL radars on the grid this is mounted on. Addons are calculated AFTER multipliers.

            VelocityErrorMultiplier = 1, // Velocity error multiplier for ALL radars on the grid this is mounted on. Multipliers are calculated BEFORE addons.
            VelocityErrorAddon = 0, // Velocity error addon for ALL radars on the grid this is mounted on. Addons are calculated AFTER multipliers.

            NoiseFilterMultiplier = 1, // Noise filter multiplier for ALL radars on the grid this is mounted on. Multipliers are calculated BEFORE addons.
            NoiseFilterAddon = 0, // Noise filter addon for ALL radars on the grid this is mounted on. Addons are calculated AFTER multipliers.

            RCSMultiplier = 2.8f, // RCS multiplier for the grid this is mounted on. Multipliers are calculated BEFORE addons.
            RCSAddon = 0, //  RCS addon for the grid this is mounted on. Addons are calculated AFTER multipliers.

            SensitivityMultiplier = 1, // Sensitivity multiplier for ALL radars on the grid this is mounted on. Multipliers are calculated BEFORE addons.
            SensitivityAddon = 0, // Sensitivity addon for ALL radars on the grid this is mounted on. Addons are calculated AFTER multipliers.
        };
        public override void Init(MyObjectBuilder_SessionComponent sessionComponent)
        {
            byte[] data = MyAPIGateway.Utilities.SerializeToBinary(cfg);
            MyAPIGateway.Utilities.SendModMessage(DefConstants.MessageHandlerId, data);
        }
    }
}
