using UnityEngine;

/// <summary>
/// Shows the result of the day when the round raises "day won" or "day lost".
/// </summary>
public class LandfillEndPanel : MonoBehaviour
{
    [SerializeField] private GameEvent dayWon;
    [SerializeField] private DayLostEvent dayLost;

    private void OnEnable()
    {
        dayWon.Subscribe(ShowWin);
        dayLost.Subscribe(ShowLose);
    }

    private void OnDisable()
    {
        dayWon.Unsubscribe(ShowWin);
        dayLost.Unsubscribe(ShowLose);
    }

    private void ShowWin()
    {
        ResultOverlay.Show("Found it!", "The target is back with its owner.\nThe city agrees: the sorting plant works.", UiTheme.Accent);
    }

    private void ShowLose(LossReason reason)
    {
        ResultOverlay.Show("Day failed", Describe(reason), UiTheme.Danger);
    }

    private static string Describe(LossReason reason)
    {
        switch (reason)
        {
            case LossReason.TargetBuried:
                return "The target went to the waste pile. It is buried again.";
            case LossReason.TimeUp:
                return "The work day is over, and the target is still somewhere in the trash.";
            default:
                return "The day is over.";
        }
    }
}
