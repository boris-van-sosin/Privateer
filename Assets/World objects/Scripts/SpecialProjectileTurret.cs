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
        p.WeaponEffectKey = ObjectFactory.WeaponEffect.PlasmaExplosion;
        p.transform.position = Muzzles[_nextBarrel].position;
    }

    public override bool IsTurretModCombatible(TurretMod m)
    {
        switch (m)
        {
            case TurretMod.None:
            case TurretMod.ImprovedCapacitors:
            case TurretMod.AdvancedTargeting:
                return true;
            case TurretMod.FastAutoloader:
            case TurretMod.TractorBeam:
            case TurretMod.Harpax:
            case TurretMod.Accelerator:
            default:
                return false;
        }
    }

    public float MuzzleVelocity;
}
