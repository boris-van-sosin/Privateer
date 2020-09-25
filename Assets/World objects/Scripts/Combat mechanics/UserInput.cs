using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

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
        //ContextMenu.transform.parent.transform.rotation = Quaternion.LookRotation(_userCamera.transform.forward, _userCamera.transform.up);
        _selectBox = ObjectFactory.GetSelectionBoxCanvas();
        _selectBoxRect = _selectBox.GetComponent<RectTransform>();
        _displaySelectBox = false;
    }

    void Awake()
    {
        _selectionHandler.SelectedShipPanel = SelectedShipPanel;
        _ammoStrings = new string[TurretBase.MaxWarheads];
        for (int i = 0; i < _ammoStrings.Length; ++i)
        {
            _ammoStrings[i] = ((char)('A' + i)).ToString();
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (_controlledShip == null)
        {
            return;
        }
        if (_statusTopLevelDisplay == null)
        {
            _statusTopLevelDisplay = ObjectFactory.CreateStatusPanel(_controlledShip, ShipStatusPanel);
            _statusTopLevelDisplay.SetName(_controlledShip.DisplayName);
        }
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        Vector3? hitFlat;
        Collider colliderHit;
        if (Physics.Raycast(ray, out hit, 2000))
        {
            hitFlat = new Vector3(hit.point.x, 0, hit.point.z);
            colliderHit = hit.collider;
            ShipBase s = ShipBase.FromCollider(colliderHit);
            if (DisplayContextMenu && s != null && s is Ship && ContextMenu != null)
            {
                ContextMenu.gameObject.SetActive(true);
                ContextMenu.transform.position = _userCamera.WorldToScreenPoint(ray.origin) + new Vector3(70, -10, 0);
                if (ContextMenu.DisplayedShip != s)
                {
                    ContextMenu.DisplayedShip = (Ship)s;
                    ContextMenu.SetText();
                }
            }
            else
            {
                ContextMenu.gameObject.SetActive(false);
            }
        }
        else
        {
            hitFlat = null;
            colliderHit = null;
            ContextMenu.gameObject.SetActive(false);
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

        _userCamera.transform.position = _controlledShip.transform.position + (_cameraOffsetFactor * _cameraOffset);
    }

    private void ShipControlMode(Vector3? clickPt, Collider colliderHit)
    {
        if (clickPt.HasValue)
        {
            _controlledShip.ManualTarget(clickPt.Value);
            if (!EventSystem.current.IsPointerOverGameObject() && Input.GetMouseButton(0))
            {
                if (!_grapplingMode)
                {
                    _controlledShip.FireManual(clickPt.Value);
                }
                else
                {
                    _controlledShip.FireHarpaxManual(clickPt.Value);
                }
            }
            if (!EventSystem.current.IsPointerOverGameObject() && Input.GetMouseButtonDown(1))
            {
                if (_controlledShipAI.ControlType == ShipAIController.ShipControlType.Manual)
                {
                    _controlledShipAI.ControlType = ShipAIController.ShipControlType.SemiAutonomous;
                }
                if (_controlledShipAI.ControlType == ShipAIController.ShipControlType.SemiAutonomous)
                {
                    _controlledShipAI.UserNavigateTo(clickPt.Value);
                }
            }
        }

        if (Input.GetKey(_keyMapping[UserOperation.Forward]))
        {
            if (_controlledShipAI.ControlType != ShipAIController.ShipControlType.Manual)
            {
                _controlledShipAI.ControlType = ShipAIController.ShipControlType.Manual;
            }
            _controlledShip.MoveForward();
        }
        else if (Input.GetKey(_keyMapping[UserOperation.Backward]))
        {
            if (_controlledShipAI.ControlType != ShipAIController.ShipControlType.Manual)
            {
                _controlledShipAI.ControlType = ShipAIController.ShipControlType.Manual;
            }
            _controlledShip.MoveBackward();
        }
        else if (Input.GetKey(_keyMapping[UserOperation.Break]))
        {
            if (_controlledShipAI.ControlType != ShipAIController.ShipControlType.Manual)
            {
                _controlledShipAI.ControlType = ShipAIController.ShipControlType.Manual;
            }
            _controlledShip.ApplyBraking();
        }

        if (Input.GetKey(_keyMapping[UserOperation.Left]))
        {
            if (_controlledShipAI.ControlType != ShipAIController.ShipControlType.Manual)
            {
                _controlledShipAI.ControlType = ShipAIController.ShipControlType.Manual;
            }
            _controlledShip.ApplyTurning(true);
        }
        else if (Input.GetKey(_keyMapping[UserOperation.Right]))
        {
            if (_controlledShipAI.ControlType != ShipAIController.ShipControlType.Manual)
            {
                _controlledShipAI.ControlType = ShipAIController.ShipControlType.Manual;
            }
            _controlledShip.ApplyTurning(false);
        }

        if (Input.GetKeyDown(_keyMapping[UserOperation.MagneticClamps]))
        {
            _controlledShip.ToggleElectromagneticClamps();
        }
        else if (Input.GetKeyDown(_keyMapping[UserOperation.Shields]))
        {
            _controlledShip.ToggleShields();
        }
        else if (Input.GetKeyDown(_keyMapping[UserOperation.GrapplingTool]))
        {
            if (_controlledShip.TowingByHarpax == null)
            {
                _controlledShip.GrapplingMode = !_controlledShip.GrapplingMode;
            }
            else
            {
                _controlledShip.DisconnectHarpaxTowing();
            }
        }
        else if (Input.GetKeyDown(_keyMapping[UserOperation.StrikeCraftLaunch]))
        {
            CarrierBehavior c;
            if ((c = _controlledShip.GetComponent<CarrierBehavior>()) != null)
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

        if (Input.GetKeyDown(_keyMapping[UserOperation.SwitchAmmo]))
        {
            IReadOnlyList<ITurret> manualTurrets = _controlledShip.WeaponGroups.ManualTurrets;
            _ammoIdx = (_ammoIdx + 1) % TurretBase.MaxWarheads;
            for (int i = 0; i < manualTurrets.Count; ++i)
            {
                if (manualTurrets[i] is GunTurret gt)
                {
                    gt.SwitchAmmoType(_ammoIdx);
                }
            }
            CurrAmmoTextBox.text = _ammoStrings[_ammoIdx];
        }

        foreach (ValueTuple<UserOperation, int> cg in _controlGroupKeys)
        {
            if (Input.GetKeyDown(_keyMapping[cg.Item1]))
            {
                if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
                {
                    _controlledShip.WeaponGroups.ToggleGroupAuto(cg.Item2);
                }
                else
                {
                    _controlledShip.WeaponGroups.SetGroupToMode(cg.Item2, TurretBase.TurretMode.Manual);
                }
                _statusTopLevelDisplay.ForceUpdateTurretModes();
            }
        }
    }

    private void FleetCommandMode(Vector3? clickPt, Collider colliderHit)
    {
        if (clickPt.HasValue)
        {
            if (!EventSystem.current.IsPointerOverGameObject())
            {
                if (Input.GetMouseButtonDown(0))
                {
                    _selectionHandler.ClickSelect(colliderHit);
                    _dragOrigin = clickPt.Value;
                }
                else if (Input.GetMouseButton(0))
                {
                    if (!_displaySelectBox)
                    {
                        _displaySelectBox = true;
                        _selectBox.gameObject.SetActive(true);
                    }
                    //Vector3 corner1 = _userCamera.WorldToScreenPoint(_dragOrigin);
                    //Vector3 corner2 = _userCamera.WorldToScreenPoint(clickPt.Value);
                    Vector3 corner1 = _dragOrigin;
                    Vector3 corner2 = clickPt.Value;
                    Vector3 cornerMaxMin = new Vector3(Mathf.Max(_dragOrigin.x, clickPt.Value.x), 0, Mathf.Min(_dragOrigin.z, clickPt.Value.z));
                    _selectBox.transform.position = cornerMaxMin;
                    _selectBoxRect.sizeDelta = new Vector2(Mathf.Abs(corner2.x - corner1.x), Mathf.Abs(corner2.z - corner1.z));
                    _dragDest = corner2;
                }
                else if (Input.GetMouseButtonUp(0))
                {
                    _selectionHandler.BoxSelect(_dragOrigin, _dragDest);
                    _displaySelectBox = false;
                    _selectBox.gameObject.SetActive(false);

                }
                if (Input.GetMouseButtonDown(1))
                {
                    ShipBase clickTarget = colliderHit != null ? ShipBase.FromCollider(colliderHit) : null;
                    if (clickTarget != null)
                    {
                        _selectionHandler.ClickOrder(clickTarget, SelectionHandler.OrderType.Follow);
                    }
                    else
                    {
                        _selectionHandler.ClickOrder(clickPt.Value);
                    }
                }
            }
        }
    }

    public Ship ControlledShip
    {
        get
        {
            return _controlledShip;
        }
        set
        {
            _controlledShip = value;
            if (_controlledShip != null)
                _controlledShipAI = _controlledShip.GetComponent<ShipAIController>();
            else
                _controlledShipAI = null;
        }
    }
    private Ship _controlledShip;
    private ShipAIController _controlledShipAI;
    private bool _grapplingMode = false; // temporary
    private StatusTopLevel _statusTopLevelDisplay = null;

    public Transform ShipStatusPanel;
    public ShipContextMenu ContextMenu;
    public bool DisplayContextMenu;
    public RectTransform SelectedShipPanel;
    public TextMeshProUGUI CurrAmmoTextBox;

    private Camera _userCamera;
    private Vector3 _cameraOffset;
    private float _cameraOffsetFactor = 1.0f;
    private bool _fleetMode = false;
    private int _ammoIdx = 0;
    private string[] _ammoStrings;

    private SelectionHandler _selectionHandler = new SelectionHandler();

    // Selection bo stuff:
    private bool _displaySelectBox;
    private Canvas _selectBox;
    private RectTransform _selectBoxRect;
    private Vector3 _dragOrigin, _dragDest;

    public enum UserOperation
    {
        Forward, Backward, Left, Right, Break, MagneticClamps, Shields, GrapplingTool, StrikeCraftLaunch, SwitchAmmo,
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
        { UserOperation.SwitchAmmo, KeyCode.Q },
        { UserOperation.ControlGroup1, KeyCode.Alpha1 },
        { UserOperation.ControlGroup2, KeyCode.Alpha2 },
        { UserOperation.ControlGroup3, KeyCode.Alpha3 },
        { UserOperation.ControlGroup4, KeyCode.Alpha4 },
        { UserOperation.ControlGroup5, KeyCode.Alpha5 },
        { UserOperation.ControlGroup6, KeyCode.Alpha6 },
    };

    private readonly ValueTuple<UserOperation, int>[] _controlGroupKeys = new ValueTuple<UserOperation, int>[]
    {
        new ValueTuple<UserOperation, int>(UserOperation.ControlGroup1, 1),
        new ValueTuple<UserOperation, int>(UserOperation.ControlGroup2, 2),
        new ValueTuple<UserOperation, int>(UserOperation.ControlGroup3, 3),
        new ValueTuple<UserOperation, int>(UserOperation.ControlGroup4, 4),
        new ValueTuple<UserOperation, int>(UserOperation.ControlGroup5, 5),
        new ValueTuple<UserOperation, int>(UserOperation.ControlGroup6, 6)
    };
}
