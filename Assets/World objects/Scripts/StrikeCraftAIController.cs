﻿using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class StrikeCraftAIController : ShipAIController
{
    protected override void Start()
    {
        base.Start();
        _controlledCraft = _controlledShip.GetComponent<StrikeCraft>();
        _formation = _controlledCraft.ContainingFormation;
        _formationAI = _formation.GetComponent<StrikeCraftFormationAIController>();
    }

    protected override void AdvanceToTarget()
    {
        Vector3 vecToTarget = _navTarget - transform.position;
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

        if (vecToTarget.sqrMagnitude <= (_strikeCraftDistEps * _strikeCraftDistEps))
        {
            _controlledShip.ApplyBraking();
            if (_controlledShip.ActualVelocity.sqrMagnitude < (_strikeCraftDistEps * _strikeCraftDistEps) && atRequiredHeaing)
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
                return _formation.GetPosition(_controlledCraft) + _formation.transform.up * 2f;
            }
            else
            {
                return _formation.GetPosition(_controlledCraft) - _formation.transform.up * 0.1f;
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
                NavigateTo(_formation.GetPosition(_controlledCraft) + _formation.transform.up * 2f);
            }
            else
            {
                NavigateTo(_formation.GetPosition(_controlledCraft) - _formation.transform.up * 0.1f);
            }
        }
    }

    private static readonly float _strikeCraftAngleEps = 5f;
    private static readonly float _strikeCraftDistEps = 0.01f;
    private StrikeCraft _controlledCraft;
    private StrikeCraftFormation _formation;
    private StrikeCraftFormationAIController _formationAI;
}
