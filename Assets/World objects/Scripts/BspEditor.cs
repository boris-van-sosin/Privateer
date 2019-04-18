using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;
using System;

[CustomEditor(typeof(BspPath))]
public class BspEditor : Editor
{
    [DrawGizmo(GizmoType.Selected | GizmoType.NonSelected)]
    static void DrawGizmosSelected(BspPath path, GizmoType g)
    {
        if (path.Points == null || path.Points.Any(x => x == null))
        {
            return;
        }
        foreach (Transform tr in path.Points)
        {
            Gizmos.DrawSphere(tr.position, 0.02f);
        }
        int numSamples = 100;
        BspPathLight lt = path.ExractLightweightPath();
        if (path.UseForwardOrientaion || path.UseUpOrientaion)
        {
            IEnumerable<Tuple<Vector3, Vector3, Vector3>> evalPts = SampleCurveWithOrientation(lt, numSamples);

            IEnumerator<Tuple<Vector3, Vector3, Vector3>> ptsIter = evalPts.GetEnumerator();
            ptsIter.MoveNext();
            Tuple<Vector3, Vector3, Vector3> prevPt = ptsIter.Current;
            ptsIter.MoveNext();
            Tuple<Vector3, Vector3, Vector3> nextPt = ptsIter.Current;
            for (int i = 0; i < numSamples; ++i)
            {
                Gizmos.DrawLine(prevPt.Item1, nextPt.Item1);
                if (i % 5 == 0)
                {
                    Gizmos.DrawLine(prevPt.Item1, prevPt.Item1 + (prevPt.Item2 * 0.2f));
                    Gizmos.DrawLine(prevPt.Item1, prevPt.Item1 + (prevPt.Item3 * 0.2f));
                }

                prevPt = nextPt;
                ptsIter.MoveNext();
                nextPt = ptsIter.Current;
            }
        }
        else
        {
            IEnumerable<Vector3> evalPts = SampleCurve(lt, numSamples);

            IEnumerator<Vector3> ptsIter = evalPts.GetEnumerator();
            ptsIter.MoveNext();
            Vector3 prevPt = ptsIter.Current;
            ptsIter.MoveNext();
            Vector3 nextPt = ptsIter.Current;
            for (int i = 0; i < numSamples; ++i)
            {
                Gizmos.DrawLine(prevPt, nextPt);
                prevPt = nextPt;
                ptsIter.MoveNext();
                nextPt = ptsIter.Current;
            }
        }
    }

    private static IEnumerable<Vector3> SampleCurve(BspPathLight path, int numSamples)
    {
        return Enumerable.Range(0, numSamples + 1).Select(i => path.EvalPoint(Mathf.Clamp01(((float)i) / numSamples)));
    }

    private static IEnumerable<Tuple<Vector3, Vector3, Vector3>> SampleCurveWithOrientation(BspPathLight path, int numSamples)
    {
        return Enumerable.Range(0, numSamples + 1).Select(i => path.EvalPointAndOrientation(Mathf.Clamp01(((float)i) / numSamples)));
    }
}
