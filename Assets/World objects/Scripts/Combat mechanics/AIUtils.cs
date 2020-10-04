

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

    public static ShipControlType GetControlType(this ShipAIHandle shipAI)
    {
        return shipAI.AIHandle.GetControlType(shipAI.ControlledShip as Ship);
    }

    public static void SetControlType(this ShipAIHandle shipAI, ShipControlType controlType)
    {
        shipAI.AIHandle.SetControlType(shipAI.ControlledShip as Ship, controlType);
    }

    public static ShipBase GetTargetShip(this ShipAIHandle shipAI)
    {
        return shipAI.AIHandle.GetCurrentTarget(shipAI.ControlledShip);
    }
}

public static class FormationAIExtensions
{
    public static void AssignStrikeCraft(this FormationAIHandle formationAI, StrikeCraft s)
    {
        formationAI.AIHandle.AssignStrikeCraftToFormation(s, formationAI.ControlledFormation);
    }
    public static void OrderEscort(this FormationAIHandle formationAI, ShipBase toEscort)
    {
        formationAI.AIHandle.OrderEscort(formationAI.ControlledFormation, toEscort);
    }

    public static void OrderReturnToHost(this FormationAIHandle formationAI)
    {
        formationAI.AIHandle.OrderReturnToHost(formationAI.ControlledFormation);
    }
}
