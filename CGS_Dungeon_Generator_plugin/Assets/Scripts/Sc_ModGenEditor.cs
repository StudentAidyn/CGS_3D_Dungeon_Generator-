using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;

[CustomEditor(typeof(Sc_ModGenerator))]
public class Sc_ModGenEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        Sc_ModGenerator gen = (Sc_ModGenerator)target;

        if (GUILayout.Button("Generate Rotation Variants"))
        {
            gen.CreateRotatedVariants();
        }

        if (GUILayout.Button("DELETE Rotation Variants"))
        {
            gen.DeleteAllVariantsInFolder();
        }

        if (GUILayout.Button("Generate Connections"))
        {
            gen.CreateConnections();
        }

        if (GUILayout.Button("Reset Connections"))
        {
            gen.ResetModuleNeighbours();
        }
    }
}
