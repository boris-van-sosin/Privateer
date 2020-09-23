
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
            ShipClassName = this.ShipClassName,
            ShipComponents = new Dictionary<Ship.ShipSection, ShipComponentDefinition[]>(),
            Turrets = this.Turrets.Select(t => ShipShadow.TurretPlacement.FromTemplate(t)).ToArray(),
            WeaponConfig = this.WeaponConfig,
            IsModifiedClass = false
        };

        for (int i = 0; i < ShipComponents.Length; ++i)
        {
            ShipComponentDefinition[] currComps;
            if (ShipComponents[i].Components != null)
            {
                currComps = new ShipComponentDefinition[ShipComponents[i].Components.Length];
            }
            else
            {
                currComps = new ShipComponentDefinition[0];
            }
            res.ShipComponents.Add(ShipComponents[i].Section, currComps);

            if (ShipComponents[i].Components != null)
            {
                for (int j = 0; j < ShipComponents[i].Components.Length; ++j)
                {
                    if (ShipComponents[i].Components[j] != null)
                    {
                        currComps[j] = new ShipComponentDefinition()
                        {
                            ComponentTemplate = ShipComponents[i].Components[j],
                            MaxHitPoints = ShipComponents[i].Components[j].ComponentGlobalMaxHitPoints,
                            CurrHitPoints = ShipComponents[i].Components[j].ComponentGlobalMaxHitPoints
                        };
                    }
                    else
                    {
                        currComps[j] = null;
                    }
                }
            }
        }

        return res;
    }

    public ShipShadow ToNewShip(ShipDisplayName name)
    {
        ShipShadow res = ToNewShip();
        res.DisplayName = name;
        return res;
    }

    public ShipShadow ToNewShip(IEnumerable<ShipCharacter> characters)
    {
        ShipShadow res = ToNewShip();
        res.Crew = characters.ToArray();
        return res;
    }

    public ShipShadow ToNewShip(ShipDisplayName name, IEnumerable<ShipCharacter> characters)
    {
        ShipShadow res = ToNewShip(name);
        res.Crew = characters.ToArray();
        return res;
    }
}

[Serializable]
public class ShipComponentDefinition
{
    public ShipComponentTemplateDefinition ComponentTemplate { get; set; }
    public int MaxHitPoints { get; set; }
    public int CurrHitPoints { get; set; }
    public ShipComponentBase CreateComponent()
    {
        ShipComponentBase comp = ComponentTemplate.CreateComponent();
        if (comp is ShipActiveComponentBase compWithHitpoints)
        {
            compWithHitpoints.ComponentMaxHitPoints = MaxHitPoints;
            compWithHitpoints.ComponentHitPoints = CurrHitPoints;
        }
        return comp;
    }
}

[Serializable]
public class ShipShadow
{
    public string ShipHullProductionKey { get; set; }
    public string ShipClassName { get; set; }
    public bool IsModifiedClass { get; set; }
    public ShipDisplayName DisplayName { get; set; }
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
