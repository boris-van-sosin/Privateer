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
            _treatAsFixed = parentHardpoint.TreatAsFixed;
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
            HardpointAIHint = parentHardpoint.WeaponAIHint;
        }
        else
        {
            _minRotation = 0.0f;
            _maxRotation = 0.0f;
            _deadZoneAngleStrings = null;
            HardpointAIHint = TurretAIHint.Main;
        }
        _containingShip = FindContainingShip(transform.parent);
        ParseDeadZones();
        ParseMuzzles();
        AlternatingFire = DefaultAlternatingFire;
        SetDefaultAngle();
        LastFire = 0f;
        _initialized = true;
        _firingIntervalCoeff = 1f;
        _vsStrikeCraftModifier = 0f;
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
            _deadZoneAngleRanges = new ValueTuple<float, float>[_deadZoneAngleStrings.Length];
            for (int i = 0; i < _deadZoneAngleStrings.Length; ++i)
            {
                string[] nums = _deadZoneAngleStrings[i].Split(',');
                _deadZoneAngleRanges[i] = new ValueTuple<float, float>(float.Parse(nums[0]), float.Parse(nums[1]));
            }
        }
        if (_minRotation < _maxRotation)
        {
            _rotationAllowedRanges = new ValueTuple<float, float>[1]
            {
                new ValueTuple<float, float>(_minRotation, _maxRotation),
            };
        }
        else if (_maxRotation < _minRotation)
        {
            _rotationAllowedRanges = new ValueTuple<float, float>[2]
{
                new ValueTuple<float, float>(_minRotation, 360),
                new ValueTuple<float, float>(0, _maxRotation)
            };
        }
        else
        {
            _fixed = true;
            _isLegalFireAngle = _isLegalAimAngle = true;
        }
    }

    protected virtual void ParseMuzzles()
    {
        List<ValueTuple<Transform, Transform>> barrelsFound = FindBarrels(transform).ToList();
        Barrels = new Transform[barrelsFound.Count];
        Muzzles = new Transform[barrelsFound.Count];
        if (AlternatingFire)
        {
            ActualFiringInterval = FiringInterval / Barrels.Length;
        }
        else
        {
            ActualFiringInterval = FiringInterval;
        }
        MuzzleFx = new ParticleSystem[barrelsFound.Count];
        for (int i = 0; i < barrelsFound.Count; ++i)
        {
            Barrels[i] = barrelsFound[i].Item1;
            Muzzles[i] = barrelsFound[i].Item2;
            MuzzleFx[i] = barrelsFound[i].Item2.GetComponentInChildren<ParticleSystem>();
        }
    }

    protected static IEnumerable<ValueTuple<Transform, Transform>> FindBarrels(Transform root)
    {
        if (root.name.StartsWith(BarrelString))
        {
            string suffix = root.name.Substring(BarrelString.Length);
            Transform muzzle = root.Find(MuzzleString + suffix);
            yield return new ValueTuple<Transform, Transform>(root, muzzle);
        }
        else
        {
            for (int i = 0; i < root.childCount; ++i)
            {
                IEnumerable<ValueTuple<Transform, Transform>> resInChildren = FindBarrels(root.GetChild(i));
                foreach (ValueTuple<Transform, Transform> r in resInChildren)
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
            if (!_targetShip.Targetable || (transform.position - _targetShip.EntityLocation).sqrMagnitude > (MaxRange * GlobalDistances.TurretTargetAcquisitionRangeFactor) * (MaxRange * GlobalDistances.TurretTargetAcquisitionRangeFactor))
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

    protected abstract bool TargetInFiringArc(Vector3 target, float tolerance);

    protected virtual bool CanFire()
    {
        if (!ReadyToFire())
        {
            //Debug.Log(string.Format("{0} on {1} not ready to fire", this, _containingShip));
            return false;
        }
        if (_status == ComponentStatus.KnockedOut || _status == ComponentStatus.Destroyed)
        {
            return false;
        }
        if (!_isLegalAimAngle && Mode == TurretMode.Manual)
        {
            return false;
        }
        if (_deadZoneAngleRanges != null)
        {
            foreach (ValueTuple<float, float> d in _deadZoneAngleRanges)
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
            if (CanRotate && !IsAimedAtTarget())
            {
                if (!TargetableEntityUtils.IsTargetable(_targetShip.TargetableBy, TargetableEntityInfo.AntiTorpedo))
                    return false;
            }
            Vector3 origin = Muzzles[_nextBarrel].position; //TODO: a different ray computation for torpedo tubes
            origin.y = 0;
            Vector3 firingVector = Muzzles[_nextBarrel].up;
            firingVector.y = 0;
            RaycastHit[] hits = Physics.RaycastAll(origin, firingVector, MaxRange, ObjectFactory.NavBoxesAllLayerMask);
            int closestHit = -1;
            for (int i = 0; i < hits.Length; ++i)
            {
                ShipBase shipDetected = ShipBase.FromCollider(hits[i].collider);
                if (shipDetected == ContainingShip)
                {
                    continue;
                }
                if (closestHit < 0 || hits[i].distance < hits[closestHit].distance)
                {
                    closestHit = i;
                }
            }
            if (closestHit >= 0 && _targetShip.Equals(ShipBase.FromCollider(hits[closestHit].collider)))
            {
                return true;
            }
            else if (!TargetableEntityUtils.IsTargetable(_targetShip.TargetableBy, TargetableEntityInfo.Flak))
            {
                return false;
            }
        }
        return true;
    }

    public virtual bool ReadyToFire()
    {
        float currTime = Time.time;
        return currTime - LastFire > ActualFiringInterval * _firingIntervalCoeff;
    }

    protected abstract void FireInner(Vector3 firingVector, int barrelIdx);

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
        if (AlternatingFire)
        {
            bool fired = FireFromBarrel(target, _nextBarrel);
            if (fired)
            {
                LastFire = Time.time;
                _containingShip.NotifyInComabt();
                if (Muzzles.Length > 1)
                {
                    _nextBarrel = (_nextBarrel + 1) % Muzzles.Length;
                }
            }
        }
        else
        {
            StartCoroutine(FireFull(target));
        }
    }

    protected bool FireFromBarrel(Vector3 target, int barrelIdx)
    {
        Vector3 vecToTarget = target - Muzzles[barrelIdx].position;
        if (!MuzzleOppositeDirCheck(Muzzles[barrelIdx], vecToTarget))
        {
            return false;
        }
        Vector3 firingVector = GetFiringVector(vecToTarget);
        if (!_containingShip.TryChangeEnergyAndHeat(-EnergyToFire, HeatToFire))
        {
            //Debug.Log(string.Format("{0} on {1} not enough heat or energy to fire", this, _containingShip));
            return false;
        }

        FireInner(firingVector, barrelIdx);
        return true;
    }

    protected IEnumerator FireFull(Vector3 target)
    {
        bool firedAny = false;
        for (int i = 0; i < Muzzles.Length; ++i)
        {
            bool fired = FireFromBarrel(target, i);
            if (fired && !firedAny)
            {
                firedAny = true;
                LastFire = Time.time;
                _containingShip.NotifyInComabt();
            }
            yield return _fullBroadsideDelay;
        }
    }

    protected virtual bool IsAimedAtTarget()
    {
        return Mathf.Abs(AngleToTargetShip - CurrAngle) <= AIMaxAngleToTarget;
    }

    public bool HasGrapplingTool()
    {
        return InstalledTurretMod == TurretMod.Harpax || InstalledTurretMod == TurretMod.TractorBeam;
    }

    protected abstract void FireGrapplingToolInner(Vector3 firingVector, int barrelIdx);

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
        LastFire = Time.time;
        FireGrapplingToolInner(firingVector, _nextBarrel);
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
        Vector3 flatDir = Vector3.ProjectOnPlane(dir, _containingShip.transform.up);
        return Vector3.SignedAngle(_containingShip.transform.forward, flatDir, _containingShip.transform.up) + 180;
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
                    if (_targetShip == null || !_targetShip.Targetable || Time.time > _retargetTime)
                    {
                        _targetShip = AcquireTarget();
                        _retargetTime = Time.time + UnityEngine.Random.Range(1f, 2f);
                    }
                    if (_targetShip != null)
                    {
                        ManualTarget(_targetShip.EntityLocation);
                    }
                    else
                    {
                        ManualTarget(transform.position + _containingShip.transform.TransformDirection(_defaultDirection));
                        //Debug.DrawLine(transform.position, transform.position + (_containingShip.transform.TransformDirection(_defaultDirection) * 1), Color.magenta, 0.1f);
                    }
                    break;
                default:
                    break;
            }
            yield return _autoBehaviorPulseDelay;
        }
    }

    protected abstract ITargetableEntity AcquireTarget();

    protected int TargetScore(ITargetableEntity target)
    {
        int score = 0;
        if (TargetPriorityList != null)
        {
            int idx = TargetPriorityList.IndexOf(target.TargetableEntityType);
            if (idx != -1)
            {
                score += TargetPriorityList.Count - idx;
            }
        }
        // Prefer targets within the firing arc of the weapon:
        if (TargetInFiringArc(target.EntityLocation, _AIFiringArcTolerance))
        {
            score += _AIFiringArcScoreBonus;
        }

        // Prefer the "main target" for this ship (according to the ship AI), if applicable:
        if (ContainingShip != null && ContainingShip.PreferredTarget != null && ContainingShip.PreferredTarget == target)
        {
            score += _AIPreferredTargetScoreBonus;
        }

        // If the weapon already has a target, prefer it:
        if (_targetShip != null && _targetShip == target)
        {
            score += _AICurrTargetScoreBonus;
        }
        return score;
    }

    public virtual bool IsOutOfAmmo => false;

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

    public bool AlternatingFire
    {
        get { return _alnternatingFire; }
        set
        {
            _alnternatingFire = value;
            if (_alnternatingFire)
            {
                ActualFiringInterval = FiringInterval / Muzzles.Length;
            }
            else
            {
                ActualFiringInterval = FiringInterval;
            }
        }
    }
    public bool DefaultAlternatingFire;

    public virtual void ApplyBuff(Buff b)
    {
        _firingIntervalCoeff = Mathf.Clamp(1f / (1f + b.WeaponRateOfFireFactor), 0.5f, 2f);
    }

    public void ApplyHitPointBuff(Buff b)
    {
        if (ComponentHitPoints > 0)
        {
            switch (TurretType)
            {
                case ComponentSlotType.SmallBroadside:
                case ComponentSlotType.SmallBarbetteDual:
                    ComponentMaxHitPoints += b.HitPointModifiers.SmallBroadside;
                    ComponentHitPoints += b.HitPointModifiers.SmallBroadside;
                    break;
                case ComponentSlotType.SmallFixed:
                case ComponentSlotType.SmallBarbette:
                    ComponentMaxHitPoints += b.HitPointModifiers.SmallBarbette;
                    ComponentHitPoints += b.HitPointModifiers.SmallBarbette;
                    break;
                case ComponentSlotType.SmallTurret:
                case ComponentSlotType.SmallTurretDual:
                    ComponentMaxHitPoints += b.HitPointModifiers.SmallTurret;
                    ComponentHitPoints += b.HitPointModifiers.SmallTurret;
                    break;
                case ComponentSlotType.MediumBroadside:
                    ComponentMaxHitPoints += b.HitPointModifiers.MediumBroadside;
                    ComponentHitPoints += b.HitPointModifiers.MediumBroadside;
                    break;
                case ComponentSlotType.MediumBarbette:
                case ComponentSlotType.MediumBarbetteDualSmall:
                    ComponentMaxHitPoints += b.HitPointModifiers.MediumBarbette;
                    ComponentHitPoints += b.HitPointModifiers.MediumBarbette;
                    break;
                case ComponentSlotType.MediumTurret:
                case ComponentSlotType.MediumTurretDualSmall:
                    ComponentMaxHitPoints += b.HitPointModifiers.MediumTurret;
                    ComponentHitPoints += b.HitPointModifiers.MediumTurret;
                    break;
                case ComponentSlotType.LargeBarbette:
                    ComponentMaxHitPoints += b.HitPointModifiers.HeavyBarbette;
                    ComponentHitPoints += b.HitPointModifiers.HeavyBarbette;
                    break;
                case ComponentSlotType.LargeTurret:
                    ComponentMaxHitPoints += b.HitPointModifiers.HeavyTurret;
                    ComponentHitPoints += b.HitPointModifiers.HeavyTurret;
                    break;
                case ComponentSlotType.TorpedoTube:
                    ComponentMaxHitPoints += b.HitPointModifiers.TorpedoTube;
                    ComponentHitPoints += b.HitPointModifiers.TorpedoTube;
                    break;
                default:
                    break;
            }
        }
    }

    protected bool _initialized = false; // ugly hack

    // Rotation behavior variables:
    protected float _minRotation, _maxRotation, _rotationSpan;
    protected ValueTuple<float, float>[] _rotationAllowedRanges;
    protected bool _treatAsFixed = false;
    private bool _fixed = false;
    public float RotationSpeed;
    protected float _targetAngle;
    protected Vector3 _vectorToTarget;

    protected Vector3 _defaultDirection;
    private string[] _deadZoneAngleStrings;
    private ValueTuple<float, float>[] _deadZoneAngleRanges;
    public RotationAxis TurretAxis;
    protected bool _isLegalFireAngle = false;
    protected bool _isLegalAimAngle = false;
    protected bool CanRotate { get { return (!_fixed) && _status != ComponentStatus.HeavilyDamaged && _status != ComponentStatus.KnockedOut && _status != ComponentStatus.Destroyed; } }

    // Barrels, muzzles, and muzzleFx data:
    protected Transform[] Barrels;
    protected Transform[] Muzzles;
    protected ParticleSystem[] MuzzleFx;
    protected int _nextBarrel = 0;
    protected bool _alnternatingFire;

    // Weapon data:
    public ComponentSlotType TurretType;
    public float MaxRange;
    public float FiringInterval;
    public float ActualFiringInterval { get; protected set; }
    public ObjectFactory.WeaponSize TurretSize;
    public ObjectFactory.WeaponType TurretWeaponType;
    protected float _firingIntervalCoeff;
    protected float _vsStrikeCraftModifier;

    // Turret status:
    public int IsJammed { get; private set; }

    // Fire delay
    public float LastFire { get; protected set; }
    public int EnergyToFire;
    public int HeatToFire;

    // Auto control
    public TurretMode Mode { get; set; }

    private ITargetableEntity _targetShip = null;

    public int MaxHitpoints;
    private int _currHitPoints;
    private ComponentStatus _status;

    public IList<ObjectFactory.TacMapEntityType> TargetPriorityList { get; set; }

    public TurretAIHint HardpointAIHint { get; private set; }

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
                Debug.Log(string.Format("{0} on {1} is heavily damaged. Time: {2}", this, _containingShip, Time.time));
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

    protected static readonly float _targetingFiringArcToleranceSmall = 5.0f;
    protected static readonly float _targetingFiringArcToleranceLarge = 15.0f;

    protected virtual float AIMaxAngleToTarget => 2.0f;
    protected static readonly float _AIFiringArcTolerance = 10.0f;
    protected static readonly int _AIFiringArcScoreBonus = 2;
    protected static readonly int _AIPreferredTargetScoreBonus = 1;
    protected static readonly int _AICurrTargetScoreBonus = 1;

    private float _retargetTime = 0f;

    public float GetMaxRange { get { return MaxRange; } }

    public virtual string SpriteKey { get { return "Turret"; } }

    public IEnumerable<ComponentSlotType> AllowedSlotTypes { get { return _allowedSlotTypes; } }

    public ObjectFactory.ShipSize MinShipSize { get { return ObjectFactory.ShipSize.Sloop; } }
    public ObjectFactory.ShipSize MaxShipSize { get { return ObjectFactory.ShipSize.CapitalShip; } }

    private ComponentSlotType[] _allowedSlotTypes;

    protected bool _flippedX = false;

    private static Buff _defaultBuff = Buff.Default();
    public Buff ComponentBuff => _defaultBuff;

    protected static readonly WaitForSeconds _autoBehaviorPulseDelay = new WaitForSeconds(0.1f);
    protected static readonly WaitForSeconds _fullBroadsideDelay = new WaitForSeconds(0.1f);
}