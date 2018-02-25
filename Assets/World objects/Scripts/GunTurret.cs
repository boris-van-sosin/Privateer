using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GunTurret : TurretBase
{
    protected override void FireInner(Vector3 firingVector)
    {
        base.FireInner(firingVector);
        Warhead warhead = ObjectFactory.CreateWarhead(TurretWeaponType, TurretSize, AmmoType);
        Projectile p = ObjectFactory.CreateProjectile(firingVector, MuzzleVelocity, MaxRange, warhead, _containingShip);
        switch (AmmoType)
        {
            case ObjectFactory.AmmoType.KineticPenetrator:
                p.WeaponEffectKey = ObjectFactory.WeaponEffect.KineticImpactSparks;
                break;
            case ObjectFactory.AmmoType.ShapedCharge:
                p.WeaponEffectKey = ObjectFactory.WeaponEffect.SmallExplosion;
                break;
            case ObjectFactory.AmmoType.ShrapnelRound:
                p.WeaponEffectKey = ObjectFactory.WeaponEffect.FlakBurst;
                break;
            default:
                break;
        }
        p.transform.position = Muzzles[_nextBarrel].position;
        if (MuzzleFx[_nextBarrel] != null)
        {
            MuzzleFx[_nextBarrel].Play(true);
        }
    }

    public float MuzzleVelocity;
    public ObjectFactory.AmmoType AmmoType;
}
