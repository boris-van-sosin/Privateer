using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System;

public abstract class TurretBase : MonoBehaviour, ITurret
{
    // Use this for initialization
    protected virtual void Start()
    {
        LastFire = 0f;
    }

    public virtual void Init(string turretSlotType, TurretMod turretMod)
    {
        SlotType = turretSlotType;
        ComponentHitPoints = ComponentMaxHitPoints = ComponentGlobalMaxHitPoints;
        ParseMuzzles();
        AlternatingFire = DefaultAlternatingFire;
        _allowedSlotTypes = new string[] { turretSlotType };
        _warheads = new Warhead[MaxWarheads];
        if (IsTurretModCombatible(turretMod))
        {
            _turretMod = turretMod;
            TurretModBuffApplier applier = ObjectFactory.GetTurretModBuff(turretMod);
            if (applier != null)
            {
                _turretModBuffs = new TurretModBuff[applier.TurretModBuffs.Length];
                applier.TurretModBuffs.CopyTo(_turretModBuffs, 0);
                _firingIntervalCoeffBuff = 0f;
                for (int i = 0; i < _turretModBuffs.Length; ++i)
                {
                    if (IsTurretModBuffApplicable(i))
                    {
                        _firingIntervalCoeffBuff += _turretModBuffs[i].RateOfFireBuff;
                    }
                }
                ApplyBuff(DynamicBuff.Default());
            }
        }
    }

    public virtual void PostInstallTurret(ShipBase s)
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
        _containingShip = s;
        ParseDeadZones();
        SetDefaultAngle();
        _initialized = true;
        _firingIntervalCoeff = 1f;
        _vsStrikeCraftModifier = 0f;
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
        List<Transform> muzzlesFound = FindMuzzles(transform).ToList();
        //Barrels = new Transform[barrelsFound.Count];
        Muzzles = new Transform[muzzlesFound.Count];
        if (AlternatingFire)
        {
            ActualFiringInterval = FiringInterval / Muzzles.Length;
        }
        else
        {
            ActualFiringInterval = FiringInterval;
        }
        MuzzleFx = new ParticleSystem[muzzlesFound.Count];
        for (int i = 0; i < muzzlesFound.Count; ++i)
        {
            //Barrels[i] = barrelsFound[i].Item1;
            Muzzles[i] = muzzlesFound[i];
            MuzzleFx[i] = muzzlesFound[i].GetComponentInChildren<ParticleSystem>();
        }
    }

    protected static IEnumerable<Transform> FindMuzzles(Transform root)
    {
        if (root.name.StartsWith(MuzzleString))
        {
            yield return root;
        }
        else
        {
            for (int i = 0; i < root.childCount; ++i)
            {
                IEnumerable<Transform> resInChildren = FindMuzzles(root.GetChild(i));
                foreach (Transform r in resInChildren)
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
            //Debug.LogFormat("{0} on {1} not ready to fire", this, _containingShip);
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
            for (int i = 0; i < _deadZoneAngleRanges.Length; ++i)
            {
                float currAngle = CurrLocalAngle;
                if (_deadZoneAngleRanges[i].Item1 < currAngle && currAngle < _deadZoneAngleRanges[i].Item2)
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
            //Debug.LogFormat("{0} on {1} not enough heat or energy to fire", this, _containingShip);
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
    }
    protected TurretMod _turretMod;
    protected TurretModBuff[] _turretModBuffs = null;

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

    public virtual void ApplyBuff(DynamicBuff b)
    {
        float factor = 1f + b.WeaponRateOfFireFactor + _firingIntervalCoeffBuff;
        _firingIntervalCoeff = Mathf.Clamp(1f / factor, 0.5f, 2f);
    }

    protected virtual bool IsTurretModBuffApplicable(int idx)
    {
        return string.IsNullOrEmpty(_turretModBuffs[idx].ApplyToWeapon) || _turretModBuffs[idx].ApplyToWeapon == TurretWeaponType;
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
    //protected Transform[] Barrels;
    protected Transform[] Muzzles;
    protected ParticleSystem[] MuzzleFx;
    protected int _nextBarrel = 0;
    protected bool _alnternatingFire;

    // Weapon data:
    public string TurretType;
    public float MaxRange;
    public float FiringInterval;
    protected Warhead[] _warheads;
    public static readonly int MaxWarheads = 2;

    public float ActualFiringInterval { get; protected set; }
    public string TurretWeaponSize;
    public string TurretWeaponType;
    protected float _firingIntervalCoeff;
    protected float _vsStrikeCraftModifier;
    protected float _firingIntervalCoeffBuff;

    // Turret status:
    public int IsJammed { get; private set; }

    // Fire delay
    public float LastFire { get; protected set; }
    public int EnergyToFire;
    public int HeatToFire;

    // Auto control
    public TurretMode Mode { get; set; }

    private ITargetableEntity _targetShip = null;

    public int GlobalMaxHitpoints;
    private int _maxHitpoints;
    private int _currHitPoints;
    private ComponentStatus _status;

    public IList<ObjectFactory.TacMapEntityType> TargetPriorityList { get; set; }

    public TurretAIHint HardpointAIHint { get; private set; }

    public ShipBase ContainingShip { get { return _containingShip; } }

    public string SlotType { get; private set; }

    // The ship containing the turret:
    protected ShipBase _containingShip;

    public enum TurretMode { Off, Manual, Auto };
    public enum RotationAxis { XAxis, YAxis, ZAxis };

    //protected static readonly string BarrelString = "Barrel";
    protected static readonly string MuzzleString = "Muzzle";

    public event Action OnHitpointsChanged;

    // Hit point stuff:
    public virtual int ComponentGlobalMaxHitPoints
    {
        get
        {
            return GlobalMaxHitpoints;
        }
        protected set
        {
            if (value > 0)
            {
                GlobalMaxHitpoints = value;
            }
        }
    }
    public virtual int ComponentMaxHitPoints
    {
        get
        {
            return _maxHitpoints;
        }
        set
        {
            if (value > 0)
            {
                _maxHitpoints = value;
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
            else if (_currHitPoints <= ComponentGlobalMaxHitPoints / 10)
            {
                Status = ComponentStatus.KnockedOut;
            }
            else if (_currHitPoints <= ComponentGlobalMaxHitPoints / 4)
            {
                Status = ComponentStatus.HeavilyDamaged;
                Debug.LogFormat("{0} on {1} is heavily damaged. Time: {2}", this, _containingShip, Time.time);
            }
            else if (_currHitPoints <= ComponentGlobalMaxHitPoints / 2)
            {
                Status = ComponentStatus.LightlyDamaged;
            }
            else
            {
                Status = ComponentStatus.Undamaged;
            }
            OnHitpointsChanged?.Invoke();
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

    public IEnumerable<string> AllowedSlotTypes { get { return _allowedSlotTypes; } }
    public abstract ObjectFactory.WeaponBehaviorType BehaviorType { get; }

    public ObjectFactory.ShipSize MinShipSize { get { return ObjectFactory.ShipSize.Sloop; } }
    public ObjectFactory.ShipSize MaxShipSize { get { return ObjectFactory.ShipSize.CapitalShip; } }

    private string[] _allowedSlotTypes;

    protected bool _flippedX = false;

    private static readonly DynamicBuff _defaultBuff = DynamicBuff.Default();
    public DynamicBuff ComponentBuff => _defaultBuff;

    // Ugly optimization:
    protected Collider[] _collidersCache = new Collider[1024];

    protected static readonly WaitForSeconds _autoBehaviorPulseDelay = new WaitForSeconds(0.1f);
    protected static readonly WaitForSeconds _fullBroadsideDelay = new WaitForSeconds(0.1f);
}
