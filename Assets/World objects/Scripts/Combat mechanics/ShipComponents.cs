using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System;

/*
public class TurretComponent : ITurret
{
    public TurretComponent(TurretBase t)
    {
        _innerTurret = t;
    }

    public float CurrAngle => _innerTurret.CurrAngle;

    public float CurrLocalAngle => _innerTurret.CurrAngle;

    public int ComponentMaxHitPoints => _innerTurret.ComponentMaxHitPoints;

    public int ComponentHitPoints { get { return _innerTurret.ComponentHitPoints; } set { _innerTurret.ComponentHitPoints = value; if (OnHitpointsChanged != null) { OnHitpointsChanged(); } } }

    public bool ComponentIsWorking => _innerTurret.ComponentIsWorking;

    public ComponentStatus Status => _innerTurret.Status;

    public ShipBase ContainingShip => _innerTurret.ContainingShip;

    public void Fire(Vector3 target)
    {
        _innerTurret.Fire(target);
    }

    public void ManualTarget(Vector3 target)
    {
        _innerTurret.ManualTarget(target);
    }

    public void SetTurretBehavior(TurretBase.TurretMode newMode)
    {
        _innerTurret.SetTurretBehavior(newMode);
    }

    public TurretBase.TurretMode GetTurretBehavior() => _innerTurret.GetTurretBehavior();

    public bool HasGrapplingTool() => _innerTurret.HasGrapplingTool();

    public void FireGrapplingTool(Vector3 target) => _innerTurret.FireGrapplingTool(target);

    public float GetMaxRange => _innerTurret.GetMaxRange;

    public IEnumerable<ComponentSlotType> AllowedSlotTypes => _innerTurret.AllowedSlotTypes;

    public string SpriteKey => "Turret";

    public ObjectFactory.ShipSize MinShipSize => _innerTurret.MinShipSize;
    public ObjectFactory.ShipSize MaxShipSize => _innerTurret.MaxShipSize;

    public bool IsOutOfAmmo => _innerTurret.IsOutOfAmmo;

    private TurretBase _innerTurret;

    private readonly ComponentSlotType[] SolotTypes;

    public event ComponentHitpointsChangedDelegate OnHitpointsChanged;
}*/

public class PowerPlant : ShipActiveComponentBase, IPeriodicActionComponent
{
    public int PowerOutput, HeatOutput;

    public void PeriodicAction()
    {
        if (!ComponentIsWorking)
        {
            return;
        }
        ContainingShip.TryChangeEnergyAndHeat(PowerOutput, HeatOutput);
    }

    public int EnergyPerTick => -1;
    public int HeatPerTick => HeatOutput;

    public override IEnumerable<string> AllowedSlotTypes { get { return _allowedSlotTypes; } }

    public override string SpriteKey { get { return "Power plant"; } }

    public static PowerPlant DefaultComponent(Ship containingShip)
    {
        return new PowerPlant()
        {
            ComponentMaxHitPoints = 400,
            ComponentHitPoints = 400,
            Status = ComponentStatus.Undamaged,
            PowerOutput = 3,
            HeatOutput = 1,
            _containingShip = containingShip
        };
    }

    public static PowerPlant DefaultComponent(ObjectFactory.ShipSize grade, Ship containingShip)
    {
        switch (grade)
        {
            case ObjectFactory.ShipSize.Sloop:
                return new PowerPlant()
                {
                    ComponentMaxHitPoints = 300,
                    ComponentHitPoints = 300,
                    Status = ComponentStatus.Undamaged,
                    PowerOutput = 6,
                    HeatOutput = 1,
                    _containingShip = containingShip,
                    MinShipSize = grade,
                    MaxShipSize = ObjectFactory.ShipSize.CapitalShip
                };
            case ObjectFactory.ShipSize.Frigate:
                return new PowerPlant()
                {
                    ComponentMaxHitPoints = 400,
                    ComponentHitPoints = 400,
                    Status = ComponentStatus.Undamaged,
                    PowerOutput = 6,
                    HeatOutput = 1,
                    _containingShip = containingShip,
                    MinShipSize = grade,
                    MaxShipSize = ObjectFactory.ShipSize.CapitalShip
                };
            case ObjectFactory.ShipSize.Destroyer:
                return new PowerPlant()
                {
                    ComponentMaxHitPoints = 600,
                    ComponentHitPoints = 600,
                    Status = ComponentStatus.Undamaged,
                    PowerOutput = 8,
                    HeatOutput = 1,
                    _containingShip = containingShip,
                    MinShipSize = grade,
                    MaxShipSize = ObjectFactory.ShipSize.CapitalShip
                };
            case ObjectFactory.ShipSize.Cruiser:
                return new PowerPlant()
                {
                    ComponentMaxHitPoints = 800,
                    ComponentHitPoints = 800,
                    Status = ComponentStatus.Undamaged,
                    PowerOutput = 10,
                    HeatOutput = 2,
                    _containingShip = containingShip,
                    MinShipSize = grade,
                    MaxShipSize = ObjectFactory.ShipSize.CapitalShip
                };
            case ObjectFactory.ShipSize.CapitalShip:
                return new PowerPlant()
                {
                    ComponentMaxHitPoints = 1100,
                    ComponentHitPoints = 1100,
                    Status = ComponentStatus.Undamaged,
                    PowerOutput = 10,
                    HeatOutput = 3,
                    _containingShip = containingShip,
                    MinShipSize = grade,
                    MaxShipSize = ObjectFactory.ShipSize.CapitalShip
                };
            default:
                return null;
        }
    }

    private static readonly string[] _allowedSlotTypes = new string[] { "ShipSystemCenter" };
}

public class CapacitorBank : ShipComponentBase, IEnergyCapacityComponent
{
    public int Capacity;

    public int EnergyCapacity
    {
        get
        {
            return Capacity;
        }
    }

    public override IEnumerable<string> AllowedSlotTypes { get { return _allowedSlotTypes; } }

    public static CapacitorBank DefaultComponent(Ship containingShip)
    {
        return new CapacitorBank()
        {
            Capacity = 50,
            _containingShip = containingShip,
            MinShipSize = ObjectFactory.ShipSize.Sloop,
            MaxShipSize = ObjectFactory.ShipSize.CapitalShip
        };
    }

    private static readonly string[] _allowedSlotTypes = new string[] { "ShipSystem", "ShipSystemCenter" };
}

public class HeatSink : ShipComponentBase, IHeatCapacityComponent
{
    public int Capacity;

    public override IEnumerable<string> AllowedSlotTypes { get { return _allowedSlotTypes; } }

    public int HeatCapacity => Capacity;

    public static HeatSink DefaultComponent(Ship containingShip)
    {
        return new HeatSink()
        {
            Capacity = 50,
            _containingShip = containingShip,
            MinShipSize = ObjectFactory.ShipSize.Sloop,
            MaxShipSize = ObjectFactory.ShipSize.CapitalShip
        };
    }

    private static readonly string[] _allowedSlotTypes = new string[] { "ShipSystem", "ShipSystemCenter" };
}
public class ShieldGenerator : ShipActiveComponentBase, IPeriodicActionComponent, IShieldComponent
{
    public int MaxShieldPoints { get; private set; }
    public int CurrShieldPoints
    {
        get
        {
            return _currShieldPoints;
        }
        set
        {
            if (value <= 0)
            {
                _currShieldPoints = 0;
                _isShieldActive = false;
                _ticksSinceInactive = 0;
            }
            else
            {
                _currShieldPoints = value;
            }
        }
    }

    public int EnergyPerTick => PowerPerShieldRegeneration;
    public int HeatPerTick => HeatGenerationPerShieldRegeneration;

    public int MaxShieldPointRegeneration;
    public int PowerUsage;
    public int PowerPerShieldRegeneration;
    public int PowerToRestart;
    public float RestartDelay;
    public int HeatGeneration;
    public int HeatGenerationPerShieldRegeneration;
    public int HeatToRestart;
    private bool _isShieldActive;
    private bool _shieldRequired;
    private int _ticksSinceInactive = 0;
    private int _currShieldPoints;

    public event Action<bool> OnToggle;

    public void PeriodicAction()
    {
        if (!ComponentIsWorking || !_shieldRequired)
        {
            return;
        }
        if (!ContainingShip.TryChangeEnergyAndHeat(-PowerUsage, HeatGeneration))
        {
            CurrShieldPoints = 0;
            _isShieldActive = false;
            _ticksSinceInactive = 0;
        }
        if (_isShieldActive && CurrShieldPoints < MaxShieldPoints)
        {
            if (ContainingShip.TryChangeEnergyAndHeat(-PowerPerShieldRegeneration, HeatGenerationPerShieldRegeneration))
            {
                CurrShieldPoints = System.Math.Min(MaxShieldPoints, CurrShieldPoints + MaxShieldPointRegeneration);
            }
        }
        if (!_isShieldActive)
        {
            if (++_ticksSinceInactive >= RestartDelay && ContainingShip.TryChangeEnergyAndHeat(-PowerToRestart, HeatToRestart))
            {
                _isShieldActive = true;
            }
        }
    }

    public bool ComponentActive
    {
        get
        {
            return _shieldRequired;
        }
        set
        {
            if (_shieldRequired == false && value == true)
            {
                CurrShieldPoints = 0;
                _isShieldActive = false;
                _ticksSinceInactive = 0;
            }
            else if (_shieldRequired == true && value == false)
            {
                _isShieldActive = false;
                CurrShieldPoints = 0;
            }
            _shieldRequired = value;
        }
    }

    public override IEnumerable<string> AllowedSlotTypes { get { return _allowedSlotTypes; } }

    public override string SpriteKey { get { return "Shield generator"; } }

    public static ShieldGenerator DefaultComponent(Ship containingShip)
    {
        return new ShieldGenerator()
        {
            ComponentMaxHitPoints = 400,
            ComponentHitPoints = 400,
            Status = ComponentStatus.Undamaged,
            MaxShieldPoints = 1000,
            CurrShieldPoints = 1000,
            MaxShieldPointRegeneration = 2,
            PowerUsage = 2,
            HeatGeneration = 1,
            PowerPerShieldRegeneration = 3,
            HeatGenerationPerShieldRegeneration = 2,
            PowerToRestart = 20,
            HeatToRestart = 10,
            RestartDelay = 20 * 4,
            _shieldRequired = true,
            _containingShip = containingShip
        };
    }

    public static ShieldGenerator DefaultComponent(ObjectFactory.ShipSize grade, Ship containingShip)
    {
        ShieldGenerator res = DefaultComponent(containingShip);
        res.MinShipSize = grade;
        res.MaxShipSize = ObjectFactory.ShipSize.CapitalShip;
        switch (grade)
        {
            case ObjectFactory.ShipSize.Sloop:
                res.ComponentHitPoints = res.ComponentMaxHitPoints = 300;
                break;
            case ObjectFactory.ShipSize.Frigate:
                res.ComponentHitPoints = res.ComponentMaxHitPoints = 400;
                break;
            case ObjectFactory.ShipSize.Destroyer:
                res.ComponentHitPoints = res.ComponentMaxHitPoints = 600;
                break;
            case ObjectFactory.ShipSize.Cruiser:
                res.ComponentHitPoints = res.ComponentMaxHitPoints = 800;
                break;
            case ObjectFactory.ShipSize.CapitalShip:
                res.ComponentHitPoints = res.ComponentMaxHitPoints = 1100;
                break;
            default:
                break;
        }
        return res;
    }

    private static readonly string[] _allowedSlotTypes = new string[] { "ShipSystem", "ShipSystemCenter" };
}

public class DamageControlNode : ShipActiveComponentBase, IPeriodicActionComponent
{
    public int PowerUsage;
    public int HullMaxHitPointRegeneration;
    public int SystemMaxHitPointRegeneration;
    public int ArmorMaxPointRegeneration;
    public float TimeOutOfCombatToRepair;

    
    public void PeriodicAction()
    {
        if (!ComponentIsWorking)
        {
            return;
        }
        if (Time.time - _containingShip.LastInCombat < TimeOutOfCombatToRepair)
        {
            return;
        }
        bool armourAtFull = _containingShip.ArmourAtFull;
        bool componentsAtFull = _containingShip.ComponentsAtFull;
        if (_containingShip.HullHitPoints == _containingShip.MaxHullHitPoints && armourAtFull && componentsAtFull)
        {
            return;
        }

        if (!ContainingShip.TryChangeEnergyAndHeat(-PowerUsage, 0))
        {
            return;
        }

        if (!componentsAtFull)
        {
            int systemPointRegenLeft = SystemMaxHitPointRegeneration;
            while (systemPointRegenLeft > 0)
            {
                systemPointRegenLeft = _containingShip.RepairComponents(systemPointRegenLeft);
            }
        }
        _containingShip.RepairHull(HullMaxHitPointRegeneration);
        if (!armourAtFull)
        {
            _containingShip.RepairArmour(ArmorMaxPointRegeneration);
        }
    }

    public override IEnumerable<string> AllowedSlotTypes { get { return _allowedSlotTypes; } }

    public override string SpriteKey { get { return "Damage control"; } }

    public int EnergyPerTick => PowerUsage;
    public int HeatPerTick => 0;

    public static DamageControlNode DefaultComponent(Ship containingShip)
    {
        return new DamageControlNode()
        {
            ComponentMaxHitPoints = 400,
            ComponentHitPoints = 400,
            Status = ComponentStatus.Undamaged,
            HullMaxHitPointRegeneration = 2,
            ArmorMaxPointRegeneration = 1,
            SystemMaxHitPointRegeneration = 2,
            TimeOutOfCombatToRepair = 5.0f,
            PowerUsage = 5,
            _containingShip = containingShip
        };
    }

    public static DamageControlNode DefaultComponent(ObjectFactory.ShipSize grade, Ship containingShip)
    {
        DamageControlNode res = DefaultComponent(containingShip);
        res.MinShipSize = grade;
        res.MaxShipSize = ObjectFactory.ShipSize.CapitalShip;
        switch (grade)
        {
            case ObjectFactory.ShipSize.Sloop:
                res.ComponentHitPoints = res.ComponentMaxHitPoints = 300;
                break;
            case ObjectFactory.ShipSize.Frigate:
                res.ComponentHitPoints = res.ComponentMaxHitPoints = 400;
                break;
            case ObjectFactory.ShipSize.Destroyer:
                res.ComponentHitPoints = res.ComponentMaxHitPoints = 600;
                break;
            case ObjectFactory.ShipSize.Cruiser:
                res.ComponentHitPoints = res.ComponentMaxHitPoints = 800;
                break;
            case ObjectFactory.ShipSize.CapitalShip:
                res.ComponentHitPoints = res.ComponentMaxHitPoints = 1100;
                break;
            default:
                break;
        }
        return res;
    }

    private static readonly string[] _allowedSlotTypes = new string[] { "ShipSystem", "ShipSystemCenter" };
}

public class HeatExchange : ShipComponentBase, IPeriodicActionComponent
{
    public int CoolingRate;

    public void PeriodicAction()
    {
        _containingShip.TryChangeHeat(-CoolingRate);
    }

    public override IEnumerable<string> AllowedSlotTypes { get { return _allowedSlotTypes; } }

    public int EnergyPerTick => 0;
    public int HeatPerTick => -1;

    public static HeatExchange DefaultComponent(Ship containingShip)
    {
        return new HeatExchange()
        {
            CoolingRate = 6,
            _containingShip = containingShip,
            MinShipSize = ObjectFactory.ShipSize.Sloop,
            MaxShipSize = ObjectFactory.ShipSize.CapitalShip
        };
    }

    private static readonly string[] _allowedSlotTypes = new string[] { "ShipSystem", "ShipSystemCenter" };
}

public class ExtraArmour : ShipComponentBase
{
    public int ArmourAmount;
    public int MitigationArmourAmount;
    public override IEnumerable<string> AllowedSlotTypes { get { return _allowedSlotTypes; } }

    private static readonly string[] _allowedSlotTypes = new string[] { "ShipSystem" };
}

public class ShipEngine : ShipActiveComponentBase, IUserToggledComponent, IPeriodicActionComponent
{

    public int EnergyPerThrust;
    public int HeatPerThrust;
    private bool _active;

    public bool ComponentActive
    {
        get
        {
            return _active;
        }

        set
        {
            if (value)
            {
                RequestEngine();
            }
            else
            {
                _active = false;
            }
        }
    }

    public bool RequestEngine()
    {
        if (_active)
        {
            return true;
        }
        if (ComponentIsWorking && ContainingShip.TryChangeEnergyAndHeat(-EnergyPerThrust, HeatPerThrust))
        {
            _active = true;
            OnToggle(true);
            return true;
        }
        else
        {
            return false;
        }
    }

    public void SetBraking()
    {
        _nextBrake = true;
    }

    public void PeriodicAction()
    {
        if (ComponentIsWorking && _active)
        {
            OnToggle?.Invoke(true);
            _nextDeactivate = true;
        }
        else if (_nextBrake)
        {
            OnToggle?.Invoke(true);
            _nextDeactivate = true;
        }
        else
        {
            OnToggle?.Invoke(false);
        }
        if (_nextDeactivate)
        {
            _active = _nextBrake = false;
            _nextDeactivate = false;
            OnToggle?.Invoke(false);
        }
    }

    private bool _nextDeactivate = false;
    private bool _nextBrake = false;

    public event Action<bool> OnToggle;

    public override IEnumerable<string> AllowedSlotTypes { get { return _allowedSlotTypes; } }

    public override string SpriteKey { get { return "Engine"; } }

    public int EnergyPerTick => EnergyPerThrust;
    public int HeatPerTick => HeatPerThrust;

    public static ShipEngine DefaultComponent(Ship containingShip)
    {
        return new ShipEngine()
        {
            ComponentMaxHitPoints = 600,
            ComponentHitPoints = 600,
            Status = ComponentStatus.Undamaged,
            EnergyPerThrust = 2,
            HeatPerThrust = 1,
            _containingShip = containingShip
        };
    }

    public static ShipEngine DefaultComponent(ObjectFactory.ShipSize grade, Ship containingShip)
    {
        ShipEngine res = DefaultComponent(containingShip);
        res.MinShipSize = grade;
        res.MaxShipSize = ObjectFactory.ShipSize.CapitalShip;
        switch (grade)
        {
            case ObjectFactory.ShipSize.Sloop:
                res.ComponentHitPoints = res.ComponentMaxHitPoints = 450;
                break;
            case ObjectFactory.ShipSize.Frigate:
                res.ComponentHitPoints = res.ComponentMaxHitPoints = 600;
                break;
            case ObjectFactory.ShipSize.Destroyer:
                res.ComponentHitPoints = res.ComponentMaxHitPoints = 900;
                break;
            case ObjectFactory.ShipSize.Cruiser:
                res.ComponentHitPoints = res.ComponentMaxHitPoints = 1200;
                break;
            case ObjectFactory.ShipSize.CapitalShip:
                res.ComponentHitPoints = res.ComponentMaxHitPoints = 1650;
                break;
            default:
                break;
        }
        return res;
    }

    private static readonly string[] _allowedSlotTypes = new string[] { "Engine" };
}

public class ElectromagneticClamps : ShipActiveComponentBase, IUserToggledComponent, IPeriodicActionComponent
{

    public int EnergyPerPulse;
    public int HeatPerPulse;
    private bool _active;

    public bool ComponentActive
    {
        get
        {
            return _active;
        }

        set
        {
            _active = value;
        }
    }

    public bool ClampsWorking { get; private set; }

    public void PeriodicAction()
    {
        if (ComponentIsWorking && _active && ContainingShip.TryChangeEnergyAndHeat(-EnergyPerPulse, HeatPerPulse))
        {
            if (!ClampsWorking && OnToggle != null)
            {
                OnToggle(true);
            }
            ClampsWorking = true;
        }
        else
        {
            if (ClampsWorking && OnToggle != null)
            {
                OnToggle(false);
            }
            ClampsWorking = false;
        }
    }

    public event Action<bool> OnToggle;

    public override IEnumerable<string> AllowedSlotTypes { get { return _allowedSlotTypes; } }

    public override string SpriteKey { get { return "Electromagentic Clamps"; } }

    public int EnergyPerTick => EnergyPerPulse;
    public int HeatPerTick => HeatPerPulse;

    public static ElectromagneticClamps DefaultComponent(Ship containingShip)
    {
        return new ElectromagneticClamps()
        {
            ComponentMaxHitPoints = 1,
            ComponentHitPoints = 1,
            Status = ComponentStatus.Undamaged,
            EnergyPerPulse = 1,
            HeatPerPulse = 0,
            _containingShip = containingShip
        };
    }

    private static readonly string[] _allowedSlotTypes = new string[] { "Hidden" };
}

public class CombatDetachment : ShipComponentBase
{
    public int CrewCapacity;
    public List<ShipCharacter> Forces { get; private set; }

    public void AddForces(IEnumerable<ShipCharacter> forces)
    {
        foreach (ShipCharacter c in forces)
        {
            if (Forces.Count >= CrewCapacity)
            {
                break;
            }
            Forces.Add(c);
        }
    }
    public override IEnumerable<string> AllowedSlotTypes { get { return _allowedSlotTypes; } }

    public static CombatDetachment DefaultComponent(Ship s)
    {
        return new CombatDetachment()
        {
            CrewCapacity = 10,
            Forces = new List<ShipCharacter>(),
            _containingShip = s
        };
    }

    public static CombatDetachment DefaultComponent(ObjectFactory.ShipSize grade, Ship s)
    {
        CombatDetachment res = DefaultComponent(s);
        switch (grade)
        {
            case ObjectFactory.ShipSize.Sloop:
                res.CrewCapacity = 5;
                break;
            case ObjectFactory.ShipSize.Frigate:
                res.CrewCapacity = 10;
                break;
            case ObjectFactory.ShipSize.Destroyer:
                res.CrewCapacity = 20;
                break;
            case ObjectFactory.ShipSize.Cruiser:
                res.CrewCapacity = 45;
                break;
            case ObjectFactory.ShipSize.CapitalShip:
                res.CrewCapacity = 65;
                break;
            default:
                break;
        }
        res.Forces.Capacity = res.CrewCapacity;
        res.MinShipSize = grade;
        res.MaxShipSize = ObjectFactory.ShipSize.CapitalShip;
        return res;
    }

    private static readonly string[] _allowedSlotTypes = new string[] { "ShipSystem", "ShipSystemCenter" };
}

public class FireControlGeneral : ShipActiveComponentBase, IPeriodicActionComponent
{
    public int PowerUsage;
    private DynamicBuff _activeBuff;
    private static DynamicBuff _inactiveBuff = DynamicBuff.Default();
    private bool _nextActive;

    public void PeriodicAction()
    {
        _nextActive = false;
        if (!ComponentIsWorking)
        {
            return;
        }

        if (!ContainingShip.TryChangeEnergyAndHeat(-PowerUsage, 0))
        {
            return;
        }
        _nextActive = true;
    }

    public override DynamicBuff ComponentBuff => _nextActive ? _activeBuff : _inactiveBuff;

    public override IEnumerable<string> AllowedSlotTypes { get { return _allowedSlotTypes; } }

    public override string SpriteKey { get { return "Fire control general"; } }

    public int EnergyPerTick => PowerUsage;
    public int HeatPerTick => 0;

    public static FireControlGeneral DefaultComponent(Ship containingShip)
    {
        return new FireControlGeneral()
        {
            ComponentMaxHitPoints = 400,
            ComponentHitPoints = 400,
            Status = ComponentStatus.Undamaged,
            _activeBuff = new DynamicBuff()
            {
                WeaponAccuracyFactor = 0.1f,

                SpeedFactor = 0,
                AcceleraionFactor = 0,
                WeaponRateOfFireFactor = 0f,
                WeaponVsStrikeCraftFactor = 0,
                RepairRateModifier = 0,
                ShieldRechargeRateModifier = 0
            },
            PowerUsage = 1,
            _containingShip = containingShip
        };
    }

    public static FireControlGeneral DefaultComponent(ObjectFactory.ShipSize grade, Ship containingShip)
    {
        FireControlGeneral res = DefaultComponent(containingShip);
        res.MinShipSize = grade;
        res.MaxShipSize = ObjectFactory.ShipSize.CapitalShip;
        switch (grade)
        {
            case ObjectFactory.ShipSize.Sloop:
                res.ComponentHitPoints = res.ComponentMaxHitPoints = 300;
                break;
            case ObjectFactory.ShipSize.Frigate:
                res.ComponentHitPoints = res.ComponentMaxHitPoints = 400;
                break;
            case ObjectFactory.ShipSize.Destroyer:
                res.ComponentHitPoints = res.ComponentMaxHitPoints = 600;
                break;
            case ObjectFactory.ShipSize.Cruiser:
                res.ComponentHitPoints = res.ComponentMaxHitPoints = 800;
                break;
            case ObjectFactory.ShipSize.CapitalShip:
                res.ComponentHitPoints = res.ComponentMaxHitPoints = 1100;
                break;
            default:
                break;
        }
        return res;
    }

    private static readonly string[] _allowedSlotTypes = new string[] { "ShipSystem", "ShipSystemCenter" };
}
