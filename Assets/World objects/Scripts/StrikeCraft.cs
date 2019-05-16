﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class StrikeCraft : ShipBase
{
    public override void Activate()
    {
        base.Activate();
        foreach (TurretHardpoint hp in WeaponHardpoints)
        {
            TurretBase t = hp.GetComponentInChildren<TurretBase>();
            if (t != null)
            {
                t.SetTurretBehavior(TurretBase.TurretMode.Auto);
            }
        }
        _recoverySpeed = MaxSpeed * 1f;
    }

    protected override void Update()
    {
        if (!_inRecoveryFinalPhase)
        {
            base.Update();
        }
        else
        {
            float recAdvance = Time.deltaTime * _recoverySpeed;
            if ((transform.position - _recoveryFinalPhaseTarget.position).sqrMagnitude < recAdvance * recAdvance)
            {
                Destroy(gameObject);
            }
            else if (!_inRecoveryFinalPhaseDescent)
            {
                Vector3 recTargetFlat = new Vector3(_recoveryFinalPhaseTarget.position.x, transform.position.y, _recoveryFinalPhaseTarget.position.z);
                if ((transform.position - recTargetFlat).sqrMagnitude < recAdvance * recAdvance)
                {
                    _inRecoveryFinalPhaseDescent = true;
                }
                transform.position = Vector3.MoveTowards(transform.position, recTargetFlat, recAdvance);
                Vector3 recForward = Vector3.ProjectOnPlane(_recoveryFinalPhaseTarget.position - transform.position, Vector3.up);
                transform.rotation = Quaternion.LookRotation(Vector3.up, recForward);
            }
            else
            {
                transform.position = Vector3.MoveTowards(transform.position, _recoveryFinalPhaseTarget.position, recAdvance);
                Vector3 recForward = Vector3.ProjectOnPlane(_recoveryFinalPhaseTarget.position - transform.position, Vector3.up);
                transform.rotation = Quaternion.LookRotation(Vector3.up, recForward);
            }
        }
    }

    protected override void ApplyThrust()
    {
        _thrustCoefficient = 1.0f;
        base.ApplyThrust();
    }

    public override void ApplyBraking()
    {
        _brakingFactor = 1.0f;
        _brakingTargetSpeedFactor = 0f;
        base.ApplyBraking();
    }

    public override void ApplyTurning(bool left)
    {
        _turnCoefficient = 1.0f;
        base.ApplyTurning(left);
    }

    public override void TakeHit(Warhead w, Vector3 location)
    {
        if (w.HullDamage == 0) // Ugly hack. Fix later.
        {
            --_strikeCraftHitPoints;
        }
        else
        {
            HullHitPoints -= w.HullDamage;
        }
        if (_strikeCraftHitPoints <= 0)
        {
            --HullHitPoints;
            _strikeCraftHitPoints = 5;
        }
        if (HullHitPoints <= 0)
        {
            ParticleSystem ps = ObjectFactory.CreateWeaponEffect(ObjectFactory.WeaponEffect.SmallExplosion, transform.position);
            ps.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);
            ps.Play();
            Destroy(ps.gameObject, 1.0f);
            Destroy(this.gameObject);
        }
    }

    public override TargetableEntityInfo TargetableBy
    {
        get
        {
            return TargetableEntityInfo.Flak;
        }
    }

    public bool IsOutOfAmmo()
    {
        return Turrets.Any(t => t.IsOutOfAmmo);
    }

    public StrikeCraftFormation ContainingFormation { get; private set; }

    public void AddToFormation(StrikeCraftFormation f)
    {
        if (ContainingFormation != null)
        {
            RemoveFromFormation();
        }
        ContainingFormation = f;
        ContainingFormation.AddStrikeCraft(this);
    }

    public void RemoveFromFormation()
    {
        if (ContainingFormation == null)
        {
            return;
        }
        ContainingFormation.RemoveStrikeCraft(this);
    }

    public Vector3 PositionInFormation
    {
        get
        {
            return ContainingFormation.GetPosition(this);
        }
    }

    public bool InPositionInFormation()
    {
        return ContainingFormation != null &&
            (ContainingFormation.GetPosition(this) - transform.position).sqrMagnitude <= (StrikeCraftFormation.DistThreshold * StrikeCraftFormation.DistThreshold);
    }

    public bool AheadOfPositionInFormation()
    {
        if (ContainingFormation == null)
        {
            return false;
        }
        Vector3 offset = transform.position - ContainingFormation.GetPosition(this);
        return
            Vector3.Dot(offset, transform.up) > 0 &&
            Vector3.Angle(offset, transform.up) < 30;
    }

    void OnDestroy()
    {
        RemoveFromFormation();
    }

    public void BeginRecoveryFinalPhase(Transform target)
    {
        _inRecoveryFinalPhase = true;
        _recoveryFinalPhaseTarget = target;
        _rigidBody.isKinematic = true;
        _rigidBody.velocity = Vector3.zero;
    }

    private int _strikeCraftHitPoints = 5;
    private bool _inRecoveryFinalPhase = false;
    private bool _inRecoveryFinalPhaseDescent = false;
    private Transform _recoveryFinalPhaseTarget;
    private float _recoverySpeed;
}
