using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpecialProjectileTurret : TurretBase
{
    protected override void FireInner(Vector3 firingVector)
    {
        base.FireInner(firingVector);
        Warhead testWarhead = new Warhead()
        {
            ShieldDamage = 10,
            ArmourPenetration = 20,
            ArmourDamage = 5,
            SystemDamage = 3,
            HullDamage = 5
        };
        Projectile p = ObjectFactory.CreateProjectile(firingVector, MuzzleVelocity, MaxRange, testWarhead, _containingShip);
        p.transform.position = Muzzles[_nextBarrel].position;
    }

    public float MuzzleVelocity;
}
