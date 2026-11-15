using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Shows a short message above the shop for a few seconds, when something needs the player's attention.
/// </summary>
public class HudMessageLine : MonoBehaviour
{
    private const int CanvasSortingOrder = 10;
    private const int FontSize = 30;
    private const float MessageSeconds = 3.5f;

    private static readonly Vector2 BottomCenter = new Vector2(0.5f, 0f);
    private static readonly Vector2 Size = new Vector2(1300f, 50f);
    private static readonly Vector2 Position = new Vector2(0f, 200f);

    [SerializeField] private LandfillDaySettings settings;
    [SerializeField] private MachineShop shop;

    [Header("Listens to")]
    [SerializeField] private GameEvent wrongPick;
    [SerializeField] private GameEvent tableFull;

    private Text messageText;
    private Coroutine messageRoutine;

    private void Awake()
    {
        Canvas canvas = UiFactory.CreateCanvas("Message Canvas", CanvasSortingOrder);
        canvas.transform.SetParent(transform, false);

        messageText = UiFactory.CreateText(canvas.transform, "Message", BottomCenter, Size, FontSize, TextAnchor.MiddleCenter);
        messageText.rectTransform.anchoredPosition = Position;
        messageText.color = UiTheme.Accent;
        messageText.text = "";
    }

    private void OnEnable()
    {
        shop.Refused += ShowMessage;
        wrongPick.Subscribe(ShowWrongPick);
        tableFull.Subscribe(ShowTableFull);
    }

    private void OnDisable()
    {
        shop.Refused -= ShowMessage;
        wrongPick.Unsubscribe(ShowWrongPick);
        tableFull.Unsubscribe(ShowTableFull);
    }

    private void ShowWrongPick()
    {
        ShowMessage($"That's not it. Compare with the target card. (-{settings.wrongPickPenaltySeconds:0} seconds)");
    }

    private void ShowTableFull()
    {
        ShowMessage("The table is full, so the belt stopped. Sort some items.");
    }

    private void ShowMessage(string message)
    {
        if (messageRoutine != null)
        {
            StopCoroutine(messageRoutine);
        }
        messageRoutine = StartCoroutine(MessageRoutine(message));
    }

    private IEnumerator MessageRoutine(string message)
    {
        messageText.text = message;
        yield return new WaitForSeconds(MessageSeconds);
        messageText.text = "";
    }
}
