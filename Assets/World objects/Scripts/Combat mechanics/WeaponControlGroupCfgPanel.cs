using System;
using System.Collections.Generic;
using UnityEngine;

public class WeaponControlGroupCfgPanel : MonoBehaviour
{
    void Awake()
    {
        if (_stackinglayout == null)
        {
            _stackinglayout = WeaponConfigsBox.GetComponent<StackingLayout>();
        }
    }

    public void SetShipTemplate(ShipHullDefinition s)
    {
        if (_stackinglayout == null)
        {
            _stackinglayout = WeaponConfigsBox.GetComponent<StackingLayout>();
        }
        _stackinglayout.AutoRefresh = false;
        ClearInner();
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

            WeaponCtrlCfgLine ln = Instantiate(LineTemplate);
            ln.transform.SetParent(WeaponConfigsBox, false);
            ln.transform.SetAsLastSibling();
            RectTransform rt = ln.GetComponent<RectTransform>();
            float pivotOffset = rt.pivot.x * rt.rect.width;
            rt.anchoredPosition = new Vector2(pivotOffset, 0);

            ln.WeaponTextBox.text = displayString;
            ln.HardpointKey = hp.HardpointNode.Name;

            for (int i = 0; i < ln.WeaponGroupCheckboxes.Length; i++)
            {
                ln.WeaponGroupCheckboxes[i].isOn = (i == (defaultGroup - 1));
            }

            ++items;
        }
        Footer.SetAsLastSibling();
        _stackinglayout.ForceRefresh();
        _stackinglayout.AutoRefresh = true;
        
        RectTransform scrollBoxRt = WeaponConfigsBox.GetComponent<RectTransform>();
        if (scrollBoxRt != null)
        {
            float footerHeight = Footer.sizeDelta.y;
            scrollBoxRt.sizeDelta = new Vector2(scrollBoxRt.sizeDelta.x, footerHeight * items);
        }
    }

    public void Clear()
    {
        if (_stackinglayout == null)
        {
            _stackinglayout = WeaponConfigsBox.GetComponent<StackingLayout>();
        }
        _stackinglayout.AutoRefresh = true;
        ClearInner();
        _stackinglayout.ForceRefresh();
        _stackinglayout.AutoRefresh = true;
    }

    private void ClearInner()
    {
        while (WeaponConfigsBox.childCount > 2)
        {
            GameObject toDelete = WeaponConfigsBox.GetChild(1).gameObject;
            toDelete.transform.SetParent(null);
            Destroy(toDelete);
        }
    }

    public WeaponsConfigCompiled Compile()
    {
        WeaponsConfigCompiled res = new WeaponsConfigCompiled() { WeaponGroups = new WeaponsConfigCompiledLine[NumControlGroups] };

        WeaponCtrlCfgLine footerCfg = Footer.GetComponent<WeaponCtrlCfgLine>();
        for (int i = 0; i < NumControlGroups; ++i)
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
            res.WeaponGroups[i] = new WeaponsConfigCompiledLine() { GroupNum = i + 1, HardpointKeys = hardpointsInGroup.ToArray(), DefaultAuto = footerCfg.WeaponGroupCheckboxes[i] };
        }

        return res;
    }

    public static WeaponsConfigCompiled DefaultForShip(ShipHullDefinition def)
    {
        WeaponsConfigCompiled res = new WeaponsConfigCompiled() { WeaponGroups = new WeaponsConfigCompiledLine[NumControlGroups] };
        Dictionary<int, (List<string>, bool)> tmpConfig = new Dictionary<int, (List<string>, bool)>();
        for (int i = 0; i < 6; ++i)
        {
            tmpConfig[i + 1] = (new List<string>(), true);
        }

        foreach (WeaponHardpointDefinition hp in def.WeaponHardpoints)
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

            tmpConfig[defaultGroup].Item1.Add(hp.HardpointNode.Name);
        }

        foreach (KeyValuePair<int, (List<string>, bool)> cfgItem in tmpConfig)
        {
            res.WeaponGroups[cfgItem.Key - 1].GroupNum = cfgItem.Key;
            res.WeaponGroups[cfgItem.Key - 1].HardpointKeys = cfgItem.Value.Item1.ToArray();
            res.WeaponGroups[cfgItem.Key - 1].DefaultAuto = cfgItem.Value.Item2;
        }

        return res;
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }

    public RectTransform WeaponConfigsBox;
    public RectTransform Footer;
    public WeaponCtrlCfgLine LineTemplate;
    private StackingLayout _stackinglayout;

    [Serializable]
    public struct WeaponsConfigCompiled
    {
        public WeaponsConfigCompiledLine[] WeaponGroups { get; set; }
    }

    [Serializable]
    public struct WeaponsConfigCompiledLine
    {
        public int GroupNum { get; set; }
        public string[] HardpointKeys { get; set; }
        public bool DefaultAuto { get; set; }
    }

    public static readonly int NumControlGroups = 6;
}
