using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class FlagsUtils
{
    public static bool IsSet<T>(T flags, T flag) where T : struct
    {
        int flagsValue = (int)(object)flags;
        int flagValue = (int)(object)flag;

        return (flagsValue & flagValue) != 0;
    }
}
