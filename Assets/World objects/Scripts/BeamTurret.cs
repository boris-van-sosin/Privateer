using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BeamTurret : GeneralBeamTurret
{
    IEnumerator HandleBeam()
    {
        yield return new WaitForEndOfFrame();
        _beamRenderer.enabled = true;
        Warhead w = ObjectFactory.CreateWarhead(ObjectFactory.WeaponType.Lance, TurretSize);
        DoBeamHit(w);
        yield return new WaitForSeconds(BeamDuration);
        _beamRenderer.enabled = false;
        yield return null;
    }

    protected override void FireInner(Vector3 firingVector)
    {
        base.FireInner(firingVector);
        _firingVector = firingVector;
        _beamOrigin = Muzzles[_nextBarrel].position;
        StartCoroutine(HandleBeam());
    }

    public float BeamDuration;
}
