using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(Sc_Map))]
public class MapEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        Sc_Map map = (Sc_Map)target;


        if (GUILayout.Button("Generate Dungeon"))
        {
            map.GenerateMap();
        }

        if (GUILayout.Button("Clear Dungeon"))
        {
            map.ClearGOList();
        }
    }
}
