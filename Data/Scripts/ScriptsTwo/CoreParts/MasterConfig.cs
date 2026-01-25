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
                mss_lg_t_flail,
                mss_lg_t_crook,

                // Cannons
                mss_lg_t_horus,
                mss_lg_t_bastet,

                // Railguns
                mss_lg_t_apophis,
                mss_lg_t_amunra,
                mss_lg_f_tyet,
                mss_lg_f_ennead,

                // Missiles
                mss_lg_f_anubis,
                mss_lg_f_jackal,
                mss_lg_f_nepthys,

                // JumpDisruptors
                mss_lg_t_thoth
            };
            PartDefinitions(
                CompileWeapons(Weapons)
                );

            ArmorDefinitions(
                Compile
                (
                    ToEnumerable(
                        CreateFromWeapons(Weapons, NonArmor, 4f, 1f)
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
