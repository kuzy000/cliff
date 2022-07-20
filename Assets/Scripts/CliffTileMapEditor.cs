using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(CliffTileMap))]
public class CliffTileMapEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        var tileMap = (CliffTileMap)target;
        if (GUILayout.Button("Update chunks"))
        {
            tileMap.UpdateChunks();
        }
    }
}
