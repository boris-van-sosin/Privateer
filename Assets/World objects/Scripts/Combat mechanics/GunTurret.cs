using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System;

public class GunTurret : DirectionalTurret
{
    protected override void FireInner(Vector3 firingVector, int barrelIdx)
    {
        Warhead warhead = ObjectFactory.CreateWarhead(TurretWeaponType, TurretWeaponSize, AmmoType);
        warhead.EffectVsStrikeCraft = Mathf.Clamp(warhead.EffectVsStrikeCraft + _vsStrikeCraftModifier, 0.05f, 0.95f);
        Projectile p = ObjectFactory.AcquireProjectile(Muzzles[barrelIdx].position, firingVector, MuzzleVelocity, MaxRange, ProjectileScale, warhead, _containingShip);
        p.WeaponEffectKey = ObjectFactory.GetEffectKey(TurretWeaponType, TurretWeaponSize, AmmoType);
        p.ProximityProjectile = warhead.BlastRadius > 0f;
        if (MuzzleFx[barrelIdx] != null)
        {
            MuzzleFx[barrelIdx].Play(true);
        }
    }

    public override bool IsTurretModCombatible(TurretMod m)
    {
        switch (m)
        {
            case TurretMod.None:
            case TurretMod.Harpax:
            case TurretMod.Accelerator:
            case TurretMod.AdvancedTargeting:
            case TurretMod.FastAutoloader:
                return true;
            case TurretMod.TractorBeam:
            case TurretMod.ImprovedCapacitors:
            default:
                return false;
        }
    }

    protected override void FireGrapplingToolInner(Vector3 firingVector, int barrelIdx)
    {
        HarpaxBehavior p = ObjectFactory.CreateHarpaxProjectile(firingVector, MuzzleVelocity, MaxRange, _containingShip);
        p.transform.position = Muzzles[_nextBarrel].position;
        if (MuzzleFx[barrelIdx] != null)
        {
            MuzzleFx[barrelIdx].Play(true);
        }
    }

    public override void ApplyBuff(DynamicBuff b)
    {
        base.ApplyBuff(b);
        _inaccuracyCoeff = Mathf.Max(1f - b.WeaponAccuracyFactor, 0.05f);
        _vsStrikeCraftModifier = b.WeaponVsStrikeCraftFactor;
    }

    public ValueTuple<float, float, float> DebugGetDPS()
    {
        int numBarrels = FindMuzzles(transform).Count();
        float fireRate = numBarrels / FiringInterval;
        Warhead w = ObjectFactory.CreateWarhead(TurretWeaponType, TurretWeaponSize, AmmoType);
        return new ValueTuple<float, float, float>(w.ShieldDamage * fireRate, w.SystemDamage * fireRate, w.HullDamage * fireRate);
    }

    public override ObjectFactory.WeaponBehaviorType BehaviorType => ObjectFactory.WeaponBehaviorType.Gun;

    public float ProjectileScale;
    public float MuzzleVelocity;
    public string AmmoType;
}
