using UnityEngine;

/// <summary>
/// The rules of the day: finding the target wins, and running out of time or burying the target loses.
/// It only raises events about the result. The end panel listens to them.
/// </summary>
public class LandfillRound : MonoBehaviour
{
    [SerializeField] private LandfillDaySettings settings;
    [SerializeField] private DayTimer timer;

    [Tooltip("Turned off when the day ends: the belt, the truck, the timer and the clicks. Items already moving settle by themselves")]
    [SerializeField] private Behaviour[] stopWhenDayEnds;

    [Header("Listens to")]
    [SerializeField] private GameEvent targetFound;
    [SerializeField] private GameEvent targetLost;
    [SerializeField] private GameEvent wrongPick;

    [Header("Raises")]
    [SerializeField] private GameEvent dayWon;
    [SerializeField] private TextEvent dayLost;

    private bool ended;

    private void OnEnable()
    {
        targetFound.Subscribe(Win);
        targetLost.Subscribe(LoseTarget);
        wrongPick.Subscribe(PunishWrongPick);
        timer.RanOut += LoseTime;
    }

    private void OnDisable()
    {
        targetFound.Unsubscribe(Win);
        targetLost.Unsubscribe(LoseTarget);
        wrongPick.Unsubscribe(PunishWrongPick);
        timer.RanOut -= LoseTime;
    }

    private void Win()
    {
        if (End())
        {
            dayWon.Raise();
        }
    }

    private void LoseTarget()
    {
        if (End())
        {
            dayLost.Raise("The target went to the waste pile. It is buried again.");
        }
    }

    private void LoseTime()
    {
        if (End())
        {
            dayLost.Raise("The work day is over, and the target is still somewhere in the trash.");
        }
    }

    private void PunishWrongPick()
    {
        timer.Subtract(settings.wrongPickPenaltySeconds);
    }

    /// <summary>Marks the day as ended. Returns false if it had already ended, so only the first result counts.</summary>
    private bool End()
    {
        if (ended)
        {
            return false;
        }
        ended = true;
        foreach (Behaviour part in stopWhenDayEnds)
        {
            part.enabled = false;
        }
        return true;
    }
}
