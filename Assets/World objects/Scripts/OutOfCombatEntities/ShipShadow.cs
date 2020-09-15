
using System;
using System.Collections.Generic;

[Serializable]
public class ShipShadow
{
    public string ShipHullProductionKey;
    public Dictionary<Ship.ShipSection, List<ShipComponentTemplateDefinition>> ShipComponents;
    public TurretPlacement[] Turrets;
    public WeaponControlGroupCfgPanel.WeaponsConfigCompiled WeaponConfig;
    public ShipCharacter[] Crew;

    [Serializable]
    public struct TurretPlacement
    {
        public string HardpointKey;
        public string TurretType;
        public string WeaponType;
        public string WeaponSize;
        public string WeaponNum;
        public TurretMod InstalledMod;
        public string[] AmmoTypes;
        public bool AlternatingFire;
    }
}
