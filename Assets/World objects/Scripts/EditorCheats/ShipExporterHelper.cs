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

        Ship ship = (Ship)target;
        if (GUILayout.Button("Serialize"))
        {
            string yaml = HierarchySerializer.SerializeObject(ship);
            SerializationDisplayWindow window = EditorWindow.CreateWindow<SerializationDisplayWindow>();
            window.SetText(yaml);
        }
    }
}
