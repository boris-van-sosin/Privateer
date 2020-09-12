﻿using System;


[Serializable]
public class ShipComponentTemplateDefinition
{
    public string ComponentName { get; set; }
    public string ComponentMaxHitPoints { get; set; }
    public string MinShipSize { get; set; }
    public string MaxShipSize { get; set; }
    public string[] AllowedSlotTypes { get; set; }
    public PowerPlantTemplateDefinition PowerPlantDefinition {get; set;}
    public CapacitorBankTemplateDefinition CapacitorBankDefinition { get; set; }
    public HeatSinkTemplateDefinition HeatSinkDefinition { get; set; }
    public HeatExchangeTemplateDefinition HeatExchangeDefinition { get; set; }
    public DamageControTemplateDefinition DamageControDefinition { get; set; }
    public ElectromagneticClampsTemplateDefinition ElectromagneticClampsDefinition { get; set; }
    public ExtraArmourTemplateDefinition ExtraArmourDefinition { get; set; }
    public FireControlGeneralTemplateDefinition FireControlGeneralDefinition { get; set; }
    public ShieldGeneratorTemplateDefinition ShieldGeneratorDefinition { get; set; }
    public ShipEngineTemplateDefinition ShipEngineDefinition { get; set; }
}

[Serializable]
public class PowerPlantTemplateDefinition
{
    public int PowerOutput { get; set; }
    public int HeatOutput { get; set; }
}

[Serializable]
public class CapacitorBankTemplateDefinition
{
    public int PowerCapacity { get; set; }
}

[Serializable]
public class HeatSinkTemplateDefinition
{
    public int HeatCapacity { get; set; }
    public int HeatOutput { get; set; }
}

[Serializable]
public class HeatExchangeTemplateDefinition
{
    public int CoolignRate { get; set; }
}

[Serializable]
public class DamageControTemplateDefinition
{
    public int PowerUsage { get; set; }
    public int HullMaxHitPointRegeneration { get; set; }
    public int SystemMaxHitPointRegeneration { get; set; }
    public int ArmorMaxPointRegeneration { get; set; }
    public float TimeOutOfCombatToRepair { get; set; }
}

[Serializable]
public class ElectromagneticClampsTemplateDefinition
{
    public int EnergyPerPulse { get; set; }
    public int HeatPerPulse { get; set; }
}

[Serializable]
public class ExtraArmourTemplateDefinition
{
    public int ArmourAmount { get; set; }
    public int MitigationArmourAmount { get; set; }
}

[Serializable]
public class FireControlGeneralTemplateDefinition
{
    public int PowerUsage { get; set; }
    public float WeaponAccuracyFactor { get; set; }
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
}

[Serializable]
public class ShipEngineTemplateDefinition
{
    public int EnergyPerThrust { get; set; }
    public int HeatPerThrust { get; set; }
}