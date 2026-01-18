using static Scripts.Structure;
using static Scripts.Structure.ArmorDefinition.ArmorType;
using static Scripts.Structure.WeaponDefinition.TargetingDef.BlockTypes;
using System.Linq;
using System.Collections.Generic;
using VRageMath;
using Sandbox.ModAPI;
using Sandbox.Definitions;
using VRage.Game;

namespace Scripts
{
    partial class Parts
    {
        internal Parts()
        {
            var Weapons = new WeaponDefinition[]
            {
                // PD

                // Cannons
                mss_lg_t_horus,

                // Railguns
                mss_lg_t_apophis,
                mss_lg_t_amunra,

                // Missiles
                mss_lg_f_anubis
            };
            PartDefinitions(
                CompileWeapons(Weapons)
                );

            ArmorDefinitions(
                Compile
                (
                    ToEnumerable(
                        CreateFromWeapons(Weapons, NonArmor, 0.25f, 1f)
                    )
                    //CreateThrusterDefinitions()
                ).ToArray()
                );

            SupportDefinitions();
            UpgradeDefinitions();

            Definitions = null;
        }
    }
}
