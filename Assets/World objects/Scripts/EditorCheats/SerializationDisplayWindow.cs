using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class SerializationDisplayWindow : EditorWindow
{
    [MenuItem("Serialization/Show Text")]
    public static void OpenCustomWindow()
    {
        EditorWindow win = GetWindow<SerializationDisplayWindow>();
        GUIContent title = new GUIContent("YAML for this object");
        win.titleContent = title;
    }

    void OnGUI()
    {
        _scrollPos = GUILayout.BeginScrollView(_scrollPos, GUIStyle.none);
        GUILayout.TextArea(_data);
        EditorGUILayout.EndScrollView();
    }

    void OnDestroy()
    {
        _data = "";
    }

    public void SetText(string text)
    {
        _data = text;
    }

    private string _data = "";
    private Vector2 _scrollPos;
}
