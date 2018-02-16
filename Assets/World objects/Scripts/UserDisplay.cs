using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UserDisplay : MonoBehaviour
{
	// Use this for initialization
	void Start ()
    {
		
	}
	
	// Update is called once per frame
	void Update ()
    {
		if (EnergyBar != null)
        {
            EnergyBar.MaxValue = InputHandler.ControlledShip.MaxEnergy;
            EnergyBar.Value = InputHandler.ControlledShip.Energy;
        }
        if (HeatBar != null)
        {
            HeatBar.MaxValue = InputHandler.ControlledShip.MaxHeat;
            HeatBar.Value = InputHandler.ControlledShip.Heat;
        }

    }

    public UserInput InputHandler;
    public GradientBar HealthBar;
    public GradientBar ShieldBar;
    public GradientBar EnergyBar;
    public GradientBar HeatBar;
}
