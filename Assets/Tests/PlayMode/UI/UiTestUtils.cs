using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>Finds the texts and buttons the UI components create in code, by the names they give them.</summary>
public static class UiTestUtils
{
    public static Text Text(Component root, string name)
    {
        Text[] found = root.GetComponentsInChildren<Text>(true).Where(text => text.name == name).ToArray();
        Assert.AreEqual(1, found.Length, $"One text called {name} under {root.name}");
        return found[0];
    }

    public static Button Button(Component root, string name)
    {
        return root.GetComponentsInChildren<Button>(true).Single(button => button.name == name);
    }

    /// <summary>The end-of-day screen, or null if it isn't shown.</summary>
    public static Canvas ResultScreen()
    {
        GameObject canvas = GameObject.Find("Result Canvas");
        return canvas == null ? null : canvas.GetComponent<Canvas>();
    }

    /// <summary>Destroys the objects that the UI code creates at the root of the scene.</summary>
    public static void DestroyRootUi()
    {
        foreach (Canvas canvas in Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None).Where(canvas => canvas.transform.parent == null))
        {
            Object.DestroyImmediate(canvas.gameObject);
        }
        foreach (EventSystem system in Object.FindObjectsByType<EventSystem>(FindObjectsSortMode.None))
        {
            Object.DestroyImmediate(system.gameObject);
        }
        GameObject sound = GameObject.Find("Sound Effects");
        if (sound != null)
        {
            Object.DestroyImmediate(sound);
        }
    }
}
