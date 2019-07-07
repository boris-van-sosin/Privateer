using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class ShipContextMenu : MonoBehaviour
{
    public TextMeshProUGUI ShipShortNameText;
    public TextMeshProUGUI ShipFullNameText;
    public TextMeshProUGUI FluffText;
    public Ship DisplayedShip { get; set; }
    public void SetText()
    {
        if (DisplayedShip != null)
        {
            ShipShortNameText.text = DisplayedShip.DisplayName.ShortName;
            ShipFullNameText.text = DisplayedShip.DisplayName.FullName;
            FluffText.text = DisplayedShip.DisplayName.Fluff;
        }
    }
}
