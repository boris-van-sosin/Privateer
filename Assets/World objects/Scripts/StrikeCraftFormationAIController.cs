using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class StrikeCraftFormationAIController : MonoBehaviour
{
    // Use this for initialization
    void Start()
    {
        _controlledFormation = GetComponent<StrikeCraftFormation>();
        StartCoroutine(AcquireTargetPulse());
        _currState = FormationState.Idle;
    }

    // Update is called once per frame
    void Update()
    {
        if (_currState == FormationState.Recovering)
        {
            _doNavigate = false;
            foreach (StrikeCraft craft in _controlledFormation.AllStrikeCraft())
            {
                StrikeCraftAIController ctl = craft.GetComponent<StrikeCraftAIController>();
                if (ctl != null)
                {
                    ctl.OrderReturnToHost(_controlledFormation.HostCarrier.GetRecoveryTransforms());
                }
            }
        }
        if (_doNavigate || _doFollow)
        {
            AdvanceToTarget();
        }
    }

    private void AcquireTarget()
    {
        Collider[] colliders = Physics.OverlapSphere(transform.position, 30, ObjectFactory.AllShipsLayerMask);
        ShipBase foundTarget = null;
        foreach (Collider c in colliders)
        {
            ShipBase s = ShipBase.FromCollider(c);
            if (s == null)
            {
                continue;
            }
            else if (!s.ShipActiveInCombat)
            {
                continue;
            }
            if (_controlledFormation.Owner.IsEnemy(s.Owner))
            {
                foundTarget = s;
            }
        }

        if (foundTarget != null)
        {
            if (TargetToFollow(foundTarget))
            {
                _targetShip = foundTarget;
            }
        }
        else
        {
            if (_currState == FormationState.InCombat)
            {
                _currState = FormationState.Idle;
            }
        }
    }

    private void CheckTarget()
    {
        if (_targetShip != null && (_targetShip.ShipActiveInCombat || (_targetShip.transform.position - transform.position).sqrMagnitude > 50 * 50))
        {
            _targetShip = null;
        }
    }

    protected virtual bool TargetToFollow(ShipBase s)
    {
        return s is ShipBase;
    }

    protected virtual Vector3 AttackPosition(ShipBase enemyShip)
    {
        Vector3 vecToTarget = transform.position - enemyShip.transform.position;
        Vector3 unitVecToTarget = vecToTarget.normalized;
        return enemyShip.transform.position - unitVecToTarget * _attackDist;
    }

    protected virtual void AdvanceToTarget()
    {
        Vector3 vecToTarget;
        if (_doNavigate)
        {
            vecToTarget = _navTarget - transform.position;
        }
        else if (_doFollow)
        {
            vecToTarget = _followTarget.transform.position - transform.position;
            Vector3 dirToTarget = vecToTarget.normalized;
            vecToTarget -= dirToTarget * _followDist;
        }
        else
        {
            return;
        }

        Vector3 heading = transform.up;
        Quaternion qToTarget = Quaternion.LookRotation(vecToTarget, transform.forward);
        Quaternion qHeading = Quaternion.LookRotation(heading, transform.forward);
        float angleToTarget = Vector3.SignedAngle(heading, vecToTarget, Vector3.up);
        bool atRequiredHeaing = false;
        if (angleToTarget > _angleEps)
        {
            _controlledFormation.ApplyTurning(false);
            //Debug.Log("Strike craft turning right");
        }
        else if (angleToTarget < -_angleEps)
        {
            _controlledFormation.ApplyTurning(true);
            //Debug.Log("Strike craft turning left");
        }
        else
        {
            atRequiredHeaing = true;
            //Debug.Log("Strike craft going straight");
        }

        if (_targetShip != null && vecToTarget.sqrMagnitude <= (_attackDist * _attackDist))
        {
            _currState = FormationState.InCombat;
        }
        else if (vecToTarget.sqrMagnitude <= (_distEps * _distEps))
        {
            _controlledFormation.ApplyBraking();
            if (_controlledFormation.ActualVelocity.sqrMagnitude < (_distEps * _distEps) && atRequiredHeaing)
            {
                if (_doNavigate)
                {
                    _doNavigate = false;
                    _orderCallback?.Invoke();
                }
            }
        }
        else
        {
            SetSpeed();
        }
    }

    private void NavigateTo(Vector3 target)
    {
        NavigateTo(target, null);
    }

    private void NavigateTo(Vector3 target, ShipAIController.OrderCompleteDlg onCompleteNavigation)
    {
        _followTarget = null;
        _doFollow = false;

        _navTarget = target;
        Debug.DrawLine(transform.position, _navTarget, Color.red, 0.5f);
        _doNavigate = true;
        _orderCallback = onCompleteNavigation;
    }

    private void SetFollowTarget(Transform followTarget, float dist)
    {
        _doNavigate = false;

        _followTarget = followTarget;
        _followDist = dist;
        _doFollow = true;
    }

    private Vector3? BypassObstacle(Vector3 direction)
    {
        Vector3 directionNormalized = direction.normalized;
        Vector3 rightVec = Quaternion.AngleAxis(90, Vector3.up) * directionNormalized;
        float projectFactor = _controlledFormation.Diameter * 4;
        Vector3 projectedPath = directionNormalized * projectFactor;
        RaycastHit[] hits = Physics.SphereCastAll(transform.position, _controlledFormation.Diameter * 3.0f, directionNormalized, projectFactor, ObjectFactory.AllTargetableLayerMask);
        bool obstruction = false;
        List<float> dotToCorners = new List<float>(4 * hits.Length);
        float dotMin = -1;
        foreach (RaycastHit h in hits)
        {
            if (h.collider.gameObject == this.gameObject)
            {
                continue;
            }
            Ship other = h.collider.GetComponent<Ship>();
            if (other != null)
            {
                obstruction = true;
                Vector3 obstructionLocation = h.point;
                obstructionLocation.y = 0;
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
                    Debug.DrawLine(other.transform.position, otherShipCorners[i], Color.cyan, 0.25f);
                    dotToCorners.Add(Vector3.Dot(otherShipCorners[i] - transform.position, rightVec));
                    if (dotMin < 0 || Mathf.Abs(dotToCorners[i]) < dotMin)
                    {
                        dotMin = Mathf.Abs(dotToCorners[i]);
                    }
                }
            }
        }
        if (obstruction)
        {
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
                return (transform.position - (rightVec * dotMin * 2f));
            }
            else if (maxRight == -1)
            {
                return (transform.position + (rightVec * dotMin * 2f));
            }
            else if (-dotToCorners[maxLeft] >= dotToCorners[maxRight])
            {
                return (transform.position + (rightVec * dotToCorners[maxRight] * 2f));
            }
            else if (-dotToCorners[maxLeft] < dotToCorners[maxRight])
            {
                return (transform.position + (rightVec * dotToCorners[maxLeft] * 2f));
            }
        }
        return null;
    }

    private IEnumerator AcquireTargetPulse()
    {
        yield return new WaitForSeconds(0.25f);
        while (true)
        {
            if (_controlledFormation.AllStrikeCraft().All(s => s.IsOutOfAmmo()))
            {
                CarrierBehavior c = _controlledFormation.HostCarrier;
                if (c != null)
                {
                    OrderReturnToHost(c.transform);
                    yield break;
                }
            }
            if (_targetShip == null)
            {
                AcquireTarget();
            }
            if (_targetShip != null)
            {
                if (_targetShip.ShipDisabled)
                {
                    _targetShip = null;
                    continue;
                }
                Vector3 attackPos = AttackPosition(_targetShip);
                Vector3? bypassVec = BypassObstacle(attackPos);
                if (bypassVec == null)
                {
                    NavigateTo(attackPos);
                }
                else
                {
                    NavigateTo(bypassVec.Value);
                }
            }
            yield return new WaitForSeconds(0.25f);
        }
    }

    private void SetSpeed()
    {
        if (_controlledFormation.AllInFormation())
        {
            _controlledFormation.TargetSpeed = _controlledFormation.MaxSpeed;
            foreach (StrikeCraft s in _controlledFormation.AllStrikeCraft())
            {
                s.TargetSpeed = s.MaxSpeed;
            }
        }
        else
        {
            _controlledFormation.TargetSpeed = _controlledFormation.MaxSpeed * _controlledFormation.MaintainFormationSpeedCoefficient;
            foreach (ValueTuple<StrikeCraft, bool> s in _controlledFormation.InFormationStatus())
            {
                if (s.Item2)
                {
                    s.Item1.TargetSpeed = s.Item1.MaxSpeed * _controlledFormation.MaintainFormationSpeedCoefficient;
                }
                else if (s.Item1.AheadOfPositionInFormation())
                {
                    s.Item1.TargetSpeed = s.Item1.MaxSpeed * _controlledFormation.MaintainFormationSpeedCoefficient * _controlledFormation.MaintainFormationSpeedCoefficient;
                }
                else
                {
                    s.Item1.TargetSpeed = s.Item1.MaxSpeed;
                }
            }

        }
    }

    private enum FormationState { Idle, Launching, Recovering, Defending, Moving, InCombat };

    public bool DoMaintainFormation()
    {
        return _currState != FormationState.InCombat;
    }

    public void OrderReturnToHost()
    {
        CarrierBehavior c = _controlledFormation.HostCarrier;
        if (c != null)
        {
            OrderReturnToHost(c.transform);
        }
    }

    public void OrderReturnToHost(Transform recoveryPosition)
    {
        _currState = FormationState.Recovering;
        foreach (StrikeCraft craft in _controlledFormation.AllStrikeCraft())
        {
            StrikeCraftAIController ctl = craft.GetComponent<StrikeCraftAIController>();
            if (ctl != null)
            {
                ctl.OrderStartNavigatenToHost();
            }
        }
        Vector3 vecToRecovery = recoveryPosition.position - transform.position;
        float m = vecToRecovery.magnitude;
        if (m < _recoveryTargetDist)
        {
            bool isFacingHost = Vector3.Dot(vecToRecovery, transform.up) >= 0f;
            if (!isFacingHost)
            {
                Vector3 dirToRecovery = vecToRecovery / m;
                Vector3 halfTurn = Quaternion.AngleAxis(90, Vector3.up) * dirToRecovery;
                float radius = 1f;
                NavigateTo(transform.position + radius * halfTurn);
            }
        }
        else
        {
            SetFollowTarget(recoveryPosition, _recoveryTargetDist);
        }
    }

    protected StrikeCraftFormation _controlledFormation;

    protected ShipBase _targetShip = null;
    protected Vector3 _navTarget;
    private Vector3 _targetHeading;
    protected Transform _followTarget = null;
    protected float _followDist;
    protected ShipAIController.OrderCompleteDlg _orderCallback = null;
    private static readonly float _angleEps = 0.1f;
    private static readonly float _distEps = 0.01f;
    private static readonly float _attackDist = 2.0f;
    protected bool _doNavigate = false;
    protected bool _doFollow = false;
    private static readonly float _recoveryTargetDist = 2.5f;
    private FormationState _currState;
}
