using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UserDisplay : MonoBehaviour
{
	
	// Update is called once per frame
	void Update ()
    {
        if (InputHandler.ControlledShip == null)
        {
            return;
        }
        if (StatusTopLevelDisplay.AttachedShip == null)
        {
            StatusTopLevelDisplay.AttachShip(InputHandler.ControlledShip);
        }
        if (HealthBar != null)
        {
            HealthBar.MaxValue = InputHandler.ControlledShip.MaxHullHitPoints;
            HealthBar.Value = InputHandler.ControlledShip.HullHitPoints;
        }
        if (ShieldBar != null)
        {
            ShieldBar.MaxValue = InputHandler.ControlledShip.ShipTotalMaxShields;
            ShieldBar.Value = InputHandler.ControlledShip.ShipTotalShields;
        }
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
    public StatusTopLevel StatusTopLevelDisplay;
}
