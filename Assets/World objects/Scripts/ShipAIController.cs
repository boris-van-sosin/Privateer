using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class ShipAIController : MonoBehaviour
{

	// Use this for initialization
	void Start ()
    {
        _controlledShip = GetComponent<Ship>();
        StartCoroutine(AcquireTargetPulse());
	}
	
	// Update is called once per frame
	void Update ()
    {
        if (_doNavigate)
        {
            AdvanceToTarget();
        }
	}

    private void AcquireTarget()
    {
        Collider[] colliders = Physics.OverlapSphere(transform.position, 30);
        Ship foundTarget = null;
        foreach (Collider c in colliders)
        {
            Ship s = c.GetComponent<Ship>();
            if (s == null)
            {
                continue;
            }
            else if (s.ShipDisabled)
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
            _targetShip = foundTarget;
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
        if (_targetShip != null && (_targetShip.ShipDisabled || (_targetShip.transform.position - transform.position).sqrMagnitude > 50 * 50))
        {
            _targetShip = null;
        }
    }

    private Vector3 AttackPosition(Ship enemmyShip)
    {
        float minRange = _controlledShip.Turrets.Select(x => x.GetMaxRange).Min();
        Vector3 Front = enemmyShip.transform.up.normalized;
        //Vector3 Left = enemmyShip.transform.right.normalized * minRange * 0.95f;
        //Vector3 Right = -Left;
        //Vector3 Rear = -Front;
        int numAngles = 12;
        int numDistances = 3;
        List<Vector3> positions = new List<Vector3>(numAngles * numDistances);
        for (int i = 0; i < numAngles; ++i)
        {
            Vector3 dir = Quaternion.AngleAxis((float)i / numAngles * 360, Vector3.up) * Front;
            for (int j = 0; j < numDistances; ++j)
            {
                float dist = minRange * 0.95f * (j + 1) / numDistances;
                positions.Add(dir * dist);
            }
        }

        int minPos = 0;
        float minDist = (positions[minPos] - transform.position).sqrMagnitude;
        for (int i = 1; i < positions.Count; ++i)
        {
            float currDist = (positions[i] - transform.position).sqrMagnitude;
            if (currDist < minDist)
            {
                minPos = i;
                minDist = currDist;
            }
        }
        return enemmyShip.transform.position + positions[minPos];
    }

    private void AdvanceToTarget()
    {
        Vector3 vecToTarget = _navTarget - transform.position;
        Vector3 heading = transform.up;
        Quaternion qToTarget = Quaternion.LookRotation(vecToTarget, transform.forward);
        Quaternion qHeading = Quaternion.LookRotation(heading, transform.forward);
        float angleToTarget = Quaternion.FromToRotation(heading, vecToTarget).eulerAngles.y;
        bool atRequiredHeaing = false;
        if (angleToTarget > 180 && angleToTarget < 360 -_angleEps)
        {
            _controlledShip.ApplyTurning(true);
        }
        else if (angleToTarget < 180 && angleToTarget > _angleEps)
        {
            _controlledShip.ApplyTurning(false);
        }
        else
        {
            atRequiredHeaing = true;
        }

        if (vecToTarget.sqrMagnitude <= (_distEps * _distEps))
        {
            _controlledShip.ApplyBraking();
            if (_controlledShip.ActualVelocity.sqrMagnitude < (_distEps * _distEps) && atRequiredHeaing)
            {
                _doNavigate = false;
            }
        }
        else
        {
            if (Vector3.Dot(vecToTarget, heading) > 0)
            {
                _controlledShip.MoveForeward();
            }
            else
            {
                _controlledShip.MoveBackward();
            }
        }
    }

    private void NavigateTo(Vector3 target)
    {
        _navTarget = target;
        Debug.DrawLine(transform.position, _navTarget, Color.red, 1);
        _doNavigate = true;
    }

    private IEnumerator AcquireTargetPulse()
    {
        yield return new WaitForSeconds(0.25f);
        while (true)
        {
            if (!_controlledShip.ShipDisabled)
            {
                if (_targetShip == null)
                {
                    AcquireTarget();
                }
                if (_targetShip != null)
                {
                    NavigateTo(AttackPosition(_targetShip));
                }
            }
            yield return new WaitForSeconds(0.25f);
        }
    }

    private Ship _controlledShip;

    private Ship _targetShip = null;
    private Vector3 _navTarget;
    private Vector3 _targetHeading;
    private static readonly float _angleEps = 0.1f;
    private static readonly float _distEps = 0.01f;
    private bool _doNavigate = false;
}
