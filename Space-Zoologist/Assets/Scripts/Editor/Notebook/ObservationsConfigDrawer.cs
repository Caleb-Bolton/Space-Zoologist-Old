﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomPropertyDrawer(typeof(ObservationsConfig))]
public class ObservationsConfigDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        SerializedProperty scaffold = property.FindPropertyRelative(nameof(scaffold));
        SerializedProperty initialEntries = property.FindPropertyRelative(nameof(initialEntries));

        // Set height for only control
        position.height = EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

        // Do a foldout
        property.isExpanded = EditorGUI.Foldout(position, property.isExpanded, label);
        position.y += position.height;

        if(property.isExpanded)
        {
            EditorGUI.indentLevel++;
            NotebookEditorUtility.EnclosureScaffoldAndArrayField(position, scaffold, initialEntries);
            EditorGUI.indentLevel--;
        }
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        SerializedProperty scaffold = property.FindPropertyRelative(nameof(scaffold));
        SerializedProperty initialEntries = property.FindPropertyRelative(nameof(initialEntries));

        // Set height for one control
        float height = EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

        if(property.isExpanded)
        {
            // Add in height for the scaffold and the foldout of the initial texts
            height += EditorGUI.GetPropertyHeight(scaffold);
            height += NotebookEditorUtility.GetSizeControlledArrayHeight(initialEntries);
        }

        return height;
    }
}
