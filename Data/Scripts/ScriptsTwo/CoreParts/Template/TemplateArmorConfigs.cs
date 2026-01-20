using static Scripts.Structure;
using static Scripts.Structure.ArmorDefinition.ArmorType;
namespace Scripts {   
    partial class Parts {
        // Don't edit above this line
        ArmorDefinition WeaponStandard => new ArmorDefinition
        {
            SubtypeIds = new[] {
                "Beskar"
            },
            EnergeticResistance = 1f,
            KineticResistance = 1f,
            Kind = Light,
        };
        ArmorDefinition WeaponCapital => new ArmorDefinition
        {
            SubtypeIds = new[] {
                "Durasteel",
                "DurasteelSlope",
                "DurasteelCorner"
            },
            EnergeticResistance = 1f, //Resistance to Energy damage. 0.5f = 200% damage, 2f = 50% damage
            KineticResistance = 1f, //Resistance to Kinetic damage. Leave these as 1 for no effect
            Kind = Heavy, //Heavy, Light, NonArmor - which ammo damage multipliers to apply
        };
        
    }
}
