

using UnityEngine;

public static class ShipAIExtensions
{
    public static void NavigateTo(this ShipAIHandle shipAI, Vector3 target)
    {
        shipAI.AIHandle.NavigateTo(shipAI.ControlledShip as Ship, target);
    }

    public static void UserNavigateTo(this ShipAIHandle shipAI, Vector3 target)
    {
        shipAI.AIHandle.UserNavigateTo(shipAI.ControlledShip as Ship, target);
    }

    public static void Follow(this ShipAIHandle shipAI, ShipBase followTarget)
    {
        shipAI.AIHandle.Follow(shipAI.ControlledShip as Ship, followTarget);
    }

    public static ShipAIController.ShipControlType GetControlType(this ShipAIHandle shipAI)
    {
        return shipAI.AIHandle.GetControlType(shipAI.ControlledShip as Ship);
    }

    public static void SetControlType(this ShipAIHandle shipAI, ShipAIController.ShipControlType controlType)
    {
        shipAI.AIHandle.SetControlType(shipAI.ControlledShip as Ship, controlType);
    }
}
