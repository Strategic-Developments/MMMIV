using System.Collections.Generic;
using static Scripts.Structure.WeaponDefinition;
using static Scripts.Structure.WeaponDefinition.AnimationDef;
using static Scripts.Structure.WeaponDefinition.AnimationDef.PartAnimationSetDef.EventTriggers;
using static Scripts.Structure.WeaponDefinition.AnimationDef.RelMove.MoveType;
using static Scripts.Structure.WeaponDefinition.AnimationDef.RelMove;
namespace Scripts
{ // Don't edit above this line
    partial class Parts
    {
        private AnimationDef mss_lg_f_jackal_anim => new AnimationDef
        {
            EventParticles = new Dictionary<PartAnimationSetDef.EventTriggers, EventParticle[]>
            {
                [PreFire] = new[]{ //This particle fires in the Prefire state, during the 10 second windup the gauss cannon has.
                                   //Valid options include Firing, Reloading, Overheated, Tracking, On, Off, BurstReload, OutOfAmmo, PreFire.
                       new EventParticle
                       {
                           EmptyNames = Names("muzzle_missile_1",
                                "muzzle_missile_2",
                                "muzzle_missile_3",
                                "muzzle_missile_4",
                                "muzzle_missile_5",
                                "muzzle_missile_6",
                                "muzzle_missile_7",
                                "muzzle_missile_8"), //If you want an effect on your own dummy
                           MuzzleNames = Names("muzzle_missile_1",
                                "muzzle_missile_2",
                                "muzzle_missile_3",
                                "muzzle_missile_4",
                                "muzzle_missile_5",
                                "muzzle_missile_6",
                                "muzzle_missile_7",
                                "muzzle_missile_8"), //If you want an effect on the muzzle
                           StartDelay = 0, //ticks 60 = 1 second, delay until particle starts.
                           LoopDelay = 0, //ticks 60 = 1 second
                           ForceStop = false,
                           Particle = new ParticleDef
                           {
                               Name = "mss_missile_prefire_smoke", //Particle subtypeID
                               Color = Color(red: 1, green: 1, blue: 1, alpha: 1), //This is redundant as recolouring is no longer supported.
                               Offset = Vector(0,0,-3f),
                               Extras = new ParticleOptionDef //do your particle colours in your particle file instead.
                               {
                                   Loop = true, //Should match your particle definition.
                                   Restart = false,
                                   MaxDistance = 1000, //meters
                                   MaxDuration = 180, //ticks 60 = 1 second
                                   Scale = 1f, //How chunky the particle is.
                               }
                           }
                       },
                   },
            },
            //These are the animation sets the weapon uses in various states

        };
    }
}
