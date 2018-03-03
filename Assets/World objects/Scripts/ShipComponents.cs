using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class TurretComponent : ITurret
{
    public TurretComponent(TurretBase t)
    {
        _innerTurret = t;
    }

    public float CurrAngle { get { return _innerTurret.CurrAngle; } }

    public float CurrLocalAngle { get { return _innerTurret.CurrAngle; } }

    public int ComponentMaxHitPoints { get { return _innerTurret.ComponentMaxHitPoints; } }
    
    public int ComponentHitPoints { get { return _innerTurret.ComponentHitPoints; } set { _innerTurret.ComponentHitPoints = value; if (OnHitpointsChanged != null) { OnHitpointsChanged(); } } }

    public bool ComponentIsWorking { get { return _innerTurret.ComponentIsWorking; } }

    public ComponentStatus Status { get { return _innerTurret.Status; } }

    public Ship ContainingShip { get { return _innerTurret.ContainingShip; } }

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

    public TurretBase.TurretMode GetTurretBehavior()
    {
        return _innerTurret.GetTurretBehavior();
    }

    public float GetMaxRange { get { return _innerTurret.GetMaxRange; } }

    public ComponentSlotType ComponentType { get { return _innerTurret.ComponentType; } }

    public string SpriteKey { get { return "Turret"; } }

    private TurretBase _innerTurret;

    public event ComponentHitpointsChangedDelegate OnHitpointsChanged;
}

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

    public override ComponentSlotType ComponentType { get { return ComponentSlotType.ShipSystem; } }

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

    public override ComponentSlotType ComponentType { get { return ComponentSlotType.ShipSystem; } }

    public static CapacitorBank DefaultComponent(Ship containingShip)
    {
        return new CapacitorBank()
        {
            Capacity = 50,
            _containingShip = containingShip
        };
    }
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

    public int MaxShieldPointRegeneration;
    public int PowerUsage;
    public int PowerPerShieldRegeneration;
    public int PowerToRestart;
    public float RestartDelay;
    public int HeatGeneration;
    public int HeatGenerationPerShieldRegeneration;
    public int HeatToRestart;
    private bool _isShieldActive;
    private int _ticksSinceInactive = 0;
    private int _currShieldPoints;

    public void PeriodicAction()
    {
        if (!ComponentIsWorking)
        {
            return;
        }
        if (!ContainingShip.TryChangeEnergyAndHeat(PowerUsage, HeatGeneration))
        {
            CurrShieldPoints = 0;
            _isShieldActive = false;
            _ticksSinceInactive = 0;
        }
        if (_isShieldActive && CurrShieldPoints < MaxShieldPoints)
        {
            if (ContainingShip.TryChangeEnergyAndHeat(PowerPerShieldRegeneration, HeatGenerationPerShieldRegeneration))
            {
                CurrShieldPoints = System.Math.Min(MaxShieldPoints, CurrShieldPoints + MaxShieldPointRegeneration);
            }
        }
        if (!_isShieldActive)
        {
            if (++_ticksSinceInactive >= RestartDelay && ContainingShip.TryChangeEnergyAndHeat(PowerToRestart, HeatToRestart))
            {
                _isShieldActive = true;
            }
        }
    }

    public override ComponentSlotType ComponentType { get { return ComponentSlotType.ShipSystem; } }

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
            MaxShieldPointRegeneration = 8,
            PowerUsage = 2,
            HeatGeneration = 1,
            PowerPerShieldRegeneration = 3,
            HeatGenerationPerShieldRegeneration = 2,
            PowerToRestart = 20,
            HeatToRestart = 10,
            RestartDelay = 20 * 4,
            _containingShip = containingShip
        };
    }
}

public class DamageControlNode : ShipActiveComponentBase, IPeriodicActionComponent//, IEnergyUsingComponent, IHeatUsingComponent
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

        if (!ContainingShip.TryChangeEnergyAndHeat(PowerUsage, 0))
        {
            return;
        }

        if (!componentsAtFull)
        {
            int SystemPointRegenLeft = SystemMaxHitPointRegeneration;
            foreach (IShipActiveComponent c in _containingShip.AllComponents.Where(x =>  x is IShipActiveComponent).Select(y => y as IShipActiveComponent).Where(z => z.Status != ComponentStatus.Destroyed))
            {
                if (c.ComponentMaxHitPoints - c.ComponentHitPoints <= SystemPointRegenLeft)
                {
                    SystemPointRegenLeft -= (c.ComponentMaxHitPoints - c.ComponentHitPoints);
                    c.ComponentHitPoints = c.ComponentMaxHitPoints;
                }
                else
                {
                    c.ComponentHitPoints += SystemPointRegenLeft;
                    SystemPointRegenLeft = 0;
                    break;
                }
            }
        }
        _containingShip.RepairHull(HullMaxHitPointRegeneration);
        if (!armourAtFull)
        {
            _containingShip.RepairArmour(ArmorMaxPointRegeneration);
        }
    }

    public override ComponentSlotType ComponentType { get { return ComponentSlotType.ShipSystem; } }

    public override string SpriteKey { get { return "Damage control"; } }

    public static DamageControlNode DefaultComponent(Ship containingShip)
    {
        return new DamageControlNode()
        {
            ComponentMaxHitPoints = 400,
            ComponentHitPoints = 400,
            Status = ComponentStatus.Undamaged,
            HullMaxHitPointRegeneration = 10,
            ArmorMaxPointRegeneration = 1,
            SystemMaxHitPointRegeneration = 2,
            TimeOutOfCombatToRepair = 5.0f,
            PowerUsage = 10,
            _containingShip = containingShip
        };
    }
}

public class HeatExchange : ShipComponentBase, IPeriodicActionComponent
{
    public int CoolingRate;

    public void PeriodicAction()
    {
        ContainingShip.TryChangeHeat(-CoolingRate);
    }

    public override ComponentSlotType ComponentType { get { return ComponentSlotType.ShipSystem; } }

    public static HeatExchange DefaultComponent(Ship containingShip)
    {
        return new HeatExchange()
        {
            CoolingRate = 6,
            _containingShip = containingShip
        };
    }
}

public class ExtraArmour : ShipComponentBase
{
    public int ArmourAmount;
    public override ComponentSlotType ComponentType { get { return ComponentSlotType.ShipSystem; } }
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
            _active = value;
        }
    }

    public bool ThrustWorks { get; private set; }

    public void PeriodicAction()
    {
        if (ComponentIsWorking && _active && ContainingShip.TryChangeEnergyAndHeat(-EnergyPerThrust, HeatPerThrust))
        {
            ThrustWorks = true;
            _nextDeactivate = true;
        }
        else
        {
            ThrustWorks = false;
        }
        if (_nextDeactivate)
        {
            _active = false;
            _nextDeactivate = false;
        }
    }

    private bool _nextDeactivate = false;

    public override ComponentSlotType ComponentType { get { return ComponentSlotType.Engine; } }

    public override string SpriteKey { get { return "Engine"; } }

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
}
