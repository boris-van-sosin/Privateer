using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using System.Linq;

public class Ship : MonoBehaviour
{
    void Awake()
    {
        if (Follow)
        {
            _userCamera = Camera.main;
            _cameraOffset = _userCamera.transform.position - transform.position;
            CameraOffsetFactor = 1.0f;
        }
        HullHitPoints = MaxHullHitPoints;
        Energy = 0;
        Heat = 0;
        MaxHeat = 100;
        ComputeLength();
        InitComponents();
        InitArmour();
    }

    // Use this for initialization
    void Start ()
    {
        FindTurrets();
        _manualTurrets = new HashSet<ITurret>(_turrets);
        StartCoroutine(ContinuousComponents());
    }
	
    private void FindTurrets()
    {
        TurretHardpoint[] hardpoints = GetComponentsInChildren<TurretHardpoint>();
        List<ITurret> turrets = new List<ITurret>(hardpoints.Length);
        foreach (TurretHardpoint hp in hardpoints)
        {
            ITurret turret = hp.GetComponentInChildren<TurretBase>();
            if (turret != null)
            {
                turrets.Add(turret);
            }
        }
        _turrets = turrets.ToArray();
    }

    private void InitComponents()
    {
        _componentSlots.Add(ShipSection.Fore, new List<Tuple<ComponentSlotType, IShipComponent>>()
        {
            Tuple<ComponentSlotType,IShipComponent>.Create(ComponentSlotType.ShipSystem, PowerPlant.DefaultComponent(this)),
            Tuple<ComponentSlotType,IShipComponent>.Create(ComponentSlotType.ShipSystem, PowerPlant.DefaultComponent(this)),
            Tuple<ComponentSlotType,IShipComponent>.Create(ComponentSlotType.ShipSystem, CapacitorBank.DefaultComponent(this)),
            Tuple<ComponentSlotType,IShipComponent>.Create(ComponentSlotType.ShipSystem, HeatExchange.DefaultComponent(this)),
            Tuple<ComponentSlotType,IShipComponent>.Create(ComponentSlotType.ShipSystem, ShieldGenerator.DefaultComponent(this))
        });
        _componentSlots.Add(ShipSection.Aft, new List<Tuple<ComponentSlotType, IShipComponent>>());
        _componentSlots.Add(ShipSection.Left, new List<Tuple<ComponentSlotType, IShipComponent>>());
        _componentSlots.Add(ShipSection.Right, new List<Tuple<ComponentSlotType, IShipComponent>>());

        _energyCapacityComps = AllComponents.Where(x => x is IEnergyCapacityComponent).Select(y => y as IEnergyCapacityComponent).ToArray();
        _updateComponents = AllComponents.Where(x => x is IPeriodicActionComponent).Select(y => y as IPeriodicActionComponent).ToArray();
        _shieldComponents = AllComponents.Where(x => x is IShieldComponent).Select(y => y as IShieldComponent).ToArray();
        _totalMaxShield = 0;
        foreach (IShieldComponent shield in _shieldComponents)
        {
            _totalMaxShield += shield.MaxShieldPoints;
        }
    }

    private void InitArmour()
    {
        _maxArmour.Add(ShipSection.Fore, DefaultArmorFront);
        _currArmour.Add(ShipSection.Fore, DefaultArmorFront);
        _maxArmour.Add(ShipSection.Aft, DefaultArmorAft);
        _currArmour.Add(ShipSection.Aft, DefaultArmorAft);
        _maxArmour.Add(ShipSection.Left, DefaultArmorLeft);
        _currArmour.Add(ShipSection.Left, DefaultArmorLeft);
        _maxArmour.Add(ShipSection.Right, DefaultArmorRight);
        _currArmour.Add(ShipSection.Right, DefaultArmorRight);
        foreach (ShipSection section in _componentSlots.Keys)
        {
            foreach (Tuple<ComponentSlotType, IShipComponent> comp in _componentSlots[section])
            {
                if (!_maxArmour.ContainsKey(section))
                {
                    _maxArmour.Add(section, 0);
                }
                if (!_currArmour.ContainsKey(section))
                {
                    _currArmour.Add(section, _maxArmour[section]);
                }
                ExtraArmour a = comp.Item2 as ExtraArmour;
                if (a != null)
                {
                    _maxArmour[section] += a.ArmourAmount;
                    _currArmour[section] += a.ArmourAmount;
                }
            }
        }
    }

    private void ComputeLength()
    {
        Mesh m = GetComponent<MeshFilter>().mesh;
        _shipLength = m.bounds.size.y;
    }

	// Update is called once per frame
	void Update()
    {
        float directionMult = 0.0f;
        if (MovementDirection == ShipDirection.Forward)
        {
            directionMult = 1.0f;
        }
        else if (MovementDirection == ShipDirection.Reverse)
        {
            directionMult = -1.0f;
        }
        transform.position += Time.deltaTime * (ActualVelocity = directionMult * _speed * transform.up);
        //if (Follow) Debug.Log(string.Format("Velocity vector: {0}", ActualVelocity));
        if (_autoHeading)
        {
            RotateToHeading();
        }
        if (_userCamera != null)
        {
            _userCamera.transform.position = transform.position + (_cameraOffset * CameraOffsetFactor);
        }
	}

    private IEnumerator ContinuousComponents()
    {
        while(true)
        {
            int newMaxEnergy = 0;
            foreach (IEnergyCapacityComponent comp in _energyCapacityComps)
            {
                newMaxEnergy += comp.EnergyCapacity;
            }
            MaxEnergy = newMaxEnergy;
            foreach (IPeriodicActionComponent comp in _updateComponents)
            {
                comp.PeriodicAction();
            }
            yield return new WaitForSeconds(0.25f);
        }
    }

    public void ManualTarget(Vector3 target)
    {
        foreach (ITurret t in _manualTurrets)
        {
            t.ManualTarget(target);
        }
    }

    public void MoveForeward()
    {
        if (MovementDirection == ShipDirection.Stopped)
        {
            MovementDirection = ShipDirection.Forward;
        }
        if (MovementDirection == ShipDirection.Forward)
        {
            ApplyThrust();
        }
        else if (MovementDirection == ShipDirection.Reverse)
        {
            ApplyBraking();
        }
    }

    private void ApplyThrust()
    {
        float newSpeed = _speed + Thrust * Time.deltaTime;
        if (newSpeed > MaxSpeed)
        {
            _speed = MaxSpeed;
        }
        else
        {
            _speed = newSpeed;
        }
    }

    private void ApplyThrust(float factor, float targetSpeedFactor)
    {
        float targetSpeed = MaxSpeed * Mathf.Clamp01(targetSpeedFactor);
        if (targetSpeed < _speed)
        {
            return;
        }
        float newSpeed = _speed + Thrust * Mathf.Clamp01(factor) * Time.deltaTime;
        if (newSpeed > MaxSpeed * Mathf.Clamp01(targetSpeedFactor))
        {
            _speed = MaxSpeed * Mathf.Clamp01(targetSpeedFactor);
        }
        else
        {
            _speed = newSpeed;
        }
    }

    public void MoveBackward()
    {
        if (MovementDirection == ShipDirection.Stopped)
        {
            MovementDirection = ShipDirection.Reverse;
        }
        if (MovementDirection == ShipDirection.Forward)
        {
            ApplyBraking();
        }
        else if (MovementDirection == ShipDirection.Reverse)
        {
            ApplyThrust();
        }
    }

    private void ApplyBraking()
    {
        float newSpeed = _speed - Braking * Time.deltaTime;
        if (newSpeed < 0)
        {
            _speed = 0;
            MovementDirection = ShipDirection.Stopped;
        }
        else
        {
            _speed = newSpeed;
        }
    }

    private void ApplyBraking(float factor, float targetSpeedFactor)
    {
        float targetSpeed = MaxSpeed * Mathf.Clamp01(targetSpeedFactor);
        float newSpeed = _speed - Braking * Mathf.Clamp01(factor) * Time.deltaTime;
        if (targetSpeed > _speed)
        {
            return;
        }
        if (newSpeed < targetSpeed)
        {
            _speed = MaxSpeed * targetSpeed;
            if (_speed <= 0)
            {
                MovementDirection = ShipDirection.Stopped;
                _speed = 0;
            }
        }
        else
        {
            _speed = newSpeed;
        }
    }

    public void ApplyTurning(bool left)
    {
        float turnFactor = 1.0f;
        if (left)
        {
            turnFactor = -1.0f;
        }
        Quaternion deltaRot = Quaternion.AngleAxis(turnFactor * TurnRate * Time.deltaTime, transform.forward);
        transform.rotation = deltaRot * transform.rotation;
        ApplyBraking(0.5f, 0.5f);
    }

    public void SetRequiredHeading(Vector3 targetPoint)
    {
        Vector3 requiredHeadingVector = targetPoint - transform.position;
        requiredHeadingVector.y = 0;
        _autoHeadingRotation = Quaternion.LookRotation(transform.forward, requiredHeadingVector);
        _autoHeading = true;
    }

    private void RotateToHeading()
    {
        if (Quaternion.Angle(transform.rotation,_autoHeadingRotation) < 0.5f)
        {
            _autoHeading = false;
            return;
        }
        transform.rotation = Quaternion.RotateTowards(transform.rotation, _autoHeadingRotation, TurnRate * Time.deltaTime);
    }

    public bool MovingForward { get { return MovementDirection == ShipDirection.Forward; } }

    public void FireManual(Vector3 target)
    {
        StringBuilder sb = new StringBuilder();
        foreach (ITurret t in _manualTurrets)
        {
            t.Fire(target);
            sb.AppendFormat("Turret {0}:{1}, ", t, t.CurrLocalAngle);
        }
        Debug.Log(sb.ToString());
    }

    public Vector3 ActualVelocity { get; private set; }

    public bool TryChangeEnergy(int delta)
    {
        int newEnergy = Energy + delta;
        if (0 <= newEnergy)
        {
            Energy = System.Math.Min(newEnergy, MaxEnergy);
            return true;
        }
        return false;
    }

    public bool TryChangeHeat(int delta)
    {
        int newHeat = Heat + delta;
        if (newHeat <= MaxHeat)
        {
            Heat = System.Math.Max(newHeat, 0);
            return true;
        }
        return false;
    }

    public bool TryChangeEnergyAndHeat(int deltaEnergy, int deltaHeat)
    {
        int newEnergy = Energy + deltaEnergy;
        int newHeat = Heat + deltaHeat;
        if (newEnergy >= 0 && newHeat <= MaxHeat)
        {
            Energy = System.Math.Min(newEnergy, MaxEnergy);
            Heat = System.Math.Max(newHeat, 0);
            return true;
        }
        return false;
    }

    public void TakeHit(Warhead w, Vector3 location)
    {
        ShipSection sec = GetHitSection(location);
        Debug.Log(string.Format("Ship {0} hit in {1}", name, sec));
        // if shields are present, take shield damage
        if (!Combat.DamageShields(w.ShieldDamage, _shieldComponents))
        {
            return;
        }

        // armour penetration
        int armourAtLocation = _currArmour[sec];
        if (Combat.ArmourPenetration(armourAtLocation, w.ArmourPenetration))
        {
            // a random component at the section is damaged.
            List<IShipActiveComponent> damageableComps = new List<IShipActiveComponent>(_componentSlots[sec].Count);
            foreach (Tuple<ComponentSlotType, IShipComponent> c in _componentSlots[sec])
            {
                IShipActiveComponent c2 = c.Item2 as IShipActiveComponent;
                if (c2 != null)
                {
                    damageableComps.Add(c2);
                }
            }
            if (damageableComps.Count > 0)
            {
                IShipActiveComponent comp = ObjectFactory.GetRandom(damageableComps);
                comp.ComponentHitPoints -= w.SystemDamage;
            }
            HullHitPoints = System.Math.Max(0, HullHitPoints - w.HullDamage);
        }
        _currArmour[sec] = System.Math.Max(0, _currArmour[sec] - w.ArmourDamage);
    }

    private ShipSection GetHitSection(Vector3 hitLocation)
    {
        Vector3 localHitLocation = transform.InverseTransformPoint(hitLocation);
        if (localHitLocation.y > _shipLength / 6)
        {
            return ShipSection.Fore;
        }
        else if (localHitLocation.y < -_shipLength / 6)
        {
            return ShipSection.Aft;
        }
        else if (localHitLocation.x > 0)
        {
            return ShipSection.Left;
        }
        else
        {
            return ShipSection.Right;
        }
    }

    public int ShipTotalShields
    {
        get
        {
            int totalShields = 0;
            foreach (IShieldComponent shield in _shieldComponents)
            {
                totalShields += shield.CurrShieldPoints;
            }
            return totalShields;
        }
    }

    public int ShipTotalMaxShields
    {
        get
        {
            return _totalMaxShield;
        }
    }


    private enum ShipDirection { Stopped, Forward, Reverse };
    public enum ShipSection { Fore, Aft, Left, Right };

    public float MaxSpeed;
    public float Mass;
    public float Thrust;
    public float Braking;
    public float TurnRate;
    private float _speed;
    private bool _autoHeading = false;
    private Quaternion _autoHeadingRotation;
    private ShipDirection MovementDirection = ShipDirection.Stopped;

    private ITurret[] _turrets;
    private IEnumerable<ITurret> _manualTurrets;

    public int Energy { get; private set; }
    public int MaxEnergy { get; private set; }
    public int Heat { get; private set; }
    public int MaxHeat { get; private set; }

    public ComponentSlotType[] ForeComponentSlots;
    public ComponentSlotType[] AftComponentSlots;
    public ComponentSlotType[] LeftComponentSlots;
    public ComponentSlotType[] RightComponentSlots;
    private Dictionary<ShipSection, List<Tuple<ComponentSlotType, IShipComponent>>> _componentSlots = new Dictionary<ShipSection, List<Tuple<ComponentSlotType, IShipComponent>>>();

    public IEnumerable<IShipComponent> AllComponents
    {
        get
        {
            foreach (List<Tuple<ComponentSlotType, IShipComponent>> l in _componentSlots.Values)
            {
                foreach (Tuple<ComponentSlotType, IShipComponent> comp in l)
                {
                    yield return comp.Item2;
                }
            }
        }
    }

    private IEnergyCapacityComponent[] _energyCapacityComps;
    private IPeriodicActionComponent[] _updateComponents;
    private IShieldComponent[] _shieldComponents;
    private float _shipLength;

    public int MaxHullHitPoints;
    public int HullHitPoints { get; private set; }
    private int _totalMaxShield;
    public int DefaultArmorFront;
    public int DefaultArmorAft;
    public int DefaultArmorLeft;
    public int DefaultArmorRight;
    private Dictionary<ShipSection, int> _maxArmour = new Dictionary<ShipSection, int>();
    private Dictionary<ShipSection, int> _currArmour = new Dictionary<ShipSection, int>();

    private Camera _userCamera;
    private Vector3 _cameraOffset;
    public float CameraOffsetFactor { get; set; }

    public bool Follow; // tmp
}
