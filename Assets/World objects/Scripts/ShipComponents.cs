﻿using System.Collections;
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

    public int ComponentMaxHitpoints { get { return _innerTurret.ComponentMaxHitpoints; } }

    public int ComponentHitPoints { get { return _innerTurret.ComponentHitPoints; } }

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



public class PowerPlant : ShipActiveComponentBase, IPeriodicActionComponent//, IEnergyUsingComponent, IHeatUsingComponent
{
    public int PowerOutput, HeatOutput;

    public void PeriodicAction()
    {
        ContainingShip.TryChangeEnergyAndHeat(PowerOutput, HeatOutput);
    }

    /*public int EnergyDelta
    {
        get
        {
            return PowerOutput;
        }
    }

    public int HeatDelta
    {
        get
        {
            return 5;
        }
    }*/

    public static PowerPlant DefaultComponent(Ship containingShip)
    {
        return new PowerPlant()
        {
            ComponentMaxHitpoints = 40,
            ComponentHitPoints = 40,
            ComponentIsWorking = true,
            Status = ComponentStatus.Undamaged,
            PowerOutput = 10,
            HeatOutput = 4,
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
            ComponentMaxHitpoints = 40,
            ComponentHitPoints = 40,
            ComponentIsWorking = true,
            Status = ComponentStatus.Undamaged,
            Capacity = 50,
            _containingShip = containingShip
        };
    }
}

public class ShieldGenerator : ShipActiveComponentBase, IPeriodicActionComponent//, IEnergyUsingComponent, IHeatUsingComponent
{
    public int MaxShieldPoints;
    public int CurrShieldPoints { get; private set; }
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

    /*public int EnergyDelta
    {
        get
        {
            return -PowerUsage;
        }
    }

    public int HeatDelta
    {
        get
        {
            return 0;
        }
    }*/
    public void PeriodicAction()
    {
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
            ComponentMaxHitpoints = 40,
            ComponentHitPoints = 40,
            ComponentIsWorking = true,
            Status = ComponentStatus.Undamaged,
            MaxShieldPoints = 100,
            CurrShieldPoints = 100,
            MaxShieldPointRegeneration = 4,
            PowerUsage = 5,
            HeatGeneration = 1,
            PowerPerShieldRegeneration = 10,
            HeatGenerationPerShieldRegeneration = 5,
            PowerToRestart = 20,
            HeatToRestart = 10,
            RestartDelay = 20,
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
        // nothing for now
    }

    public static DamageControlNode DefaultComponent(Ship containingShip)
    {
        return new DamageControlNode()
        {
            ComponentMaxHitpoints = 40,
            ComponentHitPoints = 40,
            ComponentIsWorking = true,
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
            CoolingRate = 10,
            _containingShip = containingShip
        };
    }
}

public class ExtraArmour : ShipComponentBase
{
    public int ArmourAmount;
}
