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

    public PowerPlant(int maxHitPoints, int hitPoints, int powerOutput, int headOutput, ObjectFactory.ShipSize minShipSize, ObjectFactory.ShipSize maxShipSize)
    {
        ComponentGlobalMaxHitPoints = maxHitPoints;
        ComponentHitPoints = hitPoints;
        PowerOutput = powerOutput;
        HeatOutput = headOutput;
        MinShipSize = minShipSize;
        MaxShipSize = maxShipSize;
    }

    public static PowerPlant DefaultComponent(Ship containingShip)
    {
        PowerPlant res = new PowerPlant(400, 400, 3, 1, ObjectFactory.ShipSize.Sloop, ObjectFactory.ShipSize.CapitalShip);
        res._containingShip = containingShip;
        return res;
    }

    public static PowerPlant DefaultComponent(ObjectFactory.ShipSize grade, Ship containingShip)
    {
        PowerPlant res = null;
        switch (grade)
        {
            case ObjectFactory.ShipSize.Sloop:
                res = new PowerPlant(300, 300, 6, 1, grade, ObjectFactory.ShipSize.CapitalShip);
                break;
            case ObjectFactory.ShipSize.Frigate:
                res = new PowerPlant(400, 400, 6, 1, grade, ObjectFactory.ShipSize.CapitalShip);
                break;
            case ObjectFactory.ShipSize.Destroyer:
                res = new PowerPlant(600, 600, 8, 2, grade, ObjectFactory.ShipSize.CapitalShip);
                break;
            case ObjectFactory.ShipSize.Cruiser:
                res = new PowerPlant(800, 800, 10, 3, grade, ObjectFactory.ShipSize.CapitalShip);
                break;
            case ObjectFactory.ShipSize.CapitalShip:
                res = new PowerPlant(1100, 1100, 10, 3, grade, ObjectFactory.ShipSize.CapitalShip);
                break;
            default:
                return null;
        }
        res._containingShip = containingShip;
        return res;
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

    public CapacitorBank(int powerCapacity, ObjectFactory.ShipSize minShipSize, ObjectFactory.ShipSize maxShipSize)
    {
        Capacity = powerCapacity;
        MinShipSize = minShipSize;
        MaxShipSize = maxShipSize;
    }

    public static CapacitorBank DefaultComponent(Ship containingShip)
    {
        CapacitorBank res = new CapacitorBank(50, ObjectFactory.ShipSize.Sloop, ObjectFactory.ShipSize.CapitalShip);
        res._containingShip = containingShip;
        return res;
    }

    private static readonly string[] _allowedSlotTypes = new string[] { "ShipSystem", "ShipSystemCenter" };
}

public class HeatSink : ShipComponentBase, IHeatCapacityComponent
{
    public int Capacity;

    public override IEnumerable<string> AllowedSlotTypes { get { return _allowedSlotTypes; } }

    public int HeatCapacity => Capacity;

    public HeatSink(int heatCapacity, ObjectFactory.ShipSize minShipSize, ObjectFactory.ShipSize maxShipSize)
    {
        Capacity = heatCapacity;
        MinShipSize = minShipSize;
        MaxShipSize = maxShipSize;
    }

    public static HeatSink DefaultComponent(Ship containingShip)
    {
        HeatSink res = new HeatSink(50, ObjectFactory.ShipSize.Sloop, ObjectFactory.ShipSize.CapitalShip);
        res._containingShip = containingShip;
        return res;
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

    public ShieldGenerator(int maxHitPoints, int hitPoints, int maxShieldPoints, int shieldPoints,
                           int maxShieldPointRegeneration, int powerUsage, int heatGeneration, int powerPerRegen, int heatPerRegen,
                           int powerToRestart, int heatToRestart, float restartDelay,
                           ObjectFactory.ShipSize minShipSize, ObjectFactory.ShipSize maxShipSize)
    {
        ComponentGlobalMaxHitPoints = maxHitPoints;
        ComponentHitPoints = hitPoints;
        MaxShieldPoints = maxShieldPoints;
        CurrShieldPoints = shieldPoints;
        MaxShieldPointRegeneration = maxShieldPointRegeneration;
        PowerUsage = powerUsage;
        HeatGeneration = heatGeneration;
        PowerPerShieldRegeneration = powerPerRegen;
        HeatGenerationPerShieldRegeneration = heatPerRegen;
        PowerToRestart = powerToRestart;
        HeatToRestart = heatToRestart;
        RestartDelay = restartDelay;
        MinShipSize = minShipSize;
        MaxShipSize = maxShipSize;
        _shieldRequired = true;
    }

    public static ShieldGenerator DefaultComponent(Ship containingShip)
    {
        ShieldGenerator res = new ShieldGenerator(400, 400, 1000, 1000, 2, 2, 1, 3, 2, 20, 10, 20 * 4, ObjectFactory.ShipSize.Sloop, ObjectFactory.ShipSize.CapitalShip);
        res._containingShip = containingShip;
        return res;
    }

    public static ShieldGenerator DefaultComponent(ObjectFactory.ShipSize grade, Ship containingShip)
    {
        ShieldGenerator res = DefaultComponent(containingShip);
        res.MinShipSize = grade;
        res.MaxShipSize = ObjectFactory.ShipSize.CapitalShip;
        switch (grade)
        {
            case ObjectFactory.ShipSize.Sloop:
                res.ComponentHitPoints = res.ComponentGlobalMaxHitPoints = 300;
                break;
            case ObjectFactory.ShipSize.Frigate:
                res.ComponentHitPoints = res.ComponentGlobalMaxHitPoints = 400;
                break;
            case ObjectFactory.ShipSize.Destroyer:
                res.ComponentHitPoints = res.ComponentGlobalMaxHitPoints = 600;
                break;
            case ObjectFactory.ShipSize.Cruiser:
                res.ComponentHitPoints = res.ComponentGlobalMaxHitPoints = 800;
                break;
            case ObjectFactory.ShipSize.CapitalShip:
                res.ComponentHitPoints = res.ComponentGlobalMaxHitPoints = 1100;
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
    public int HeatGeneration;
    public int HullMaxHitPointRegeneration;
    public int SystemMaxHitPointRegeneration;
    public int ArmourMaxPointRegeneration;
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

        if (!ContainingShip.TryChangeEnergyAndHeat(-PowerUsage, HeatGeneration))
        {
            return;
        }

        if (!componentsAtFull)
        {
            int systemPointRegenLeft = SystemMaxHitPointRegeneration;
            while (systemPointRegenLeft > 0)
            {
                (int, bool) repairRes = _containingShip.RepairComponents(systemPointRegenLeft);
                if (repairRes.Item2)
                {
                    break;
                }
                systemPointRegenLeft = repairRes.Item1;
            }
        }
        _containingShip.RepairHull(HullMaxHitPointRegeneration);
        if (!armourAtFull)
        {
            _containingShip.RepairArmour(ArmourMaxPointRegeneration);
        }
    }

    public override IEnumerable<string> AllowedSlotTypes { get { return _allowedSlotTypes; } }

    public override string SpriteKey { get { return "Damage control"; } }

    public int EnergyPerTick => PowerUsage;
    public int HeatPerTick => HeatGeneration;

    public DamageControlNode(int maxHitPoints, int hitPoints,
                             int hullHitPointRegen, int armourPointRegen, int systemHitPointRegen,
                             int powerUsage, int heatGeneration,
                             float timeOutOfCombatToRepair,
                             ObjectFactory.ShipSize minShipSize, ObjectFactory.ShipSize maxShipSize)
    {
        ComponentGlobalMaxHitPoints = maxHitPoints;
        ComponentHitPoints = hitPoints;
        HullMaxHitPointRegeneration = hullHitPointRegen;
        ArmourMaxPointRegeneration = armourPointRegen;
        SystemMaxHitPointRegeneration = systemHitPointRegen;
        TimeOutOfCombatToRepair = timeOutOfCombatToRepair;
        PowerUsage = powerUsage;
        HeatGeneration = heatGeneration;
        MinShipSize = minShipSize;
        MaxShipSize = maxShipSize;
    }

    public static DamageControlNode DefaultComponent(Ship containingShip)
    {
        DamageControlNode res = new DamageControlNode(400, 400, 2, 1, 2, 5, 0, 5.0f, ObjectFactory.ShipSize.Sloop, ObjectFactory.ShipSize.CapitalShip);
        res._containingShip = containingShip;
        return res;
    }

    public static DamageControlNode DefaultComponent(ObjectFactory.ShipSize grade, Ship containingShip)
    {
        DamageControlNode res = DefaultComponent(containingShip);
        res.MinShipSize = grade;
        res.MaxShipSize = ObjectFactory.ShipSize.CapitalShip;
        switch (grade)
        {
            case ObjectFactory.ShipSize.Sloop:
                res.ComponentHitPoints = res.ComponentGlobalMaxHitPoints = 300;
                break;
            case ObjectFactory.ShipSize.Frigate:
                res.ComponentHitPoints = res.ComponentGlobalMaxHitPoints = 400;
                break;
            case ObjectFactory.ShipSize.Destroyer:
                res.ComponentHitPoints = res.ComponentGlobalMaxHitPoints = 600;
                break;
            case ObjectFactory.ShipSize.Cruiser:
                res.ComponentHitPoints = res.ComponentGlobalMaxHitPoints = 800;
                break;
            case ObjectFactory.ShipSize.CapitalShip:
                res.ComponentHitPoints = res.ComponentGlobalMaxHitPoints = 1100;
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
    public int HeatPerTick => -CoolingRate;

    public HeatExchange(int coolingRate, ObjectFactory.ShipSize minShipSize, ObjectFactory.ShipSize maxShipSize)
    {
        CoolingRate = coolingRate;
        MinShipSize = minShipSize;
        MaxShipSize = maxShipSize;
    }
    public static HeatExchange DefaultComponent(Ship containingShip)
    {
        HeatExchange res = new HeatExchange(6, ObjectFactory.ShipSize.Sloop, ObjectFactory.ShipSize.CapitalShip);
        res._containingShip = containingShip;
        return res;
    }

    private static readonly string[] _allowedSlotTypes = new string[] { "ShipSystem", "ShipSystemCenter" };
}

public class ExtraArmour : ShipComponentBase
{
    public int ArmourAmount;
    public int MitigationArmourAmount;
    public override IEnumerable<string> AllowedSlotTypes { get { return _allowedSlotTypes; } }

    public ExtraArmour(int armourAmount, int mitigationArmourAmount, ObjectFactory.ShipSize minShipSize, ObjectFactory.ShipSize maxShipSize)
    {
        ArmourAmount = armourAmount;
        MitigationArmourAmount = mitigationArmourAmount;
        MinShipSize = minShipSize;
        MaxShipSize = maxShipSize;
    }

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

    public ShipEngine(int maxHitPoints, int hitPoints, int energyPerThrust, int heatPerThrust, ObjectFactory.ShipSize minShipSize, ObjectFactory.ShipSize maxShipSize)
    {
        ComponentGlobalMaxHitPoints = maxHitPoints;
        ComponentHitPoints = hitPoints;
        EnergyPerThrust = energyPerThrust;
        HeatPerThrust = heatPerThrust;
        MinShipSize = minShipSize;
        MaxShipSize = maxShipSize;
    }
    public static ShipEngine DefaultComponent(Ship containingShip)
    {
        ShipEngine res = new ShipEngine(600, 600, 2, 2, ObjectFactory.ShipSize.Sloop, ObjectFactory.ShipSize.CapitalShip);
        res._containingShip = containingShip;
        return res;
    }

    public static ShipEngine DefaultComponent(ObjectFactory.ShipSize grade, Ship containingShip)
    {
        ShipEngine res = DefaultComponent(containingShip);
        res.MinShipSize = grade;
        res.MaxShipSize = ObjectFactory.ShipSize.CapitalShip;
        switch (grade)
        {
            case ObjectFactory.ShipSize.Sloop:
                res.ComponentHitPoints = res.ComponentGlobalMaxHitPoints = 450;
                break;
            case ObjectFactory.ShipSize.Frigate:
                res.ComponentHitPoints = res.ComponentGlobalMaxHitPoints = 600;
                break;
            case ObjectFactory.ShipSize.Destroyer:
                res.ComponentHitPoints = res.ComponentGlobalMaxHitPoints = 900;
                break;
            case ObjectFactory.ShipSize.Cruiser:
                res.ComponentHitPoints = res.ComponentGlobalMaxHitPoints = 1200;
                break;
            case ObjectFactory.ShipSize.CapitalShip:
                res.ComponentHitPoints = res.ComponentGlobalMaxHitPoints = 1650;
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

    public ElectromagneticClamps(int maxHitPoints, int hitPoints, int energyPerPulse, int heatPerPulse, ObjectFactory.ShipSize minShipSize, ObjectFactory.ShipSize maxShipSize)
    {
        ComponentGlobalMaxHitPoints = maxHitPoints;
        ComponentHitPoints = hitPoints;
        EnergyPerPulse = energyPerPulse;
        HeatPerPulse = heatPerPulse;
        MinShipSize = minShipSize;
        MaxShipSize = maxShipSize;
    }

    public static ElectromagneticClamps DefaultComponent(Ship containingShip)
    {
        ElectromagneticClamps res = new ElectromagneticClamps(1, 1, 1, 0, ObjectFactory.ShipSize.Sloop, ObjectFactory.ShipSize.CapitalShip);
        res._containingShip = containingShip;
        return res;
    }

    private static readonly string[] _allowedSlotTypes = new string[] { "Hidden" };
}

public class ShipArmoury : ShipComponentBase
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

    public ShipArmoury(int crewCapacity, ObjectFactory.ShipSize minShipSize, ObjectFactory.ShipSize maxShipSize)
    {
        CrewCapacity = crewCapacity;
        Forces = new List<ShipCharacter>(crewCapacity);
        MinShipSize = minShipSize;
        MaxShipSize = maxShipSize;
    }

    public static ShipArmoury DefaultComponent(Ship s)
    {
        ShipArmoury res = new ShipArmoury(10, ObjectFactory.ShipSize.Sloop, ObjectFactory.ShipSize.CapitalShip);
        res._containingShip = s;
        return res;
    }

    public static ShipArmoury DefaultComponent(ObjectFactory.ShipSize grade, Ship s)
    {
        ShipArmoury res = DefaultComponent(s);
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
                return null;
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
    public int HeatGeneration;
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
    public int HeatPerTick => HeatGeneration;

    public FireControlGeneral(int maxHitPoints, int hitPoints, float weaponAccuracyFactor, int powerUsage, int heatGeneration, ObjectFactory.ShipSize minShipSize, ObjectFactory.ShipSize maxShipSize)
    {
        ComponentGlobalMaxHitPoints = maxHitPoints;
        ComponentHitPoints = hitPoints;
        _activeBuff = new DynamicBuff()
        {
            WeaponAccuracyFactor = weaponAccuracyFactor,

            SpeedFactor = 0,
            AcceleraionFactor = 0,
            WeaponRateOfFireFactor = 0f,
            WeaponVsStrikeCraftFactor = 0,
            RepairRateModifier = 0,
            ShieldRechargeRateModifier = 0
        };
        PowerUsage = powerUsage;
        HeatGeneration = heatGeneration;
        MinShipSize = minShipSize;
        MaxShipSize = maxShipSize;
    }

    public static FireControlGeneral DefaultComponent(Ship containingShip)
    {
        FireControlGeneral res = new FireControlGeneral(400, 400, 0.1f, 4, 0, ObjectFactory.ShipSize.Sloop, ObjectFactory.ShipSize.CapitalShip);
        res._containingShip = containingShip;
        return res;
    }

    public static FireControlGeneral DefaultComponent(ObjectFactory.ShipSize grade, Ship containingShip)
    {
        FireControlGeneral res = DefaultComponent(containingShip);
        res.MinShipSize = grade;
        res.MaxShipSize = ObjectFactory.ShipSize.CapitalShip;
        switch (grade)
        {
            case ObjectFactory.ShipSize.Sloop:
                res.ComponentHitPoints = res.ComponentGlobalMaxHitPoints = 300;
                break;
            case ObjectFactory.ShipSize.Frigate:
                res.ComponentHitPoints = res.ComponentGlobalMaxHitPoints = 400;
                break;
            case ObjectFactory.ShipSize.Destroyer:
                res.ComponentHitPoints = res.ComponentGlobalMaxHitPoints = 600;
                break;
            case ObjectFactory.ShipSize.Cruiser:
                res.ComponentHitPoints = res.ComponentGlobalMaxHitPoints = 800;
                break;
            case ObjectFactory.ShipSize.CapitalShip:
                res.ComponentHitPoints = res.ComponentGlobalMaxHitPoints = 1100;
                break;
            default:
                break;
        }
        return res;
    }

    private static readonly string[] _allowedSlotTypes = new string[] { "ShipSystem", "ShipSystemCenter" };
}
