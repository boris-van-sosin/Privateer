using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class StatusTopLevel : MonoBehaviour
{
    void Start()
    {
        _compsPanel = transform.Find("CompsPanel").GetComponent<RectTransform>();
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

            foreach (IShipActiveComponent comp in _attachedShip.AllComponents.Where(x => x is IShipActiveComponent && !(x is TurretBase)).Select(y => y as IShipActiveComponent))
            {
                StatusSubsystem compStatus = ObjectFactory.CreateStatusSubsytem(comp);
                RectTransform compRT = compStatus.GetComponent<RectTransform>();
                compRT.SetParent(_compsPanel);
            }
        }
    }

    public void DetachShip()
    {

    }

    public Ship AttachedShip { get { return _attachedShip; } }

    private Ship _attachedShip = null;
    private RectTransform _compsPanel;
}
