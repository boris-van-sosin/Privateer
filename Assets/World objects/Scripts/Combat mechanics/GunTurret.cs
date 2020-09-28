using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System;

public class GunTurret : DirectionalTurret
{
    protected override void FireInner(Vector3 firingVector, int barrelIdx)
    {
        Warhead w = _warheadsAfterBuffs[_currAmmoType];

        w.EffectVsStrikeCraft = Mathf.Clamp(w.EffectVsStrikeCraft + _vsStrikeCraftModifier, 0.05f, 0.95f);
        Projectile p = ObjectFactory.AcquireProjectile(Muzzles[barrelIdx].position, firingVector, _velocityAfterBuffs[_currAmmoType], _rangeAfterBuffs[_currAmmoType], ProjectileScale, w, _containingShip);
        p.WeaponEffectKey = ObjectFactory.GetEffectKey(TurretWeaponType, TurretWeaponSize, _ammoTypes[_currAmmoType]);
        p.ProximityProjectile = w.BlastRadius > 0f;
        if (MuzzleFx[barrelIdx] != null)
        {
            MuzzleFx[barrelIdx].Play(true);
        }
    }

    public override void Init(string turretSlotType, TurretMod turretMod)
    {
        _ammoTypes = new string[MaxWarheads];
        _warheadsAfterBuffs = new Warhead[MaxWarheads];
        _velocityAfterBuffs = new float[MaxWarheads];
        _rangeAfterBuffs = new float[MaxWarheads];
        _currAmmoType = 0;
        base.Init(turretSlotType, turretMod);
        CalculateTurretModBuffs();
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

    private void CalculateTurretModBuffs()
    {
        for (int i = 0; i < _warheads.Length; ++i)
        {
            _warheadsAfterBuffs[i] = _warheads[i];
            _velocityAfterBuffs[i] = MuzzleVelocity;
            _rangeAfterBuffs[i] = MaxRange;
            if (_turretModBuffs != null)
            {
                float velocityFactor = 1f;
                float damageFactor = 1f;
                float APFactor = 1f;
                float rangeFactor = 1f;
                for (int j = 0; j < _turretModBuffs.Length; j++)
                {
                    if (IsTurretModBuffApplicable(j))
                    {
                        velocityFactor += _turretModBuffs[j].MuzzleVelocityBuff;
                        damageFactor += _turretModBuffs[j].DamageBuff;
                        APFactor += _turretModBuffs[j].ArmourPenetrationBuff;
                        rangeFactor += _turretModBuffs[j].RangeBuff;
                    }
                }
                _warheadsAfterBuffs[i].ArmourPenetration = Mathf.FloorToInt(_warheadsAfterBuffs[i].ArmourPenetration * APFactor);
                _warheadsAfterBuffs[i].ArmourDamage = Mathf.FloorToInt(_warheadsAfterBuffs[i].ArmourDamage * damageFactor);
                _warheadsAfterBuffs[i].ShieldDamage = Mathf.FloorToInt(_warheadsAfterBuffs[i].ShieldDamage * damageFactor);
                _warheadsAfterBuffs[i].HullDamage = Mathf.FloorToInt(_warheadsAfterBuffs[i].HullDamage * damageFactor);
                _warheadsAfterBuffs[i].SystemDamage = Mathf.FloorToInt(_warheadsAfterBuffs[i].SystemDamage * damageFactor);
                _velocityAfterBuffs[i] *= velocityFactor;
                _rangeAfterBuffs[i] *= rangeFactor;
            }
        }

        // Accuracy. This does not depend on the selected ammo type.
        _accuracyFactorBuff = 0f;
        if (_turretModBuffs != null)
        {
            for (int i = 0; i < _turretModBuffs.Length; ++i)
            {
                if (IsTurretModBuffApplicable(i))
                {
                    _accuracyFactorBuff += _turretModBuffs[i].AccuracyBuff;
                }
            }
        }
    }

    protected override bool IsTurretModBuffApplicable(int idx)
    {
        return base.IsTurretModBuffApplicable(idx) &&
               (string.IsNullOrEmpty(_turretModBuffs[idx].ApplyToAmmo) || _turretModBuffs[idx].ApplyToAmmo == _ammoTypes[_currAmmoType]);
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
        _inaccuracyCoeff = Mathf.Max(1f - b.WeaponAccuracyFactor - _accuracyFactorBuff, 0.05f);
        _vsStrikeCraftModifier = b.WeaponVsStrikeCraftFactor;
    }

    protected override ITargetableEntity AcquireTarget()
    {
        ITargetableEntity res = base.AcquireTarget();
        if (_turretMod == TurretMod.DualAmmoFeed)
        {
            int bestAmmoType = _currAmmoType;
            if (res is Ship shipTarget)
            {
                for (int i = 0; i < _warheads.Length; ++i)
                {
                    if (i == bestAmmoType)
                    {
                        continue;
                    }
                    else if (shipTarget.ShipTotalShields < _warheads[i].ShieldDamage &&
                        _warheads[i].ArmourPenetration > _warheads[bestAmmoType].ArmourPenetration)
                    {
                        bestAmmoType = i;
                    }
                    else if (shipTarget.ShipTotalShields > _warheads[i].ShieldDamage &&
                             (shipTarget.ShipTotalShields > _warheads[bestAmmoType].ShieldDamage) &&
                             _warheads[i].ShieldDamage > _warheads[bestAmmoType].ShieldDamage)
                    {
                        bestAmmoType = i;
                    }
                }
            }
            else if (res is Torpedo || res is StrikeCraft)
            {
                for (int i = 0; i < _warheads.Length; ++i)
                {
                    if (i == bestAmmoType)
                    {
                        continue;
                    }
                    else if (_warheads[i].EffectVsStrikeCraft > _warheads[bestAmmoType].EffectVsStrikeCraft)
                    {
                        bestAmmoType = i;
                    }
                }
            }
            SwitchAmmoType(bestAmmoType);
        }
        return res;
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
        CalculateTurretModBuffs();
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

    public string SelectedAmmoType => _ammoTypes[_currAmmoType];

    public override ObjectFactory.WeaponBehaviorType BehaviorType => ObjectFactory.WeaponBehaviorType.Gun;

    public float ProjectileScale;
    public float MuzzleVelocity;
    private string[] _ammoTypes;
    private int _currAmmoType;

    private Warhead[] _warheadsAfterBuffs;
    private float[] _rangeAfterBuffs;
    private float[] _velocityAfterBuffs;
    private float _accuracyFactorBuff;
}
