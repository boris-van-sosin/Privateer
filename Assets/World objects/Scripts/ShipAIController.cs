using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using UnityEngine.AI;

public class ShipAIController : MonoBehaviour
{
	// Use this for initialization
	protected virtual void Start ()
    {
        _controlledShip = GetComponent<ShipBase>();
        CurrActivity = ShipActivity.ControllingPosition;
        StartCoroutine(AcquireTargetPulse());
        _bug0Alg = GenBug0Algorithm();
        TargetShip = null;

        _navAgent = GetComponent<NavMeshAgent>();
        // temporary default:
        _currAttackBehavior = ShipAttackPattern.Aggressive;
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
        Collider[] colliders = Physics.OverlapSphere(transform.position, 30, ObjectFactory.NavBoxesLayerMask);
        ShipBase foundTarget = null;
        _currAttackBehavior = ShipAttackPattern.Aggressive;
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
            else
            {
                if (s != _controlledShip && s is Ship alliedShip && _controlledShip is Ship currShip)
                {
                    if (currShip.ShipSize == ObjectFactory.ShipSize.Destroyer &&
                        (alliedShip.ShipSize == ObjectFactory.ShipSize.Cruiser || alliedShip.ShipSize == ObjectFactory.ShipSize.CapitalShip))
                    {
                        _currAttackBehavior = ShipAttackPattern.HitAndRun;
                    }
                }
            }
        }

        if (foundTarget != null)
        {
            if (TargetToFollow(foundTarget))
            {
                TargetShip = foundTarget;
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
        if (TargetShip != null && (TargetShip.ShipActiveInCombat || (TargetShip.transform.position - transform.position).sqrMagnitude > 50 * 50))
        {
            TargetShip = null;
        }
    }

    protected virtual bool TargetToFollow(ShipBase s)
    {
        return s is Ship;
    }

    protected virtual Vector3 AttackPosition(ShipBase enemyShip)
    {
        float minRange = _controlledShip.Turrets.Where(t => t.HardpointAIHint == TurretAIHint.Main || t.HardpointAIHint == TurretAIHint.Secondary).Select(x => x.GetMaxRange).Min();
        Vector3 Front = enemyShip.transform.forward;
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
                float dist = minRange * GlobalDistances.ShipAIAttackRangeCoefficient * (j + 1) / _numAttackDistances;
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

    protected virtual Vector3 AttackPositionArtilery(ShipBase enemyShip)
    {
        float attackRange = _controlledShip.Turrets.Where(t => t.HardpointAIHint == TurretAIHint.Main).Select(x => x.GetMaxRange).Min();
        float mainEnemyRange = enemyShip.Turrets.Select(x => x.GetMaxRange).Max();
        Vector3 attackVec = enemyShip.transform.position - transform.position;
        //Vector3 attackVecNormalized = attackVec.normalized;

        Collider[] colliders = Physics.OverlapSphere(enemyShip.transform.position, attackRange, ObjectFactory.NavBoxesLayerMask);
        Vector3 friendlyShipsCentroid = Vector3.zero;
        Vector3 enemyShipsCentroid = enemyShip.transform.position;
        float threatMaxRange = 0f;
        int friendlyCount = 0, enemyCount = 0;
        foreach (Collider c in colliders)
        {
            ShipBase sb = ShipBase.FromCollider(c);
            if (sb == _controlledShip || sb == enemyShip || !sb.ShipActiveInCombat)
            {
                continue;
            }
            if (sb is Ship currShip)
            {
                if (_controlledShip.Owner.IsEnemy(currShip.Owner))
                {
                    //enemyShipsCentroid += currShip.transform.position;
                    float threatRange = currShip.Turrets.Select(x => x.GetMaxRange).Max();
                    Vector3 threatVec = (transform.position - currShip.transform.position).normalized * threatRange;
                    Vector3 threatThresholdVec = Vector3.Project(-threatVec, attackVec);
                    if (threatThresholdVec.sqrMagnitude > threatMaxRange)
                    {
                        threatMaxRange = threatThresholdVec.sqrMagnitude;
                        ++enemyCount;
                    }
                }
                else
                {
                    friendlyShipsCentroid += currShip.transform.position;
                    ++friendlyCount;
                }
            }
        }
        if (enemyCount > 0)
        {
            threatMaxRange = Mathf.Sqrt(threatMaxRange);
            if (friendlyCount > 0 && friendlyCount > enemyCount)
            {
                friendlyShipsCentroid = friendlyShipsCentroid / friendlyCount;
                Vector3 newAttackVec = enemyShip.transform.position - friendlyShipsCentroid;
                return friendlyShipsCentroid + (newAttackVec.normalized * threatMaxRange * GlobalDistances.ShipAIArtilleryKeepDistCoefficient);
            }
            else if (friendlyCount > 0)
            {
                return transform.position + (attackVec.normalized * Mathf.Min(attackRange, threatMaxRange * GlobalDistances.ShipAIArtilleryKeepDistCoefficient));
            }
            else
            {
                enemyShipsCentroid = enemyShipsCentroid / (enemyCount + 1);
                return transform.position - ((enemyShipsCentroid - transform.position).normalized * threatMaxRange * GlobalDistances.ShipAIArtilleryKeepDistCoefficient);
            }
        }
        else
        {
            return transform.position + (attackVec.normalized * Mathf.Min(attackRange, mainEnemyRange * GlobalDistances.ShipAIArtilleryKeepDistCoefficient));
        }
    }

    protected virtual Vector3 HitAndRunDest(ShipBase enemyShip)
    {
        float attackRange = _controlledShip.Turrets.Where(t => t.HardpointAIHint == TurretAIHint.HitandRun).Select(x => x.GetMaxRange).Min();
        float mainEnemyRange = enemyShip.Turrets.Select(x => x.GetMaxRange).Max();

        Collider[] colliders = Physics.OverlapSphere(enemyShip.transform.position, Mathf.Max(mainEnemyRange, attackRange), ObjectFactory.NavBoxesLayerMask);
        int friendlyCount = 0, enemyCount = 1;
        Vector3 friendlyCentroid = Vector3.zero;
        Vector3 enemyCentroid = enemyShip.transform.position;
        foreach (Collider c in colliders)
        {
            ShipBase sb = ShipBase.FromCollider(c);
            if (sb == _controlledShip || sb == enemyShip || !sb.ShipActiveInCombat)
            {
                continue;
            }
            if (sb is Ship currShip)
            {
                if (_controlledShip.Owner.IsEnemy(currShip.Owner))
                {
                    enemyCentroid += currShip.transform.position;
                    ++enemyCount;
                }
                else
                {
                    friendlyCentroid += currShip.transform.position;
                    ++friendlyCount;
                }
            }
        }


        friendlyCentroid = friendlyCentroid / friendlyCount;
        enemyCentroid = enemyCentroid / enemyCount;

        Vector3 fightVec = enemyCentroid - friendlyCentroid;

        if ((enemyCount * 3) <= (friendlyCount * 4) &&
            _controlledShip.Turrets.
                Where(t => t.HardpointAIHint == TurretAIHint.HitandRun && t.ComponentIsWorking).
                    All(t2 => t2.ReadyToFire()))
        {
            // Hit
            Quaternion q1 = Quaternion.AngleAxis(45, Vector3.up);
            Quaternion q2 = Quaternion.AngleAxis(-45, Vector3.up);
            Vector3 scaledFightVec = fightVec.normalized * attackRange * GlobalDistances.ShipAIHitAndRunAttackRangeCoefficient;
            Vector3 attackPt1 = enemyShip.transform.position + (q1 * scaledFightVec);
            Vector3 attackPt2 = enemyShip.transform.position + (q2 * scaledFightVec);
            if ((transform.position - attackPt1).sqrMagnitude < (transform.position - attackPt2).sqrMagnitude)
            {
                return attackPt1;
            }
            else
            {
                return attackPt2;
            }
        }
        else
        {
            // Run
            Quaternion q1 = Quaternion.AngleAxis(90, Vector3.up);
            Vector3 scaledFightVec = fightVec.normalized * _controlledShip.ShipLength * GlobalDistances.ShipAIAntiClumpLengthFactor;
            Vector3 retreatPt1 = friendlyCentroid + (q1 * scaledFightVec);
            Vector3 retreatPt2 = friendlyCentroid - (q1 * scaledFightVec);
            if ((transform.position - retreatPt1).sqrMagnitude < (transform.position - retreatPt2).sqrMagnitude)
            {
                return retreatPt1;
            }
            else
            {
                return retreatPt2;
            }
        }
    }

    protected virtual void AdvanceToTarget()
    {
        Vector3 vecToTarget;
        if (!GetCurrMovementTarget(out vecToTarget))
        {
            if (_navAgent != null)
            {
                _navAgent.isStopped = true;
            }
            else
            {
                _bug0Alg.HasNavTarget = false;
            }
            return;
        }

        if (_navAgent != null)
        {
        }
        else
        {

            _bug0Alg.NavTarget = _navTarget;

            _bug0Alg.Step();

            if (_bug0Alg.AtDestination)
            {
                _doNavigate = false;
            }
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
        if (_navAgent != null && !_controlledShip.ShipImmobilized)
        {
            _navAgent.SetDestination(_navTarget);
        }
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

    protected virtual Vector3? AntiClumpNav()
    {
        float clumpingDetectRadius = _controlledShip.ShipLength * GlobalDistances.ShipAIAntiClumpLengthFactor;
        Collider[] colliders = Physics.OverlapSphere(transform.position, clumpingDetectRadius, ObjectFactory.NavBoxesLayerMask);
        int clumping = 1;
        Vector3 centroid = transform.position;
        foreach (Collider c in colliders)
        {
            ShipBase s = ShipBase.FromCollider(c);
            if (s == null || s == _controlledShip)
            {
                continue;
            }
            centroid += s.transform.position;
            ++clumping;
        }
        if (clumping >= 4)
        {
            return (transform.position - (centroid / clumping)).normalized * (clumpingDetectRadius * GlobalDistances.ShipAIAntiClumpMoveDistFactor * clumping);
        }
        else
        {
            return null;
        }
    }

    private IEnumerator AcquireTargetPulse()
    {
        yield return _targetAcquirePulseDelay;
        while (true)
        {
            if (_controlledShip.ShipControllable && DoSeekTargets)
            {
                if (TargetShip == null)
                {
                    AcquireTarget();
                }
                if (TargetShip != null)
                {
                    if (TargetShip.ShipDisabled)
                    {
                        TargetShip = null;
                        continue;
                    }

                    if (_cyclesToRecomputePath == 0)
                    {
                        switch (_currAttackBehavior)
                        {
                            case ShipAttackPattern.Aggressive:
                                NormalAttackBehavior();
                                break;
                            case ShipAttackPattern.Artillery:
                                ArtilleryAttackBehavior();
                                break;
                            case ShipAttackPattern.HitAndRun:
                                HitAndRunBehavior();
                                break;
                            default:
                                break;
                        }
                        _cyclesToRecomputePath = Random.Range(3, 7);
                    }
                    else
                    {
                        --_cyclesToRecomputePath;
                    }
                }
                else
                {
                    if (_cyclesToRecomputePath == 0)
                    {
                        NavigateWithoutTarget();
                        _cyclesToRecomputePath = Random.Range(3, 7);
                    }
                    else
                    {
                        --_cyclesToRecomputePath;
                    }
                }
            }
            else if (!_controlledShip.ShipActiveInCombat)
            {
                yield break;
            }

            yield return _targetAcquirePulseDelay;
        }
    }

    protected virtual Vector3 NavigationDest(ShipBase targetShip)
    {
        return AttackPosition(TargetShip);
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

    protected void NormalAttackBehavior()
    {
        Vector3 attackPos = AttackPosition(TargetShip);
        NavigateTo(attackPos);
    }

    protected void ArtilleryAttackBehavior()
    {
        Vector3 attackPos = NavigationDest(TargetShip);
        NavigateTo(attackPos);
    }

    protected void HitAndRunBehavior()
    {
        NavigateTo(HitAndRunDest(TargetShip));
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
    protected NavMeshAgent _navAgent;

    protected static readonly int _numAttackAngles = 12;
    protected static readonly int _numAttackDistances = 3;
    protected Vector3[] _attackPositions = new Vector3[_numAttackAngles * _numAttackDistances];
    protected float[] _attackPositionWeights = new float[_numAttackAngles * _numAttackDistances];

    public ShipBase TargetShip { get; protected set; }
    protected Vector3 _navTarget;
    private Vector3 _targetHeading;
    protected Transform _followTarget = null;
    protected float _followDist;
    protected OrderCompleteDlg _orderCallback = null;
    private static readonly float _angleEps = 0.1f;
    protected bool _doNavigate = false;
    protected bool _doFollow = false;
    protected ShipAttackPattern _currAttackBehavior;
    protected int _cyclesToRecomputePath = 0;

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

    public enum ShipAttackPattern
    {
        None,
        Aggressive,
        Artillery,
        HitAndRun
    }

    public ShipActivity CurrActivity { get; protected set; }

    protected static readonly WaitForSeconds _targetAcquirePulseDelay = new WaitForSeconds(0.25f);
}
