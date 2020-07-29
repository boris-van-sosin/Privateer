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
        return new TurretDefinition()
        {
            Geometry = t.transform.ToSerializableHierarchy(),
            TurretAxis = t.TurretAxis,
            TurretType = t.TurretType,
            WeaponSize = t.TurretSize,
            WeaponType = t.TurretWeaponType
        };
    }
}
