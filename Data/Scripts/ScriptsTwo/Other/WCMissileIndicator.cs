using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using CoreSystems.Api;
using Sandbox.Definitions;
using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI;
using VRage;
using VRage.Collections;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.Entity;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRage.ObjectBuilders;
using VRage.Utils;
using VRageMath;
namespace Scripts
{
    [MySessionComponentDescriptor(MyUpdateOrder.BeforeSimulation)]
    public class WCMissileIndicator : MySessionComponentBase
    {
        public static WCMissileIndicator I;
        public WcApi WcApi;
        public bool HasWeaponcore;
        public override void LoadData()
        {
            I = this;
            WcApi = new WcApi();

            HasWeaponcore = false;
            foreach (var mod in Session.Mods)
            {
                if (mod.PublishedFileId == 3154371364)
                    HasWeaponcore = true;
            }
        }
        public override void Init(MyObjectBuilder_SessionComponent sessionComponent)
        {
            if (HasWeaponcore)
            {
                WcApi.Load();
            }
        }
        public override void UpdateBeforeSimulation()
        {
            if (!HasWeaponcore)
            {
                return;
            }
            if (!MyAPIGateway.Utilities.IsDedicated && WcApi.IsReady)
            {
                IMyCockpit cockpit = MyAPIGateway.Session?.Player?.Character?.Parent as IMyCockpit;
                IMyCubeGrid controlledGrid = cockpit?.CubeGrid;

                if (controlledGrid != null)
                {
                    var numProjectiles = WcApi.GetProjectilesLockedOn((MyEntity)controlledGrid).Item2;
                    if (numProjectiles > 0)
                    {
                        MyAPIGateway.Utilities.ShowNotification($"{numProjectiles} missiles incoming!", 17, "Red");
                    }
                }
            }
        }

        protected override void UnloadData()
        {
            if (!HasWeaponcore)
            {
                return;
            }
            WcApi.Unload();
            I = null;
        }
    }
}

