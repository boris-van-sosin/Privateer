using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

public class Ship : MonoBehaviour
{
    void Awake()
    {
        if (Follow)
        {
            _userCamera = Camera.main;
            _cameraOffset = _userCamera.transform.position - transform.position;
            CameraOffsetFactor = 1.0f;
        }
    }

    // Use this for initialization
    void Start ()
    {
        /*Transform turret1Root = transform.Find("Turret1");
        if (turret1Root != null)
        {
            TurretBase t = turret1Root.GetComponentInChildren<TurretBase>();
            if (t != null)
            {
                _turrets = new ITurret[]
                {
                    t
                };
                HashSet<ITurret> selectedTurrets = new HashSet<ITurret>()
                {
                    t
                };
                _manualTurrets = selectedTurrets;
            }
        }*/
        FindTurrets();
        _manualTurrets = new HashSet<ITurret>(_turrets);
    }
	
    private void FindTurrets()
    {
        TurretHardpoint[] hardpoints = GetComponentsInChildren<TurretHardpoint>();
        List<ITurret> turrets = new List<ITurret>(hardpoints.Length);
        foreach (TurretHardpoint hp in hardpoints)
        {
            ITurret turret = hp.GetComponentInChildren<TurretBase>();
            if (turret != null)
            {
                turrets.Add(turret);
            }
        }
        _turrets = turrets.ToArray();
    }

	// Update is called once per frame
	void Update()
    {
        float directionMult = 0.0f;
        if (MovementDirection == ShipDirection.Forward)
        {
            directionMult = 1.0f;
        }
        else if (MovementDirection == ShipDirection.Reverse)
        {
            directionMult = -1.0f;
        }
        transform.position += Time.deltaTime * (ActualVelocity = Mathf.Abs(Vector3.Dot(_velocity, transform.up.normalized)) * directionMult * transform.up.normalized);
        if (Follow) Debug.Log(string.Format("Velocity vector: {0}", ActualVelocity));
        if (_autoHeading)
        {
            RotateToHeading();
            //ApplyThrust();
        }
        if (_userCamera != null)
        {
            _userCamera.transform.position = transform.position + (_cameraOffset * CameraOffsetFactor);
        }
	}

    public void ManualTarget(Vector3 target)
    {
        foreach (ITurret t in _manualTurrets)
        {
            t.ManualTarget(target);
        }
    }

    public void MoveForeward()
    {
        if (MovementDirection == ShipDirection.Stopped)
        {
            MovementDirection = ShipDirection.Forward;
        }
        if (MovementDirection == ShipDirection.Forward)
        {
            ApplyThrust();
        }
        else if (MovementDirection == ShipDirection.Reverse)
        {
            ApplyBraking();
        }
    }

    private void ApplyThrust()
    {
        Vector3 thrustVec = Thrust * Time.deltaTime * transform.up.normalized;
        Vector3 newVelocity = _velocity + thrustVec;
        if (newVelocity.sqrMagnitude > (MaxSpeed * MaxSpeed))
        {
            _velocity = newVelocity.normalized * MaxSpeed;
        }
        else
        {
            _velocity = newVelocity;
        }
    }

    public void MoveBackward()
    {
        if (MovementDirection == ShipDirection.Stopped)
        {
            MovementDirection = ShipDirection.Reverse;
        }
        if (MovementDirection == ShipDirection.Forward)
        {
            ApplyBraking();
        }
        else if (MovementDirection == ShipDirection.Reverse)
        {
            ApplyThrust();
        }

    }

    private void ApplyBraking()
    {
        Vector3 brakeVec = -1f * Braking * Time.deltaTime * _velocity.normalized;
        Vector3 newVelocity = _velocity + brakeVec;
        if (Vector3.Dot(newVelocity, transform.up) < 0)
        {
            _velocity = Vector3.zero;
            MovementDirection = ShipDirection.Stopped;
        }
        else
        {
            _velocity = newVelocity;
        }
    }

    public void ApplyTurning(bool left)
    {
        float turnFactor = 1.0f;
        if (left)
        {
            turnFactor = -1.0f;
        }
        Quaternion deltaRot = Quaternion.AngleAxis(turnFactor * TurnRate * Time.deltaTime, transform.forward);
        transform.rotation = deltaRot * transform.rotation;
    }

    public void SetRequiredHeading(Vector3 targetPoint)
    {
        Vector3 requiredHeadingVector = targetPoint - transform.position;
        requiredHeadingVector.y = 0;
        _autoHeadingRotation = Quaternion.LookRotation(transform.forward, requiredHeadingVector);
        _autoHeading = true;
    }

    private void RotateToHeading()
    {
        if (Quaternion.Angle(transform.rotation,_autoHeadingRotation) < 0.5f)
        {
            _autoHeading = false;
            return;
        }
        transform.rotation = Quaternion.RotateTowards(transform.rotation, _autoHeadingRotation, TurnRate * Time.deltaTime);
    }

    public bool MovingForward { get { return Vector3.Dot(_velocity, transform.up) > 0; } }

    public void FireManual(Vector3 target)
    {
        StringBuilder sb = new StringBuilder();
        foreach (ITurret t in _manualTurrets)
        {
            t.Fire(target);
            sb.AppendFormat("Turret {0}:{1}, ", t, t.CurrLocalAngle);
        }
        Debug.Log(sb.ToString());
    }

    public Vector3 ActualVelocity { get; private set; }

    private enum ShipDirection { Stopped, Forward, Reverse };

    public float MaxSpeed;
    public float Mass;
    public float Thrust;
    public float Braking;
    public float TurnRate;
    private Vector3 _velocity;
    private bool _autoHeading = false;
    private Quaternion _autoHeadingRotation;
    private ShipDirection MovementDirection = ShipDirection.Stopped;

    private ITurret[] _turrets;
    private IEnumerable<ITurret> _manualTurrets;

    private Camera _userCamera;
    private Vector3 _cameraOffset;
    public float CameraOffsetFactor { get; set; }



    public bool Follow; // tmp
}
