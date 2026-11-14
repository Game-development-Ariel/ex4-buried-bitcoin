using System;
using UnityEngine;

/// <summary>
/// Buying and selling machines: the player picks a machine, then clicks an empty slot to install it.
/// Clicking a slot that has a machine sells it back for half the price.
/// </summary>
public class MachineShop : MonoBehaviour
{
    [SerializeField] private Wallet wallet;
    [SerializeField] private MachineSlot[] slots;

    [Tooltip("The part of the price that comes back when a machine is sold. 0.5 = half")]
    [SerializeField] private float refundShare = 0.5f;

    /// <summary>The machine that the next click on an empty slot will buy. Null if nothing is picked.</summary>
    public MachineData Selected { get; private set; }

    public event Action SelectionChanged;

    /// <summary>Happens when a click can't be done. Gives a short reason to show the player.</summary>
    public event Action<string> Refused;

    private void OnEnable()
    {
        foreach (MachineSlot slot in slots)
        {
            slot.Clicked += OnSlotClicked;
        }
    }

    private void OnDisable()
    {
        foreach (MachineSlot slot in slots)
        {
            slot.Clicked -= OnSlotClicked;
        }
    }

    public void Select(MachineData machine)
    {
        Selected = machine;
        SelectionChanged?.Invoke();
    }

    private void OnSlotClicked(MachineSlot slot)
    {
        if (!slot.IsEmpty)
        {
            wallet.Add(Mathf.RoundToInt(slot.Machine.price * refundShare));
            slot.Clear();
        }
        else if (Selected == null)
        {
            Refused?.Invoke("Pick a machine in the shop first.");
        }
        else if (!wallet.TrySpend(Selected.price))
        {
            Refused?.Invoke($"Not enough money for the {Selected.displayName}. Sell something first.");
        }
        else
        {
            slot.Install(Selected);
            Select(null);
        }
    }
}
