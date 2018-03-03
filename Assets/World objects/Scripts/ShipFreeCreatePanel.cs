using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class ShipFreeCreatePanel : MonoBehaviour
{

	// Use this for initialization
	void Start ()
    {
        ShipDropdown.AddOptions(ObjectFactory.GetAllShipTypes().ToList());
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
        Ship s = ObjectFactory.CreateShip(shipKey);

        s.PlaceComponent(Ship.ShipSection.Left, DamageControlNode.DefaultComponent(s));
        s.PlaceComponent(Ship.ShipSection.Left, PowerPlant.DefaultComponent(s));
        s.PlaceComponent(Ship.ShipSection.Right, PowerPlant.DefaultComponent(s));
        s.PlaceComponent(Ship.ShipSection.Center, CapacitorBank.DefaultComponent(s));
        s.PlaceComponent(Ship.ShipSection.Center, HeatExchange.DefaultComponent(s));
        s.PlaceComponent(Ship.ShipSection.Center, ShieldGenerator.DefaultComponent(s));
        s.PlaceComponent(Ship.ShipSection.Aft, ShipEngine.DefaultComponent(s));
        foreach (TurretHardpoint hp in s.WeaponHardpoints)
        {
            TurretBase t = ObjectFactory.CreateTurret(hp.AllowedWeaponTypes[0], ObjectFactory.WeaponType.Howitzer);
            GunTurret gt = t as GunTurret;
            if (gt != null)
            {
                gt.AmmoType = ObjectFactory.AmmoType.ShapedCharge;
            }
            s.PlaceTurret(hp, t);
        }
        s.Activate();

        Faction[] factions = FindObjectsOfType<Faction>();
        Faction faction1 = factions.Where(f => f.PlayerFaction).First(), faction2 = factions.Where(f => !f.PlayerFaction).First();
        if (friendly)
        {
            s.Owner = faction1;
        }
        else
        {
            s.Owner = faction2;
            s.transform.Translate(30, 0, 0);
        }
        if (friendly && userShip)
        {
            UserInput input = FindObjectOfType<UserInput>();
            input.ControlledShip = s;
        }
        else
        {
            s.gameObject.AddComponent<ShipAIController>();
        }
    }

    public UnityEngine.UI.Dropdown ShipDropdown;
    public UnityEngine.UI.Dropdown SideDropdown;
    public UnityEngine.UI.Toggle UserToggle;
}
