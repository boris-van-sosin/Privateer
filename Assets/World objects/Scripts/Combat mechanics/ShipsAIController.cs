using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ShipsAIController : MonoBehaviour
{
    void Start()
    {
        StartCoroutine(NavUpdate());
        StartCoroutine(TacticsUpdate());
    }

    public void AddShip(Ship s)
    {
        if (_controlledShipsLookup.ContainsKey(s))
        {
            Debug.LogWarningFormat("Attempted to add ship to AI control more than once: {0}", s);
            return;
        }

        ShipAIController.ShipControlType controlType = ShipAIController.ShipControlType.Manual;

        NavigationGuide navGuide = ObjectFactory.CreateNavGuide(s.transform.position, s.transform.forward);
        navGuide.Attach(s);
        navGuide.ManualControl = controlType == ShipAIController.ShipControlType.Manual;
        _controlledShips.Add(new ShipAIData(s, navGuide, controlType));
        _controlledShipsLookup[s] = _controlledShips.Count - 1;
    }

    public void ReactivateShip(Ship s, NavigationGuide navGuide)
    {
        if (navGuide == null || navGuide.AttachedEntity != s)
        {
            Debug.LogWarningFormat("Attempted to reactivate ship {0} with either a null or mis-matched nav guide", s);
            return;
        }

        ShipAIController.ShipControlType controlType = ShipAIController.ShipControlType.Manual;

        navGuide.ManualControl = controlType == ShipAIController.ShipControlType.Manual;
        _controlledShips.Add(new ShipAIData(s, navGuide, controlType));
        _controlledShipsLookup[s] = _controlledShips.Count - 1;
    }

    private IEnumerator NavUpdate()
    {
        yield return _targetAcquirePulseDelay;
        while (true)
        {
            int i = 0;
            while (i < _controlledShips.Count)
            {
                ShipAIData shipAI = _controlledShips[i];
                try
                {
                    if (!shipAI.ControlledShip.ShipControllable)
                    {
                        _controlledShips.RemoveAt(i);
                        _controlledShipsLookup.Remove(shipAI.ControlledShip);
                        for (int j = 0; j < _controlledShips.Count; j++)
                        {
                            _controlledShipsLookup[_controlledShips[j].ControlledShip] = j;
                        }
                        continue;
                    }

                    if (shipAI.DoNavigate || shipAI.DoFollow)
                    {
                        AdvanceToTarget(shipAI);
                    }
                }
                catch (Exception exc)
                {
                    Debug.LogWarningFormat("Got exception for navigation: {0}", exc);
                }
                ++i;
                yield return _navDelay;
            }
            if (_controlledShips.Count == 0)
            {
                yield return _navDelay;
            }
        }
    }

    private static void AdvanceToTarget(ShipAIData shipAI)
    {
        if (!GetCurrMovementTarget(shipAI, out _))
        {
            shipAI.NavGuide.Halt();
        }
    }

    private static bool GetCurrMovementTarget(ShipAIData shipAI, out Vector3 vecToTarget)
    {
        if (shipAI.DoNavigate)
        {
            vecToTarget = shipAI.NavTarget - shipAI.ControlledShip.transform.position;
            return true;
        }
        else if (shipAI.DoFollow && shipAI.CurrActivity == ShipAIController.ShipActivity.Following)
        {
            vecToTarget = shipAI.FollowTarget.transform.position - shipAI.ControlledShip.transform.position;
            Vector3 dirToTarget = vecToTarget.normalized;
            vecToTarget -= dirToTarget * shipAI.FollowDist;
            return true;
        }
        else if (shipAI.DoFollow && shipAI.CurrActivity != ShipAIController.ShipActivity.Following)
        {
            vecToTarget = shipAI.FollowTransform.position - shipAI.ControlledShip.transform.position;
            Vector3 dirToTarget = vecToTarget.normalized;
            vecToTarget -= dirToTarget * shipAI.FollowDist;
            return true;
        }
        else
        {
            vecToTarget = Vector3.zero;
            return false;
        }
    }

    public void NavigateTo(Ship s, Vector3 target)
    {
        int idx;
        if (!_controlledShipsLookup.TryGetValue(s, out idx))
        {
            return;
        }
        NavigateTo(_controlledShips[idx], target, null);
    }

    public void UserNavigateTo(Ship s, Vector3 target)
    {
        int idx;
        if (!_controlledShipsLookup.TryGetValue(s, out idx))
        {
            return;
        }
        ShipAIData ai = _controlledShips[idx];
        ai.CurrActivity = ShipAIController.ShipActivity.ControllingPosition;
        ai.DoFollow = false;
        ai.DoNavigate = true;
        NavigateTo(ai, target, null);
    }

    private static void NavigateTo(ShipAIData shipAI, Vector3 target, Action onCompleteNavigation)
    {
        shipAI.NavTarget = target;
        shipAI.OrderCallback = onCompleteNavigation;
        if (!shipAI.ControlledShip.ShipImmobilized)
        {
            shipAI.NavGuide.SetDestination(shipAI.NavTarget);
        }
    }

    public void Follow(Ship s, ShipBase followTarget)
    {
        int idx;
        if (!_controlledShipsLookup.TryGetValue(s, out idx))
        {
            return;
        }
        ShipAIData ai = _controlledShips[idx];
        if (followTarget == ai.ControlledShip)
        {
            return;
        }
        SetFollowTarget(ai, followTarget, ShipBase.FormationHalfSpacing(ai.ControlledShip) + ShipBase.FormationHalfSpacing(followTarget));
        ai.CurrActivity = ShipAIController.ShipActivity.Following;
    }

    private void SetFollowTarget(ShipAIData shipAI, ShipBase followTarget, float dist)
    {
        // Cancel navigate order, if there is one:
        shipAI.DoNavigate = false;
        shipAI.OrderCallback = null;

        shipAI.FollowTarget = followTarget;
        shipAI.FollowTransform = null;
        shipAI.FollowDist = dist;
        shipAI.DoFollow = true;
    }

    private IEnumerator TacticsUpdate()
    {
        yield return _targetAcquirePulseDelay;
        while (true)
        {
            int i = 0;
            while (i < _controlledShips.Count)
            {
                ShipAIData shipAI = _controlledShips[i];
                try
                {
                    if (!shipAI.ControlledShip.ShipControllable)
                    {
                        ++i;
                        continue;
                    }

                    switch (shipAI.ControlType)
                    {
                        case ShipAIController.ShipControlType.SemiAutonomous:
                            SemiAutonomousBehaviorPulse(shipAI);
                            break;
                        case ShipAIController.ShipControlType.Autonomous:
                            AutonomousBehaviorPulse(shipAI);
                            break;
                        default:
                            break;
                    }
                }
                catch (Exception exc)
                {
                    Debug.LogWarningFormat("Got exception for tactics: {0}", exc);
                }
                yield return _navDelay;
                ++i;
            }
            yield return _targetAcquirePulseDelay;
        }
    }

    private void AutonomousBehaviorPulse(ShipAIData shipAI)
    {
        if (shipAI.ControlledShip.ShipControllable && DoSeekTargets(shipAI))
        {
            if (shipAI.TargetShip == null)
            {
                AcquireTarget(shipAI);
            }
            if (shipAI.TargetShip != null)
            {
                if (shipAI.TargetShip.ShipDisabled)
                {
                    shipAI.TargetShip = null;
                    return;
                }

                if (shipAI.CyclesToRecomputePath == 0)
                {
                    switch (shipAI.AttackPattern)
                    {
                        case ShipAIController.ShipAttackPattern.Aggressive:
                            NormalAttackBehavior(shipAI);
                            break;
                        case ShipAIController.ShipAttackPattern.Artillery:
                            ArtilleryAttackBehavior(shipAI);
                            break;
                        case ShipAIController.ShipAttackPattern.HitAndRun:
                            HitAndRunBehavior(shipAI);
                            break;
                        default:
                            break;
                    }
                    shipAI.CyclesToRecomputePath = UnityEngine.Random.Range(3, 7);
                }
                else
                {
                    --shipAI.CyclesToRecomputePath;
                }
            }
            else
            {
                if (shipAI.CyclesToRecomputePath == 0)
                {
                    //NavigateWithoutTarget(shipAI); nothing? really?
                    shipAI.CyclesToRecomputePath = UnityEngine.Random.Range(3, 7);
                }
                else
                {
                    --shipAI.CyclesToRecomputePath;
                }
            }
        }
        else if (!shipAI.ControlledShip.ShipActiveInCombat)
        {
            return;
        }
        else
        {
            if (shipAI.CyclesToRecomputePath == 0)
            {
                if (shipAI.CurrActivity == ShipAIController.ShipActivity.Following)
                {
                    NavigateInFormation(shipAI);
                }
                else
                {
                    //NavigateWithoutTarget(shipAI); nothing? really?
                }
                shipAI.CyclesToRecomputePath = UnityEngine.Random.Range(3, 7);
            }
            else
            {
                --shipAI.CyclesToRecomputePath;
            }
        }
    }

    private static bool DoSeekTargets(ShipAIData shipAI)
    {
        switch (shipAI.CurrActivity)
        {
            case ShipAIController.ShipActivity.Idle:
            case ShipAIController.ShipActivity.ControllingPosition:
            case ShipAIController.ShipActivity.Defending:
                return true;
            case ShipAIController.ShipActivity.ForceMoving:
            case ShipAIController.ShipActivity.Attacking:
            case ShipAIController.ShipActivity.Following:
            case ShipAIController.ShipActivity.Launching:
            case ShipAIController.ShipActivity.NavigatingToRecovery:
            case ShipAIController.ShipActivity.StartingRecovery:
                return false;
            default:
                break;
        }
        return true;
    }

    private void NormalAttackBehavior(ShipAIData shipAI)
    {
        Vector3 attackPos = AttackPosition(shipAI);
        shipAI.DoNavigate = true;
        shipAI.DoFollow = false;
        NavigateTo(shipAI, attackPos, null);
    }

    private void ArtilleryAttackBehavior(ShipAIData shipAI)
    {
        Vector3 attackPos = AttackPositionArtilery(shipAI);
        shipAI.DoNavigate = true;
        shipAI.DoFollow = false;
        NavigateTo(shipAI, attackPos, null);
    }

    private void HitAndRunBehavior(ShipAIData shipAI)
    {
        shipAI.DoNavigate = true;
        shipAI.DoFollow = false;
        NavigateTo(shipAI, HitAndRunDest(shipAI), null);
    }

    private static readonly Func<ITurret, bool> _turretsMain = t => t.HardpointAIHint == TurretAIHint.Main;
    private static readonly Func<ITurret, bool> _turretsMainAndSecondary = t => t.HardpointAIHint == TurretAIHint.Main || t.HardpointAIHint == TurretAIHint.Secondary;
    private static readonly Func<ITurret, bool> _turretsHitAndRun = t => t.HardpointAIHint == TurretAIHint.HitandRun;
    private static readonly Func<ITurret, bool> _turretsAll = t => true;

    private Vector3 AttackPosition(ShipAIData shipAI)
    {
        ShipBase enemyShip = shipAI.TargetShip;
        float minRange = shipAI.ControlledShip.TurretsGetAttackRange(_turretsMainAndSecondary);
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
                shipAI.AttackPositions[k] = enemyShip.transform.position + dir * dist;
                shipAI.AttackPositionWeights[k] = currWeight;
                ++k;
            }
        }

        int minPos = 0;
        float minScore = (shipAI.AttackPositions[minPos] - shipAI.ControlledShip.transform.position).sqrMagnitude - shipAI.AttackPositionWeights[minPos];
        for (int i = 1; i < shipAI.AttackPositions.Length; ++i)
        {
            float currScore = (shipAI.AttackPositions[i] - shipAI.ControlledShip.transform.position).sqrMagnitude - shipAI.AttackPositionWeights[i];
            if (currScore < minScore)
            {
                minPos = i;
                minScore = currScore;
            }
        }
        return shipAI.AttackPositions[minPos];
    }

    private Vector3 AttackPositionArtilery(ShipAIData shipAI)
    {
        ShipBase enemyShip = shipAI.TargetShip;
        float attackRange = shipAI.ControlledShip.TurretsGetAttackRange(_turretsMainAndSecondary);
        float mainEnemyRange = enemyShip.TurretsGetAttackRange(_turretsAll, false);
        Vector3 attackVec = enemyShip.transform.position - shipAI.ControlledShip.transform.position;
        //Vector3 attackVecNormalized = attackVec.normalized;

        int numHits = Physics.OverlapSphereNonAlloc(enemyShip.transform.position, attackRange, _collidersCache, ObjectFactory.NavBoxesLayerMask);
        Vector3 friendlyShipsCentroid = Vector3.zero;
        Vector3 enemyShipsCentroid = enemyShip.transform.position;
        float threatMaxRange = 0f;
        int friendlyCount = 0, enemyCount = 0;
        for (int i = 0; i < numHits; ++i)
        {
            ShipBase sb = ShipBase.FromCollider(_collidersCache[i]);
            if (sb == shipAI.ControlledShip || sb == enemyShip || !sb.ShipActiveInCombat)
            {
                continue;
            }
            if (sb is Ship currShip)
            {
                if (shipAI.ControlledShip.Owner.IsEnemy(currShip.Owner))
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
                return shipAI.ControlledShip.transform.position + (attackVec.normalized * Mathf.Min(attackRange, threatMaxRange * GlobalDistances.ShipAIArtilleryKeepDistCoefficient));
            }
            else
            {
                enemyShipsCentroid = enemyShipsCentroid / (enemyCount + 1);
                return shipAI.ControlledShip.transform.position - ((enemyShipsCentroid - shipAI.ControlledShip.transform.position).normalized * threatMaxRange * GlobalDistances.ShipAIArtilleryKeepDistCoefficient);
            }
        }
        else
        {
            return shipAI.ControlledShip.transform.position + (attackVec.normalized * Mathf.Min(attackRange, mainEnemyRange * GlobalDistances.ShipAIArtilleryKeepDistCoefficient));
        }
    }

    private Vector3 HitAndRunDest(ShipAIData shipAI)
    {
        ShipBase enemyShip = shipAI.TargetShip;
        float attackRange = shipAI.ControlledShip.TurretsGetAttackRange(_turretsHitAndRun);
        float mainEnemyRange = enemyShip.TurretsGetAttackRange(_turretsAll);

        int numHits = Physics.OverlapSphereNonAlloc(enemyShip.transform.position, Mathf.Max(mainEnemyRange, attackRange), _collidersCache, ObjectFactory.NavBoxesLayerMask);
        int friendlyCount = 0, enemyCount = 1;
        Vector3 friendlyCentroid = Vector3.zero;
        Vector3 enemyCentroid = enemyShip.transform.position;
        for (int i = 0; i < numHits; ++i)
        {
            ShipBase sb = ShipBase.FromCollider(_collidersCache[i]);
            if (sb == shipAI.ControlledShip || sb == enemyShip || !sb.ShipActiveInCombat)
            {
                continue;
            }
            if (sb is Ship currShip)
            {
                if (shipAI.ControlledShip.Owner.IsEnemy(currShip.Owner))
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
            shipAI.ControlledShip.TurretsAllReadyToFire(_turretsHitAndRun))
        {
            // Hit
            Quaternion q1 = Quaternion.AngleAxis(45, Vector3.up);
            Quaternion q2 = Quaternion.AngleAxis(-45, Vector3.up);
            Vector3 scaledFightVec = fightVec.normalized * attackRange * GlobalDistances.ShipAIHitAndRunAttackRangeCoefficient;
            Vector3 attackPt1 = enemyShip.transform.position + (q1 * scaledFightVec);
            Vector3 attackPt2 = enemyShip.transform.position + (q2 * scaledFightVec);
            if ((shipAI.ControlledShip.transform.position - attackPt1).sqrMagnitude < (shipAI.ControlledShip.transform.position - attackPt2).sqrMagnitude)
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
            Vector3 scaledFightVec = fightVec.normalized * shipAI.ControlledShip.ShipLength * GlobalDistances.ShipAIAntiClumpLengthFactor;
            Vector3 retreatPt1 = friendlyCentroid + (q1 * scaledFightVec);
            Vector3 retreatPt2 = friendlyCentroid - (q1 * scaledFightVec);
            if ((shipAI.ControlledShip.transform.position - retreatPt1).sqrMagnitude < (shipAI.ControlledShip.transform.position - retreatPt2).sqrMagnitude)
            {
                return retreatPt1;
            }
            else
            {
                return retreatPt2;
            }
        }
    }

    private void NavigateInFormation(ShipAIData shipAI)
    {
        if (shipAI.FollowTarget == null)
        {
            shipAI.DoFollow = false;
            shipAI.CurrActivity = ShipAIController.ShipActivity.ControllingPosition;
        }
        else if (!shipAI.FollowTarget.ShipActiveInCombat)
        {
            ShipBase nextFollowTarget = null;
            int followingShipIdx;
            if (shipAI.FollowTarget is Ship followingShip && _controlledShipsLookup.TryGetValue(followingShip, out followingShipIdx))
            {
                ShipAIData followingShipAI = _controlledShips[followingShipIdx];
                if (followingShipAI.CurrActivity == ShipAIController.ShipActivity.Following)
                {
                    nextFollowTarget = followingShipAI.FollowTarget;
                }
            }
            shipAI.FollowTarget = nextFollowTarget;
            if (shipAI.FollowTarget == null)
            {
                shipAI.DoFollow = false;
                shipAI.CurrActivity = ShipAIController.ShipActivity.ControllingPosition;
            }
        }
        else
        {
            Vector3 vecToFollowTarget = (shipAI.FollowTarget.transform.position - shipAI.ControlledShip.transform.position).normalized * shipAI.FollowDist;
            shipAI.NavGuide.SetDestination(shipAI.FollowTarget.transform.position - vecToFollowTarget);
        }
        AcquireTarget(shipAI);
    }

    private int TargetsToFollowLayerMask => ObjectFactory.NavBoxesLayerMask;
    private int TargetsToAttackLayerMask => ObjectFactory.NavBoxesAllLayerMask; 

    private void AcquireTarget(ShipAIData shipAI)
    {
        int numHits = Physics.OverlapSphereNonAlloc(shipAI.ControlledShip.transform.position, 30, _collidersCache, TargetsToAttackLayerMask);
        ShipBase foundTarget = null;
        bool staticAttack = false;
        shipAI.AttackPattern = ShipAIController.ShipAttackPattern.Aggressive;
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
            if (validFollowTarget && shipAI.ControlledShip.Owner.IsEnemy(s.Owner))
            {
                foundTarget = s;
            }
            else if (validFollowTarget)
            {
                if (s != shipAI.ControlledShip && s is Ship alliedShip && shipAI.ControlledShip is Ship currShip)
                {
                    if (currShip.ShipSize == ObjectFactory.ShipSize.Destroyer &&
                        (alliedShip.ShipSize == ObjectFactory.ShipSize.Cruiser || alliedShip.ShipSize == ObjectFactory.ShipSize.CapitalShip))
                    {
                        shipAI.AttackPattern = ShipAIController.ShipAttackPattern.HitAndRun;
                    }
                }
            }
            else if (validStaticAttackTarget && shipAI.ControlledShip.Owner.IsEnemy(s.Owner))
            {
                staticAttack = true;
            }
        }

        if (foundTarget != null)
        {
            if (ShouldFollowTarget(shipAI, foundTarget))
            {
                shipAI.TargetShip = foundTarget;
            }
            foreach (ITurret t in shipAI.ControlledShip.Turrets)
            {
                t.SetTurretBehavior(TurretBase.TurretMode.Auto);
            }
        }
        else if (staticAttack)
        {
            foreach (ITurret t in shipAI.ControlledShip.Turrets)
            {
                t.SetTurretBehavior(TurretBase.TurretMode.Auto);
            }
        }
        else
        {
            foreach (ITurret t in shipAI.ControlledShip.Turrets)
            {
                t.SetTurretBehavior(TurretBase.TurretMode.Off);
            }
        }
    }

    private bool ShouldFollowTarget(ShipAIData shipAI, ShipBase target)
    {
        return target is Ship;
    }

    private void SemiAutonomousBehaviorPulse(ShipAIData shipAI)
    {
        if (!shipAI.ControlledShip.ShipActiveInCombat)
        {
            return;
        }

        if (shipAI.CyclesToRecomputePath == 0)
        {
            if (shipAI.CurrActivity == ShipAIController.ShipActivity.Following)
            {
                NavigateInFormation(shipAI);
            }
            shipAI.CyclesToRecomputePath = UnityEngine.Random.Range(3, 7);
        }
        else
        {
            --shipAI.CyclesToRecomputePath;
        }
    }

    public ShipAIController.ShipControlType GetControlType(Ship s)
    {
        int idx;
        if (_controlledShipsLookup.TryGetValue(s, out idx))
        {
            return _controlledShips[idx].ControlType;
        }
        else
        {
            return ShipAIController.ShipControlType.SemiAutonomous;
        }   
    }

    public void SetControlType(Ship s, ShipAIController.ShipControlType controlType)
    {
        int idx;
        if (!_controlledShipsLookup.TryGetValue(s, out idx))
        {
            return;
        }
        ShipAIData shipAI = _controlledShips[idx];
        bool changeControlType = shipAI.ControlType != controlType;
        //ShipAIController.ShipControlType prevControlType = shipAI.ControlType;
        shipAI.ControlType = controlType;
        if (shipAI.NavGuide != null)
            shipAI.NavGuide.ManualControl = controlType == ShipAIController.ShipControlType.Manual;

        if (changeControlType)
        {
            if (shipAI.ControlType == ShipAIController.ShipControlType.Manual)
            {
                foreach (ITurret t in shipAI.ControlledShip.Turrets)
                {
                    t.SetTurretBehavior(TurretBase.TurretMode.Manual);
                }
            }
            else if (shipAI.ControlType == ShipAIController.ShipControlType.SemiAutonomous)
            {
                foreach (ITurret t in shipAI.ControlledShip.Turrets)
                {
                    t.SetTurretBehavior(TurretBase.TurretMode.Auto);
                }
            }
        }
    }

    private Dictionary<Ship, int> _controlledShipsLookup = new Dictionary<Ship, int>();
    private List<ShipAIData> _controlledShips = new List<ShipAIData>();

    private class ShipAIData
    {
        public ShipAIData(Ship s, NavigationGuide navGuide, ShipAIController.ShipControlType controlType)
        {
            ControlledShip = s;
            ControlType = controlType;
            CurrActivity = ShipAIController.ShipActivity.Idle;
            AttackPattern = ShipAIController.ShipAttackPattern.Aggressive;
            NavGuide = navGuide;
            AttackPositions = new Vector3[_numAttackAngles * _numAttackDistances];
            AttackPositionWeights = new float[_numAttackAngles * _numAttackDistances];
            TargetShip = null;
            NavTarget = Vector3.zero;
            TargetHeading = Vector3.zero;
            FollowTarget = null;
            FollowTransform = null;
            FollowDist = 0f;
            OrderCallback = null;
            DoNavigate = false;
            DoFollow = false;
            CyclesToRecomputePath = 0;
        }

        public Ship ControlledShip;
        public NavigationGuide NavGuide;
        public ShipAIController.ShipControlType ControlType;
        public ShipAIController.ShipActivity CurrActivity;
        public ShipAIController.ShipAttackPattern AttackPattern;
        public Vector3[] AttackPositions;
        public float[] AttackPositionWeights;
        public ShipBase TargetShip;
        public Vector3 NavTarget;
        public Vector3 TargetHeading;
        public ShipBase FollowTarget;
        public Transform FollowTransform;
        public float FollowDist;
        public Action OrderCallback;
        public bool DoNavigate;
        public bool DoFollow;
        public int CyclesToRecomputePath;
    }

    private Collider[] _collidersCache = new Collider[1024];
    private static readonly WaitForEndOfFrame _navDelay = new WaitForEndOfFrame();
    private static readonly WaitForSeconds _targetAcquirePulseDelay = new WaitForSeconds(0.25f);
    private static readonly int _numAttackAngles = 12;
    private static readonly int _numAttackDistances = 3;
}
