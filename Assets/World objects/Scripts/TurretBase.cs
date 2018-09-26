using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

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
    }

    private void ParseDeadZones()
    {
        if (_deadZoneAngleStrings != null)
        {
            _deadZoneAngleRanges = new Tuple<float, float>[_deadZoneAngleStrings.Length];
            for (int i = 0; i < _deadZoneAngleStrings.Length; ++i)
            {
                string[] nums = _deadZoneAngleStrings[i].Split(',');
                _deadZoneAngleRanges[i] = Tuple<float, float>.Create(float.Parse(nums[0]), float.Parse(nums[1]));
            }
        }
        if (_minRotation < _maxRotation)
        {
            _rotationAllowedRanges = new Tuple<float, float>[1]
            {
                Tuple<float, float>.Create(_minRotation, _maxRotation),
            };
        }
        else if (_maxRotation < _minRotation)
        {
            _rotationAllowedRanges = new Tuple<float, float>[2]
{
                Tuple<float, float>.Create(_minRotation, 360),
                Tuple<float, float>.Create(0, _maxRotation)
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
            yield return Tuple<Transform, Transform>.Create(root, muzzle);
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

    private static Ship FindContainingShip(Transform t)
    {
        if (t == null)
        {
            return null;
        }
        Ship s = t.GetComponent<Ship>();
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
            if (_targetShip.ShipDisabled || _targetShip.ShipSurrendered || _targetShip.InBoarding || (transform.position - _targetShip.transform.position).sqrMagnitude > (MaxRange * 1.05f) * (MaxRange * 1.05f))
            {
                _targetShip = null;
            }
            else
            {
                Fire(_targetShip.transform.position);
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
            if (CanRotate && Mathf.Abs(AngleToTargetShip - CurrAngle) > 2.0f)
            {
                return false;
            }
            Vector3 origin = Muzzles[_nextBarrel].position;
            origin.y = 0;
            Vector3 firingVector = Muzzles[_nextBarrel].up;
            firingVector.y = 0;
            RaycastHit[] hits = Physics.RaycastAll(origin, firingVector, MaxRange);
            int closestHit = -1;
            for (int i = 0; i < hits.Length; ++i)
            {
                if (hits[i].collider.gameObject == ContainingShip.gameObject || hits[i].collider.gameObject == ContainingShip.ShieldCapsule.gameObject)
                {
                    continue;
                }
                if (closestHit < 0 || hits[i].distance < hits[closestHit].distance)
                {
                    closestHit = i;
                }
            }
            if (closestHit >= 0 && (hits[closestHit].collider.gameObject == _targetShip.gameObject || hits[closestHit].collider.gameObject == _targetShip.ShieldCapsule.gameObject))
            {
                return true;
            }
            else
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

    public void Fire(Vector3 target)
    {
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
            return AngleToShipHeading(FilterRotation(transform.rotation.eulerAngles));
        }
    }

    protected float AngleToShipHeading(float globalAngle)
    {
        return AngleToShipHeading(globalAngle, false);
    }

    protected float AngleToShipHeading(float globalAngle, bool inverse)
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
    }

    private float AngleToTargetShip
    {
        get
        {
            if (_targetShip == null)
            {
                return 0;
            }
            Vector3 vecToTargetShip = _targetShip.transform.position - transform.position;
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
                    if (_targetShip == null)
                    {
                        _targetShip = AcquireTarget();
                    }
                    if (_targetShip != null)
                    {
                        ManualTarget(_targetShip.transform.position);
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

    protected abstract Ship AcquireTarget();

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
    protected float _minRotation, _maxRotation;
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

    private Ship _targetShip = null;

    public int MaxHitpoints;
    private int _currHitPoints;
    private ComponentStatus _status;

    public Ship ContainingShip { get { return _containingShip; } }

    // The ship containing the turret:
    protected Ship _containingShip;

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

    public float GetMaxRange { get { return MaxRange; } }

    public string SpriteKey { get { return "Turret"; } }

    public ComponentSlotType ComponentType { get { return TurretType; } }

    public ObjectFactory.ShipSize MinShipSize { get { return ObjectFactory.ShipSize.Sloop; } }
    public ObjectFactory.ShipSize MaxShipSize { get { return ObjectFactory.ShipSize.CapitalShip; } }
}
