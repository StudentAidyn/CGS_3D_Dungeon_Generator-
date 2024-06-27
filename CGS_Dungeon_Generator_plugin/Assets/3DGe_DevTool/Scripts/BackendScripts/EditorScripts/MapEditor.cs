using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

// Custom Editor Display of Map Script

[CustomEditor(typeof(Map))]
public class MapEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        Map map = (Map)target;


        if (GUILayout.Button("Generate Dungeon"))
        {
            map.GenerateMap();
        }

        if (GUILayout.Button("Clear Dungeon"))
        {
            map.ClearMap();
        }
    }
}
