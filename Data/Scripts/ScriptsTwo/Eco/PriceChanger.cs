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
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.ModAPI;
using VRage.ObjectBuilders;
using VRage;
using Meridian.Utilities;
using Sandbox.Game.Entities.Cube;
using VRageMath;
using Sandbox.Engine.Utils;

namespace Meridian.Economy
{
    [MySessionComponentDescriptor(MyUpdateOrder.NoUpdate)]
    internal class PriceChanger : MySessionComponentBase
    {
        public static PriceChanger Instance;
        public ResourceCosts Costs;
        private List<IMyCubeGrid> GridDumpList;
        public override void LoadData()
        {
            ItemUtils.Init();
            Costs = new ResourceCosts();
            Instance = this;
            GridDumpList = new List<IMyCubeGrid>();

            Costs.ChangeAndComputePrices();
        }
        public override void BeforeStart()
        {
            ChatCommands.AddChatCommand("/getgridprice", ChatCommand_GetPrice);
        }

        
        protected override void UnloadData()
        {
            ItemUtils.Unload();
            
            Costs = null;
            GridDumpList = null;

            Instance = null;
        }
        public void ChatCommand_GetPrice(ulong SenderId, string message)
        {
            LineD line = GetRaycastLine();
            IMyCubeGrid grid = GetRaycastedGrid(ref line);

            if (grid == null)
            {
                ChatCommands.ShowMessage("Error: no grid found.");
                return;
            }

            var price = GetGridPrice(grid);

            var priceB = (MyFixedPoint)Math.Ceiling((double)price.Item1);
            var priceI = (MyFixedPoint)Math.Ceiling((double)price.Item2);
            var priceG = (MyFixedPoint)Math.Ceiling((double)price.Item3);

            ChatCommands.ShowMessage($"Grid {grid.CustomName} is {priceB + priceI + priceG} SC ({priceB} in blocks, {priceI} in items, {priceG} in gasses)");
        }
        public LineD GetRaycastLine()
        {
            if (MyAPIGateway.Session.CameraController is MySpectatorCameraController)
            {
                MatrixD matrix = MySpectator.Static.Orientation;

                return new LineD(MySpectator.Static.Position, MySpectator.Static.Position + matrix.Forward * 300);
            }
            Vector3D forward = MyAPIGateway.Session.Camera.WorldMatrix.Forward;
            Vector3D eyePosition = MyAPIGateway.Session.Player.Character.PositionComp.GetPosition() + MyAPIGateway.Session.Player.Character.WorldMatrix.Up * 1.8;
            return new LineD(eyePosition, eyePosition + forward * 300);
        }

        public IMyCubeGrid GetRaycastedGrid(ref LineD rayLine)
        {
            List<IHitInfo> HitEntities = new List<IHitInfo>();
            MyAPIGateway.Physics.CastRay(rayLine.To, rayLine.From, HitEntities);

            IMyCubeGrid closestGrid = null;
            double closestFraction = double.MaxValue;
            foreach (var hitEntity in HitEntities)
            {
                if (hitEntity.HitEntity is IMyCubeGrid)
                {
                    if (hitEntity.Fraction < closestFraction)
                    {
                        closestFraction = hitEntity.Fraction;
                        closestGrid = hitEntity.HitEntity as IMyCubeGrid;
                    }
                }
            }
            return closestGrid;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="grid"></param>
        /// <returns>blockValue, itemValue, gridValue</returns>
        public MyTuple<MyFixedPoint, MyFixedPoint, MyFixedPoint> GetGridPrice(IMyCubeGrid grid)
        {
            MyFixedPoint blockValue = 0, invValue = 0, gasValue = 0;

            grid.GetGridGroup(GridLinkTypeEnum.Mechanical).GetGrids(GridDumpList);

            foreach (var g in GridDumpList)
            {
                var p = GetShipCostNoSubgrids(g);

                blockValue += p.Item1;
                invValue += p.Item2;
                gasValue += p.Item3;
            }

            GridDumpList.Clear();

            return new MyTuple<MyFixedPoint, MyFixedPoint, MyFixedPoint>(blockValue, invValue, gasValue);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="grid"></param>
        /// <returns>blockValue, itemValue, gridValue</returns>
        public MyTuple<MyFixedPoint, MyFixedPoint, MyFixedPoint> GetShipCostNoSubgrids(IMyCubeGrid grid)
        {
            MyFixedPoint blockValue = 0, invValue = 0, gasValue = 0;
            grid.GetBlocks(null, (block) =>
            {
                float integrity = 0;

                if (!block.IsFullIntegrity)
                {
                    foreach (var component in ((MyCubeBlockDefinition)block.BlockDefinition).Components)
                    {
                        float componentIntegrity = component.Definition.MaxIntegrity * component.Count;

                        if (integrity + componentIntegrity <= block.BuildIntegrity)
                        {
                            integrity += componentIntegrity;
                        }
                        else
                        {
                            float remainingIntegrity = block.BuildIntegrity - integrity;

                            int count = (int)Math.Ceiling(remainingIntegrity / component.Definition.MaxIntegrity);
                            MyFixedPoint individualCost2;
                            if (!Costs.AllItemCosts.TryGetValue(component.Definition.Id, out individualCost2) || individualCost2 == -1)
                            {
                                continue;
                            }

                            blockValue += individualCost2 * count;
                            break;
                        }
                    }
                }
                else
                {
                    MyFixedPoint individualBlockValue = 0;
                    if (Costs.AllBlockCosts.TryGetValue(block.BlockDefinition.Id, out individualBlockValue))
                    {
                        blockValue += individualBlockValue;
                    }
                }



                if (block.FatBlock != null && block.FatBlock.InventoryCount > 0)
                {
                    // sentenced to another file because using issues
                    invValue += ItemUtils.GetInventoryCosts(block);
                }

                if (block.FatBlock != null && block.FatBlock is IMyGasTank)
                {
                    IMyGasTank tank = block.FatBlock as IMyGasTank;
                    MyGasTankDefinition def = ((MyGasTankDefinition)block.BlockDefinition);

                    float Amount = (float)(def.Capacity * tank.FilledRatio);

                    MyFixedPoint price = (MyFixedPoint)(Costs.GasCosts.GetValueOrDefault(def.StoredGasId.SubtypeName, 0) * Amount);
                    gasValue += price;
                }

                return false;
            });
            return new MyTuple<MyFixedPoint, MyFixedPoint, MyFixedPoint>(blockValue, invValue, gasValue);
        }
    }
}
