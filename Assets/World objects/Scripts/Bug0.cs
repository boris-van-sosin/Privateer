using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Bug0
{
    public Bug0(MovementBase controlledEntity, float entityLegnth, float entityWidth)
    {
        _controlledShip = controlledEntity;
        _entityLength = entityLegnth;
        _entityWidth = entityWidth;
        _wallFollowMinRange = _entityLength * _wallFollowMinRangeFactor;
        _wallFollowMaxRange = entityLegnth * _wallFollowMaxRangeFactor;
        _wallFollowRangeDiff = entityLegnth * _wallFollowRangeDiffFactor;
        _accelerateOnTurn = false;
        AtDestination = false;
    }

    public void Step()
    {
        if (!HasNavTarget)
        {
            return;
        }
        Vector3 vecToTarget = NavTarget - _controlledShip.transform.position;

        switch (_bug0State)
        {
            case Bug0State.MovingToTarget:
                {
                    int turnRes = TurnToHeading(vecToTarget);
                    Vector3 predictedHeading;
                    if (turnRes == 0)
                    {
                        _bug0State = Bug0State.MovingToTarget;
                        predictedHeading = _controlledShip.transform.up;
                    }
                    else
                    {
                        _controlledShip.ApplyTurning(turnRes == 1);
                        predictedHeading = PredictHeading(_controlledShip.transform.up, -turnRes, _cacheItemValidTime);
                    }
                    _controlledShip.MoveForward();
                    RaycastHit? hit = CheckForObstructions(RaycastOrigin.Center, predictedHeading, true, RaycastCacheKey.ForwardPredicted);
                    if (hit.HasValue)
                    {
                        int bypassDir = ChooseBypassDirection(hit.Value);
                        if (bypassDir == 1)
                        {
                            _bug0State = Bug0State.TurningToBypassLeft;
                        }
                        else if (bypassDir == -1)
                        {
                            _bug0State = Bug0State.TurningToBypassRight;
                        }
                    }
                }
                break;
            case Bug0State.TurningToTarget:
                {
                    int turnRes = TurnToHeading(vecToTarget);
                    if (turnRes == 0)
                    {
                        _bug0State = Bug0State.MovingToTarget;
                    }
                    else
                    {
                        _controlledShip.ApplyTurning(turnRes == 1);
                    }
                }
                break;
            case Bug0State.TurningToBypassRight:
                {
                    RaycastHit? hit = CheckForObstructions(RaycastOrigin.Center, _controlledShip.transform.up, true, RaycastCacheKey.Forward);
                    if (hit.HasValue)
                    {
                        _controlledShip.ApplyTurning(true);
                        if (_accelerateOnTurn)
                        {
                            _controlledShip.MoveForward();
                        }
                        Debug.DrawLine(_controlledShip.transform.position, _controlledShip.transform.position - _entityLength * 1f * _controlledShip.transform.up, _gizmoColor2r, 0.1f);
                    }
                    else
                    {
                        _bug0State = Bug0State.BypassingRight;
                    }
                }
                break;
            case Bug0State.TurningToBypassLeft:
                {
                    RaycastHit? hit = CheckForObstructions(RaycastOrigin.Center, _controlledShip.transform.up, true, RaycastCacheKey.Forward);
                    if (hit.HasValue)
                    {
                        _controlledShip.ApplyTurning(false);
                        if (_accelerateOnTurn)
                        {
                            _controlledShip.MoveForward();
                        }
                        Debug.DrawLine(_controlledShip.transform.position, _controlledShip.transform.position - _entityLength * 1f * _controlledShip.transform.up, _gizmoColor2l, 0.1f);
                    }
                    else
                    {
                        _bug0State = Bug0State.BypassingLeft;
                    }
                }
                break;
            case Bug0State.BypassingRight:
                {
                    // Apply simple wall-following:
                    // First check if forward direction is obstructed:
                    RaycastHit? hit = CheckForObstructions(RaycastOrigin.Center, _controlledShip.transform.up, true, RaycastCacheKey.Forward);
                    if (hit.HasValue)
                    {
                        _bug0State = Bug0State.TurningToBypassRight;
                        _accelerateOnTurn = false;
                        Debug.DrawLine(_controlledShip.transform.position, _controlledShip.transform.position + _entityLength * _forwardCastDistFactor * _controlledShip.transform.up, _gizmoColor2hit, 0.1f);
                    }

                    if (_bug0State == Bug0State.BypassingRight)
                    {
                        // Maintain distance from obstacle:
                        RaycastHit? hitFore = CheckForObstructions(RaycastOrigin.Fore, -_controlledShip.transform.right, false, RaycastCacheKey.RightFore);
                        RaycastHit? hitAft = CheckForObstructions(RaycastOrigin.Aft, -_controlledShip.transform.right, false, RaycastCacheKey.RightAft);
                        if ((!hitFore.HasValue) ||
                            (hitFore.HasValue && hitAft.HasValue && (hitFore.Value.distance > _wallFollowMaxRange || hitFore.Value.distance > hitFore.Value.distance + _wallFollowRangeDiff)))
                        {
                            _controlledShip.ApplyTurning(false);
                        }
                        else if (hitFore.HasValue && hitAft.HasValue &&
                                 (hitFore.Value.distance < _wallFollowMinRange || hitFore.Value.distance < hitFore.Value.distance - _wallFollowRangeDiff))
                        {
                            _controlledShip.ApplyTurning(true);
                        }
                        _controlledShip.MoveForward();
                        Debug.DrawLine(_controlledShip.transform.position, _controlledShip.transform.position - _entityLength * 1f * _controlledShip.transform.up, _gizmoColor2r, 0.1f);
                        Debug.DrawLine(_controlledShip.transform.position, _controlledShip.transform.position + _entityLength * _forwardCastDistFactor * _controlledShip.transform.up, _gizmoColor2noHit, 0.1f);
                        Debug.DrawLine(_controlledShip.transform.position, _controlledShip.transform.position - _wallFollowMaxRange * _controlledShip.transform.right, hitFore.HasValue ? _gizmoColor2hit : _gizmoColor2noHit, 0.1f);
                        Debug.DrawLine(_controlledShip.transform.position, _controlledShip.transform.position - _wallFollowMaxRange * _controlledShip.transform.right, hitAft.HasValue ? _gizmoColor2hit : _gizmoColor2noHit, 0.1f);


                        // Check if can continue to target:
                        Vector3 vecToTargetNormalized = vecToTarget.normalized;
                        RaycastHit? toTarget = CheckForObstructions(RaycastOrigin.Center, vecToTargetNormalized, true, RaycastCacheKey.ToTarget);
                        if (!toTarget.HasValue)
                        {
                            _bug0State = Bug0State.TurningToTarget;
                            _accelerateOnTurn = true;
                        }
                    }
                }
                break;
            case Bug0State.BypassingLeft:
                {
                    // Apply simple wall-following:
                    // Apply simple wall-following:
                    // First check if forward direction is obstructed:
                    RaycastHit? hit = CheckForObstructions(RaycastOrigin.Center, _controlledShip.transform.up, true, RaycastCacheKey.Forward);
                    if (hit.HasValue)
                    {
                        _bug0State = Bug0State.TurningToBypassLeft;
                        _accelerateOnTurn = false;
                        Debug.DrawLine(_controlledShip.transform.position, _controlledShip.transform.position + _entityLength * _forwardCastDistFactor * _controlledShip.transform.up, _gizmoColor2hit, 0.1f);
                    }

                    if (_bug0State == Bug0State.BypassingLeft)
                    {
                        // Maintain distance from obstacle:
                        RaycastHit? hitFore = CheckForObstructions(RaycastOrigin.Fore, _controlledShip.transform.right, false, RaycastCacheKey.LeftFore);
                        RaycastHit? hitAft = CheckForObstructions(RaycastOrigin.Aft, _controlledShip.transform.right, false, RaycastCacheKey.LeftAft);
                        if ((!hitFore.HasValue) ||
                            (hitFore.HasValue && hitAft.HasValue && (hitFore.Value.distance > _wallFollowMaxRange || hitFore.Value.distance > hitFore.Value.distance + _wallFollowRangeDiff)))
                        {
                            _controlledShip.ApplyTurning(true);
                        }
                        else if (hitFore.HasValue && hitAft.HasValue &&
                                 (hitFore.Value.distance < _wallFollowMinRange || hitFore.Value.distance < hitFore.Value.distance - _wallFollowRangeDiff))
                        {
                            _controlledShip.ApplyTurning(false);
                        }
                        _controlledShip.MoveForward();
                        Debug.DrawLine(_controlledShip.transform.position, _controlledShip.transform.position - _entityLength * 1f * _controlledShip.transform.up, _gizmoColor2l, 0.1f);
                        Debug.DrawLine(_controlledShip.transform.position, _controlledShip.transform.position + _entityLength * _forwardCastDistFactor * _controlledShip.transform.up, _gizmoColor2noHit, 0.1f);
                        Debug.DrawLine(_controlledShip.transform.position, _controlledShip.transform.position + _wallFollowMaxRange * _controlledShip.transform.right, hitFore.HasValue ? _gizmoColor2hit : _gizmoColor2noHit, 0.1f);
                        Debug.DrawLine(_controlledShip.transform.position, _controlledShip.transform.position + _wallFollowMaxRange * _controlledShip.transform.right, hitAft.HasValue ? _gizmoColor2hit : _gizmoColor2noHit, 0.1f);

                        // Check if can continue to target:
                        Vector3 vecToTargetNormalized = vecToTarget.normalized;
                        RaycastHit? toTarget = CheckForObstructions(RaycastOrigin.Center, vecToTargetNormalized, true, RaycastCacheKey.ToTarget);
                        if (!toTarget.HasValue)
                        {
                            _bug0State = Bug0State.TurningToTarget;
                            _accelerateOnTurn = true;
                        }
                    }
                }
                break;
            default:
                break;
        }

        float stoppingDist = StoppingDistance(_controlledShip.CurrSpeed, _controlledShip.Braking) + 20f * GlobalDistances.ShipAIDistEps;
        if (vecToTarget.sqrMagnitude <= stoppingDist * stoppingDist)
        {
            _controlledShip.ApplyBraking();
            if (_controlledShip.ActualVelocity.sqrMagnitude == 0f)
            {
                _bug0State = Bug0State.Stopped;
                _accelerateOnTurn = false;
                AtDestination = true;
            }
        }
    }

    private RaycastHit? CheckForObstructions(RaycastOrigin origin, Vector3 direction, bool wide, RaycastCacheKey key)
    {
        if (_raycastCache.TryGetValue(key, out RaycastCacheItem fromCache))
        {
            if (fromCache.Timestamp > Time.time - _cacheItemValidTime)
            {
                if (fromCache.HitExists)
                    return fromCache.Hit;
                else
                    return null;
            }
        }
        float projectFactor;
        Vector3 originPt;
        switch (origin)
        {
            case RaycastOrigin.Center:
                originPt = _controlledShip.transform.position;
                projectFactor = _entityLength * _forwardCastDistFactor;
                break;
            case RaycastOrigin.Fore:
                originPt = _controlledShip.transform.position + 0.25f * _entityLength * _controlledShip.transform.up;
                projectFactor = _wallFollowMaxRange * 1.1f;
                break;
            case RaycastOrigin.Aft:
                originPt = _controlledShip.transform.position - 0.25f * _entityLength * _controlledShip.transform.up;
                projectFactor = _wallFollowMaxRange * 1.1f;
                break;
            default:
                originPt = Vector3.zero;
                projectFactor = 0f;
                break;
        }
        RaycastHit[] hits = CheckForObstructionsInner(originPt, direction, projectFactor, wide);
        foreach (RaycastHit h in hits)
        {
            if (Vector3.Dot(h.point - originPt, direction) < 0f)
            {
                continue;
            }
            ShipBase other = ShipBase.FromCollider(h.collider);
            if (other != null && other != _controlledShip && other is Ship)
            {
                _raycastCache[key] = new RaycastCacheItem() { Timestamp = Time.time, Hit = h, HitExists = true };
                Debug.DrawLine(originPt, h.point, wide ? _gizmoColor0 : _gizmoColor1, 0.1f);
                return h;
            }
        }
        _raycastCache[key] = new RaycastCacheItem() { Timestamp = Time.time, HitExists = false };
        return null;
    }

    private RaycastHit[] CheckForObstructionsInner(Vector3 origin, Vector3 direction, float dist, bool wide)
    {
        if (wide)
            return Physics.SphereCastAll(origin, _entityWidth * _forwardCastWidthFactor, direction, dist, ObjectFactory.NavBoxesLayerMask);
        else
            return Physics.RaycastAll(origin, direction, dist, ObjectFactory.NavBoxesLayerMask);
    }

    private int TurnToHeading(Vector3 targetHeading)
    {
        Vector3 heading = _controlledShip.transform.up;
        float angleToTarget = Vector3.SignedAngle(heading, targetHeading, Vector3.up);
        if (angleToTarget < -_angleEps)
        {
            return 1;
        }
        else if (angleToTarget > _angleEps)
        {
            return -1;
        }
        else
        {
            return 0;
        }
    }

    private Vector3 PredictHeading(Vector3 heading, int dirFactor, float delta)
    {
        Quaternion deltaRot = Quaternion.AngleAxis(_controlledShip.TurnRate * delta * dirFactor, _controlledShip.transform.forward);
        return deltaRot * heading;
    }

    private int ChooseBypassDirection(RaycastHit h)
    {
        List<float> dotToCorners = new List<float>(4);
        Vector3 rightVec = Quaternion.AngleAxis(90, Vector3.up) * _controlledShip.transform.up;
        float dotMin = -1;
        Vector3 obstructionLocation = h.point;
        obstructionLocation.y = 0;
        ShipBase other = ShipBase.FromCollider(h.collider);
        float obstructionLength = other.ShipLength * 1.1f;
        float obstructionWidth = other.ShipLength * 1.1f;
        Vector3[] otherShipCorners = new Vector3[]
        {
            other.transform.position + (other.transform.up * obstructionLength) + (other.transform.right * obstructionWidth),
            other.transform.position + (other.transform.up * obstructionLength) - (other.transform.right * obstructionWidth),
            other.transform.position - (other.transform.up * obstructionLength) + (other.transform.right * obstructionWidth),
            other.transform.position - (other.transform.up * obstructionLength) - (other.transform.right * obstructionWidth),
        };
        for (int i = 0; i < otherShipCorners.Length; ++i)
        {
            //Debug.DrawLine(other.transform.position, otherShipCorners[i], Color.cyan, 0.25f);
            dotToCorners.Add(Vector3.Dot(otherShipCorners[i] - _controlledShip.transform.position, rightVec));
            if (dotMin < 0 || Mathf.Abs(dotToCorners[i]) < dotMin)
            {
                dotMin = Mathf.Abs(dotToCorners[i]);
            }
        }

        int maxRight = -1;
        int maxLeft = -1;
        for (int i = 0; i < dotToCorners.Count; ++i)
        {
            if (dotToCorners[i] > 0 && (maxRight == -1 || dotToCorners[i] > dotToCorners[maxRight]))
            {
                maxRight = i;
            }
            else if (dotToCorners[i] < 0 && (maxLeft == -1 || dotToCorners[i] < dotToCorners[maxLeft]))
            {
                maxLeft = i;
            }
        }

        if (maxLeft == -1)
        {
            return -1;
        }
        else if (maxRight == -1)
        {
            return 1;
        }
        else if (-dotToCorners[maxLeft] >= dotToCorners[maxRight])
        {
            return -1;
        }
        else if (-dotToCorners[maxLeft] < dotToCorners[maxRight])
        {
            return 1;
        }
        return 0;
    }

    private float StoppingDistance(float speed, float deceleration)
    {
        return speed * speed / (2f * deceleration);
    }

    public enum Bug0State { Stopped, MovingToTarget, TurningToTarget, TurningToBypassRight, TurningToBypassLeft, BypassingRight, BypassingLeft };

    private enum RaycastOrigin { Center, Fore, Aft };
    private enum RaycastCacheKey { Forward, ForwardPredicted, ToTarget, RightFore, RightAft, LeftFore,LeftAft };
    private struct RaycastCacheItem
    {
        public RaycastHit Hit;
        public bool HitExists;
        public float Timestamp;
    }
    private static readonly float _cacheItemValidTime = 0.5f;

    private static readonly float _angleEps = 0.1f;
    private static readonly float _wallFollowMinRangeFactor = 0.5f;
    private static readonly float _wallFollowMaxRangeFactor = 1f;
    private static readonly float _wallFollowRangeDiffFactor = 0.2f;
    private static readonly float _forwardCastWidthFactor = 1f;
    private static readonly float _forwardCastDistFactor = 3f;

    public Vector3 NavTarget
    {
        get
        {
            return _navTarget;
        }
        set
        {
            _navTarget = value;
            HasNavTarget = true;
            if (_bug0State == Bug0State.Stopped)
            {
                _bug0State = Bug0State.TurningToTarget;
            }
            AtDestination = false;
        }
    }
    public bool HasNavTarget { get; set; }
    public bool AtDestination { get; private set; }

    private Vector3 _navTarget;

    private Bug0State _bug0State;
    private bool _accelerateOnTurn;
    private Dictionary<RaycastCacheKey, RaycastCacheItem> _raycastCache = new Dictionary<RaycastCacheKey, RaycastCacheItem>();

    private readonly MovementBase _controlledShip;
    private readonly float _entityLength, _entityWidth;
    private readonly float _wallFollowMinRange, _wallFollowMaxRange, _wallFollowRangeDiff;
    private readonly Color _gizmoColor0 = new Color(0f, 0f, 0.6f);
    private readonly Color _gizmoColor1 = new Color(0.3f, 0f, 0.3f);
    private readonly Color _gizmoColor2hit = new Color(1f, 1f, 0.2f);
    private readonly Color _gizmoColor2noHit = new Color(0.2f, 1f, 1f);
    private readonly Color _gizmoColor2l = new Color(0.7f, 0.7f, 0f);
    private readonly Color _gizmoColor2r = new Color(0f, 0.7f, 0.7f);
}
