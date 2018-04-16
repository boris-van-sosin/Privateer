﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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
    BoardingForce,
    // Engine
    Engine,
    // Special
    Hidden
};

public enum ComponentStatus { Undamaged, LightlyDamaged, HeavilyDamaged, KnockedOut, Destroyed };

public delegate void ComponentHitpointsChangedDelegate();
public delegate void ComponentToggledDelegate(bool active);

public interface IShipComponent
{
    Ship ContainingShip { get; }
    ComponentSlotType ComponentType { get; }
    ObjectFactory.ShipSize MinShipSize { get; }
    ObjectFactory.ShipSize MaxShipSize { get; }
}

public interface IShipActiveComponent : IShipComponent
{
    int ComponentMaxHitPoints { get; }
    int ComponentHitPoints { get; set; }
    bool ComponentIsWorking { get; }
    ComponentStatus Status { get; }
    string SpriteKey { get; }
    event ComponentHitpointsChangedDelegate OnHitpointsChanged;
}

public interface ITurret : IShipActiveComponent
{
    void ManualTarget(Vector3 target);
    void Fire(Vector3 target);
    bool HasGrapplingTool();
    void FireGrapplingTool(Vector3 target);
    void SetTurretBehavior(TurretBase.TurretMode newMode);
    TurretBase.TurretMode GetTurretBehavior();
    float CurrAngle { get; }
    float CurrLocalAngle { get; }
    float GetMaxRange { get; }
}

public interface IPeriodicActionComponent : IShipComponent
{
    void PeriodicAction();
}

public interface IUserActivatedComponent : IShipComponent
{
    bool CanActivate(Vector3 target);
    void Activate(Vector3 target);
}

public interface IUserToggledComponent : IShipComponent
{
    bool ComponentActive { get; set; }
    event ComponentToggledDelegate OnToggle;
}

public interface IEnergyCapacityComponent
{
    int EnergyCapacity { get; }
}

public interface IShieldComponent : IShipComponent, IUserToggledComponent
{
    int MaxShieldPoints { get; }
    int CurrShieldPoints { get; set; }
}
