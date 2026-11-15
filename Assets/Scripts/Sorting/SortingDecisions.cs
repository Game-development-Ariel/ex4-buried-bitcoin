using System;
using UnityEngine;

/// <summary>
/// Carries out the player's decision about the item selected on the table: waste, sell, or "this is the target".
/// </summary>
public class SortingDecisions : MonoBehaviour
{
    [SerializeField] private LandfillDaySettings settings;
    [SerializeField] private TableContents table;
    [SerializeField] private Wallet wallet;

    [Header("Raises")]
    [SerializeField] private GameEvent targetFound;
    [SerializeField] private GameEvent targetLost;
    [SerializeField] private GameEvent wrongPick;

    /// <summary>The item the buttons act on. Null if nothing is selected.</summary>
    public TrashItem Selected { get; private set; }

    public event Action SelectionChanged;

    private void OnEnable()
    {
        table.ItemClicked += Select;
        table.ItemRemoved += OnItemRemoved;
    }

    private void OnDisable()
    {
        table.ItemClicked -= Select;
        table.ItemRemoved -= OnItemRemoved;
    }

    public void ThrowSelectedToWaste()
    {
        if (Selected == null)
        {
            return;
        }
        if (Selected.Data == settings.target)
        {
            targetLost.Raise();
        }
        DiscardSelected();
    }

    public void SellSelected()
    {
        if (Selected == null)
        {
            return;
        }
        if (Selected.Data == settings.target)
        {
            targetLost.Raise(); // Sold for scrap by mistake.
        }
        wallet.Add(Selected.Data.sellPrice);
        DiscardSelected();
    }

    public void MarkSelectedAsTarget()
    {
        if (Selected == null)
        {
            return;
        }
        if (Selected.Data == settings.target)
        {
            targetFound.Raise();
        }
        else
        {
            // The wrong item is unselected, so a double click can't give the penalty twice.
            Select(null);
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
        if (Selected != null)
        {
            Selected.SetHighlighted(true);
        }
        SelectionChanged?.Invoke();
    }

    private void OnItemRemoved(TrashItem item)
    {
        if (item == Selected)
        {
            Select(null);
        }
    }

    private void DiscardSelected()
    {
        TrashItem item = Selected;
        Select(null);
        table.Discard(item);
    }
}
