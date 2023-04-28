using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TurretControlGrouping
{
    public static TurretControlGrouping FromConfig(Ship s, WeaponControlGroupCfgPanel.WeaponsConfigCompiled c)
    {
        TurretControlGrouping res = new TurretControlGrouping()
        {
            ControlGroups = new Dictionary<int, IReadOnlyList<ITurret>>(),
            ControlGroupStatus = new Dictionary<int, TurretBase.TurretMode>(),
            ControlGroupDefaultStatus = new Dictionary<int, TurretBase.TurretMode>()
        };

        foreach (WeaponControlGroupCfgPanel.WeaponsConfigCompiledLine group in c.WeaponGroups)
        {
            if (group.DefaultAuto)
            {
                res.ControlGroupStatus[group.GroupNum] = res.ControlGroupDefaultStatus[group.GroupNum] = TurretBase.TurretMode.Auto;
            }
            else
            {
                res.ControlGroupStatus[group.GroupNum] = res.ControlGroupDefaultStatus[group.GroupNum] = TurretBase.TurretMode.Off;
            }
            List<ITurret> weaponsInGroup = new List<ITurret>();
            foreach (string k in group.HardpointKeys)
            {
                foreach (TurretHardpoint hp in s.WeaponHardpoints)
                {
                    if (hp.name == k)
                    {
                        TurretBase currTurret = hp.GetComponentInChildren<TurretBase>();
                        if (currTurret != null)
                        {
                            currTurret.SetTurretBehavior(res.ControlGroupDefaultStatus[group.GroupNum]);
                            weaponsInGroup.Add(currTurret);
                        }
                        else
                        {
                            Debug.LogWarningFormat("Tried to bind empty weapon hardpoint {0} to weapon control group {1}.: {0}.",
                                                   hp.DisplayString, group.GroupNum);
                        }
                        break;
                    }
                }
            }
            res.ControlGroups[group.GroupNum] = weaponsInGroup;
        }
        res.CacheManualTurrets();
        return res;
    }

    public static TurretControlGrouping AllAuto(ShipBase s)
    {
        TurretControlGrouping res = new TurretControlGrouping()
        {
            ControlGroups = new Dictionary<int, IReadOnlyList<ITurret>>(),
            ControlGroupStatus = new Dictionary<int, TurretBase.TurretMode>(),
            ControlGroupDefaultStatus = new Dictionary<int, TurretBase.TurretMode>()
        };

        res.ControlGroupStatus[1] = res.ControlGroupDefaultStatus[1] = TurretBase.TurretMode.Auto;
        res.ControlGroups[1] = new List<ITurret>(s.Turrets);
        res.CacheManualTurrets();
        return res;
    }

    private Dictionary<int, IReadOnlyList<ITurret>> ControlGroups { get; set; }
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

    public IReadOnlyList<ITurret> ManualTurrets
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

