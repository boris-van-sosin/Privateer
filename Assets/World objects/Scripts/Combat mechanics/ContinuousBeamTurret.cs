using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ContinuousBeamTurret : GeneralBeamTurret
{
    public override void Init(string turretSlotType, TurretMod turretMod)
    {
        base.Init(turretSlotType, turretMod);
        _pulseDelay = new WaitForSeconds(BeamPulseTime);
        _warheads[0] = ObjectFactory.CreateWarhead(TurretWeaponType, TurretWeaponSize);
    }

    protected override void FireInner(Vector3 firingVector, int barrelIdx)
    {
        bool prevFiring = _firing;
        _firing = true;
        _firingVector = firingVector;
        _beamOrigin = Muzzles[barrelIdx].position;
        if (!prevFiring)
        {
            StartCoroutine(DoBeam(_warheads[0]));
        }
    }

    public override bool ReadyToFire()
    {
        if (_pulesLeft > 0)
        {
            return true;
        }
        else
        {
            return base.ReadyToFire();
        }
    }

    private IEnumerator DoBeam(Warhead w)
    {
        yield return _endOfFrameWait;
        Warhead pulseWarhead = new Warhead()
        {
            ShieldDamage = w.ShieldDamage / 10,
            ArmourPenetrationMedian = w.ArmourPenetrationMedian,
            ArmourDamage = w.ArmourDamage / 10,
            SystemDamage = w.SystemDamage / 10,
            HullDamage = w.HullDamage / 10,
            HeatGenerated = w.HeatGenerated / 10,
            WeaponEffectScale = w.WeaponEffectScale
        };
        _beamRenderer.enabled = true;
        float BeamStarted = Time.time;
        _pulesLeft = Mathf.FloorToInt(BeamDuration / BeamPulseTime);
        float CurrTime = BeamStarted;
        while (_pulesLeft > 0)
        {
            if (_pulesLeft > 1)
            {
                DoBeamHit(pulseWarhead);
                {
                    w.ShieldDamage -= pulseWarhead.ShieldDamage;
                    w.ArmourDamage -= pulseWarhead.ArmourDamage / 10;
                    w.SystemDamage -= pulseWarhead.SystemDamage / 10;
                    w.HullDamage -= pulseWarhead.HullDamage / 10;
                    w.HeatGenerated -= pulseWarhead.HeatGenerated / 10;
                    w.WeaponEffectScale -= pulseWarhead.WeaponEffectScale;
                }
            }
            else
            {
                DoBeamHit(w);
            }
            _firing = false;
            yield return _pulseDelay;
            CurrTime = Time.time;
            --_pulesLeft;
        }
        _beamRenderer.enabled = false;
        _firing = false;
        yield return null;
    }

    public override bool IsTurretModCombatible(TurretMod m)
    {
        switch (m)
        {
            case TurretMod.None:
            case TurretMod.TractorBeam:
            case TurretMod.ImprovedCapacitors:
                return true;
            case TurretMod.FastAutoloader:
            case TurretMod.Harpax:
            case TurretMod.Accelerator:
            case TurretMod.AdvancedTargeting:
            default:
                return false;
        }
    }

    public override ObjectFactory.WeaponBehaviorType BehaviorType => ObjectFactory.WeaponBehaviorType.ContinuousBeam;

    protected override void FireGrapplingToolInner(Vector3 firingVector, int barrelIdx)
    {
        // Not implemeted yet
    }

    public float BeamPulseTime;
    public float BeamDuration;
    private bool _firing;
    private int _pulesLeft;

    private WaitForSeconds _pulseDelay;
}
