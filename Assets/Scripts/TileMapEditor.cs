using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(TileMap))]
public class TileMapEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        var tileMap = (TileMap)target;
        if (GUILayout.Button("Create"))
        {
            tileMap.Create(16, 16);
        }
        if (GUILayout.Button("Generate"))
        {
            tileMap.Generate();
        }
    }
}
