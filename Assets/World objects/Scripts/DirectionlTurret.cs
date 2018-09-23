using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class DirectionlTurret : TurretBase
{
    protected override void SetDefaultAngle()
    {
        _defaultDirection = _containingShip.transform.InverseTransformDirection(-transform.forward);
    }

    protected override void Update()
    {
        if (CanRotate)
        {
            float maxRotation = RotationSpeed * Time.deltaTime;
            //Debug.Log(string.Format("Turret angle: global: {0} local: {1} target (global): {2}", CurrAngle, CurrLocalAngle, _globalTargetAngle));
            if (Mathf.Abs(_globalTargetAngle - CurrAngle) < maxRotation)
            {
                switch (TurretAxis)
                {
                    case RotationAxis.XAxis:
                        transform.rotation = Quaternion.Euler(_globalTargetAngle, transform.rotation.eulerAngles.y, transform.rotation.z);
                        break;
                    case RotationAxis.YAxis:
                        transform.rotation = Quaternion.Euler(transform.rotation.eulerAngles.x, _globalTargetAngle, transform.rotation.z);
                        break;
                    case RotationAxis.ZAxis:
                        transform.rotation = Quaternion.Euler(transform.rotation.eulerAngles.x, transform.rotation.y, _globalTargetAngle);
                        break;
                    default:
                        break;
                }
            }
            else
            {
                transform.rotation = transform.rotation * Quaternion.AngleAxis(maxRotation * _rotationDir, TurretAxisVector);
            }
        }
        base.Update();
    }

    public override void ManualTarget(Vector3 target)
    {
        base.ManualTarget(target);
        if (!_initialized || !CanRotate)
        {
            return;
        }

        _vectorToTarget = target - transform.position;
        Vector3 flatVec = new Vector3(_vectorToTarget.x, 0, _vectorToTarget.z);
        float angleToTarget = Quaternion.LookRotation(-flatVec).eulerAngles.y;
        float relativeAngle = AngleToShipHeading(angleToTarget);
        //Debug.Log(string.Format("Angle to target: {0}", relativeAngle));
        _isLegalAngle = false;
        float closestLegalAngle = 0.0f, angleDiff = 360.0f;
        foreach (Tuple<float, float> r in _rotationAllowedRanges)
        {
            if (r.Item1 < relativeAngle && relativeAngle < r.Item2)
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
        _globalTargetAngle = AngleToShipHeading(_targetAngle, true);

        float currLocal = CurrLocalAngle;
        if (_minRotation < _maxRotation)
        {
            if (_minRotation == 0.0f && _maxRotation == 360.0f)
            {
                if (Mathf.Abs(_globalTargetAngle - CurrAngle) <= 180.0f)
                {
                    _rotationDir = Mathf.Sign(_globalTargetAngle - CurrAngle);
                }
                else
                {
                    _rotationDir = -Mathf.Sign(_globalTargetAngle - CurrAngle);
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
    }

    protected override Vector3 GetFiringVector(Vector3 vecToTarget)
    {
        return vecToTarget - (Muzzles[_nextBarrel].right * Vector3.Dot(Muzzles[_nextBarrel].right, vecToTarget));
    }

    protected override Ship AcquireTarget()
    {
        Collider[] colliders = Physics.OverlapSphere(transform.position, MaxRange * 1.05f);
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
            if (ContainingShip.Owner.IsEnemy(s.Owner))
            {
                foundTarget = s;
            }
        }
        return foundTarget;
    }

    private float _rotationDir;
}
