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
        ShipDropdown.AddOptions(ObjectFactory.GetAllShipTypes().ToList());
        //ShipDropdown.AddOptions(ObjectFactory.GetAllStrikeCraftTypes().ToList());
        ShipDropdown.onValueChanged.AddListener(ShipSelectChanged);
        _weaponsCfgPanel = FindObjectOfType<WeaponControlGroupCfgPanel>();
        _cfgPanelVisible = false;
        _weaponsCfgPanel.gameObject.SetActive(false);
    }
	
	// Update is called once per frame
	void Update ()
    {
		
	}

    public void CreateShip()
    {
        string shipKey = ShipDropdown.options[ShipDropdown.value].text;
        bool friendly = SideDropdown.value == 0;
        bool userShip = UserToggle.isOn;
        if (ObjectFactory.GetAllShipTypes().Contains(shipKey))
        {
            for (int i = 0; i < _numToSpawn; ++i)
            {
                Vector3 offset = new Vector3(friendly ? -(i % 4) : (i % 4), -(i / 4), 0) * 6;
                CreateShipInner(shipKey, friendly, userShip && (i == 0), offset);
            }
        }
        else if (ObjectFactory.GetAllStrikeCraftTypes().Contains(shipKey))
        {
            CreateStrikeCraftWingInner(shipKey, friendly);
        }
    }

    private void CreateShipInner(string shipKey, bool friendly, bool userShip, Vector3 offset)
    {
        Ship s = ObjectFactory.CreateShip(shipKey);

        s.PlaceComponent(Ship.ShipSection.Left, DamageControlNode.DefaultComponent(s.ShipSize, s));
        s.PlaceComponent(Ship.ShipSection.Right, DamageControlNode.DefaultComponent(s.ShipSize, s));
        s.PlaceComponent(Ship.ShipSection.Center, PowerPlant.DefaultComponent(s.ShipSize, s));
        s.PlaceComponent(Ship.ShipSection.Center, CapacitorBank.DefaultComponent(s));
        s.PlaceComponent(Ship.ShipSection.Center, HeatExchange.DefaultComponent(s));
        s.PlaceComponent(Ship.ShipSection.Center, ShieldGenerator.DefaultComponent(s.ShipSize, s));
        s.PlaceComponent(Ship.ShipSection.Center, PowerPlant.DefaultComponent(s.ShipSize, s));
        s.PlaceComponent(Ship.ShipSection.Aft, ShipEngine.DefaultComponent(s.ShipSize, s));
        if (s.ShipSize == ObjectFactory.ShipSize.Cruiser || s.ShipSize == ObjectFactory.ShipSize.Destroyer)
        {
            s.PlaceComponent(Ship.ShipSection.Fore, HeatExchange.DefaultComponent(s));
            s.PlaceComponent(Ship.ShipSection.Aft, CapacitorBank.DefaultComponent(s));
        }
        else if (s.ShipSize == ObjectFactory.ShipSize.CapitalShip)
        {
            s.PlaceComponent(Ship.ShipSection.Left, HeatExchange.DefaultComponent(s));
            s.PlaceComponent(Ship.ShipSection.Right, HeatExchange.DefaultComponent(s));
            s.PlaceComponent(Ship.ShipSection.Center, HeatExchange.DefaultComponent(s));
            s.PlaceComponent(Ship.ShipSection.Fore, ShieldGenerator.DefaultComponent(s.ShipSize, s));
            s.PlaceComponent(Ship.ShipSection.Center, PowerPlant.DefaultComponent(s.ShipSize, s));
            s.PlaceComponent(Ship.ShipSection.Center, PowerPlant.DefaultComponent(s.ShipSize, s));
            s.PlaceComponent(Ship.ShipSection.Fore, CapacitorBank.DefaultComponent(s));
            s.PlaceComponent(Ship.ShipSection.Aft, CapacitorBank.DefaultComponent(s));
            s.PlaceComponent(Ship.ShipSection.Aft, FireControlGeneral.DefaultComponent(s.ShipSize, s));
        }
        foreach (TurretHardpoint hp in s.WeaponHardpoints)
        {
            TurretBase t;
            if (hp.AllowedWeaponTypes.Contains(ComponentSlotType.SmallBarbette) ||
                hp.AllowedWeaponTypes.Contains(ComponentSlotType.SmallBarbetteDual))
            {
                t = ObjectFactory.CreateTurret(BestWeapon(hp.AllowedWeaponTypes), ObjectFactory.WeaponType.Autocannon);
            }
            else if (hp.AllowedWeaponTypes.Contains(ComponentSlotType.TorpedoTube))
            {
                t = ObjectFactory.CreateTurret(ComponentSlotType.TorpedoTube, ObjectFactory.WeaponType.TorpedoTube);
                (t as TorpedoTurret).LoadedTorpedoType = ObjectFactory.TorpedoType.Tracking;
            }
            else
            {
                t = ObjectFactory.CreateTurret(BestWeapon(hp.AllowedWeaponTypes), ObjectFactory.WeaponType.Howitzer);
            }
            t.TargetPriorityList = ObjectFactory.GetDefaultTargetPriorityList(t.TurretWeaponType, t.TurretSize);
            GunTurret gt = t as GunTurret;
            if (gt != null)
            {
                if (gt.TurretSize == ObjectFactory.WeaponSize.Light && gt.TurretWeaponType == ObjectFactory.WeaponType.Autocannon)
                {
                    gt.AmmoType = ObjectFactory.AmmoType.ShrapnelRound;
                }
                else if (gt.TurretWeaponType == ObjectFactory.WeaponType.HVGun)
                {
                    gt.AmmoType = ObjectFactory.AmmoType.KineticPenetrator;
                }
                else
                {
                    gt.AmmoType = ObjectFactory.AmmoType.ShapedCharge;
                }
                t.InstalledTurretMod = TurretMod.Harpax;
            }
            s.PlaceTurret(hp, t);
        }
        int numCrew = (s.OperationalCrew + s.MaxCrew) / 2;//userShip ? (s.OperationalCrew + s.MaxCrew) / 2 : (s.OperationalCrew + s.SkeletonCrew) / 2;
        for (int i = 0; i < numCrew; ++i)
        {
            ShipCharacter currCrew = ShipCharacter.GenerateTerranShipCrew();
            currCrew.Level = ShipCharacter.CharacterLevel.Trained;
            s.AddCrew(currCrew);
        }
        if (friendly && userShip)
        {
            CombatDetachment d = CombatDetachment.DefaultComponent(s.ShipSize, s);
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

        Faction[] factions = FindObjectsOfType<Faction>();
        Faction faction1 = factions.Where(f => f.PlayerFaction).First(), faction2 = factions.Where(f => !f.PlayerFaction).First();
        if (friendly)
        {
            s.Owner = faction1;
            s.SetCircleToFactionColor();
        }
        else
        {
            s.Owner = faction2;
            s.transform.Translate(30, 0, 0);
        }
        s.SetCircleToFactionColor();
        if (friendly && userShip)
        {
            UserInput input = FindObjectOfType<UserInput>();
            input.ControlledShip = s;
        }
        else
        {
            s.gameObject.AddComponent<ShipAIController>();
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
        ComponentSlotType[] slotTypes = new ComponentSlotType[] { ComponentSlotType.SmallBarbetteDual, ComponentSlotType.MediumBarbette, ComponentSlotType.LargeBarbette };
        ObjectFactory.WeaponType[] weaponTypes = new ObjectFactory.WeaponType[] { ObjectFactory.WeaponType.Autocannon, ObjectFactory.WeaponType.Howitzer, ObjectFactory.WeaponType.HVGun };
        ObjectFactory.AmmoType[] ammoTypes = new ObjectFactory.AmmoType[] { ObjectFactory.AmmoType.KineticPenetrator, ObjectFactory.AmmoType.ShapedCharge, ObjectFactory.AmmoType.ShrapnelRound };
        foreach (ComponentSlotType cst in slotTypes)
        {
            foreach (ObjectFactory.WeaponType wt in weaponTypes)
            {
                GunTurret tb = ObjectFactory.CreateTurret(ComponentSlotType.SmallBarbetteDual, ObjectFactory.WeaponType.Autocannon) as GunTurret;
                foreach (ObjectFactory.AmmoType at in ammoTypes)
                {
                    tb.AmmoType = at;
                    ValueTuple<float, float, float> dps = tb.DebugGetDPS();
                    Debug.Log(string.Format("Weapon: {0} {1}, {2}: SH={3} SY={4} HL={5}", cst, wt, at, dps.Item1, dps.Item2, dps.Item3));
                }
                Destroy(tb);
            }
        }
    }

    private void ShipSelectChanged(int i)
    {
        if (_cfgPanelVisible)
        {
            string shipKey = ShipDropdown.options[i].text;
            Ship s = ObjectFactory.GetShipTemplate(shipKey);
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

    private ComponentSlotType BestWeapon(IEnumerable<ComponentSlotType> slotTypes)
    {
        Dictionary<ComponentSlotType, int> strength = new Dictionary<ComponentSlotType, int>()
        {
            { ComponentSlotType.SmallBarbette, 1 },
            { ComponentSlotType.SmallTurret, 1 },
            { ComponentSlotType.SmallFixed, 1 },
            { ComponentSlotType.SmallBroadside, 1 },
            { ComponentSlotType.SmallBarbetteDual, 2 },
            { ComponentSlotType.MediumBarbetteDualSmall, 3 },
            { ComponentSlotType.MediumTurretDualSmall, 3 },
            { ComponentSlotType.MediumBroadside, 4 },
            { ComponentSlotType.MediumBarbette, 5 },
            { ComponentSlotType.MediumTurret, 5 },
            { ComponentSlotType.LargeBarbette, 6 },
            { ComponentSlotType.LargeTurret, 6 },
        };

        IEnumerator<ComponentSlotType> iter = slotTypes.GetEnumerator();
        iter.MoveNext();
        ComponentSlotType res = iter.Current;
        while (iter.MoveNext())
        {
            if (!strength.ContainsKey(res))
            {
                res = iter.Current;
            }
            else if (strength.ContainsKey(res) && strength.ContainsKey(iter.Current) &&
                strength[iter.Current] > strength[res])
            {
                res = iter.Current;
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
