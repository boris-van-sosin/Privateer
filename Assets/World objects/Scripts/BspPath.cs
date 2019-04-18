using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System;

public class BspPath : MonoBehaviour
{
    void Awake()
    {
        if (UseUpOrientaion)
        {
            DefaultPath = new BspPathLight(Points, 3, UseForwardOrientaion);
        }
        else
        {
            DefaultPath = new BspPathLight(Points, 3, UseForwardOrientaion, transform.up);
        }
        Initialized = true;
    }

    public BspPathLight ExractLightweightPath(Transform tr)
    {
        if (UseUpOrientaion)
        {
            return new BspPathLight(Points, 3, UseForwardOrientaion, tr);
        }
        else
        {
            return new BspPathLight(Points, 3, UseForwardOrientaion, transform.up, tr);
        }
    }

    public BspPathLight ExractLightweightPath()
    {
        if (UseUpOrientaion)
        {
            return new BspPathLight(Points, 3, UseForwardOrientaion);
        }
        else
        {
            return new BspPathLight(Points, 3, UseForwardOrientaion, transform.up);
        }
    }

    public Vector3 EvalPoint(float t)
    {
        return DefaultPath.EvalPoint(t);
    }

    public Tuple<Vector3, Vector3> EvalPointAndVelocity(float t)
    {
        return DefaultPath.EvalPointAndVelocity(t);
    }

    public Tuple<Vector3, Vector3> EvalPointAndForward(float t)
    {
        return DefaultPath.EvalPointAndForward(t);
    }

    public Tuple<Vector3, Vector3, Vector3> EvalPointAndOrientation(float t)
    {
        return DefaultPath.EvalPointAndOrientation(t);
    }

    public bool Initialized
    {
        get; private set;
    }

    public BspPathLight DefaultPath { get; private set; }

    public Transform[] Points;
    public bool UseForwardOrientaion;
    public bool UseUpOrientaion;
}
