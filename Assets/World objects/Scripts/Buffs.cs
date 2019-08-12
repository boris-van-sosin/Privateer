using System.Collections.Generic;

public struct Buff
{
    public float WeaponAccuracyFactor;
    public float WeaponRateOfFireFactor;
    public float WeaponVsStrikeCraftFactor;
    public float SpeedFactor;
    public float AcceleraionFactor;

    public int RepairRateModifier;
    public int ShieldRechargeRateModifier;

    public HitPointBuff HitPointModifiers;

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

        target.HitPointModifiers.Combine(other.HitPointModifiers);
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
        HitPointModifiers.ResetToDefault();
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
            ShieldRechargeRateModifier = 0,
            HitPointModifiers = HitPointBuff.Default()
        };
    }

    public struct HitPointBuff
    {
        public int Hull;

        public int Component;

        public int SmallBarbette;
        public int SmallTurret;
        public int SmallBroadside;

        public int MediumBarbette;
        public int MediumTurret;
        public int MediumBroadside;

        public int HeavyBarbette;
        public int HeavyTurret;
        public int HeavyBroadside;

        public int TorpedoTube;

        public void Combine(HitPointBuff other)
        {
            Hull += other.Hull;

            Component += other.Component;

            SmallBarbette += other.SmallBarbette;
            SmallTurret += other.SmallTurret;
            SmallBroadside += other.SmallBroadside;

            MediumBarbette += other.MediumBarbette;
            MediumTurret += other.MediumTurret;
            MediumBroadside += other.MediumBroadside;

            HeavyBarbette += other.HeavyBarbette;
            HeavyTurret += other.HeavyTurret;
            HeavyBroadside += other.HeavyBroadside;

            TorpedoTube += other.TorpedoTube;
        }

        public void ResetToDefault()
        {
            Hull = 0;

            Component = 0;

            SmallBarbette = 0;
            SmallTurret = 0;
            SmallBroadside = 0;

            MediumBarbette = 0;
            MediumTurret = 0;
            MediumBroadside = 0;

            HeavyBarbette = 0;
            HeavyTurret = 0;
            HeavyBroadside = 0;

            TorpedoTube = 0;
        }

        public static HitPointBuff Default()
        {
            return new HitPointBuff()
            {
                Hull = 0,

                Component = 0,

                SmallBarbette = 0,
                SmallTurret = 0,
                SmallBroadside = 0,

                MediumBarbette = 0,
                MediumTurret = 0,
                MediumBroadside = 0,

                HeavyBarbette = 0,
                HeavyTurret = 0,
                HeavyBroadside = 0,

                TorpedoTube = 0
            };
        }
    }
}

public static class StandardBuffs
{
    public static void SetUnderCrewedDebuff(ref Buff crewNumBuf, int currCrew, int operationalCrew, int skeletonCrew)
    {
        if (currCrew >= operationalCrew)
        {
            crewNumBuf.ResetToDefault();
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
    }

    public static Buff MediumCruiserBuff()
    {
        Buff res = Buff.Default();
        res.HitPointModifiers.MediumBarbette = 75;
        return res;
    }

    public static Buff DemiBattleshipBuff()
    {
        Buff res = Buff.Default();
        res.HitPointModifiers.SmallBarbette = 25;
        res.HitPointModifiers.SmallTurret = 25;
        return res;
    }

    public static Buff BattleshipBuff()
    {
        Buff res = Buff.Default();
        res.HitPointModifiers.SmallBarbette = 50;
        res.HitPointModifiers.SmallTurret = 50;
        return res;
    }
}
