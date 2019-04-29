﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System;

public abstract class TurretBase : MonoBehaviour, ITurret
{

    // Use this for initialization
    protected virtual void Start()
    {
        TurretHardpoint parentHardpoint;
        if (transform.parent != null && (parentHardpoint = GetComponentInParent<TurretHardpoint>()) != null)
        {
            _minRotation = parentHardpoint.MinRotation;
            _maxRotation = parentHardpoint.MaxRotation;
            _deadZoneAngleStrings = parentHardpoint.DeadZoneAngles.ToArray();
            if (parentHardpoint.transform.lossyScale.x < 0)
            {
                _flippedX = true;
            }
            if (_maxRotation > _minRotation)
            {
                _rotationSpan = _maxRotation - _minRotation;
            }
            else
            {
                _rotationSpan = 360 - _maxRotation + _minRotation;
            }
        }
        else
        {
            _minRotation = 0.0f;
            _maxRotation = 0.0f;
            _deadZoneAngleStrings = null;
        }
        ParseDeadZones();
        ParseMuzzles();
        _containingShip = FindContainingShip(transform.parent);
        SetDefaultAngle();
        _initialized = true;
    }

    protected virtual void Awake()
    {
        ComponentHitPoints = ComponentMaxHitPoints;
        _allowedSlotTypes = new ComponentSlotType[] { TurretType };
    }

    private void ParseDeadZones()
    {
        if (_deadZoneAngleStrings != null)
        {
            _deadZoneAngleRanges = new Tuple<float, float>[_deadZoneAngleStrings.Length];
            for (int i = 0; i < _deadZoneAngleStrings.Length; ++i)
            {
                string[] nums = _deadZoneAngleStrings[i].Split(',');
                _deadZoneAngleRanges[i] = new Tuple<float, float>(float.Parse(nums[0]), float.Parse(nums[1]));
            }
        }
        if (_minRotation < _maxRotation)
        {
            _rotationAllowedRanges = new Tuple<float, float>[1]
            {
                new Tuple<float, float>(_minRotation, _maxRotation),
            };
        }
        else if (_maxRotation < _minRotation)
        {
            _rotationAllowedRanges = new Tuple<float, float>[2]
{
                new Tuple<float, float>(_minRotation, 360),
                new Tuple<float, float>(0, _maxRotation)
            };
        }
        else
        {
            _fixed = true;
            _isLegalAngle = true;
        }
    }

    protected virtual void ParseMuzzles()
    {
        List<Tuple<Transform, Transform>> barrelsFound = FindBarrels(transform).ToList();
        Barrels = new Transform[barrelsFound.Count];
        Muzzles = new Transform[barrelsFound.Count];
        _actualFiringInterval = FiringInterval / Barrels.Length;
        MuzzleFx = new ParticleSystem[barrelsFound.Count];
        for (int i = 0; i < barrelsFound.Count; ++i)
        {
            Barrels[i] = barrelsFound[i].Item1;
            Muzzles[i] = barrelsFound[i].Item2;
            MuzzleFx[i] = barrelsFound[i].Item2.GetComponentInChildren<ParticleSystem>();
        }
    }

    protected static IEnumerable<Tuple<Transform, Transform>> FindBarrels(Transform root)
    {
        if (root.name.StartsWith(BarrelString))
        {
            string suffix = root.name.Substring(BarrelString.Length);
            Transform muzzle = root.Find(MuzzleString + suffix);
            yield return new Tuple<Transform, Transform>(root, muzzle);
        }
        else
        {
            for (int i = 0; i < root.childCount; ++i)
            {
                IEnumerable<Tuple<Transform, Transform>> resInChildren = FindBarrels(root.GetChild(i));
                foreach (Tuple<Transform, Transform> r in resInChildren)
                {
                    yield return r;
                }
            }
        }
    }

    private static ShipBase FindContainingShip(Transform t)
    {
        if (t == null)
        {
            return null;
        }
        ShipBase s = t.GetComponent<ShipBase>();
        if (s != null)
        {
            return s;
        }
        return FindContainingShip(t.parent);
    }

    protected abstract void SetDefaultAngle();

    // Update is called once per frame
    protected virtual void Update()
    {
        if (_targetShip != null && Mode == TurretMode.Auto)
        {
            if (!_targetShip.Targetable || (transform.position - _targetShip.EntityLocation).sqrMagnitude > (MaxRange * 1.05f) * (MaxRange * 1.05f))
            {
                _targetShip = null;
            }
            else
            {
                Fire(_targetShip.EntityLocation);
            }
        }
    }

    public abstract void ManualTarget(Vector3 target);

    protected virtual bool CanFire()
    {
        if (!ReadyToFire())
        {
            return false;
        }
        if (_status == ComponentStatus.KnockedOut || _status == ComponentStatus.Destroyed)
        {
            return false;
        }
        if (!_isLegalAngle && Mode == TurretMode.Manual)
        {
            return false;
        }
        if (_deadZoneAngleRanges != null)
        {
            foreach (Tuple<float, float> d in _deadZoneAngleRanges)
            {
                float currAngle = CurrLocalAngle;
                if (d.Item1 < currAngle && currAngle < d.Item2)
                {
                    return false;
                }
            }
        }
        if (Mode == TurretMode.Auto)
        {
            if (CanRotate && Mathf.Abs(AngleToTargetShip - CurrAngle) > MaxAngleToTarget) //TODO: a different rule for torpedo tubes
            {
                if (!(_targetShip is Torpedo)) //TODO: needs better solution
                    return false;
            }
            Vector3 origin = Muzzles[_nextBarrel].position; //TODO: a different ray computation for torpedo tubes
            origin.y = 0;
            Vector3 firingVector = Muzzles[_nextBarrel].up;
            firingVector.y = 0;
            RaycastHit[] hits = Physics.RaycastAll(origin, firingVector, MaxRange, ObjectFactory.AllTargetableLayerMask);
            int closestHit = -1;
            for (int i = 0; i < hits.Length; ++i)
            {
                if (hits[i].collider.gameObject == ContainingShip.gameObject ||
                    (ContainingShip.ShieldCapsule != null && hits[i].collider.gameObject == ContainingShip.ShieldCapsule.gameObject))
                {
                    continue;
                }
                if (closestHit < 0 || hits[i].distance < hits[closestHit].distance)
                {
                    closestHit = i;
                }
            }
            if (closestHit >= 0 && _targetShip == Ship.FromCollider(hits[closestHit].collider))
            {
                return true;
            }
            else if (!(_targetShip is Torpedo) && !(_targetShip is StrikeCraft)) //TODO: needs better solution
            {
                return false;
            }
        }
        return true;
    }

    protected virtual bool ReadyToFire()
    {
        float currTime = Time.time;
        return currTime - _lastFire > _actualFiringInterval;
    }

    protected abstract void FireInner(Vector3 firingVector);

    protected abstract Vector3 GetFiringVector(Vector3 vecToTarget);

    protected virtual bool MuzzleOppositeDirCheck(Transform Muzzle, Vector3 vecToTarget)
    {
        return Vector3.Dot(Muzzle.up, vecToTarget) >= 0;
    }

    public void Fire(Vector3 target)
    {
        if (!CanFire())
        {
            return;
        }
        Vector3 vecToTarget = target - Muzzles[_nextBarrel].position;
        if (!MuzzleOppositeDirCheck(Muzzles[_nextBarrel], vecToTarget))
        {
            return;
        }
        Vector3 firingVector = GetFiringVector(vecToTarget);
        if (!_containingShip.TryChangeEnergyAndHeat(-EnergyToFire, HeatToFire))
        {
            return;
        }
        _lastFire = Time.time;

        FireInner(firingVector);
        _containingShip.NotifyInComabt();

        if (Muzzles.Length > 1)
        {
            _nextBarrel = (_nextBarrel + 1) % Muzzles.Length;
        }
    }

    public bool HasGrapplingTool()
    {
        return InstalledTurretMod == TurretMod.Harpax || InstalledTurretMod == TurretMod.TractorBeam;
    }

    protected abstract void FireGrapplingToolInner(Vector3 firingVector);

    public void FireGrapplingTool(Vector3 target)
    {
        if (!HasGrapplingTool())
        {
            return;
        }

        if (!CanFire())
        {
            return;
        }
        Vector3 vecToTarget = target - Muzzles[_nextBarrel].position;
        if (Vector3.Dot(Muzzles[_nextBarrel].up, vecToTarget) < 0)
        {
            return;
        }
        Vector3 firingVector = GetFiringVector(vecToTarget);
        if (!_containingShip.TryChangeEnergyAndHeat(-EnergyToFire, HeatToFire))
        {
            return;
        }
        _lastFire = Time.time;
        FireGrapplingToolInner(firingVector);
        _containingShip.NotifyInComabt();

        if (Muzzles.Length > 1)
        {
            _nextBarrel = (_nextBarrel + 1) % Muzzles.Length;
        }
    }


    public float CurrAngle { get { return FilterRotation(transform.rotation.eulerAngles); } }
    public float CurrLocalAngle
    {
        get
        {
            return GlobalDirToShipHeading(-transform.forward);
        }
    }

    /*protected float AngleToShipHeading(float globalAngle)
    {
        return AngleToShipHeading(globalAngle, false);
    }*/

    protected Vector3 DirectionToLocal(Vector3 dir)
    {
        return DirectionToLocal(dir, false);
    }

    protected Vector3 DirectionToLocal(Vector3 dir, bool clean)
    {
        return _containingShip.transform.InverseTransformDirection(dir.x,
                                                                   clean ? 0 : dir.y,
                                                                   dir.z);
    }

    protected Vector3 DirectionToGlobal(Vector3 dir)
    {
        return DirectionToGlobal(dir, false);
    }

    protected Vector3 DirectionToGlobal(Vector3 dir, bool clean)
    {
        Vector3 res = _containingShip.transform.TransformDirection(dir);
        if (clean)
        {
            res.y = 0;
        }
        return res;
    }

    protected float LocalDirToShipHeading(Vector3 dir)
    {
        Vector3 localDir = DirectionToLocal(dir);
        Vector3 flatDir = Vector3.ProjectOnPlane(localDir, Vector3.forward);
        return Vector3.SignedAngle(Vector3.up, flatDir, Vector3.forward) + 180;
    }

    protected float GlobalDirToShipHeading(Vector3 dir)
    {
        Vector3 flatDir = Vector3.ProjectOnPlane(dir, _containingShip.transform.forward);
        return Vector3.SignedAngle(_containingShip.transform.up, flatDir, _containingShip.transform.forward) + 180;
    }

    /*protected float AngleToGlobal(float angle)
    {
        Quaternion rot = Quaternion.AngleAxis(angle, _containingShip.transform.forward);
        Quaternion worldDir = Quaternion.LookRotation(Vector3.right);
        return Quaternion.Angle(rot, worldDir);
    }*/

    /*protected float AngleToShipHeading(float globalAngle, bool inverse)
    {
        Vector3 forwardClean = Vector3.forward;
        forwardClean.y = 0;
        Vector3 shipHeadingClean = _containingShip.transform.up;
        shipHeadingClean.y = 0;
        float angleOffset = Quaternion.FromToRotation(shipHeadingClean, forwardClean).eulerAngles.y;
        if (inverse)
        {
            angleOffset = -angleOffset;
        }
        float finalAngle = globalAngle + angleOffset;
        if (finalAngle > 360f)
        {
            finalAngle -= 360f;
        }
        else if (finalAngle < 0)
        {
            finalAngle += 360;
        }
        return finalAngle;
    }*/

    private float AngleToTargetShip
    {
        get
        {
            if (_targetShip == null)
            {
                return 0;
            }
            Vector3 vecToTargetShip = _targetShip.EntityLocation - transform.position;
            vecToTargetShip.y = 0;
            return Quaternion.LookRotation(-vecToTargetShip).eulerAngles.y;
        }
    }

    private float FilterRotation(Vector3 rot)
    {
        switch (TurretAxis)
        {
            case RotationAxis.XAxis:
                return rot.x;
            case RotationAxis.YAxis:
                return rot.y;
            case RotationAxis.ZAxis:
                return rot.z;
            default:
                return rot.y; // This is the most common.
        }
    }

    protected Vector3 TurretAxisVector
    {
        get
        {
            switch (TurretAxis)
            {
                case RotationAxis.XAxis:
                    return Vector3.right;
                case RotationAxis.YAxis:
                    return Vector3.up;
                case RotationAxis.ZAxis:
                    return Vector3.forward;
                default:
                    return Vector3.up; // This is the most common.
            }
        }
    }

    public void SetTurretBehavior(TurretMode newMode)
    {
        if (Mode == newMode)
        {
            return;
        }
        if ((Mode == TurretMode.Off || Mode == TurretMode.Manual) && newMode == TurretMode.Auto)
        {
            Mode = newMode;
            StartCoroutine(TurretAutoBehavior());
        }
        else
        {
            Mode = newMode;
        }
    }

    public TurretMode GetTurretBehavior()
    {
        return Mode;
    }

    private IEnumerator TurretAutoBehavior()
    {
        yield return new WaitUntil(() => _containingShip != null);
        while (true)
        {
            switch (Mode)
            {
                case TurretMode.Off:
                    yield break;
                case TurretMode.Manual:
                    yield break;
                case TurretMode.Auto:
                    if (_targetShip == null || !_targetShip.Targetable)
                    {
                        _targetShip = AcquireTarget();
                    }
                    if (_targetShip != null)
                    {
                        ManualTarget(_targetShip.EntityLocation);
                    }
                    else
                    {
                        ManualTarget(transform.position + _containingShip.transform.TransformDirection(_defaultDirection));
                        Debug.DrawLine(transform.position, transform.position + (_containingShip.transform.TransformDirection(_defaultDirection) * 1), Color.magenta, 0.1f);
                    }
                    break;
                default:
                    break;
            }
            yield return new WaitForSeconds(0.1f);
        }
    }

    protected abstract ITargetableEntity AcquireTarget();

    public virtual bool IsTurretModCombatible(TurretMod m)
    {
        return m == TurretMod.None;
    }

    public TurretMod InstalledTurretMod
    {
        get
        {
            return _turretMod;
        }
        set
        {
            if (IsTurretModCombatible(value))
            {
                _turretMod = value;
            }
        }
    }
    protected TurretMod _turretMod;

    protected bool _initialized = false; // ugly hack

    // Rotation behavior variables:
    protected float _minRotation, _maxRotation, _rotationSpan;
    protected Tuple<float, float>[] _rotationAllowedRanges;
    private bool _fixed = false;
    public float RotationSpeed;
    protected float _targetAngle;
    protected Vector3 _vectorToTarget;

    protected Vector3 _defaultDirection;
    private string[] _deadZoneAngleStrings;
    private Tuple<float, float>[] _deadZoneAngleRanges;
    public RotationAxis TurretAxis;
    protected bool _isLegalAngle = false;
    protected bool CanRotate { get { return (!_fixed) && _status != ComponentStatus.HeavilyDamaged && _status != ComponentStatus.KnockedOut && _status != ComponentStatus.Destroyed; } }

    // Barrels, muzzles, and muzzleFx data:
    protected Transform[] Barrels;
    protected Transform[] Muzzles;
    protected ParticleSystem[] MuzzleFx;
    protected int _nextBarrel = 0;

    // Weapon data:
    public ComponentSlotType TurretType;
    public float MaxRange;
    public float FiringInterval;
    protected float _actualFiringInterval;
    public ObjectFactory.WeaponSize TurretSize;
    public ObjectFactory.WeaponType TurretWeaponType;

    // Turret status:
    public int IsJammed { get; private set; }

    // Fire delay
    protected float _lastFire = 0.0f;
    public int EnergyToFire;
    public int HeatToFire;

    // Auto control
    public TurretMode Mode { get; set; }

    private ITargetableEntity _targetShip = null;

    public int MaxHitpoints;
    private int _currHitPoints;
    private ComponentStatus _status;

    public ShipBase ContainingShip { get { return _containingShip; } }

    // The ship containing the turret:
    protected ShipBase _containingShip;

    public enum TurretMode { Off, Manual, Auto };
    public enum RotationAxis { XAxis, YAxis, ZAxis };

    protected static readonly string BarrelString = "Barrel";
    protected static readonly string MuzzleString = "Muzzle";

    public event ComponentHitpointsChangedDelegate OnHitpointsChanged;

    // Hit point stuff:
    public virtual int ComponentMaxHitPoints
    {
        get
        {
            return MaxHitpoints;
        }
        protected set
        {
            if (value > 0)
            {
                MaxHitpoints = value;
            }
        }
    }
    public virtual int ComponentHitPoints
    {
        get
        {
            return _currHitPoints;
        }
        set
        {
            _currHitPoints = System.Math.Max(0, value);
            if (_currHitPoints == 0)
            {
                Status = ComponentStatus.Destroyed;
            }
            else if (_currHitPoints <= ComponentMaxHitPoints / 10)
            {
                Status = ComponentStatus.KnockedOut;
            }
            else if (_currHitPoints <= ComponentMaxHitPoints / 4)
            {
                Status = ComponentStatus.HeavilyDamaged;
                Debug.Log(string.Format("{0} is heavily damaged. Time: {1}", this, Time.time));
            }
            else if (_currHitPoints <= ComponentMaxHitPoints / 2)
            {
                Status = ComponentStatus.LightlyDamaged;
            }
            else
            {
                Status = ComponentStatus.Undamaged;
            }
            if (OnHitpointsChanged != null)
            {
                OnHitpointsChanged();
            }
        }
    }
    public virtual bool ComponentIsWorking
    {
        get
        {
            switch (Status)
            {
                case ComponentStatus.Undamaged:
                    return true;
                case ComponentStatus.LightlyDamaged:
                    return true;
                case ComponentStatus.HeavilyDamaged:
                    return true;
                case ComponentStatus.KnockedOut:
                    return false;
                case ComponentStatus.Destroyed:
                    return false;
                default:
                    return false;
            }
        }
    }
    public virtual ComponentStatus Status
    {
        get
        {
            return _status;
        }
        protected set
        {
            _status = value;
        }
    }

    protected virtual float MaxAngleToTarget => 2.0f;

    public float GetMaxRange { get { return MaxRange; } }

    public string SpriteKey { get { return "Turret"; } }

    public IEnumerable<ComponentSlotType> AllowedSlotTypes { get { return _allowedSlotTypes; } }

    public ObjectFactory.ShipSize MinShipSize { get { return ObjectFactory.ShipSize.Sloop; } }
    public ObjectFactory.ShipSize MaxShipSize { get { return ObjectFactory.ShipSize.CapitalShip; } }

    private ComponentSlotType[] _allowedSlotTypes;

    protected bool _flippedX = false;
}
