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
        Physics.IgnoreLayerCollision(LayerMask.NameToLayer("Background"), LayerMask.NameToLayer("Ships"), true);
        Physics.IgnoreLayerCollision(LayerMask.NameToLayer("Background"), LayerMask.NameToLayer("Shields"), true);
        Physics.IgnoreLayerCollision(LayerMask.NameToLayer("Background"), LayerMask.NameToLayer("Stike Craft"), true);
        Physics.IgnoreLayerCollision(LayerMask.NameToLayer("Background"), LayerMask.NameToLayer("Stike Torpedoes"), true);
        Physics.IgnoreLayerCollision(LayerMask.NameToLayer("Background"), LayerMask.NameToLayer("Weapons"), true);
        Physics.IgnoreLayerCollision(LayerMask.NameToLayer("Background"), LayerMask.NameToLayer("Effects"), true);
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
                if (!_grapplingMode)
                {
                    ControlledShip.FireManual(hitFlat);
                }
                else
                {
                    ControlledShip.FireHarpaxManual(hitFlat);
                }
            }
            if (Input.GetMouseButtonDown(1))
            {
                ControlledShip.SetRequiredHeading(hitFlat);
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
        else if (Input.GetKeyDown(_keyMapping[UserOperation.GrapplingTool]))
        {
            if (ControlledShip.TowingByHarpax == null)
            {
                ControlledShip.GrapplingMode = !ControlledShip.GrapplingMode;
            }
            else
            {
                ControlledShip.DisconnectHarpaxTowing();
            }
        }
        foreach (Tuple<UserOperation, int> cg in _controlGroupKeys)
        {
            if (Input.GetKeyDown(_keyMapping[cg.Item1]))
            {
                if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
                {
                    ControlledShip.WeaponGroups.ToggleGroupAuto(cg.Item2);
                }
                else
                {
                    ControlledShip.WeaponGroups.SetGroupToMode(cg.Item2, TurretBase.TurretMode.Manual);
                }
            }
        }

        _userCamera.transform.position = ControlledShip.transform.position + (_cameraOffsetFactor * _cameraOffset);
    }

    public Ship ControlledShip; // temporary
    private bool _autoTarget = false; // temporary
    private bool _grapplingMode = false; // temporary
    private StatusTopLevel _statusTopLevelDisplay = null;
    public Transform ShipStatusPanel;
    private Camera _userCamera;
    private Vector3 _cameraOffset;
    private float _cameraOffsetFactor = 1.0f;

    public enum UserOperation
    {
        Forward, Backward, Left, Right, Break, MagneticClamps, Shields, GrapplingTool,
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

    private readonly Dictionary<UserOperation, KeyCode> _keyMapping = new Dictionary<UserOperation, KeyCode>()
    {
        { UserOperation.Forward, KeyCode.W },
        { UserOperation.Left, KeyCode.A },
        { UserOperation.Backward, KeyCode.S },
        { UserOperation.Right, KeyCode.D },
        { UserOperation.Break, KeyCode.X },
        { UserOperation.MagneticClamps, KeyCode.F },
        { UserOperation.Shields, KeyCode.G },
        { UserOperation.GrapplingTool, KeyCode.H },
        { UserOperation.ControlGroup1, KeyCode.Alpha1 },
        { UserOperation.ControlGroup2, KeyCode.Alpha2 },
        { UserOperation.ControlGroup3, KeyCode.Alpha3 },
        { UserOperation.ControlGroup4, KeyCode.Alpha4 },
        { UserOperation.ControlGroup5, KeyCode.Alpha5 },
        { UserOperation.ControlGroup6, KeyCode.Alpha6 },
    };

    private readonly Tuple<UserOperation, int>[] _controlGroupKeys = new Tuple<UserOperation, int>[]
    {
        Tuple<UserOperation, int>.Create(UserOperation.ControlGroup1, 1),
        Tuple<UserOperation, int>.Create(UserOperation.ControlGroup2, 2),
        Tuple<UserOperation, int>.Create(UserOperation.ControlGroup3, 3),
        Tuple<UserOperation, int>.Create(UserOperation.ControlGroup4, 4),
        Tuple<UserOperation, int>.Create(UserOperation.ControlGroup5, 5),
        Tuple<UserOperation, int>.Create(UserOperation.ControlGroup6, 6)
    };
}
