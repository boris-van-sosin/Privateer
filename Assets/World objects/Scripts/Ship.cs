using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Ship : MonoBehaviour {

	// Use this for initialization
	void Start ()
    {
        Transform turret1Root = transform.Find("Turret1");
        if (turret1Root != null)
        {
            TurretBehavior t = turret1Root.GetComponentInChildren<TurretBehavior>();
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
        }
    }
	
	// Update is called once per frame
	void Update()
    {
        transform.position += Mathf.Abs(Vector3.Dot(_velocity, transform.up.normalized)) * transform.up.normalized;
	}

    public void ManualTarget(Vector3 target)
    {
        foreach (ITurret t in _manualTurrets)
        {
            t.ManualTarget(target);
        }
    }

    public void ApplyThrust()
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

    public void ApplyBraking()
    {
        Vector3 brakeVec = -1f * Braking * Time.deltaTime * _velocity.normalized;
        Vector3 newVelocity = _velocity + brakeVec;
        if (Vector3.Dot(newVelocity, transform.up) < 0)
        {
            _velocity = Vector3.zero;
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

    public bool MovingForward { get { return Vector3.Dot(_velocity, transform.up) > 0; } }

    public void FireManual(Vector3 target)
    {
        foreach (ITurret t in _manualTurrets)
        {
            t.Fire(target);
        }
    }

    public float MaxSpeed;
    public float Mass;
    public float Thrust;
    public float Braking;
    public float TurnRate;
    private Vector3 _velocity;

    private ITurret[] _turrets;
    private IEnumerable<ITurret> _manualTurrets;
}
