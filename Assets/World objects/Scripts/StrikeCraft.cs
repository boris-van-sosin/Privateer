using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StrikeCraft : ShipBase
{
    protected override void ApplyThrust()
    {
        _thrustCoefficient = 1.0f;
        base.ApplyThrust();
    }

    public override void ApplyBraking()
    {
        _brakingFactor = 1.0f;
        _brakingTargetSpeedFactor = 0f;
        base.ApplyBraking();
    }

    public override void ApplyTurning(bool left)
    {
        _turnCoefficient = 1.0f;
        base.ApplyTurning(left);
    }

    public override void TakeHit(Warhead w, Vector3 location)
    {
        ParticleSystem ps = ObjectFactory.CreateWeaponEffect(ObjectFactory.WeaponEffect.SmallExplosion, transform.position);
        ps.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);
        ps.Play();
        Destroy(ps.gameObject, 1.0f);
        Destroy(this.gameObject);
    }
}
