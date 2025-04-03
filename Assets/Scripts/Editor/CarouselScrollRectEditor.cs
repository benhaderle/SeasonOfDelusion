using System.Collections;
using System.Collections.Generic;
using UnityEditor.UI;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(CarouselScrollRect))]
public class CarouselScrollRectEditor : ScrollRectEditor
{
    SerializedProperty propOnDragEnded;

    protected override void OnEnable()
    {
        base.OnEnable();

        propOnDragEnded = serializedObject.FindProperty("OnDragEnded");
    }

    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        EditorGUILayout.PropertyField(propOnDragEnded);

        serializedObject.ApplyModifiedProperties();
    }
}
