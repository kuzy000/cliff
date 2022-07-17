using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.EditorTools;
using UnityEditor.ShortcutManagement;

[EditorTool("TileMap Tool", typeof(TileMap))]
public class TileMapTool : EditorTool, IDrawSelectedHandles
{
    private int _x;
    private int _y;

    private bool _isValid = false;

    private bool _isPaint = false;
    private bool _isInverse = false;

    private int _lastHotControl = -1;

    private bool[,] _isPainted;

    private TileMap _tileMap;

    void OnEnable()
    {
        base.OnActivated();
        _tileMap = (TileMap)target;
    }

    public override void OnToolGUI(EditorWindow window)
    {
        if (!(window is SceneView sceneView))
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
                        Paint(_x, _y);
                        _tileMap.Generate();
                    }
                }
                break;

            case EventType.MouseUp:
                LockFocus(false);
                EndPaint();
                break;
        }
    }

    // IDrawSelectedHandles interface allows tools to draw gizmos when the target objects are selected, but the tool
    // has not yet been activated. This allows you to keep MonoBehaviour free of debug and gizmo code.
    public void OnDrawHandles()
    {
        //var tileMap = (TileMap)target;
        //Handles.zTest = UnityEngine.Rendering.CompareFunction.LessEqual;
        //Handles.color = Color.white;
        //DrawGrid();

        //Handles.zTest = UnityEngine.Rendering.CompareFunction.Greater;
        //Handles.color = Color.gray;
        //DrawGrid();
    }

    private void UpdateCursor()
    {
        var tileMap = (TileMap)target;

        var plane = new Plane(Vector3.up, 0);
        var ray = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);

        float d;
        if (plane.Raycast(ray, out d))
        {
            var p = ray.GetPoint(d);
            p.x /= tileMap.block.x;
            p.z /= tileMap.block.z;

            int _lastX = _x;
            int _lastY = _y;

            _x = (int)Mathf.Floor(p.x);
            _y = (int)Mathf.Floor(p.z);

            _isValid = tileMap.IsValid(_x, _y);

            if (_isValid && _isPaint)
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
        var tileMap = (TileMap)target;

        for (int x = 0; x <= tileMap.width; ++x)
        {
            var p = tileMap.transform.position + new Vector3(x * tileMap.block.x, 0, 0);
            Handles.DrawLine(p, p + new Vector3(0, 0, tileMap.height * tileMap.block.z));
        }

        for (int y = 0; y <= tileMap.height; ++y)
        {
            var p = tileMap.transform.position + new Vector3(0, 0, y * tileMap.block.z);
            Handles.DrawLine(p, p + new Vector3(tileMap.width * tileMap.block.x, 0, 0));
        }
    }

    private void BeginPaint()
    {
        var tileMap = (TileMap)target;

        _isPaint = true;
        _isInverse = (Event.current.modifiers & EventModifiers.Shift) != EventModifiers.None;
        _isPainted = new bool[tileMap.width, tileMap.height];
    }

    private void PaintLine(int x1, int y1, int x2, int y2)
    {
        Debug.Assert(_isPaint);

        BresenhamLine(x1, y1, x2, y2, (x, y) => Paint(x, y));

        _tileMap.Generate();
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

        if (!_tileMap.IsValid(x, y))
        {
            return;
        }

        if (!_isPainted[x, y])
        {
            _tileMap.tiles[x, y].height += _isInverse ? -1 : +1;
            _isPainted[x, y] = true;
        }
    }

    private void EndPaint()
    {
        _isPaint = false;
        _isPainted = null;
    }

    private void DrawBrush()
    {
        if (_isValid)
        {
            DrawRect(_x, _y, _tileMap.tiles[_x, _y].height);
        }
    }

    private void DrawRect(int x, int y, int z)
    {
        Handles.zTest = UnityEngine.Rendering.CompareFunction.LessEqual;
        Handles.color = new Color(0f, 1f, 0f, 1f);
        DrawRectPass(x, y, z);

        Handles.zTest = UnityEngine.Rendering.CompareFunction.Greater;
        Handles.color = new Color(0f, 0.4f, 0f, 1f);
        DrawRectPass(x, y, z);
    }

    private void DrawRectPass(int x, int y, int z)
    {
        var tileMap = (TileMap)target;
        var py = tileMap.transform.position.y + z * tileMap.block.y + 0.1f;
        var a = tileMap.transform.position + new Vector3(x * tileMap.block.x, 0, y * tileMap.block.z);
        var b = tileMap.transform.position + new Vector3((x + 1) * tileMap.block.x, 0, (y + 1) * tileMap.block.z);

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
