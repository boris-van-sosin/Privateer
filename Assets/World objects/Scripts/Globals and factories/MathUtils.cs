using PlasticGui.WorkspaceWindow.PendingChanges;
using System;
using UnityEngine;

public static class MathUtils
{
    public static float TurningCircleRadius(float speed, float turnRate)
    {
        /*
         * Explanation:
         * The time to complete a half-circle is 180 / turnRate.
         * The arc-length of the half-circle is the distance traveled at that time, in the gicen speed (i.e. speed * 180 / turnRate).
         * We now know the circumefence of the half-circle (or half the circumefence of the circle). Divide by pi to get the radius.
        */
        return (speed * 180f / turnRate) / Mathf.PI;
    }

    public static int lcm(int a, int b)
    {
        if (a == 0)
            return b;
        else if (b == 0)
            return a;
        else
            return a * b / gcd(a, b);
    }

    public static int gcd(int a, int b)
    {
        if (a == b)
        {
            return a;
        }
        else if (a < b)
        {
            int c = a;
            a = b;
            b = c;
        }

        int rem = a % b, div = a / b;
        if (rem == 0)
        {
            return b;
        }
        else
        {
            return gcd(b, rem);
        }
    }
}
