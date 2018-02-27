using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpecialProjectileTurret : TurretBase
{
    protected override void FireInner(Vector3 firingVector)
    {
        base.FireInner(firingVector);
        Warhead w = ObjectFactory.CreateWarhead(ObjectFactory.WeaponType.PlasmaCannon, TurretSize);
        Projectile p = ObjectFactory.CreatePlasmaProjectile(firingVector, MuzzleVelocity, MaxRange, w, _containingShip);
        p.transform.position = Muzzles[_nextBarrel].position;
    }

    public float MuzzleVelocity;
}
