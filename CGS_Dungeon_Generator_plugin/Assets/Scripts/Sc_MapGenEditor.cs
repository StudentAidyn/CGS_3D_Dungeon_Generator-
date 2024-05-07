using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(Sc_MapGenerator))]
public class Sc_MapGenEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        Sc_MapGenerator gen = (Sc_MapGenerator)target;

        if(GUILayout.Button("Generate Dungeon"))
        {
            gen.Generate();
        }

        if (GUILayout.Button("Clear Dungeon"))
        {
            gen.ClearGOList();
        }
    }
}
