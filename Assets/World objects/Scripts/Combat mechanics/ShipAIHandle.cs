﻿using UnityEngine;

public class ShipAIHandle : MonoBehaviour
{
    public ShipBase ControlledShip { get; set; }
    public ShipsAIController AIHandle { get; set; }
    public NavigationGuide NavGuide { get; set; }

    void OnDestroy()
    {
        if (NavGuide != null)
        {
            Destroy(NavGuide);
        }
    }
}
