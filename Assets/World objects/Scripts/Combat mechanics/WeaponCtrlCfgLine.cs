using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class WeaponCtrlCfgLine : MonoBehaviour
{
    void Awake()
    {
        WeaponTextBox = GetComponentInChildren<TextMeshProUGUI>();
    }

    public TextMeshProUGUI WeaponTextBox { get; private set; }
    public Toggle[] WeaponGroupCheckboxes;
    public string HardpointKey { get; set; }
}
