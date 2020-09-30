using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class CombatSceneShipCreator
{
    public static Ship CreateAndFitOutShip(ShipShadow shadow, Faction owner)
    {
        return CreateAndFitOutShip(shadow, owner, null);
    }

    public static Ship CreateAndFitOutShip(ShipShadow shadow, Faction owner, Vector3 offset)
    {
        return CreateAndFitOutShip(shadow, owner, null, offset);
    }

    public static Ship CreateAndFitOutShip(ShipShadow shadow, Faction owner, UserInput inputController, Vector3 offset)
    {
        Ship res = CreateAndFitOutShip(shadow, owner, inputController);
        res.transform.Translate(offset);
        return res;
    }

    public static Ship CreateAndFitOutShip(ShipShadow shadow, Faction owner, UserInput inputController)
    {
        Ship s = ObjectFactory.CreateShip(shadow.ShipHullProductionKey);

        // Place the components:
        foreach (KeyValuePair<Ship.ShipSection, ShipComponentDefinition[]> sectionComps in shadow.ShipComponents)
        {
            for (int i = 0; i < sectionComps.Value.Length; ++i)
            {
                if (sectionComps.Value[i] == null)
                {
                    continue;
                }

                ShipComponentBase comp = sectionComps.Value[i].CreateComponent();
                if (!s.PlaceComponent(sectionComps.Key, comp))
                {
                    Debug.LogWarningFormat("Failed to place component on ship. Ship hull: {0}. Ship class: {1}. Section: {2}. Component: {3}",
                        shadow.ShipHullProductionKey, shadow.ShipClassName, sectionComps.Key, comp);
                }
            }
        }

        // Place the weapons:
        foreach (ShipShadow.TurretPlacement turretPlacement in shadow.Turrets)
        {
            TurretMod turretModToInstall = TurretMod.None;
            if (turretPlacement.Template.InstalledMods != null && turretPlacement.Template.InstalledMods.Length > 0)
            {
                turretModToInstall = turretPlacement.Template.InstalledMods[0];
            }

            TurretBase turret =
                ObjectFactory.CreateTurret(turretPlacement.Template.TurretType,
                                           turretPlacement.Template.WeaponNum,
                                           turretPlacement.Template.WeaponSize,
                                           turretPlacement.Template.WeaponType,
                                           turretModToInstall);

            turret.ComponentMaxHitPoints = turretPlacement.MaxHitPoints;
            turret.ComponentHitPoints = turretPlacement.CurrHitPoints;

            if (turret is GunTurret gt)
            {
                for (int i = 0; i < turretPlacement.Template.AmmoTypes.Length; ++i)
                {
                    gt.SetAmmoType(i, turretPlacement.Template.AmmoTypes[i]);
                }
            }
            else if (turret is TorpedoTurret tt)
            {
                tt.LoadedTorpedoType = turretPlacement.Template.AmmoTypes[0];
            }

            turret.AlternatingFire = turretPlacement.Template.AlternatingFire;

            bool placedTurret = false;
            foreach (TurretHardpoint hp in s.WeaponHardpoints)
            {
                if (hp.name == turretPlacement.Template.HardpointKey)
                {
                    placedTurret = s.PlaceTurret(hp, turret);
                    break;
                }
            }
            if (!placedTurret)
            {
                Debug.LogWarningFormat("Failed to place turret on ship. Ship hull: {0}. Ship class: {1}. Hardpoint key: {2}. Turret: {3}",
                                       shadow.ShipHullProductionKey, shadow.ShipClassName, turretPlacement.Template.HardpointKey, turret);
            }
        }

        // Configure the weapon control groups:
        s.SetTurretConfig(TurretControlGrouping.FromConfig(s, shadow.WeaponConfig));

        // Add the crew:
        for (int i = 0; i < shadow.Crew.Length; ++i)
        {
            s.AddCrew(shadow.Crew[i]);
        }

        s.Activate();

        s.Owner = owner;
        s.SetCircleToFactionColor();
        ShipAIController AIController = s.gameObject.AddComponent<ShipAIController>();
        if (owner.PlayerFaction && inputController != null)
        {
            inputController.ControlledShip = s;
            AIController.ControlType = ShipAIController.ShipControlType.Manual;
        }
        else if (owner.PlayerFaction)
        {
            AIController.ControlType = ShipAIController.ShipControlType.SemiAutonomous;
        }
        else
        {
            AIController.ControlType = ShipAIController.ShipControlType.Autonomous;
        }

        s.DisplayName = shadow.DisplayName;

        return s;
    }
}

