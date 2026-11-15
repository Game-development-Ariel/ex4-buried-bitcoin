using UnityEngine;

/// <summary>
/// Shows the result of the day when the round raises "day won" or "day lost".
/// </summary>
public class LandfillEndPanel : MonoBehaviour
{
    [SerializeField] private GameEvent dayWon;
    [SerializeField] private TextEvent dayLost;

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

    private void ShowLose(string reason)
    {
        ResultOverlay.Show("Day failed", reason, UiTheme.Danger);
    }
}
