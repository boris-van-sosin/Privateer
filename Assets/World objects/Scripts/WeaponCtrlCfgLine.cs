using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class WeaponCtrlCfgLine : MonoBehaviour
{
    void Awake()
    {
        WeaponTextBox = GetComponentInChildren<Text>();
    }

    public Text WeaponTextBox { get; private set; }
    public Toggle[] WeaponGroupCheckboxes;
    public string HardpointKey { get; set; }
}
