using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ContinuousBeamTurret : GeneralBeamTurret
{
    protected override void FireInner(Vector3 firingVector)
    {
        base.FireInner(firingVector);
        bool prevFiring = _firing;
        _firing = true;
        _firingVector = firingVector;
        _beamOrigin = Muzzles[_nextBarrel].position;
        if (!prevFiring)
        {
            StartCoroutine(DoBeam(ObjectFactory.CreateWarhead(ObjectFactory.WeaponType.Laser, TurretSize)));
        }
    }

    protected override bool ReadyToFire()
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
        yield return new WaitForEndOfFrame();
        Warhead pulseWarhead = new Warhead()
        {
            ShieldDamage = w.ShieldDamage / 10,
            ArmourPenetration = w.ArmourPenetration,
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
            yield return new WaitForSeconds(BeamPulseTime);
            CurrTime = Time.time;
            --_pulesLeft;
        }
        _beamRenderer.enabled = false;
        _firing = false;
        yield return null;
    }

    public float BeamPulseTime;
    public float BeamDuration;
    private bool _firing;
    private int _pulesLeft;
}
