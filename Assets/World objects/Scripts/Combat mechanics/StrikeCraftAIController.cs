using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System;

[Obsolete("Individal AI controllers are obsolete. Use ShipsAIController.")]
public class StrikeCraftAIController : ShipAIController
{
    protected override void Start()
    {
        base.Start();
        _controlledCraft = _controlledShip.GetComponent<StrikeCraft>();
        _formation = (StrikeCraftFormation)(_controlledCraft.ContainingFormation);
        _formationAI = _formation.GetComponent<StrikeCraftFormationAIController>();
        ControlType = ShipControlType.Autonomous;
        _hitAndRunPhase = true; // Hit
    }

    protected override void Update()
    {
        base.Update();
        if (!_controlledShip.ShipControllable)
        {
            return;
        }
        if (CurrActivity == ShipActivity.StartingRecovery)
        {
            Vector3 vecToRecovery = _recoveryTarget.RecoveryStart.position - transform.position;
            //Vector3 vecToRecoveryFlat = new Vector3(vecToRecovery.x, 0, vecToRecovery.z);
            if (vecToRecovery.sqrMagnitude <= GlobalDistances.StrikeCraftAIRecoveryDist * GlobalDistances.StrikeCraftAIRecoveryDist)
            {
                RecoveryStartClimb();
            }
        }
    }

    private System.Collections.IEnumerator BeginRecoveryFinalPhase(Maneuver mm)
    {
        yield return _endOfFrameWait;
        _controlledCraft.BeginRecoveryFinalPhase(_recoveryTarget.RecoveryEnd, _recoveryTarget.Idx);
        yield return null;
    }

    protected override void AdvanceToTarget()
    {
        Vector3 vecToTarget;
        if (!GetCurrMovementTarget(out vecToTarget))
        {
            _navGuide.Halt();
            return;
        }

        //_bug0Alg.NavTarget = transform.position + vecToTarget;
    }

    protected override int TargetsToFollowLayerMask => ObjectFactory.NavBoxesAllLayerMask;

    protected override Vector3 AttackPosition(ShipBase enemyShip)
    {
        float minRange = _controlledShip.TurretsGetAttackRange(_turretsAll);
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
            Vector3 vecToTarget = enemyShip.transform.position - transform.position;
            float distToTarget = vecToTarget.magnitude;
            //float turningCircleRadius = MathUtils.TurningCircleRadius(_controlledCraft.CurrSpeed, _controlledCraft.TurnRate);
            if (_hitAndRunPhase && distToTarget < minRange * GlobalDistances.StrikeCraftAIAttackPosRangeHitFactor / _numAttackDistances)
            {
                // Run
                _hitAndRunPhase = false;
            }
            else if (!_hitAndRunPhase && distToTarget > minRange * GlobalDistances.StrikeCraftAIAttackPosRangeRunFactor)
            {
                // Hit
                _hitAndRunPhase = true;
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
                if (_hitAndRunPhase)
                {
                    // Hit
                    for (int j = 0; j < _numAttackDistances; ++j)
                    {
                        float dist = minRange * GlobalDistances.StrikeCraftAIAttackPosRangeHitFactor * (j + 1) / _numAttackDistances;
                        _attackPositions[k] = enemyShip.transform.position + dir * dist;
                        _attackPositionWeights[k] = currWeight;
                        ++k;
                    }
                }
                else
                {
                    // Run
                    for (int j = 0; j < _numAttackDistances; ++j)
                    {
                        float dist = minRange * GlobalDistances.StrikeCraftAIAttackPosRangeRunFactor * (j + 1);
                        _attackPositions[k] = enemyShip.transform.position + dir * dist;
                        _attackPositionWeights[k] = currWeight;
                        ++k;
                    }
                }
            }

            int minPos = 0;
            float minScore = (_attackPositions[minPos] - transform.position).sqrMagnitude * _attackPositionWeights[minPos];
            for (int i = 1; i < _attackPositions.Length; ++i)
            {
                float currScore = (_attackPositions[i] - transform.position).sqrMagnitude * _attackPositionWeights[i];
                if (currScore < minScore)
                {
                    minPos = i;
                    minScore = currScore;
                }
            }
            return _attackPositions[minPos];
        }
    }

    protected override bool TargetToFollow(ShipBase s)
    {
        return true;
    }

    protected override Vector3? AntiClumpNav()
    {
        return null;
    }

    protected override Vector3 NavigationDest(ShipBase targetShip)
    {
        if (_formationAI.DoMaintainFormation())
        {
            if (_controlledCraft.AheadOfPositionInFormation())
            {
                return _formation.GetPosition(_controlledCraft) + _formation.transform.up * GlobalDistances.StrikeCraftAIAheadOfFormationNavDist;
            }
            else
            {
                return _formation.GetPosition(_controlledCraft) - _formation.transform.up * GlobalDistances.StrikeCraftAIBehindFormationNavDist;
            }
        }
        else
        {
            return AttackPosition(TargetShip);
        }
    }

    protected override void NavigateWithoutTarget()
    {
        if (_doFollow)
        {
            Vector3 followVec;
            GetCurrMovementTarget(out followVec);
            NavigateTo(transform.position + followVec);
        }
        else if (_formationAI.DoMaintainFormation())
        {
            Vector3 navTarget;
            if (_controlledCraft.AheadOfPositionInFormation())
            {
                navTarget = _formation.GetPosition(_controlledCraft) + (_formation.transform.up * GlobalDistances.StrikeCraftAIAheadOfFormationNavDist);
                
            }
            else
            {
                navTarget = _formation.GetPosition(_controlledCraft) - (_formation.transform.up * GlobalDistances.StrikeCraftAIBehindFormationNavDist);
            }
            NavigateTo(navTarget);
        }
    }

    protected override Bug0 GenBug0Algorithm()
    {
        return new Bug0(_controlledShip, _controlledShip.ShipLength, _controlledShip.ShipWidth, true);
    }

    protected override void OnDrawGizmos()
    {
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
                if (_hitAndRunPhase)
                {
                    Gizmos.color = Color.red;
                }
                else
                {
                    Gizmos.color = DbgOrange;
                }
                Gizmos.DrawLine(transform.position, _navTarget);
                break;
            case ShipActivity.Following:
                Gizmos.color = Color.red;
                Gizmos.DrawLine(transform.position, _followTarget.transform.position);
                break;
            default:
                break;
        }
    }

    public void OrderStartNavigatenToHost()
    {
        CurrActivity = ShipActivity.NavigatingToRecovery;
    }

    public void OrderReturnToHost(CarrierBehavior.RecoveryTransforms recoveryPositions)
    {
        if (CurrActivity == ShipActivity.StartingRecovery || CurrActivity == ShipActivity.Recovering)
        {
            return;
        }
        CurrActivity = ShipActivity.StartingRecovery;
        _recoveryTarget = recoveryPositions;
        Vector3 vecToRecovery = recoveryPositions.RecoveryStart.position - transform.position;
        float m = vecToRecovery.magnitude;
        if (m < GlobalDistances.StrikeCraftAIRecoveryDist)
        {
            Debug.Log("Too close for recovery?");
            RecoveryStartClimb();
        }
        else
        {
            Vector3 dirToRecovery = vecToRecovery / m;
            SetFollowTransform(recoveryPositions.RecoveryStart, GlobalDistances.StrikeCraftAIRecoveryDist * GlobalDistances.StrikeCraftAICarrierFollowDistFactor);
        }
    }

    private void RecoveryStartClimb()
    {
        Vector3 carrierVelocity = _formation.HostCarrier.Velocity;
        if (_formation.HostCarrier.RecoveryTryStartSingle(_controlledCraft, _recoveryTarget.Idx, _controlledCraft.OnRecoveryHangerOpen))
        {
            _controlledCraft.IgnoreHits = true;
            Maneuver m = CreateClimbForRecoveryManeuver(transform, _recoveryTarget.RecoveryStart.transform, carrierVelocity);
            m.OnManeuverFinish += delegate (Maneuver mm)
            {
                StartCoroutine(BeginRecoveryFinalPhase(mm));
            };
            _controlledCraft.StartManeuver(m);
            CurrActivity = ShipActivity.Recovering;
        }
    }

    private IEnumerable<ValueTuple<Func<Vector3, Vector3>, Func<Vector3, Vector3>>> PathFixes(BspPath rawPath, Transform currLaunchTr, Transform carrierRecoveryHint, float speedHint, Vector3 carrierVelocity)
    {
        int numPathPts = rawPath.Points.Length;
        Matrix4x4 ptTransform = Matrix4x4.TRS(currLaunchTr.position, currLaunchTr.rotation, Vector3.one);

        float maneuverTime = (carrierRecoveryHint.position - transform.position).magnitude / speedHint;
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

    private Maneuver CreateClimbForRecoveryManeuver(Transform currTr, Transform carrierRecoveryHint, Vector3 carrierVelocity)
    {
        BspPath launchPath = ObjectFactory.GetPath("Strike craft carrier climb");
        //float expectedTime = (carrierRecoveryHint.position - transform.position).magnitude / _controlledCraft.CurrSpeed;
        float speedHint = _controlledCraft.CurrSpeed;
        Maneuver.AccelerationModifier acc = null;
        if (_controlledCraft.CurrSpeed < _controlledCraft.MaxSpeed * 0.75f)
        {
            acc = new Maneuver.SelfAccelerateToTargetSpeedFraction() { TargetSpeedFrac = 0.75f };
            speedHint = _controlledCraft.MaxSpeed * 0.75f;
        }

        // DEBUG:
        if (acc == null)
        {
            Debug.LogFormat("Creating recovery climb path. Initial speed: {0}. Will not accelerate.", _controlledCraft.CurrSpeed);
        }
        else
        {
            Debug.LogFormat("Creating recovery climb path. Initial speed: {0}. Will accelerate to {1}.", _controlledCraft.CurrSpeed, speedHint);
        }

        Maneuver.BsplinePathSegmnet seg = new Maneuver.BsplinePathSegmnet()
        {
            AccelerationBehavior = acc,
            Path = launchPath.ExractLightweightPath(PathFixes(launchPath, currTr, carrierRecoveryHint, speedHint, carrierVelocity))
        };
        return new Maneuver(seg);
    }

    private Vector3 ForceY(Vector3 v, float y)
    {
        return new Vector3(v.x, y, v.z);
    }

    void OnDestroy()
    {
        if (_navGuide != null)
        {
            Destroy(_navGuide.gameObject);
        }
    }

    private StrikeCraft _controlledCraft;
    private StrikeCraftFormation _formation;
    private StrikeCraftFormationAIController _formationAI;
    private CarrierBehavior.RecoveryTransforms _recoveryTarget;
    private bool _hitAndRunPhase;

    private static readonly WaitForEndOfFrame _endOfFrameWait = new WaitForEndOfFrame();

    private static readonly Color DbgOrange = new Color(200, 200, 0);
}
