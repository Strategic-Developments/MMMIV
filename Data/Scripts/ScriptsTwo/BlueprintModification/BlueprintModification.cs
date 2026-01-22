using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Sandbox.Definitions;
using Sandbox.Game;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using VRage.Game.ModAPI.Interfaces;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.Entity;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRage.Utils;
using VRageMath;
using Sandbox.Game.EntityComponents;
using Sandbox.Common.ObjectBuilders;
using VRage.ObjectBuilders;
using VRage.Game.Models;
using VRage.Render.Particles;
using System.Linq.Expressions;
using System.IO;
using Sandbox.ModAPI.Interfaces;
using Sandbox.Game.Weapons;
using VRage;
using VRage.Collections;
using VRage.Voxels;
using ProtoBuf;
using System.Collections.Concurrent;
using VRage.Serialization;
using Sandbox.Engine.Physics;
using Sandbox.Game.GameSystems;
using System.Data;
using AmountBpPair = VRage.MyTuple<VRage.MyFixedPoint, Sandbox.Definitions.MyBlueprintDefinitionBase>;
using Sandbox.Game.Entities.Cube;

namespace BlueprintMod
{
    [MySessionComponentDescriptor(MyUpdateOrder.NoUpdate)]
    internal class BlueprintModification : MySessionComponentBase
    {
        public override void LoadData()
        {
            foreach (var item in ResourceCosts.BaseItemCosts)
            {
                ResourceCosts.AllItemCosts.Add(MyDefinitionId.Parse(MyObjectBuilderType.LEGACY_TYPE_PREFIX + item.Key), (MyFixedPoint)item.Value);
            }
            List<MyBlueprintDefinitionBase.Item> items = new List<MyBlueprintDefinitionBase.Item>();

            foreach (var definition in MyDefinitionManager.Static.GetBlueprintDefinitions())
            {
                if (!definition.Enabled || !definition.Public || definition.Results.Length > 1)
                {
                    continue;
                }

                var bpCost = MyFixedPoint.Zero;
                foreach (var item in definition.Prerequisites)
                {
                    MyFixedPoint itemCost = 0;
                    if (!ResourceCosts.AllItemCosts.TryGetValue(item.Id, out bpCost))
                    {
                        items.Add(item);
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
                        Amount = (MyFixedPoint)Math.Ceiling((float)bpCost),
                        Id = MyDefinitionId.Parse("PhysicalObject/SpaceCredit")
                    });
                }

                definition.Prerequisites = items.ToArray();
                items.Clear();

                definition.BaseProductionTimeInSeconds = 1 / 60f;
            }
            
            // KILL MAREK ROSA
            foreach (var item in MyDefinitionManager.Static.GetAllDefinitions())
            {
                if (item is MyCubeBlockDefinition)
                {
                    var def = item as MyCubeBlockDefinition;
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

                        case "JumpDrive/SmallPrototechJumpDrive":
                        case "JumpDrive/LargePrototechJumpDrive":
                        case "Refinery/SmallPrototechRefinery":
                        case "Refinery/LargePrototechRefinery":

                        case "TurretControlBlock/LargeTurretControlBlock":
                        case "TurretControlBlock/SmallTurretControlBlock":
                            if (def is MyJumpDriveDefinition)
                                ((MyJumpDriveDefinition)def).MaxJumpDistance = 1;
                            else if (def is MyRefineryDefinition)
                                ((MyRefineryDefinition)def).RefineSpeed = 0.0001f;

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
