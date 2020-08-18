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
        GUILayout.EndVertical();
    }

    private string _meshAssetBundlePath = "";
    private string _meshAssetPath = "";
    private string _particleSystemAssetBundlePath = "";
    private string _particleSystemAssetPath = "";
}
