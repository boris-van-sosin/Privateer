using System;
using System.Collections.Generic;
using System.Linq;

[Serializable]
public class TurretDefinition
{
    public HierarchyNode Geometry { get; set; }
    public TurretBase.RotationAxis TurretAxis { get; set; }
    public string TurretType { get; set; }
    public string WeaponNum { get; set; }
    public string WeaponSize { get; set; }
    public string WeaponType { get; set; }
    public ObjectFactory.WeaponBehaviorType BehaviorType { get; set; }

    public static TurretDefinition FromTurret(TurretBase t)
    {
        return FromTurret(t, "", "", "", "");
    }

    public static TurretDefinition FromTurret(TurretBase t, string meshABPath, string meshAssetPath, string partSysABPath, string partSysAssetPath)
    {
        ObjectFactory.WeaponBehaviorType behaviorType = t.BehaviorType;

        return new TurretDefinition()
        {
            Geometry = t.transform.ToSerializableHierarchy(meshABPath, meshAssetPath, partSysABPath, partSysAssetPath),
            TurretAxis = t.TurretAxis,
            TurretType = t.TurretType,
            WeaponNum = "1",
            WeaponSize = t.TurretWeaponSize,
            WeaponType = t.TurretWeaponType,
            BehaviorType = behaviorType
        };
    }

    public static bool IsTurretModCompatible(TurretDefinition def, TurretMod turretMod)
    {
        switch (def.BehaviorType)
        {
            case ObjectFactory.WeaponBehaviorType.Gun:
                return turretMod == TurretMod.None || 
                       turretMod == TurretMod.Accelerator ||
                       turretMod == TurretMod.AdvancedTargeting ||
                       turretMod == TurretMod.DualAmmoFeed||
                       turretMod == TurretMod.FastAutoloader ||
                       turretMod == TurretMod.Harpax;
            case ObjectFactory.WeaponBehaviorType.Beam:
            case ObjectFactory.WeaponBehaviorType.ContinuousBeam:
                return turretMod == TurretMod.None ||
                       turretMod == TurretMod.AdvancedTargeting ||
                       turretMod == TurretMod.ImprovedCapacitors;
            case ObjectFactory.WeaponBehaviorType.Unknown:
            case ObjectFactory.WeaponBehaviorType.Torpedo:
            case ObjectFactory.WeaponBehaviorType.BomberTorpedo:
            case ObjectFactory.WeaponBehaviorType.Special:
            default:
                return turretMod == TurretMod.None;
        }
    }
}
