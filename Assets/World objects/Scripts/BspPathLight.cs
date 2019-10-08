using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;


public class BspPathLight
{
    public BspPathLight(IEnumerable<Transform> points, int order, bool useForwardOrientation)
        : this(points, order, useForwardOrientation, true, Vector3.zero, p => p, v => v)
    {
    }

    public BspPathLight(IEnumerable<Transform> points, int order, bool useForwardOrientation, Vector3 defaultUp)
        : this(points, order, useForwardOrientation, false, defaultUp, p => p, v => v)
    {
    }

    public BspPathLight(IEnumerable<Transform> points, int order, bool useForwardOrientation, Transform tr)
        : this(points, order, useForwardOrientation, true, Vector3.zero, p => tr.TransformPoint(p), v => tr.TransformDirection(v))
    {
    }

    public BspPathLight(IEnumerable<Transform> points, int order, bool useForwardOrientation, Vector3 defaultUp, Transform tr)
        : this(points, order, useForwardOrientation, false, tr.TransformDirection(defaultUp), p => tr.TransformPoint(p), v => tr.TransformDirection(v))
    {
    }

    private BspPathLight(IEnumerable<Transform> points, int order, bool useForwardOrientation, bool useUpOrientation, Vector3 defaultUp, Func<Vector3, Vector3> ptTransform, Func<Vector3, Vector3> dirTransform)
    {
        _defaultUp = defaultUp;
        _useForwardOrientaion = useForwardOrientation;
        _useUpOrientaion = useUpOrientation;
        IEnumerable<Vector3> pathPoints = points.Select(t => ptTransform(t.position));
        _pathCurve = BSplineCurve<Vector3>.UniformOpen(pathPoints, order, Vector3.Lerp);
        _velocityCurve = _pathCurve.Derivative((v1, a, v2, b) => v1 * a + v2 * b);
        if (_useForwardOrientaion)
        {
            _forwardCurve = BSplineCurve<Vector3>.UniformOpen(points.Select(t => dirTransform(t.forward)), order, Vector3.Slerp);
        }
        else
        {
            _forwardCurve = null;
        }
        if (_useUpOrientaion)
        {
            _upCurve = BSplineCurve<Vector3>.UniformOpen(points.Select(t => dirTransform(t.up)), order, Vector3.Slerp);
        }
        else
        {
            _upCurve = null;
        }
    }

    public BspPathLight(IEnumerable<Transform> points, int order, bool useForwardOrientation, IEnumerable<ValueTuple<Func<Vector3, Vector3>, Func<Vector3, Vector3>>> transforms)
        : this(points, order, useForwardOrientation, true, Vector3.zero, transforms)
    {
    }

    public BspPathLight(IEnumerable<Transform> points, int order, bool useForwardOrientation, Vector3 defaultUp, IEnumerable<ValueTuple<Func<Vector3, Vector3>, Func<Vector3, Vector3>>> transforms)
        : this(points, order, useForwardOrientation, false, defaultUp, transforms)
    {
    }

    private BspPathLight(IEnumerable<Transform> points, int order, bool useForwardOrientation, bool useUpOrientation, Vector3 defaultUp, IEnumerable<ValueTuple<Func<Vector3, Vector3>, Func<Vector3, Vector3>>> transforms)
    {
        _defaultUp = defaultUp;
        _useForwardOrientaion = useForwardOrientation;
        _useUpOrientaion = useUpOrientation;
        IEnumerable<Vector3> pathPoints = points.Zip(transforms, (p, t) => t.Item1(p.position));
        _pathCurve = BSplineCurve<Vector3>.UniformOpen(pathPoints, order, Vector3.Lerp);
        _velocityCurve = _pathCurve.Derivative((v1, a, v2, b) => v1 * a + v2 * b);
        if (_useForwardOrientaion)
        {
            _forwardCurve = BSplineCurve<Vector3>.UniformOpen(points.Zip(transforms, (p, t) => t.Item2(p.forward)), order, Vector3.Slerp);
        }
        else
        {
            _forwardCurve = null;
        }
        if (_useUpOrientaion)
        {
            _upCurve = BSplineCurve<Vector3>.UniformOpen(points.Zip(transforms, (p, t) => t.Item2(p.up)), order, Vector3.Slerp);
        }
        else
        {
            _upCurve = null;
        }
    }


    public Vector3 EvalPoint(float t)
    {
        return _pathCurve.Eval(t);
    }

    public ValueTuple<Vector3, Vector3> EvalPointAndVelocity(float t)
    {
        return new ValueTuple<Vector3, Vector3>(_pathCurve.Eval(t), _velocityCurve.Eval(t));
    }

    public ValueTuple<Vector3, Vector3> EvalPointAndForward(float t)
    {
        if (!_useForwardOrientaion)
        {
            return new ValueTuple<Vector3, Vector3>(_pathCurve.Eval(t), _velocityCurve.Eval(t).normalized);
        }
        else
        {
            return new ValueTuple<Vector3, Vector3>(_pathCurve.Eval(t), _forwardCurve.Eval(t));
        }
    }

    public ValueTuple<Vector3, Vector3, Vector3> EvalPointAndOrientation(float t)
    {
        Vector3 pt = _pathCurve.Eval(t);
        Vector3 forward = (!_useForwardOrientaion) ? _velocityCurve.Eval(t).normalized : _forwardCurve.Eval(t);
        Vector3 up = Vector3.ProjectOnPlane((!_useUpOrientaion) ? _defaultUp : _upCurve.Eval(t), forward).normalized;
        return new ValueTuple<Vector3, Vector3, Vector3>(pt, forward, up);
    }

    private readonly bool _useForwardOrientaion;
    private readonly bool _useUpOrientaion;
    private readonly Vector3 _defaultUp;
    private readonly BSplineCurve<Vector3> _pathCurve;
    private readonly BSplineCurve<Vector3> _velocityCurve;
    private readonly BSplineCurve<Vector3> _forwardCurve;
    private readonly BSplineCurve<Vector3> _upCurve;
}
