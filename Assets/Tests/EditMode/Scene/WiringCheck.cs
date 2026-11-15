using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Finds the problems a broken scene or prefab would only show while playing:
/// missing scripts, and object fields of our components that were left empty.
/// </summary>
public static class WiringCheck
{
    /// <summary>The assembly of the game. Only its components are checked for empty fields.</summary>
    public static readonly System.Reflection.Assembly Game = typeof(LandfillRound).Assembly;

    public static List<string> MissingScripts(IEnumerable<GameObject> roots)
    {
        var problems = new List<string>();
        foreach (GameObject root in roots)
        {
            foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
            {
                if (GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(child.gameObject) > 0)
                {
                    problems.Add(Path(child));
                }
            }
        }
        return problems;
    }

    /// <summary>Every empty object reference (also inside arrays) on the game's components.</summary>
    public static List<string> EmptyReferences(IEnumerable<GameObject> roots, ICollection<string> allowed)
    {
        var problems = new List<string>();
        foreach (GameObject root in roots)
        {
            foreach (MonoBehaviour component in root.GetComponentsInChildren<MonoBehaviour>(true))
            {
                if (component == null || component.GetType().Assembly != Game)
                {
                    continue;
                }
                var fields = new SerializedObject(component);
                SerializedProperty property = fields.GetIterator();
                while (property.NextVisible(true))
                {
                    if (property.propertyType != SerializedPropertyType.ObjectReference || property.name == "m_Script")
                    {
                        continue;
                    }
                    string key = component.GetType().Name + "." + property.propertyPath;
                    if (property.objectReferenceValue == null && !allowed.Contains(key))
                    {
                        problems.Add($"{Path(component.transform)}: {key}");
                    }
                }
            }
        }
        return problems;
    }

    private static string Path(Transform transform)
    {
        return transform.parent == null ? transform.name : Path(transform.parent) + "/" + transform.name;
    }
}
