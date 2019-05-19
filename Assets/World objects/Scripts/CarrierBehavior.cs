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
        _launchTransform = CarrierHangerAnim.Select(t => t.transform.Find("CarrierLaunchTr")).ToArray();
        _recoveryStartTransform = CarrierHangerAnim.Select(t => t.transform.Find("CarrierRecoveryStartTr")).ToArray();
        _recoveryEndTransform = CarrierHangerAnim.Select(t => t.transform.Find("CarrierRecoveryEndTr")).ToArray();
        _elevatorBed = CarrierHangerAnim.Select(t => t.transform.Find("Elevator")).ToArray();
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
        if (CarrierHangerAnim.Length == 0)
        {
            yield break;
        }

        _inLaunch = true;
        StrikeCraftFormation formation = ObjectFactory.CreateStrikeCraftFormation("Fighter Wing");

        StrikeCraft[] currLaunchingStrikeCraft = new StrikeCraft[CarrierHangerAnim.Length];

        formation.Owner = _ship.Owner;
        formation.HostCarrier = this;
        int i = 0;
        formation.transform.position = _launchTransform[0].position;
        formation.transform.rotation = _ship.transform.rotation;

        int numLaunched = 0;
        while (numLaunched < formation.Positions.Length)
        {
            if (CarrierHangerAnim[i].HangerState == CarrierHangerGenericAnim.State.Closed)
            {
                StrikeCraft s = ObjectFactory.CreateStrikeCraftAndFitOut(strikeCraftKey);
                s.Owner = formation.Owner;
                s.transform.position = _elevatorBed[i].position;
                s.transform.rotation = _elevatorBed[i].rotation;
                s.AddToFormation(formation);
                s.Activate();
                s.AttachToHangerElevator(_elevatorBed[i], Vector3.zero);
                formation.MaxSpeed = s.MaxSpeed * 1.1f;
                formation.TurnRate = s.TurnRate * 0.5f;
                currLaunchingStrikeCraft[i] = s;
                CarrierHangerAnim[i].Open();
            }
            else if (CarrierHangerAnim[i].HangerState == CarrierHangerGenericAnim.State.Open)
            {
                currLaunchingStrikeCraft[i].DetachHangerElevator();
                Maneuver m = CreateLaunchManeuver(_launchTransform[i]);
                if (numLaunched == 0)
                {
                    m.OnManeuverFinish += delegate (Maneuver m1)
                    {
                        formation.transform.position = formation.AllStrikeCraft().First().transform.position;
                        formation.transform.rotation = formation.AllStrikeCraft().First().transform.rotation;
                        formation.GetComponent<StrikeCraftFormationAIController>().OrderEscort(_ship);
                    };
                }
                currLaunchingStrikeCraft[i].StartManeuver(m);
                ++numLaunched;
                yield return new WaitForSeconds(0.5f);
                CarrierHangerAnim[i].Close();
            }

            ++i;
            if (i >= _launchTransform.Length)
            {
                i = 0;
                yield return new WaitForSeconds(0.5f);
            }
            else
            {
                yield return new WaitForEndOfFrame();
            }
        }

        _inLaunch = false;
        yield return null;
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
        return new Tuple<Transform, Transform, Vector3>(_recoveryStartTransform.First(), _recoveryEndTransform.First(), _ship.ActualVelocity);
    }

    private Transform[] _launchTransform;
    private Transform[] _recoveryStartTransform;
    private Transform[] _recoveryEndTransform;
    private Transform[] _elevatorBed;

    private ShipBase _ship;
    private bool _inLaunch;
    public CarrierHangerGenericAnim[] CarrierHangerAnim;
}
