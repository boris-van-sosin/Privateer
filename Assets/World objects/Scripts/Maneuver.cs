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
        StartSegment();
        _shipRB = _ship.GetComponent<Rigidbody>();
        _shipRB.AddForce(-_shipRB.velocity, ForceMode.VelocityChange);
    }

    public IEnumerable<Tuple<Vector3, Vector3>> DebugCurve()
    {
        if (_segments[_currSeg] is BsplinePathSegmnet path)
        {
            int numSamples = 20;
            IEnumerable<Tuple<Vector3, Vector3, Vector3>> evalPts =
                Enumerable.Range(0, numSamples + 1).Select(i => path.Path.EvalPointAndOrientation(Mathf.Clamp01(((float)i) / numSamples)));

            IEnumerator<Tuple<Vector3, Vector3, Vector3>> ptsIter = evalPts.GetEnumerator();
            ptsIter.MoveNext();
            Tuple<Vector3, Vector3, Vector3> prevPt = ptsIter.Current;
            ptsIter.MoveNext();
            Tuple<Vector3, Vector3, Vector3> nextPt = ptsIter.Current;
            for (int i = 0; i < numSamples; ++i)
            {
                yield return new Tuple<Vector3, Vector3>(prevPt.Item1, nextPt.Item1);
                
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

    private void Finish()
    {
        Finished = true;
        _shipRB.AddForce(_currVelocity, ForceMode.VelocityChange);
    }

    public void Advance(float timeInterval)
    {
        Transform tr = _ship.transform;
        Vector3 velocity = _ship.ActualVelocity;
        if (_toNextSeg)
        {
            StartSegment();
        }
        ManeuverSegment activeSeg = _segments[_currSeg];

        if (activeSeg is ConditionPathSegment conditionSeg)
        {
            Vector3 newVelocity = velocity;
            if (conditionSeg.AccelerationBehavior != null)
            {
                if (conditionSeg.AccelerationBehavior.Accelerate)
                {
                    newVelocity += _ship.transform.up * (_ship.Thrust * timeInterval);
                }
                else
                {
                    newVelocity += _ship.transform.up * (-_ship.Braking * timeInterval);

                }

                if (newVelocity.sqrMagnitude > _ship.MaxSpeed * _ship.MaxSpeed)
                {
                    newVelocity = _ship.transform.up * _ship.MaxSpeed;
                }
            }
            _ship.transform.position += velocity * timeInterval;
            _currVelocity = velocity;
            if (conditionSeg.Condition(_ship))
            {
                _toNextSeg = true;
            }
        }
        else if (activeSeg is BsplinePathSegmnet pathSeg)
        {
            float paramSpeed = pathSeg.Path.EvalPointAndVelocity(_t).Item2.magnitude;
            float currSpeed = velocity.magnitude;
            float targetSpeed = currSpeed;
            Vector3 newVelocity = velocity;
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

            _t = Mathf.Min(_t + (targetSpeed / paramSpeed), 1f);
            Tuple<Vector3, Vector3, Vector3> nextPos = pathSeg.Path.EvalPointAndOrientation(_t);
            _ship.transform.SetPositionAndRotation(nextPos.Item1, Quaternion.LookRotation(nextPos.Item3, nextPos.Item2));
            _currVelocity = nextPos.Item2 * targetSpeed;
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

    private MovementBase _ship;
    private Rigidbody _shipRB;
    private int _currSeg;
    private float _t;
    private Vector3 _segStartVelocity;
    private Vector3 _currVelocity;
    private float _segStartSpeed;
    private bool _toNextSeg;
}
