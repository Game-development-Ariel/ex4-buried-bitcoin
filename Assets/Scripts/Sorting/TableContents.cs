using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Keeps the list of the items lying on the sorting table, and tells the line when the table is full.
/// Needs a trigger collider over the table.
/// </summary>
public class TableContents : MonoBehaviour
{
    [SerializeField] private LandfillDaySettings settings;

    [Header("Raises")]
    [SerializeField] private GameEvent tableFull;
    [SerializeField] private GameEvent tableHasRoom;

    private readonly List<TrashItem> items = new List<TrashItem>();
    private bool isFull;

    public int ItemCount => items.Count;
    public int Capacity => settings.tableCapacity;

    /// <summary>Happens when the number of items on the table changes.</summary>
    public event Action Changed;

    /// <summary>Happens when the player clicks an item that is on the table.</summary>
    public event Action<TrashItem> ItemClicked;

    /// <summary>Happens when an item leaves the table: it was sorted, or it bounced off.</summary>
    public event Action<TrashItem> ItemRemoved;

    private void OnTriggerEnter2D(Collider2D other)
    {
        TrashItem item = other.GetComponent<TrashItem>();
        if (item == null || items.Contains(item))
        {
            return;
        }

        // The table listens to the item's clicks only while the item is on it.
        items.Add(item);
        item.Clicked += OnItemClicked;
        if (!isFull && items.Count >= settings.tableCapacity)
        {
            isFull = true;
            tableFull.Raise();
        }
        Changed?.Invoke();
    }

    // Also runs when an item on the table is destroyed some other way: Physics 2D sends OnTriggerExit2D
    // for a collider that is destroyed or turned off ("Callbacks On Disable" in the Physics 2D settings).
    private void OnTriggerExit2D(Collider2D other)
    {
        TrashItem item = other.GetComponent<TrashItem>();
        if (item != null && items.Contains(item))
        {
            Remove(item); // It bounced off the table.
        }
    }

    // OnDestroy and not OnDisable: Unity sends trigger messages to a turned-off script too,
    // so the list has to keep working while the script is off.
    private void OnDestroy()
    {
        foreach (TrashItem item in items)
        {
            if (item != null)
            {
                item.Clicked -= OnItemClicked;
            }
        }
        items.Clear();
    }

    /// <summary>Takes the item off the table and destroys it. Called when the player sorts it.</summary>
    public void Discard(TrashItem item)
    {
        Remove(item);
        ObjectRemoval.Remove(item.gameObject);
    }

    private void Remove(TrashItem item)
    {
        items.Remove(item);
        item.Clicked -= OnItemClicked;

        // The line starts again only when a few places are free, so an item that bounces
        // at the edge of the table doesn't stop and start the belt again and again.
        if (isFull && items.Count <= settings.tableCapacity - settings.freePlacesToRestart)
        {
            isFull = false;
            tableHasRoom.Raise();
        }
        ItemRemoved?.Invoke(item);
        Changed?.Invoke();
    }

    private void OnItemClicked(TrashItem item)
    {
        ItemClicked?.Invoke(item);
    }
}
