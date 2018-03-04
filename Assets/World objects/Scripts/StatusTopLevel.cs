using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class StatusTopLevel : MonoBehaviour
{
    void Awake()
    {
        _compsPanel = transform.Find("CompsPanel").GetComponent<RectTransform>();
        _healthBar = transform.Find("HitPointBar").GetComponent<GradientBar>();
        _shieldBar = transform.Find("ShieldBar").GetComponent<GradientBar>();
        _energyBar = transform.Find("EnergyBar").GetComponent<GradientBar>();
        _heatBar  = transform.Find("HeatBar").GetComponent<GradientBar>();
    }

    public void AttachShip(Ship s)
    {
        if (_attachedShip != null)
        {
            DetachShip();
        }
        _attachedShip = s;
        if (_attachedShip != null)
        {
            foreach (TurretHardpoint hp in _attachedShip.WeaponHardpoints)
            {
                string hardointName = hp.name;
                TurretBase currTurret = hp.GetComponentInChildren<TurretBase>();
                if (currTurret != null)
                {
                    Transform hardpointDisplay = transform.Find(hardointName);
                    if (hardpointDisplay != null)
                    {
                        StatusSubsystem compStatus = ObjectFactory.CreateStatusSubsytem(currTurret);
                        compStatus.transform.SetParent(hardpointDisplay);
                        compStatus.transform.localPosition = Vector2.zero;
                    }
                }
            }

            foreach (IShipActiveComponent comp in _attachedShip.AllComponents.Where(x => x is IShipActiveComponent && !(x is TurretBase) && x.ComponentType != ComponentSlotType.Hidden).Select(y => y as IShipActiveComponent))
            {
                StatusSubsystem compStatus = ObjectFactory.CreateStatusSubsytem(comp);
                RectTransform compRT = compStatus.GetComponent<RectTransform>();
                compRT.SetParent(_compsPanel);
            }

            _healthBar.MaxValue = _attachedShip.MaxHullHitPoints;
            _shieldBar.MaxValue = _attachedShip.ShipTotalMaxShields;
            _energyBar.MaxValue = _attachedShip.MaxEnergy;
            _heatBar.MaxValue = _attachedShip.MaxHeat;
        }
    }

    public void DetachShip()
    {

    }

    void Update()
    {
        if (_attachedShip != null)
        {
            _healthBar.Value = _attachedShip.HullHitPoints;
            _shieldBar.Value = _attachedShip.ShipTotalShields;
            _energyBar.Value = _attachedShip.Energy;
            _heatBar.Value = _attachedShip.Heat;
        }
    }

    public string ShipProductionKey;
    public Ship AttachedShip { get { return _attachedShip; } }

    private Ship _attachedShip = null;
    private GradientBar _healthBar;
    private GradientBar _shieldBar;
    private GradientBar _energyBar;
    private GradientBar _heatBar;
    private RectTransform _compsPanel;
}
