using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TurretComponent : ITurret
{
    public TurretComponent(TurretBase t)
    {
        _innerTurret = t;
    }

    public float CurrAngle { get { return _innerTurret.CurrAngle; } }

    public float CurrLocalAngle { get { return _innerTurret.CurrAngle; } }

    public int ComponentMaxHitPoints { get { return _innerTurret.ComponentMaxHitPoints; } }

    public int ComponentHitPoints { get { return _innerTurret.ComponentHitPoints; } set { _innerTurret.ComponentHitPoints = value; } }

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

    private TurretBase _innerTurret;
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

public class CapacitorBank : ShipActiveComponentBase, IEnergyCapacityComponent
{
    public int Capacity;

    public int EnergyCapacity
    {
        get
        {
            return Capacity;
        }
    }

    public static CapacitorBank DefaultComponent(Ship containingShip)
    {
        return new CapacitorBank()
        {
            ComponentMaxHitPoints = 400,
            ComponentHitPoints = 400,
            Status = ComponentStatus.Undamaged,
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
    public int MaxArmorPointRegeneration;

    public void PeriodicAction()
    {
        if (!ComponentIsWorking)
        {
            return;
        }
        // nothing for now
    }

    public static DamageControlNode DefaultComponent(Ship containingShip)
    {
        return new DamageControlNode()
        {
            ComponentMaxHitPoints = 400,
            ComponentHitPoints = 400,
            Status = ComponentStatus.Undamaged,
            HullMaxHitPointRegeneration = 10,
            MaxArmorPointRegeneration = 1,
            SystemMaxHitPointRegeneration = 2,
            PowerUsage = 10,
            _containingShip = containingShip
        };
    }
}

public class HeatExchange : ShipComponentBase, IPeriodicActionComponent//, IHeatUsingComponent
{
    public int CoolingRate;

    public void PeriodicAction()
    {
        ContainingShip.TryChangeHeat(-CoolingRate);
    }

    public static HeatExchange DefaultComponent(Ship containingShip)
    {
        return new HeatExchange()
        {
            CoolingRate = 4,
            _containingShip = containingShip
        };
    }
}

public class ExtraArmour : ShipComponentBase
{
    public int ArmourAmount;
}
