using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// The table at the end of the belt. Items pile up on it, the player selects one with a click
/// and decides: waste, sell, or "this is the target". Needs a trigger collider over the table.
/// </summary>
public class SortingTable : MonoBehaviour
{
    [SerializeField] private LandfillDaySettings settings;
    [SerializeField] private Wallet wallet;

    [Header("Raises")]
    [SerializeField] private GameEvent tableFull;
    [SerializeField] private GameEvent tableHasRoom;
    [SerializeField] private GameEvent targetFound;
    [SerializeField] private GameEvent targetLost;
    [SerializeField] private GameEvent wrongPick;

    private readonly List<TrashItem> items = new List<TrashItem>();

    public TrashItem Selected { get; private set; }
    public int ItemCount => items.Count;
    public int Capacity => settings.tableCapacity;

    /// <summary>Happens when the selected item or the number of items changes.</summary>
    public event Action Changed;

    private void OnTriggerEnter2D(Collider2D other)
    {
        TrashItem item = other.GetComponent<TrashItem>();
        if (item == null || items.Contains(item))
        {
            return;
        }

        items.Add(item);
        item.Clicked += Select;
        if (items.Count == settings.tableCapacity)
        {
            tableFull.Raise();
        }
        Changed?.Invoke();
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        TrashItem item = other.GetComponent<TrashItem>();
        if (item != null && items.Contains(item))
        {
            Remove(item); // It bounced off the table.
        }
    }

    public void ThrowSelectedToWaste()
    {
        if (Selected == null)
        {
            return;
        }
        if (Selected.Data.isTarget)
        {
            targetLost.Raise();
        }
        DestroySelected();
    }

    public void SellSelected()
    {
        if (Selected == null)
        {
            return;
        }
        if (Selected.Data.isTarget)
        {
            targetLost.Raise(); // Sold for scrap by mistake.
        }
        wallet.Add(Selected.Data.sellPrice);
        DestroySelected();
    }

    public void MarkSelectedAsTarget()
    {
        if (Selected == null)
        {
            return;
        }
        if (Selected.Data.isTarget)
        {
            targetFound.Raise();
        }
        else
        {
            wrongPick.Raise();
        }
    }

    private void Select(TrashItem item)
    {
        if (Selected != null)
        {
            Selected.SetHighlighted(false);
        }
        Selected = item;
        Selected.SetHighlighted(true);
        Changed?.Invoke();
    }

    private void DestroySelected()
    {
        TrashItem item = Selected;
        Remove(item);
        Destroy(item.gameObject);
    }

    /// <summary>Takes the item off the table's list and stops listening to its clicks.</summary>
    private void Remove(TrashItem item)
    {
        bool wasFull = items.Count == settings.tableCapacity;
        items.Remove(item);
        item.Clicked -= Select;
        if (item == Selected)
        {
            item.SetHighlighted(false);
            Selected = null;
        }
        if (wasFull)
        {
            tableHasRoom.Raise();
        }
        Changed?.Invoke();
    }
}
