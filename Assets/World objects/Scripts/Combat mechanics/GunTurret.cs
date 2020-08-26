using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System;

public class GunTurret : DirectionalTurret
{
    protected override void FireInner(Vector3 firingVector, int barrelIdx)
    {
        Warhead w = _warheads[_currAmmoType];
        w.EffectVsStrikeCraft = Mathf.Clamp(w.EffectVsStrikeCraft + _vsStrikeCraftModifier, 0.05f, 0.95f);
        Projectile p = ObjectFactory.AcquireProjectile(Muzzles[barrelIdx].position, firingVector, MuzzleVelocity, MaxRange, ProjectileScale, w, _containingShip);
        p.WeaponEffectKey = ObjectFactory.GetEffectKey(TurretWeaponType, TurretWeaponSize, _ammoTypes[_currAmmoType]);
        p.ProximityProjectile = w.BlastRadius > 0f;
        if (MuzzleFx[barrelIdx] != null)
        {
            MuzzleFx[barrelIdx].Play(true);
        }
    }

    public override void Init(string turretSlotType)
    {
        base.Init(turretSlotType);
        _ammoTypes = new string[_warheads.Length];
        _currAmmoType = 0;
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
            case TurretMod.DualAmmoFeed:
                return true;
            case TurretMod.TractorBeam:
            case TurretMod.ImprovedCapacitors:
            default:
                return false;
        }
    }

    protected override void FireGrapplingToolInner(Vector3 firingVector, int barrelIdx)
    {
        HarpaxBehavior p = ObjectFactory.AcquireHarpaxProjectile(Muzzles[_nextBarrel].position, firingVector, MuzzleVelocity, MaxRange, ProjectileScale, _containingShip);
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
        Warhead w = ObjectFactory.CreateWarhead(TurretWeaponType, TurretWeaponSize, _ammoTypes[0]);
        return new ValueTuple<float, float, float>(w.ShieldDamage * fireRate, w.SystemDamage * fireRate, w.HullDamage * fireRate);
    }

    public void SetAmmoType(int idx, string ammoType)
    {
        SetAmmoType(idx, ammoType, ObjectFactory.CreateWarhead(TurretWeaponType, TurretWeaponSize, ammoType));
    }

    public void SetAmmoType(int idx, string ammoType, Warhead w)
    {
        _ammoTypes[idx] = ammoType;
        _warheads[idx] = w;
    }

    public void SwitchAmmoType(int idx)
    {
        if (_turretMod == TurretMod.DualAmmoFeed)
        {
            if (idx < 0 || idx >= _warheads.Length)
            {
                throw new ArgumentOutOfRangeException("idx", string.Format("Attempted to switch to ammo index {0}. Allowed range: 0-{1}", idx, _warheads.Length - 1));
            }
            bool changed = _currAmmoType != idx;
            _currAmmoType = idx;
            if (changed)
            {
                LastFire = Time.time;
            }
        }
    }

    public void CycleAmmoType()
    {
        if (_turretMod == TurretMod.DualAmmoFeed)
            SwitchAmmoType((_currAmmoType + 1) % _warheads.Length);
    }

    public override ObjectFactory.WeaponBehaviorType BehaviorType => ObjectFactory.WeaponBehaviorType.Gun;

    public float ProjectileScale;
    public float MuzzleVelocity;
    private string[] _ammoTypes;
    private int _currAmmoType;
}
