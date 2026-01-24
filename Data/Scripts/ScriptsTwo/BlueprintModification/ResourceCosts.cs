using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRage;
using VRage.Game;

namespace BlueprintMod
{
    public class ResourceCosts
    {
        public static readonly Dictionary<string, double> BaseItemCosts = new Dictionary<string, double>()
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
            /*["Ingot/Platinum"] = 45000,*/
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

        public static Dictionary<MyDefinitionId, MyFixedPoint> AllItemCosts = new Dictionary<MyDefinitionId, MyFixedPoint>();
    }
}