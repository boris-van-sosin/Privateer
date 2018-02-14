using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpecialProjectileTurret : TurretBase
{
    protected override void FireInner(Vector3 firingVector)
    {
        base.FireInner(firingVector);
        Projectile p = ObjectFactory.CreateProjectile(firingVector, MuzzleVelocity, MaxRange, _containingShip);
        p.transform.position = Muzzles[_nextBarrel].position;
    }

    public float MuzzleVelocity;
}
