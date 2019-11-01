using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class NavigationGuide : MonoBehaviour
{
    void Awake()
    {
        _navAgent = GetComponent<NavMeshAgent>();
    }

    public void Attach(MovementBase mb)
    {
        // Copy values from the object we need to guide:
        _attachedObject = mb;
        _navAgent.speed = _baseSpeed = mb.MaxSpeed;
        _navAgent.acceleration = 10f * mb.Thrust;
        _navAgent.angularSpeed = mb.TurnRate;
        _navAgent.stoppingDistance = StoppingDistance = mb.MaxSpeed / (2f * mb.Braking);
        _navAgent.radius = mb.ObjectSize / 2f;
        SetPositionToAttached();
    }

    public bool SetDestination(Vector3 dest)
    {
        SetPositionToAttached();
        return _navAgent.SetDestination(dest);
    }

    public void SetTargetSpeed(float speed)
    {
        _baseSpeed = speed;
    }

    public void Halt()
    {
        _navAgent.isStopped = true;
    }

    public bool SetPosition(Vector3 pos)
    {
        _navAgent.speed = _baseSpeed;
        return _navAgent.Warp(pos);
    }

    public bool SetPositionToAttached()
    {
        return SetPosition(_attachedObject.transform.position);
    }

    void Update()
    {
        if (ManualControl)
        {
            SetPositionToAttached();
        }
        else if (_navAgent.isOnNavMesh)
        {
            Vector3 vecToGuide = transform.position - _attachedObject.transform.position;

            // If the agent has reached its destination, and the object we
            // are guiding has stopped at the destination, then do nothing:
            if (NavAgentAtDest() &&
                vecToGuide.sqrMagnitude < StoppingDistance * StoppingDistance &&
                _attachedObject.ActualVelocity.sqrMagnitude <= Mathf.Epsilon)
            {
                return;
            }

            // Determine if we need to apply turning:
            TurnToHeading(vecToGuide);

            if (NavAgentAtDest())
            {
                // If the agent has reached its destination, and the object we
                // are guiding is close enough, then start braking:
                if (vecToGuide.sqrMagnitude <= FollowDistance * FollowDistance)
                {
                    _attachedObject.ApplyBraking();
                }
            }
            else if (vecToGuide.sqrMagnitude > FollowDistance * FollowDistance)
            {
                // If the object we are guiding is too far from the agent, slow the agent down:
                _navAgent.speed =
                    _baseSpeed *
                        (1f / (1f + SlowdownCoefficient * (vecToGuide.sqrMagnitude - FollowDistance * FollowDistance)));
                // Apply acceleration or braking to the object we are guiding:
                if (ObjectAheadOfGuide(vecToGuide))
                {
                    _attachedObject.MoveForward();
                }
                else if (ObjectBehindGuide(vecToGuide))
                {
                    _attachedObject.ApplyBraking();
                }
            }
            else
            {
                // If the object we are guiding is following the agent within the required
                // distance, keep going at the normal speed:
                _navAgent.speed = _baseSpeed;
            }
        }
    }

    private void TurnToHeading(Vector3 targetHeading)
    {
        Vector3 heading = _attachedObject.transform.forward;
        float angleToTarget = Vector3.Angle(heading, targetHeading);
        if (angleToTarget > _angleEps)
        {
            _attachedObject.ApplyTurningToward(targetHeading);
        }
    }

    private bool ObjectAheadOfGuide(Vector3 vecToHelper)
    {
        return Vector3.Dot(vecToHelper, _attachedObject.transform.forward) > 0;
    }

    private bool ObjectBehindGuide(Vector3 vecToHelper)
    {
        return Vector3.Dot(vecToHelper, _attachedObject.transform.forward) < 0;
    }

    private bool NavAgentAtDest()
    {
        return
            (!_navAgent.pathPending) &&
            _navAgent.remainingDistance <= _navAgent.stoppingDistance &&
            (!_navAgent.hasPath || _navAgent.velocity.sqrMagnitude == 0f);
    }

    public float FollowDistance;
    public float SlowdownCoefficient;
    public float StoppingDistance;

    public bool ManualControl { get; set; }

    private NavMeshAgent _navAgent;
    private MovementBase _attachedObject;
    private float _baseSpeed;
    private static readonly float _angleEps = 2f;
}

