using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.EditorTools;
using UnityEditor.ShortcutManagement;

namespace Cliff
{
    [EditorTool("TileMap Tool", typeof(TileMap))]
    public class TileMapTool : EditorTool, IDrawSelectedHandles
    {
        public enum BrushShape
        {
            Rectangle,
            Circle,
        }

        [SerializeField] public int brushSize => EditorUtility.tileMapEditor.toolBrushSize;
        public BrushShape brushShape => EditorUtility.tileMapEditor.toolBrushShape;

        //private int _brushSize = 3;

        private Vector3 blockSize => _tileMap.tileSet.blockSize;

        private int _x;
        private int _y;

        private bool _isValid = false;

        private bool _isPaint = false;
        private bool _isInverse = false;

        private int _lastHotControl = -1;
        private int _undoGroup = -1;

        private string _undoTitle => "Paint on CliffTileMap";

        private TileMap _tileMap;
        private TileMapData _tileMapData;

        private Dictionary<(int, int), ChunkData> _chunks = new Dictionary<(int, int), ChunkData>();

        void OnEnable()
        {
            EditorUtility.SetTileMapTool(this);
            BindToTileMap();
        }

        void OnDisable()
        {
            EditorUtility.SetTileMapTool(null);

            _tileMap = null;
            _tileMapData = null;
        }

        private void BindToTileMap()
        {
            _tileMap = (TileMap)target;
            _tileMapData = _tileMap.tileMapData;
        }

        public override void OnToolGUI(EditorWindow window)
        {
            if (!(window is SceneView sceneView))
            {
                return;
            }

            BindToTileMap();
            if (_tileMapData == null)
            {
                return;
            }

            var tileMap = (TileMap)target;
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
                            PaintBrush(_x, _y);
                        }
                    }
                    break;

                case EventType.MouseUp:
                    LockFocus(false);
                    EndPaint();
                    break;

                case EventType.KeyDown:
                    switch (e.keyCode)
                    {
                        case KeyCode.LeftBracket:
                            EditorUtility.tileMapEditor.SetToolBrushSize(Math.Max(0, brushSize - 1));
                            e.Use();
                            break;
                        case KeyCode.RightBracket:
                            EditorUtility.tileMapEditor.SetToolBrushSize(Math.Min(32, brushSize + 1));
                            e.Use();
                            break;
                    }
                    break;
            }

            _tileMap.SyncWithData();
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

        private class ChunkData
        {
            public ChunkData(int chunkSize)
            {
                this.isPainted = new bool[chunkSize, chunkSize];
            }

            public bool[,] isPainted;
        }

        private bool IsPainted(int x, int y)
        {
            (int localX, int localY) = _tileMapData.GetLocalCoord(x, y);

            ChunkData chunkData;
            if (_chunks.TryGetValue(_tileMapData.GetChunkCoord(x, y), out chunkData))
            {
                return chunkData.isPainted[localX, localY];
            }

            return false;
        }

        private void SetPainted(int x, int y)
        {
            var chunkXY = _tileMapData.GetChunkCoord(x, y);

            ChunkData chunkData;
            if (!_chunks.TryGetValue(chunkXY, out chunkData))
            {
                chunkData = new ChunkData(_tileMapData.chunkSize);
                _chunks.Add(chunkXY, chunkData);
            }

            (int localX, int localY) = _tileMapData.GetLocalCoord(x, y);
            chunkData.isPainted[localX, localY] = true;
        }

        private void UpdateCursor()
        {
            var plane = new Plane(Vector3.up, 0);
            var ray = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);

            float d;
            if (plane.Raycast(ray, out d))
            {
                var p = ray.GetPoint(d);
                p += new Vector3(blockSize.x, 0, blockSize.z) * 0.5f;

                if (brushSize % 2 == 0)
                {
                    p.x += blockSize.x * 0.5f;
                    p.z += blockSize.z * 0.5f;
                }

                p.x /= blockSize.x;
                p.z /= blockSize.z;

                int _lastX = _x;
                int _lastY = _y;

                _x = (int)Mathf.Floor(p.x);
                _y = (int)Mathf.Floor(p.z);

                _isValid = true;

                if (_isPaint)
                {
                    PaintBrushLine(_lastX, _lastY, _x, _y);
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

        private void ForEachBrushCell(int brushX, int brushY, Action<int, int> f)
        {
            int a = brushSize / 2;
            int b = brushSize - a;

            for (int y = brushY - a; y < brushY + b; ++y)
            {
                for (int x = brushX - a; x < brushX + b; ++x)
                {
                    if (brushShape == BrushShape.Circle)
                    {
                        float r = (float)brushSize / 2 - 0.25f;
                        float fX = (x - brushX) + (brushSize % 2 == 0 ? 0.5f : 0f);
                        float fY = (y - brushY) + (brushSize % 2 == 0 ? 0.5f : 0f);

                        if (fX * fX + fY * fY < r * r)
                        {
                            f(x, y);
                        }
                    }
                    else
                    {
                        f(x, y);
                    }
                }
            }
        }

        public bool CanSetVertex(int x, int y, Vertex vertex)
        {
            var bl = _tileMapData.GetTile(x - 1, y - 1);
            var br = _tileMapData.GetTile(x - 0, y - 1);
            var tr = _tileMapData.GetTile(x - 0, y - 0);
            var tl = _tileMapData.GetTile(x - 1, y - 0);

            bl.tr = vertex;
            br.tl = vertex;
            tr.bl = vertex;
            tl.br = vertex;

            int height;
            TileShape shape;

            _tileMap.tileSet.GetHeightAndShape(bl, out height, out shape);
            if (!shape.isValid)
            {
                return false;
            }

            _tileMap.tileSet.GetHeightAndShape(br, out height, out shape);
            if (!shape.isValid)
            {
                return false;
            }

            _tileMap.tileSet.GetHeightAndShape(tr, out height, out shape);
            if (!shape.isValid)
            {
                return false;
            }

            _tileMap.tileSet.GetHeightAndShape(tl, out height, out shape);
            if (!shape.isValid)
            {
                return false;
            }

            return true;
        }

        private void DrawGrid()
        {
            foreach (var chunk in _tileMapData.chunks)
            {
                var offset =
                    Vector3.Scale(new Vector3(chunk.x * _tileMapData.chunkSize, 0, chunk.y * _tileMapData.chunkSize), blockSize);

                for (int x = 0; x <= _tileMapData.chunkSize; ++x)
                {
                    var p = offset + _tileMap.transform.position + new Vector3(x * blockSize.x, 0, 0);
                    Handles.DrawLine(p, p + new Vector3(0, 0, _tileMapData.chunkSize * blockSize.z));
                }

                for (int y = 0; y <= _tileMapData.chunkSize; ++y)
                {
                    var p = offset + _tileMap.transform.position + new Vector3(0, 0, y * blockSize.z);
                    Handles.DrawLine(p, p + new Vector3(_tileMapData.chunkSize * blockSize.x, 0, 0));
                }
            }
        }

        private void BeginPaint()
        {
            _isPaint = true;
            _isInverse = (Event.current.modifiers & EventModifiers.Shift) != EventModifiers.None;
            _chunks = new Dictionary<(int, int), ChunkData>();

            Undo.RecordObject(_tileMapData, _undoTitle);
            _undoGroup = Undo.GetCurrentGroup();
        }

        private void PaintBrushLine(int x1, int y1, int x2, int y2)
        {
            Debug.Assert(_isPaint);

            BresenhamLine(x1, y1, x2, y2, (x, y) => PaintBrush(x, y));
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

        private void PaintBrush(int brushX, int brushY)
        {
            Debug.Assert(_isPaint);

            ForEachBrushCell(brushX, brushY, (x, y) =>
            {
                if (!IsPainted(x, y))
                {
                    var vertex = PaintVertex(_tileMapData[x, y]);

                    if (CanSetVertex(x, y, vertex))
                    {
                        Undo.RecordObject(_tileMapData, _undoTitle);

                        _tileMapData[x, y] = vertex;
                        SetPainted(x, y);
                    }
                }
            });
        }

        private Vertex PaintVertex(Vertex vertex)
        {
            if (vertex.isEmpty)
            {
                vertex.height = 1;
            }
            else
            {
                vertex.height += _isInverse ? -1 : +1;
            }

            return vertex;
        }

        private void EndPaint()
        {
            _isPaint = false;
            _chunks = null;

            Undo.CollapseUndoOperations(_undoGroup);
            _undoGroup = -1;
        }

        private void DrawBrush()
        {
            if (_isValid)
            {
                ForEachBrushCell(_x, _y, (x, y) =>
                {
                    var vertex = _tileMapData[x, y];

                    DrawRect(x, y, vertex.height, !CanSetVertex(x, y, PaintVertex(vertex)));
                });
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
            var tileMap = (TileMap)target;
            var py = tileMap.transform.position.y + z * blockSize.y + 0.1f;
            var half = new Vector3(blockSize.x, 0, blockSize.z) * 0.5f;
            var a = tileMap.transform.position + new Vector3(x * blockSize.x, 0, y * blockSize.z) - half;
            var b = tileMap.transform.position + new Vector3((x + 1) * blockSize.x, 0, (y + 1) * blockSize.z) - half;

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
}
