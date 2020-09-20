using System;

[Serializable]
public class ShipComponentTemplateDefinition
{
    public string ComponentName { get; set; }
    public string ComponentType { get; set; }
    public int ComponentGlobalMaxHitPoints { get; set; }
    public string MinShipSize { get; set; }
    public string MaxShipSize { get; set; }
    public string[] AllowedSlotTypes { get; set; }
    public PowerPlantTemplateDefinition PowerPlantDefinition { get; set; }
    public CapacitorBankTemplateDefinition CapacitorBankDefinition { get; set; }
    public HeatSinkTemplateDefinition HeatSinkDefinition { get; set; }
    public HeatExchangeTemplateDefinition HeatExchangeDefinition { get; set; }
    public DamageControlTemplateDefinition DamageControDefinition { get; set; }
    public ElectromagneticClampsTemplateDefinition ElectromagneticClampsDefinition { get; set; }
    public ExtraArmourTemplateDefinition ExtraArmourDefinition { get; set; }
    public FireControlGeneralTemplateDefinition FireControlGeneralDefinition { get; set; }
    public ShieldGeneratorTemplateDefinition ShieldGeneratorDefinition { get; set; }
    public ShipEngineTemplateDefinition ShipEngineDefinition { get; set; }
    public ShipArmouryTemplateDefinition ShipArmouryDefinition { get; set;}

    public ShipComponentBase CreateComponent()
    {
        ObjectFactory.ShipSize minSz, maxSz;
        if (!Enum.TryParse(MinShipSize, out minSz) || !Enum.TryParse(MaxShipSize, out maxSz))
        {
            return null;
        }

        if (PowerPlantDefinition != null)
        {
            return new PowerPlant(ComponentGlobalMaxHitPoints, ComponentGlobalMaxHitPoints, PowerPlantDefinition.PowerOutput, PowerPlantDefinition.HeatOutput, minSz, maxSz);
        }
        if (CapacitorBankDefinition != null)
        {
            return new CapacitorBank(CapacitorBankDefinition.PowerCapacity, minSz, maxSz);
        }
        if (HeatSinkDefinition != null)
        {
            return new HeatSink(HeatSinkDefinition.HeatCapacity, minSz, maxSz);
        }
        if (HeatExchangeDefinition != null)
        {
            return new HeatExchange(HeatExchangeDefinition.CoolignRate, minSz, maxSz);
        }
        if (DamageControDefinition != null)
        {
            return new DamageControlNode(ComponentGlobalMaxHitPoints, ComponentGlobalMaxHitPoints, DamageControDefinition.HullMaxHitPointRegeneration, DamageControDefinition.ArmorMaxPointRegeneration, DamageControDefinition.ArmorMaxPointRegeneration, DamageControDefinition.PowerUsage, DamageControDefinition.HeatGeneration, DamageControDefinition.TimeOutOfCombatToRepair, minSz, maxSz);
        }
        if (ElectromagneticClampsDefinition != null)
        {
            return new ElectromagneticClamps(ComponentGlobalMaxHitPoints, ComponentGlobalMaxHitPoints, ElectromagneticClampsDefinition.PowerUsage, ElectromagneticClampsDefinition.HeatGeneration, minSz, maxSz);
        }
        if (ExtraArmourDefinition != null)
        {
            return new ExtraArmour(ExtraArmourDefinition.ArmourAmount, ExtraArmourDefinition.MitigationArmourAmount, minSz, maxSz);
        }
        if (FireControlGeneralDefinition != null)
        {
            return new FireControlGeneral(ComponentGlobalMaxHitPoints, ComponentGlobalMaxHitPoints, FireControlGeneralDefinition.WeaponAccuracyFactor, FireControlGeneralDefinition.PowerUsage, FireControlGeneralDefinition.HeatGeneration, minSz, maxSz);
        }
        if (ShieldGeneratorDefinition != null)
        {
            return new ShieldGenerator(ComponentGlobalMaxHitPoints, ComponentGlobalMaxHitPoints,
                                       ShieldGeneratorDefinition.MaxShieldPoints, ShieldGeneratorDefinition.MaxShieldPointRegeneration,
                                       ShieldGeneratorDefinition.PowerUsage, ShieldGeneratorDefinition.HeatGeneration,
                                       ShieldGeneratorDefinition.PowerPerShieldRegeneration, ShieldGeneratorDefinition.HeatPerShieldRegeneration,
                                       ShieldGeneratorDefinition.PowerToRestart, ShieldGeneratorDefinition.HeatToRestart, ShieldGeneratorDefinition.RestartDelay,
                                       minSz, maxSz);
        }
        if (ShipEngineDefinition != null)
        {
            return new ShipEngine(ComponentGlobalMaxHitPoints, ComponentGlobalMaxHitPoints, ShipEngineDefinition.PowerUsage, ShipEngineDefinition.HeatGeneration, minSz, maxSz);
        }
        if (ShipArmouryDefinition != null)
        {
            return new ShipArmoury(ShipArmouryDefinition.CrewCapacity, minSz, maxSz);
        }

        return null;
    }

    public ShipComponentTemplateDefinition CreateCopy()
    {
        ShipComponentTemplateDefinition res = new ShipComponentTemplateDefinition()
        {
            ComponentType = ComponentType,
            ComponentName = ComponentName,
            MinShipSize = MinShipSize,
            MaxShipSize = MaxShipSize,
            ComponentGlobalMaxHitPoints = ComponentGlobalMaxHitPoints,
            AllowedSlotTypes = new string[AllowedSlotTypes.Length],
            PowerPlantDefinition = PowerPlantTemplateDefinition.CreateCopy(PowerPlantDefinition),
            CapacitorBankDefinition = CapacitorBankTemplateDefinition.CreateCopy(CapacitorBankDefinition),
            HeatSinkDefinition = HeatSinkTemplateDefinition.CreateCopy(HeatSinkDefinition),
            HeatExchangeDefinition = HeatExchangeTemplateDefinition.CreateCopy(HeatExchangeDefinition),
            DamageControDefinition = DamageControlTemplateDefinition.CreateCopy(DamageControDefinition),
            ElectromagneticClampsDefinition = ElectromagneticClampsTemplateDefinition.CreateCopy(ElectromagneticClampsDefinition),
            ExtraArmourDefinition = ExtraArmourTemplateDefinition.CreateCopy(ExtraArmourDefinition),
            FireControlGeneralDefinition = FireControlGeneralTemplateDefinition.CreateCopy(FireControlGeneralDefinition),
            ShieldGeneratorDefinition = ShieldGeneratorTemplateDefinition.CreateCopy(ShieldGeneratorDefinition),
            ShipEngineDefinition = ShipEngineTemplateDefinition.CreateCopy(ShipEngineDefinition),
            ShipArmouryDefinition = ShipArmouryTemplateDefinition.CreateCopy(ShipArmouryDefinition)
        };
        AllowedSlotTypes.CopyTo(res.AllowedSlotTypes, 0);
        return res;
    }
}

[Serializable]
public class PowerPlantTemplateDefinition
{
    public int PowerOutput { get; set; }
    public int HeatOutput { get; set; }
    public static PowerPlantTemplateDefinition CreateCopy(PowerPlantTemplateDefinition src)
    {
        if (src == null)
        {
            return null;
        }
        else
        {
            return new PowerPlantTemplateDefinition()
            {
                PowerOutput = src.PowerOutput,
                HeatOutput = src.HeatOutput
            };
        }
    }
}

[Serializable]
public class CapacitorBankTemplateDefinition
{
    public int PowerCapacity { get; set; }
    public static CapacitorBankTemplateDefinition CreateCopy(CapacitorBankTemplateDefinition src)
    {
        if (src == null)
        {
            return null;
        }
        else
        {
            return new CapacitorBankTemplateDefinition()
            {
                PowerCapacity = src.PowerCapacity
            };
        }
    }
}

[Serializable]
public class HeatSinkTemplateDefinition
{
    public int HeatCapacity { get; set; }
    public static HeatSinkTemplateDefinition CreateCopy(HeatSinkTemplateDefinition src)
    {
        if (src == null)
        {
            return null;
        }
        else
        {
            return new HeatSinkTemplateDefinition()
            {
                HeatCapacity = src.HeatCapacity
            };
        }
    }
}

[Serializable]
public class HeatExchangeTemplateDefinition
{
    public int CoolignRate { get; set; }
    public static HeatExchangeTemplateDefinition CreateCopy(HeatExchangeTemplateDefinition src)
    {
        if (src == null)
        {
            return null;
        }
        else
        {
            return new HeatExchangeTemplateDefinition()
            {
                CoolignRate = src.CoolignRate
            };
        }
    }
}

[Serializable]
public class DamageControlTemplateDefinition
{
    public int PowerUsage { get; set; }
    public int HeatGeneration { get; set; }
    public int HullMaxHitPointRegeneration { get; set; }
    public int SystemMaxHitPointRegeneration { get; set; }
    public int ArmorMaxPointRegeneration { get; set; }
    public float TimeOutOfCombatToRepair { get; set; }
    public static DamageControlTemplateDefinition CreateCopy(DamageControlTemplateDefinition src)
    {
        if (src == null)
        {
            return null;
        }
        else
        {
            return new DamageControlTemplateDefinition()
            {
                PowerUsage = src.PowerUsage,
                HeatGeneration = src.HeatGeneration,
                HullMaxHitPointRegeneration = src.HullMaxHitPointRegeneration,
                SystemMaxHitPointRegeneration = src.SystemMaxHitPointRegeneration,
                ArmorMaxPointRegeneration = src.ArmorMaxPointRegeneration,
                TimeOutOfCombatToRepair = src.TimeOutOfCombatToRepair
            };
        }
    }
}

[Serializable]
public class ElectromagneticClampsTemplateDefinition
{
    public int PowerUsage { get; set; }
    public int HeatGeneration { get; set; }
    public static ElectromagneticClampsTemplateDefinition CreateCopy(ElectromagneticClampsTemplateDefinition src)
    {
        if (src == null)
        {
            return null;
        }
        else
        {
            return new ElectromagneticClampsTemplateDefinition()
            {
                PowerUsage = src.PowerUsage,
                HeatGeneration = src.HeatGeneration
            };
        }
    }
}

[Serializable]
public class ExtraArmourTemplateDefinition
{
    public int ArmourAmount { get; set; }
    public int MitigationArmourAmount { get; set; }
    public static ExtraArmourTemplateDefinition CreateCopy(ExtraArmourTemplateDefinition src)
    {
        if (src == null)
        {
            return null;
        }
        else
        {
            return new ExtraArmourTemplateDefinition()
            {
                ArmourAmount = src.ArmourAmount,
                MitigationArmourAmount = src.MitigationArmourAmount
            };
        }
    }
}

[Serializable]
public class FireControlGeneralTemplateDefinition
{
    public int PowerUsage { get; set; }
    public int HeatGeneration { get; set; }
    public float WeaponAccuracyFactor { get; set; }
    public static FireControlGeneralTemplateDefinition CreateCopy(FireControlGeneralTemplateDefinition src)
    {
        if (src == null)
        {
            return null;
        }
        else
        {
            return new FireControlGeneralTemplateDefinition()
            {
                PowerUsage = src.PowerUsage,
                HeatGeneration = src.HeatGeneration,
                WeaponAccuracyFactor = src.WeaponAccuracyFactor
            };
        }
    }
}

[Serializable]
public class ShieldGeneratorTemplateDefinition
{
    public int MaxShieldPoints { get; set; }
    public int MaxShieldPointRegeneration { get; set; }
    public int PowerUsage { get; set; }
    public int PowerPerShieldRegeneration { get; set; }
    public int PowerToRestart { get; set; }
    public float RestartDelay { get; set; }
    public int HeatGeneration { get; set; }
    public int HeatPerShieldRegeneration { get; set; }
    public int HeatToRestart { get; set; }
    public static ShieldGeneratorTemplateDefinition CreateCopy(ShieldGeneratorTemplateDefinition src)
    {
        if (src == null)
        {
            return null;
        }
        else
        {
            return new ShieldGeneratorTemplateDefinition()
            {
                MaxShieldPoints = src.MaxShieldPoints,
                MaxShieldPointRegeneration = src.MaxShieldPointRegeneration,
                PowerUsage = src.PowerUsage,
                PowerPerShieldRegeneration = src.PowerPerShieldRegeneration,
                PowerToRestart = src.PowerToRestart,
                HeatGeneration = src.HeatGeneration,
                HeatPerShieldRegeneration = src.HeatPerShieldRegeneration,
                HeatToRestart = src.HeatToRestart,
                RestartDelay = src.RestartDelay
            };
        }
    }
}

[Serializable]
public class ShipEngineTemplateDefinition
{
    public int PowerUsage { get; set; }
    public int HeatGeneration { get; set; }
    public static ShipEngineTemplateDefinition CreateCopy(ShipEngineTemplateDefinition src)
    {
        if (src == null)
        {
            return null;
        }
        else
        {
            return new ShipEngineTemplateDefinition()
            {
                PowerUsage = src.PowerUsage,
                HeatGeneration = src.HeatGeneration
            };
        }
    }
}

[Serializable]
public class ShipArmouryTemplateDefinition
{
    public int CrewCapacity { get; set; }
    public static ShipArmouryTemplateDefinition CreateCopy(ShipArmouryTemplateDefinition src)
    {
        if (src == null)
        {
            return null;
        }
        else
        {
            return new ShipArmouryTemplateDefinition()
            {
                CrewCapacity = src.CrewCapacity
            };
        }
    }
}
