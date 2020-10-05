using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using UnityEngine.AI;

[Obsolete("Individal AI controllers are obsolete. Use ShipsAIController.")]
public class StrikeCraftFormationAIController : MonoBehaviour
{
    // Use this for initialization
    void Start()
    {
        _controlledFormation = GetComponent<StrikeCraftFormation>();
        _navGuide = ObjectFactory.CreateNavGuide(transform.position, transform.forward);
        _navGuide.Attach(_controlledFormation);
        _navGuide.ManualControl = false;
        _innerNavAgent = _navGuide.GetComponent<NavMeshAgent>();
        StartCoroutine(AcquireTargetPulse());
        _currState = StrikeCraftFormationState.Idle;
    }

    // Update is called once per frame
    void Update()
    {
        if (_currState == StrikeCraftFormationState.ReturningToHost)
        {
            _currState = StrikeCraftFormationState.Recovering;
            _doNavigate = false;
            _controlledFormation.HostCarrier.RecoveryTryStart(_controlledFormation);
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
        int numHits = Physics.OverlapSphereNonAlloc(transform.position, GlobalDistances.StrikeCraftAIFormationAggroDist, _collidersCache, ObjectFactory.AllShipsLayerMask);
        ShipBase foundTarget = null;
        for (int i = 0; i < numHits; ++i)
        {
            ShipBase s = ShipBase.FromCollider(_collidersCache[i]);
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
            if (_currState == StrikeCraftFormationState.InCombat)
            {
                _currState = StrikeCraftFormationState.Idle;
            }
        }
    }

    private void CheckTarget()
    {
        if (_targetShip != null && (_targetShip.ShipActiveInCombat || (_targetShip.transform.position - transform.position).sqrMagnitude > (GlobalDistances.StrikeCraftAIFormationAggroDist * GlobalDistances.StrikeCraftAIFormationAggroDist)))
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
        return enemyShip.transform.position - unitVecToTarget * GlobalDistances.StrikeCraftAIAttackDist;
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
            Vector3 followPos = GetFollowPosition(_followTarget);
            vecToTarget = followPos - transform.position;
            if (Time.time >= _nextUpdateFollowTime)
            {
                _navGuide.SetDestination(followPos);
                _nextUpdateFollowTime = Time.time + UnityEngine.Random.Range(1f, 2f);
            }
            else
            {
                if (_innerNavAgent.isOnNavMesh && _innerNavAgent.remainingDistance < 0.1f)
                {
                    _followPosIdx = 1 - _followPosIdx;
                    _nextUpdateFollowTime = Time.time;
                }
            }
        }
        else
        {
            return;
        }

        if (_targetShip != null && vecToTarget.sqrMagnitude <= (GlobalDistances.StrikeCraftAIAttackDist * GlobalDistances.StrikeCraftAIAttackDist))
        {
            _currState = StrikeCraftFormationState.InCombat;
        }
        else if (vecToTarget.sqrMagnitude <= (GlobalDistances.StrikeCraftAIDistEps * GlobalDistances.StrikeCraftAIDistEps))
        {
            _controlledFormation.ApplyBraking();
            if (_controlledFormation.ActualVelocity.sqrMagnitude < (GlobalDistances.StrikeCraftAIDistEps * GlobalDistances.StrikeCraftAIDistEps))
            {
                if (_doNavigate)
                {
                    _doNavigate = false;
                    _orderCallback?.Invoke();
                    Debug.LogWarningFormat("Strike craft formation stopped at destination. This is highly unlikely.");
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

    private void NavigateTo(Vector3 target, Action onCompleteNavigation)
    {
        _followTarget = null;
        _doFollow = false;

        _navGuide.SetDestination(target);
        _navTarget = target;
        _doNavigate = true;
        _orderCallback = onCompleteNavigation;
    }

    private void SetFollowTarget(Transform followTarget, float dist)
    {
        _doNavigate = false;

        _followTarget = followTarget;
        _followDist = dist;
        _doFollow = true;
        _nextUpdateFollowTime = Time.time;
        _followPosIdx = 0;
    }
    
    private IEnumerator AcquireTargetPulse()
    {
        yield return _targetAcquirePulseDelay;
        while (true)
        {
            if (_controlledFormation.DestroyOnEmpty && _controlledFormation.AllOutOfAmmo())
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
                NavigateTo(attackPos);
            }
            yield return _targetAcquirePulseDelay;
        }
    }

    private void SetSpeed()
    {
        if (_controlledFormation.AllInFormation())
        {
            //_controlledFormation.TargetSpeed = _controlledFormation.MaxSpeed;
            _navGuide.SetTargetSpeed(_controlledFormation.MaxSpeed);
            foreach (StrikeCraft s in _controlledFormation.AllStrikeCraft())
            {
                s.TargetSpeed = s.MaxSpeed;
            }
        }
        else
        {
            //_controlledFormation.TargetSpeed = _controlledFormation.MaxSpeed * _controlledFormation.MaintainFormationSpeedCoefficient;
            _navGuide.SetTargetSpeed(_controlledFormation.MaxSpeed * _controlledFormation.MaintainFormationSpeedCoefficient);
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

    public bool DoMaintainFormation()
    {
        return _currState != StrikeCraftFormationState.InCombat;
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
        //
        _currState = StrikeCraftFormationState.ReturningToHost;
        foreach (StrikeCraft craft in _controlledFormation.AllStrikeCraft())
        {
            StrikeCraftAIController ctl = craft.GetComponent<StrikeCraftAIController>();
            if (ctl != null)
            {
                ctl.OrderStartNavigatenToHost();
            }
        }
        //
        Vector3 vecToRecovery = recoveryPosition.position - transform.position;
        float m = vecToRecovery.magnitude;
        if (m < GlobalDistances.StrikeCraftAIFormationRecoveryTargetDist)
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
            SetFollowTarget(recoveryPosition, GlobalDistances.StrikeCraftAIFormationRecoveryTargetDist);
        }
    }

    public void OrderEscort(ShipBase s)
    {
        SetFollowTarget(s.transform, GlobalDistances.StrikeCraftAIFormationEscortDist);
    }

    private Vector3 GetFollowPosition(Transform followTarget)
    {
        if (_followPosIdx == 0)
        {
            return followTarget.position + (followTarget.right * _followDist);
        }
        else
        {
            return followTarget.position - (followTarget.right * _followDist);
        }
    }

    void OnDestroy()
    {
        if (_navGuide != null)
        {
            Destroy(_navGuide.gameObject);
        }
    }

    protected StrikeCraftFormation _controlledFormation;

    protected ShipBase _targetShip = null;
    protected Vector3 _navTarget;
    private Vector3 _targetHeading;
    protected Transform _followTarget = null;
    protected float _followDist;
    protected float _nextUpdateFollowTime;
    protected int _followPosIdx;
    protected Action _orderCallback = null;
    protected bool _doNavigate = false;
    protected bool _doFollow = false;
    protected NavigationGuide _navGuide;
    protected NavMeshAgent _innerNavAgent;
    private StrikeCraftFormationState _currState;

    // Visual debug:
    private Vector3[] _dbgObstacleCorners = new Vector3[5];
    private void OnDrawGizmos()
    {
    }

    // Ugly optimization:
    private Collider[] _collidersCache = new Collider[1024];

    private static readonly WaitForSeconds _targetAcquirePulseDelay = new WaitForSeconds(0.25f);
}

public enum StrikeCraftFormationState { Idle, Launching, ReturningToHost, Recovering, Defending, Moving, InCombat };
