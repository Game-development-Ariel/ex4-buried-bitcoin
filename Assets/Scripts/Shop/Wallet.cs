using System;
using UnityEngine;

/// <summary>
/// The player's money.
/// </summary>
public class Wallet : MonoBehaviour
{
    [SerializeField] private LandfillDaySettings settings;

    public int Money { get; private set; }

    /// <summary>Happens every time the money changes. Gives the new amount.</summary>
    public event Action<int> Changed;

    private void Awake()
    {
        Money = settings.startMoney;
    }

    public void Add(int amount)
    {
        Money += amount;
        Changed?.Invoke(Money);
    }

    /// <summary>Pays the price if there is enough money. Returns false, and takes nothing, if there isn't.</summary>
    public bool TrySpend(int price)
    {
        if (price > Money)
        {
            return false;
        }
        Money -= price;
        Changed?.Invoke(Money);
        return true;
    }
}
