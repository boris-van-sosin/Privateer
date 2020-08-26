using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BeamTurret : GeneralBeamTurret
{
    public override void Init(string turretSlotType)
    {
        base.Init(turretSlotType);
        _beamDurationWait = new WaitForSeconds(BeamDuration);
        _warheads[0] = ObjectFactory.CreateWarhead(TurretWeaponType, TurretWeaponSize);
    }

    IEnumerator HandleBeam()
    {
        yield return _endOfFrameWait;
        _beamRenderer.enabled = true;
        DoBeamHit(_warheads[0]);
        yield return new WaitForSeconds(BeamDuration);
        _beamRenderer.enabled = false;
        yield return null;
    }

    protected override void FireInner(Vector3 firingVector, int barrelIdx)
    {        _firingVector = firingVector;
        _beamOrigin = Muzzles[barrelIdx].position;
        StartCoroutine(HandleBeam());
    }

    public override bool IsTurretModCombatible(TurretMod m)
    {
        switch (m)
        {
            case TurretMod.None:
            case TurretMod.TractorBeam:
            case TurretMod.ImprovedCapacitors:
                return true;
            case TurretMod.FastAutoloader:
            case TurretMod.Harpax:
            case TurretMod.Accelerator:
            case TurretMod.AdvancedTargeting:
            default:
                return false;
        }
    }

    protected override void FireGrapplingToolInner(Vector3 firingVector, int barrelIdx)
    {
        // Not implemented yet
    }

    public override ObjectFactory.WeaponBehaviorType BehaviorType => ObjectFactory.WeaponBehaviorType.Beam;

    public float BeamDuration;

    private WaitForSeconds _beamDurationWait;
}
