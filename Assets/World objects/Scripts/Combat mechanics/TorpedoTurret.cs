using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System;

public class TorpedoTurret : TurretBase
{
    protected override void SetDefaultAngle()
    {
        DefaultDirection = _containingShip.transform.InverseTransformDirection(transform.up);
    }

    protected override void ParseMuzzles()
    {
        Muzzles = new Transform[] { FindTorpedoBarrel() };
        MuzzleFx = null;
    }
    
    public override void PostInstallTurret(ShipBase s)
    {
        base.PostInstallTurret(s);
        TorpedoHardpoint parentHardpoint;
        if (transform.parent != null && (parentHardpoint = GetComponentInParent<TorpedoHardpoint>()) != null)
        {
            //Vector3 dirInHPSystem = parentHardpoint.transform.TransformDirection(parentHardpoint.LaunchVector);
            _launchDirection = parentHardpoint.LaunchVector;
        }
        else
        {
            _launchDirection = new Vector3(0, 1, 0);
        }
        _torpedoTubeDoorsAnim = GetComponent<GenericOpenCloseAnim>();
        (int, float, float, Warhead) launchData = ObjectFactory.TorpedoLaunchDataFromTorpedoType(LoadedTorpedoType);
        MaxRange = launchData.Item2;
        _torpedoesInSpread = launchData.Item1;
        _torpedoScale = launchData.Item3;
        _warheads[0] = launchData.Item4;
    }

    private Transform FindTorpedoBarrel()
    {
        return FindMuzzles(transform).FirstOrDefault();
    }

    public override void ManualTarget(Vector3 target, bool idle)
    {
        if (!_initialized)
        {
            return;
        }

        _vectorToTarget = target - transform.position;
        Vector3 flatVec = new Vector3(_vectorToTarget.x, 0, _vectorToTarget.z);
        if (flatVec == Vector3.zero)
        {
            return;
        }
        float relativeAngle = GlobalDirToShipHeading(flatVec);
        //Debug.LogFormat("Angle to target: {0}", relativeAngle);
        _isLegalAimAngle = false;
        foreach (ValueTuple<float, float> r in _rotationAllowedRanges)
        {
            if (r.Item1 < relativeAngle && relativeAngle < r.Item2)
            {
                _isLegalAimAngle = true;
                _targetAngle = relativeAngle;
                break;
            }
        }
        //Debug.DrawLine(transform.position + (LaunchVector*0.01f), transform.position + (LaunchVector * 0.01f) - (transform.forward * 0.1f), Color.cyan, Time.deltaTime);
    }

    protected override bool TargetInFiringArc(Vector3 target, float tolerance)
    {
        if (!_initialized)
        {
            return false;
        }

        Vector3 vecToTarget = target - transform.position;
        Vector3 flatVec = new Vector3(vecToTarget.x, 0, vecToTarget.z);

        if (flatVec == Vector3.zero)
        {
            return  false;
        }

        float relativeAngle = GlobalDirToShipHeading(flatVec);
        foreach (ValueTuple<float, float> r in _rotationAllowedRanges)
        {
            if (r.Item1 - tolerance <= relativeAngle && relativeAngle <= r.Item2 + tolerance)
            {
                return true;
            }
        }
        return false;
    }

    protected override bool IsAimedAtTarget()
    {
        return _isLegalAimAngle;
    }

    protected override void FireInner(Vector3 firingVector, int barrelIdx)
    {
        _torpedoTarget = Muzzles[barrelIdx].position + firingVector;
        if (_torpedoTubeDoorsAnim != null)
        {
            _torpedoTubeDoorsAnim.Open(NotifyTorpedoTubeOpenClose);
        }
        else
        {
            TorpedoDoorsOpen = true;
        }
        StartCoroutine(LaunchSpread());
    }

    protected override Vector3 GetFiringVector(Vector3 vecToTarget)
    {
        return vecToTarget;// - (Muzzles[_nextBarrel].up * Vector3.Dot(Muzzles[_nextBarrel].up, vecToTarget));
    }

    protected override bool MuzzleOppositeDirCheck(Transform Muzzle, Vector3 vecToTarget)
    {
        return true;
    }

    protected override void FireGrapplingToolInner(Vector3 firingVector, int barrelIdx)
    {
        throw new System.NotImplementedException();
    }

    protected override ITargetableEntity AcquireTarget()
    {
        Collider[] colliders = Physics.OverlapSphere(transform.position, MaxRange * GlobalDistances.TurretTargetAcquisitionRangeFactor, ObjectFactory.NavBoxesAllLayerMask);
        ITargetableEntity foundTarget = null;
        int bestScore = 0;
        foreach (Collider c in colliders)
        {
            Ship s = ShipBase.FromCollider(c) as Ship;
            if (s == null)
            {
                continue;
            }
            else if (!s.ShipActiveInCombat)
            {
                continue;
            }
            else if (s.ShipTotalShields > 0) // experimential: torpedoes do not affect shields
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
        return foundTarget;
    }

    public void NotifyTorpedoTubeOpenClose()
    {
        TorpedoDoorsOpen = _torpedoTubeDoorsAnim.ComponentState == GenericOpenCloseAnim.State.Open;
    }

    private IEnumerator LaunchSpread()
    {
        yield return new WaitUntil(() => TorpedoDoorsOpen);
        for (int i = 0; i < _torpedoesInSpread; ++i)
        {
            Vector3 actualLaunchVector = (LaunchVector + (UnityEngine.Random.onUnitSphere * 0.001f)).normalized;
            Torpedo t = ObjectFactory.AcquireTorpedo(Muzzles[_nextBarrel].position, LaunchVector, LaunchOrientation, _torpedoTarget, MaxRange, _warheads[0], _torpedoScale, ContainingShip);
            t.WeaponEffectKey = ObjectFactory.GetEffectKey(LoadedTorpedoType);
            t.IsTracking = (LoadedTorpedoType == "Tracking");
            yield return _spreadDelay;
        }
        if (_torpedoTubeDoorsAnim != null)
        {
            _torpedoTubeDoorsAnim.Close(NotifyTorpedoTubeOpenClose);
        }
        else
        {
            TorpedoDoorsOpen = false;
        }
        yield return null;
    }

    public override ObjectFactory.WeaponBehaviorType BehaviorType => ObjectFactory.WeaponBehaviorType.Torpedo;

    public override string SpriteKey { get { return "Torpedo tube"; } }

    private float MinTargetAngle { get { return _minRotation; } }
    private float MaxTargetAngle { get { return _maxRotation; } }
    private bool TorpedoDoorsOpen { get; set; }

    protected override float AIMaxAngleToTarget => _rotationSpan;

    private int _torpedoesInSpread;
    public string LoadedTorpedoType;
    private Vector3 _launchDirection;
    private Vector3 LaunchVector { get { return Muzzles[0].TransformDirection(_launchDirection); } }
    private Vector3 LaunchOrientation => Muzzles[0].forward;
    private GenericOpenCloseAnim _torpedoTubeDoorsAnim;
    private Vector3 _torpedoTarget;
    private float _torpedoScale;

    private static readonly WaitForSeconds _spreadDelay = new WaitForSeconds(0.1f);
}
