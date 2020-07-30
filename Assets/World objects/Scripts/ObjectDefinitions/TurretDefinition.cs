using System;
using System.Collections.Generic;
using System.Linq;

[Serializable]
public class TurretDefinition
{
    public HierarchyNode Geometry { get; set; }
    public TurretBase.RotationAxis TurretAxis { get; set; }
    public ComponentSlotType TurretType { get; set; }
    public ObjectFactory.WeaponSize WeaponSize { get; set; }
    public ObjectFactory.WeaponType WeaponType { get; set; }

    public static TurretDefinition FromTurret(TurretBase t)
    {
        return FromTurret(t, "", "", "", "");
    }

    public static TurretDefinition FromTurret(TurretBase t, string meshABPath, string meshAssetPath, string partSysABPath, string partSysAssetPath)
    {
        return new TurretDefinition()
        {
            Geometry = t.transform.ToSerializableHierarchy(meshABPath, meshAssetPath, partSysABPath, partSysAssetPath),
            TurretAxis = t.TurretAxis,
            TurretType = t.TurretType,
            WeaponSize = t.TurretSize,
            WeaponType = t.TurretWeaponType
        };
    }
}
