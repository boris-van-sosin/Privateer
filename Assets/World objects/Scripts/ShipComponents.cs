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



public class PowerPlant : ShipActiveComponentBase, IEnergyUsingComponent, IHeatUsingComponent
{
    public int PowerOutput;

    public int EnergyDelta
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
}

public class ShieldGenerator : ShipActiveComponentBase, IEnergyUsingComponent, IHeatUsingComponent
{
    public int MaxShieldPoints;
    public int CurrShieldPoints { get; private set; }
    public int MaxShieldPointRegeneration;
    public int PowerUsage;
    public int PowerPerShieldRegeneration;
    public int PowerToRestart;
    public float RestartDelay;

    public int EnergyDelta
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
    }

}

public class DamageControlNode : ShipActiveComponentBase, IEnergyUsingComponent, IHeatUsingComponent
{
    public int PowerUsage;
    public int MaxHitPointRegeneration;

    public int EnergyDelta
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
    }

}

public class HeatExchange : ShipComponentBase, IHeatUsingComponent
{
    public int CoolingRate;

    public int HeatDelta
    {
        get
        {
            return -CoolingRate;
        }
    }
}

public class ExtraArmour : ShipComponentBase
{
    public int ArmourAmount;
}
