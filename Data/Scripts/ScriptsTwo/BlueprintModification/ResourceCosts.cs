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
            ["Ingot/Stone"] = 1,
            ["Ingot/Iron"] = 40,
            ["Ingot/Nickel"] = 300,
            ["Ingot/Silicon"] = 200,
            ["Ingot/Cobalt"] = 800,
            ["Ingot/Magnesium"] = 10000,
            ["Ingot/Silver"] = 1800,
            ["Ingot/Gold"] = 15000,
            ["Ingot/Platinum"] = 45000,
            ["Ingot/Uranium"] = 30000,

            ["Ore/Ice"] = 50,
            ["Ore/Scrap"] = 1,
            ["Ingot/Scrap"] = 1,
            ["Ingot/PrototechScrap"] = 600000,
            ["PhysicalObject/SpaceCredit"] = 100,
            ["ConsumableItem/Powerkit"] = 5000,
            ["ConsumableItem/Medkit"] = 5000,
            ["Package/Package"] = 5000000,
            ["ConsumableItem/ClangCola"] = 10000,
            ["ConsumableItem/CosmicCoffee"] = 10000,
            ["Component/EngineerPlushie"] = 25000,
            ["Component/SabiroidPlushie"] = 25000,
        };

        public static Dictionary<MyDefinitionId, MyFixedPoint> AllItemCosts = new Dictionary<MyDefinitionId, MyFixedPoint>();
    }
}