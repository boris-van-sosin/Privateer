using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UserInput : MonoBehaviour
{

	// Use this for initialization
	void Start()
    {
        _userCamera = Camera.main;
        _cameraOffset = _userCamera.transform.position;
        // Use for grabbing top-down views of ships:
        //_cameraOffset.z = 0;
        //_userCamera.transform.rotation = Quaternion.LookRotation(Vector3.down, -Vector3.forward);
        //
        _cameraOffsetFactor = 1.0f;
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
        if (_statusTopLevelDisplay == null)
        {
            _statusTopLevelDisplay = ObjectFactory.CreateStatusPanel(ControlledShip, ShipStatusPanel);
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
            _cameraOffsetFactor += (-scroll * 0.1f);
        }


        if (Input.GetKey(_keyMapping[UserOperation.Forward]))
        {
            ControlledShip.MoveForeward();
        }
        else if (Input.GetKey(_keyMapping[UserOperation.Backward]))
        {
            ControlledShip.MoveBackward();
        }
        if (Input.GetKey(_keyMapping[UserOperation.Left]))
        {
            ControlledShip.ApplyTurning(true);
        }
        else if (Input.GetKey(_keyMapping[UserOperation.Right]))
        {
            ControlledShip.ApplyTurning(false);
        }
        else if (Input.GetKey(_keyMapping[UserOperation.Break]))
        {
            ControlledShip.ApplyBraking();
        }
        else if (Input.GetKeyDown(_keyMapping[UserOperation.MagneticClamps]))
        {
            ControlledShip.ToggleElectromagneticClamps();
        }
        else if (Input.GetKeyDown(_keyMapping[UserOperation.Shields]))
        {
            ControlledShip.ToggleShields();
        }

        _userCamera.transform.position = ControlledShip.transform.position + (_cameraOffsetFactor * _cameraOffset);
    }

    public Ship ControlledShip; // temporary
    private bool _autoTarget = false; // temporary
    private StatusTopLevel _statusTopLevelDisplay = null;
    public Transform ShipStatusPanel;
    private int _backgroundLayerMask = 0;
    private Camera _userCamera;
    private Vector3 _cameraOffset;
    private float _cameraOffsetFactor = 1.0f;

    public enum UserOperation
    {
        Forward, Backward, Left, Right, Break, MagneticClamps, Shields, BoardingTool,
        ControlGroup1,
        ControlGroup2,
        ControlGroup3,
        ControlGroup4,
        ControlGroup5,
        ControlGroup6,
        ControlGroup7,
        ControlGroup8,
        ControlGroup9,
        ControlGroup0
    }

    private Dictionary<UserOperation, KeyCode> _keyMapping = new Dictionary<UserOperation, KeyCode>()
    {
        { UserOperation.Forward, KeyCode.W },
        { UserOperation.Left, KeyCode.A },
        { UserOperation.Backward, KeyCode.S },
        { UserOperation.Right, KeyCode.D },
        { UserOperation.Break, KeyCode.X },
        { UserOperation.MagneticClamps, KeyCode.F },
        { UserOperation.Shields, KeyCode.G },
    };
}
