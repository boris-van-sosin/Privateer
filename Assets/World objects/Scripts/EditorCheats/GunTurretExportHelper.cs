using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(GunTurret))]
public class GunTurretExportHelper : TurretBaseExportHelper
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
    }
}
