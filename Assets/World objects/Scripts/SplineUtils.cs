using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class BSplineCurve<T>
{
    public BSplineCurve(IEnumerable<T> CtlPoints, IEnumerable<float> KV, Func<T, T, T> add, Func<T, float, T> multScalar)
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
        _add = add;
        _multScalar = multScalar;
    }

    public static BSplineCurve<T> UniformOpen(IEnumerable<T> CtlPoints, int order, Func<T, T, T> add, Func<T, float, T> multScalar)
    {
        int length = CtlPoints.Count();
        if (length < order)
        {
            throw new Exception("Attempted to create B-spline in which order is greater than length");
        }
        return new BSplineCurve<T>(CtlPoints, CreateOpenKV(order, length), add, multScalar);
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
        T[] d = new T[Order];
        int k = FindKnotInterval(x);
        for (int i = 0; i < Order; ++i)
        {
            d[i] = _ctlMesh[i + k - (Order - 1)];
        }
        for (int r = 1; r < Order; ++r)
        {
            for (int j = Order - 1; j >= r; --j)
            {
                float alpha = (x - _kv[j + k - (Order - 1)]) / (_kv[j + 1 + k - r] - _kv[j + k - (Order - 1)]);
                d[j] = _add(_multScalar(d[j - 1], 1 - alpha), _multScalar(d[j], alpha));
            }
        }
        return d[Order - 1];
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

    public readonly int Order;
    private readonly T[] _ctlMesh;
    private readonly float[] _kv;
    private readonly Func<T, T, T> _add;
    private readonly Func<T, float, T> _multScalar;
}
