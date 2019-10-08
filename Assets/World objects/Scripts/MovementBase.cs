using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class MovementBase : MonoBehaviour
{
    protected virtual void Update()
    {
        if (_currManeuver != null)
        {
            _currManeuver.Advance(Time.deltaTime);
            if (_currManeuver.Finished)
            {
                _currManeuver = null;
            }
        }
        else
        {
            ApplyMovement();
        }
    }

    private void OnDrawGizmos()
    {
        if (_currManeuver != null)
        {
            foreach (ValueTuple<Vector3, Vector3> sample in _currManeuver.DebugCurve())
            {
                Gizmos.DrawLine(sample.Item1, sample.Item2);
            }
        }
    }

    protected virtual void ApplyMovement()
    {
        ApplyUpdateAcceleration();
        ApplyUpdateTurning();

        float directionMult = 0.0f;
        if (_movementDirection == ShipDirection.Forward)
        {
            directionMult = 1.0f;
        }
        else if (_movementDirection == ShipDirection.Reverse)
        {
            directionMult = -1.0f;
        }

        _prevPos = transform.position;
        _prevRot = transform.rotation;
        ActualVelocity = directionMult * _speed * transform.up;// was: Time.deltaTime * (ActualVelocity = directionMult * _speed * transform.up);

        if (_movementDirection == ShipDirection.Stopped)
        {
            ActualVelocity = Vector3.zero;
        }
        else
        {
            transform.position += ActualVelocity * Time.deltaTime;
        }
    }

    public virtual void ApplyTurning(bool left)
    {
        _nextTurnLeft = left;
        _nextTurnRight = !left;
    }

    public virtual void MoveForward()
    {
        if (_movementDirection == ShipDirection.Stopped)
        {
            _movementDirection = ShipDirection.Forward;
        }
        if (_movementDirection == ShipDirection.Forward)
        {
            ApplyThrust();
        }
        else if (_movementDirection == ShipDirection.Reverse)
        {
            ApplyBrakingInner();
        }
    }

    public virtual void MoveBackward()
    {
        if (_movementDirection == ShipDirection.Stopped)
        {
            _movementDirection = ShipDirection.Reverse;
        }
        if (_movementDirection == ShipDirection.Forward)
        {
            ApplyBrakingInner();
        }
        else if (_movementDirection == ShipDirection.Reverse)
        {
            ApplyThrust();
        }
    }

    protected virtual void ApplyThrust()
    {
        _nextAccelerate = true;
        _nextBrake = false;
    }

    public virtual void ApplyBraking()
    {
        ApplyBrakingInner();
    }

    protected virtual void ApplyBrakingInner()
    {
        _nextAccelerate = false;
        _nextBrake = true;
    }

    protected virtual void ApplyUpdateAcceleration()
    {
        if (_nextAccelerate)
        {
            _nextAccelerate = _nextBrake = false;

            _speed = Mathf.Min(_speed + Thrust * Time.deltaTime, MaxSpeed);
        }
        else if (_nextBrake)
        {
            _nextAccelerate = _nextBrake = false;

            float newSpeed = _speed - Braking * Time.deltaTime;
            if (newSpeed <= 0)
            {
                _movementDirection = ShipDirection.Stopped;
                _speed = 0;
            }
        }
        else if (UseTargetSpeed)
        {
            _nextAccelerate = _nextBrake = false;
            float actualTargetSpeed = Mathf.Clamp(TargetSpeed, 0, MaxSpeed);
            if (_speed < actualTargetSpeed)
            {
                _speed = Mathf.Min(_speed + Thrust * Time.deltaTime, actualTargetSpeed);
            }
            else if (_speed > actualTargetSpeed)
            {
                float targetSpeedBraking = actualTargetSpeed;
                float newSpeed = _speed - Braking * Time.deltaTime;
                if (targetSpeedBraking > _speed)
                {
                    return;
                }
                if (newSpeed > targetSpeedBraking)
                {
                    _speed = newSpeed;
                }
                if (newSpeed < actualTargetSpeed)
                {
                    _speed = newSpeed = actualTargetSpeed;
                }
                if (newSpeed <= 0)
                {
                    _movementDirection = ShipDirection.Stopped;
                    _speed = 0;
                }

            }
        }
    }

    protected virtual void ApplyUpdateTurning()
    {
        if (!(_nextTurnLeft || _nextTurnRight))
        {
            return;
        }
        bool thisLeft = _nextTurnLeft;
        _nextTurnLeft = _nextTurnRight = false;

        float turnFactor = 1.0f;
        if (thisLeft)
        {
            turnFactor = -1.0f;
        }
        Quaternion deltaRot = Quaternion.AngleAxis(turnFactor * TurnRate * Time.deltaTime, transform.forward);
        transform.rotation = deltaRot * transform.rotation;
    }

    public virtual void StartManeuver(Maneuver m)
    {
        _currManeuver = m;
        _currManeuver.Start(this);
    }

    public virtual void StartManeuver(Maneuver m, float forceSpeed)
    {
        _currManeuver = m;
        _currManeuver.Start(this, forceSpeed);
    }

    public bool UseTargetSpeed { get; set; }
    public virtual float TargetSpeed
    {
        get
        {
            return _targetSpeed;
        }
        set
        {
            _targetSpeed = value;
            UseTargetSpeed = true;
            _movementDirection = ShipDirection.Forward;
        }
    }

    public Vector3 ActualVelocity { get; protected set; }
    public float CurrSpeed { get { return _speed; } }

    protected enum ShipDirection { Stopped, Forward, Reverse };

    // Movement stats
    public float MaxSpeed;
    public float Thrust;
    public float Braking;
    public float TurnRate;
    protected float _speed;
    private float _targetSpeed;

    protected ShipDirection _movementDirection = ShipDirection.Stopped;
    protected Vector3 _prevPos;
    protected Quaternion _prevRot;
    protected bool _nextAccelerate;
    protected bool _nextBrake;
    protected bool _nextTurnLeft;
    protected bool _nextTurnRight;

    private Maneuver _currManeuver;
}
