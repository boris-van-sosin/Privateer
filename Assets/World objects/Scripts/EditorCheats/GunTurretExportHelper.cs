using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(GunTurret))]
public class GunTurretExportHelper : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        TurretBase turret = (TurretBase)target;
        if (GUILayout.Button("Serialize"))
        {
            string yaml = HierarchySerializer.SerializeObject(turret.transform);
            SerializationDisplayWindow window = EditorWindow.CreateWindow<SerializationDisplayWindow>();
            window.SetText(yaml);
        }
    }
}
