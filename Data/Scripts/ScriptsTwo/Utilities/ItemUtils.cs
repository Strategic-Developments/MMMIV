using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRage;
using VRage.Game.ModAPI.Ingame;
using VRage.Game.ModAPI;
using Meridian.Economy;

namespace Meridian.Utilities
{
    public static class ItemUtils
    {
        public static List<MyInventoryItem> Items;
        public static void Init()
        {
            Items = new List<MyInventoryItem>();
        }
        public static void Unload()
        {
            Items = null;
        }
        
        public static MyFixedPoint GetInventoryCosts(VRage.Game.ModAPI.IMySlimBlock block)
        {
            MyFixedPoint retval = new MyFixedPoint();
            
            for (int i = 0; i < block.FatBlock.InventoryCount; i++)
            {
                block.FatBlock.GetInventory(i).GetItems(Items);

                foreach (var item in Items)
                {
                    VRage.Game.ModAPI.Ingame.IMyInventoryItem it = block.FatBlock.GetInventory(i).GetItemByID(item.ItemId);

                    MyFixedPoint individualCost;
                    if (!PriceChanger.Instance.Costs.AllItemCosts.TryGetValue(it.GetDefinitionId(), out individualCost) || individualCost == -1)
                    {
                        continue;
                    }

                    retval += individualCost * item.Amount;
                }

                Items.Clear();
            }
            return retval;
        }
    }
}
