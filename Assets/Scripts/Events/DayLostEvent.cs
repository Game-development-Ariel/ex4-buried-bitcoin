using System;
using UnityEngine;

/// <summary>
/// Like GameEvent, but it also carries why the day was lost. The UI turns the reason into words.
/// </summary>
[CreateAssetMenu(menuName = "Events/Day Lost Event")]
public class DayLostEvent : ScriptableObject
{
    private event Action<LossReason> Raised;

    public void Raise(LossReason reason)
    {
        Raised?.Invoke(reason);
    }

    public void Subscribe(Action<LossReason> listener)
    {
        Raised += listener;
    }

    public void Unsubscribe(Action<LossReason> listener)
    {
        Raised -= listener;
    }
}
