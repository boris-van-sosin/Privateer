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
        _formations = new List<Tuple<StrikeCraftFormation, StrikeCraftFormationAIController>>(MaxFormations);
        _inLaunch = false;
        _inRecovery = false;
    }

    public void LaunchDbg()
    {
        if (!_inLaunch && !_inRecovery && _formations.Count < MaxFormations)
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
        _formations.Add(new Tuple<StrikeCraftFormation, StrikeCraftFormationAIController>(formation, formation.GetComponent<StrikeCraftFormationAIController>()));

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
                s.AttachToHangerElevator(_elevatorBed[i]);
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

    public void DeactivateFormation(StrikeCraftFormation f)
    {
        int i = _formations.FindIndex(t => t.Item1 == f);
        if (i >= 0)
        {
            if (_currRecovering == f)
            {
                _currRecovering = null;
                _inRecovery = false;
            }
            _formations.RemoveAt(i);
        }
    }

    public bool RecoveryTryStart(StrikeCraftFormation f)
    {
        if (_inLaunch && _inRecovery)
        {
            return false;
        }

        _currRecovering = f;

        _inRecovery = true;
        return true;
    }

    public bool RecoveryTryStartSingle(StrikeCraft s, int hangerIdx, GenericEmptyDelegate onReadyToLand)
    {
        if (CarrierHangerAnim[hangerIdx].HangerState == CarrierHangerGenericAnim.State.Closed)
        {
            CarrierHangerAnim[hangerIdx].Open(onReadyToLand);
            return true;
        }
        return false;
    }

    public bool RecoveryTryLand(StrikeCraft s, int hangerIdx, GenericEmptyDelegate onFinishLanding)
    {
        if (CarrierHangerAnim[hangerIdx].HangerState == CarrierHangerGenericAnim.State.Open)
        {
            s.AttachToHangerElevator(_elevatorBed[hangerIdx]);
            CarrierHangerAnim[hangerIdx].Close(onFinishLanding);
            return true;
        }
        return false;
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

    public struct RecoveryTransforms
    {
        public Transform RecoveryStart;
        public Transform RecoveryEnd;
        public int Idx;
    }

    public RecoveryTransforms GetRecoveryTransforms()
    {
        _lastRecoveryHanger = (_lastRecoveryHanger + 1) % CarrierHangerAnim.Length;
        return new RecoveryTransforms()
        {
            RecoveryStart = _recoveryStartTransform[0],
            RecoveryEnd = _recoveryEndTransform[0],
            Idx = 0
        };
    }

    public Vector3 Velocity => _ship.ActualVelocity;

    private Transform[] _launchTransform;
    private Transform[] _recoveryStartTransform;
    private Transform[] _recoveryEndTransform;
    private Transform[] _elevatorBed;

    private ShipBase _ship;
    private bool _inLaunch;
    private bool _inRecovery;
    private StrikeCraftFormation _currRecovering = null;
    private int _lastRecoveryHanger = 0;

    private List<Tuple<StrikeCraftFormation, StrikeCraftFormationAIController>> _formations;

    public CarrierHangerGenericAnim[] CarrierHangerAnim;
    public int MaxFormations;
}
