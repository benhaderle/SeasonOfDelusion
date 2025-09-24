using System.Collections;
using System.Collections.Generic;
using UnityEditor.UI;
using UnityEngine;
using UnityEditor;
using UnityEditorInternal;

[CustomEditor(typeof(Workout))]
public class WorkoutEditor : Editor
{
    SerializedProperty propDisplayName;
    SerializedProperty propGoalVO2;
    SerializedProperty propType;
    SerializedProperty propRouteLineData;
    SerializedProperty propIntervals;
    SerializedProperty propEffects;
    SerializedProperty propSaveData;
    ReorderableList intervalsList;

    protected void OnEnable()
    {
        propDisplayName = serializedObject.FindProperty("displayName");
        propGoalVO2 = serializedObject.FindProperty("goalVO2");
        propType = serializedObject.FindProperty("workoutType");
        propRouteLineData = serializedObject.FindProperty("routeLineData");
        propIntervals = serializedObject.FindProperty("intervals");
        propEffects = serializedObject.FindProperty("effects");
        propSaveData = serializedObject.FindProperty("saveData");

        intervalsList = new ReorderableList(serializedObject, propIntervals, true, true, true, true)
        {
            drawElementCallback = DrawIntervalElement,
            drawHeaderCallback = DrawIntervalListHeader
        };
    }

    public override void OnInspectorGUI()
    {
        EditorGUILayout.PropertyField(propSaveData);
        propDisplayName.stringValue = EditorGUILayout.TextField("Display Name", propDisplayName.stringValue);
        EditorGUILayout.PropertyField(propType);
        EditorGUILayout.PropertyField(propRouteLineData);
        propGoalVO2.floatValue = EditorGUILayout.FloatField("Goal VO2", propGoalVO2.floatValue);

        intervalsList.DoLayoutList();

        EditorGUILayout.PropertyField(propEffects);

        serializedObject.ApplyModifiedProperties();
    }


    void DrawIntervalListHeader(Rect r)
    {
        const int checkboxSize = 14;
        const int closedSize = 50;

        Rect rLabel = r;
        rLabel.width = r.width - checkboxSize - closedSize;
        EditorGUI.LabelField(rLabel, "Interval Sets");
    }

    // Draws the elements on the list
    void DrawIntervalElement(Rect r, int i, bool isActive, bool isFocused)
    {
        r.yMin += 1;
        r.yMax -= 2;
        SerializedProperty prop = propIntervals.GetArrayElementAtIndex(i);
        SerializedProperty repeats = prop.FindPropertyRelative(nameof(Interval.repeats));
        SerializedProperty length = prop.FindPropertyRelative(nameof(Interval.length));
        SerializedProperty rest = prop.FindPropertyRelative(nameof(Interval.rest));

        using (var chChk = new EditorGUI.ChangeCheckScope())
        {
            GUIContent[] labels = new GUIContent[] { new GUIContent("Repeats"), new GUIContent("Length"), new GUIContent("Rest") };
            float[] values = new float[] { repeats.intValue, length.floatValue, rest.floatValue };
            MultiFloatField(r, labels, values);

            if (chChk.changed)
            {
                repeats.intValue = Mathf.Max(1, (int)values[0]);
                length.floatValue = Mathf.Max(0, values[1]);
                rest.floatValue = Mathf.Max(0, values[2]);
            }
        }
    }

    private void MultiFloatField(Rect position, GUIContent[] subLabels, float[] values)
    {
        int eCount = values.Length;
        const int kSpacingSubLabel = 4;
        float w = (position.width - (eCount - 1) * kSpacingSubLabel) / eCount;
        Rect nr = new Rect(position) { width = w };
        float t = EditorGUIUtility.labelWidth;
        int l = EditorGUI.indentLevel;
        EditorGUI.indentLevel = 0;
        for (int i = 0; i < values.Length; i++)
        {
            EditorGUIUtility.labelWidth = CalcPrefixLabelWidth(subLabels[i]);
            values[i] = EditorGUI.FloatField(nr, subLabels[i], values[i]);
            nr.x += w + kSpacingSubLabel;
        }

        EditorGUIUtility.labelWidth = t;
        EditorGUI.indentLevel = l;
    }
    
    // from: https://github.com/Unity-Technologies/UnityCsReference/blob/master/Editor/Mono/EditorGUI.cs
    private float CalcPrefixLabelWidth(GUIContent label, GUIStyle style = null)
    {
        if (style == null) style = EditorStyles.label;
        return style.CalcSize(label).x;
    }
}
