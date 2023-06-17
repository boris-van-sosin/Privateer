using System;
using UnityEngine;

public abstract class DirectionalTurret : TurretBase
{
    protected override void SetDefaultAngle()
    {
        DefaultDirection = _containingShip.transform.InverseTransformDirection(-transform.forward);
    }

    protected override void Update()
    {
        if (CanRotate && Mode != TurretMode.Off && !_rotationAtIdle)
        {
            float maxRotation = RotationSpeed * Time.deltaTime;
            float angleToRotate = Mathf.Abs(Mathf.MoveTowardsAngle(0, _targetAngle - CurrLocalAngle, maxRotation));
            if (angleToRotate > GlobalOtherConstants.TurretAngleEps)
            {
                transform.localRotation = transform.localRotation * Quaternion.AngleAxis(angleToRotate * _rotationDir, TurretAxisVector);
            }
            else if (Mathf.Abs(CurrLocalAngle - _defaultAngle) < GlobalOtherConstants.TurretAngleEps)
            {
                _rotationAtIdle = true;
            }
        }
        base.Update();
    }

    public override void ManualTarget(Vector3 target, bool idle)
    {
        if (!_initialized || !CanRotate)
        {
            // Do something smart with the legal angle?
            return;
        }

        if (!idle && _rotationAtIdle)
        {
            _rotationAtIdle = false;
        }
        else if (idle && _rotationAtIdle)
        {
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
        for (int i = 0; i < _rotationAllowedRanges.Length; ++i)
        {
            if (_rotationAllowedRanges[i].Item1 - angleTol <= relativeAngle && relativeAngle <= _rotationAllowedRanges[i].Item2 + angleTol)
            {
                _isLegalAimAngle = true;
            }
            if (_rotationAllowedRanges[i].Item1 <= relativeAngle && relativeAngle <= _rotationAllowedRanges[i].Item2)
            {
                _targetAngle = relativeAngle;
                _isLegalFireAngle = true;
                break;
            }
            else
            {
                float diff1, diff2;
                if ((diff1 = Mathf.Abs(_rotationAllowedRanges[i].Item1 - relativeAngle)) < angleDiff)
                {
                    angleDiff = diff1;
                    closestLegalAngle = _rotationAllowedRanges[i].Item1;
                }
                if ((diff2 = Mathf.Abs(_rotationAllowedRanges[i].Item2 - relativeAngle)) < angleDiff)
                {
                    angleDiff = diff2;
                    closestLegalAngle = _rotationAllowedRanges[i].Item2;
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
            for (int i = 0; i < _rotationAllowedRanges.Length; ++i)
            {
                if (_rotationAllowedRanges[i].Item1 - tolerance <= relativeAngle && relativeAngle <= _rotationAllowedRanges[i].Item2 + tolerance)
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
        int numHits = Physics.OverlapSphereNonAlloc(transform.position, MaxRange * 1.05f, _collidersCache, ObjectFactory.NavBoxesAllLayerMask);
        ITargetableEntity foundTarget = null;
        int bestScore = 0;
        for (int i = 0; i < numHits; ++i)
        {
            ShipBase s;
            Torpedo t;
            if ((s = ShipBase.FromCollider(_collidersCache[i])) != null)
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
            else if ((t = Torpedo.FromCollider(_collidersCache[i])) != null)
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
    protected bool _rotationAtIdle = false;

    private float _rotationDir;
}
