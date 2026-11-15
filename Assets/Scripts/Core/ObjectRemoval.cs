using UnityEngine;

/// <summary>
/// Removes a game object the right way for the moment it runs in.
/// While the game plays it calls Destroy, which removes the object at the end of the frame, as usual.
/// Outside Play mode (the scene builder and the EditMode tests) Destroy is not allowed: it only logs an error
/// and leaves the object alive. There the object is removed at once instead.
/// It is a static class with no state, so it is safe to call from anywhere.
/// </summary>
public static class ObjectRemoval
{
    public static void Remove(Object target)
    {
        if (Application.isPlaying)
        {
            Object.Destroy(target);
        }
        else
        {
            Object.DestroyImmediate(target);
        }
    }
}
