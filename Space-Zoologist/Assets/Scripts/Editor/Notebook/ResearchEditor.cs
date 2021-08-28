using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(ResearchModel))]
public class ResearchEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditResearchEntryList(serializedObject.FindProperty("speciesResearch"), "Species Research");
        EditResearchEntryList(serializedObject.FindProperty("foodResearch"), "Food Research");
        EditResearchEntryList(serializedObject.FindProperty("tileResearch"), "Tile Research");

        serializedObject.ApplyModifiedProperties();
    }

    private void EditResearchEntryList(SerializedProperty list, string label)
    {
        list.isExpanded = EditorGUILayout.Foldout(list.isExpanded, new GUIContent(label));

        if(list.isExpanded)
        {
            // Increase indent
            EditorGUI.indentLevel++;

            EditorGUILayout.PropertyField(list.FindPropertyRelative("Array.size"));

            for(int i = 0; i < list.arraySize; i++)
            {
                SerializedProperty listElement = list.GetArrayElementAtIndex(i);
                // Get the category name, and make sure it is non-empty
                string categoryName = listElement.FindPropertyRelative("category.name").stringValue;
                if (categoryName == "") categoryName = "Element " + i;
                // Set out the list element 
                EditorGUILayout.PropertyField(listElement, new GUIContent(categoryName));
            }

            // Restore old indent
            EditorGUI.indentLevel--;
        }
    }
}
