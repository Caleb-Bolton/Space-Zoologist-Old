﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomPropertyDrawer(typeof(ParallelItemRegistryAttribute))]
public class ParallelItemRegistryDrawer : EditArrayWrapperOnEnumDrawer
{
    #region Private Fields
    private ParallelArrayEditor<ItemData> innerArrayEditor = new ParallelArrayEditor<ItemData>()
    {
        arrayElementPropertyField = (r, s, g, e) =>
        {
            SerializedProperty id = s.FindPropertyRelative(nameof(id));

            if (id != null)
            {
                // Try to get the item id
                ItemID itemID = ItemRegistry.Exists(e);

                // Make sure the item id is valid
                if (itemID.IsValid)
                {
                    // Set the sub properties on this property
                    id.Next(true);
                    id.enumValueIndex = (int)itemID.Category;
                    id.Next(false);
                    id.intValue = itemID.Index;
                }
            }

            // Layout the property field for this array element
            EditorGUI.PropertyField(r, s, g, true);
        },
        arrayElementLabel = (s, e) => new GUIContent(e.Name.ToString())
    };
    #endregion

    #region Public Methods
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        editor.arrayElementPropertyField = (r, s, g, e) =>
        {
            // Get the array inside of the current array element
            SerializedProperty innerArray = InnerWrappedArray(s);
            innerArrayEditor.OnGUI(r, innerArray, g, ItemRegistry.GetItemsWithCategoryName(e));
        };
        editor.arrayElementPropertyHeight = (s, e) =>
        {
            // Get the array inside of the current array element
            SerializedProperty innerArray = InnerWrappedArray(s);
            return innerArrayEditor.GetPropertyHeight(innerArray, ItemRegistry.GetItemsWithCategoryName(e));
        };

        base.OnGUI(position, property, label);
    }
    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        SerializedProperty wrappedArray = WrappedArray(property);
        return editor.GetPropertyHeight(wrappedArray, System.Enum.GetNames(typeof(ItemRegistry.Category)));
    }
    public SerializedProperty InnerWrappedArray(SerializedProperty wrappedProperty)
    {
        ParallelItemRegistryAttribute att = attribute as ParallelItemRegistryAttribute;
        SerializedProperty innerWrappedArray = wrappedProperty.FindPropertyRelative(att.InnerWrappedPropertyPath);

        if (innerWrappedArray != null && innerWrappedArray.isArray) return innerWrappedArray;
        else if(innerWrappedArray == null)
        {
            Debug.LogError("ParallelItemRegistryDrawer: wrapper property at path '" +
                wrappedProperty.propertyPath + "' expected to find wrapped property at path '" +
                wrappedProperty.propertyPath + "." + att.InnerWrappedPropertyPath +
                "' but no such property could be found.  Make sure that the 'innerWrappedPropertyPath' field " +
                "on the attribute applied to this wrapper points to a property within the wrapper");
            throw new ExitGUIException();
        }
        else
        {
            Debug.LogError("ParallelItemRegistryDrawer: array wrapper at path '" +
                wrappedProperty.propertyPath + "' expected the relative property at path '" +
                innerWrappedArray.propertyPath + "' to be an array type, but instead it has the type '" +
                innerWrappedArray.propertyType + "'. Make sure that the relative property at path '" +
                innerWrappedArray.propertyType + "' is a type of array");
            throw new ExitGUIException();
        }
    }
    #endregion
}
