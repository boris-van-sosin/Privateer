using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
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
            // Do something smarted with the legal angle?
            return;
        }

        _vectorToTarget = target - transform.position;
        Vector3 flatVec = new Vector3(_vectorToTarget.x, 0, _vectorToTarget.z);

        if (Vector3.Angle(-transform.forward, flatVec) < GlobalOtherConstants.TurretAngleEps)
        {
            return;
        }

        float relativeAngle = GlobalDirToShipHeading(flatVec);
        //Debug.LogFormat("Angle to target: {0}", relativeAngle);
        _isLegalFireAngle = _isLegalAimAngle = false;
        float angleTol = _treatAsFixed ? _targetingFiringArcToleranceLarge : _targetingFiringArcToleranceSmall;
        float closestLegalAngle = 0.0f, angleDiff = 360.0f;
        foreach (ValueTuple<float, float> r in _rotationAllowedRanges)
        {
            if (r.Item1 - angleTol <= relativeAngle && relativeAngle <= r.Item2 + angleTol)
            {
                _isLegalAimAngle = true;
            }
            if (r.Item1 <= relativeAngle && relativeAngle <= r.Item2)
            {
                _targetAngle = relativeAngle;
                _isLegalFireAngle = true;
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
        if (!_isLegalFireAngle)
        {
            _targetAngle = closestLegalAngle;
        }

        float currLocal = CurrLocalAngle;
        if (_minRotation < _maxRotation)
        {
            if (_minRotation == 0.0f && _maxRotation == 360.0f)
            {
                //Debug.LogFormat("Target angle: {0} Current Angle {1}", _targetAngle, currLocal);
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

    protected override bool TargetInFiringArc(Vector3 target, float tolerance)
    {
        if (!_initialized)
        {
            return false;
        }

        Vector3 vecToTarget = target - transform.position;
        Vector3 flatVec = new Vector3(vecToTarget.x, 0, vecToTarget.z);
        if (CanRotate)
        {
            float relativeAngle = GlobalDirToShipHeading(flatVec);
            foreach (ValueTuple<float, float> r in _rotationAllowedRanges)
            {
                if (r.Item1 - tolerance <= relativeAngle && relativeAngle <= r.Item2 + tolerance)
                {
                    return true;
                }
            }
        }
        else
        {
            return Vector3.Angle(flatVec, Muzzles[_nextBarrel].up) <= tolerance;
        }
        return false;
    }

    protected override Vector3 GetFiringVector(Vector3 vecToTarget)
    {
        Vector3 preciseVec = Vector3.ProjectOnPlane(vecToTarget, Muzzles[_nextBarrel].right);
        float computedInaccuracy = Mathf.Clamp(Inaccuracy * _inaccuracyCoeff, 0f, _maxInaccuracy);
        if (computedInaccuracy == 0f)
        {
            return preciseVec;
        }
        else
        {
            float currInaccuracy = UnityEngine.Random.Range(-computedInaccuracy, computedInaccuracy);
            Quaternion q = Quaternion.AngleAxis(currInaccuracy, _containingShip.transform.up);
            return q * preciseVec;
        }
    }

    protected override ITargetableEntity AcquireTarget()
    {
        Collider[] colliders = Physics.OverlapSphere(transform.position, MaxRange * 1.05f, ObjectFactory.NavBoxesAllLayerMask);
        ITargetableEntity foundTarget = null;
        int bestScore = 0;
        foreach (Collider c in colliders)
        {
            ShipBase s;
            Torpedo t;
            if ((s = ShipBase.FromCollider(c)) != null)
            {
                if (!s.ShipActiveInCombat)
                {
                    continue;
                }
                if (ContainingShip.Owner.IsEnemy(s.Owner))
                {
                    int currScore = TargetScore(s);
                    if (foundTarget == null || currScore > bestScore)
                    {
                        foundTarget = s;
                        bestScore = currScore;
                    }
                }
            }
            else if ((t = Torpedo.FromCollider(c)) != null)
            {
                if (t.OriginShip != null && ContainingShip.Owner.IsEnemy(t.OriginShip.Owner))
                {
                    int currScore = TargetScore(t);
                    if (foundTarget == null || currScore > bestScore)
                    {
                        foundTarget = t;
                        bestScore = currScore;
                    }
                }
            }
        }
        return foundTarget;
    }

    public float Inaccuracy
    {
        get => _inaccuracy;
        set => _inaccuracy = Mathf.Clamp(value, 0f, _maxInaccuracy);
    }

    private float _inaccuracy;
    protected float _inaccuracyCoeff = 1f;
    protected static float _maxInaccuracy = 45f;

    private float _rotationDir;
}
