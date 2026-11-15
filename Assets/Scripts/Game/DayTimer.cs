using System;
using UnityEngine;

/// <summary>
/// Counts down the time of the work day.
/// </summary>
public class DayTimer : MonoBehaviour
{
    [SerializeField] private LandfillDaySettings settings;

    public float SecondsLeft { get; private set; }

    /// <summary>Happens once, when the time reaches zero.</summary>
    public event Action RanOut;

    private void Awake()
    {
        SecondsLeft = settings.timeLimitSeconds;
    }

    private void Update()
    {
        Subtract(Time.deltaTime);
    }

    /// <summary>Takes seconds off the time left: the passing time, and the penalty for a wrong pick.</summary>
    public void Subtract(float seconds)
    {
        if (SecondsLeft <= 0f)
        {
            return;
        }

        SecondsLeft = Mathf.Max(0f, SecondsLeft - seconds);
        if (SecondsLeft == 0f)
        {
            RanOut?.Invoke();
        }
    }
}
