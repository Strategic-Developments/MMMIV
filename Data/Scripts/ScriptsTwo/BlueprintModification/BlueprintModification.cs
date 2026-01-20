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
        }
    }
}
