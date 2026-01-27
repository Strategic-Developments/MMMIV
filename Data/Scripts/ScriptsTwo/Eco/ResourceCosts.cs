using Sandbox.Definitions;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRage;
using VRage.Game;
using VRage.ObjectBuilders;

namespace Meridian
{
    public class ResourceCosts
    {
        public readonly Dictionary<MyDefinitionId, MyFixedPoint> BaseItemCosts;
        public readonly Dictionary<MyDefinitionId, MyFixedPoint> AllItemCosts;
        public readonly Dictionary<MyDefinitionId, MyFixedPoint> AllBlockCosts;

        public readonly Dictionary<string, double> GasCosts;

        public ResourceCosts()
        {
            var baseItemCosts = new Dictionary<string, double>()
            {
                /* Ingots */
                ["Ingot/Stone"] = 0.01,
                ["Ingot/Iron"] = 0.4,
                ["Ingot/Nickel"] = 3,
                ["Ingot/Silicon"] = 2,
                ["Ingot/Cobalt"] = 8,
                ["Ingot/Magnesium"] = 100,
                ["Ingot/Silver"] = 180,
                ["Ingot/Gold"] = 150,
                /*["Ingot/Platinum"] = 450,*/
                ["Ingot/Uranium"] = 300,

                ["Ore/Ice"] = 0.5,
                ["Ore/Scrap"] = 1,
                ["Ingot/Scrap"] = 1,
                ["Ingot/PrototechScrap"] = 6000,
                ["ConsumableItem/Powerkit"] = 50,
                ["ConsumableItem/Medkit"] = 50,
                ["Package/Package"] = 50000,
                ["ConsumableItem/ClangCola"] = 100,
                ["ConsumableItem/CosmicCoffee"] = 100,
                ["Component/EngineerPlushie"] = 250,
                ["Component/SabiroidPlushie"] = 250,
            };

            BaseItemCosts = new Dictionary<MyDefinitionId, MyFixedPoint>();
            AllItemCosts = new Dictionary<MyDefinitionId, MyFixedPoint>();
            AllBlockCosts = new Dictionary<MyDefinitionId, MyFixedPoint>();

            GasCosts = new Dictionary<string, double>()
            {
                ["Oxygen"] = 0.0045,
                ["Hydrogen"] = 0.00225,
            };

            foreach (var item in baseItemCosts)
            {
                BaseItemCosts.Add(MyDefinitionId.Parse(MyObjectBuilderType.LEGACY_TYPE_PREFIX + item.Key), (MyFixedPoint)item.Value);
                AllItemCosts.Add(MyDefinitionId.Parse(MyObjectBuilderType.LEGACY_TYPE_PREFIX + item.Key), (MyFixedPoint)item.Value);
            }
        }

        public void ChangeAndComputePrices()
        {
            List<MyBlueprintDefinitionBase.Item> items = new List<MyBlueprintDefinitionBase.Item>();

            foreach (var definition in MyDefinitionManager.Static.GetBlueprintDefinitions())
            {
                if (!definition.Enabled || !definition.Public)
                {
                    continue;
                }

                switch (definition.Id.SubtypeName)
                {
                    case "Position0080_NATO_25x184mmMagazine":
                    case "Position0090_AutocannonClip":
                    case "Position0110_MediumCalibreAmmo":
                    case "Position0120_LargeCalibreAmmo":
                    case "Position0130_SmallRailgunAmmo":
                    case "Position0140_LargeRailgunAmmo":
                        definition.Prerequisites = new[]
                        {
                            new MyBlueprintDefinitionBase.Item()
                            {
                                Amount = (MyFixedPoint)Math.Ceiling((float)1),
                                Id = MyDefinitionId.Parse("Component/ZoneChip")
                            }
                        };
                        definition.BaseProductionTimeInSeconds = 99999;
                        definition.Enabled = false;
                        definition.Public = false;
                        continue;
                    default:
                        break;
                }

                if (definition.Id.SubtypeName.Contains("OreToIngot")
                    && !definition.Id.SubtypeName.Contains("Uranium"))
                {
                    definition.Prerequisites = new[]
                        {
                            new MyBlueprintDefinitionBase.Item()
                            {
                                Amount = (MyFixedPoint)Math.Ceiling((float)100000),
                                Id = MyDefinitionId.Parse("Ore/Gold")
                            }
                        };
                    definition.BaseProductionTimeInSeconds = 99999;
                    definition.Enabled = false;
                    definition.Public = false;
                }

                var bpCost = MyFixedPoint.Zero;
                foreach (var item in definition.Prerequisites)
                {
                    MyFixedPoint itemCost = 0;
                    if (!BaseItemCosts.TryGetValue(item.Id, out bpCost))
                    {
                        if (item.Id == MyDefinitionId.Parse("Ingot/Platinum"))
                        {
                            switch (definition.Id.SubtypeName)
                            {
                                case "Position0030_EliteAutoPistol":
                                case "Position0070_UltimateAutomaticRifle":
                                case "Position0090_AdvancedHandHeldLauncher":
                                case "Position0040_AngleGrinder4":
                                case "Position0080_HandDrill4":
                                case "Position0120_Welder4":
                                case "Position0100_Missile200mm":
                                case "PrototechPropulsionUnit":
                                case "PrototechCircuitry":
                                case "PrototechCoolingUnit":
                                case "ThrustComponent":
                                    bpCost += 450 * item.Amount;
                                    break;
                                default:
                                    items.Add(item);
                                    break;
                            }
                        }
                        else
                        {
                            items.Add(item);
                        }
                    }
                    else
                    {
                        bpCost += itemCost * item.Amount;
                    }
                }
                if (bpCost > 0)
                {

                    items.Add(new MyBlueprintDefinitionBase.Item()
                    {
                        Amount = bpCost,
                        Id = MyDefinitionId.Parse("PhysicalObject/SpaceCredit")
                    });
                    definition.BaseProductionTimeInSeconds = 1 / 60f;
                }

                definition.Prerequisites = items.ToArray();
                items.Clear();

                if (definition?.Results != null && definition.Results.Length > 0)
                {
                    MyFixedPoint other;
                    if (AllItemCosts.TryGetValue(definition.Results[0].Id, out other))
                    {
                        if (other > 0 && bpCost > 0)
                            AllItemCosts[definition.Results[0].Id] = other > bpCost ? bpCost : other;
                        else if (bpCost > 0)
                            AllItemCosts[definition.Results[0].Id] = bpCost;
                    }
                    else
                    {
                        AllItemCosts.Add(definition.Results[0].Id, bpCost);
                    }
                }
            }

            // KILL MAREK ROSA
            foreach (var item in MyDefinitionManager.Static.GetAllDefinitions())
            {
                if (item is MyCubeBlockDefinition)
                {
                    var def = item as MyCubeBlockDefinition;

                    MyFixedPoint totalCost = 0;
                    foreach (var component in def.Components)
                    {
                        MyFixedPoint cost = 0;
                        if (AllItemCosts.TryGetValue(component.Definition.Id, out cost))
                        {
                            totalCost += cost * component.Count;
                        }
                    }
                    AllBlockCosts.Add(def.Id, totalCost);

                    switch (def.Id.ToString().Replace(MyObjectBuilderType.LEGACY_TYPE_PREFIX, ""))
                    {
                        case "Thrust/LargeBlockLargeHydrogenThrust":
                        case "Thrust/LargeBlockSmallHydrogenThrust":
                        case "Thrust/LargeBlockLargeHydrogenThrustIndustrial":
                        case "Thrust/LargeBlockSmallHydrogenThrustIndustrial":
                        case "Thrust/LargeBlockLargeHydrogenThrustReskin":
                        case "Thrust/LargeBlockSmallHydrogenThrustReskin":

                        case "Thrust/LargeBlockLargeThrust":
                        case "Thrust/LargeBlockSmallThrust":
                        case "Thrust/LargeBlockLargeThrustSciFi":
                        case "Thrust/LargeBlockSmallThrustSciFi":
                        case "Thrust/LargeBlockLargeModularThruster":
                        case "Thrust/LargeBlockSmallModularThruster":

                        case "Thrust/LargeBlockLargeAtmosphericThrust":
                        case "Thrust/LargeBlockSmallAtmosphericThrust":
                        case "Thrust/LargeBlockLargeFlatAtmosphericThrust":
                        case "Thrust/LargeBlockLargeFlatAtmosphericThrustDShape":
                        case "Thrust/LargeBlockSmallFlatAtmosphericThrust":
                        case "Thrust/LargeBlockSmallFlatAtmosphericThrustDShape":
                        case "Thrust/LargeBlockLargeAtmosphericThrustSciFi":
                        case "Thrust/LargeBlockSmallAtmosphericThrustSciFi":

                        case "Thrust/SmallBlockLargeThrust":
                        case "Thrust/SmallBlockSmallThrust":
                        case "Thrust/SmallBlockLargeThrustSciFi":
                        case "Thrust/SmallBlockSmallThrustSciFi":
                        case "Thrust/SmallBlockLargeModularThruster":
                        case "Thrust/SmallBlockSmallModularThruster":

                        case "Thrust/SmallBlockLargeHydrogenThrust":
                        case "Thrust/SmallBlockSmallHydrogenThrust":
                        case "Thrust/SmallBlockLargeHydrogenThrustIndustrial":
                        case "Thrust/SmallBlockSmallHydrogenThrustIndustrial":
                        case "Thrust/SmallBlockLargeHydrogenThrustReskin":
                        case "Thrust/SmallBlockSmallHydrogenThrustReskin":

                        case "Thrust/SmallBlockLargeAtmosphericThrust":
                        case "Thrust/SmallBlockSmallAtmosphericThrust":
                        case "Thrust/SmallBlockLargeFlatAtmosphericThrust":
                        case "Thrust/SmallBlockLargeFlatAtmosphericThrustDShape":
                        case "Thrust/SmallBlockSmallFlatAtmosphericThrust":
                        case "Thrust/SmallBlockSmallFlatAtmosphericThrustDShape":
                        case "Thrust/SmallBlockLargeAtmosphericThrustSciFi":
                        case "Thrust/SmallBlockSmallAtmosphericThrustSciFi":

                        case "Thrust/LargeBlockPrototechThruster":
                        case "Thrust/SmallBlockPrototechThruster":
                            var thrustDef = item as MyThrustDefinition;
                            thrustDef.Enabled = false;
                            thrustDef.Public = false;
                            thrustDef.ForceMagnitude = 0.001f;
                            break;

                        case "LargeGatlingTurret/(null)":
                        case "LargeGatlingTurret/LargeGatlingTurretReskin":
                        case "LargeMissileTurret/(null)":
                        case "LargeMissileTurret/LargeMissileTurretReskin":
                        case "LargeMissileTurret/LargeCalibreTurret":
                        case "LargeMissileTurret/LargeBlockMediumCalibreTurret":
                        case "InteriorTurret/LargeInteriorTurret":

                        case "LargeGatlingTurret/SmallGatlingTurret":
                        case "LargeGatlingTurret/SmallGatlingTurretReskin":
                        case "LargeMissileTurret/SmallMissileTurret":
                        case "LargeMissileTurret/SmallMissileTurretReskin":
                        case "LargeMissileTurret/SmallBlockMediumCalibreTurret":
                        case "LargeMissileTurret/AutoCannonTurret":

                        case "SmallMissileLauncher/LargeMissileLauncher":
                        case "ConveyorSorter/LargeRailgun":
                        case "SmallMissileLauncher/LargeBlockLargeCalibreGun":
                        case "SmallMissileLauncher/LargeFlareLauncher":
                        case "SmallMissileLauncher/(null)":
                        case "SmallMissileLauncher/SmallMissileLauncherWarfare2":
                        case "SmallMissileLauncherReload/SmallRocketLauncherReload":
                        case "SmallGatlingGun/(null)":
                        case "SmallGatlingGun/SmallGatlingGunWarfare2":
                        case "SmallGatlingGun/SmallBlockAutocannon":
                        case "ConveyorSorter/SmallRailgun":
                        case "SmallMissileLauncherReload/SmallBlockMediumCalibreGun":
                        case "SmallMissileLauncher/SmallFlareLauncher":

                        case "TurretControlBlock/LargeTurretControlBlock":
                        case "TurretControlBlock/SmallTurretControlBlock":
                            def.Enabled = false;
                            def.Public = false;
                            break;
                        case "JumpDrive/SmallPrototechJumpDrive":
                        case "JumpDrive/LargePrototechJumpDrive":
                            ((MyJumpDriveDefinition)def).MaxJumpDistance = 1;
                            def.Enabled = false;
                            def.Public = false;
                            break;
                        case "Refinery/SmallPrototechRefinery":
                        case "Refinery/LargePrototechRefinery":
                        case "Refinery/Blast Furnace":
                            ((MyRefineryDefinition)def).RefineSpeed = 0.0001f;
                            def.Enabled = false;
                            def.Public = false;
                            break;


                        case "ShipGrinder/LargeShipGrinder":
                        case "ShipGrinder/LargeShipGrinderReskin":
                            ((MyShipGrinderDefinition)def).SensorRadius = 0.0001f;
                            def.Enabled = false;
                            def.Public = false;
                            break;
                        case "ShipWelder/LargeShipWelder":
                        case "ShipWelder/LargeShipWelderReskin":
                            ((MyShipWelderDefinition)def).SensorRadius = 0.0001f;
                            def.Enabled = false;
                            def.Public = false;
                            break;
                        case "Drill/LargeBlockDrill":
                        case "Drill/LargeBlockDrillReskin":
                        case "Drill/LargeBlockPrototechDrill":
                            var drillDef = def as MyShipDrillDefinition;
                            drillDef.SensorRadius = 0.0001f;
                            drillDef.SensorOffset = -5f;
                            drillDef.CutOutOffset = 0.0001f;
                            drillDef.CutOutRadius = 0.0001f;
                            drillDef.Speed = 0.0001f;
                            drillDef.DiscardingMultiplier = 0.0001f;
                            def.Enabled = false;
                            def.Public = false;
                            break;
                        default:
                            break;
                    }
                }
            }
        }
    }
}