using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System;

public class StrikeCraftAIController : ShipAIController
{
    protected override void Start()
    {
        base.Start();
        _controlledCraft = _controlledShip.GetComponent<StrikeCraft>();
        _formation = _controlledCraft.ContainingFormation;
        _formationAI = _formation.GetComponent<StrikeCraftFormationAIController>();
    }

    protected override void Update()
    {
        base.Update();
        if (!_controlledShip.ShipControllable)
        {
            return;
        }
        if (CurrActivity == ShipActivity.StartingRecovering)
        {
            Vector3 vecToRecovery = _recoveryTarget.Item1.position - transform.position;
            Vector3 vecToRecoveryFlat = new Vector3(vecToRecovery.x, 0, vecToRecovery.z);
            if (vecToRecovery.sqrMagnitude <= GlobalDistances.StrikeCraftAIRecoveryDist * GlobalDistances.StrikeCraftAIRecoveryDist)
            {
                Maneuver m = CreateClimbForRecoveryManeuver(transform, _recoveryTarget.Item1.transform, _recoveryTarget.Item3);
                m.OnManeuverFinish += delegate (Maneuver mm)
                {
                    StartCoroutine(BeginRecoveryFinalPhase(mm));
                };
                _controlledCraft.StartManeuver(m);
                CurrActivity = ShipActivity.Recovering;
            }
        }
    }

    private System.Collections.IEnumerator BeginRecoveryFinalPhase(Maneuver mm)
    {
        yield return new WaitForEndOfFrame();
        _controlledCraft.BeginRecoveryFinalPhase(_recoveryTarget.Item2);
        yield return null;
    }

    protected override void AdvanceToTarget()
    {
        Vector3 vecToTarget;
        if (!GetCurrMovementTarget(out vecToTarget))
        {
            return;
        }

        Vector3 heading = transform.up;
        Quaternion qToTarget = Quaternion.LookRotation(vecToTarget, transform.forward);
        Quaternion qHeading = Quaternion.LookRotation(heading, transform.forward);
        float angleToTarget = Vector3.SignedAngle(heading, vecToTarget, Vector3.up);
        bool atRequiredHeaing = false;
        if (angleToTarget > _strikeCraftAngleEps)
        {
            _controlledShip.ApplyTurning(false);
            //Debug.Log("Strike craft turning right");
        }
        else if (angleToTarget < -_strikeCraftAngleEps)
        {
            _controlledShip.ApplyTurning(true);
            //Debug.Log("Strike craft turning left");
        }
        else
        {
            atRequiredHeaing = true;
            //Debug.Log("Strike craft going straight");
        }

        if (vecToTarget.sqrMagnitude <= (GlobalDistances.StrikeCraftAIDistEps * GlobalDistances.StrikeCraftAIDistEps))
        {
            _controlledShip.ApplyBraking();
            if (_controlledShip.ActualVelocity.sqrMagnitude < (GlobalDistances.StrikeCraftAIDistEps * GlobalDistances.StrikeCraftAIDistEps) && atRequiredHeaing)
            {
                _doNavigate = false;
            }
        }
        else
        {
            _controlledShip.TargetSpeed = _controlledShip.MaxSpeed;
        }
    }

    protected override Vector3 AttackPosition(ShipBase enemyShip)
    {
        float minRange = _controlledShip.Turrets.Select(x => x.GetMaxRange).Min();
        Vector3 Front = enemyShip.transform.up.normalized;

        if (enemyShip is StrikeCraft)
        {
            return enemyShip.transform.position - Front * 0.01f * minRange;
        }
        else
        {
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
                for (int j = 0; j < _numAttackDistances; ++j)
                {
                    float dist = minRange * 0.75f * (j + 1) / _numAttackDistances;
                    _attackPositions[k] = enemyShip.transform.position + dir * dist;
                    _attackPositionWeights[k] = currWeight;
                    ++k;
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
            return AttackPosition(_targetShip);
        }
    }

    protected override void NavigateWithoutTarget()
    {
        if (_formationAI.DoMaintainFormation())
        {
            if (_controlledCraft.AheadOfPositionInFormation())
            {
                NavigateTo(_formation.GetPosition(_controlledCraft) + _formation.transform.up * GlobalDistances.StrikeCraftAIAheadOfFormationNavDist);
            }
            else
            {
                NavigateTo(_formation.GetPosition(_controlledCraft) - _formation.transform.up * GlobalDistances.StrikeCraftAIBehindFormationNavDist);
            }
        }
    }

    public void OrderStartNavigatenToHost()
    {
        CurrActivity = ShipActivity.NavigatingToRecovery;
    }

    public void OrderReturnToHost(Tuple<Transform, Transform, Vector3> recoveryPositions)
    {
        if (CurrActivity == ShipActivity.StartingRecovering || CurrActivity == ShipActivity.Recovering)
        {
            return;
        }
        CurrActivity = ShipActivity.StartingRecovering;
        _recoveryTarget = recoveryPositions;
        Vector3 vecToRecovery = recoveryPositions.Item1.position - transform.position;
        float m = vecToRecovery.magnitude;
        if (m < GlobalDistances.StrikeCraftAIRecoveryDist)
        {
            Debug.Log("Too close for recovery?");
            Maneuver ma = CreateClimbForRecoveryManeuver(transform, _recoveryTarget.Item1.transform, _recoveryTarget.Item3);
            ma.OnManeuverFinish += delegate (Maneuver ma2)
            {
                StartCoroutine(BeginRecoveryFinalPhase(ma2));
            };
            _controlledCraft.StartManeuver(ma);
            CurrActivity = ShipActivity.Recovering;
        }
        else
        {
            Vector3 dirToRecovery = vecToRecovery / m;
            SetFollowTarget(recoveryPositions.Item1, GlobalDistances.StrikeCraftAIRecoveryDist * 0.95f);
        }
    }

    private IEnumerable<Tuple<Func<Vector3, Vector3>, Func<Vector3, Vector3>>> PathFixes(BspPath rawPath, Transform currLaunchTr, Transform carrierRecoveryHint, Vector3 carrierVelocity)
    {
        int numPathPts = rawPath.Points.Length;
        Matrix4x4 ptTransform = Matrix4x4.TRS(currLaunchTr.position, currLaunchTr.rotation, Vector3.one);

        float maneuverTime = (carrierRecoveryHint.position - transform.position).magnitude / _controlledCraft.CurrSpeed;
        //Matrix4x4 dirTransform = Matrix4x4.TRS(Vector3.zero, currLaunchTr.rotation, Vector3.one);
        for (int i = 0; i < numPathPts; i++)
        {
            if (i < numPathPts - 2)
            {
                yield return
                    new Tuple<Func<Vector3, Vector3>, Func<Vector3, Vector3>>(
                        p => ptTransform.MultiplyPoint3x4(p),
                        v => ptTransform.MultiplyVector(v));
            }
            else if (i == numPathPts - 2)
            {
                yield return
                    new Tuple<Func<Vector3, Vector3>, Func<Vector3, Vector3>>(
                        p => carrierRecoveryHint.position + (maneuverTime * carrierVelocity) - (GlobalDistances.StrikeCraftAIRecoveryPathFixSize * carrierRecoveryHint.up),
                        v => carrierRecoveryHint.up);
            }
            else if (i == numPathPts - 1)
            {
                yield return
                    new Tuple<Func<Vector3, Vector3>, Func<Vector3, Vector3>>(
                        p => carrierRecoveryHint.position + (maneuverTime * carrierVelocity),
                        v => carrierRecoveryHint.up);
            }
        }
    }

    private Maneuver CreateClimbForRecoveryManeuver(Transform currTr, Transform carrierRecoveryHint, Vector3 carrierVelocity)
    {
        BspPath launchPath = ObjectFactory.GetPath("Strike craft carrier climb");
        Maneuver.BsplinePathSegmnet seg = new Maneuver.BsplinePathSegmnet()
        {
            AccelerationBehavior = null,
            Path = launchPath.ExractLightweightPath(PathFixes(launchPath, currTr, carrierRecoveryHint, carrierVelocity))
        };
        return new Maneuver(seg);
    }

    private Vector3 ForceY(Vector3 v, float y)
    {
        return new Vector3(v.x, y, v.z);
    }

    private static readonly float _strikeCraftAngleEps = 5f;
    private StrikeCraft _controlledCraft;
    private StrikeCraftFormation _formation;
    private StrikeCraftFormationAIController _formationAI;
    private Tuple<Transform, Transform, Vector3> _recoveryTarget;
}
