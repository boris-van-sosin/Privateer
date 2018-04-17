using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TurretControlGrouping
{
    public static TurretControlGrouping FromConfig(Ship s, WeaponControlGroupCfgPanel.WeaponsConfigCompiled c)
    {
        TurretControlGrouping res = new TurretControlGrouping()
        {
            ControlGroups = new Dictionary<int, IEnumerable<ITurret>>(),
            ControlGroupStatus = new Dictionary<int, TurretBase.TurretMode>(),
            ControlGroupDefaultStatus = new Dictionary<int, TurretBase.TurretMode>()
        };

        foreach (int i in c.WeaponGroups.Keys)
        {
            if (c.WeaponGroups[i].Item2)
            {
                res.ControlGroupStatus[i] = res.ControlGroupDefaultStatus[i] = TurretBase.TurretMode.Auto;
            }
            else
            {
                res.ControlGroupStatus[i] = res.ControlGroupDefaultStatus[i] = TurretBase.TurretMode.Off;
            }
            List<ITurret> weaponsInGroup = new List<ITurret>();
            foreach (string k in c.WeaponGroups[i].Item1)
            {
                foreach (TurretHardpoint hp in s.WeaponHardpoints)
                {
                    if (hp.name == k)
                    {
                        TurretBase currTurret = hp.GetComponentInChildren<TurretBase>();
                        currTurret.SetTurretBehavior(res.ControlGroupDefaultStatus[i]);
                        weaponsInGroup.Add(currTurret);
                        break;
                    }
                }
            }
            res.ControlGroups[i] = weaponsInGroup;
        }
        res.CacheManualTurrets();
        return res;
    }

    public static TurretControlGrouping AllAuto(Ship s)
    {
        TurretControlGrouping res = new TurretControlGrouping()
        {
            ControlGroups = new Dictionary<int, IEnumerable<ITurret>>(),
            ControlGroupStatus = new Dictionary<int, TurretBase.TurretMode>(),
            ControlGroupDefaultStatus = new Dictionary<int, TurretBase.TurretMode>()
        };

        res.ControlGroupStatus[1] = res.ControlGroupDefaultStatus[1] = TurretBase.TurretMode.Auto;
        res.ControlGroups[1] = s.Turrets;
        res.CacheManualTurrets();
        return res;
    }

    private Dictionary<int, IEnumerable<ITurret>> ControlGroups { get; set; }
    private Dictionary<int, TurretBase.TurretMode> ControlGroupStatus { get; set; }
    private Dictionary<int, TurretBase.TurretMode> ControlGroupDefaultStatus { get; set; }

    public void SetGroupToMode(int group, TurretBase.TurretMode mode)
    {
        if (!(ControlGroups.ContainsKey(group) && ControlGroupStatus.ContainsKey(group)))
        {
            return;
        }

        foreach (int g in ControlGroups.Keys)
        {
            if (ControlGroupStatus[g] == TurretBase.TurretMode.Manual)
            {
                ControlGroupStatus[g] = ControlGroupDefaultStatus[g];
                break;
            }
        }
        ControlGroupStatus[group] = mode;

        foreach (int g in ControlGroups.Keys)
        {
            if (ControlGroupStatus[g] == TurretBase.TurretMode.Off)
            {
                foreach (ITurret t in ControlGroups[g])
                {
                    t.SetTurretBehavior(ControlGroupStatus[g]);
                }
            }
        }
        foreach (int g in ControlGroups.Keys)
        {
            if (ControlGroupStatus[g] == TurretBase.TurretMode.Auto)
            {
                foreach (ITurret t in ControlGroups[g])
                {
                    t.SetTurretBehavior(ControlGroupStatus[g]);
                }
            }
        }
        foreach (ITurret t in ControlGroups[group])
        {
            t.SetTurretBehavior(ControlGroupStatus[group]);
        }
        CacheManualTurrets();
    }

    public void ToggleGroupAuto(int group)
    {
        switch (ControlGroupStatus[group])
        {
            case TurretBase.TurretMode.Off:
            case TurretBase.TurretMode.Manual:
                SetGroupToMode(group, TurretBase.TurretMode.Auto);
                break;
            case TurretBase.TurretMode.Auto:
                SetGroupToMode(group, TurretBase.TurretMode.Off);
                break;
            default:
                break;
        }
    }

    public IEnumerable<ITurret> ManualTurrets
    {
        get
        {
            return _manualTurrets;
        }
    }

    private void CacheManualTurrets()
    {
        _manualTurrets.Clear();
        foreach (int i in ControlGroups.Keys)
        {
            if (ControlGroupStatus[i] == TurretBase.TurretMode.Manual)
            {
                _manualTurrets.AddRange(ControlGroups[i]);
            }
        }
    }

    private List<ITurret> _manualTurrets = new List<ITurret>();
}

