using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NerdRadar
{
    [Serializable]
    public class UserConfig
    {
        public float BountyPayoutMultiplier;
        public UserConfig()
        {
        }

        public UserConfig(float bountyMult)
        {
            BountyPayoutMultiplier = bountyMult;
        }
    }
}
