using System.Collections.Generic;
using System.Text;
using UnityEngine;
using System.Linq;

[RequireComponent(typeof(Rigidbody))]
public abstract class ShipBase : MovementBase, ITargetableEntity
{
    protected virtual void Awake()
    {
        HullHitPoints = MaxHullHitPoints;
        ComputeLength();
        WeaponGroups = null;
        TowingByHarpax = null;
        TowedByHarpax = null;
        GrapplingMode = false;
        _rigidBody = GetComponent<Rigidbody>();
        UseTargetSpeed = false;
    }

    public virtual void Activate()
    {
        FindTurrets();
        InitShield();
        ShipDisabled = false;
        ShipImmobilized = false;
    }

    protected override void ApplyMovement()
    {
        ApplyUpdateAcceleration();
        ApplyUpdateTurning();

        float directionMult = 0.0f;
        if (_movementDirection == ShipDirection.Forward)
        {
            directionMult = 1.0f;
        }
        else if (_movementDirection == ShipDirection.Reverse)
        {
            directionMult = -1.0f;
        }

        _prevPos = transform.position;
        _prevRot = transform.rotation;
        Vector3 targetVelocity = (ActualVelocity = directionMult * _speed * transform.up);// was: Time.deltaTime * (ActualVelocity = directionMult * _speed * transform.up);
        Vector3 rbVelocity = _rigidBody.velocity;
        if (TowedByHarpax != null)
        {
            _prevForceTow = (targetVelocity - rbVelocity) * _rigidBody.mass;
            if (!_hasPrevForceTow)
            {
                _rigidBody.AddForce((targetVelocity - rbVelocity) * _rigidBody.mass, ForceMode.Impulse);
            }
            else
            {
                _rigidBody.AddForce((targetVelocity - rbVelocity) * _rigidBody.mass - _prevForceTow, ForceMode.Impulse);
            }
            _hasPrevForceTow = true;
        }
        else if (_movementDirection == ShipDirection.Stopped)
        {
            _rigidBody.angularVelocity = Vector3.zero;
            _rigidBody.velocity = Vector3.zero;
        }
        else
        {
            _rigidBody.AddForce(targetVelocity - rbVelocity, ForceMode.VelocityChange);
        }
        if (_autoHeading && ShipControllable)
        {
            RotateToHeading();
        }

        // Breaking free from a Harpax
        if (!ShipImmobilized && !ShipDisabled && TowedByHarpax != null)
        {
            if (Time.time - _towedTime > 5.0f)
            {
                DisconnectHarpaxTowed();
            }
        }
        else if (TowedByHarpax)
        {
            _towedTime = Time.time;
        }
    }

    private void RotateToHeading()
    {
        if (Mathf.Abs(Vector3.Cross(transform.up, _autoHeadingVector).y) < 0.1f)
        {
            _autoHeading = false;
            return;
        }

        ApplyTurning(Vector3.Cross(transform.up, _autoHeadingVector).y < 0);
    }

    protected override void ApplyUpdateAcceleration()
    {
        if (_nextAccelerate)
        {
            _nextAccelerate = _nextBrake = false;

            if (!CanDoAcceleration())
                return;

            _speed = Mathf.Min(_speed + Thrust * _thrustCoefficient * Time.deltaTime, MaxSpeed);
        }
        else if (_nextBrake)
        {
            _nextAccelerate = _nextBrake = false;

            if (!CanDoBraking())
                return;

            float targetSpeed = MaxSpeed * Mathf.Clamp01(_brakingTargetSpeedFactor);
            float newSpeed = _speed - Braking * Mathf.Clamp01(_brakingFactor) * Time.deltaTime;
            if (targetSpeed > _speed)
            {
                return;
            }
            if (newSpeed > targetSpeed)
            {
                _speed = newSpeed;
            }
            if (newSpeed <= 0)
            {
                _movementDirection = ShipDirection.Stopped;
                _speed = 0;
            }
        }
        else if (UseTargetSpeed)
        {
            _nextAccelerate = _nextBrake = false;
            float actualTargetSpeed = Mathf.Clamp(TargetSpeed, 0, MaxSpeed);
            if (_speed < actualTargetSpeed)
            {
                if (!CanDoAcceleration())
                    return;

                _speed = Mathf.Min(_speed + Thrust * _thrustCoefficient * Time.deltaTime, actualTargetSpeed);
            }
            else if (_speed > actualTargetSpeed)
            {
                if (!CanDoBraking())
                    return;

                float targetSpeedBraking = Mathf.Min(actualTargetSpeed, MaxSpeed * Mathf.Clamp01(_brakingTargetSpeedFactor));
                float newSpeed = _speed - Braking * Mathf.Clamp01(_brakingFactor) * Time.deltaTime;
                if (targetSpeedBraking > _speed)
                {
                    return;
                }
                if (newSpeed > targetSpeedBraking)
                {
                    _speed = newSpeed;
                }
                if (newSpeed < actualTargetSpeed)
                {
                    _speed = newSpeed = actualTargetSpeed;
                }
                if (newSpeed <= 0)
                {
                    _movementDirection = ShipDirection.Stopped;
                    _speed = 0;
                }

            }
        }
    }

    protected override void ApplyUpdateTurning()
    {
        if (!(_nextTurnLeft || _nextTurnRight))
        {
            return;
        }
        bool thisLeft = _nextTurnLeft;
        _nextTurnLeft = _nextTurnRight = false;
        if (!CanDoTurning())
            return;

        float turnFactor = 1.0f;
        if (thisLeft)
        {
            turnFactor = -1.0f;
        }
        Quaternion deltaRot = Quaternion.AngleAxis(turnFactor * TurnRate * _turnCoefficient * Time.deltaTime, transform.forward);
        transform.rotation = deltaRot * transform.rotation;
    }

    protected virtual bool CanDoAcceleration()
    {
        return true;
    }

    protected virtual bool CanDoBraking()
    {
        return true;
    }

    protected virtual bool CanDoTurning()
    {
        return true;
    }

    public bool ShipDisabled { get; protected set; }
    public bool ShipImmobilized { get; protected set; }
    public virtual bool ShipControllable { get { return ShipActiveInCombat && !(ShipImmobilized); } }
    public virtual bool ShipActiveInCombat { get { return (!ShipDisabled) && HullHitPoints > 0; } }

    private void ComputeLength()
    {
        Mesh m = GetComponent<MeshFilter>().mesh;
        ShipUnscaledLength = m.bounds.size.y;
        ShipUnscaledWidth = m.bounds.size.x;
        ShipLength = ShipUnscaledLength * transform.lossyScale.y;
        ShipWidth = ShipUnscaledWidth * transform.lossyScale.x;
    }

    protected virtual void FindTurrets()
    {
        TurretHardpoint[] hardpoints = GetComponentsInChildren<TurretHardpoint>();
        List<ITurret> turrets = new List<ITurret>(hardpoints.Length);

        foreach (TurretHardpoint hp in hardpoints)
        {
            TurretBase turret = hp.GetComponentInChildren<TurretBase>();
            if (turret != null)
            {
                turrets.Add(turret);
            }
        }
        _turrets = turrets.ToArray();
    }

    public IEnumerable<TurretHardpoint> WeaponHardpoints
    {
        get
        {
            TurretHardpoint[] hardpoints = GetComponentsInChildren<TurretHardpoint>();
            foreach (TurretHardpoint hp in hardpoints)
            {
                yield return hp;
            }
        }
    }

    public virtual bool PlaceTurret(TurretHardpoint hp, TurretBase t)
    {
        if (hp == null || t == null || !hp.AllowedWeaponTypes.Contains(t.TurretType))
        {
            return false;
        }
        TurretBase existingTurret = hp.GetComponentInChildren<TurretBase>();
        if (existingTurret != null)
        {
            return false;
        }
        Quaternion q = Quaternion.LookRotation(-hp.transform.up, hp.transform.forward);
        t.transform.rotation = q;
        t.transform.parent = hp.transform;
        t.transform.localScale = Vector3.one;
        t.transform.localPosition = Vector3.zero;

        if (_turrets != null)
        {
            ITurret[] newTurretArr = new ITurret[_turrets.Length + 1];
            _turrets.CopyTo(newTurretArr, 0);
            newTurretArr[newTurretArr.Length - 1] = t;
            _turrets = newTurretArr;
        }
        else
        {
            _turrets = new ITurret[]
            {
                t
            };
        }

        return true;
    }

    public IEnumerable<ITurret> Turrets
    {
        get
        {
            return _turrets;
        }
    }

    public void SetTurretConfig(TurretControlGrouping cfg)
    {
        WeaponGroups = cfg;
    }
    public void SetTurretConfigAllAuto()
    {
        WeaponGroups = TurretControlGrouping.AllAuto(this);
    }

    public void ManualTarget(Vector3 target)
    {
        if (WeaponGroups == null)
        {
            foreach (ITurret t in _manualTurrets)
            {
                t.ManualTarget(target);
            }
        }
        else
        {
            foreach (ITurret t in WeaponGroups.ManualTurrets)
            {
                t.ManualTarget(target);
            }
        }
    }

    public void FireManual(Vector3 target)
    {
        if (!GrapplingMode)
        {
            FireWeaponManual(target);
        }
        else
        {
            FireHarpaxManual(target);
        }
    }

    public void FireWeaponManual(Vector3 target)
    {
        StringBuilder sb = new StringBuilder();
        if (WeaponGroups == null)
        {
            foreach (ITurret t in _manualTurrets.Where(x => x.GetTurretBehavior() == TurretBase.TurretMode.Manual))
            {
                t.Fire(target);
                sb.AppendFormat("Turret {0}:{1}, ", t, t.CurrLocalAngle);
            }
        }
        else
        {
            foreach (ITurret t in WeaponGroups.ManualTurrets)
            {
                t.Fire(target);
                sb.AppendFormat("Turret {0}:{1}, ", t, t.CurrLocalAngle);
            }
        }
        //Debug.Log(sb.ToString());
    }

    public void FireHarpaxManual(Vector3 target)
    {
        StringBuilder sb = new StringBuilder();
        if (WeaponGroups == null)
        {
            foreach (ITurret t in _manualTurrets.Where(x => x.GetTurretBehavior() == TurretBase.TurretMode.Manual))
            {
                t.FireGrapplingTool(target);
                sb.AppendFormat("Turret {0}:{1}, ", t, t.CurrLocalAngle);
            }
        }
        else
        {
            foreach (ITurret t in WeaponGroups.ManualTurrets)
            {
                t.FireGrapplingTool(target);
                sb.AppendFormat("Turret {0}:{1}, ", t, t.CurrLocalAngle);
            }
        }
        //Debug.Log(sb.ToString());
    }

    public virtual void MoveForeward()
    {
        if (_movementDirection == ShipDirection.Stopped)
        {
            _movementDirection = ShipDirection.Forward;
        }
        if (_movementDirection == ShipDirection.Forward)
        {
            ApplyThrust();
        }
        else if (_movementDirection == ShipDirection.Reverse)
        {
            ApplyBrakingInner();
        }
    }

    public virtual void MoveBackward()
    {
        if (_movementDirection == ShipDirection.Stopped)
        {
            _movementDirection = ShipDirection.Reverse;
        }
        if (_movementDirection == ShipDirection.Forward)
        {
            ApplyBrakingInner();
        }
        else if (_movementDirection == ShipDirection.Reverse)
        {
            ApplyThrust();
        }
    }

    protected override void ApplyBrakingInner()
    {
        base.ApplyBrakingInner();
        _brakingTargetSpeedFactor = 0.0f;
        _brakingFactor = 1.0f;
    }

    protected virtual void ApplyBrakingCoefficients(float factor, float targetSpeedFactor)
    {
        _brakingFactor = factor;
        _brakingTargetSpeedFactor = targetSpeedFactor;
    }

    public override float TargetSpeed
    {
        set
        {
            base.TargetSpeed = value;
            _thrustCoefficient = 1.0f;
        }
    }

    public bool MovingForward { get { return _movementDirection == ShipDirection.Forward; } }
    public bool MovingBackwards { get { return _movementDirection == ShipDirection.Reverse; } }

    private void InitShield()
    {
        Transform t = transform.Find("Shield");
        if (t != null)
        {
            _shieldCapsule = t.gameObject;
            if (ShipTotalShields > 0 && _shieldCapsule)
            {
                _shieldCapsule.SetActive(true);
            }
        }
    }

    public virtual int ShipTotalShields
    {
        get
        {
            return 0;
        }
    }

    public int ShipTotalMaxShields
    {
        get
        {
            return _totalMaxShield;
        }
    }

    public void DisconnectHarpaxTowing()
    {
        CableBehavior cable;
        if ((cable = TowingByHarpax) != null)
        {
            cable.DisconnectAndDestroy();
        }
    }
    protected void DisconnectHarpaxTowed()
    {
        CableBehavior cable;
        if ((cable = TowedByHarpax) != null)
        {
            cable.DisconnectAndDestroy();
        }
    }

    public CableBehavior TowingByHarpax
    {
        get
        {
            if (_towing)
            {
                return _connectedHarpax;
            }
            else
            {
                return null;
            }
        }
        set
        {
            if (value != null)
            {
                _towing = true;
                _connectedHarpax = value;
            }
            else
            {
                _connectedHarpax = null;
                _hasPrevForceTow = false;
            }
        }
    }
    public CableBehavior TowedByHarpax
    {
        get
        {
            if (!_towing)
            {
                return _connectedHarpax;
            }
            else
            {
                return null;
            }
        }
        set
        {
            if (value != null)
            {
                _towing = false;
                _connectedHarpax = value;
                _towedTime = Time.time;
            }
            else
            {
                _connectedHarpax = null;
                _hasPrevForceTow = false;
            }
        }
    }

    // Finding this entity:
    public static ShipBase FromCollider(Collider c)
    {
        ShipBase s;
        if ((s = c.GetComponent<ShipBase>()) != null || (s = c.GetComponentInParent<ShipBase>()) != null)
        {
            return s;
        }
        return null;
    }

    // Getting hit:
    public abstract void TakeHit(Warhead w, Vector3 location);

    // TargetableEntity properties:
    public Vector3 EntityLocation { get { return transform.position; } }
    public virtual bool Targetable
    {
        get
        {
            return this != null;
        }
    }

    // Things not in use, but needed in other classes:
    public virtual bool TryChangeEnergyAndHeat(int deltaEnergy, int deltaHeat) { return true; }
    public virtual void NotifyInComabt() { }

    public string ProductionKey;

    // Hull hit points:
    public int MaxHullHitPoints;
    public int HullHitPoints { get; set; }

    // Dimesions:
    public float ShipLength { get; private set; }
    public float ShipWidth { get; private set; }
    public float ShipUnscaledLength { get; private set; }
    public float ShipUnscaledWidth { get; private set; }

    // Turrets and weapons:
    public TurretControlGrouping WeaponGroups { get; private set; }
    protected ITurret[] _turrets;
    protected IEnumerable<ITurret> _manualTurrets;

    // Shields:
    protected GameObject _shieldCapsule;
    public GameObject ShieldCapsule { get { return _shieldCapsule; } }
    protected int _totalMaxShield;

    // Harpax-related fields:
    public bool GrapplingMode { get; set; }
    private CableBehavior _connectedHarpax = null;
    protected bool _towing = false;
    protected Vector3 _prevForceTow;
    protected bool _hasPrevForceTow = false;
    protected float _towedTime;

    protected Rigidbody _rigidBody;

    // Movement fields
    protected bool _autoHeading = false;
    protected Vector3 _autoHeadingVector;
    protected float _thrustCoefficient;
    protected float _brakingFactor;
    protected float _brakingTargetSpeedFactor;
    protected float _turnCoefficient;

    public Faction Owner;
}
