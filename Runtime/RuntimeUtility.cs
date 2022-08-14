using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace Cliff
{
    public static class RuntimeUtility
    {
        public static T InstantiatePrefab<T>(T original, Vector3 position, Quaternion rotation, Transform parent) where T : Component
        {
            var obj = (T)PrefabUtility.InstantiatePrefab(original, parent);
            obj.transform.SetPositionAndRotation(position, rotation);
            return obj;
        }
    }
}