using System;
using UnityEngine;

/// <summary>
/// One item on the sorting line. It takes its look and weight from a TrashItemData.
/// </summary>
[RequireComponent(typeof(Rigidbody2D), typeof(SpriteRenderer))]
public class TrashItem : MonoBehaviour, IClickable
{
    [Tooltip("A child that is shown around the item when it is selected")]
    [SerializeField] private GameObject outline;

    public TrashItemData Data { get; private set; }

    /// <summary>Happens when the player clicks the item. The sorting table listens while the item is on it.</summary>
    public event Action<TrashItem> Clicked;

    public void Setup(TrashItemData data)
    {
        Data = data;
        name = data.displayName;
        transform.localScale = Vector3.one * data.size;
        GetComponent<SpriteRenderer>().color = data.color;
        GetComponent<Rigidbody2D>().mass = data.mass;
        SetHighlighted(false);
    }

    public void OnClicked()
    {
        Clicked?.Invoke(this);
    }

    public void SetHighlighted(bool highlighted)
    {
        outline.SetActive(highlighted);
    }
}
