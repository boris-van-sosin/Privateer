using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(ObjectPrototypes))]
public class ShipComponentTemplateExportHelper : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        GUILayout.Space(10f);
        GUILayout.BeginVertical();

        if (GUILayout.Button("Serialize Power plant"))
        {
            ShipComponentTemplateDefinition def = new ShipComponentTemplateDefinition()
            {
                ComponentName = "Power converter",
                ComponentType = "Power converter",
                ComponentGlobalMaxHitPoints = 300,
                AllowedSlotTypes = PowerPlant.DefaultComponent(null).AllowedSlotTypes.ToArray(),
                MinShipSize = ObjectFactory.ShipSize.Sloop.ToString(),
                MaxShipSize = ObjectFactory.ShipSize.CapitalShip.ToString(),
                PowerPlantDefinition = new PowerPlantTemplateDefinition()
                {
                    PowerOutput = 6,
                    HeatOutput = 1
                }
            };
            string yaml = HierarchySerializer.SerializeObject(def);
            SerializationDisplayWindow window = EditorWindow.CreateWindow<SerializationDisplayWindow>();
            window.SetText(yaml);
        }

        if (GUILayout.Button("Serialize Capacitor bank"))
        {
            ShipComponentTemplateDefinition def = new ShipComponentTemplateDefinition()
            {
                ComponentName = "Capacitor bank",
                ComponentType = "Capacitor bank",
                ComponentGlobalMaxHitPoints = 0,
                AllowedSlotTypes = CapacitorBank.DefaultComponent(null).AllowedSlotTypes.ToArray(),
                MinShipSize = ObjectFactory.ShipSize.Sloop.ToString(),
                MaxShipSize = ObjectFactory.ShipSize.CapitalShip.ToString(),
                CapacitorBankDefinition = new CapacitorBankTemplateDefinition()
                {
                    PowerCapacity = 50
                }
            };
            string yaml = HierarchySerializer.SerializeObject(def);
            SerializationDisplayWindow window = EditorWindow.CreateWindow<SerializationDisplayWindow>();
            window.SetText(yaml);
        }

        if (GUILayout.Button("Serialize Heat sink"))
        {
            ShipComponentTemplateDefinition def = new ShipComponentTemplateDefinition()
            {
                ComponentName = "Heat sink",
                ComponentType = "Heat sink",
                ComponentGlobalMaxHitPoints = 0,
                AllowedSlotTypes = HeatSink.DefaultComponent(null).AllowedSlotTypes.ToArray(),
                MinShipSize = ObjectFactory.ShipSize.Sloop.ToString(),
                MaxShipSize = ObjectFactory.ShipSize.CapitalShip.ToString(),
                HeatSinkDefinition = new HeatSinkTemplateDefinition()
                {
                    HeatCapacity = 50
                }
            };
            string yaml = HierarchySerializer.SerializeObject(def);
            SerializationDisplayWindow window = EditorWindow.CreateWindow<SerializationDisplayWindow>();
            window.SetText(yaml);
        }

        if (GUILayout.Button("Serialize Heat exchange"))
        {
            ShipComponentTemplateDefinition def = new ShipComponentTemplateDefinition()
            {
                ComponentName = "Heat exchange",
                ComponentType = "Heat exchange",
                ComponentGlobalMaxHitPoints = 0,
                AllowedSlotTypes = HeatExchange.DefaultComponent(null).AllowedSlotTypes.ToArray(),
                MinShipSize = ObjectFactory.ShipSize.Sloop.ToString(),
                MaxShipSize = ObjectFactory.ShipSize.CapitalShip.ToString(),
                HeatExchangeDefinition = new HeatExchangeTemplateDefinition()
                {
                    CoolignRate = 6
                }
            };
            string yaml = HierarchySerializer.SerializeObject(def);
            SerializationDisplayWindow window = EditorWindow.CreateWindow<SerializationDisplayWindow>();
            window.SetText(yaml);
        }

        if (GUILayout.Button("Serialize Damage control"))
        {
            ShipComponentTemplateDefinition def = new ShipComponentTemplateDefinition()
            {
                ComponentName = "Damage control center",
                ComponentType = "Damage control center",
                ComponentGlobalMaxHitPoints = 300,
                AllowedSlotTypes = DamageControlNode.DefaultComponent(null).AllowedSlotTypes.ToArray(),
                MinShipSize = ObjectFactory.ShipSize.Sloop.ToString(),
                MaxShipSize = ObjectFactory.ShipSize.CapitalShip.ToString(),
                DamageControlDefinition = new DamageControlTemplateDefinition()
                {
                    HullMaxHitPointRegeneration = 2,
                    SystemMaxHitPointRegeneration = 2,
                    ArmorMaxPointRegeneration = 1,
                    TimeOutOfCombatToRepair = 10.0f,
                    PowerUsage = 4,
                    HeatGeneration = 0
                }
            };
            string yaml = HierarchySerializer.SerializeObject(def);
            SerializationDisplayWindow window = EditorWindow.CreateWindow<SerializationDisplayWindow>();
            window.SetText(yaml);
        }

        if (GUILayout.Button("Serialize Electromagnetic clamps"))
        {
            ShipComponentTemplateDefinition def = new ShipComponentTemplateDefinition()
            {
                ComponentName = "Electromagnetic clamps",
                ComponentType = "Electromagnetic clamps",
                ComponentGlobalMaxHitPoints = 1,
                AllowedSlotTypes = ElectromagneticClamps.DefaultComponent(null).AllowedSlotTypes.ToArray(),
                MinShipSize = ObjectFactory.ShipSize.Sloop.ToString(),
                MaxShipSize = ObjectFactory.ShipSize.CapitalShip.ToString(),
                ElectromagneticClampsDefinition = new ElectromagneticClampsTemplateDefinition()
                {
                    PowerUsage = 4,
                    HeatGeneration = 0
                }
            };
            string yaml = HierarchySerializer.SerializeObject(def);
            SerializationDisplayWindow window = EditorWindow.CreateWindow<SerializationDisplayWindow>();
            window.SetText(yaml);
        }

        if (GUILayout.Button("Serialize Extra armour"))
        {
            ShipComponentTemplateDefinition def = new ShipComponentTemplateDefinition()
            {
                ComponentName = "Extra armour plating",
                ComponentType = "Extra armour plating",
                ComponentGlobalMaxHitPoints = 0,
                AllowedSlotTypes = new ExtraArmour(1, 1, ObjectFactory.ShipSize.Sloop, ObjectFactory.ShipSize.Sloop).AllowedSlotTypes.ToArray(),
                MinShipSize = ObjectFactory.ShipSize.Sloop.ToString(),
                MaxShipSize = ObjectFactory.ShipSize.CapitalShip.ToString(),
                ExtraArmourDefinition = new ExtraArmourTemplateDefinition()
                {
                    ArmourAmount = 50,
                    MitigationArmourAmount = 100
                }
            };
            string yaml = HierarchySerializer.SerializeObject(def);
            SerializationDisplayWindow window = EditorWindow.CreateWindow<SerializationDisplayWindow>();
            window.SetText(yaml);
        }

        if (GUILayout.Button("Serialize Fire control"))
        {
            ShipComponentTemplateDefinition def = new ShipComponentTemplateDefinition()
            {
                ComponentName = "Fire control computer",
                ComponentType = "Fire control computer",
                ComponentGlobalMaxHitPoints = 300,
                AllowedSlotTypes = FireControlGeneral.DefaultComponent(null).AllowedSlotTypes.ToArray(),
                MinShipSize = ObjectFactory.ShipSize.Sloop.ToString(),
                MaxShipSize = ObjectFactory.ShipSize.CapitalShip.ToString(),
                FireControlGeneralDefinition = new FireControlGeneralTemplateDefinition()
                {
                    PowerUsage = 4,
                    HeatGeneration = 1,
                    WeaponAccuracyFactor = 0.1f
                }
            };
            string yaml = HierarchySerializer.SerializeObject(def);
            SerializationDisplayWindow window = EditorWindow.CreateWindow<SerializationDisplayWindow>();
            window.SetText(yaml);
        }

        if (GUILayout.Button("Serialize Shield generator"))
        {
            ShipComponentTemplateDefinition def = new ShipComponentTemplateDefinition()
            {
                ComponentName = "Shield generator",
                ComponentType = "Shield generator",
                ComponentGlobalMaxHitPoints = 300,
                AllowedSlotTypes = ShieldGenerator.DefaultComponent(null).AllowedSlotTypes.ToArray(),
                MinShipSize = ObjectFactory.ShipSize.Sloop.ToString(),
                MaxShipSize = ObjectFactory.ShipSize.CapitalShip.ToString(),
                ShieldGeneratorDefinition = new ShieldGeneratorTemplateDefinition()
                {
                    MaxShieldPoints = 1000,
                    PowerUsage = 2,
                    HeatGeneration = 1,
                    MaxShieldPointRegeneration = 2,
                    PowerPerShieldRegeneration = 3,
                    HeatPerShieldRegeneration = 2,
                    PowerToRestart = 20,
                    HeatToRestart = 10,
                    RestartDelay = 20
                }
            };
            string yaml = HierarchySerializer.SerializeObject(def);
            SerializationDisplayWindow window = EditorWindow.CreateWindow<SerializationDisplayWindow>();
            window.SetText(yaml);
        }

        if (GUILayout.Button("Serialize Engine"))
        {
            ShipComponentTemplateDefinition def = new ShipComponentTemplateDefinition()
            {
                ComponentName = "Ship engine",
                ComponentType = "Ship engine",
                ComponentGlobalMaxHitPoints = 300,
                AllowedSlotTypes = ShipEngine.DefaultComponent(null).AllowedSlotTypes.ToArray(),
                MinShipSize = ObjectFactory.ShipSize.Sloop.ToString(),
                MaxShipSize = ObjectFactory.ShipSize.CapitalShip.ToString(),
                ShipEngineDefinition = new ShipEngineTemplateDefinition()
                {
                    PowerUsage = 2,
                    HeatGeneration = 2
                }
            };
            string yaml = HierarchySerializer.SerializeObject(def);
            SerializationDisplayWindow window = EditorWindow.CreateWindow<SerializationDisplayWindow>();
            window.SetText(yaml);
        }

        if (GUILayout.Button("Serialize Ship armoury"))
        {
            ShipComponentTemplateDefinition def = new ShipComponentTemplateDefinition()
            {
                ComponentName = "Ship armoury",
                ComponentType = "Ship armoury",
                ComponentGlobalMaxHitPoints = 300,
                AllowedSlotTypes = ShipArmoury.DefaultComponent(null).AllowedSlotTypes.ToArray(),
                MinShipSize = ObjectFactory.ShipSize.Sloop.ToString(),
                MaxShipSize = ObjectFactory.ShipSize.CapitalShip.ToString(),
                ShipArmouryDefinition = new ShipArmouryTemplateDefinition()
                {
                    CrewCapacity = 5
                }
            };
            string yaml = HierarchySerializer.SerializeObject(def);
            SerializationDisplayWindow window = EditorWindow.CreateWindow<SerializationDisplayWindow>();
            window.SetText(yaml);
        }

        if (GUILayout.Button("Serialize Turret Mod Buff"))
        {
            TurretModBuffApplier buff = new TurretModBuffApplier()
            {
                Name = "Placeholder",
                TurretModKey = TurretMod.None
            };
            TurretModBuff innerBuff = TurretModBuff.Default();
            innerBuff.ApplyToWeapon = "Weapon";
            innerBuff.ApplyToAmmo = "Ammo";
            buff.TurretModBuffs = new TurretModBuff[] { innerBuff };
            string yaml = HierarchySerializer.SerializeObject(buff);
            SerializationDisplayWindow window = EditorWindow.CreateWindow<SerializationDisplayWindow>();
            window.SetText(yaml);
        }

        GUILayout.EndVertical();
    }
}
