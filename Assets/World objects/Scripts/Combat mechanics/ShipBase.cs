using System.Collections.Generic;
using System.Text;
using UnityEngine;
using System.Linq;
using System;

[RequireComponent(typeof(Rigidbody))]
public abstract class ShipBase : MovementBase, ITargetableEntity
{
    protected override void Awake()
    {
        base.Awake();
        WeaponGroups = null;
        TowingByHarpax = null;
        TowedByHarpax = null;
        GrapplingMode = false;
        _rigidBody = GetComponent<Rigidbody>();
        UseTargetSpeed = false;
        _circleStatus = ShipCircleStatus.Deselected;
        _speedOnTurningCoefficient = 1f;
    }

    public override void PostAwake()
    {
        ComputeLength();
        HullHitPoints = MaxHullHitPoints;
    }

    public virtual void Activate()
    {
        FindTurrets();
        InitShield();
        InitCircle();
        ShipDisabled = false;
        ShipImmobilized = false;
        CombinedBuff = DynamicBuff.Default();

        Transform navBox = transform.Find("NavBox");
        if (navBox != null && HullObject != null)
        {
            Bounds bbox = HullObject.GetComponent<MeshFilter>().mesh.bounds;
            Vector3 tmpSz = bbox.size * NavBoxExpandFactor * HullObject.localScale.x;
            bbox.size = new Vector3(tmpSz.x, tmpSz.z, tmpSz.y);
            /* Old box collider implemetation: */
            /*
            BoxCollider navBoxCollider = navBox.gameObject.AddComponent<BoxCollider>();
            navBoxCollider.center = bbox.center;
            navBoxCollider.size = bbox.size;
            navBoxCollider.isTrigger = true;
            */

            /* New sphere collider implemetation: */
            SphereCollider navSphereCollider = navBox.gameObject.AddComponent<SphereCollider>();
            navSphereCollider.center = bbox.center;
            navSphereCollider.radius = Mathf.Max(bbox.size.x, Mathf.Max(bbox.size.y, bbox.size.z)) / 2f;
        }
    }

    protected override void ApplyMovementManual()
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
        Vector3 targetVelocity = (ActualVelocity = directionMult * _speed * transform.forward);// was: Time.deltaTime * (ActualVelocity = directionMult * _speed * transform.up);
        Vector3 rbVelocity = _rigidBody.velocity;
        if (TowedByHarpax != null)
        {
            _prevForceTow = (targetVelocity - rbVelocity) * _rigidBody.mass;
            _needsApplyForce = true;
            _forceMode = ForceMode.Impulse;
            if (!_hasPrevForceTow)
            {
                //_rigidBody.AddForce((targetVelocity - rbVelocity) * _rigidBody.mass, ForceMode.Impulse);
                _forceVec = (targetVelocity - rbVelocity) * _rigidBody.mass;
            }
            else
            {
                //_rigidBody.AddForce((targetVelocity - rbVelocity) * _rigidBody.mass - _prevForceTow, ForceMode.Impulse);
                _forceVec = (targetVelocity - rbVelocity) * _rigidBody.mass - _prevForceTow;
            }
            _hasPrevForceTow = true;
        }
        else if (_movementDirection == ShipDirection.Stopped)
        {
            _rigidBody.angularVelocity = Vector3.zero;
            _rigidBody.velocity = Vector3.zero;
            _needsApplyForce = false;
        }
        else
        {
            _needsApplyForce = true;
            _forceMode = ForceMode.VelocityChange;
            _forceVec = targetVelocity - rbVelocity;
            //_rigidBody.AddForce((targetVelocity - rbVelocity), ForceMode.VelocityChange);
            //_rigidBody.velocity = targetVelocity;
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

    protected virtual void FixedUpdate()
    {
        if (_needsApplyForce)
        {
            _rigidBody.AddForce(_forceVec, _forceMode);
        }
        _needsApplyForce = false;
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

            _speed = Mathf.Min(_speed + AccelerationWBuf * _thrustCoefficient * Time.deltaTime, MaxSpeedWBuf);
        }
        else if (_nextBrake)
        {
            _nextAccelerate = _nextBrake = false;

            if (!CanDoBraking())
                return;

            float targetSpeed = MaxSpeedWBuf * Mathf.Clamp01(_brakingTargetSpeedFactor);
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
            float actualTargetSpeed = Mathf.Clamp(TargetSpeed, 0, MaxSpeedWBuf);
            if (_speed < actualTargetSpeed)
            {
                if (!CanDoAcceleration())
                    return;

                _speed = Mathf.Min(_speed + AccelerationWBuf * _thrustCoefficient * Time.deltaTime, actualTargetSpeed);
            }
            else if (_speed > actualTargetSpeed)
            {
                if (!CanDoBraking())
                    return;

                float targetSpeedBraking = Mathf.Min(actualTargetSpeed, MaxSpeedWBuf * Mathf.Clamp01(_brakingTargetSpeedFactor));
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
        bool thisLeft = _nextTurnLeft, thisTurnToDir = _nextTurnToDir;
        _nextTurnLeft = _nextTurnRight = _nextTurnToDir = false;

        if (!CanDoTurning())
            return;

        if (thisTurnToDir)
        {
            Vector3 newHeading = Vector3.RotateTowards(transform.forward, _nextTurnTarget, TurnRate * Time.deltaTime * Mathf.Deg2Rad, 0f);
            transform.rotation = Quaternion.LookRotation(newHeading, Vector3.up);
        }
        else
        {
            float turnFactor = 1.0f;
            if (thisLeft)
            {
                turnFactor = -1.0f;
            }
            Quaternion deltaRot = Quaternion.AngleAxis(turnFactor * TurnRate * Time.deltaTime, Vector3.up);
            transform.rotation = deltaRot * transform.rotation;
        }
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

    public override void StartManeuver(Maneuver m)
    {
        _rigidBody.isKinematic = true;
        _rigidBody.velocity = Vector3.zero;
        base.StartManeuver(m);
        m.OnManeuverFinish += delegate (Maneuver mm)
        {
            float dirDot = Vector3.Dot(mm.Velocity, transform.up);
            if (dirDot > 0)
            {
                _movementDirection = ShipDirection.Forward;
            }
            else if (dirDot < 0)
            {
                _movementDirection = ShipDirection.Reverse;
            }
            else
            {
                _movementDirection = ShipDirection.Stopped;
            }
            _speed = mm.Velocity.magnitude;
            _rigidBody.isKinematic = false;
            _rigidBody.velocity = mm.Velocity;
        };
    }

    public bool ShipDisabled { get; protected set; }
    public bool ShipImmobilized { get; protected set; }
    public virtual bool ShipControllable { get { return ShipActiveInCombat && !(ShipImmobilized); } }
    public virtual bool ShipActiveInCombat { get { return (!ShipDisabled) && HullHitPoints > 0; } }

    private void ComputeLength()
    {
        if (HullObject != null)
        {
            Mesh m = HullObject.GetComponent<MeshFilter>().mesh;
            ShipUnscaledLength = m.bounds.size.z;
            ShipUnscaledWidth = m.bounds.size.x;

            ShipLength = ShipUnscaledLength * HullObject.lossyScale.z;
            ShipWidth = ShipUnscaledWidth * HullObject.lossyScale.x;
        }
        else
        {
            ShipLength = ShipWidth = 0f;
        }
    }

    public override float ObjectSize => Mathf.Max(ShipWidth, ShipLength);

    private void InitCircle()
    {
        _statusCircle = GetComponentInChildren<LineRenderer>();
        if (_statusCircle != null)
        {
            int numPts = 48;
            _statusCircle.positionCount = numPts;
            _statusCircle.loop = true;
            float angleStep = Mathf.PI * 2 / numPts;
            float r = ShipUnscaledLength * 0.5f * 0.85f;
            _statusCircle.SetPositions(Enumerable.Range(0, numPts).Select(i => new Vector3(r * Mathf.Cos(i * angleStep), r * Mathf.Sin(i * angleStep), 0)).ToArray());
        }
    }

    public void SetCircleSelectStatus(bool selected)
    {
        if (selected && _circleStatus == ShipCircleStatus.Deselected)
        {
            _circleStatus = ShipCircleStatus.Selected;
            _statusCircle.sharedMaterial = ObjectFactory.GetMaterial("ShipRingSelectedMtl");
        }
        else if (!selected && _circleStatus == ShipCircleStatus.Selected)
        {
            _circleStatus = ShipCircleStatus.Deselected;
            _statusCircle.sharedMaterial = ObjectFactory.GetMaterial("ShipRingDeselectedMtl");
        }
    }

    protected void SetCircleStatus(ShipCircleStatus s)
    {
        switch (s)
        {
            case ShipCircleStatus.Deselected:
                _statusCircle.sharedMaterial = ObjectFactory.GetMaterial("ShipRingDeselectedMtl");
                break;
            case ShipCircleStatus.Selected:
                _statusCircle.sharedMaterial = ObjectFactory.GetMaterial("ShipRingSelectedMtl");
                break;
            case ShipCircleStatus.Disabled:
                break;
            case ShipCircleStatus.Surrendered:
                _statusCircle.sharedMaterial = ObjectFactory.GetMaterial("ShipRingSurrenderMtl");
                //_statusCircle.startColor = Color.white;
                _statusCircle.endColor = Color.white;
                break;
            case ShipCircleStatus.Destroyed:
                break;
            default:
                break;
        }
    }

    public void SetCircleToFactionColor()
    {
        if (Owner != null)
        {
            if (TeamColorComponents != null)
            {
                foreach (MeshRenderer mr in TeamColorComponents)
                {
                    mr.material.color = Owner.FactionColor;
                }
            }
            if (_statusCircle != null)
            {
                _statusCircle.startColor = Owner.FactionColor;
                _statusCircle.endColor = Owner.FactionColor;
            }
        }
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
        if (hp == null || t == null || !hp.AllowedWeaponTypes.Contains(t.SlotType))
        {
            return false;
        }
        TurretBase existingTurret = hp.GetComponentInChildren<TurretBase>();
        if (existingTurret != null)
        {
            return false;
        }
        Quaternion q = Quaternion.LookRotation(hp.transform.forward, hp.transform.up);
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

        t.PostInstallTurret(this);

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
            for (int i = 0; i < _turrets.Length; ++i)
            {
                _turrets[i].ManualTarget(target);
            }
        }
        else
        {
            IReadOnlyList<ITurret> currManualTurrets = WeaponGroups.ManualTurrets;
            for (int i = 0; i < currManualTurrets.Count; ++i)
            {
                currManualTurrets[i].ManualTarget(target);
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
        //StringBuilder sb = new StringBuilder();
        if (WeaponGroups == null)
        {
            for (int i = 0; i < _turrets.Length; ++i)
            {
                if (_turrets[i].GetTurretBehavior() == TurretBase.TurretMode.Manual)
                {
                    _turrets[i].Fire(target);
                    //sb.AppendFormat("Turret {0}:{1}, ", t, t.CurrLocalAngle);
                }
            }
        }
        else
        {
            IReadOnlyList<ITurret> currManualTurrets = WeaponGroups.ManualTurrets;
            for (int i = 0; i < currManualTurrets.Count; ++i)
            {
                currManualTurrets[i].Fire(target);
                //sb.AppendFormat("Turret {0}:{1}, ", currManualTurrets[i], currManualTurrets[i].CurrLocalAngle);
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
                sb.AppendFormat("Turret {0}:{1}, ", t, t.CurrAngle);
            }
        }
        else
        {
            foreach (ITurret t in WeaponGroups.ManualTurrets)
            {
                t.FireGrapplingTool(target);
                sb.AppendFormat("Turret {0}:{1}, ", t, t.CurrAngle);
            }
        }
        //Debug.Log(sb.ToString());
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
            Physics.IgnoreCollision(GetComponent<Collider>(), t.GetComponent<Collider>());
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
            cable.DisconnectAndRecycle();
        }
    }
    protected void DisconnectHarpaxTowed()
    {
        CableBehavior cable;
        if ((cable = TowedByHarpax) != null)
        {
            cable.DisconnectAndRecycle();
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

    public static float FormationHalfSpacing(ShipBase s)
    {
        return s.ObjectSize * GlobalDistances.ShipFormationHalfSpacingFactor;
    }

    // Getting hit:
    public abstract void TakeHit(Warhead w, Vector3 location);

    public virtual ITargetableEntity PreferredTarget
    {
        get
        {
            if (_shipAI == null)
                return null;

            return _shipAI.GetTargetShip();
        }
    }

    // TargetableEntity properties:
    public Vector3 EntityLocation { get { return transform.position; } }
    public virtual bool Targetable
    {
        get
        {
            return this != null;
        }
    }
    public abstract TargetableEntityInfo TargetableBy
    {
        get;
    }

    // Behavior in a formation:
    public FormationBase ContainingFormation { get; protected set; }

    public abstract void AddToFormation(FormationBase f);

    public abstract void RemoveFromFormation();

    public Vector3 PositionInFormation
    {
        get
        {
            return ContainingFormation.GetPosition(this);
        }
    }

    public abstract bool InPositionInFormation();

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

    // Attack range, for AI:
    public float TurretsGetAttackRange(Func<ITurret, bool> turretFilter)
    {
        return TurretsGetAttackRange(turretFilter, true);
    }
    public float TurretsGetAttackRange(Func<ITurret, bool> turretFilter, bool min)
    {
        float range = -1f;
        for (int i = 0; i < _turrets.Length; ++i)
        {
            if (_turrets[i] != null && turretFilter(_turrets[i]))
            {
                float currRange = _turrets[i].GetMaxRange;
                if (range < 0f || (min && range < currRange) || (!min && range > currRange))
                {
                    range = currRange;
                }
            }
        }
        return range;
    }

    public bool TurretsAllReadyToFire(Func<ITurret, bool> turretFilter)
    {
        for (int i = 0; i < _turrets.Length; ++i)
        {
            if (_turrets[i] != null && _turrets[i].ComponentIsWorking && turretFilter(_turrets[i]) && !_turrets[i].ReadyToFire())
            {
                return false;
            }
        }
        return true;
    }

    // Things not in use, but needed in other classes:
    public virtual bool TryChangeEnergyAndHeat(int deltaEnergy, int deltaHeat) { return true; }
    public virtual bool TryChangeEnergyAndHeat(int deltaEnergy, int deltaHeat, bool allowEnergyOverflow, bool allowHeatUndeflow) { return true; }
    public virtual void NotifyInComabt() { }
    public abstract ObjectFactory.TacMapEntityType TargetableEntityType { get; }

    public Transform HullObject;

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
    protected float _speedOnTurningCoefficient;
    protected bool _needsApplyForce = false;
    protected ForceMode _forceMode;
    protected Vector3 _forceVec;

    public Faction Owner;

    // Team color
    public MeshRenderer[] TeamColorComponents;
    private LineRenderer _statusCircle;
    private ShipCircleStatus _circleStatus;

    protected enum ShipCircleStatus { Deselected, Selected, Disabled, Surrendered, Destroyed };

    // Buff/debuff mechanic
    public DynamicBuff CombinedBuff { get; protected set; }
    protected float MaxSpeedWBuf => Mathf.Max(MaxSpeed * 0.25f, MaxSpeed * (1f + CombinedBuff.SpeedFactor));
    protected float AccelerationWBuf => Mathf.Max(Thrust * 0.25f, Thrust * (1f + CombinedBuff.AcceleraionFactor));

    // AI:
    protected ShipAIHandle _shipAI;

    private static readonly float NavBoxExpandFactor = 1.1f;
}
