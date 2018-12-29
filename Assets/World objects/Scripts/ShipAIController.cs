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
        if (_controlledShip.ShipDisabled || _controlledShip.HullHitPoints <= 0 || _controlledShip.ShipSurrendered)
        {
            return;
        }

        if (_doNavigate)
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
            if (foundTarget is Ship)
            {
                _targetShip = (Ship)foundTarget;
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
        if (_targetShip != null && (_targetShip.ShipDisabled || (_targetShip.transform.position - transform.position).sqrMagnitude > 50 * 50))
        {
            _targetShip = null;
        }
    }

    private Vector3 AttackPosition(Ship enemyShip)
    {
        float minRange = _controlledShip.Turrets.Select(x => x.GetMaxRange).Min();
        Vector3 Front = enemyShip.transform.up.normalized;
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
        return enemyShip.transform.position + positions[minPos];
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
        Debug.DrawLine(transform.position, _navTarget, Color.red, 0.5f);
        _doNavigate = true;
    }

    private Vector3? BypassObstacle(Vector3 direction)
    {
        Vector3 directionNormalized = direction.normalized;
        Vector3 rightVec = Quaternion.AngleAxis(90, Vector3.up) * directionNormalized;
        float projectFactor = _controlledShip.ShipLength * 2;
        Vector3 projectedPath = directionNormalized * projectFactor;
        RaycastHit[] hits = Physics.CapsuleCastAll(transform.position, transform.position + projectedPath, _controlledShip.ShipWidth * 2.0f, directionNormalized, projectFactor, ObjectFactory.AllTargetableLayerMask);
        bool obstruction = false;
        List<float> dotToCorners = new List<float>(4 * hits.Length);
        float dotMin = -1;
        foreach (RaycastHit h in hits)
        {
            if (h.collider.gameObject == this.gameObject)
            {
                continue;
            }
            Ship other = h.collider.GetComponent<Ship>();
            if (other != null)
            {
                obstruction = true;
                Vector3 obstructionLocation = h.point;
                obstructionLocation.y = 0;
                float obstructionLength = other.ShipLength * 1.1f;
                float obstructionWidth = other.ShipLength * 1.1f;
                Vector3[] otherShipCorners = new Vector3[]
                {
                    other.transform.position + (other.transform.up * obstructionLength) + (other.transform.right * obstructionWidth),
                    other.transform.position + (other.transform.up * obstructionLength) - (other.transform.right * obstructionWidth),
                    other.transform.position - (other.transform.up * obstructionLength) + (other.transform.right * obstructionWidth),
                    other.transform.position - (other.transform.up * obstructionLength) - (other.transform.right * obstructionWidth),
                };
                for (int i = 0; i < otherShipCorners.Length; ++i)
                {
                    Debug.DrawLine(other.transform.position, otherShipCorners[i], Color.cyan, 0.25f);
                    dotToCorners.Add(Vector3.Dot(otherShipCorners[i] - transform.position, rightVec));
                    if (dotMin < 0 || Mathf.Abs(dotToCorners[i]) < dotMin)
                    {
                        dotMin = Mathf.Abs(dotToCorners[i]);
                    }
                }
            }
        }
        if (obstruction)
        {
            int maxRight = -1;
            int maxLeft = -1;
            for (int i = 0; i < dotToCorners.Count; ++i)
            {
                if (dotToCorners[i] > 0 && (maxRight == -1 || dotToCorners[i] > dotToCorners[maxRight]))
                {
                    maxRight = i;
                }
                else if (dotToCorners[i] < 0 && (maxLeft == -1 || dotToCorners[i] < dotToCorners[maxLeft]))
                {
                    maxLeft = i;
                }
            }
            if (maxLeft == -1)
            {
                return (transform.position - (rightVec * dotMin * 2f));
            }
            else if (maxRight == -1)
            {
                return (transform.position + (rightVec * dotMin * 2f));
            }
            else if (-dotToCorners[maxLeft] >= dotToCorners[maxRight])
            {
                return (transform.position + (rightVec * dotToCorners[maxRight] * 2f));
            }
            else if (-dotToCorners[maxLeft] < dotToCorners[maxRight])
            {
                return (transform.position + (rightVec * dotToCorners[maxLeft] * 2f));
            }
        }
        return null;
    }

    private IEnumerator AcquireTargetPulse()
    {
        yield return new WaitForSeconds(0.25f);
        while (true)
        {
            if (!_controlledShip.ShipDisabled && !_controlledShip.ShipSurrendered)
            {
                if (_targetShip == null)
                {
                    AcquireTarget();
                }
                if (_targetShip != null)
                {
                    Vector3 attackPos = AttackPosition(_targetShip);
                    Vector3? bypassVec = BypassObstacle(attackPos);
                    if (bypassVec == null)
                    {
                        NavigateTo(attackPos);
                    }
                    else
                    {
                        NavigateTo(bypassVec.Value);
                    }
                }
            }
            else if (_controlledShip.HullHitPoints == 0 || _controlledShip.ShipSurrendered)
            {
                yield break;
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
