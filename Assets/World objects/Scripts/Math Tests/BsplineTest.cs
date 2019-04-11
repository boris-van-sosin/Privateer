using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Tests
{
    public class BsplineTest
    {
        // A Test behaves as an ordinary method
        [Test]
        public void BsplineTestBasic()
        {
            Vector3[] ctlMesh = new Vector3[]
            {
                new Vector3(0, 0, 0),
                new Vector3(0, 1, 0.7f),
                new Vector3(0, 1, 0.7f + 1.5f),
                new Vector3(0, 0, 0.7f + 1.5f + 0.7f)
            };
            BSplineCurve<Vector3> bsp = BSplineCurve<Vector3>.UniformOpen(ctlMesh, 3, Vector3.Lerp);
            Assert.IsTrue(bsp.Eval(0f) == new Vector3(0, 0, 0));
            Assert.IsTrue(bsp.Eval(0.2f) == new Vector3(0, 0.64f, 0.568f));
            Assert.IsTrue(bsp.Eval(0.45f) == new Vector3(0, 0.99f, 1.3005f));
            Assert.IsTrue(bsp.Eval(0.5f) == new Vector3(0, 1f, 1.45f));
            Assert.IsTrue(bsp.Eval(0.86f) == new Vector3(0, 0.4816f, 2.50408f));
            Assert.IsTrue(bsp.Eval(1f) == new Vector3(0, 0, 2.9f));
        }

        [Test]
        public void BsplineTestDerivative()
        {
            Vector3[] ctlMesh = new Vector3[]
            {
                new Vector3(0, 0, 0),
                new Vector3(0, 1, 0.7f),
                new Vector3(0, 1, 0.7f + 1.5f),
                new Vector3(0, 0, 0.7f + 1.5f + 0.7f)
            };
            BSplineCurve<Vector3> bsp = BSplineCurve<Vector3>.UniformOpen(ctlMesh, 3, Vector3.Lerp);
            BSplineCurve<Vector3> deriv = bsp.Derivative((v1, a, v2, b) => (v1 * a) + (v2 * b));
            Assert.IsTrue(deriv.Eval(0f) == new Vector3(0, 4f, 2.8f));
            Assert.IsTrue(deriv.Eval(0.2f) == new Vector3(0, 2.4f, 2.88f));
            Assert.IsTrue(deriv.Eval(0.45f) == new Vector3(0, 0.4f, 2.98f));
            Assert.IsTrue(deriv.Eval(0.5f) == new Vector3(0, 0f, 3f));
            Assert.IsTrue(deriv.Eval(0.86f) == new Vector3(0, -2.88f, 2.856f));
            Assert.IsTrue(deriv.Eval(1f) == new Vector3(0, -4f, 2.8f));
        }

        // A UnityTest behaves like a coroutine in Play Mode. In Edit Mode you can use
        // `yield return null;` to skip a frame.
        [UnityTest]
        public IEnumerator BsplineTestWithEnumeratorPasses()
        {
            // Use the Assert class to test conditions.
            // Use yield to skip a frame.
            yield return null;
        }
    }
}
