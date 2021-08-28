using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomPropertyDrawer(typeof(EnclosureID))]
public class EnclosureIDDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        // Get sub properties to edit
        SerializedProperty levelNumber = property.FindPropertyRelative(nameof(levelNumber));
        SerializedProperty enclosureNumber = property.FindPropertyRelative(nameof(enclosureNumber));
        GUIStyle textSyle = new GUIStyle(EditorStyles.label);
        textSyle.alignment = TextAnchor.MiddleCenter;

        // Set height for only one control
        position.height = EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

        // If we are in wide mode then inline the controls
        if(EditorGUIUtility.wideMode)
        {
            position = EditorGUI.PrefixLabel(position, label);
        }
        else
        {
            // Layout the label
            EditorGUI.LabelField(position, label);
            position.y += position.height;

            // Get the indented version of the rect
            EditorGUI.indentLevel++;
            position = EditorGUI.IndentedRect(position);
        }

        // Erase indent so that controls in the same line are not indented
        int oldIndent = EditorGUI.indentLevel;
        EditorGUI.indentLevel = 0;

        // Compute the width for one label and one control
        float levelLabelWidth = 40f;
        float enclosureLabelWidth = 20f;
        float controlWidth = (position.width - levelLabelWidth - enclosureLabelWidth) / 2f;

        // Layout the label for the level number
        position.width = levelLabelWidth;
        EditorGUI.LabelField(position, new GUIContent("Level"), textSyle);
        position.x += position.width;

        // Layout the level number
        position.width = controlWidth;
        EditorGUI.PropertyField(position, levelNumber, GUIContent.none);
        position.x += position.width;

        // Layout the label for the enclosure number
        position.width = enclosureLabelWidth;
        EditorGUI.LabelField(position, new GUIContent("E"), textSyle);
        position.x += position.width;

        // Layout the enclosure number
        position.width = controlWidth;
        EditorGUI.PropertyField(position, enclosureNumber, GUIContent.none);

        // Restore the old indent
        EditorGUI.indentLevel = oldIndent;
    }
    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        if (EditorGUIUtility.wideMode)
        {
            return EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
        }
        else return 2f * (EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing);
    }
}
