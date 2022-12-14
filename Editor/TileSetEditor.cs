using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor;
using UnityEditor.UIElements;

namespace Cliff
{
    [CustomEditor(typeof(TileSet))]
    public class TileSetEditor : Editor
    {
        public override VisualElement CreateInspectorGUI()
        {
            var root = new VisualElement();

            Action<string> prop = (path) =>
            {
                var p = new PropertyField();
                p.bindingPath = path;
                root.Add(p);
            };

            prop("_transform");
            prop("_blockSize");
            prop("_meshGround");

            for (int index = 1; index < 15; ++index)
            {
                var container = new VisualElement();
                root.Add(container);

                var binary = Convert.ToString(index, 2).PadLeft(4, '0');
                var labelBinary = new Label(binary);
                labelBinary.style.unityFontStyleAndWeight = FontStyle.Bold;
                container.Add(labelBinary);

                SetupMesh($"_ground._mesh{binary}", "Ground", container, root);
                SetupMesh($"_cliff._mesh{binary}", "Cliff", container, root);
            }

            {
                var labelTitle = new Label("Fill by Prefix");
                labelTitle.style.unityFontStyleAndWeight = FontStyle.Bold;
                root.Add(labelTitle);

                var fillObject = new ObjectField("Object");
                fillObject.objectType = typeof(Transform);
                root.Add(fillObject);

                var groundPrefix = new TextField("Ground Prefix");
                root.Add(groundPrefix);
                var cliff = new TextField("Cliff Prefix");
                root.Add(cliff);

                var buttonFill = new Button();
                buttonFill.text = "Fill";
                root.Add(buttonFill);
                buttonFill.RegisterCallback<MouseUpEvent>(e =>
                    FillByPrefix(fillObject.value as Transform, groundPrefix.value, cliff.value));
            }

            return root;
        }

        private void SetupMesh(string bindingPath, string label, VisualElement container, VisualElement root)
        {
            var inner = new VisualElement();
            inner.style.flexDirection = FlexDirection.Row;
            inner.style.marginLeft = 10;
            container.Add(inner);

            var labelCliff = new Label(label);
            labelCliff.style.width = 100;
            labelCliff.style.unityTextAlign = TextAnchor.MiddleLeft;
            inner.Add(labelCliff);

            var buttonAdd = new Button(() =>
            {
                var property = serializedObject.FindProperty(bindingPath);
                property.arraySize += 1;
                serializedObject.ApplyModifiedProperties();
            });
            buttonAdd.text = "+";
            inner.Add(buttonAdd);

            var buttonRem = new Button(() =>
            {
                var property = serializedObject.FindProperty(bindingPath);
                if (property.arraySize == 0)
                {
                    return;
                }
                property.arraySize -= 1;
                serializedObject.ApplyModifiedProperties();
            });
            buttonRem.text = "-";
            inner.Add(buttonRem);

            var list = new VisualElement();
            list.style.flexDirection = FlexDirection.Row;
            list.style.flexWrap = Wrap.Wrap;
            inner.Add(list);

            SetupList(bindingPath, list);

            var propertyForSize = serializedObject.FindProperty(bindingPath + ".Array");
            propertyForSize.Next(true);
            root.TrackPropertyValue(propertyForSize, prop => SetupList(bindingPath, list));
        }

        private void SetupList(string bindingPath, VisualElement container)
        {
            var property = serializedObject.FindProperty(bindingPath + ".Array");
            var endProperty = property.GetEndProperty();

            int childIndex = 0;
            property.NextVisible(true); // Expand the first child.
            do
            {
                // Stop if you reach the end of the array
                if (SerializedProperty.EqualContents(property, endProperty))
                    break;

                // Skip the array size property
                if (property.propertyType == SerializedPropertyType.ArraySize)
                    continue;

                ObjectField element;

                // Find an existing element or create one
                if (childIndex < container.childCount)
                {
                    element = (ObjectField)container[childIndex];
                }
                else
                {
                    element = new ObjectField();
                    element.objectType = typeof(Mesh);
                    element.style.height = 18;
                    container.Add(element);
                }

                element.BindProperty(property);

                ++childIndex;
            }
            while (property.NextVisible(false)); // Never expand children.

            // Remove excess elements if the array is now smaller
            while (childIndex < container.childCount)
            {
                container.RemoveAt(container.childCount - 1);
            }
        }

        void FillByPrefix(Transform root, string prefixGround, string prefixCliff)
        {
            Action<SerializedProperty, string> fill = (SerializedProperty prop, string prefix) =>
            {
                // TODO use trie for better performance
                for (int index = 1; index < 15; ++index)
                {
                    var binary = Convert.ToString(index, 2).PadLeft(4, '0');
                    var prefixBinary = prefix + binary;

                    {
                        var meshProp = prop.FindPropertyRelative($"_mesh{binary}");
                        meshProp.arraySize = 0;
                    }

                    foreach (Transform child in root)
                    {
                        var meshFilter = child.GetComponent<MeshFilter>();
                        if (!meshFilter || !meshFilter.sharedMesh)
                        {
                            continue;
                        }

                        if (child.name.StartsWith(prefixBinary))
                        {
                            var meshProp = prop.FindPropertyRelative($"_mesh{binary}");
                            meshProp.arraySize += 1;
                            meshProp = meshProp.GetArrayElementAtIndex(meshProp.arraySize - 1);
                            meshProp.objectReferenceValue = meshFilter.sharedMesh;
                        }
                    }
                }
            };

            fill(serializedObject.FindProperty("_ground"), prefixGround);
            fill(serializedObject.FindProperty("_cliff"), prefixCliff);

            serializedObject.ApplyModifiedProperties();
        }
    }
}