using System;
using System.Collections.Generic;
using UnityEngine;

public class WeaponControlGroupCfgPanel : MonoBehaviour
{
    public void SetShipTemplate(ShipHullDefinition s)
    {
        int items = 2;
        foreach (WeaponHardpointDefinition hp in s.WeaponHardpoints)
        {
            string displayString = string.Empty;
            int defaultGroup = 1;
            if (hp.TurretHardpoint != null)
            {
                displayString = hp.TurretHardpoint.DisplayString;
                defaultGroup = hp.TurretHardpoint.DefaultGroup;
            }
            else if (hp.TorpedoHardpoint != null)
            {
                displayString = hp.TorpedoHardpoint.HardpointBaseDefinition.DisplayString;
                defaultGroup = hp.TorpedoHardpoint.HardpointBaseDefinition.DefaultGroup;
            }

            WeaponCtrlCfgLine ln = ObjectFactory.CreateWeaponCtrlCfgLine(displayString);
            ln.HardpointKey = hp.HardpointNode.Name;
            for (int i = 0; i < ln.WeaponGroupCheckboxes.Length; i++)
            {
                ln.WeaponGroupCheckboxes[i].isOn = (i == (defaultGroup - 1));
            }
            ln.transform.SetParent(WeaponConfigsBox);
            ln.transform.SetAsLastSibling();
            ++items;
        }
        Footer.SetAsLastSibling();
        RectTransform rt = WeaponConfigsBox.GetComponent<RectTransform>();
        if (rt != null)
        {
            float footerHeight = Footer.sizeDelta.y;
            rt.sizeDelta = new Vector2(rt.sizeDelta.x, footerHeight * items);
        }
    }

    public void Clear()
    {
        for (int i = 1; i < WeaponConfigsBox.childCount - 1; ++i)
        {
            Destroy(WeaponConfigsBox.GetChild(i).gameObject);
        }
        AttachedShip = null;
    }

    public WeaponsConfigCompiled Compile()
    {
        WeaponsConfigCompiled res = new WeaponsConfigCompiled() { WeaponGroups = new Dictionary<int, Tuple<IEnumerable<string>, bool>>() };

        WeaponCtrlCfgLine footerCfg = Footer.GetComponent<WeaponCtrlCfgLine>();
        for (int i = 0; i < 6; ++i)
        {
            List<string> hardpointsInGroup = new List<string>();
            for (int j = 1; j < WeaponConfigsBox.childCount - 1; ++j)
            {
                Transform t = WeaponConfigsBox.GetChild(j);
                WeaponCtrlCfgLine cfgLn = t.GetComponent<WeaponCtrlCfgLine>();
                if (cfgLn != null)
                {
                    if (cfgLn.WeaponGroupCheckboxes[i].isOn)
                    {
                        hardpointsInGroup.Add(cfgLn.HardpointKey);
                    }
                }
            }
            res.WeaponGroups[i + 1] = new Tuple<IEnumerable<string>, bool>(hardpointsInGroup, footerCfg.WeaponGroupCheckboxes[i]);
        }

        return res;
    }

    public RectTransform WeaponConfigsBox;
    public RectTransform Footer;
    public Ship AttachedShip { get; set; }

    public struct WeaponsConfigCompiled
    {
        public Dictionary<int, Tuple<IEnumerable<string>, bool>> WeaponGroups;
    }
}
