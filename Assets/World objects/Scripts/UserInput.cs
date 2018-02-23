using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UserInput : MonoBehaviour
{

	// Use this for initialization
	void Start()
    {

	}

    void Awake()
    {
        _backgroundLayerMask = LayerMask.GetMask("Background");
        Physics.IgnoreLayerCollision(LayerMask.NameToLayer("Background"), LayerMask.NameToLayer("Default"), true);
    }

    // Update is called once per frame
    void Update()
    {
        if (ControlledShip == null)
        {
            return;
        }
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit, 2000))
        {
            Vector3 hitFlat = new Vector3(hit.point.x, 0, hit.point.z);
            if (!_autoTarget)
            {
                ControlledShip.ManualTarget(hitFlat);
            }
            if (Input.GetMouseButton(0))
            {
                ControlledShip.FireManual(hitFlat);
            }
            if (Input.GetMouseButtonDown(1))
            {
                ControlledShip.SetRequiredHeading(hitFlat);
            }
            if (Input.GetKeyDown(KeyCode.Alpha1))
            {
                _autoTarget = !_autoTarget;
                foreach (ITurret t in ControlledShip.Turrets)
                {
                    if (_autoTarget)
                    {
                        t.SetTurretBehavior(TurretBase.TurretMode.Auto);
                    }
                    else
                    {
                        t.SetTurretBehavior(TurretBase.TurretMode.Manual);
                    }
                }
            }
        }

        float scroll;
        if ((scroll = Input.GetAxis("Mouse ScrollWheel")) != 0.0f)
        {
            ControlledShip.CameraOffsetFactor += (-scroll * 0.1f);
        }


        if (Input.GetKey(KeyCode.W))
        {
            ControlledShip.MoveForeward();
        }
        else if (Input.GetKey(KeyCode.S))
        {
            ControlledShip.MoveBackward();
        }
        if (Input.GetKey(KeyCode.A))
        {
            ControlledShip.ApplyTurning(true);
        }
        else if (Input.GetKey(KeyCode.D))
        {
            ControlledShip.ApplyTurning(false);
        }
    }

    public Ship ControlledShip; // temporary
    private bool _autoTarget = false; // temporary
    private Ship _heightTargetShip;
    private int _backgroundLayerMask = 0;
}
