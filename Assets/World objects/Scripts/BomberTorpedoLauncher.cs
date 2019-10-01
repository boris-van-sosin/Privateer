using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class BomberTorpedoLauncher : TurretBase
{
    protected override void Start()
    {
        TorpedoHardpoint parentHardpoint;
        if (transform.parent != null && (parentHardpoint = GetComponentInParent<TorpedoHardpoint>()) != null)
        {
            Vector3 dirInHPSystem = parentHardpoint.transform.TransformDirection(parentHardpoint.LaunchVector);
            _launchDirection = transform.InverseTransformDirection(dirInHPSystem);
            _dummyTorpedoRoot = parentHardpoint.transform;
        }
        else
        {
            _launchDirection = new Vector3(0, 1, 0);
            _dummyTorpedoRoot = transform;
        }
        base.Start();
        Tuple<int, float> launchData = ObjectFactory.TorpedoLaunchDataFromTorpedoType(LoadedTorpedoType);
        MaxRange = launchData.Item2;
        TorpedoesLoaded = _torpedoesInSpread = Muzzles.Length;
    }

    protected override void ParseMuzzles()
    {
        Muzzles = FindDummyTorpedoes(_dummyTorpedoRoot).ToArray();
        ActualFiringInterval = 0;
        MuzzleFx = null;
    }

    private IEnumerable<Transform> FindDummyTorpedoes(Transform root)
    {
        if (root.name.StartsWith(DummyTorpedoString))
        {
            yield return root;
        }
        else
        {
            for (int i = 0; i < root.childCount; ++i)
            {
                IEnumerable<Transform> resInChild = FindDummyTorpedoes(root.GetChild(i));
                foreach (Transform tr in resInChild)
                {
                    yield return tr;
                }
            }
        }
    }

    public override void ManualTarget(Vector3 target)
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
        Debug.Log(string.Format("Torpedo bomber angle to target: {0}", relativeAngle));
        _isLegalAngle = false;
        foreach (Tuple<float, float> r in _rotationAllowedRanges)
        {
            if (r.Item1 < relativeAngle && relativeAngle < r.Item2)
            {
                _isLegalAngle = true;
                _targetAngle = relativeAngle;
                break;
            }
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

        if (flatVec == Vector3.zero)
        {
            return false;
        }

        float relativeAngle = GlobalDirToShipHeading(flatVec);
        foreach (Tuple<float, float> r in _rotationAllowedRanges)
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
        return _isLegalAngle;
    }

    protected override void FireInner(Vector3 firingVector, int barrelIdx)
    {
        _torpedoTarget = Muzzles[barrelIdx].position + firingVector;
        StartCoroutine(LaunchSpread());
    }

    private IEnumerator LaunchSpread()
    {
        while (TorpedoesLoaded > 0)
        {
            int idx = TorpedoesLoaded - 1;
            Vector3 actualLaunchVector = (LaunchVector + (UnityEngine.Random.onUnitSphere * 0.001f)).normalized;
            Warhead w = ObjectFactory.CreateWarhead(LoadedTorpedoType);
            Torpedo t = ObjectFactory.CreateTorpedo(LaunchVector, Muzzles[idx].up, _torpedoTarget, MaxRange, w, ContainingShip);
            t.ColdLaunchDist = GlobalDistances.TorpedoBomberColdLaunchDist;
            t.IsTracking = (LoadedTorpedoType == ObjectFactory.TorpedoType.Heavy);
            t.transform.position = Muzzles[idx].position;
            Muzzles[idx].gameObject.SetActive(false);
            --TorpedoesLoaded;
            yield return new WaitForSeconds(0.1f);
        }
        yield return null;
    }

    protected override Vector3 GetFiringVector(Vector3 vecToTarget)
    {
        return vecToTarget;// - (Muzzles[_nextBarrel].up * Vector3.Dot(Muzzles[_nextBarrel].up, vecToTarget));
    }

    protected override bool MuzzleOppositeDirCheck(Transform Muzzle, Vector3 vecToTarget)
    {
        return true;
    }

    protected override ITargetableEntity AcquireTarget()
    {
        Collider[] colliders = Physics.OverlapSphere(transform.position, MaxRange * 1.05f, ObjectFactory.NavBoxesAllLayerMask);
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

    protected override void FireGrapplingToolInner(Vector3 firingVector, int barrelIdx)
    {
        throw new System.NotImplementedException();
    }

    protected override void SetDefaultAngle()
    {
        _defaultDirection = _containingShip.transform.InverseTransformDirection(transform.up);
    }

    private Vector3 LaunchVector
    {
        get
        {
            return Vector3.Slerp(transform.TransformDirection(_launchDirection).normalized, _containingShip.transform.up, 0.5f);
        }
    }

    public int TorpedoesLoaded { get; private set; }

    public override bool IsOutOfAmmo => TorpedoesLoaded <= 0;

    private Vector3 _launchDirection;
    public ObjectFactory.TorpedoType LoadedTorpedoType;
    public string DummyTorpedoString;
    private int _torpedoesInSpread;
    private Vector3 _torpedoTarget;
    private Transform _dummyTorpedoRoot;
}
