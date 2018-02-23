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
        if (userShip)
        {
            UserInput input = FindObjectOfType<UserInput>();
            input.ControlledShip = s;
        }
    }

    public UnityEngine.UI.Dropdown ShipDropdown;
    public UnityEngine.UI.Dropdown SideDropdown;
    public UnityEngine.UI.Toggle UserToggle;
}
