using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BeamTurret : TurretBase
{
    protected override void FireInner(Vector3 firingVector)
    {
        base.FireInner(firingVector);
        // render beam
        // do damage
    }

    public float BeamDuration;
}
