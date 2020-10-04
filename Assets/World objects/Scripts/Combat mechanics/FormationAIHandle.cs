using UnityEngine;

public class FormationAIHandle : MonoBehaviour
{
    public StrikeCraftFormation ControlledFormation { get; set; }
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
