using System;
using System.Collections.Generic;
using System.Linq;

[Serializable]
public struct DynamicBuff
{
    public float WeaponAccuracyFactor { get; set; }
    public float WeaponRateOfFireFactor { get; set; }
    public float WeaponVsStrikeCraftFactor { get; set; }
    public float SpeedFactor { get; set; }
    public float AcceleraionFactor { get; set; }

    public int RepairRateModifier { get; set; }
    public int ShieldRechargeRateModifier { get; set; }

    //public HitPointBuff HitPointModifiers;

    public static DynamicBuff Combine(IEnumerable<DynamicBuff> buffs)
    {
        DynamicBuff res = DynamicBuff.Default();
        foreach (DynamicBuff b in buffs)
        {
            CombineSingle(ref res, b);
        }
        return res;
    }

    private static void CombineSingle(ref DynamicBuff target, DynamicBuff other)
    {
        target.Combine(other);
    }

    public void Combine(DynamicBuff other)
    {
        WeaponAccuracyFactor += other.WeaponAccuracyFactor;
        WeaponRateOfFireFactor += other.WeaponRateOfFireFactor;
        WeaponVsStrikeCraftFactor += other.WeaponVsStrikeCraftFactor;
        SpeedFactor += other.SpeedFactor;
        AcceleraionFactor += other.AcceleraionFactor;
        RepairRateModifier += other.RepairRateModifier;
        ShieldRechargeRateModifier += other.ShieldRechargeRateModifier;
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

    public static DynamicBuff Default()
    {
        return new DynamicBuff()
        {
            WeaponAccuracyFactor = 0f,
            WeaponRateOfFireFactor = 0f,
            WeaponVsStrikeCraftFactor = 0f,
            SpeedFactor = 0f,
            AcceleraionFactor = 0f,
            RepairRateModifier = 0,
            ShieldRechargeRateModifier = 0,
        };
    }
}

[Serializable]
public class StaticBuff
{
    public DynamicBuff DynamicData { get; set; }
    public HitPointBuff HitPointData { get; set; }

    public static StaticBuff Combine(IEnumerable<StaticBuff> buffs)
    {
        StaticBuff res = Default();
        foreach (StaticBuff other in buffs)
        {
            res.CombineSingle(other);
        }
        return res;
    }

    private void CombineSingle(StaticBuff other)
    {
        DynamicData = DynamicBuff.Combine(GetDynamicBuffs().Concat(other.GetDynamicBuffs()));
        HitPointBuff.CombineSingle(HitPointData, other.HitPointData);
    }

    private IEnumerable<DynamicBuff> GetDynamicBuffs()
    {
        yield return DynamicData;
    }

    public static StaticBuff Default()
    {
        return new StaticBuff()
        {
            DynamicData = DynamicBuff.Default(),
            HitPointData = HitPointBuff.Default()
        };
    }

    public void ResetToDefault()
    {
        DynamicData.ResetToDefault();
        HitPointData.ResetToDefault();
    }

    public class HitPointBuff
    {
        public int Hull { get; set; }
        public int Component { get; set; }
        public Dictionary<string, int> TurretBuffs { get; set; }

        public static void CombineSingle(HitPointBuff target, HitPointBuff other)
        {
            target.Hull += other.Hull;
            target.Component += other.Component;

            if (target.TurretBuffs == null)
            {
                target.TurretBuffs = new Dictionary<string, int>();
            }

            if (other.TurretBuffs != null)
            {
                foreach (KeyValuePair<string, int> t in other.TurretBuffs)
                {
                    if (target.TurretBuffs.ContainsKey(t.Key))
                    {
                        target.TurretBuffs[t.Key] += t.Value;
                    }
                    else
                    {
                        target.TurretBuffs[t.Key] = t.Value;
                    }
                }
            }
        }

        public void ResetToDefault()
        {
            Hull = 0;
            Component = 0;
            TurretBuffs.Clear();
        }

        public static HitPointBuff Default()
        {
            return new HitPointBuff()
            {
                Hull = 0,
                Component = 0,
                TurretBuffs = new Dictionary<string, int>()
            };
        }
    }
}

[Serializable]
public struct TurretModBuff
{
    public string ApplyToWeapon { get; set; }
    public string ApplyToAmmo { get; set; }
    public float MuzzleVelocityBuff { get; set; }
    public float DamageBuff { get; set; }
    public float ArmourPenetrationBuff { get; set; }
    public float AccuracyBuff { get; set; }
    public float RateOfFireBuff { get; set; }
    public float RangeBuff { get; set; }

    public static TurretModBuff Default()
    {
        return new TurretModBuff()
        {
            MuzzleVelocityBuff = 0f,
            DamageBuff = 0f,
            ArmourPenetrationBuff = 0f,
            AccuracyBuff = 0f,
            RateOfFireBuff = 0
        };
    }
}

public static class StandardBuffs
{
    public static DynamicBuff UndercrewedDebuff(int currCrew, int operationalCrew, int skeletonCrew)
    {
        DynamicBuff crewNumBuf = DynamicBuff.Default();
        if (currCrew >= operationalCrew)
        {
        }
        else if (currCrew >= skeletonCrew)
        {
            float diff = currCrew - skeletonCrew;
            float maxDiff = operationalCrew - skeletonCrew;
            float movBuff = -0.75f * (1f - (diff / maxDiff));
            crewNumBuf.SpeedFactor = movBuff;
            crewNumBuf.AcceleraionFactor = movBuff;
            crewNumBuf.WeaponRateOfFireFactor = movBuff;
            crewNumBuf.WeaponAccuracyFactor = -0.2f * (1f - (diff / maxDiff));
            crewNumBuf.WeaponVsStrikeCraftFactor = 0;
            crewNumBuf.RepairRateModifier = -1000;
            crewNumBuf.ShieldRechargeRateModifier = 0;
        }
        else
        {
            crewNumBuf.SpeedFactor = -0.75f;
            crewNumBuf.AcceleraionFactor = -0.75f;
            crewNumBuf.WeaponRateOfFireFactor = -0.75f;
            crewNumBuf.WeaponAccuracyFactor = -0.2f;
            crewNumBuf.WeaponVsStrikeCraftFactor = 0;
            crewNumBuf.RepairRateModifier = -1000;
            crewNumBuf.ShieldRechargeRateModifier = -1;
        }
        return crewNumBuf;
    }

    public static DynamicBuff CrewExperienceBuff(int Level, bool underStrength)
    {
        if (Level > 0 && underStrength)
        {
            return DynamicBuff.Default();
        }
        DynamicBuff res = DynamicBuff.Default();
        switch (Level)
        {
            case 0:
                // Recruit
                res.WeaponAccuracyFactor = -0.5f;
                res.WeaponRateOfFireFactor = -0.25f;
                res.WeaponVsStrikeCraftFactor = -0.5f;
                res.AcceleraionFactor = -0.25f;
                res.SpeedFactor = -0.25f;
                res.RepairRateModifier = -1;
                res.ShieldRechargeRateModifier = -1;
                break;
            case 1:
                // Trained
                // Default values
                break;
            case 2:
                // Experienced
                res.WeaponAccuracyFactor = 0.1f;
                res.AcceleraionFactor = 0.05f;
                res.SpeedFactor = 0.05f;
                break;
            case 3:
                // Veteran
                res.WeaponAccuracyFactor = 0.1f;
                res.WeaponRateOfFireFactor = 0.1f;
                res.WeaponVsStrikeCraftFactor = 0.2f;
                res.RepairRateModifier = 1;
                res.ShieldRechargeRateModifier = 1;
                break;
            case 4:
                // Elite
                res.WeaponAccuracyFactor = 0.2f;
                res.WeaponRateOfFireFactor = 0.1f;
                res.WeaponVsStrikeCraftFactor = 0.2f;
                res.AcceleraionFactor = 0.1f;
                res.SpeedFactor = 0.1f;
                res.RepairRateModifier = 1;
                res.ShieldRechargeRateModifier = 1;
                break;
            default:
                break;
        };
        return res;
    }

    //public static DynamicBuff MediumCruiserBuff()
    //{
    //    DynamicBuff res = DynamicBuff.Default();
    //    res.HitPointModifiers.MediumBarbette = 75;
    //    return res;
    //}

    //public static DynamicBuff DemiBattleshipBuff()
    //{
    //    DynamicBuff res = DynamicBuff.Default();
    //    res.HitPointModifiers.SmallBarbette = 25;
    //    res.HitPointModifiers.SmallTurret = 25;
    //    return res;
    //}

    //public static DynamicBuff BattleshipBuff()
    //{
    //    DynamicBuff res = DynamicBuff.Default();
    //    res.HitPointModifiers.SmallBarbette = 50;
    //    res.HitPointModifiers.SmallTurret = 50;
    //    return res;
    //}
}
