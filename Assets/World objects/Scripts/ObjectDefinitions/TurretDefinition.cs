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
}
