using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PowerPlant : ActiveShipComponent
{
    public int PowerOutput;
}

public class CapacitorBank : ActiveShipComponent
{
    public int Capacity;
}

public class ShieldGenerator : ActiveShipComponent
{
    public int MaxShieldPoints;
    public int CurrShieldPoints { get; private set; }
    public int MaxShieldPointRegeneration;
    public int PowerUsage;
    public int PowerPerShieldRegeneration;
    public int PowerToRestart;
    public float RestartDelay;
}

public class DamageControlNode : ActiveShipComponent
{
    public int PowerUsage;
    public int MaxHitPointRegeneration;
}

public class HeatExchange : ShipComponent
{
    public int CoolingRate;
}

public class ExtraArmour : ShipComponent
{
    public int ArmourAmount;
}
