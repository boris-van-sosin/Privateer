using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Maneuver
{
    public Maneuver(IEnumerable<ManeuverSegment> segs)
    {
        _segments = segs.ToArray();
    }

    public Maneuver(params ManeuverSegment[] segs)
    {
        _segments = segs;
    }

    public void Start(MovementBase s)
    {
        _ship = s;
        _currSeg = 0;
        Velocity = _ship.ActualVelocity;
        StartSegment();
    }

    public void Start(MovementBase s, float forceSpeed)
    {
        _ship = s;
        _currSeg = 0;
        Velocity = _ship.transform.forward * forceSpeed;
        StartSegment(forceSpeed);
    }

    public IEnumerable<ValueTuple<Vector3, Vector3>> DebugCurve()
    {
        if (_segments[_currSeg] is BsplinePathSegmnet path)
        {
            int numSamples = 20;
            IEnumerable<ValueTuple<Vector3, Vector3, Vector3>> evalPts =
                Enumerable.Range(0, numSamples + 1).Select(i => path.Path.EvalPointAndOrientation(Mathf.Clamp01(((float)i) / numSamples)));

            IEnumerator<ValueTuple<Vector3, Vector3, Vector3>> ptsIter = evalPts.GetEnumerator();
            ptsIter.MoveNext();
            ValueTuple<Vector3, Vector3, Vector3> prevPt = ptsIter.Current;
            ptsIter.MoveNext();
            ValueTuple<Vector3, Vector3, Vector3> nextPt = ptsIter.Current;
            for (int i = 0; i < numSamples; ++i)
            {
                yield return new ValueTuple<Vector3, Vector3>(prevPt.Item1, nextPt.Item1);
                
                prevPt = nextPt;
                ptsIter.MoveNext();
                nextPt = ptsIter.Current;
            }
        }
        else
        {
            yield break;
        }
    }

    private ManeuverSegment[] _segments;

    public abstract class AccelerationModifier
    {
    }

    public class SelfAccelerate : AccelerationModifier
    {
        public bool Accelerate { get; set; }
    }

    public class AccelerateToTargetSpeedFraction : AccelerationModifier
    {
        public float TargetSpeedFrac { get; set; }
    }

    public abstract class ManeuverSegment
    {
    }

    public class BsplinePathSegmnet : ManeuverSegment
    {
        public BspPathLight Path { get; set; }
        public AccelerationModifier AccelerationBehavior { get; set; }
    }

    public class ConditionPathSegment : ManeuverSegment
    {
        public Func<MovementBase, bool> Condition { get; set; }
        public SelfAccelerate AccelerationBehavior { get; set; }
    }

    private void StartSegment()
    {
        _t = 0;
        _segStartVelocity = _ship.ActualVelocity;
        _segStartSpeed = _segStartVelocity.magnitude;
        _toNextSeg = false;
        Finished = false;
    }

    private void StartSegment(float forceSpeed)
    {
        _t = 0;
        _segStartSpeed = forceSpeed;
        _toNextSeg = false;
        Finished = false;
    }

    private void Finish()
    {
        Finished = true;
        if (OnManeuverFinish != null)
        {
            OnManeuverFinish(this);
            OnManeuverFinish = null;
        }
    }

    public void Advance(float timeInterval)
    {
        Transform tr = _ship.transform;
        if (_toNextSeg)
        {
            StartSegment();
        }
        ManeuverSegment activeSeg = _segments[_currSeg];

        if (activeSeg is ConditionPathSegment conditionSeg)
        {
            Vector3 newVelocity = Velocity;
            if (conditionSeg.AccelerationBehavior != null)
            {
                if (conditionSeg.AccelerationBehavior.Accelerate)
                {
                    newVelocity += _ship.transform.forward * (_ship.Thrust * timeInterval);
                }
                else
                {
                    newVelocity += _ship.transform.forward * (-_ship.Braking * timeInterval);

                }

                if (newVelocity.sqrMagnitude > _ship.MaxSpeed * _ship.MaxSpeed)
                {
                    newVelocity = _ship.transform.forward * _ship.MaxSpeed;
                }
            }
            _ship.transform.position += newVelocity * timeInterval;
            Velocity = newVelocity;
            if (conditionSeg.Condition(_ship))
            {
                _toNextSeg = true;
            }
        }
        else if (activeSeg is BsplinePathSegmnet pathSeg)
        {
            ValueTuple<Vector3, Vector3> pathVelocity = pathSeg.Path.EvalPointAndVelocity(_t);
            float paramSpeed = pathVelocity.Item2.magnitude;
            float currSpeed = Velocity.magnitude;
            float targetSpeed = currSpeed;
            if (pathSeg.AccelerationBehavior == null)
            {
            }
            else if (pathSeg.AccelerationBehavior is SelfAccelerate accelerate)
            {
                if (accelerate.Accelerate)
                {
                    targetSpeed += _ship.Thrust * timeInterval;
                }
                else
                {
                    targetSpeed -= _ship.Braking * timeInterval;
                }
                targetSpeed = Mathf.Clamp(targetSpeed, 0.0f, _ship.MaxSpeed);
            }
            else if (pathSeg.AccelerationBehavior is AccelerateToTargetSpeedFraction fracAccelerate)
            {
                float segTargetSpeed = _ship.MaxSpeed * fracAccelerate.TargetSpeedFrac;
                targetSpeed = Mathf.Lerp(_segStartSpeed, targetSpeed, _t);
            }

            float minSpeed = _ship.Thrust * Time.deltaTime;
            if (targetSpeed < minSpeed)
            {
                targetSpeed = minSpeed;
            }

            ValueTuple<Vector3, Vector3, Vector3> nextPos;
            float nextT = _t + (targetSpeed * timeInterval / paramSpeed);
            float tOvershoot = nextT - 1f;
            if (tOvershoot > 0f)
            {
                tOvershoot = nextT - 1f;
                _t = 1f;
                nextPos = pathSeg.Path.EvalPointAndOrientation(_t);
                nextPos = new ValueTuple<Vector3, Vector3, Vector3>(
                    nextPos.Item1 + pathVelocity.Item2 * tOvershoot,
                    nextPos.Item2,
                    nextPos.Item3);
            }
            else
            {
                _t = nextT;
                nextPos = pathSeg.Path.EvalPointAndOrientation(_t);
            }
            _ship.transform.SetPositionAndRotation(nextPos.Item1, Quaternion.LookRotation(nextPos.Item2, nextPos.Item3));
            if (tOvershoot > 0)
            {
                Velocity = nextPos.Item2 * (targetSpeed + tOvershoot / paramSpeed);
            }
            else
            {
                Velocity = nextPos.Item2 * targetSpeed;
            }
            if (Mathf.Approximately(_t, 1f))
            {
                _toNextSeg = true;
            }
        }
        if (_toNextSeg)
        {
            ++_currSeg;
            if (_currSeg >= _segments.Length - 1)
            {
                Finish();
            }
        }
    }

    public bool Finished { get; private set; }

    public Vector3 Velocity { get; private set; }

    public event Action<Maneuver> OnManeuverFinish;

    private MovementBase _ship;
    private int _currSeg;
    private float _t;
    private Vector3 _segStartVelocity;
    private float _segStartSpeed;
    private bool _toNextSeg;
}
