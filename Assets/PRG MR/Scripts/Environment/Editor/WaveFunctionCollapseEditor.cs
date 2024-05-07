using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(WaveFunctionCollapse))]
public class WaveFunctionCollapseEditor : Editor
{
    public override void OnInspectorGUI()
    {
        // Draw the default inspector
        DrawDefaultInspector();

        // Update the serialized object
        serializedObject.Update();

        var WFC = target as WaveFunctionCollapse;
        
        if(GUILayout.Button("Start WFC"))
        {
            WFC.StartWFC();
        }

        if(GUILayout.Button("Clear"))
        {
            WFC.CleanUpMap();
        }
    }
}
