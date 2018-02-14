﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class TurretBase : ActiveShipComponent, ITurret
{

	// Use this for initialization
	void Start()
    {
		
	}

    void Awake()
    {
        TurretHardpoint parentHardpoint;
        if (transform.parent != null && (parentHardpoint = GetComponentInParent<TurretHardpoint>()) != null)
        {
            _minRotation = parentHardpoint.MinRotation;
            _maxRotation = parentHardpoint.MaxRotation;
            _deadZoneAngleStrings = parentHardpoint.DeadZoneAngles.ToArray();
        }
        else
        {
            _minRotation = 0.0f;
            _maxRotation = 0.0f;
            _deadZoneAngleStrings = null;
        }
        ParseDeadZones();
        ParseMuzzles();
        _containingShip = FindContainingShip(transform.parent);
    }

    private void ParseDeadZones()
    {
        if (_deadZoneAngleStrings != null)
        {
            _deadZoneAngleRanges = new Tuple<float, float>[_deadZoneAngleStrings.Length];
            for (int i = 0; i < _deadZoneAngleStrings.Length; ++i)
            {
                string[] nums = _deadZoneAngleStrings[i].Split(',');
                _deadZoneAngleRanges[i] = Tuple<float, float>.Create(float.Parse(nums[0]), float.Parse(nums[1]));
            }
        }
        if (_minRotation < _maxRotation)
        {
            _rotationAllowedRanges = new Tuple<float, float>[1]
            {
                Tuple<float, float>.Create(_minRotation, _maxRotation),
            };
        }
        else if (_maxRotation < _minRotation)
        {
            _rotationAllowedRanges = new Tuple<float, float>[2]
{
                Tuple<float, float>.Create(_minRotation, 360),
                Tuple<float, float>.Create(0, _maxRotation)
            };
        }
        else
        {
            _fixed = true;
        }
    }

    private void ParseMuzzles()
    {
        List<Tuple<Transform, Transform>> barrelsFound = FindBarrels(transform).ToList();
        Barrels = new Transform[barrelsFound.Count];
        Muzzles = new Transform[barrelsFound.Count];
        MuzzleFx = new ParticleSystem[barrelsFound.Count];
        for (int i = 0; i < barrelsFound.Count; ++i)
        {
            Barrels[i] = barrelsFound[i].Item1;
            Muzzles[i] = barrelsFound[i].Item2;
            MuzzleFx[i] = barrelsFound[i].Item2.GetComponentInChildren<ParticleSystem>();
        }
    }

    private static IEnumerable<Tuple<Transform, Transform>> FindBarrels(Transform root)
    {
        if (root.name.StartsWith(BarrelString))
        {
            string suffix = root.name.Substring(BarrelString.Length);
            Transform muzzle = root.Find(MuzzleString + suffix);
            yield return Tuple<Transform, Transform>.Create(root, muzzle);
        }
        else
        {
            for (int i = 0; i < root.childCount; ++i)
            {
                IEnumerable<Tuple<Transform, Transform>> resInChildren = FindBarrels(root.GetChild(i));
                foreach (Tuple<Transform, Transform> r in resInChildren)
                {
                    yield return r;
                }
            }
        }
    }

    private static Ship FindContainingShip(Transform t)
    {
        if (t == null)
        {
            return null;
        }
        Ship s = t.GetComponent<Ship>();
        if (s != null)
        {
            return s;
        }
        return FindContainingShip(t.parent);
    }

    private void SetDefaultAngle()
    {
        _defaultAngle = transform.rotation.eulerAngles.z;
    }

    // Update is called once per frame
    void Update()
    {
        float maxRotation = RotationSpeed * Time.deltaTime;
        //Debug.Log(string.Format("Turret angle: global: {0} local: {1} target (global): {2}", CurrAngle, CurrLocalAngle, _globalTargetAngle));
        if (Mathf.Abs(_globalTargetAngle - CurrAngle) < maxRotation)
        {
            switch (TurretAxis)
            {
                case RotationAxis.XAxis:
                    transform.rotation = Quaternion.Euler(_globalTargetAngle, transform.rotation.eulerAngles.y, transform.rotation.z);
                    break;
                case RotationAxis.YAxis:
                    transform.rotation = Quaternion.Euler(transform.rotation.eulerAngles.x, _globalTargetAngle, transform.rotation.z);
                    break;
                case RotationAxis.ZAxis:
                    transform.rotation = Quaternion.Euler(transform.rotation.eulerAngles.x, transform.rotation.y, _globalTargetAngle);
                    break;
                default:
                    break;
            }
        }
        else
        {
            transform.rotation = transform.rotation * Quaternion.AngleAxis(maxRotation * _rotationDir, TurretAxisVector);
        }
    }

    public void ManualTarget(Vector3 target)
    {
        if (_fixed)
        {
            return;
        }

        _vectorToTarget = target - transform.position;
        Vector3 flatVec = new Vector3(_vectorToTarget.x, 0, _vectorToTarget.z);
        float angleToTarget = Quaternion.LookRotation(-flatVec).eulerAngles.y;
        float relativeAngle = AngleToShipHeading(angleToTarget);
        //Debug.Log(string.Format("Angle to target: {0}", relativeAngle));
        bool isLegalAngle = false;
        float closestLegalAngle = 0.0f, angleDiff = 360.0f;
        foreach (Tuple<float, float> r in _rotationAllowedRanges)
        {
            if (r.Item1 < relativeAngle && relativeAngle < r.Item2)
            {
                isLegalAngle = true;
                _targetAngle = relativeAngle;
                break;
            }
            else
            {
                float diff1, diff2;
                if ((diff1 = Mathf.Abs(r.Item1 - relativeAngle)) < angleDiff)
                {
                    angleDiff = diff1;
                    closestLegalAngle = r.Item1;
                }
                else if ((diff2 = Mathf.Abs(r.Item2 - relativeAngle)) < angleDiff)
                {
                    angleDiff = diff2;
                    closestLegalAngle = r.Item2;
                }
            }
        }
        if (!isLegalAngle)
        {
            _targetAngle = closestLegalAngle;
        }
        _globalTargetAngle = AngleToShipHeading(_targetAngle, true);

        if (_minRotation < _maxRotation)
        {
            if (_minRotation == 0.0f && _maxRotation == 360.0f)
            {
                if (Mathf.Abs(_globalTargetAngle - CurrAngle) <= 180.0f)
                {
                    _rotationDir = Mathf.Sign(_globalTargetAngle - CurrAngle);
                }
                else
                {
                    _rotationDir = -Mathf.Sign(_globalTargetAngle - CurrAngle);
                }
            }
            else
            {
                _rotationDir = Mathf.Sign(_targetAngle - CurrLocalAngle);
            }
        }
        else
        {
            float curr = CurrLocalAngle;
            if (_maxRotation < curr && _maxRotation < _targetAngle)
            {
                _rotationDir = Mathf.Sign(_targetAngle - curr);
            }
            else if (curr < _minRotation && _targetAngle < _minRotation)
            {
                _rotationDir = Mathf.Sign(_targetAngle - curr);
            }
            else
            {
                _rotationDir = Mathf.Sign(-_targetAngle + curr);
            }
        }
    }

    private bool CanFire()
    {
        if (_deadZoneAngleRanges != null)
        {
            foreach (Tuple<float, float> d in _deadZoneAngleRanges)
            {
                float currAngle = CurrLocalAngle;
                if (d.Item1 < currAngle && currAngle < d.Item2)
                {
                    return false;
                }
            }
        }
        return true;
    }

    protected virtual void FireInner(Vector3 firingVector)
    {
        // Stub!
    }

    public void Fire(Vector3 target)
    {
        if (!CanFire())
        {
            return;
        }
        Vector3 firingVector = Muzzles[_nextBarrel].up;
        firingVector.y = target.y - Muzzles[_nextBarrel].position.y;

        FireInner(firingVector);

        if (Muzzles.Length > 1)
        {
            _nextBarrel = (_nextBarrel + 1) % Muzzles.Length;
        }
    }

    public float CurrAngle { get { return FilterRotation(transform.rotation.eulerAngles); } }
    public float CurrLocalAngle
    {
        get
        {
            return AngleToShipHeading(FilterRotation(transform.rotation.eulerAngles));
        }
    }

    private float AngleToShipHeading(float globalAngle)
    {
        return AngleToShipHeading(globalAngle, false);
    }

    private float AngleToShipHeading(float globalAngle, bool inverse)
    {
        Vector3 forwardClean = Vector3.forward;
        forwardClean.y = 0;
        //float angleOffset = Quaternion.Angle(Quaternion.LookRotation(-Vector3.forward, Vector3.up), Quaternion.LookRotation(forwardClean, Vector3.up));
        Vector3 shipHeadingClean = _containingShip.transform.up;
        shipHeadingClean.y = 0;
        float angleOffset = Quaternion.FromToRotation(shipHeadingClean, forwardClean).eulerAngles.y;
        if (inverse)
        {
            angleOffset = -angleOffset;
        }
        float finalAngle = globalAngle + angleOffset;
        if (finalAngle > 360f)
        {
            finalAngle -= 360f;
        }
        else if (finalAngle < 0)
        {
            finalAngle += 360;
        }
        return finalAngle;
    }

    private float FilterRotation(Vector3 rot)
    {
        switch (TurretAxis)
        {
            case RotationAxis.XAxis:
                return rot.x;
                break;
            case RotationAxis.YAxis:
                return rot.y;
                break;
            case RotationAxis.ZAxis:
                return rot.z;
                break;
            default:
                return rot.y; // This is the most common.
                break;
        }
    }

    private Vector3 TurretAxisVector
    {
        get
        {
            switch (TurretAxis)
            {
                case RotationAxis.XAxis:
                    return Vector3.right;
                    break;
                case RotationAxis.YAxis:
                    return Vector3.up;
                    break;
                case RotationAxis.ZAxis:
                    return Vector3.forward;
                    break;
                default:
                    return Vector3.up; // This is the most common.
                    break;
            }
        }
    }

    // Rotation behavior variables:
    private float _minRotation, _maxRotation;
    private Tuple<float, float>[] _rotationAllowedRanges;
    private bool _fixed = false;
    public float RotationSpeed;
    private float _targetAngle;
    private float _globalTargetAngle;
    private float _localTargetAngle;
    private Vector3 _vectorToTarget;
    private float _rotationDir;
    private float _defaultAngle;
    private bool _targeting, _onTarget;
    private string[] _deadZoneAngleStrings;
    private Tuple<float, float>[] _deadZoneAngleRanges;
    public RotationAxis TurretAxis;

    // Barrels, muzzles, and muzzleFx data:
    protected Transform[] Barrels;
    protected Transform[] Muzzles;
    protected ParticleSystem[] MuzzleFx;
    protected int _nextBarrel = 0;

    // Weapon data:
    public ComponentSlotType TurretType;
    public float MaxRange;
    public float FiringInterval;

    // Turret status:
    public int IsJammed { get; private set; }

    public enum TurretMode { Off, Manual, Auto, AutoTracking };
    public enum RotationAxis { XAxis, YAxis, ZAxis };

    private static readonly string BarrelString = "Barrel";
    private static readonly string MuzzleString = "Muzzle";
}