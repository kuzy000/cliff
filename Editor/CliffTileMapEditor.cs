using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor;
using UnityEditor.EditorTools;
using UnityEditor.UIElements;

[CustomEditor(typeof(CliffTileMap))]
public class CliffTileMapEditor : Editor
{
    public int toolBrushSize => _toolBrushSize;
    public CliffTileMapTool.BrushShape toolBrushShape => _toolBrushShape;

    const string resourceFilename = "CliffEditorUXML";
    public override VisualElement CreateInspectorGUI()
    {
        VisualElement root = new VisualElement();
        var visualTree = Resources.Load(resourceFilename) as VisualTreeAsset;
        visualTree.CloneTree(root);

        _sliderBrushSize = root.Q<SliderInt>("brush-size");
        _sliderBrushSize.RegisterValueChangedCallback<int>(e => _toolBrushSize = e.newValue);

        var enumBrushShape = root.Q<EnumField>("brush-shape");
        enumBrushShape.Init(CliffTileMapTool.BrushShape.Circle);
        enumBrushShape.RegisterValueChangedCallback<Enum>(e => _toolBrushShape = (CliffTileMapTool.BrushShape)e.newValue);

        return root;
    }

    public void SetToolBrushSize(int value)
    {
        _sliderBrushSize.value = value;
    }

    private void OnEnable()
    {
        CliffEditor.SetTileMapEditor(this);
    }

    private void OnDisable()
    {
        CliffEditor.SetTileMapEditor(null);
    }

    private int _toolBrushSize = 1;
    private CliffTileMapTool.BrushShape _toolBrushShape = CliffTileMapTool.BrushShape.Circle;

    private SliderInt _sliderBrushSize;
}
