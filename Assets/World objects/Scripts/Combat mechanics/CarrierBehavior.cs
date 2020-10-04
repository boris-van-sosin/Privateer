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
        _formations = new List<ValueTuple<StrikeCraftFormation, FormationAIHandle, string>>(MaxFormations);
        _actionQueue = new LinkedList<(string, bool)>();
        foreach (string prodKey in ObjectFactory.GetAllStrikeCraftTypes())
        {
            _availableCraft[prodKey] = 99;
            _lockFormationNum[prodKey] = -1;
        }
        _inLaunch = false;
        _inRecovery = false;
    }

    public void LaunchDbg()
    {
        if (!_inLaunch && !_inRecovery && _formations.Count < MaxFormations && !_ship.ShipDisabled)
        {
            StartCoroutine(LaunchSequence("Fed Torpedo Bomber"));
        }
    }

    public bool LaunchFormationOfType(string key)
    {
        if (!_inLaunch && !_inRecovery && _formations.Count < MaxFormations && !_ship.ShipDisabled)
        {
            StartCoroutine(LaunchSequence(key));
            return true;
        }
        else
        {
            return false;
        }
    }

    public void QueueLaunchFormationOfType(string key)
    {
        if (!_inLaunch && !_inRecovery && _formations.Count < MaxFormations)
        {
            LaunchFormationOfType(key);
        }
        else if (_inLaunch || _inRecovery)
        {
            EnqueueActionInner(key, true);
        }
    }

    private bool EnqueueActionInner(string strikeCraftKey, bool launch, bool negateOnly)
    {
        bool negated = false;
        LinkedList<ValueTuple<string, bool>>.Enumerator idx = _actionQueue.GetEnumerator();
        while (idx.MoveNext())
        {
            if (idx.Current.Item1 == strikeCraftKey && idx.Current.Item2 == !launch)
            {
                _actionQueue.Remove(idx.Current);
                negated = true;
                break;
            }
        }
        if (!negateOnly && !negated)
        {
            _actionQueue.AddLast((strikeCraftKey, launch));
            return true;
        }
        return negated;
    }

    private bool EnqueueActionInner(string strikeCraftKey, bool launch)
    {
        return EnqueueActionInner(strikeCraftKey, launch, false);
    }

    private IEnumerator LaunchSequence(string strikeCraftKey)
    {
        if (CarrierHangerAnim.Length == 0)
        {
            yield break;
        }

        _inLaunch = true;

        yield return _endOfFrameWait;

        bool finishedClosing = false;
        while (!finishedClosing)
        {
            bool allClosed = true;
            for (int j = 0; j < CarrierHangerAnim.Length; ++j)
            {
                if (CarrierHangerAnim[j].ComponentState != GenericOpenCloseAnim.State.Closed)
                {
                    allClosed = false;
                    break;
                }
            }
            finishedClosing = allClosed;
            yield return _endOfFrameWait;
        }

        StrikeCraftFormation formation = ObjectFactory.CreateStrikeCraftFormation(strikeCraftKey);
        formation.DestroyOnEmpty = false;
        _formations.Add(new ValueTuple<StrikeCraftFormation, FormationAIHandle, string>(formation, formation.GetComponent<FormationAIHandle>(), strikeCraftKey));

        onLaunchStart?.Invoke(this);
        yield return _endOfFrameWait;

        StrikeCraft[] currLaunchingStrikeCraft = new StrikeCraft[CarrierHangerAnim.Length];

        formation.Owner = _ship.Owner;
        formation.HostCarrier = this;
        int i = 0;
        formation.transform.position = _launchTransform[0].position;
        formation.transform.rotation = _ship.transform.rotation;

        int numLaunched = 0;
        int numCreated = 0;
        while (numLaunched < formation.Positions.Length)
        {
            if (numCreated < formation.Positions.Length && CarrierHangerAnim[i].ComponentState == GenericOpenCloseAnim.State.Closed)
            {
                StrikeCraft s = ObjectFactory.CreateStrikeCraftAndFitOut(strikeCraftKey);
                s.IgnoreHits = true;
                s.Owner = formation.Owner;
                s.transform.position = _elevatorBed[i].position;
                s.transform.rotation = Quaternion.LookRotation(_elevatorBed[i].up, _elevatorBed[i].forward);
                s.AddToFormation(formation);
                s.Activate();
                s.AttachToHangerElevator(_elevatorBed[i]);
                formation.MaxSpeed = s.MaxSpeed * GlobalOtherConstants.StrikeCraftFormtionMaxSpeedFactor;
                formation.TurnRate = s.TurnRate * GlobalOtherConstants.StrikeCraftFormtionTurnRateFactor;
                currLaunchingStrikeCraft[i] = s;
                CarrierHangerAnim[i].Open();
                ++numCreated;
            }
            else if (CarrierHangerAnim[i].ComponentState == GenericOpenCloseAnim.State.Open)
            {
                currLaunchingStrikeCraft[i].DetachHangerElevator();
                Maneuver m = CreateLaunchManeuver(_launchTransform[i]);
                StrikeCraft currStrikeCraft = currLaunchingStrikeCraft[i];
                if (numLaunched == 0)
                {
                    m.OnManeuverFinish += delegate (Maneuver m1)
                    {
                        formation.transform.position = formation.AllStrikeCraft().First().transform.position;
                        formation.transform.rotation = formation.AllStrikeCraft().First().transform.rotation;
                        formation.GetComponent<FormationAIHandle>().OrderEscort(_ship);
                        currStrikeCraft.IgnoreHits = false;
                    };
                }
                else
                {
                    m.OnManeuverFinish += delegate (Maneuver m1)
                    {
                        currStrikeCraft.IgnoreHits = false;
                    };
                }
                float launchSpeed = Vector3.Dot(_launchTransform[i].up, _ship.ActualVelocity);
                if (launchSpeed > 0)
                {
                    currStrikeCraft.StartManeuver(m, launchSpeed);
                }
                else
                {
                    currStrikeCraft.StartManeuver(m);
                }
                ++numLaunched;
                if (numLaunched == formation.Positions.Length)
                {
                    formation.DestroyOnEmpty = true;
                }
                yield return _hangerLaunchCycleDelay;
                CarrierHangerAnim[i].Close();
            }

            ++i;
            if (i >= _launchTransform.Length)
            {
                i = 0;
                yield return _hangerLaunchCycleDelay;
            }
            else
            {
                yield return _endOfFrameWait;
            }
        }

        _inLaunch = false;

        yield return _endOfFrameWait;
        onLaunchFinish?.Invoke(this);
        PostLaunchAndRecovery();

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
                _formations.RemoveAt(i);
                PostLaunchAndRecovery();
                onRecoveryFinish?.Invoke(this);
            }
            else
            {
                _formations.RemoveAt(i);
                onFormationRemoved?.Invoke(this, f);
            }
            MaintainLockedFormationsNum();
        }
    }

    public bool StartRecallFormation(string strikeCraftType)
    {
        FormationAIHandle c =
            _formations.Where(f1 => f1.Item3 == strikeCraftType).Select(f2 => f2.Item2).FirstOrDefault();
        if (c != null)
        {
            c.OrderReturnToHost();
            return true;
        }
        else
        {
            return false;
        }
    }

    public void QueueRecallFormationOfType(string key)
    {
        if (!EnqueueActionInner(key, false, true))
        {
            if (!_inLaunch && !_inRecovery)
            {
                StartRecallFormation(key);
            }
            else
            {
                _actionQueue.AddLast((key, false));
            }
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
        onRecoveryStart?.Invoke(this);

        return true;
    }

    public bool RecoveryTryStartSingle(StrikeCraft s, int hangerIdx, Action onReadyToLand)
    {
        if (CarrierHangerAnim[hangerIdx].ComponentState == GenericOpenCloseAnim.State.Closed)
        {
            CarrierHangerAnim[hangerIdx].Open(onReadyToLand);
            return true;
        }
        return false;
    }

    public bool RecoveryTryLand(StrikeCraft s, int hangerIdx, Action onFinishLanding)
    {
        if (CarrierHangerAnim[hangerIdx].ComponentState == GenericOpenCloseAnim.State.Open)
        {
            s.AttachToHangerElevator(_elevatorBed[hangerIdx]);
            CarrierHangerAnim[hangerIdx].Close(onFinishLanding);
            return true;
        }
        return false;
    }

    private IEnumerable<ValueTuple<Func<Vector3, Vector3>, Func<Vector3, Vector3>>> PathFixes(BspPath rawPath, Transform currLaunchTr)
    {
        int numPathPts = rawPath.Points.Length;
        Matrix4x4 ptTransform = Matrix4x4.TRS(currLaunchTr.position, currLaunchTr.rotation, Vector3.one);
        //Matrix4x4 dirTransform = Matrix4x4.TRS(Vector3.zero, currLaunchTr.rotation, Vector3.one);
        for (int i = 0; i < numPathPts; i++)
        {
            if (i < numPathPts - 2)
            {
                yield return
                    new ValueTuple<Func<Vector3, Vector3>, Func<Vector3, Vector3>>(
                        p => ptTransform.MultiplyPoint3x4(p),
                        v => ptTransform.MultiplyVector(v));
            }
            else
            {
                yield return
                    new ValueTuple<Func<Vector3, Vector3>, Func<Vector3, Vector3>>(
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

    public IEnumerable<ValueTuple<StrikeCraftFormation, FormationAIHandle, string>> ActiveFormationsOfType(string strikeCraftKey)
    {
        return _formations.Where(f => f.Item3 == strikeCraftKey);
    }

    public int NumActiveFormationsOfType(string strikeCraftKey)
    {
        return ActiveFormationsOfType(strikeCraftKey).Count();
    }

    public void LockFormationNumSet(string strikeCraftKey, bool doLock)
    {
        if (doLock)
        {
            _lockFormationNum[strikeCraftKey] = NumActiveFormationsOfType(strikeCraftKey);
        }
        else
        {
            _lockFormationNum[strikeCraftKey] = -1;
        }
    }

    public bool LockFormationNumGet(string strikeCraftKey)
    {
        return _lockFormationNum[strikeCraftKey] != -1;
    }

    private bool MaintainLockedFormationsNum()
    {
        bool changed = false;
        foreach (KeyValuePair<string, int> f in _lockFormationNum)
        {
            if (f.Value >= 0 && NumActiveFormationsOfType(f.Key) < f.Value)
            {
                LaunchFormationOfType(f.Key);
                changed = true;
            }
            else if (f.Value >= 0 && NumActiveFormationsOfType(f.Key) > f.Value)
            {
                StartRecallFormation(f.Key);
                changed = true;
            }
        }
        return changed;
    }

    private void PostLaunchAndRecovery()
    {
        bool doneAutoAction = MaintainLockedFormationsNum();
        while (!doneAutoAction && _actionQueue.Count > 0)
        {
            if (_actionQueue.First.Value.Item2)
            {
                doneAutoAction = LaunchFormationOfType(_actionQueue.First.Value.Item1);
            }
            else
            {
                doneAutoAction = StartRecallFormation(_actionQueue.First.Value.Item1);
            }
            _actionQueue.RemoveFirst();
        }
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

    private List<ValueTuple<StrikeCraftFormation, FormationAIHandle, string>> _formations;
    public IEnumerable<ValueTuple<StrikeCraftFormation, FormationAIHandle, string>> ActiveFormations => _formations;
    private Dictionary<string, int> _lockFormationNum = new Dictionary<string, int>();

    private LinkedList<ValueTuple<string, bool>> _actionQueue = new LinkedList<(string, bool)>();

    public GenericOpenCloseAnim[] CarrierHangerAnim;
    public int MaxFormations;

    public IReadOnlyDictionary<string, int> AvailableCraft => _availableCraft;
    private Dictionary<string, int> _availableCraft = new Dictionary<string, int>();

    public event Action<CarrierBehavior> onLaunchStart;
    public event Action<CarrierBehavior> onLaunchFinish;
    public event Action<CarrierBehavior> onRecoveryStart;
    public event Action<CarrierBehavior> onRecoveryFinish;
    public event Action<CarrierBehavior, StrikeCraftFormation> onFormationRemoved;

    private static readonly WaitForEndOfFrame _endOfFrameWait = new WaitForEndOfFrame();
    private static readonly WaitForSeconds _hangerLaunchCycleDelay = new WaitForSeconds(0.5f);
}
