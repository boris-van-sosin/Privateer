using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(Ship))]
public class ShipExporterHelper : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        GUILayout.Space(10f);
        GUILayout.BeginVertical();

        GUILayout.Label("Mesh AssetBundle path:");
        _meshAssetBundlePath = GUILayout.TextField(_meshAssetBundlePath);
        GUILayout.Space(5f);

        GUILayout.Label("Mesh Asset path:");
        _meshAssetPath = GUILayout.TextField(_meshAssetPath);
        GUILayout.Space(5f);

        GUILayout.Label("Particle system AssetBundle path:");
        _particleSystemAssetBundlePath = GUILayout.TextField(_particleSystemAssetBundlePath);
        GUILayout.Space(5f);

        GUILayout.Label("Particle system Asset path:");
        _particleSystemAssetPath = GUILayout.TextField(_particleSystemAssetPath);
        GUILayout.Space(5f);

        Ship ship = (Ship)target;
        if (GUILayout.Button("Serialize"))
        {
            string yaml = HierarchySerializer.SerializeObject(ship, _meshAssetBundlePath, _meshAssetPath, _particleSystemAssetBundlePath, _particleSystemAssetPath);
            SerializationDisplayWindow window = EditorWindow.CreateWindow<SerializationDisplayWindow>();
            window.SetText(yaml);
        }

        if (Application.isPlaying && ship != null && ship.CurrMitigationArmour != null)
        {
            GUILayout.Space(10f);
            GUILayout.BeginHorizontal();
            GUILayout.Label("Hit points:");
            GUILayout.TextArea(ship.HullHitPoints.ToString());
            GUILayout.EndHorizontal();

            GUILayout.Space(5f);
            GUILayout.Label("Current mitigation armour:");

            GUILayout.BeginHorizontal();
            GUILayout.Label("Fore:");
            GUILayout.TextArea(ship.CurrMitigationArmour[Ship.ShipSection.Fore].ToString());
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Aft:");
            GUILayout.TextArea(ship.CurrMitigationArmour[Ship.ShipSection.Aft].ToString());
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Left:");
            GUILayout.TextArea(ship.CurrMitigationArmour[Ship.ShipSection.Left].ToString());
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Right:");
            GUILayout.TextArea(ship.CurrMitigationArmour[Ship.ShipSection.Right].ToString());
            GUILayout.EndHorizontal();

            GUILayout.Space(5f);
            GUILayout.BeginHorizontal();
            GUILayout.Label("Crew:");
            GUILayout.TextArea(ship.AllCrew.Where(c => c.Status == ShipCharacter.CharacterStaus.Active).Count().ToString());
            GUILayout.EndHorizontal();
        }
        GUILayout.EndVertical();
    }

    private string _meshAssetBundlePath = "";
    private string _meshAssetPath = "";
    private string _particleSystemAssetBundlePath = "";
    private string _particleSystemAssetPath = "";
}
