using System.Collections.Generic;
using VRage.Game;
using VRage.Game.Components;
using NerdRadar.Definitions;
using Sandbox.ModAPI;
using ShipClass = VRage.MyTuple<int, string>;

namespace NerdRadar.ExampleMod
{
    [MySessionComponentDescriptor(MyUpdateOrder.NoUpdate)]
    public class ShipClassifications : MySessionComponentBase
    {
        
        ClassConfig cfg => new ClassConfig()
        {
            // Feel free to change the Example_ShipClassificationStats to something else, along with the namespace (NerdRadar.ExampleMod)
            // Aside from those, do not change anything above this line

            ReplaceClasses = false, // set to true to overwrite ALL previous classes from any other mod loaded before this one
                                    // set to false to add these classes to the existing list
                                    // Weird and unintended behavior may occur when false and adding a class with exactly the same max blockcount as another

            /* Class type priority is as follows:
             - If station, then use station classes
             - If the "largest grid" of the grid group is large grid, use the large grid classes
             - Else, use small grid classes
             where the "largest grid" is determined by the grid of highest value of the (expression blockcount * grid size)
             - this means large grid blockcount counts for 5x the small grid blockcount
             
            Anyways, did I mention subgrids are a pain to deal with code wise?
            */
            LargeGridClasses = new List<ShipClass>() // list of large grid ship classes
            {
                new ShipClass(3750, "Auxiliary Cruiser"), // first number is the MAXIMUM blockcount, the second is the class name.
                new ShipClass(15000, "Fleet Battleship"), // first number is the MAXIMUM blockcount, the second is the class name.
            },
            SmallGridClasses = new List<ShipClass>() // list of small grid ship classes
            {
                new ShipClass(25, "Debris"), // first number is the MAXIMUM blockcount, the second is the class name.
            },
            StationClasses = new List<ShipClass>() // list of station classes, whether large or small grid (yes small grid stations are possible)
            {
                new ShipClass(20000, "City"), // first number is the MAXIMUM blockcount, the second is the class name.
            },
        };
        // Do not modify below this line
        public override void Init(MyObjectBuilder_SessionComponent sessionComponent)
        {
            byte[] data = MyAPIGateway.Utilities.SerializeToBinary(cfg);
            MyAPIGateway.Utilities.SendModMessage(DefConstants.MessageHandlerId, data);
        }
    }
}
