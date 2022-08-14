using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace Cliff
{
    [InitializeOnLoad]
    public class EditorUtility
    {
        public static TileMapTool tileMapTool => _tileMapTool;
        public static TileMapEditor tileMapEditor => _tileMapEditor;

        static EditorUtility()
        {
            EditorApplication.update += Update;
        }

        public static void Enqueue(Action f)
        {
            _actions.Enqueue(f);
        }

        public static void SetTileMapTool(TileMapTool tileMapTool)
        {
            _tileMapTool = tileMapTool;
        }

        public static void SetTileMapEditor(TileMapEditor tileMapEditor)
        {
            _tileMapEditor = tileMapEditor;
        }

        private static Queue<Action> _actions = new Queue<Action>();
        private static TileMapTool _tileMapTool;
        private static TileMapEditor _tileMapEditor;

        private static void Update()
        {
            while (_actions.Count > 0)
            {
                _actions.Dequeue()();
            }
        }
    }
}