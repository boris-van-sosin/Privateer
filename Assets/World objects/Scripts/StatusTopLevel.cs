using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StatusTopLevel : MonoBehaviour
{
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
        }
    }

    public void DetachShip()
    {

    }

    public Ship AttachedShip { get { return _attachedShip; } }

    private Ship _attachedShip = null;
}
