﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class ShipAIController : MonoBehaviour
{
	// Use this for initialization
	protected virtual void Start ()
    {
        _controlledShip = GetComponent<ShipBase>();
        CurrActivity = ShipActivity.ControllingPosition;
        StartCoroutine(AcquireTargetPulse());
        _bug0Alg = GenBug0Algorithm();
    }
	
	// Update is called once per frame
	protected virtual void Update ()
    {
        if (!_controlledShip.ShipControllable)
        {
            return;
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
            if (_controlledShip.Owner.IsEnemy(s.Owner))
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
            foreach (ITurret t in _controlledShip.Turrets)
            {
                t.SetTurretBehavior(TurretBase.TurretMode.Auto);
            }
        }
        else
        {
            foreach (ITurret t in _controlledShip.Turrets)
            {
                t.SetTurretBehavior(TurretBase.TurretMode.Off);
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
        return s is Ship;
    }

    protected virtual Vector3 AttackPosition(ShipBase enemyShip)
    {
        float minRange = _controlledShip.Turrets.Select(x => x.GetMaxRange).Min();
        Vector3 Front = enemyShip.transform.up.normalized;
        //Vector3 Left = enemmyShip.transform.right.normalized * minRange * 0.95f;
        //Vector3 Right = -Left;
        //Vector3 Rear = -Front;
        int k = 0;
        for (int i = 0; i < _numAttackAngles; ++i)
        {
            Vector3 dir = Quaternion.AngleAxis((float)i / _numAttackAngles * 360, Vector3.up) * Front;
            float currWeight;
            if (Vector3.Angle(dir, Front) < 45f)
            {
                currWeight = minRange * minRange * 2;
            }
            else if (Vector3.Angle(dir, Front) > 135f)
            {
                currWeight = minRange * minRange * 4;
            }
            else
            {
                currWeight = minRange * minRange;
            }
            for (int j = 0; j < _numAttackDistances; ++j)
            {
                float dist = minRange * _rangeCoefficient * (j + 1) / _numAttackDistances;
                _attackPositions[k] = enemyShip.transform.position + dir * dist;
                _attackPositionWeights[k] = currWeight;
                ++k;
            }
        }

        int minPos = 0;
        float minScore = (_attackPositions[minPos] - transform.position).sqrMagnitude - _attackPositionWeights[minPos];
        for (int i = 1; i < _attackPositions.Length; ++i)
        {
            float currScore = (_attackPositions[i] - transform.position).sqrMagnitude - _attackPositionWeights[i];
            if (currScore < minScore)
            {
                minPos = i;
                minScore = currScore;
            }
        }
        return _attackPositions[minPos];
    }

    protected virtual void AdvanceToTarget()
    {
        Vector3 vecToTarget;
        if (!GetCurrMovementTarget(out vecToTarget))
        {
            _bug0Alg.HasNavTarget = false;
            return;
        }

        _bug0Alg.NavTarget = _navTarget;

        _bug0Alg.Step();

        if (_bug0Alg.AtDestination)
        {
            _doNavigate = false;
        }
    }

    protected bool GetCurrMovementTarget(out Vector3 vecToTarget)
    {
        if (_doNavigate)
        {
            vecToTarget = _navTarget - transform.position;
            return true;
        }
        else if (_doFollow)
        {
            vecToTarget = _followTarget.transform.position - transform.position;
            Vector3 dirToTarget = vecToTarget.normalized;
            vecToTarget -= dirToTarget * _followDist;
            return true;
        }
        else
        {
            vecToTarget = Vector3.zero;
            return false;
        }
    }

    public void NavigateTo(Vector3 target)
    {
        NavigateTo(target, null);
    }

    protected void NavigateTo(Vector3 target, OrderCompleteDlg onCompleteNavigation)
    {
        _doFollow = false;

        _navTarget = target;
        _doNavigate = true;
        _orderCallback = onCompleteNavigation;
    }

    protected void SetFollowTarget(Transform followTarget, float dist)
    {
        // Cancel navigate order, if there is one:
        _doNavigate = false;
        _orderCallback = null;

        _followTarget = followTarget;
        _followDist = dist;
        _doFollow = true;
    } 

    private IEnumerator AcquireTargetPulse()
    {
        yield return new WaitForSeconds(0.25f);
        while (true)
        {
            if (_controlledShip.ShipControllable && DoSeekTargets)
            {
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
                    Vector3 attackPos = NavigationDest(_targetShip);
                    NavigateTo(attackPos);
                }
                else
                {
                    NavigateWithoutTarget();
                }
            }
            else if (!_controlledShip.ShipActiveInCombat)
            {
                yield break;
            }
            yield return new WaitForSeconds(0.25f);
        }
    }

    protected virtual Vector3 NavigationDest(ShipBase targetShip)
    {
        return AttackPosition(_targetShip);
    }

    protected virtual void NavigateWithoutTarget()
    {
    }

    public bool DoSeekTargets
    {
        get
        {
            switch (CurrActivity)
            {
                case ShipActivity.Idle:
                case ShipActivity.ControllingPosition:
                case ShipActivity.Defending:
                    return true;
                case ShipActivity.Attacking:
                case ShipActivity.Following:
                case ShipActivity.Launching:
                case ShipActivity.NavigatingToRecovery:
                case ShipActivity.StartingRecovery:
                    return false;
                default:
                    break;
            }
            return true;
        }
    }

    protected virtual Bug0 GenBug0Algorithm()
    {
        return new Bug0(_controlledShip, _controlledShip.ShipLength, _controlledShip.ShipWidth, false);
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawLine(transform.position, _navTarget);
        _bug0Alg.DrawDebugLines();
    }

    public delegate void OrderCompleteDlg();

    protected ShipBase _controlledShip;
    protected Bug0 _bug0Alg;

    protected static readonly int _numAttackAngles = 12;
    protected static readonly int _numAttackDistances = 3;
    protected Vector3[] _attackPositions = new Vector3[_numAttackAngles * _numAttackDistances];
    protected float[] _attackPositionWeights = new float[_numAttackAngles * _numAttackDistances];

    protected ShipBase _targetShip = null;
    protected Vector3 _navTarget;
    private Vector3 _targetHeading;
    protected Transform _followTarget = null;
    protected float _followDist;
    protected OrderCompleteDlg _orderCallback = null;
    private static readonly float _angleEps = 0.1f;
    private static readonly float _rangeCoefficient = 0.95f;
    protected bool _doNavigate = false;
    protected bool _doFollow = false;

    public enum ShipActivity
    {
        Idle,
        ControllingPosition,
        Attacking,
        Following,
        Defending,
        Launching,
        NavigatingToRecovery,
        StartingRecovery,
        Recovering
    }

    public ShipActivity CurrActivity { get; protected set; }
}
