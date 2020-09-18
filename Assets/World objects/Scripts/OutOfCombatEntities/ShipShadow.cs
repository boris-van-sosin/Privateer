
using System;
using System.Collections.Generic;
using System.Linq;

[Serializable]
public class ShipTemplate
{
    public string ShipHullProductionKey { get; set; }
    public string ShipClassName { get; set; }
    public ShipComponentList[] ShipComponents { get; set; }
    public TemplateTurretPlacement[] Turrets { get; set; }
    public WeaponControlGroupCfgPanel.WeaponsConfigCompiled WeaponConfig { get; set; }

    [Serializable]
    public struct ShipComponentList
    {
        public Ship.ShipSection Section { get; set; }
        public ShipComponentTemplateDefinition[] Components { get; set; }
    }

    [Serializable]
    public struct TemplateTurretPlacement
    {
        public string HardpointKey { get; set; }
        public string TurretType { get; set; }
        public string WeaponType { get; set; }
        public string WeaponSize { get; set; }
        public string WeaponNum { get; set; }
        public TurretMod[] InstalledMods { get; set; }
        public string[] AmmoTypes { get; set; }
        public bool AlternatingFire { get; set; }
    }

    public ShipShadow ToNewShip()
    {
        ShipShadow res = new ShipShadow()
        {
            ShipHullProductionKey = this.ShipHullProductionKey,
            ShipComponents = new Dictionary<Ship.ShipSection, ShipComponentDefinition[]>(),
            Turrets = this.Turrets.Select(t => ShipShadow.TurretPlacement.FromTemplate(t)).ToArray(),
            WeaponConfig = this.WeaponConfig
        };

        return res;
    }
}

[Serializable]
public class ShipComponentDefinition
{
    public ShipComponentTemplateDefinition ComponentTemplate { get; set; }
    public int MaxHitPoints { get; set; }
    public int CurrHitPoints { get; set; }
}

[Serializable]
public class ShipShadow
{
    public string ShipHullProductionKey { get; set; }
    public string ShipClassName { get; set; }
    public bool IsModifiedClass { get; set; }
    public ShipDisplayName ShipDisplayName { get; set; }
    public Dictionary<Ship.ShipSection, ShipComponentDefinition[]> ShipComponents { get; set; }
    public TurretPlacement[] Turrets { get; set; }
    public WeaponControlGroupCfgPanel.WeaponsConfigCompiled WeaponConfig { get; set; }
    public ShipCharacter[] Crew { get; set; }

    [Serializable]
    public struct TurretPlacement
    {
        public ShipTemplate.TemplateTurretPlacement Template;
        public int MaxHitPoints { get; set; }
        public int CurrHitPoints { get; set; }

        public static TurretPlacement FromTemplate(ShipTemplate.TemplateTurretPlacement t)
        {
            TurretPlacement res = new TurretPlacement() { Template = t };
            res.CurrHitPoints = res.MaxHitPoints = ObjectFactory.TurretMountMaxHitPoints(t.TurretType);
            return res;
        }
    }
}
