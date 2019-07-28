using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct Buff
{
    public float WeaponAccuracyFactor { get; set; }
    public float WeaponRateOfFireFactor { get; set; }
    public float WeaponVsStrikeCraftFactor { get; set; }
    public float SpeedFactor { get; set; }
    public float AcceleraionFactor { get; set; }

    public int RepairRateModifier { get; set; }
    public int ShieldRechargeRateModifier { get; set; }

    public static Buff Combine(IEnumerable<Buff> buffs)
    {
        Buff res = Buff.Default();
        foreach (Buff b in buffs)
        {
            CombineSingle(ref res, b);
        }
        return res;
    }

    private static void CombineSingle(ref Buff target, Buff other)
    {
        target.WeaponAccuracyFactor += other.WeaponAccuracyFactor;
        target.WeaponRateOfFireFactor += other.WeaponRateOfFireFactor;
        target.WeaponVsStrikeCraftFactor += other.WeaponVsStrikeCraftFactor;
        target.SpeedFactor += other.SpeedFactor;
        target.AcceleraionFactor += other.AcceleraionFactor;
        target.RepairRateModifier += other.RepairRateModifier;
        target.ShieldRechargeRateModifier += other.ShieldRechargeRateModifier;
    }


    public void ResetToDefault()
    {
        WeaponAccuracyFactor = 0f;
        WeaponRateOfFireFactor = 0f;
        WeaponVsStrikeCraftFactor = 0f;
        SpeedFactor = 0f;
        AcceleraionFactor = 0f;
        RepairRateModifier = 0;
        ShieldRechargeRateModifier = 0;
    }

    public static Buff Default()
    {
        return new Buff()
        {
            WeaponAccuracyFactor = 0f,
            WeaponRateOfFireFactor = 0f,
            WeaponVsStrikeCraftFactor = 0f,
            SpeedFactor = 0f,
            AcceleraionFactor = 0f,
            RepairRateModifier = 0,
            ShieldRechargeRateModifier = 0
        };
    }
}
