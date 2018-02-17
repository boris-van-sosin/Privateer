using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GunTurret : TurretBase
{
    protected override void FireInner(Vector3 firingVector)
    {
        base.FireInner(firingVector);
        Warhead testWarhead = new Warhead()
        {
            ShieldDamage = 40,
            ArmourPenetration = 150,
            ArmourDamage = 5,
            SystemDamage = 10,
            HullDamage = 5
        };
        Projectile p = ObjectFactory.CreateProjectile(firingVector, MuzzleVelocity, MaxRange, testWarhead, _containingShip);
        p.transform.position = Muzzles[_nextBarrel].position;
        if (MuzzleFx[_nextBarrel] != null)
        {
            MuzzleFx[_nextBarrel].Play(true);
        }
    }

    public float MuzzleVelocity;
}
