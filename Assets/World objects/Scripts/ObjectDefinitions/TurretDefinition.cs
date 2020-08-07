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
}
