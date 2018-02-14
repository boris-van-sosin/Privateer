using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ContinuousBeamTurret : TurretBase
{
    protected override void FireInner(Vector3 firingVector)
    {
        base.FireInner(firingVector);
        // render beam
        StartCoroutine(DoBeamDamage());
    }

    private IEnumerator DoBeamDamage()
    {
        float BeamStarted = Time.time;
        float CurrTime = BeamStarted;
        while (CurrTime - BeamStarted < BeamDuration)
        {
            // do damage
            yield return new WaitForSeconds(BeamPulseTime);
            CurrTime = Time.time;
        }
        yield return null;
    }

    public float BeamPulseTime;
    public float BeamDuration;
}
