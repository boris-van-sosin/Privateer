using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//public enum ComponentSlotType
//{
//    // Generic:
//    ShipSystem, ShipSystemCenter,
//    // Weapons:
//    SmallFixed, SmallBroadside, SmallBarbette, SmallTurret, SmallBarbetteDual, SmallTurretDual,
//    MediumBroadside, MediumBarbette, MediumTurret, MediumBarbetteDualSmall, MediumTurretDualSmall,
//    LargeBroadside, LargeBarbetteSingle, LargeBarbette, LargeTurret,
//    TorpedoTube, SpecialWeapon,
//    FighterCannon, FighterAutogun, BomberAutogun, BomberTorpedoTube,
//    // Boarding tool
//    BoardingTool,
//    // Boarding / anti-boarding forces
//    BoardingForce,
//    // Engine
//    Engine,
//    // Special
//    Hidden
//};

public enum ComponentStatus { Undamaged, LightlyDamaged, HeavilyDamaged, KnockedOut, Destroyed };

public interface IShipComponent
{
    ShipBase ContainingShip { get; }
    IEnumerable<string> AllowedSlotTypes { get; }
    ObjectFactory.ShipSize MinShipSize { get; }
    ObjectFactory.ShipSize MaxShipSize { get; }
    DynamicBuff ComponentBuff { get; }
}

public interface IShipActiveComponent : IShipComponent
{
    int ComponentGlobalMaxHitPoints { get; }
    int ComponentMaxHitPoints { get; }
    int ComponentHitPoints { get; set; }
    bool ComponentIsWorking { get; }
    ComponentStatus Status { get; }
    string SpriteKey { get; }
    void ApplyHitPointBuff(StaticBuff.HitPointBuff b);
    event Action OnHitpointsChanged;
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
    bool IsOutOfAmmo { get; }
    bool ReadyToFire();
    void ApplyBuff(DynamicBuff b);
    IList<ObjectFactory.TacMapEntityType> TargetPriorityList { get; }
    TurretAIHint HardpointAIHint { get; }
}

public interface IPeriodicActionComponent : IShipComponent
{
    void PeriodicAction();
    int EnergyPerTick { get; }
    int HeatPerTick { get; }
}

public interface IUserActivatedComponent : IShipComponent
{
    bool CanActivate(Vector3 target);
    void Activate(Vector3 target);
}

public interface IUserToggledComponent : IShipComponent
{
    bool ComponentActive { get; set; }
    event Action<bool> OnToggle;
}

public interface IEnergyCapacityComponent
{
    int EnergyCapacity { get; }
}

public interface IHeatCapacityComponent
{
    int HeatCapacity { get; }
}

public interface IShieldComponent : IShipComponent, IUserToggledComponent
{
    int MaxShieldPoints { get; }
    int CurrShieldPoints { get; set; }
}
