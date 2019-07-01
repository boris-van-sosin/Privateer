using System;
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
        Physics.IgnoreLayerCollision(LayerMask.NameToLayer("Background"), LayerMask.NameToLayer("Strike Craft"), true);
        Physics.IgnoreLayerCollision(LayerMask.NameToLayer("Background"), LayerMask.NameToLayer("Torpedoes"), true);
        Physics.IgnoreLayerCollision(LayerMask.NameToLayer("Background"), LayerMask.NameToLayer("Weapons"), true);
        Physics.IgnoreLayerCollision(LayerMask.NameToLayer("Background"), LayerMask.NameToLayer("Effects"), true);

        Physics.IgnoreLayerCollision(LayerMask.NameToLayer("NavColliders"), LayerMask.NameToLayer("Ships"), true);
        Physics.IgnoreLayerCollision(LayerMask.NameToLayer("NavColliders"), LayerMask.NameToLayer("Shields"), true);
        Physics.IgnoreLayerCollision(LayerMask.NameToLayer("NavColliders"), LayerMask.NameToLayer("Strike Craft"), true);
        Physics.IgnoreLayerCollision(LayerMask.NameToLayer("NavColliders"), LayerMask.NameToLayer("Torpedoes"), true);
        Physics.IgnoreLayerCollision(LayerMask.NameToLayer("NavColliders"), LayerMask.NameToLayer("Weapons"), true);
        Physics.IgnoreLayerCollision(LayerMask.NameToLayer("NavColliders"), LayerMask.NameToLayer("Effects"), true);
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
        Vector3? hitFlat;
        Collider colliderHit;
        if (Physics.Raycast(ray, out hit, 2000))
        {
            hitFlat = new Vector3(hit.point.x, 0, hit.point.z);
            colliderHit = hit.collider;
        }
        else
        {
            hitFlat = null;
            colliderHit = null;
        }

        float scroll;
        if ((scroll = Input.GetAxis("Mouse ScrollWheel")) != 0.0f)
        {
            _cameraOffsetFactor += (-scroll * 0.1f);
        }

        if (Input.GetKeyDown(_keyMapping[UserOperation.SwitchMode]))
        {
            _fleetMode = !_fleetMode;
        }

        if (!_fleetMode)
        {
            ShipControlMode(hitFlat, colliderHit);
        }
        else
        {
            FleetCommandMode(hitFlat, colliderHit);
        }

        _userCamera.transform.position = ControlledShip.transform.position + (_cameraOffsetFactor * _cameraOffset);
    }

    private void ShipControlMode(Vector3? clickPt, Collider colliderHit)
    {
        if (clickPt.HasValue)
        {
            ControlledShip.ManualTarget(clickPt.Value);
            if (Input.GetMouseButton(0))
            {
                if (!_grapplingMode)
                {
                    ControlledShip.FireManual(clickPt.Value);
                }
                else
                {
                    ControlledShip.FireHarpaxManual(clickPt.Value);
                }
            }
            if (Input.GetMouseButtonDown(1))
            {
                ControlledShip.SetRequiredHeading(clickPt.Value);
            }
        }

        if (Input.GetKey(_keyMapping[UserOperation.Forward]))
        {
            ControlledShip.MoveForward();
        }
        else if (Input.GetKey(_keyMapping[UserOperation.Backward]))
        {
            ControlledShip.MoveBackward();
        }
        else if (Input.GetKey(_keyMapping[UserOperation.Break]))
        {
            ControlledShip.ApplyBraking();
        }

        if (Input.GetKey(_keyMapping[UserOperation.Left]))
        {
            ControlledShip.ApplyTurning(true);
        }
        else if (Input.GetKey(_keyMapping[UserOperation.Right]))
        {
            ControlledShip.ApplyTurning(false);
        }

        if (Input.GetKeyDown(_keyMapping[UserOperation.MagneticClamps]))
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
        else if (Input.GetKeyDown(_keyMapping[UserOperation.StrikeCraftLaunch]))
        {
            CarrierBehavior c;
            if ((c = ControlledShip.GetComponent<CarrierBehavior>()) != null)
            {
                c.LaunchDbg();
            }
        }
        else if (Input.GetKeyDown(KeyCode.R))
        {
            StrikeCraftFormationAIController sc = FindObjectOfType<StrikeCraftFormationAIController>();
            if (sc != null)
            {
                sc.OrderReturnToHost();
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
                _statusTopLevelDisplay.ForceUpdateTurretModes();
            }
        }
    }

    private void FleetCommandMode(Vector3? clickPt, Collider colliderHit)
    {
        if (clickPt.HasValue)
        {
            if (Input.GetMouseButton(0))
            {
                if (colliderHit != null)
                {
                    ShipBase s = ShipBase.FromCollider(colliderHit);
                    if (s != null && s is Ship)
                    {
                        _fleetSelectedShips.Clear();
                        _fleetSelectedShips.Add(s);
                    }
                }
            }
            if (Input.GetMouseButtonDown(1))
            {
                foreach (Ship s in _fleetSelectedShips)
                {
                    ShipAIController controller = s.GetComponent<ShipAIController>();
                    if (controller != null && !s.ShipDisabled && !s.ShipSurrendered)
                    {
                        controller.NavigateTo(clickPt.Value);
                    }
                }
            }
        }
    }

    public Ship ControlledShip; // temporary
    private bool _grapplingMode = false; // temporary
    private StatusTopLevel _statusTopLevelDisplay = null;
    public Transform ShipStatusPanel;
    private Camera _userCamera;
    private Vector3 _cameraOffset;
    private float _cameraOffsetFactor = 1.0f;
    private bool _fleetMode = false;
    private List<ShipBase> _fleetSelectedShips = new List<ShipBase>();

    public enum UserOperation
    {
        Forward, Backward, Left, Right, Break, MagneticClamps, Shields, GrapplingTool, StrikeCraftLaunch,
        SwitchMode,
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
        { UserOperation.SwitchMode, KeyCode.Tab },
        { UserOperation.MagneticClamps, KeyCode.F },
        { UserOperation.Shields, KeyCode.G },
        { UserOperation.GrapplingTool, KeyCode.H },
        { UserOperation.StrikeCraftLaunch, KeyCode.L },
        { UserOperation.ControlGroup1, KeyCode.Alpha1 },
        { UserOperation.ControlGroup2, KeyCode.Alpha2 },
        { UserOperation.ControlGroup3, KeyCode.Alpha3 },
        { UserOperation.ControlGroup4, KeyCode.Alpha4 },
        { UserOperation.ControlGroup5, KeyCode.Alpha5 },
        { UserOperation.ControlGroup6, KeyCode.Alpha6 },
    };

    private readonly Tuple<UserOperation, int>[] _controlGroupKeys = new Tuple<UserOperation, int>[]
    {
        new Tuple<UserOperation, int>(UserOperation.ControlGroup1, 1),
        new Tuple<UserOperation, int>(UserOperation.ControlGroup2, 2),
        new Tuple<UserOperation, int>(UserOperation.ControlGroup3, 3),
        new Tuple<UserOperation, int>(UserOperation.ControlGroup4, 4),
        new Tuple<UserOperation, int>(UserOperation.ControlGroup5, 5),
        new Tuple<UserOperation, int>(UserOperation.ControlGroup6, 6)
    };
}
