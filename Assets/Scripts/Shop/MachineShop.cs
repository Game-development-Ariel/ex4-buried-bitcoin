using System;
using UnityEngine;

/// <summary>
/// Turns clicks on the machine slots into trades: an empty slot buys the machine picked in the shop,
/// and a slot with a machine sells it back for part of the price.
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

    /// <summary>Why a click on a slot did nothing.</summary>
    public enum Refusal
    {
        NothingPicked,
        NotEnoughMoney,
    }

    /// <summary>Happens when a click can't be done. Gives the reason, and the UI chooses the words.</summary>
    public event Action<Refusal> Refused;

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
            Refused?.Invoke(Refusal.NothingPicked);
        }
        else if (!wallet.TrySpend(Selected.price))
        {
            Refused?.Invoke(Refusal.NotEnoughMoney);
        }
        else
        {
            slot.Install(Selected);
            Select(null);
        }
    }
}
