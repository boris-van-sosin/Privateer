﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System;

public class GunTurret : DirectionalTurret
{
    protected override void FireInner(Vector3 firingVector, int barrelIdx)
    {
        Warhead warhead = ObjectFactory.CreateWarhead(TurretWeaponType, TurretSize, AmmoType);
        warhead.EffectVsStrikeCraft = Mathf.Clamp(warhead.EffectVsStrikeCraft + _vsStrikeCraftModifier, 0.05f, 0.95f);
        Projectile p = ObjectFactory.CreateProjectile(firingVector, MuzzleVelocity, MaxRange, ProjectileScale, warhead, _containingShip);
        switch (AmmoType)
        {
            case ObjectFactory.AmmoType.KineticPenetrator:
                p.WeaponEffectKey = ObjectFactory.WeaponEffect.KineticImpactSparks;
                p.ProximityProjectile = false;
                break;
            case ObjectFactory.AmmoType.ShapedCharge:
                p.WeaponEffectKey = ObjectFactory.WeaponEffect.SmallExplosion;
                p.ProximityProjectile = false;
                break;
            case ObjectFactory.AmmoType.ShrapnelRound:
                p.WeaponEffectKey = ObjectFactory.WeaponEffect.FlakBurst;
                p.ProximityProjectile = true;
                break;
            default:
                break;
        }
        p.transform.position = Muzzles[barrelIdx].position;
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

    public override void ApplyBuff(Buff b)
    {
        base.ApplyBuff(b);
        _inaccuracyCoeff = Mathf.Max(1f - b.WeaponAccuracyFactor, 0.05f);
        _vsStrikeCraftModifier = b.WeaponVsStrikeCraftFactor;
    }

    public ValueTuple<float, float, float> DebugGetDPS()
    {
        int numBarrels = FindBarrels(transform).Count();
        float fireRate = numBarrels / FiringInterval;
        Warhead w = ObjectFactory.CreateWarhead(TurretWeaponType, TurretSize, AmmoType);
        return new ValueTuple<float, float, float>(w.ShieldDamage * fireRate, w.SystemDamage * fireRate, w.HullDamage * fireRate);
    }

    public float ProjectileScale;
    public float MuzzleVelocity;
    public ObjectFactory.AmmoType AmmoType;
}