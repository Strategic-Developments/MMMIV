@BlockID "Erebus_LargeBeamDrill"
@Version 2
@Author AryxCami

#--- Declarations
using drill_particle as Emitter("emitter")

var bIsActive = false
var bIsDrilling = false

func StartBeam() {
	if (bIsActive == false)
	{
		drill_particle.playParticle("Erebus_LargeBeamDrill_Beam", 1, 1)
	}
	bIsActive = true
}

func StopBeam() {
    drill_particle.stopParticle()
    bIsActive = false
}

#--- Actions
action Shiptool() {
    activated(bIsDrilling) {
		if (bIsDrilling == true) {
			StartBeam();		
		}
		if (bIsDrilling == false) {
			StopBeam();		
		}		
    }
}
