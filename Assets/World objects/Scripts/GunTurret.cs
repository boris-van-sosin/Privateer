using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GunTurret : TurretBase
{
    protected override void FireInner(Vector3 firingVector)
    {
        base.FireInner(firingVector);
        Warhead testWarhead = ObjectFactory.CreateWarhead(TurretWeaponType, TurretSize, AmmoType);
        Projectile p = ObjectFactory.CreateProjectile(firingVector, MuzzleVelocity, MaxRange, testWarhead, _containingShip);
        p.transform.position = Muzzles[_nextBarrel].position;
        if (MuzzleFx[_nextBarrel] != null)
        {
            MuzzleFx[_nextBarrel].Play(true);
        }
    }

    public float MuzzleVelocity;
    public ObjectFactory.AmmoType AmmoType;
}
