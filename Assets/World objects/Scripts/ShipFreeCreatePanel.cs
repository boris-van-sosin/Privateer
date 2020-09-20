using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System;
using TMPro;

public class ShipFreeCreatePanel : MonoBehaviour
{
	// Use this for initialization
	void Start ()
    {
        ShipDropdown.AddOptions(ObjectFactory.GetAllShipClassTemplates().ToList());
        //ShipDropdown.AddOptions(ObjectFactory.GetAllStrikeCraftTypes().ToList());
        ShipDropdown.onValueChanged.AddListener(ShipSelectChanged);
        _weaponsCfgPanel = FindObjectOfType<WeaponControlGroupCfgPanel>();
        _cfgPanelVisible = false;
        _weaponsCfgPanel.gameObject.SetActive(false);
    }

    public void CreateShip()
    {
        string shipKey = ShipDropdown.options[ShipDropdown.value].text;
        bool friendly = SideDropdown.value == 0;
        bool userShip = UserToggle.isOn;
        if (ObjectFactory.GetAllShipClassTemplates().Contains(shipKey))
        {
            for (int i = 0; i < _numToSpawn; ++i)
            {
                Vector3 offset = new Vector3(friendly ? -(i % 4) : (i % 4), 0f, -(i / 4)) * 6;
                if (!friendly)
                {
                    offset += new Vector3(30, 0, 0);
                }
                CreateShipInner(shipKey, friendly, userShip && (i == 0), offset);
            }
        }
    }

    private void CreateShipInner(string shipKey, bool friendly, bool userShip, Vector3 offset)
    {
        ShipTemplate template = ObjectFactory.GetShipClassTemplate(shipKey);
        ShipShadow shadow = template.ToNewShip();

        Faction[] factions = FindObjectsOfType<Faction>();
        Faction faction1 = factions.Where(f => f.PlayerFaction).First(), faction2 = factions.Where(f => !f.PlayerFaction).First();
        Faction owner = friendly ? faction1 : faction2;

        // Fill the parts that aren't implemeted yet:
        ShipHullDefinition hullDef = ObjectFactory.GetShipHullDefinition(shadow.ShipHullProductionKey);
        ObjectFactory.ShipSize sz = (ObjectFactory.ShipSize) Enum.Parse(typeof(ObjectFactory.ShipSize), hullDef.ShipSize);
        shadow.ShipDisplayName  =
            NamingSystem.GenShipName(ObjectFactory.GetCultureNames("Terran"),
                                     UnityEngine.Random.Range(0, 2) == 0 ? "British" : "German",
                                     ObjectFactory.InternalShipTypeToNameType(sz), owner.UsedNames);
        owner.UsedNames.Add(shadow.ShipDisplayName.FullNameKey);
        int numCrew = (hullDef.OperationalCrew + hullDef.MaxCrew) / 2;//userShip ? (s.OperationalCrew + s.MaxCrew) / 2 : (s.OperationalCrew + s.SkeletonCrew) / 2;
        shadow.Crew = new ShipCharacter[numCrew];
        for (int i = 0; i < numCrew; ++i)
        {
            ShipCharacter currCrew = ShipCharacter.GenerateTerranShipCrew();
            currCrew.Level = ShipCharacter.CharacterLevel.Trained;
            shadow.Crew[i] = currCrew;
        }

        Ship s = CreateAndFitOutShip(shadow, owner, friendly && userShip);

        s.transform.Translate(offset);

        return;

        // old creation process:

        s = ObjectFactory.CreateShip(shipKey);

        s.PlaceComponent(Ship.ShipSection.Left, DamageControlNode.DefaultComponent(s.ShipSize, s));
        s.PlaceComponent(Ship.ShipSection.Right, DamageControlNode.DefaultComponent(s.ShipSize, s));
        s.PlaceComponent(Ship.ShipSection.Center, PowerPlant.DefaultComponent(s.ShipSize, s));
        s.PlaceComponent(Ship.ShipSection.Center, CapacitorBank.DefaultComponent(s));
        s.PlaceComponent(Ship.ShipSection.Center, HeatSink.DefaultComponent(s));
        s.PlaceComponent(Ship.ShipSection.Fore, HeatExchange.DefaultComponent(s));
        s.PlaceComponent(Ship.ShipSection.Center, ShieldGenerator.DefaultComponent(s.ShipSize, s));
        s.PlaceComponent(Ship.ShipSection.Center, PowerPlant.DefaultComponent(s.ShipSize, s));
        s.PlaceComponent(Ship.ShipSection.Aft, ShipEngine.DefaultComponent(s.ShipSize, s));
        if (s.ShipSize == ObjectFactory.ShipSize.Destroyer)
        {
            s.PlaceComponent(Ship.ShipSection.Left, HeatExchange.DefaultComponent(s));
            s.PlaceComponent(Ship.ShipSection.Aft, CapacitorBank.DefaultComponent(s));
            s.PlaceComponent(Ship.ShipSection.Right, HeatSink.DefaultComponent(s));
        }
        else if (s.ShipSize == ObjectFactory.ShipSize.Cruiser)
        {
            s.PlaceComponent(Ship.ShipSection.Left, HeatExchange.DefaultComponent(s));
            s.PlaceComponent(Ship.ShipSection.Aft, CapacitorBank.DefaultComponent(s));
            s.PlaceComponent(Ship.ShipSection.Right, HeatSink.DefaultComponent(s));
        }
        else if (s.ShipSize == ObjectFactory.ShipSize.CapitalShip)
        {
            s.PlaceComponent(Ship.ShipSection.Left, HeatExchange.DefaultComponent(s));
            s.PlaceComponent(Ship.ShipSection.Right, HeatExchange.DefaultComponent(s));
            s.PlaceComponent(Ship.ShipSection.Center, HeatExchange.DefaultComponent(s));
            s.PlaceComponent(Ship.ShipSection.Fore, ShieldGenerator.DefaultComponent(s.ShipSize, s));
            s.PlaceComponent(Ship.ShipSection.Center, PowerPlant.DefaultComponent(s.ShipSize, s));
            //s.PlaceComponent(Ship.ShipSection.Center, PowerPlant.DefaultComponent(s.ShipSize, s));
            s.PlaceComponent(Ship.ShipSection.Left, CapacitorBank.DefaultComponent(s));
            s.PlaceComponent(Ship.ShipSection.Right, CapacitorBank.DefaultComponent(s));
            s.PlaceComponent(Ship.ShipSection.Aft, FireControlGeneral.DefaultComponent(s.ShipSize, s));
            s.PlaceComponent(Ship.ShipSection.Fore, HeatSink.DefaultComponent(s));
            s.PlaceComponent(Ship.ShipSection.Aft, HeatSink.DefaultComponent(s));
        }
        foreach (TurretHardpoint hp in s.WeaponHardpoints)
        {
            TurretBase t;
            (string, string, string) currBest;
            if (hp.AllowedWeaponTypes.Contains("LightBarbette1Light") ||
                hp.AllowedWeaponTypes.Contains("LightBarbette2Light"))
            {
                currBest = BestWeapon(hp.AllowedWeaponTypes);
                t = ObjectFactory.CreateTurret(currBest.Item1, currBest.Item2, currBest.Item3, "HVGun"); // was Autocannon
            }
            else if (hp.AllowedWeaponTypes.Contains("TorpedoTube"))
            {
                currBest = ("TorpedoTube", "", "");
                t = ObjectFactory.CreateTurret(currBest.Item1, currBest.Item2, currBest.Item3, "TorpedoWeapon");
                (t as TorpedoTurret).LoadedTorpedoType = "Tracking";
            }
            else
            {
                currBest = BestWeapon(hp.AllowedWeaponTypes);
                t = ObjectFactory.CreateTurret(currBest.Item1, currBest.Item2, currBest.Item3, "Howitzer");
            }
            if (t == null)
            {
                Debug.LogErrorFormat("Failed to create turret. Ship: {0}. Hardpoint: {1}. Turret key: {2}", s.name, hp.name, currBest);
            }
            t.TargetPriorityList = ObjectFactory.GetDefaultTargetPriorityList(t.TurretWeaponType, t.TurretWeaponSize);
            GunTurret gt = t as GunTurret;
            if (gt != null)
            {
                if (gt.TurretWeaponSize == "Light" && gt.TurretWeaponType == "Autocannon")
                {
                    gt.SetAmmoType(0, "ShrapnelRound");
                }
                else if (gt.TurretWeaponType == "HVGun")
                {
                    gt.SetAmmoType(0, "KineticPenetrator");
                }
                else
                {
                    gt.SetAmmoType(0, "ShapedCharge");
                }
                if (s.ShipSize != ObjectFactory.ShipSize.Sloop && gt.TurretWeaponSize == "Light")
                {
                    gt.InstalledTurretMod = TurretMod.DualAmmoFeed;
                    gt.SetAmmoType(0, "ShrapnelRound");
                    gt.SetAmmoType(1, "KineticPenetrator");
                    gt.AlternatingFire = true;
                }
                else
                {
                    t.InstalledTurretMod = TurretMod.Harpax;
                }
            }
            s.PlaceTurret(hp, t);
        }
        numCrew = (s.OperationalCrew + s.MaxCrew) / 2;//userShip ? (s.OperationalCrew + s.MaxCrew) / 2 : (s.OperationalCrew + s.SkeletonCrew) / 2;
        for (int i = 0; i < numCrew; ++i)
        {
            ShipCharacter currCrew = ShipCharacter.GenerateTerranShipCrew();
            currCrew.Level = ShipCharacter.CharacterLevel.Trained;
            s.AddCrew(currCrew);
        }
        if (friendly && userShip)
        {
            ShipArmoury d = ShipArmoury.DefaultComponent(s.ShipSize, s);
            s.PlaceComponent(Ship.ShipSection.Center, d);

            if (_cfgPanelVisible)
            {
                s.SetTurretConfig(TurretControlGrouping.FromConfig(s, _weaponsCfgPanel.Compile()));
            }
        }
        else
        {
            s.SetTurretConfigAllAuto();
        }

        if (friendly)
        {
            for (int i = 0; i < 100; ++i)
            {
                s.AddCrew(ShipCharacter.GenerateTerranCombatCrew(30));
            }
        }

        s.Activate();

        if (friendly)
        {
            s.Owner = faction1;
        }
        else
        {
            s.Owner = faction2;
            s.transform.Translate(30, 0, 0);
        }
        s.SetCircleToFactionColor();
        ShipAIController AIController = s.gameObject.AddComponent<ShipAIController>();
        if (friendly && userShip)
        {
            UserInput input = FindObjectOfType<UserInput>();
            input.ControlledShip = s;
            AIController.ControlType = ShipAIController.ShipControlType.Manual;
        }
        else if (friendly)
        {
            AIController.ControlType = ShipAIController.ShipControlType.SemiAutonomous;
        }
        else
        {
            AIController.ControlType = ShipAIController.ShipControlType.Autonomous;
        }

        s.DisplayName =
            NamingSystem.GenShipName(ObjectFactory.GetCultureNames("Terran"),
            UnityEngine.Random.Range(0,2) == 0 ? "British" : "German",
            ObjectFactory.InternalShipTypeToNameType(s.ShipSize), s.Owner.UsedNames);
        s.Owner.UsedNames.Add(s.DisplayName.FullNameKey);

        s.transform.Translate(offset);
        //s.transform.Rotate(Vector3.up, UnityEngine.Random.Range(0f, 360f), Space.World);

        _cfgPanelVisible = false;
        _weaponsCfgPanel.gameObject.SetActive(_cfgPanelVisible);
    }

    private static Ship CreateAndFitOutShip(ShipShadow shadow, Faction owner, bool userControlled)
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
            TurretBase turret =
                ObjectFactory.CreateTurret(turretPlacement.Template.TurretType,
                                           turretPlacement.Template.WeaponNum,
                                           turretPlacement.Template.WeaponSize,
                                           turretPlacement.Template.WeaponType);

            turret.ComponentMaxHitPoints = turretPlacement.MaxHitPoints;
            turret.ComponentHitPoints = turretPlacement.CurrHitPoints;

            if (turretPlacement.Template.InstalledMods != null && turretPlacement.Template.InstalledMods.Length > 0)
            {
                turret.InstalledTurretMod = turretPlacement.Template.InstalledMods[0];
            }
            else
            {
                turret.InstalledTurretMod = TurretMod.None;
            }

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

        Faction[] factions = FindObjectsOfType<Faction>();
        Faction faction1 = factions.Where(f => f.PlayerFaction).First(), faction2 = factions.Where(f => !f.PlayerFaction).First();
        s.Owner = owner;
        s.SetCircleToFactionColor();
        ShipAIController AIController = s.gameObject.AddComponent<ShipAIController>();
        if (owner == faction1 && userControlled)
        {
            UserInput input = FindObjectOfType<UserInput>();
            input.ControlledShip = s;
            AIController.ControlType = ShipAIController.ShipControlType.Manual;
        }
        else if (owner == faction1)
        {
            AIController.ControlType = ShipAIController.ShipControlType.SemiAutonomous;
        }
        else
        {
            AIController.ControlType = ShipAIController.ShipControlType.Autonomous;
        }

        s.DisplayName = shadow.ShipDisplayName;

        return s;
    }

    private void CreateStrikeCraftWingInner(string shipKey, bool friendly)
    {
        StrikeCraftFormation formation = ObjectFactory.CreateStrikeCraftFormation("Fighter Wing");
        Faction[] factions = FindObjectsOfType<Faction>();
        Faction faction1 = factions.Where(f => f.PlayerFaction).First(), faction2 = factions.Where(f => !f.PlayerFaction).First();
        if (friendly)
        {
            formation.Owner = faction1;
        }
        else
        {
            formation.Owner = faction2;
            formation.transform.Translate(30, 0, 0);
        }
        foreach (Transform tr in formation.Positions)
        {
            StrikeCraft s = ObjectFactory.CreateStrikeCraftAndFitOut(shipKey);
            s.Owner = formation.Owner;
            s.transform.position = tr.position;
            s.AddToFormation(formation);
            s.Activate();
            formation.MaxSpeed = s.MaxSpeed * 1.1f;
            formation.TurnRate = s.TurnRate * 0.5f;
        }
    }

    private void TestWeapons()
    {
        string[] slotTypes = ObjectFactory.GetAllTurretMounts().ToArray();
        (string, string, string)[] weaponTypes = ObjectFactory.GetAllProjectileWarheads().ToArray();
        foreach (string cst in slotTypes)
        {
            foreach ((string, string, string) wt in weaponTypes)
            {
                GunTurret tb = ObjectFactory.CreateTurret(cst, "1", wt.Item1, wt.Item2) as GunTurret;
                if (tb != null)
                {
                    tb.SetAmmoType(0, wt.Item3);
                    ValueTuple<float, float, float> dps = tb.DebugGetDPS();
                    Debug.LogFormat("Weapon: {0} {1}: SH={2} SY={3} HL={4}", cst, wt, dps.Item1, dps.Item2, dps.Item3);
                    Destroy(tb);
                }
            }
        }
    }

    private void ShipSelectChanged(int i)
    {
        if (_cfgPanelVisible)
        {
            string shipKey = ShipDropdown.options[i].text;
            ShipHullDefinition s = ObjectFactory.GetShipHullDefinition(shipKey);
            if (s != null)
            {
                _weaponsCfgPanel.Clear();
                _weaponsCfgPanel.SetShipTemplate(s);
            }
        }
    }

    public void ToggleConfigPanel()
    {
        _cfgPanelVisible = !_cfgPanelVisible;
        _weaponsCfgPanel.gameObject.SetActive(_cfgPanelVisible);
        if (_cfgPanelVisible)
        {
            ShipSelectChanged(ShipDropdown.value);
        }
    }

    private (string, string ,string) BestWeapon(IEnumerable<string> slotTypes)
    {
        Dictionary<(string, string, string), int> strength = new Dictionary<(string, string, string), int>()
        {
            { ("LightBarbette", "1", "Light"), 1 },
            { ("LightTurret", "", "Light"), 1 },
            { ("LightFixed", "", "Light"), 1 },
            { ("LightBroadside", "", "Light"), 1 },
            { ("LightBarbette", "2", "Light"), 2 },
            { ("MediumTurret", "2", "Light"), 3 },
            { ("MediumBroadside", "", "Medium"), 4 },
            { ("MediumBarbette", "2", "Medium"), 5 },
            { ("MediumTurret", "2", "Medium"), 5 },
            { ("MediumTurret", "1", "Heavy"), 6 },
            { ("HeavyBroadside", "", "Heavy"), 6 },
            { ("HeavyBarbette", "2", "Heavy"), 7 },
            { ("HeavyTurret", "2", "Heavy"), 7 },
        };

        IEnumerator<string> iter = slotTypes.GetEnumerator();
        (string, string, string) res = ("", "", "");
        int bestStrength = 0;
        while (iter.MoveNext())
        {
            string curr = iter.Current;
            foreach (KeyValuePair<(string, string, string), int> w in strength)
            {
                string currCandidate = w.Key.Item1 + w.Key.Item2 + w.Key.Item3;
                if (curr == currCandidate && w.Value > bestStrength)
                {
                    res = w.Key;
                    bestStrength = w.Value;
                    break;
                }
            }
        }
        return res;
    }

    public void SetNumToSpawn()
    {
        _numToSpawn = Mathf.RoundToInt(SliderNumToSpawn.value);
        TextNumToSpawn.text = _numToSpawn.ToString();
    }

    public TMP_Dropdown ShipDropdown;
    public TMP_Dropdown SideDropdown;
    public UnityEngine.UI.Toggle UserToggle;
    public UnityEngine.UI.Slider SliderNumToSpawn;
    public TextMeshProUGUI TextNumToSpawn;
    private WeaponControlGroupCfgPanel _weaponsCfgPanel;
    private int _numToSpawn = 1;
    private bool _cfgPanelVisible;
}
