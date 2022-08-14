using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[InitializeOnLoad]
public class CliffEditor
{
    public static CliffTileMapTool tileMapTool => _tileMapTool;
    public static CliffTileMapEditor tileMapEditor => _tileMapEditor;

    static CliffEditor()
    {
        EditorApplication.update += Update;
    }

    public static void Enqueue(Action f)
    {
        _actions.Enqueue(f);
    }

    public static void SetTileMapTool(CliffTileMapTool tileMapTool)
    {
        _tileMapTool = tileMapTool;
    }

    public static void SetTileMapEditor(CliffTileMapEditor tileMapEditor)
    {
        _tileMapEditor = tileMapEditor;
    }

    private static Queue<Action> _actions = new Queue<Action>();
    private static CliffTileMapTool _tileMapTool;
    private static CliffTileMapEditor _tileMapEditor;

    private static void Update()
    {
        while (_actions.Count > 0)
        {
            _actions.Dequeue()();
        }
    }
}
