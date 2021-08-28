using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public static class NotebookEditorUtility
{
    public static void EnclosureScaffoldAndArrayField(Rect position, SerializedProperty enclosureScaffold, SerializedProperty array)
    {
        // Set height for only control
        position.height = EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

        // Edit enclosure scaffold and check for a change
        EditorGUI.PropertyField(position, enclosureScaffold, true);
        position.y += EditorGUI.GetPropertyHeight(enclosureScaffold);

        // If array is expanded and object reference exists then 
        // edit the array based on total scaffold levels
        if (enclosureScaffold.objectReferenceValue)
        {
            int totalLevels = (enclosureScaffold.objectReferenceValue as EnclosureScaffold).TotalLevels;
            ScaffoldedArrayField(position, array, totalLevels);
        }
        // If no object reference exists the array will be empty
        else
        {
            ScaffoldedArrayField(position, array, 0);
        }
    }

    public static void ScaffoldedArrayField(Rect position, SerializedProperty array, int arraySize)
    {
        SizeControlledArrayField(position, array, arraySize, (i) =>
        {
            string highLowComment;

            // Comment on the scaffold level. Number is switched so this will help with understanding
            if (i == 0) highLowComment = " (Highest)";
            else if (i < arraySize - 1) highLowComment = " (Lower)";
            else highLowComment = " (Lowest)";

            return new GUIContent("Scaffolding Level " + i + highLowComment);
        });
    }

    public static void SizeControlledArrayField(Rect position, SerializedProperty array, int arraySize, Func<int, GUIContent> elementLabelGetter = null)
    {
        // Set the array size for this array
        array.arraySize = arraySize;

        if(arraySize > 0)
        {
            // Layout the foldout for the texts
            array.isExpanded = EditorGUI.Foldout(position, array.isExpanded, new GUIContent(array.displayName));
            position.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

            if (array.isExpanded)
            {
                // Increase indent
                EditorGUI.indentLevel++;

                for (int i = 0; i < array.arraySize; i++)
                {
                    // Get the element and create its label
                    SerializedProperty element = array.GetArrayElementAtIndex(i);
                    // Get the correct label for the element
                    GUIContent elementLabel = new GUIContent("Element " + i);
                    if (elementLabelGetter != null) elementLabel = elementLabelGetter.Invoke(i);

                    // Edit the element
                    position.height = EditorGUI.GetPropertyHeight(element);
                    EditorGUI.PropertyField(position, element, elementLabel, true);
                    position.y += position.height;
                }

                EditorGUI.indentLevel--;
            }
        }
        else
        {
            position = EditorGUI.PrefixLabel(position, new GUIContent(array.displayName));
            EditorGUI.LabelField(position, new GUIContent("(no elements to edit)"));
        }
    }

    public static float GetSizeControlledArrayHeight(SerializedProperty array)
    {
        // Set height for one control
        float height = EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

        // If array is expanded then add height for each element individually
        if(array.isExpanded)
        {
            for (int i = 0; i < array.arraySize; i++)
            {
                height += EditorGUI.GetPropertyHeight(array.GetArrayElementAtIndex(i), true);
            }
        }

        return height;
    }
}
