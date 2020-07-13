using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class BSplineCurve<T>
{
    public BSplineCurve(IEnumerable<T> CtlPoints, IEnumerable<float> KV, Func<T, T, float, T> lerpFunc)
    {
        _ctlMesh = CtlPoints.ToArray();
        _kv = KV.ToArray();
        if (_kv.Length <= 1)
        {
            throw new Exception("Attempted to create B-spline in which empty knot vector");
        }
        Order = _kv.Length - _ctlMesh.Length;
        if (Order < 0)
        {
            throw new Exception("Attempted to create B-spline in which order is greater than length");
        }
        _lerpFunc = lerpFunc;
        _tmpEvalArray = new T[Order];
    }

    public static BSplineCurve<T> UniformOpen(IEnumerable<T> CtlPoints, int order, Func<T, T, float, T> lerpFunc)
    {
        int length = CtlPoints.Count();
        if (length < order)
        {
            throw new Exception("Attempted to create B-spline in which order is greater than length");
        }
        return new BSplineCurve<T>(CtlPoints, CreateOpenKV(order, length), lerpFunc);
    }

    private static float[] CreateOpenKV(int order, int ctlMeshLen)
    {
        int kvLen = ctlMeshLen + order;
        int interiorKnots = ctlMeshLen - order + 1;
        float[] kv = new float[kvLen];
        int i = 1, j = ctlMeshLen - order, k = 0;
        while (k < order)
        {
            kv[k] = 0;
            ++k;
        }
        while (i <= j)
        {
            kv[k] = ((float)i) / interiorKnots;
            ++k;
            ++i;
        }
        for (j = 0; j < order; j++)
        {
            kv[k++] = ((float)i) / interiorKnots;
        }
        return kv;
    }

    public T Eval(float x)
    {
        int k = FindKnotInterval(x);
        for (int i = 0; i < Order; ++i)
        {
            _tmpEvalArray[i] = _ctlMesh[i + k - (Order - 1)];
        }
        for (int r = 1; r < Order; ++r)
        {
            for (int j = Order - 1; j >= r; --j)
            {
                float alpha = (x - _kv[j + k - (Order - 1)]) / (_kv[j + 1 + k - r] - _kv[j + k - (Order - 1)]);
                _tmpEvalArray[j] = _lerpFunc(_tmpEvalArray[j - 1], _tmpEvalArray[j], alpha); //was: _add(_multScalar(d[j - 1], 1 - alpha), _multScalar(d[j], alpha));
            }
        }
        return _tmpEvalArray[Order - 1];
    }

    private int FindKnotInterval(float x)
    {
        if (x < _kv[0])
        {
            throw new Exception("parameter outside domain of B-spline");
        }
        for (int i = 0; i < _kv.Length - 1; ++i)
        {
            if (_kv[i] <= x && x < _kv[i + 1] || (x <= _kv[i + 1] && Mathf.Approximately(_kv[i + 1], _kv.Last())))
            {
                return i;
            }
        }
        throw new Exception("parameter outside domain of B-spline");
    }

    private IEnumerable<T> DeriveCtlMesh(Func<T, float, T, float, T> blendFunc)
    {
        for (int i = 0; i < _ctlMesh.Length - 1; ++i)
        {
            float coeff = (Order - 1) / (_kv[i + Order] - _kv[i + 1]);
            yield return blendFunc(_ctlMesh[i + 1], coeff, _ctlMesh[i], -coeff);
        }
    }

    public BSplineCurve<T> Derivative(Func<T, float, T, float, T> blendFunc)
    {
        return new BSplineCurve<T>(DeriveCtlMesh(blendFunc), _kv.Skip(1).Take(_kv.Length - 2), _lerpFunc);
    }

    public readonly int Order;
    private readonly T[] _ctlMesh;
    private readonly float[] _kv;
    private readonly Func<T, T, float, T> _lerpFunc;
    private readonly T[] _tmpEvalArray;
}
