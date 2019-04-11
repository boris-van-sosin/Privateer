using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;

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
        if (path.UseForwardOrientaion || path.UseUpOrientaion)
        {
            IEnumerable<Tuple<Vector3, Vector3, Vector3>> evalPts = SampleCurveWithOrientation(path, numSamples);

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
            IEnumerable<Vector3> evalPts = SampleCurve(path, numSamples);

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

    private static IEnumerable<Vector3> SampleCurve(BspPath path, int numSamples)
    {
        IEnumerable<Vector3> evalPts;
        if (path.Initialized)
        {
            evalPts = Enumerable.Range(0, numSamples + 1).Select(i => path.EvalPoint(Mathf.Clamp01(((float)i) / numSamples)));
        }
        else
        {
            IEnumerable<Vector3> pathPoints = path.Points.Select(t => t.position);
            BSplineCurve<Vector3> pathCurve = BSplineCurve<Vector3>.UniformOpen(pathPoints, 3, Vector3.Lerp);
            evalPts = Enumerable.Range(0, numSamples + 1).Select(i => pathCurve.Eval(Mathf.Clamp01(((float)i) / numSamples)));
        }
        return evalPts;
    }

    private static IEnumerable<Tuple<Vector3, Vector3, Vector3>> SampleCurveWithOrientation(BspPath path, int numSamples)
    {
        IEnumerable<Tuple<Vector3, Vector3, Vector3>> evalPts;
        if (path.Initialized)
        {
            evalPts = Enumerable.Range(0, numSamples + 1).Select(i => path.EvalPointAndOrientation(Mathf.Clamp01(((float)i) / numSamples)));
        }
        else
        {
            IEnumerable<Vector3> pathPoints = path.Points.Select(t => t.position);
            BSplineCurve<Vector3> pathCurve = BSplineCurve<Vector3>.UniformOpen(pathPoints, 3, Vector3.Lerp);
            BSplineCurve<Vector3> forwardCurve, upCurve;
            if (path.UseForwardOrientaion)
            {
                forwardCurve = BSplineCurve<Vector3>.UniformOpen(path.Points.Select(t => t.forward), 3, Vector3.Slerp);
            }
            else
            {
                forwardCurve = pathCurve.Derivative((v1, a, v2, b) => v1 * a + v2 * b);
            }
            if (path.UseUpOrientaion)
            {
                upCurve = BSplineCurve<Vector3>.UniformOpen(path.Points.Select(t => t.up), 3, Vector3.Slerp);
            }
            else
            {
                upCurve = null;
            }
            evalPts = Enumerable.Range(0, numSamples + 1).Select(i => Mathf.Clamp01(((float)i) / numSamples)).Select(
                t => Tuple<float, Vector3, Vector3>.Create(t, pathCurve.Eval(t), forwardCurve.Eval(t).normalized)).Select(
                        pf => Tuple<Vector3,Vector3,Vector3>.Create(pf.Item2, pf.Item3, Vector3.ProjectOnPlane((!path.UseUpOrientaion) ? path.transform.up : upCurve.Eval(pf.Item1), pf.Item3).normalized));
        }
        return evalPts;
    }
}
