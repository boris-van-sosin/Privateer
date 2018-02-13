using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShipComponent : MonoBehaviour
{
    public enum ComponentSlotType
    {
        // Generic:
        ShipSystem,
        // Weapons:
        SmallFixed, SmallBroadside, SmallBarbette, SmallTurret, SmallBarbetteDual, SmallTurretDual,
        MediumBroadside, MediumBarbette, MediumTurret, MediumBarbetteDualSmall, MediumTurretDualSmall,
        LargeBarbette, LargeTurret,
        SpecialWeapon,
        // Boarding tool
        BoardingTool,
        // Boarding / anti-boarding forces
        BoardingForce
    };

    public enum ComponentStatus { Undamaged, LightlyDamaged, HeavilyDamaged, KnockedOut, Destroyed };

    // The ship containing the turret:
    protected Ship _containingShip;
}

public class ActiveShipComponent : ShipComponent
{
    public int MaxHitpoints;
    public int HitPoints { get; protected set; }
    public int IsWorking { get; protected set; }
    public ComponentStatus Status { get; protected set; }
}
