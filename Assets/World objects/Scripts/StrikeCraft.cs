using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class StrikeCraft : ShipBase
{
    protected override void Awake()
    {
        base.Awake();
        IgnoreHits = false;
    }

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
        _trail = GetComponent<TrailRenderer>();
        _shipAI = GetComponent<StrikeCraftAIController>();
    }

    protected override void Update()
    {
        if (_inRecoveryFinalPhase)
        {
            RecoveryUpdate();
        }
        else if (_attachedToHangerElevator)
        {
            Vector3 pos = _hangerElevator.TransformPoint(_hangerElevatorLocalOffset);
            transform.position = pos;
            transform.rotation = _hangerElevatorRotationOffset * _hangerElevator.rotation;
        }
        else
        {
            base.Update();
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
        if (IgnoreHits)
            return;

        HullHitPoints -= w.HullDamage;

        if (HullHitPoints <= 0)
        {
            ParticleSystem ps = ObjectFactory.CreateWeaponEffect(ObjectFactory.WeaponEffect.SmallExplosion, transform.position);
            ps.transform.localScale = GlobalDistances.ShipExplosionSizeStrikeCraft;
            ps.Play();
            Destroy(ps.gameObject, 1.0f);
            Destroy(this.gameObject);
        }
    }

    public override TargetableEntityInfo TargetableBy => TargetableEntityInfo.Flak;
    public override ObjectFactory.TacMapEntityType TargetableEntityType => ObjectFactory.TacMapEntityType.SrikeCraft;

    public bool IsOutOfAmmo()
    {
        return Turrets.Any(t => t.IsOutOfAmmo);
    }

    public override void AddToFormation(FormationBase f)
    {
        if (!(f is StrikeCraftFormation))
        {
            throw new System.Exception("Not a strike craft formation");
        }
        if (ContainingFormation != null)
        {
            RemoveFromFormation();
        }
        ContainingFormation = f;
        ((StrikeCraftFormation)ContainingFormation).AddStrikeCraft(this);
    }

    public override void RemoveFromFormation()
    {
        if (ContainingFormation == null)
        {
            return;
        }
        ((StrikeCraftFormation)ContainingFormation).RemoveStrikeCraft(this);
    }

    public override bool InPositionInFormation()
    {
        return ContainingFormation != null &&
            (ContainingFormation.GetPosition(this) - transform.position).sqrMagnitude <= (GlobalDistances.StrikeCraftAIFormationPositionTolerance * GlobalDistances.StrikeCraftAIFormationPositionTolerance);
    }

    void OnDestroy()
    {
        RemoveFromFormation();
    }

    public void BeginRecoveryFinalPhase(Transform target, int idx)
    {
        _inRecoveryFinalPhase = true;
        _recoveryFinalPhaseTarget = target;
        _recoveryFinalPhaseIdx = idx;
        _rigidBody.isKinematic = true;
        _rigidBody.velocity = Vector3.zero;
    }

    private void RecoveryUpdate()
    {
        if (_inRecoveryInsideElevator)
        {
            Vector3 pos = _hangerElevator.TransformPoint(_hangerElevatorLocalOffset);
            transform.position = pos;
            transform.rotation = _hangerElevatorRotationOffset * _hangerElevator.rotation;
            return;
        }
        float recAdvance = Time.deltaTime * _recoverySpeed;
        if (_inRecoveryHangerOpen && _inRecoveryPositionForDescent && !_inRecoveryFinalPhaseDescent)
        {
            _inRecoveryFinalPhaseDescent = true;
        }
        if (!_inRecoveryFinalPhaseDescent)
        {
            Vector3 recTargetFlat = new Vector3(_recoveryFinalPhaseTarget.position.x, transform.position.y, _recoveryFinalPhaseTarget.position.z);
            if ((transform.position - recTargetFlat).sqrMagnitude < recAdvance * recAdvance)
            {
                _inRecoveryPositionForDescent = true;
            }
            transform.position = Vector3.MoveTowards(transform.position, recTargetFlat, recAdvance);
            Vector3 recForward = Vector3.ProjectOnPlane(_recoveryFinalPhaseTarget.position - transform.position, Vector3.up);
            transform.rotation = Quaternion.LookRotation(Vector3.up, recForward);
        }
        else
        {
            if ((transform.position - _recoveryFinalPhaseTarget.position).sqrMagnitude < recAdvance * recAdvance)
            {
                if (((StrikeCraftFormation)ContainingFormation).HostCarrier.RecoveryTryLand(this, _recoveryFinalPhaseIdx, FinishRecovery))
                {
                    _inRecoveryInsideElevator = true;
                }
            }
            else
            {
                transform.position = Vector3.MoveTowards(transform.position, _recoveryFinalPhaseTarget.position, recAdvance);
                Vector3 recForward = Vector3.ProjectOnPlane(_recoveryFinalPhaseTarget.position - transform.position, Vector3.up);
                transform.rotation = Quaternion.LookRotation(Vector3.up, recForward);
            }
        }
    }

    public void OnRecoveryHangerOpen()
    {
        _inRecoveryHangerOpen = true;
    }

    private void FinishRecovery()
    {
        Destroy(gameObject);
    }

    public void AttachToHangerElevator(Transform elevator)
    {
        _hangerElevator = elevator;
        _hangerElevatorLocalOffset = elevator.InverseTransformPoint(transform.position);
        _hangerElevatorRotationOffset = transform.rotation * Quaternion.Inverse(elevator.rotation);
        _attachedToHangerElevator = true;
        if (_trail != null)
        {
            _trail.enabled = false;
        }
    }

    public void DetachHangerElevator()
    {
        _hangerElevator = null;
        _attachedToHangerElevator = false;
        if (_trail != null)
        {
            _trail.enabled = true;
        }
    }

    public bool IgnoreHits { get; set; }

    private bool _inRecoveryFinalPhase = false;
    private bool _inRecoveryPositionForDescent = false;
    private bool _inRecoveryHangerOpen = false;
    private bool _inRecoveryFinalPhaseDescent = false;
    private bool _inRecoveryInsideElevator = false;
    private bool _attachedToHangerElevator = false;

    private Transform _hangerElevator;
    private Vector3 _hangerElevatorLocalOffset;
    private Quaternion _hangerElevatorRotationOffset;
    private Transform _recoveryFinalPhaseTarget;
    private int _recoveryFinalPhaseIdx;
    private float _recoverySpeed;
    private TrailRenderer _trail;
}
