﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TurretHardpoint : MonoBehaviour
{
    public string DisplayString;
    public float MinRotation, MaxRotation;
    public string[] DeadZoneAngles;
    public bool TreatAsFixed;
    public string[] AllowedWeaponTypes;
    public Ship.ShipSection LocationOnShip;
    public TurretAIHint WeaponAIHint;
    public int DefaultGroup;
}
