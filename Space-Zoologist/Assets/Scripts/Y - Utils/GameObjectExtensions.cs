using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class GameObjectExtensions
{
    public static TComponent GetOrAddComponent<TComponent>(this GameObject gameObject) where TComponent : Component
    {
        TComponent component = gameObject.GetComponent<TComponent>();
        if (!component) component = gameObject.AddComponent<TComponent>();
        return component;
    }
}
