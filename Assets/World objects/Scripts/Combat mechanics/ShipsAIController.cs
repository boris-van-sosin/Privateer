using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;

public class ShipsAIController : MonoBehaviour
{
    void Start()
    {
        _tacticsDelay = new WaitUntil(NextTacticsPulse);
        _nextTacticsPulse = Time.time + 0.01f;
        StartCoroutine(NavUpdate());
        StartCoroutine(TacticsUpdate());
    }

    public static void AddShip(Ship s)
    {
        ShipsAIController instance = GameObject.FindObjectOfType<ShipsAIController>();
        ShipAIData AIData = instance.AddShipInner(s);
        if (AIData != null)
        {
            ShipAIHandle AIHandle = s.GetComponent<ShipAIHandle>();
            AIHandle.AIHandle = instance;
            AIHandle.NavGuide = AIData.NavGuide;
        }
    }

    private ShipAIData AddShipInner(Ship s)
    {
        if (_controlledShipsLookup.ContainsKey(s))
        {
            Debug.LogWarningFormat("Attempted to add ship to AI control more than once: {0}", s);
            return null;
        }

        ShipControlType controlType = ShipControlType.Manual;

        NavigationGuide navGuide = ObjectFactory.CreateNavGuide(s.transform.position, s.transform.forward);
        navGuide.Attach(s);
        navGuide.ManualControl = controlType == ShipControlType.Manual;
        ShipAIData AIData = new ShipAIData(s, navGuide, controlType);
        _controlledShips.Add(AIData);
        _controlledShipsLookup[s] = _controlledShips.Count - 1;
        return AIData;
    }

    public static void ReactivateShip(Ship s, NavigationGuide navGuide)
    {
        ShipsAIController instance = GameObject.FindObjectOfType<ShipsAIController>();
        instance.ReactivateShipInner(s, navGuide);
    }

    private void ReactivateShipInner(Ship s, NavigationGuide navGuide)
    {
        if (navGuide == null || navGuide.AttachedEntity != s)
        {
            Debug.LogWarningFormat("Attempted to reactivate ship {0} with either a null or mis-matched nav guide", s);
            return;
        }

        ShipControlType controlType = ShipControlType.Manual;

        navGuide.ManualControl = controlType == ShipControlType.Manual;
        _controlledShips.Add(new ShipAIData(s, navGuide, controlType));
        _controlledShipsLookup[s] = _controlledShips.Count - 1;
    }

    public static void AddStrikeCraft(StrikeCraft s)
    {
        ShipsAIController instance = GameObject.FindObjectOfType<ShipsAIController>();
        StrikeCraftAIData AIData = instance.AddStrikeCraftInner(s);
        if (AIData != null)
        {
            ShipAIHandle AIHandle = s.GetComponent<ShipAIHandle>();
            AIHandle.AIHandle = instance;
            AIHandle.NavGuide = AIData.NavGuide;
        }
    }

    private StrikeCraftAIData AddStrikeCraftInner(StrikeCraft s)
    {
        if (_controlledShipsLookup.ContainsKey(s))
        {
            Debug.LogWarningFormat("Attempted to add strike craft to AI control more than once: {0}", s);
            return null;
        }

        NavigationGuide navGuide = ObjectFactory.CreateNavGuide(s.transform.position, s.transform.forward);
        navGuide.Attach(s);
        navGuide.ManualControl = false;
        StrikeCraftAIData AIData = new StrikeCraftAIData(s, navGuide);
        _controlledShips.Add(AIData);
        _controlledShipsLookup[s] = _controlledShips.Count - 1;
        return AIData;
    }

    public static void AddStrikeCraftFormation(StrikeCraftFormation f)
    {
        ShipsAIController instance = GameObject.FindObjectOfType<ShipsAIController>();
        StrikeCraftFormationAIData AIData = instance.AddStrikeCraftFormationInner(f);
        if (AIData != null)
        {
            FormationAIHandle AIHandle = f.GetComponent<FormationAIHandle>();
            AIHandle.AIHandle = instance;
            AIHandle.NavGuide = AIData.NavGuide;
        }
    }

    private StrikeCraftFormationAIData AddStrikeCraftFormationInner(StrikeCraftFormation f)
    {
        if (_controlledFormationsLookup.ContainsKey(f))
        {
            Debug.LogWarningFormat("Attempted to add formation to AI control more than once: {0}", f);
            return null;
        }

        NavigationGuide navGuide = ObjectFactory.CreateNavGuide(f.transform.position, f.transform.forward);
        navGuide.Attach(f);
        navGuide.ManualControl = false;
        StrikeCraftFormationAIData AIData = new StrikeCraftFormationAIData(f, navGuide);
        _controlledFormations.Add(AIData);
        _controlledFormationsLookup[f] = _controlledFormations.Count - 1;
        return AIData;
    }

    private IEnumerator NavUpdate()
    {
        yield return _navDelay;
        while (true)
        {
            int i = 0;
            while (i < _controlledShips.Count)
            {
                ShipAIData shipAI = _controlledShips[i];
                try
                {
                    if ((shipAI.ControlledShip == null) || shipAI.ControlledShip.ShipDisabled)
                    {
                        _controlledShips.RemoveAt(i);
                        _controlledShipsLookup.Remove(shipAI.ControlledShip);
                        for (int j = 0; j < _controlledShips.Count; j++)
                        {
                            _controlledShipsLookup[_controlledShips[j].ControlledShip] = j;
                        }
                        continue;
                    }

                    if (!shipAI.IsStrikeCraft)
                    {
                        if (shipAI.DoNavigate || shipAI.DoFollow)
                        {
                            AdvanceToTarget(shipAI);
                        }
                    }
                    else if (shipAI is StrikeCraftAIData strikeCraftAI)
                    {
                        StrikeCraftNavigateStep(strikeCraftAI);
                    }
                }
                catch (Exception exc)
                {
                    Debug.LogWarningFormat("Got exception for navigation: {0}", exc);
                }
                ++i;
                yield return _navDelay;
            }
            
            i = 0;
            while (i < _controlledFormations.Count)
            {
                StrikeCraftFormationAIData formationAI = _controlledFormations[i];
                try
                {
                    if (formationAI.ControlledFormation == null)
                    {
                        _controlledFormations.RemoveAt(i);
                        _controlledFormationsLookup.Remove(formationAI.ControlledFormation);
                        for (int j = 0; j < _controlledFormations.Count; j++)
                        {
                            _controlledFormationsLookup[_controlledFormations[j].ControlledFormation] = j;
                        }
                        continue;
                    }

                    FormationStep(formationAI);
                }
                catch (Exception exc)
                {
                    Debug.LogWarningFormat("Got exception for navigation: {0}", exc);
                }
                ++i;
                yield return _navDelay;
            }

            if (_controlledShips.Count == 0 && _controlledFormations.Count == 0)
            {
                yield return _navDelay;
            }
        }
    }

    private void StrikeCraftNavigateStep(StrikeCraftAIData strikeCraftAI)
    {
        if (strikeCraftAI.NextCycleRecoveryFinalPhase)
        {
            strikeCraftAI.NextCycleRecoveryFinalPhase = false;
            strikeCraftAI.InRecoveryFinalPhase = true;
        }
        else if (strikeCraftAI.InRecoveryFinalPhase)
        {
            strikeCraftAI.ControlledStrikeCraft.BeginRecoveryFinalPhase(strikeCraftAI.RecoveryTarget.RecoveryEnd, strikeCraftAI.RecoveryTarget.Idx);
        }
        else
        {
            if (strikeCraftAI.DoNavigate || strikeCraftAI.DoFollow)
            {
                AdvanceToTarget(strikeCraftAI);
            }
            if (!strikeCraftAI.ControlledStrikeCraft.ShipControllable)
            {
                return;
            }
            if (strikeCraftAI.CurrActivity == ShipActivity.StartingRecovery)
            {
                Vector3 vecToRecovery = strikeCraftAI.RecoveryTarget.RecoveryStart.position - strikeCraftAI.ControlledStrikeCraft.transform.position;
                //Vector3 vecToRecoveryFlat = new Vector3(vecToRecovery.x, 0, vecToRecovery.z);
                if (vecToRecovery.sqrMagnitude <= GlobalDistances.StrikeCraftAIRecoveryDist * GlobalDistances.StrikeCraftAIRecoveryDist)
                {
                    StrikeCraftRecoveryStartClimb(strikeCraftAI);
                }
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
        else if (shipAI.DoFollow && shipAI.CurrActivity == ShipActivity.Following)
        {
            vecToTarget = shipAI.FollowTarget.transform.position - shipAI.ControlledShip.transform.position;
            Vector3 dirToTarget = vecToTarget.normalized;
            vecToTarget -= dirToTarget * shipAI.FollowDist;
            return true;
        }
        else if (shipAI.DoFollow && shipAI.CurrActivity != ShipActivity.Following)
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

    public void NavigateTo(ShipBase s, Vector3 target)
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
        ai.CurrActivity = ShipActivity.ControllingPosition;
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

    public void NavigateTo(StrikeCraftFormation formation, Vector3 target)
    {
        int idx;
        if (!_controlledFormationsLookup.TryGetValue(formation, out idx))
        {
            return;
        }
        NavigateTo(_controlledShips[idx], target, null);
    }

    private static void NavigateTo(StrikeCraftFormationAIData formationAI, Vector3 target, Action onCompleteNavigation)
    {
        formationAI.FollowTarget = null;
        formationAI.DoFollow = false;
        formationAI.NavTarget = target;
        formationAI.OrderCallback = onCompleteNavigation;
        formationAI.NavGuide.SetDestination(target);
        formationAI.DoNavigate = true;
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
        ai.CurrActivity = ShipActivity.Following;
    }

    private void SetFollowTarget(BaseAIData AI, ShipBase followTarget, float dist)
    {
        // Cancel navigate order, if there is one:
        AI.DoNavigate = false;
        AI.OrderCallback = null;

        AI.FollowTarget = followTarget;
        AI.FollowTransform = null;
        AI.FollowDist = dist;
        AI.DoFollow = true;
    }

    private void SetFollowTransform(BaseAIData AI, Transform followTr, float dist)
    {
        // Cancel navigate order, if there is one:
        AI.DoNavigate = false;
        AI.OrderCallback = null;

        AI.FollowTarget = null;
        AI.FollowTransform = followTr;
        AI.FollowDist = dist;
        AI.DoFollow = true;
    }

    private IEnumerator TacticsUpdate()
    {
        yield return _tacticsDelay;
        while (true)
        {
            _nextTacticsPulse = Time.time + 0.25f;
            int i = 0;
            while (i < _controlledShips.Count)
            {
                ShipAIData shipAI = _controlledShips[i];
                try
                {
                    if ((shipAI.ControlledShip == null) || !shipAI.ControlledShip.ShipControllable)
                    {
                        ++i;
                        continue;
                    }

                    switch (shipAI.ControlType)
                    {
                        case ShipControlType.SemiAutonomous:
                            SemiAutonomousBehaviorPulse(shipAI);
                            break;
                        case ShipControlType.Autonomous:
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

            i = 0;
            while (i < _controlledFormations.Count)
            {
                try
                {
                    StrikeCraftFormationAIData formationAI = _controlledFormations[i];
                    if (formationAI.ControlledFormation == null)
                    {
                        ++i;
                        continue;
                    }
                    FormationPulse(formationAI);
                }
                catch (Exception exc)
                {
                    Debug.LogWarningFormat("Got exception for tactics: {0}", exc);
                }
                yield return _navDelay;
                ++i;
            }
            yield return _tacticsDelay;
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
                    if (!shipAI.IsStrikeCraft)
                    {
                        switch (shipAI.AttackPattern)
                        {
                            case ShipAttackPattern.Aggressive:
                                NormalAttackBehavior(shipAI);
                                break;
                            case ShipAttackPattern.Artillery:
                                ArtilleryAttackBehavior(shipAI);
                                break;
                            case ShipAttackPattern.HitAndRun:
                                HitAndRunBehavior(shipAI);
                                break;
                            default:
                                break;
                        }
                    }
                    else
                    {
                        if (shipAI.TargetShip != null)
                        {
                            StrikeCraftAttackPosition(shipAI as StrikeCraftAIData);
                        }
                        {
                            NavigateWithoutTarget(shipAI as StrikeCraftAIData);
                        }
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
                    if (shipAI is StrikeCraftAIData strikeCraftAI)
                    {
                        NavigateWithoutTarget(strikeCraftAI);
                    }
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
                if (shipAI.CurrActivity == ShipActivity.Following)
                {
                    NavigateInFormation(shipAI);
                }
                else
                {
                    if (shipAI is StrikeCraftAIData strikeCraftAI)
                    {
                        NavigateWithoutTarget(strikeCraftAI);
                    }
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
                    Vector3 threatVec = (shipAI.ControlledShip.transform.position - currShip.transform.position).normalized * threatRange;
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
            shipAI.CurrActivity = ShipActivity.ControllingPosition;
        }
        else if (!shipAI.FollowTarget.ShipActiveInCombat)
        {
            ShipBase nextFollowTarget = null;
            int followingShipIdx;
            if (shipAI.FollowTarget is Ship followingShip && _controlledShipsLookup.TryGetValue(followingShip, out followingShipIdx))
            {
                ShipAIData followingShipAI = _controlledShips[followingShipIdx];
                if (followingShipAI.CurrActivity == ShipActivity.Following)
                {
                    nextFollowTarget = followingShipAI.FollowTarget;
                }
            }
            shipAI.FollowTarget = nextFollowTarget;
            if (shipAI.FollowTarget == null)
            {
                shipAI.DoFollow = false;
                shipAI.CurrActivity = ShipActivity.ControllingPosition;
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
        shipAI.AttackPattern = ShipAttackPattern.Aggressive;
        for (int i = 0; i < numHits; ++i)
        {
            int colliderLayer = 1 << _collidersCache[i].gameObject.layer;
            bool validFollowTarget = shipAI.IsStrikeCraft ? ((colliderLayer & TargetsToAttackLayerMask) != 0) : ((colliderLayer & TargetsToFollowLayerMask) != 0);
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
                        shipAI.AttackPattern = ShipAttackPattern.HitAndRun;
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
        return shipAI.IsStrikeCraft || target is Ship;
    }

    private void SemiAutonomousBehaviorPulse(ShipAIData shipAI)
    {
        if (!shipAI.ControlledShip.ShipActiveInCombat)
        {
            return;
        }

        if (shipAI.CyclesToRecomputePath == 0)
        {
            if (shipAI.CurrActivity == ShipActivity.Following)
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

    public ShipControlType GetControlType(Ship s)
    {
        int idx;
        if (_controlledShipsLookup.TryGetValue(s, out idx))
        {
            return _controlledShips[idx].ControlType;
        }
        else
        {
            return ShipControlType.SemiAutonomous;
        }   
    }

    public void SetControlType(Ship s, ShipControlType controlType)
    {
        int idx;
        if (!_controlledShipsLookup.TryGetValue(s, out idx))
        {
            return;
        }
        ShipAIData shipAI = _controlledShips[idx];
        bool changeControlType = shipAI.ControlType != controlType;
        //ShipControlType prevControlType = shipAI.ControlType;
        shipAI.ControlType = controlType;
        if (shipAI.NavGuide != null)
            shipAI.NavGuide.ManualControl = controlType == ShipControlType.Manual;

        if (changeControlType)
        {
            if (shipAI.ControlType == ShipControlType.Manual)
            {
                foreach (ITurret t in shipAI.ControlledShip.Turrets)
                {
                    t.SetTurretBehavior(TurretBase.TurretMode.Manual);
                }
            }
            else if (shipAI.ControlType == ShipControlType.SemiAutonomous)
            {
                foreach (ITurret t in shipAI.ControlledShip.Turrets)
                {
                    t.SetTurretBehavior(TurretBase.TurretMode.Auto);
                }
            }
        }
    }

    public ShipBase GetCurrentTarget(ShipBase s)
    {
        int idx;
        if (!_controlledShipsLookup.TryGetValue(s, out idx))
        {
            return null;
        }
        ShipAIData shipAI = _controlledShips[idx];
        return shipAI.TargetShip;
    }

    #region
    private void StrikeCraftRecoveryStartClimb(StrikeCraftAIData strikeCraftAI)
    {
        Vector3 carrierVelocity = strikeCraftAI.ContainingFormation.HostCarrier.Velocity;
        if (strikeCraftAI.ContainingFormation.HostCarrier.RecoveryTryStartSingle(strikeCraftAI.ControlledStrikeCraft,
                                                                                 strikeCraftAI.RecoveryTarget.Idx,
                                                                                 strikeCraftAI.ControlledStrikeCraft.OnRecoveryHangerOpen))
        {
            strikeCraftAI.ControlledStrikeCraft.IgnoreHits = true;
            Maneuver m = CreateClimbForRecoveryManeuver(strikeCraftAI, strikeCraftAI.RecoveryTarget.RecoveryStart.transform, carrierVelocity);
            m.OnManeuverFinish += (mm => SetNextRecoveryFinalPhase(strikeCraftAI));
            strikeCraftAI.ControlledStrikeCraft.StartManeuver(m);
            strikeCraftAI.CurrActivity = ShipActivity.Recovering;
        }
    }

    private static IEnumerable<ValueTuple<Func<Vector3, Vector3>, Func<Vector3, Vector3>>> PathFixes(Transform currEntityTr, BspPath rawPath, Transform carrierRecoveryHint, float speedHint, Vector3 carrierVelocity)
    {
        int numPathPts = rawPath.Points.Length;
        Matrix4x4 ptTransform = Matrix4x4.TRS(currEntityTr.position, currEntityTr.rotation, Vector3.one);

        float maneuverTime = (carrierRecoveryHint.position - currEntityTr.position).magnitude / speedHint;
        //Matrix4x4 dirTransform = Matrix4x4.TRS(Vector3.zero, currLaunchTr.rotation, Vector3.one);
        for (int i = 0; i < numPathPts; i++)
        {
            if (i < numPathPts - 2)
            {
                yield return
                    new ValueTuple<Func<Vector3, Vector3>, Func<Vector3, Vector3>>(
                        p => ptTransform.MultiplyPoint3x4(p),
                        v => ptTransform.MultiplyVector(v));
            }
            else if (i == numPathPts - 2)
            {
                yield return
                    new ValueTuple<Func<Vector3, Vector3>, Func<Vector3, Vector3>>(
                        p => carrierRecoveryHint.position + (maneuverTime * carrierVelocity) - (GlobalDistances.StrikeCraftAIRecoveryPathFixSize * carrierRecoveryHint.up),
                        v => carrierRecoveryHint.up);
            }
            else if (i == numPathPts - 1)
            {
                yield return
                    new ValueTuple<Func<Vector3, Vector3>, Func<Vector3, Vector3>>(
                        p => carrierRecoveryHint.position + (maneuverTime * carrierVelocity),
                        v => carrierRecoveryHint.up);
            }
        }
    }

    private Maneuver CreateClimbForRecoveryManeuver(StrikeCraftAIData strikeCraftAI, Transform carrierRecoveryHint, Vector3 carrierVelocity)
    {
        BspPath launchPath = ObjectFactory.GetPath("Strike craft carrier climb");
        //float expectedTime = (carrierRecoveryHint.position - transform.position).magnitude / _controlledCraft.CurrSpeed;
        float speedHint = strikeCraftAI.ControlledStrikeCraft.CurrSpeed;
        Maneuver.AccelerationModifier acc = null;
        if (strikeCraftAI.ControlledStrikeCraft.CurrSpeed < strikeCraftAI.ControlledStrikeCraft.MaxSpeed * 0.75f)
        {
            acc = new Maneuver.SelfAccelerateToTargetSpeedFraction() { TargetSpeedFrac = 0.75f };
            speedHint = strikeCraftAI.ControlledStrikeCraft.MaxSpeed * 0.75f;
        }

        // DEBUG:
        if (acc == null)
        {
            Debug.LogFormat("Creating recovery climb path. Initial speed: {0}. Will not accelerate.", strikeCraftAI.ControlledStrikeCraft.CurrSpeed);
        }
        else
        {
            Debug.LogFormat("Creating recovery climb path. Initial speed: {0}. Will accelerate to {1}.", strikeCraftAI.ControlledStrikeCraft.CurrSpeed, speedHint);
        }

        Maneuver.BsplinePathSegmnet seg = new Maneuver.BsplinePathSegmnet()
        {
            AccelerationBehavior = acc,
            Path = launchPath.ExractLightweightPath(PathFixes(strikeCraftAI.ControlledStrikeCraft.transform, launchPath, carrierRecoveryHint, speedHint, carrierVelocity))
        };
        return new Maneuver(seg);
    }

    private static void SetNextRecoveryFinalPhase(StrikeCraftAIData strikeCraftAI)
    {
        strikeCraftAI.NextCycleRecoveryFinalPhase = true;
    }

    public void OrderStartNavigatenToHost(StrikeCraft strikeCraft)
    {
        int idx;
        if (_controlledShipsLookup.TryGetValue(strikeCraft, out idx))
        {
            _controlledShips[idx].CurrActivity = ShipActivity.NavigatingToRecovery;
        }
    }

    public void OrderReturnToHost(StrikeCraft strikeCraft, CarrierBehavior.RecoveryTransforms recoveryPositions)
    {
        int idx;
        if (_controlledShipsLookup.TryGetValue(strikeCraft, out idx))
        {
            StrikeCraftAIData strikeCraftAI = _controlledShips[idx] as StrikeCraftAIData;
            strikeCraftAI.CurrActivity = ShipActivity.NavigatingToRecovery;
            if (strikeCraftAI.CurrActivity == ShipActivity.StartingRecovery ||
                strikeCraftAI.CurrActivity == ShipActivity.Recovering)
            {
                return;
            }
            strikeCraftAI.CurrActivity = ShipActivity.StartingRecovery;
            strikeCraftAI.RecoveryTarget = recoveryPositions;
            Vector3 vecToRecovery = recoveryPositions.RecoveryStart.position - strikeCraft.transform.position;
            float m = vecToRecovery.magnitude;
            if (m < GlobalDistances.StrikeCraftAIRecoveryDist)
            {
                Debug.Log("Too close for recovery?");
                StrikeCraftRecoveryStartClimb(strikeCraftAI);
            }
            else
            {
                Vector3 dirToRecovery = vecToRecovery / m;
                SetFollowTransform(strikeCraftAI, recoveryPositions.RecoveryStart, GlobalDistances.StrikeCraftAIRecoveryDist * GlobalDistances.StrikeCraftAICarrierFollowDistFactor);
            }
        }
    }

    private void NavigateWithoutTarget(StrikeCraftAIData strikeCraftAI)
    {
        if (strikeCraftAI.DoFollow)
        {
            Vector3 followVec;
            GetCurrMovementTarget(strikeCraftAI, out followVec);
            NavigateTo(strikeCraftAI.ControlledStrikeCraft, strikeCraftAI.ControlledStrikeCraft.transform.position + followVec);
        }
        else if (null != strikeCraftAI.ContainingFormation && null != strikeCraftAI.FormationAI && DoMaintainFormation(strikeCraftAI.FormationAI))
        {
            Vector3 navTarget;
            if (strikeCraftAI.ControlledStrikeCraft.AheadOfPositionInFormation())
            {
                navTarget = strikeCraftAI.ContainingFormation.GetPosition(strikeCraftAI.ControlledStrikeCraft) + (strikeCraftAI.ContainingFormation.transform.up * GlobalDistances.StrikeCraftAIAheadOfFormationNavDist);

            }
            else
            {
                navTarget = strikeCraftAI.ContainingFormation.GetPosition(strikeCraftAI.ControlledStrikeCraft) - (strikeCraftAI.ContainingFormation.transform.up * GlobalDistances.StrikeCraftAIBehindFormationNavDist);
            }
            NavigateTo(strikeCraftAI.ControlledStrikeCraft, navTarget);
        }
    }

    private Vector3 StrikeCraftAttackPosition(StrikeCraftAIData strikeCraftAI)
    {
        ShipBase enemyShip = strikeCraftAI.TargetShip;
        float minRange = strikeCraftAI.ControlledStrikeCraft.TurretsGetAttackRange(_turretsAll);
        Vector3 Front = enemyShip.transform.forward;

        if (enemyShip is StrikeCraft)
        {
            return enemyShip.transform.position - Front * GlobalDistances.StrikeCraftAIVsStrikeCrafRangeFactor * minRange;
        }
        else
        {
            //Vector3 Left = enemmyShip.transform.right.normalized * minRange * 0.95f;
            //Vector3 Right = -Left;
            //Vector3 Rear = -Front;
            Vector3 vecToTarget = enemyShip.transform.position - strikeCraftAI.ControlledStrikeCraft.transform.position;
            float distToTarget = vecToTarget.magnitude;
            //float turningCircleRadius = MathUtils.TurningCircleRadius(_controlledCraft.CurrSpeed, _controlledCraft.TurnRate);
            if (strikeCraftAI.HitAndRunPhase && distToTarget < minRange * GlobalDistances.StrikeCraftAIAttackPosRangeHitFactor / _numAttackDistances)
            {
                // Run
                strikeCraftAI.HitAndRunPhase = false;
            }
            else if (!strikeCraftAI.HitAndRunPhase && distToTarget > minRange * GlobalDistances.StrikeCraftAIAttackPosRangeRunFactor)
            {
                // Hit
                strikeCraftAI.HitAndRunPhase = true;
            }

            int k = 0;
            for (int i = 0; i < _numAttackAngles; ++i)
            {
                Vector3 dir = Quaternion.AngleAxis((float)i / _numAttackAngles * 360, Vector3.up) * Front;
                float currWeight;
                if (Vector3.Angle(dir, Front) < 45f)
                {
                    currWeight = 1f / 5f;
                }
                else if (Vector3.Angle(dir, Front) > 135f)
                {
                    currWeight = 1f / 10f;
                }
                else
                {
                    currWeight = 1f / 2.5f;
                }
                if (strikeCraftAI.HitAndRunPhase)
                {
                    // Hit
                    for (int j = 0; j < _numAttackDistances; ++j)
                    {
                        float dist = minRange * GlobalDistances.StrikeCraftAIAttackPosRangeHitFactor * (j + 1) / _numAttackDistances;
                        strikeCraftAI.AttackPositions[k] = enemyShip.transform.position + dir * dist;
                        strikeCraftAI.AttackPositionWeights[k] = currWeight;
                        ++k;
                    }
                }
                else
                {
                    // Run
                    for (int j = 0; j < _numAttackDistances; ++j)
                    {
                        float dist = minRange * GlobalDistances.StrikeCraftAIAttackPosRangeRunFactor * (j + 1);
                        strikeCraftAI.AttackPositions[k] = enemyShip.transform.position + dir * dist;
                        strikeCraftAI.AttackPositionWeights[k] = currWeight;
                        ++k;
                    }
                }
            }

            int minPos = 0;
            float minScore = (strikeCraftAI.AttackPositions[minPos] - strikeCraftAI.ControlledStrikeCraft.transform.position).sqrMagnitude * strikeCraftAI.AttackPositionWeights[minPos];
            for (int i = 1; i < strikeCraftAI.AttackPositions.Length; ++i)
            {
                float currScore = (strikeCraftAI.AttackPositions[i] - strikeCraftAI.ControlledStrikeCraft.transform.position).sqrMagnitude * strikeCraftAI.AttackPositionWeights[i];
                if (currScore < minScore)
                {
                    minPos = i;
                    minScore = currScore;
                }
            }
            return strikeCraftAI.AttackPositions[minPos];
        }
    }

    public void AssignStrikeCraftToFormation(StrikeCraft s, StrikeCraftFormation f)
    {
        int strikeCraftIdx, formationIdx;
        if (_controlledShipsLookup.TryGetValue(s, out strikeCraftIdx) && _controlledFormationsLookup.TryGetValue(f, out formationIdx))
        {
            StrikeCraftAIData strikeCraftAI = _controlledShips[strikeCraftIdx] as StrikeCraftAIData;
            strikeCraftAI.FormationAI = _controlledFormations[formationIdx];
            strikeCraftAI.ContainingFormation = f;
        }
    }
    #endregion

    #region
    private void FormationPulse(StrikeCraftFormationAIData formationAI)
    {
        if (formationAI.ControlledFormation.DestroyOnEmpty && formationAI.ControlledFormation.AllOutOfAmmo())
        {
            CarrierBehavior c = formationAI.ControlledFormation.HostCarrier;
            if (c != null)
            {
                OrderReturnToHost(formationAI.ControlledFormation, c.transform);
                return;
            }
        }
        if (formationAI.TargetShip == null)
        {
            StrikeCraftFormationAcquireTarget(formationAI);
        }
        if (formationAI.TargetShip != null)
        {
            if (formationAI.TargetShip.ShipDisabled)
            {
                formationAI.TargetShip = null;
                return;
            }
            Vector3 attackPos = StrikeCraftFormationAttackPosition(formationAI);
            NavigateTo(formationAI, attackPos, null);
        }
    }

    private static bool DoMaintainFormation(StrikeCraftFormationAIData formationAI)
    {
        return formationAI.CurrState != StrikeCraftFormationState.InCombat;
    }

    private void FormationStep(StrikeCraftFormationAIData formationAI)
    {
        if (formationAI.CurrState == StrikeCraftFormationState.ReturningToHost)
        {
            formationAI.CurrState = StrikeCraftFormationState.Recovering;
            formationAI.DoNavigate = false;
            formationAI.ControlledFormation.HostCarrier.RecoveryTryStart(formationAI.ControlledFormation);
            foreach (StrikeCraft craft in formationAI.ControlledFormation.AllStrikeCraft())
            {
                OrderReturnToHost(craft, formationAI.ControlledFormation.HostCarrier.GetRecoveryTransforms());
            }
        }
        if (formationAI.DoNavigate || formationAI.DoFollow)
        {
            StrikeCraftFormationAdvance(formationAI);
        }
    }

    private void StrikeCraftFormationAdvance(StrikeCraftFormationAIData formationAI)
    {
        Vector3 vecToTarget;
        if (formationAI.DoNavigate)
        {
            vecToTarget = formationAI.NavTarget - formationAI.ControlledFormation.transform.position;
        }
        else if (formationAI.DoFollow)
        {
            Vector3 followPos = GetFollowPosition(formationAI);
            vecToTarget = followPos - formationAI.ControlledFormation.transform.position;
            if (Time.time >= formationAI.NextUpdateFollowTime)
            {
                formationAI.NavGuide.SetDestination(followPos);
                formationAI.NextUpdateFollowTime = Time.time + UnityEngine.Random.Range(1f, 2f);
            }
            else
            {
                if (formationAI.InnerNavAgent.isOnNavMesh && formationAI.InnerNavAgent.remainingDistance < 0.1f)
                {
                    formationAI.FollowPosIdx = 1 - formationAI.FollowPosIdx;
                    formationAI.NextUpdateFollowTime = Time.time;
                }
            }
        }
        else
        {
            return;
        }

        if (formationAI.TargetShip != null && vecToTarget.sqrMagnitude <= (GlobalDistances.StrikeCraftAIAttackDist * GlobalDistances.StrikeCraftAIAttackDist))
        {
            formationAI.CurrState = StrikeCraftFormationState.InCombat;
        }
        else if (vecToTarget.sqrMagnitude <= (GlobalDistances.StrikeCraftAIDistEps * GlobalDistances.StrikeCraftAIDistEps))
        {
            formationAI.ControlledFormation.ApplyBraking();
            if (formationAI.ControlledFormation.ActualVelocity.sqrMagnitude < (GlobalDistances.StrikeCraftAIDistEps * GlobalDistances.StrikeCraftAIDistEps))
            {
                if (formationAI.DoNavigate)
                {
                    formationAI.DoNavigate = false;
                    formationAI.OrderCallback?.Invoke();
                    Debug.LogWarningFormat("Strike craft formation stopped at destination. This is highly unlikely.");
                }
            }
        }
        else
        {
            SetFormationSpeed(formationAI);
        }
    }

    private static Vector3 GetFollowPosition(StrikeCraftFormationAIData formationAI)
    {
        if (formationAI.FollowPosIdx == 0)
        {
            return formationAI.FollowTransform.position + (formationAI.FollowTransform.right * formationAI.FollowDist);
        }
        else
        {
            return formationAI.FollowTransform.position - (formationAI.FollowTransform.right * formationAI.FollowDist);
        }
    }

    private static void SetFormationSpeed(StrikeCraftFormationAIData formationAI)
    {
        if (formationAI.ControlledFormation.AllInFormation())
        {
            formationAI.NavGuide.SetTargetSpeed(formationAI.ControlledFormation.MaxSpeed);
            foreach (StrikeCraft s in formationAI.ControlledFormation.AllStrikeCraft())
            {
                s.TargetSpeed = s.MaxSpeed;
            }
        }
        else
        {
            formationAI.NavGuide.SetTargetSpeed(formationAI.ControlledFormation.MaxSpeed * formationAI.ControlledFormation.MaintainFormationSpeedCoefficient);
            foreach (ValueTuple<StrikeCraft, bool> s in formationAI.ControlledFormation.InFormationStatus())
            {
                if (s.Item2)
                {
                    s.Item1.TargetSpeed = s.Item1.MaxSpeed * formationAI.ControlledFormation.MaintainFormationSpeedCoefficient;
                }
                else if (s.Item1.AheadOfPositionInFormation())
                {
                    s.Item1.TargetSpeed = s.Item1.MaxSpeed * formationAI.ControlledFormation.MaintainFormationSpeedCoefficient * formationAI.ControlledFormation.MaintainFormationSpeedCoefficient;
                }
                else
                {
                    s.Item1.TargetSpeed = s.Item1.MaxSpeed;
                }
            }

        }
    }

    public void OrderReturnToHost(StrikeCraftFormation formation)
    {
        CarrierBehavior c = formation.HostCarrier;
        if (null != c)
        {
            OrderReturnToHost(formation, c.transform);
        }
    }

    public void OrderReturnToHost(StrikeCraftFormation formation, Transform recoveryPosition)
    {
        //
        int idx;
        if (_controlledFormationsLookup.TryGetValue(formation, out idx))
        {
            StrikeCraftFormationAIData formationAI = _controlledFormations[idx];

            formationAI.CurrState = StrikeCraftFormationState.ReturningToHost;
            foreach (StrikeCraft craft in formationAI.ControlledFormation.AllStrikeCraft())
            {
                StrikeCraftAIController ctl = craft.GetComponent<StrikeCraftAIController>();
                if (ctl != null)
                {
                    ctl.OrderStartNavigatenToHost();
                }
            }
            //
            Vector3 vecToRecovery = recoveryPosition.position - formationAI.ControlledFormation.transform.position;
            float m = vecToRecovery.magnitude;
            if (m < GlobalDistances.StrikeCraftAIFormationRecoveryTargetDist)
            {
                bool isFacingHost = Vector3.Dot(vecToRecovery, formationAI.ControlledFormation.transform.up) >= 0f;
                if (!isFacingHost)
                {
                    Vector3 dirToRecovery = vecToRecovery / m;
                    Vector3 halfTurn = Quaternion.AngleAxis(90, Vector3.up) * dirToRecovery;
                    float radius = 1f;
                    NavigateTo(formationAI, formationAI.ControlledFormation.transform.position + radius * halfTurn, null);
                }
            }
            else
            {
                SetFollowTransform(formationAI, recoveryPosition, GlobalDistances.StrikeCraftAIFormationRecoveryTargetDist);
            }
        }
    }

    public void OrderEscort(StrikeCraftFormation formation, ShipBase s)
    {
        int idx;
        if (_controlledFormationsLookup.TryGetValue(formation, out idx))
        {
            StrikeCraftFormationAIData formationAI = _controlledFormations[idx];
            SetFollowTransform(formationAI, s.transform, GlobalDistances.StrikeCraftAIFormationEscortDist);
        }
    }

    private void StrikeCraftFormationAcquireTarget(StrikeCraftFormationAIData formationAI)
    {
        int numHits = Physics.OverlapSphereNonAlloc(formationAI.ControlledFormation.transform.position, GlobalDistances.StrikeCraftAIFormationAggroDist, _collidersCache, ObjectFactory.AllShipsLayerMask);
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
            if (formationAI.ControlledFormation.Owner.IsEnemy(s.Owner))
            {
                foundTarget = s;
            }
        }

        if (foundTarget != null)
        {
            if (foundTarget is Ship)
            {
                formationAI.TargetShip = foundTarget;
            }
        }
        else
        {
            if (formationAI.CurrState == StrikeCraftFormationState.InCombat)
            {
                formationAI.CurrState = StrikeCraftFormationState.Idle;
            }
        }
    }

    private static Vector3 StrikeCraftFormationAttackPosition(StrikeCraftFormationAIData formationAI)
    {
        ShipBase enemyShip = formationAI.TargetShip;
        Vector3 vecToTarget = formationAI.ControlledFormation.transform.position - enemyShip.transform.position;
        Vector3 unitVecToTarget = vecToTarget.normalized;
        return enemyShip.transform.position - unitVecToTarget * GlobalDistances.StrikeCraftAIAttackDist;
    }
    #endregion

    void OnDrawGizmos()
    {
        for (int i = 0; i < _controlledShips.Count; ++i)
        {
            ShipAIData shipAI = _controlledShips[i];
            if ((shipAI.ControlledShip == null) || !shipAI.ControlledShip.ShipControllable)
            {
                continue;
            }

            if (shipAI is StrikeCraftAIData strikeCraftAI)
            {
                if (strikeCraftAI.HitAndRunPhase)
                {
                    Gizmos.color = Color.red;
                }
                else
                {
                    Gizmos.color = DbgOrange;
                }
            }
            else
            {
                Gizmos.color = Color.red;
            }

            switch (shipAI.CurrActivity)
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
                    Gizmos.DrawLine(shipAI.ControlledShip.transform.position, shipAI.NavTarget);
                    break;
                case ShipActivity.Following:
                    Gizmos.DrawLine(shipAI.ControlledShip.transform.position, shipAI.FollowTarget.transform.position);
                    break;
                default:
                    break;
            }
        }
    }

    private bool NextTacticsPulse() => Time.time >= _nextTacticsPulse;

    private Dictionary<ShipBase, int> _controlledShipsLookup = new Dictionary<ShipBase, int>();
    private List<ShipAIData> _controlledShips = new List<ShipAIData>();

    private Dictionary<StrikeCraftFormation, int> _controlledFormationsLookup = new Dictionary<StrikeCraftFormation, int>();
    private List<StrikeCraftFormationAIData> _controlledFormations = new List<StrikeCraftFormationAIData>();

    private abstract class BaseAIData
    {
        public BaseAIData(NavigationGuide navGuide)
        {
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

        public NavigationGuide NavGuide;
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

    private class ShipAIData : BaseAIData
    {
        public ShipAIData(ShipBase s, NavigationGuide navGuide, ShipControlType controlType)
            : base(navGuide)
        {
            ControlledShip = s;
            ControlType = controlType;
            CurrActivity = ShipActivity.Idle;
            AttackPattern = ShipAttackPattern.Aggressive;
        }

        public ShipBase ControlledShip;
        public ShipControlType ControlType;
        public ShipActivity CurrActivity;
        public ShipAttackPattern AttackPattern;
        public virtual bool IsStrikeCraft => false;
    }

    private class StrikeCraftAIData : ShipAIData
    {
        public StrikeCraftAIData(StrikeCraft s, NavigationGuide navGuide) : base(s, navGuide, ShipControlType.Autonomous)
        {
            ControlledStrikeCraft = s;
            ContainingFormation = (StrikeCraftFormation)(s.ContainingFormation);
            HitAndRunPhase = true;
            NextCycleRecoveryFinalPhase = false;
            InRecoveryFinalPhase = false;
        }

        public StrikeCraft ControlledStrikeCraft;
        public StrikeCraftFormation ContainingFormation;
        public StrikeCraftFormationAIData FormationAI;
        public CarrierBehavior.RecoveryTransforms RecoveryTarget;
        public bool HitAndRunPhase;
        public bool NextCycleRecoveryFinalPhase;
        public bool InRecoveryFinalPhase;

        public override bool IsStrikeCraft => true;
    }

    private class StrikeCraftFormationAIData : BaseAIData
    {
        public StrikeCraftFormationAIData(StrikeCraftFormation formation, NavigationGuide navGuide)
            : base(navGuide)
        {
            ControlledFormation = formation;
            CurrState = StrikeCraftFormationState.Idle;
            InnerNavAgent = navGuide.GetComponent<NavMeshAgent>();
        }

        public StrikeCraftFormation ControlledFormation;
        public StrikeCraftFormationState CurrState;
        public int FollowPosIdx;
        public NavMeshAgent InnerNavAgent;
        public float NextUpdateFollowTime;
    }

    private float _nextTacticsPulse = 0;
    private Collider[] _collidersCache = new Collider[1024];
    private static readonly WaitForEndOfFrame _navDelay = new WaitForEndOfFrame();
    private WaitUntil _tacticsDelay;
    private static readonly int _numAttackAngles = 12;
    private static readonly int _numAttackDistances = 3;
    private static readonly Color DbgOrange = new Color(200, 200, 0);
}
