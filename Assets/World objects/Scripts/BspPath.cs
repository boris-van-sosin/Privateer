using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class BspPath : MonoBehaviour
{
    void Awake()
    {
        IEnumerable<Vector3> pathPoints = Points.Select(t => t.position);
        _pathCurve = BSplineCurve<Vector3>.UniformOpen(pathPoints, 3, Vector3.Lerp);
        _velocityCurve = _pathCurve.Derivative((v1, a, v2, b) => v1 * a + v2 * b);
        if (UseForwardOrientaion)
        {
            _forwardCurve = BSplineCurve<Vector3>.UniformOpen(Points.Select(t => t.forward), 3, Vector3.Slerp);
        }
        else
        {
            _forwardCurve = null;
        }
        if (UseUpOrientaion)
        {
            _upCurve = BSplineCurve<Vector3>.UniformOpen(Points.Select(t => t.up), 3, Vector3.Slerp);
        }
        else
        {
            _upCurve = null;
        }
        Initialized = true;
    }

    public Vector3 EvalPoint(float t)
    {
        return _pathCurve.Eval(t);
    }

    public Tuple<Vector3, Vector3> EvalPointAndVelocity(float t)
    {
        return Tuple<Vector3, Vector3>.Create(_pathCurve.Eval(t), _velocityCurve.Eval(t));
    }

    public Tuple<Vector3, Vector3> EvalPointAndForward(float t)
    {
        if (!UseForwardOrientaion)
        {
            return Tuple<Vector3, Vector3>.Create(_pathCurve.Eval(t), _velocityCurve.Eval(t).normalized);
        }
        else
        {
            return Tuple<Vector3, Vector3>.Create(_pathCurve.Eval(t), _forwardCurve.Eval(t));
        }
    }

    public Tuple<Vector3, Vector3, Vector3> EvalPointAndOrientation(float t)
    {
        Vector3 pt = _pathCurve.Eval(t);
        Vector3 forward = (!UseForwardOrientaion) ? _velocityCurve.Eval(t).normalized : _forwardCurve.Eval(t);
        Vector3 up = Vector3.ProjectOnPlane((!UseUpOrientaion) ? transform.up : _upCurve.Eval(t), forward).normalized;
        return Tuple<Vector3, Vector3, Vector3>.Create(pt, forward, up);
    }

    public bool Initialized
    {
        get; private set;
    }

    public Transform[] Points;
    public bool UseForwardOrientaion;
    public bool UseUpOrientaion;
    private BSplineCurve<Vector3> _pathCurve;
    private BSplineCurve<Vector3> _velocityCurve;
    private BSplineCurve<Vector3> _forwardCurve;
    private BSplineCurve<Vector3> _upCurve;
}
