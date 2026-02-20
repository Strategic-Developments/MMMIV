using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRage.Game;
using VRage.ObjectBuilders;

namespace Meridian.Utilities
{
    [Serializable]
    public class UserConfig
    {
        public float BountyPayoutMultiplier;
        public float NPCPayoutMultiplier;
        public string NPCFactionStr;
        public List<RewardItemSerializable> NPCRewardItems;
        public UserConfig()
        {
        }

        public UserConfig(float bountyMult, string nPCFactionStr, List<RewardItemSerializable> rewardItems, float nPCPayoutMultiplier)
        {
            BountyPayoutMultiplier = bountyMult;
            NPCFactionStr = nPCFactionStr;
            NPCPayoutMultiplier = nPCPayoutMultiplier;
            NPCRewardItems = rewardItems;
        }
    }
    [Serializable]
    public struct RewardItemSerializable
    {
        public string Id;
        public int Amount;
        public RewardItemSerializable(string id, int amount)
        {
            Id = id;
            Amount = amount;
        }
    }
    public struct RewardItem
    {
        public MyDefinitionId Id;
        public int Amount;
        public RewardItem(MyDefinitionId id, int amount)
        {
            Id = id;
            Amount = amount;
        }
        public RewardItem(RewardItemSerializable item)
        {
            Id = MyDefinitionId.Parse(MyObjectBuilderType.LEGACY_TYPE_PREFIX + item.Id);
            Amount = item.Amount;
        }
    }
}
