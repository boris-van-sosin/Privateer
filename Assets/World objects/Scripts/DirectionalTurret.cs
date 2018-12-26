using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class DirectionalTurret : TurretBase
{
    protected override void SetDefaultAngle()
    {
        _defaultDirection = _containingShip.transform.InverseTransformDirection(-transform.forward);
    }

    protected override void Update()
    {
        if (CanRotate && Mode != TurretMode.Off)
        {
            float maxRotation = RotationSpeed * Time.deltaTime;
            float angleToRotate = Mathf.Abs(Mathf.MoveTowardsAngle(0, _targetAngle - CurrLocalAngle, maxRotation));
            transform.localRotation = transform.localRotation * Quaternion.AngleAxis(angleToRotate * _rotationDir, TurretAxisVector);
        }
        base.Update();
    }

    public override void ManualTarget(Vector3 target)
    {
        if (!_initialized || !CanRotate)
        {
            return;
        }

        _vectorToTarget = target - transform.position;
        Vector3 flatVec = new Vector3(_vectorToTarget.x, 0, _vectorToTarget.z);
        float relativeAngle = GlobalDirToShipHeading(flatVec);
        //Debug.Log(string.Format("Angle to target: {0}", relativeAngle));
        _isLegalAngle = false;
        float closestLegalAngle = 0.0f, angleDiff = 360.0f;
        foreach (Tuple<float, float> r in _rotationAllowedRanges)
        {
            if (r.Item1 <= relativeAngle && relativeAngle <= r.Item2)
            {
                _isLegalAngle = true;
                _targetAngle = relativeAngle;
                break;
            }
            else
            {
                float diff1, diff2;
                if ((diff1 = Mathf.Abs(r.Item1 - relativeAngle)) < angleDiff)
                {
                    angleDiff = diff1;
                    closestLegalAngle = r.Item1;
                }
                if ((diff2 = Mathf.Abs(r.Item2 - relativeAngle)) < angleDiff)
                {
                    angleDiff = diff2;
                    closestLegalAngle = r.Item2;
                }
            }
        }
        if (!_isLegalAngle)
        {
            _targetAngle = closestLegalAngle;
        }

        float currLocal = CurrLocalAngle;
        if (_minRotation < _maxRotation)
        {
            if (_minRotation == 0.0f && _maxRotation == 360.0f)
            {
                //Debug.Log(string.Format("Target angle: {0} Current Angle {1}", _targetAngle, currLocal));
                if (Mathf.Abs(_targetAngle - currLocal) <= 180.0f)
                {
                    _rotationDir = Mathf.Sign(_targetAngle - currLocal);
                }
                else
                {
                    _rotationDir = -Mathf.Sign(_targetAngle - currLocal);
                }
            }
            else
            {
                float currFixed = (currLocal == 360.0f) ? 0f : currLocal;
                _rotationDir = Mathf.Sign(_targetAngle - currFixed);
            }
        }
        else
        {
            if (_maxRotation < currLocal && _maxRotation < _targetAngle)
            {
                _rotationDir = Mathf.Sign(_targetAngle - currLocal);
            }
            else if (currLocal < _minRotation && _targetAngle < _minRotation)
            {
                _rotationDir = Mathf.Sign(_targetAngle - currLocal);
            }
            else
            {
                _rotationDir = Mathf.Sign(-_targetAngle + currLocal);
            }
        }
        if (_flippedX)
        {
            _rotationDir = -_rotationDir;
        }
    }

    protected override Vector3 GetFiringVector(Vector3 vecToTarget)
    {
        return vecToTarget - (Muzzles[_nextBarrel].right * Vector3.Dot(Muzzles[_nextBarrel].right, vecToTarget));
    }

    protected override ITargetableEntity AcquireTarget()
    {
        Collider[] colliders = Physics.OverlapSphere(transform.position, MaxRange * 1.05f, ObjectFactory.AllTargetableLayerMask);
        ITargetableEntity foundTarget = null;
        foreach (Collider c in colliders)
        {
            Ship s;
            Torpedo t;
            if ((s = Ship.FromCollider(c)) != null)
            {
                if (s.ShipDisabled)
                {
                    continue;
                }
                if (ContainingShip.Owner.IsEnemy(s.Owner))
                {
                    foundTarget = s;
                }
            }
            if ((t = c.GetComponent<Torpedo>()) != null)
            {
                if (t.OriginShip != null && ContainingShip.Owner.IsEnemy(t.OriginShip.Owner))
                {
                    foundTarget = t;
                }
            }
        }
        return foundTarget;
    }

    private float _rotationDir;
}
