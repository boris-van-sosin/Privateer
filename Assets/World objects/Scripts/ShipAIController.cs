using System.Collections;
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
            return;
        }

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

        if (vecToTarget.sqrMagnitude <= (GlobalDistances.ShipAIDistEps * GlobalDistances.ShipAIDistEps))
        {
            _controlledShip.ApplyBraking();
            if (_controlledShip.ActualVelocity.sqrMagnitude < (GlobalDistances.ShipAIDistEps * GlobalDistances.ShipAIDistEps) && atRequiredHeaing)
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

    protected void NavigateTo(Vector3 target)
    {
        NavigateTo(target, null);
    }

    protected void NavigateTo(Vector3 target, OrderCompleteDlg onCompleteNavigation)
    {
        _doFollow = false;

        _navTarget = target;
        Debug.DrawLine(transform.position, _navTarget, Color.red, 0.5f);
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

    protected Vector3? BypassObstacle(Vector3 direction)
    {
        Vector3 directionNormalized = direction.normalized;
        Vector3 rightVec = Quaternion.AngleAxis(90, Vector3.up) * directionNormalized;
        float projectFactor = _controlledShip.ShipLength * 4;
        Vector3 projectedPath = directionNormalized * projectFactor;
        RaycastHit[] hits = Physics.SphereCastAll(transform.position, _controlledShip.ShipWidth * 3.0f, directionNormalized, projectFactor, ObjectFactory.AllTargetableLayerMask);
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
            if (_bypassing && Time.time - _bypassStartedTime > 5f)
            {
                _bypassing = false;
            }
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
                    Vector3? bypassVec = BypassObstacle(attackPos - transform.position);
                    if (bypassVec == null)
                    {
                        NavigateTo(attackPos);
                    }
                    else
                    {
                        NavigateTo(bypassVec.Value);
                        _bypassing = true;
                        _bypassStartedTime = Time.time;
                    }
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

    public delegate void OrderCompleteDlg();

    protected ShipBase _controlledShip;

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
    protected bool _bypassing = false;
    protected float _bypassStartedTime;

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
