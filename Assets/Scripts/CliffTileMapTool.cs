using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.EditorTools;
using UnityEditor.ShortcutManagement;

[EditorTool("TileMap Tool", typeof(CliffTileMap))]
public class CliffTileMapTool : EditorTool, IDrawSelectedHandles
{
    private int _x;
    private int _y;

    private bool _isValid = false;

    private bool _isPaint = false;
    private bool _isInverse = false;

    private int _lastHotControl = -1;

    private CliffTileMap _tileMap;

    void OnEnable()
    {
        Debug.Log("CliffTileMapTool.OnEnable");

        _tileMap = (CliffTileMap)target;
        _tileMap.onCreateChunk += OnCreateChunk;
        _tileMap.onRemoveChunk += OnRemoveChunk;
        _tileMap.onTileSet += OnTileSet;

        var e = _tileMap.chunkEnumerator;
        while (e.MoveNext())
        {
            (int x, int y) = e.Current;
            OnCreateChunk(x, y);
        }
    }

    public override void OnToolGUI(EditorWindow window)
    {
        if (!(window is SceneView sceneView))
        {
            return;
        }
        var tileMap = (CliffTileMap)target;
        Handles.zTest = UnityEngine.Rendering.CompareFunction.Disabled;

        UpdateCursor();
        DrawBrush();

        var e = Event.current;
        switch (e.type)
        {
            case EventType.MouseDown:
                if (e.button == 0)
                {
                    LockFocus(true);
                    e.Use();

                    if (_isValid)
                    {
                        BeginPaint();
                        Paint(_x, _y);
                    }
                }
                break;

            case EventType.MouseUp:
                LockFocus(false);
                EndPaint();
                break;
        }

        _tileMap.UpdateChunks();
    }

    public void OnDrawHandles()
    {
        Handles.zTest = UnityEngine.Rendering.CompareFunction.LessEqual;
        Handles.color = new Color(1f, 1f, 1f, 0.1f);
        DrawGrid();

        Handles.zTest = UnityEngine.Rendering.CompareFunction.Greater;
        Handles.color = new Color(0.5f, 0.5f, 0.5f, 0.1f);
        DrawGrid();
    }

    private class Chunk
    {
        public Chunk(int chunkSize)
        {
            this.isPainted = new bool[chunkSize, chunkSize];
        }

        public void ClearIsPainted()
        {
            this.isPainted = new bool[this.isPainted.GetLength(0), this.isPainted.GetLength(0)];
        }

        public bool[,] isPainted;
    }

    private Dictionary<(int, int), Chunk> _chunks = new Dictionary<(int, int), Chunk>();

    private void OnCreateChunk(int x, int y)
    {
        Debug.Log($"OnCreateChunk: {x}, {y}");
        _chunks.Add((x, y), new Chunk(_tileMap.chunkSize));
    }

    private void OnRemoveChunk(int x, int y)
    {
        Debug.Log($"OnRemoveChunk: {x}, {y}");
        _chunks.Remove((x, y));
    }

    private void OnTileSet(int x, int y, CliffTileMap.Tile? tile)
    {
        Debug.Log($"OnTileSet: {x}, {y}, h: {(tile != null ? tile.Value.height : -999)}");
        if (_isPaint)
        {
            (int localX, int localY) = _tileMap.GetLocalCoord(x, y);
            _chunks[_tileMap.GetChunkCoord(x, y)].isPainted[localX, localY] = true;
        }
    }

    private bool IsPainted(int x, int y)
    {
        (int localX, int localY) = _tileMap.GetLocalCoord(x, y);

        Chunk chunk;
        if (_chunks.TryGetValue(_tileMap.GetChunkCoord(x, y), out chunk))
        {
            return chunk.isPainted[localX, localY];
        }

        return false;
    }

    private void SetPainted(int x, int y)
    {
        (int localX, int localY) = _tileMap.GetLocalCoord(x, y);
        _chunks[_tileMap.GetChunkCoord(x, y)].isPainted[localX, localY] = true;
    }


    private void UpdateCursor()
    {
        var plane = new Plane(Vector3.up, 0);
        var ray = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);

        float d;
        if (plane.Raycast(ray, out d))
        {
            var p = ray.GetPoint(d);
            p.x /= _tileMap.blockSize.x;
            p.z /= _tileMap.blockSize.z;

            int _lastX = _x;
            int _lastY = _y;

            _x = (int)Mathf.Floor(p.x);
            _y = (int)Mathf.Floor(p.z);

            _isValid = true;

            if (_isPaint)
            {
                PaintLine(_lastX, _lastY, _x, _y);
            }
        }
        else
        {
            _isValid = false;
        }
    }

    private void LockFocus(bool value)
    {
        if (value)
        {
            if (_lastHotControl != -1)
            {
                return;
            }

            _lastHotControl = GUIUtility.hotControl;
            GUIUtility.hotControl = GUIUtility.GetControlID(FocusType.Passive);
        }
        else
        {
            if (_lastHotControl == -1)
            {
                return;
            }

            GUIUtility.hotControl = _lastHotControl;
            _lastHotControl = -1;
        }
    }

    private void DrawGrid()
    {
        foreach ((int chunkX, int chunkY) in _chunks.Keys)
        {
            var offset =
                Vector3.Scale(new Vector3(chunkX * _tileMap.chunkSize, 0, chunkY * _tileMap.chunkSize), _tileMap.blockSize);

            for (int x = 0; x <= _tileMap.chunkSize; ++x)
            {
                var p = offset + _tileMap.transform.position + new Vector3(x * _tileMap.blockSize.x, 0, 0);
                Handles.DrawLine(p, p + new Vector3(0, 0, _tileMap.chunkSize * _tileMap.blockSize.z));
            }

            for (int y = 0; y <= _tileMap.chunkSize; ++y)
            {
                var p = offset + _tileMap.transform.position + new Vector3(0, 0, y * _tileMap.blockSize.z);
                Handles.DrawLine(p, p + new Vector3(_tileMap.chunkSize * _tileMap.blockSize.x, 0, 0));
            }
        }
    }

    private void BeginPaint()
    {
        var tileMap = (CliffTileMap)target;

        _isPaint = true;
        _isInverse = (Event.current.modifiers & EventModifiers.Shift) != EventModifiers.None;

        foreach (var chunk in _chunks.Values)
        {
            chunk.ClearIsPainted();
        }
    }

    private void PaintLine(int x1, int y1, int x2, int y2)
    {
        Debug.Assert(_isPaint);

        BresenhamLine(x1, y1, x2, y2, (x, y) => Paint(x, y));
    }


    private void BresenhamLine(int x, int y, int x2, int y2, Action<int, int> f)
    {
        int w = x2 - x;
        int h = y2 - y;
        int dx1 = 0, dy1 = 0, dx2 = 0, dy2 = 0;
        if (w < 0) dx1 = -1; else if (w > 0) dx1 = 1;
        if (h < 0) dy1 = -1; else if (h > 0) dy1 = 1;
        if (w < 0) dx2 = -1; else if (w > 0) dx2 = 1;
        int longest = Mathf.Abs(w);
        int shortest = Mathf.Abs(h);
        if (!(longest > shortest))
        {
            longest = Mathf.Abs(h);
            shortest = Mathf.Abs(w);
            if (h < 0) dy2 = -1; else if (h > 0) dy2 = 1;
            dx2 = 0;
        }
        int numerator = longest >> 1;
        for (int i = 0; i <= longest; i++)
        {
            f(x, y);
            numerator += shortest;
            if (!(numerator < longest))
            {
                numerator -= longest;
                x += dx1;
                y += dy1;
            }
            else
            {
                x += dx2;
                y += dy2;
            }
        }
    }

    private void Paint(int x, int y)
    {
        Debug.Assert(_isPaint);

        if (!IsPainted(x, y))
        {
            CliffTileMap.Tile tile;
            tile.height = _tileMap[x, y]?.height + (_isInverse ? -1 : +1) ?? 0;

            if (_tileMap.CanSetTile(x, y, tile))
            {
                _tileMap[x, y] = tile;
                SetPainted(x, y);
            }
        }
    }

    private void EndPaint()
    {
        _isPaint = false;
    }

    private void DrawBrush()
    {
        if (_isValid)
        {
            CliffTileMap.Tile tile;
            tile.height = _tileMap[_x, _y]?.height + (_isInverse ? -1 : +1) ?? 0;

            DrawRect(_x, _y, _tileMap[_x, _y]?.height ?? 0, !_tileMap.CanSetTile(_x, _y, tile));
        }
    }

    private void DrawRect(int x, int y, int z, bool red)
    {
        Handles.zTest = UnityEngine.Rendering.CompareFunction.LessEqual;
        Handles.color = red ? new Color(1f, 0f, 0f, 1f) : new Color(0f, 1f, 0f, 1f);
        DrawRectPass(x, y, z);

        Handles.zTest = UnityEngine.Rendering.CompareFunction.Greater;
        Handles.color = red ? new Color(0.4f, 0f, 0f, 1f) : new Color(0f, 0.4f, 0f, 1f);
        DrawRectPass(x, y, z);
    }

    private void DrawRectPass(int x, int y, int z)
    {
        var tileMap = (CliffTileMap)target;
        var py = tileMap.transform.position.y + z * tileMap.blockSize.y + 0.1f;
        var a = tileMap.transform.position + new Vector3(x * tileMap.blockSize.x, 0, y * tileMap.blockSize.z);
        var b = tileMap.transform.position + new Vector3((x + 1) * tileMap.blockSize.x, 0, (y + 1) * tileMap.blockSize.z);

        Vector3[] verts = new Vector3[]
        {
            new Vector3(a.x, py, a.z),
            new Vector3(a.x, py, b.z),
            new Vector3(b.x, py, b.z),
            new Vector3(b.x, py, a.z),
        };

        var centerColor = Handles.color;
        centerColor.a = 0.1f;

        Handles.DrawSolidRectangleWithOutline(verts, centerColor, Handles.color);
    }
}
