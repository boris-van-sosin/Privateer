using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface ITargetableEntity
{
    Vector3 EntityLocation { get; }
    bool Targetable { get; }
    TargetableEntityInfo TargetableBy { get; }
}

[Flags]
public enum TargetableEntityInfo
{
    None = 0,
    AntiShip = 1,
    Flak = 2,
    Torpedo = 4,
    AntiTorpedo = 8
}

public static class TargetableEntityUtils
{
    public static bool IsTargetable(TargetableEntityInfo flags, TargetableEntityInfo flag)
    {
        return (flags & flag) != TargetableEntityInfo.None;
    }
}