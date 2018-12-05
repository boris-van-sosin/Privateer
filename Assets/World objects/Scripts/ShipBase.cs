using System.Collections.Generic;
using System.Text;
using UnityEngine;
using System.Linq;

public abstract class ShipBase : MonoBehaviour, ITargetableEntity
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
    }

    public virtual void Activate()
    {
        FindTurrets();
        InitShield();
    }

    protected abstract void Update();

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

    public abstract void MoveForeward();

    public abstract void MoveBackward();

    public abstract void ApplyBraking();

    public abstract void ApplyTurning(bool left);

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


    // TargetableEntity properties:
    public Vector3 EntityLocation { get { return transform.position; } }
    public virtual bool Targetable { get { return true; } }

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

    // Movement stats
    public float MaxSpeed;
    public float Mass;
    public float Thrust;
    public float Braking;
    public float TurnRate;
    protected float _speed;

    public Faction Owner;
}
