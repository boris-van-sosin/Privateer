using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpecialProjectileTurret : DirectionalTurret
{
    protected override void FireInner(Vector3 firingVector, int barrelIdx)
    {
        Warhead w = ObjectFactory.CreateWarhead(TurretWeaponType, TurretWeaponSize);
        Projectile p = ObjectFactory.CreatePlasmaProjectile(firingVector, MuzzleVelocity, MaxRange, w, _containingShip);
        p.WeaponEffectKey = ObjectFactory.GetEffectKey(TurretWeaponType, TurretWeaponSize);
        p.transform.position = Muzzles[barrelIdx].position;
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

    public override ObjectFactory.WeaponBehaviorType BehaviorType => ObjectFactory.WeaponBehaviorType.Special;

    protected override void FireGrapplingToolInner(Vector3 firingVector, int barrelIdx)
    {
    }

    public float MuzzleVelocity;
}
