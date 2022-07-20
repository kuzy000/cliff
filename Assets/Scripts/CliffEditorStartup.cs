using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[InitializeOnLoad]
public class CliffEditorStartup
{
    static CliffEditorStartup()
    {
        EditorApplication.update += Update;
    }

    public void Enqueue(Action f)
    {
        _actions.Enqueue(f);
    }

    private static void Update()
    {
        while (_actions.Count > 0)
        {
            _actions.Dequeue()();
        }
    }

    private static Queue<Action> _actions = new Queue<Action>();
}
