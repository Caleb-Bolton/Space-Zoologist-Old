using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomPropertyDrawer(typeof(ResearchCategoryTypeEditableAttribute))]
public class ResearchCategoryTypeEditableDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        if (property.type == nameof(ResearchCategory))
        {
            // Set the height to one control height
            position.height = EditorGUIUtility.standardVerticalSpacing + EditorGUIUtility.singleLineHeight;
            // Set out the foldout
            property.isExpanded = EditorGUI.Foldout(position, property.isExpanded, label);
            position.y += position.height;
            // Edit other properties if expanded
            if(property.isExpanded)
            {
                EditorGUI.indentLevel++;

                // Get the attribute
                ResearchCategoryTypeEditableAttribute editableAttribute = (ResearchCategoryTypeEditableAttribute)attribute;

                // If the type is editable, then add a property field for it
                if(editableAttribute.TypeEditable)
                {
                    EditorGUI.PropertyField(position, property.FindPropertyRelative("type"));
                    position.y += position.height;
                }
                // In any case, add a property field for the research category name
                EditorGUI.PropertyField(position, property.FindPropertyRelative("name"));
                position.y += position.height;

                // Add property field for the image
                EditorGUI.PropertyField(position, property.FindPropertyRelative("image"));

                EditorGUI.indentLevel--;
            }
        }
        else EditorGUI.PropertyField(position, property, true);
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        if (property.type == nameof(ResearchCategory))
        {
            float height = EditorGUIUtility.standardVerticalSpacing + EditorGUIUtility.singleLineHeight;
            if (property.isExpanded)
            {
                ResearchCategoryTypeEditableAttribute editableAttribute = (ResearchCategoryTypeEditableAttribute)attribute;

                // If the type is editable, then add a property field for it
                if (editableAttribute.TypeEditable)
                {
                    height += EditorGUIUtility.standardVerticalSpacing + EditorGUIUtility.singleLineHeight;
                }

                height += 2f * (EditorGUIUtility.standardVerticalSpacing + EditorGUIUtility.singleLineHeight);
            }
            return height;
        }
        else return EditorGUI.GetPropertyHeight(property, true);
    }
}
