using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using UnityEngine.AI;
using System;

public class ShipAIController : MonoBehaviour
{
    // Use this for initialization
    protected virtual void Start()
    {
        _controlledShip = GetComponent<ShipBase>();
        CurrActivity = ShipActivity.ControllingPosition;
        if (_controlType == ShipControlType.Autonomous)
        {
            _autoBehaviorCoroutine = StartCoroutine(AcquireTargetPulse());
        }
        TargetShip = null;
        _navGuide = ObjectFactory.CreateNavGuide(transform.position, transform.forward);
        _navGuide.Attach(_controlledShip);
        _navGuide.ManualControl = _controlType == ShipControlType.Manual;
        // temporary default:
        _currAttackBehavior = ShipAttackPattern.Aggressive;
    }
	
	// Update is called once per frame
	protected virtual void Update()
    {
        if (ControlType == ShipControlType.Manual || !_controlledShip.ShipControllable)
        {
            return;
        }

        if (_doNavigate || _doFollow)
        {
            AdvanceToTarget();
        }
	}

    protected virtual int TargetsToFollowLayerMask => ObjectFactory.NavBoxesLayerMask;
    protected virtual int TargetsToAttackLayerMask => ObjectFactory.NavBoxesAllLayerMask;

    private void AcquireTarget()
    {
        int numHits = Physics.OverlapSphereNonAlloc(transform.position, 30, _collidersCache, TargetsToAttackLayerMask);
        ShipBase foundTarget = null;
        bool staticAttack = false;
        _currAttackBehavior = ShipAttackPattern.Aggressive;
        for (int i = 0; i < numHits; ++i)
        {
            int colliderLayer = 1 << _collidersCache[i].gameObject.layer;
            bool validFollowTarget = (colliderLayer & TargetsToFollowLayerMask) != 0;
            bool validStaticAttackTarget = (colliderLayer & TargetsToAttackLayerMask) != 0;
            ShipBase s = ShipBase.FromCollider(_collidersCache[i]);
            if (Torpedo.FromCollider(_collidersCache[i]) != null)
            {
                staticAttack = true;
            }
            if (s == null)
            {
                continue;
            }
            else if (!s.ShipActiveInCombat)
            {
                continue;
            }
            if (validFollowTarget && _controlledShip.Owner.IsEnemy(s.Owner))
            {
                foundTarget = s;
            }
            else if (validFollowTarget)
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
            else if (validStaticAttackTarget && _controlledShip.Owner.IsEnemy(s.Owner))
            {
                staticAttack = true;
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
        else if (staticAttack)
        {
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

    protected Func<ITurret, bool> _turretsMain = t => t.HardpointAIHint == TurretAIHint.Main;
    protected Func<ITurret, bool> _turretsMainAndSecondary = t => t.HardpointAIHint == TurretAIHint.Main || t.HardpointAIHint == TurretAIHint.Secondary;
    protected Func<ITurret, bool> _turretsHitAndRun = t => t.HardpointAIHint == TurretAIHint.HitandRun;
    protected Func<ITurret, bool> _turretsAll = t => true;
    protected virtual Vector3 AttackPosition(ShipBase enemyShip)
    {
        float minRange = _controlledShip.TurretsGetAttackRange(_turretsMainAndSecondary);
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
        float attackRange = _controlledShip.TurretsGetAttackRange(_turretsMainAndSecondary);
        float mainEnemyRange = enemyShip.TurretsGetAttackRange(_turretsAll, false);
        Vector3 attackVec = enemyShip.transform.position - transform.position;
        //Vector3 attackVecNormalized = attackVec.normalized;

        int numHits = Physics.OverlapSphereNonAlloc(enemyShip.transform.position, attackRange, _collidersCache, ObjectFactory.NavBoxesLayerMask);
        Vector3 friendlyShipsCentroid = Vector3.zero;
        Vector3 enemyShipsCentroid = enemyShip.transform.position;
        float threatMaxRange = 0f;
        int friendlyCount = 0, enemyCount = 0;
        for (int i = 0; i < numHits; ++i)
        {
            ShipBase sb = ShipBase.FromCollider(_collidersCache[i]);
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
        float attackRange = _controlledShip.TurretsGetAttackRange(_turretsHitAndRun);
        float mainEnemyRange = enemyShip.TurretsGetAttackRange(_turretsAll);

        int numHits = Physics.OverlapSphereNonAlloc(enemyShip.transform.position, Mathf.Max(mainEnemyRange, attackRange), _collidersCache, ObjectFactory.NavBoxesLayerMask);
        int friendlyCount = 0, enemyCount = 1;
        Vector3 friendlyCentroid = Vector3.zero;
        Vector3 enemyCentroid = enemyShip.transform.position;
        for (int i = 0; i < numHits; ++i)
        {
            ShipBase sb = ShipBase.FromCollider(_collidersCache[i]);
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
            _controlledShip.TurretsAllReadyToFire(_turretsHitAndRun))
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
            _navGuide.Halt();
            return;
        }
    }

    protected bool GetCurrMovementTarget(out Vector3 vecToTarget)
    {
        if (_doNavigate)
        {
            vecToTarget = _navTarget - transform.position;
            return true;
        }
        else if (_doFollow && CurrActivity == ShipActivity.Following)
        {
            vecToTarget = _followTarget.transform.position - transform.position;
            Vector3 dirToTarget = vecToTarget.normalized;
            vecToTarget -= dirToTarget * _followDist;
            return true;
        }
        else if (_doFollow && CurrActivity != ShipActivity.Following)
        {
            vecToTarget = _followTransform.position - transform.position;
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

    public void UserNavigateTo(Vector3 target)
    {
        CurrActivity = ShipActivity.ControllingPosition;
        _doFollow = false;
        _doNavigate = true;
        NavigateTo(target, null);
    }

    protected void NavigateTo(Vector3 target, Action onCompleteNavigation)
    {
        _navTarget = target;
        _orderCallback = onCompleteNavigation;
        if (!_controlledShip.ShipImmobilized)
        {
            _navGuide.SetDestination(_navTarget);
        }
    }

    public virtual void Follow(ShipBase followTarget)
    {
        if (followTarget == _controlledShip)
        {
            return;
        }
        SetFollowTarget(followTarget, ShipBase.FormationHalfSpacing(_controlledShip) + ShipBase.FormationHalfSpacing(followTarget));
        CurrActivity = ShipActivity.Following;
    }

    protected void SetFollowTarget(ShipBase followTarget, float dist)
    {
        // Cancel navigate order, if there is one:
        _doNavigate = false;
        _orderCallback = null;

        _followTarget = followTarget;
        _followTransform = null;
        _followDist = dist;
        _doFollow = true;
    }

    protected void SetFollowTransform(Transform followTr, float dist)
    {
        // Cancel navigate order, if there is one:
        _doNavigate = false;
        _orderCallback = null;

        _followTarget = null;
        _followTransform = followTr;
        _followDist = dist;
        _doFollow = true;
    }

    protected virtual Vector3? AntiClumpNav()
    {
        float clumpingDetectRadius = _controlledShip.ShipLength * GlobalDistances.ShipAIAntiClumpLengthFactor;
        int numHits = Physics.OverlapSphereNonAlloc(transform.position, clumpingDetectRadius, _collidersCache, ObjectFactory.NavBoxesLayerMask);
        int clumping = 1;
        Vector3 centroid = transform.position;
        for (int i = 0; i < numHits; ++i)
        {
            ShipBase s = ShipBase.FromCollider(_collidersCache[i]);
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
                        _cyclesToRecomputePath = UnityEngine.Random.Range(3, 7);
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
                        _cyclesToRecomputePath = UnityEngine.Random.Range(3, 7);
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
            else
            {
                if (_cyclesToRecomputePath == 0)
                {
                    if (CurrActivity == ShipActivity.Following)
                    {
                        NavigateInFormation();
                    }
                    else
                    {
                        NavigateWithoutTarget();
                    }
                    _cyclesToRecomputePath = UnityEngine.Random.Range(3, 7);
                }
                else
                {
                    --_cyclesToRecomputePath;
                }
            }

            yield return _targetAcquirePulseDelay;
        }
    }

    private IEnumerator SemiAutonomousBehaviorPulse()
    {
        yield return _targetAcquirePulseDelay;
        foreach (ITurret t in _controlledShip.Turrets)
        {
            t.SetTurretBehavior(TurretBase.TurretMode.Auto);
        }
        while (true)
        {
            if (!_controlledShip.ShipActiveInCombat)
            {
                yield break;
            }

            if (_cyclesToRecomputePath == 0)
            {
                if (CurrActivity == ShipActivity.Following)
                {
                    NavigateInFormation();
                }
                _cyclesToRecomputePath = UnityEngine.Random.Range(3, 7);
            }
            else
            {
                --_cyclesToRecomputePath;
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

    protected virtual void NavigateInFormation()
    {
        if (_followTarget == null)
        {
            _doFollow = false;
            CurrActivity = ShipActivity.ControllingPosition;
        }
        else if (!_followTarget.ShipActiveInCombat)
        {
            ShipBase nextFollowTarget = null;
            ShipAIController nextAIController = _followTarget.GetComponent<ShipAIController>();
            if (nextAIController != null && nextAIController.CurrActivity == ShipActivity.Following)
            {
                nextFollowTarget = nextAIController._followTarget;
            }
            _followTarget = nextFollowTarget;
            if (_followTarget == null)
            {
                _doFollow = false;
                CurrActivity = ShipActivity.ControllingPosition;
            }
        }
        else
        {
            Vector3 vecToFollowTarget = (_followTarget.transform.position - transform.position).normalized * _followDist;
            _navGuide.SetDestination(_followTarget.transform.position - vecToFollowTarget);
        }
        AcquireTarget();
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
                case ShipActivity.ForceMoving:
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
        _doNavigate = true;
        _doFollow = false;
        NavigateTo(attackPos);
    }

    protected void ArtilleryAttackBehavior()
    {
        Vector3 attackPos = NavigationDest(TargetShip);
        _doNavigate = true;
        _doFollow = false;
        NavigateTo(attackPos);
    }

    protected void HitAndRunBehavior()
    {
        _doNavigate = true;
        _doFollow = false;
        NavigateTo(HitAndRunDest(TargetShip));
    }

    protected virtual Bug0 GenBug0Algorithm()
    {
        return new Bug0(_controlledShip, _controlledShip.ShipLength, _controlledShip.ShipWidth, false);
    }

    protected virtual void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        switch (CurrActivity)
        {
            case ShipActivity.Idle:
            case ShipActivity.ControllingPosition:
            case ShipActivity.ForceMoving:
            case ShipActivity.Defending:
            case ShipActivity.Launching:
            case ShipActivity.NavigatingToRecovery:
            case ShipActivity.StartingRecovery:
            case ShipActivity.Recovering:
            case ShipActivity.Attacking:
                Gizmos.DrawLine(transform.position, _navTarget);
                break;
            case ShipActivity.Following:
                Gizmos.DrawLine(transform.position, _followTarget.transform.position);
                break;
            default:
                break;
        }
        //_bug0Alg.DrawDebugLines();
    }

    public ShipControlType ControlType
    {
        get
        {
            return _controlType;
        }
        set
        {
            bool changeControlType = _controlType != value;
            _controlType = value;
            if (_navGuide != null)
                _navGuide.ManualControl = _controlType == ShipControlType.Manual;

            if (changeControlType)
            {
                if (_controlType == ShipControlType.Manual)
                {
                    if (_semiAutoBehaviorCoroutine != null)
                        StopCoroutine(_semiAutoBehaviorCoroutine);
                    if (_autoBehaviorCoroutine != null)
                        StopCoroutine(_autoBehaviorCoroutine);
                }
                else
                {
                    if (_controlType == ShipControlType.SemiAutonomous)
                        _semiAutoBehaviorCoroutine = StartCoroutine(SemiAutonomousBehaviorPulse());
                    else
                        _autoBehaviorCoroutine = StartCoroutine(AcquireTargetPulse());

                }
            }
        }
    }
    private ShipControlType _controlType;

    protected ShipBase _controlledShip;
    protected NavigationGuide _navGuide = null;

    protected static readonly int _numAttackAngles = 12;
    protected static readonly int _numAttackDistances = 3;
    protected Vector3[] _attackPositions = new Vector3[_numAttackAngles * _numAttackDistances];
    protected float[] _attackPositionWeights = new float[_numAttackAngles * _numAttackDistances];

    public ShipBase TargetShip { get; protected set; }
    protected Vector3 _navTarget;
    private Vector3 _targetHeading;
    protected ShipBase _followTarget = null;
    protected Transform _followTransform = null;
    protected float _followDist;
    protected Action _orderCallback = null;
    protected bool _doNavigate = false;
    protected bool _doFollow = false;
    protected ShipAttackPattern _currAttackBehavior;
    protected int _cyclesToRecomputePath = 0;

    private Coroutine _autoBehaviorCoroutine = null, _semiAutoBehaviorCoroutine = null;

    public ShipActivity CurrActivity { get; protected set; }

    // Ugly optimization:
    protected Collider[] _collidersCache = new Collider[1024];

    protected static readonly WaitForSeconds _targetAcquirePulseDelay = new WaitForSeconds(0.25f);
}

public enum ShipActivity
{
    Idle,
    ControllingPosition,
    Attacking,
    ForceMoving,
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

public enum ShipControlType
{
    Manual,
    SemiAutonomous,
    Autonomous
}
