using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BeamTurret : TurretBase
{
    protected override void Awake()
    {
        base.Awake();
        _beamRenderer = GetComponentInChildren<LineRenderer>();
        _beamRenderer.positionCount = 2;
        _beamRenderer.useWorldSpace = true;
    }

    IEnumerator HandleBeam()
    {
        _beamRenderer.enabled = true;
        yield return new WaitForSeconds(BeamDuration);
        _beamRenderer.enabled = false;
        yield return null;
    }

    protected override void FireInner(Vector3 firingVector)
    {
        base.FireInner(firingVector);
        _beamRenderer.SetPosition(0, Muzzles[_nextBarrel].position);
        _beamRenderer.SetPosition(1, Muzzles[_nextBarrel].position + Muzzles[_nextBarrel].up * MaxRange);
        StartCoroutine(HandleBeam());
        // do damage
    }

    public float BeamDuration;
    private LineRenderer _beamRenderer;
}
