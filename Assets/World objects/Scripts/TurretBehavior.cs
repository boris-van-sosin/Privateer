using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class TurretBehavior : MonoBehaviour, ITurret
{

	// Use this for initialization
	void Start()
    {
		
	}

    void Awake()
    {
        ParseDeadZones();
        ParseMuzzles();
        _containingShip = FindContainingShip(transform.parent);
    }

    private void ParseDeadZones()
    {
        if (DeadZoneAngles != null)
        {
            _deadZoneAngleRanges = new Tuple<float, float>[DeadZoneAngles.Length];
            for (int i = 0; i < DeadZoneAngles.Length; ++i)
            {
                string[] nums = DeadZoneAngles[i].Split(',');
                _deadZoneAngleRanges[i] = Tuple<float, float>.Create(float.Parse(nums[0]), float.Parse(nums[1]));
            }
        }
        if (MinRotation < MaxRotation)
        {
            _rotationAllowedRanges = new Tuple<float, float>[1]
            {
                Tuple<float, float>.Create(MinRotation, MaxRotation),
            };
        }
        else if (MaxRotation < MinRotation)
        {
            _rotationAllowedRanges = new Tuple<float, float>[2]
{
                Tuple<float, float>.Create(MinRotation, 360),
                Tuple<float, float>.Create(0, MaxRotation)
            };
        }
    }

    private void ParseMuzzles()
    {
        List<Tuple<Transform, Transform>> barrelsFound = FindBarrels(transform).ToList();
        Barrels = new Transform[barrelsFound.Count];
        Muzzles = new Transform[barrelsFound.Count];
        for (int i = 0; i < barrelsFound.Count; ++i)
        {
            Barrels[i] = barrelsFound[i].Item1;
            Muzzles[i] = barrelsFound[i].Item2;
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
        if (Mathf.Abs(_targetAngle - CurrAngle) < maxRotation)
        {
            switch (TurretAxis)
            {
                case RotationAxis.XAxis:
                    transform.rotation = Quaternion.Euler(_targetAngle, transform.rotation.eulerAngles.y, transform.rotation.z);
                    break;
                case RotationAxis.YAxis:
                    transform.rotation = Quaternion.Euler(transform.rotation.eulerAngles.x, _targetAngle, transform.rotation.z);
                    break;
                case RotationAxis.ZAxis:
                    transform.rotation = Quaternion.Euler(transform.rotation.eulerAngles.x, transform.rotation.y, _targetAngle);
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
        Debug.Log(string.Format("Angle to target: {0}", angleToTarget));
        bool isLegalAngle = false;
        float closestLegalAngle = 0.0f, angleDiff = 360.0f;
        foreach (Tuple<float, float> r in _rotationAllowedRanges)
        {
            if (r.Item1 < angleToTarget && angleToTarget < r.Item2)
            {
                isLegalAngle = true;
                _targetAngle = angleToTarget;
                break;
            }
            else
            {
                float diff1, diff2;
                if ((diff1 = Mathf.Abs(r.Item1 - angleToTarget)) < angleDiff)
                {
                    angleDiff = diff1;
                    closestLegalAngle = r.Item1;
                }
                else if ((diff2 = Mathf.Abs(r.Item2 - angleToTarget)) < angleDiff)
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

        if (MinRotation < MaxRotation)
        {
            if (MinRotation == 0.0f && MaxRotation == 360.0f)
            {
                if (Mathf.Abs(_targetAngle - CurrAngle) <= 180.0f)
                {
                    _rotationDir = Mathf.Sign(_targetAngle - CurrAngle);
                }
                else
                {
                    _rotationDir = -Mathf.Sign(_targetAngle - CurrAngle);
                }
            }
            else
            {
                _rotationDir = Mathf.Sign(_targetAngle - CurrAngle);
            }
        }
        else
        {
            float curr = CurrAngle;
            if (MaxRotation < curr && MaxRotation < _targetAngle)
            {
                _rotationDir = Mathf.Sign(_targetAngle - curr);
            }
            else if (curr < MinRotation && _targetAngle < MinRotation)
            {
                _rotationDir = Mathf.Sign(_targetAngle - curr);
            }
            else
            {
                _rotationDir = Mathf.Sign(-_targetAngle + curr);
            }
        }
    }

    public void Fire(Vector3 target)
    {
        Vector3 firingVector = Muzzles[0].up;
        firingVector.y = target.y - Muzzles[0].position.y;
        Projectile p = ObjectFactory.CreateProjectile(firingVector, 10, 10, _containingShip);
        p.transform.position = Muzzles[0].position;
    }

    public float CurrAngle { get { return FilterRotation(transform.rotation.eulerAngles); } }

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

    public float MinRotation, MaxRotation;
    private Tuple<float, float>[] _rotationAllowedRanges;
    private bool _fixed = false;
    public float RotationSpeed;
    private float _targetAngle;
    private Vector3 _vectorToTarget;
    private float _rotationDir;
    private float _defaultAngle;
    private bool _targeting, _onTarget;
    public string[] DeadZoneAngles;
    private Tuple<float, float>[] _deadZoneAngleRanges;
    public RotationAxis TurretAxis;
    private Transform[] Barrels;
    private Transform[] Muzzles;
    private Ship _containingShip;

    public enum TurretMode { Off, Manual, Auto, AutoTracking };
    public enum RotationAxis { XAxis, YAxis, ZAxis };

    private static readonly string BarrelString = "Barrel";
    private static readonly string MuzzleString = "Muzzle";
}
