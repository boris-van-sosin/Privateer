using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System;

[RequireComponent(typeof(ShipBase))]
public class CarrierBehavior : MonoBehaviour
{
    private void Start()
    {
        _ship = GetComponent<ShipBase>();
        _carrierAnim = LaunchTransform.Select(t => t.parent.GetComponent<CarrierHangerGenericAnim>()).ToArray();
        _inLaunch = false;
    }

    public void LaunchDbg()
    {
        if (!_inLaunch)
        {
            StartCoroutine(LaunchSequence("Fed Torpedo Bomber"));
        }
    }

    private IEnumerator LaunchSequence(string strikeCraftKey)
    {
        if (LaunchTransform.Length == 0)
        {
            yield break;
        }

        _inLaunch = true;
        StrikeCraftFormation formation = ObjectFactory.CreateStrikeCraftFormation("Fighter Wing");

        formation.Owner = _ship.Owner;
        formation.HostCarrier = this;
        int i = 0;
        formation.transform.position = LaunchTransform[0].position;
        formation.transform.rotation = _ship.transform.rotation;
        foreach (Transform tr in formation.Positions)
        {
            //
            _carrierAnim[i].Open();
            yield return new WaitUntil(() => _carrierAnim[i].HangerState == CarrierHangerGenericAnim.State.Open);
            //
            StrikeCraft s = ObjectFactory.CreateStrikeCraftAndFitOut(strikeCraftKey);
            s.Owner = formation.Owner;
            s.transform.position = LaunchTransform[i].position;
            s.transform.rotation = _ship.transform.rotation;
            s.AddToFormation(formation);
            s.Activate();
            s.StartManeuver(CreateLaunchManeuver(LaunchTransform[i]));
            formation.MaxSpeed = s.MaxSpeed * 1.1f;
            formation.TurnRate = s.TurnRate * 0.5f;
            //
            _carrierAnim[i].Close();
            yield return new WaitUntil(() => _carrierAnim[i].HangerState == CarrierHangerGenericAnim.State.Closed);
            //
            ++i;
            if (i >= LaunchTransform.Length)
            {
                i = 0;
                yield return new WaitForSeconds(2);
            }
        }
        yield return null;
        _inLaunch = false;
    }

    private IEnumerable<Tuple<Func<Vector3, Vector3>, Func<Vector3, Vector3>>> PathFixes(BspPath rawPath, Transform currLaunchTr)
    {
        int numPathPts = rawPath.Points.Length;
        Matrix4x4 ptTransform = Matrix4x4.TRS(currLaunchTr.position, currLaunchTr.rotation, Vector3.one);
        //Matrix4x4 dirTransform = Matrix4x4.TRS(Vector3.zero, currLaunchTr.rotation, Vector3.one);
        for (int i = 0; i < numPathPts; i++)
        {
            if (i < numPathPts - 2)
            {
                yield return
                    new Tuple<Func<Vector3, Vector3>, Func<Vector3, Vector3>>(
                        p => ptTransform.MultiplyPoint3x4(p),
                        v => ptTransform.MultiplyVector(v));
            }
            else
            {
                yield return
                    new Tuple<Func<Vector3, Vector3>, Func<Vector3, Vector3>>(
                        p => ClearY(ptTransform.MultiplyPoint3x4(p)),
                        v => ptTransform.MultiplyVector(v));
            }
        }
    }

    private Maneuver CreateLaunchManeuver(Transform currLaunchTr)
    {
        BspPath launchPath = ObjectFactory.GetPath("Strike craft carrier launch 1");
        Maneuver.BsplinePathSegmnet seg = new Maneuver.BsplinePathSegmnet()
        {
            AccelerationBehavior = new Maneuver.SelfAccelerate() { Accelerate = true },
            Path = launchPath.ExractLightweightPath(PathFixes(launchPath, currLaunchTr))
        };
        return new Maneuver(seg);
    }

    private Vector3 ClearY(Vector3 a) => new Vector3(a.x, 0, a.z);

    public Tuple<Transform, Transform, Vector3> GetRecoveryTransforms()
    {
        return new Tuple<Transform, Transform, Vector3>(RecoveryStartTransform.First(), RecoveryEndTransform.First(), _ship.ActualVelocity);
    }

    public Transform[] LaunchTransform;
    public Transform[] RecoveryStartTransform;
    public Transform[] RecoveryEndTransform;

    private ShipBase _ship;
    private bool _inLaunch;
    private CarrierHangerGenericAnim[] _carrierAnim;
}
