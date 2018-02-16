using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class ShipComponentBase : IShipComponent
{
    public Ship ContainingShip { get { return _containingShip; } }

    protected Ship _containingShip;
}

public abstract class ShipActiveComponentBase : ShipComponentBase, IShipActiveComponent
{
    public int ComponentMaxHitpoints { get; protected set; }
    public int ComponentHitPoints { get; protected set; }
    public bool ComponentIsWorking { get; protected set; }
    public ComponentStatus Status { get; protected set; }
}
