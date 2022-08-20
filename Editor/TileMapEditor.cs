using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor;
using UnityEditor.EditorTools;
using UnityEditor.UIElements;

namespace Cliff
{
    [CustomEditor(typeof(TileMap))]
    public class TileMapEditor : Editor
    {
        public int toolBrushSize => _toolBrushSize;
        public TileMapTool.BrushShape toolBrushShape => _toolBrushShape;

        const string resourceFilename = "CliffEditorUXML";
        public override VisualElement CreateInspectorGUI()
        {
            VisualElement root = new VisualElement();
            var visualTree = Resources.Load(resourceFilename) as VisualTreeAsset;
            visualTree.CloneTree(root);

            _sliderBrushSize = root.Q<SliderInt>("brush-size");
            _sliderBrushSize.RegisterValueChangedCallback<int>(e => _toolBrushSize = e.newValue);

            var enumBrushShape = root.Q<EnumField>("brush-shape");
            enumBrushShape.Init(TileMapTool.BrushShape.Circle);
            enumBrushShape.RegisterValueChangedCallback<Enum>(e => _toolBrushShape = (TileMapTool.BrushShape)e.newValue);

            var regenerateAll = root.Q<Button>("regenerate-all");
            regenerateAll.RegisterCallback<MouseUpEvent>(e => (target as TileMap).SyncWithData(true));

            return root;
        }

        public void SetToolBrushSize(int value)
        {
            _sliderBrushSize.value = value;
        }

        private void OnEnable()
        {
            EditorUtility.SetTileMapEditor(this);
        }

        private void OnDisable()
        {
            EditorUtility.SetTileMapEditor(null);
        }

        private int _toolBrushSize = 1;
        private TileMapTool.BrushShape _toolBrushShape = TileMapTool.BrushShape.Circle;

        private SliderInt _sliderBrushSize;
    }
}