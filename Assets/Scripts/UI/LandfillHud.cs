using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// The top of the landfill screen: money, time left, which truck is dumping, the target card,
/// and a line for short messages. It listens to the game and only shows things.
/// </summary>
public class LandfillHud : MonoBehaviour
{
    private const int CanvasSortingOrder = 10;
    private const float MessageSeconds = 3.5f;

    private static readonly Vector2 TopLeft = new Vector2(0f, 1f);
    private static readonly Vector2 TopCenter = new Vector2(0.5f, 1f);
    private static readonly Vector2 TopRight = new Vector2(1f, 1f);
    private static readonly Vector2 BottomCenter = new Vector2(0.5f, 0f);

    [SerializeField] private LandfillDaySettings settings;
    [SerializeField] private Wallet wallet;
    [SerializeField] private DayTimer timer;
    [SerializeField] private TruckDumper dumper;
    [SerializeField] private MachineShop shop;

    [Header("Listens to")]
    [SerializeField] private GameEvent wrongPick;
    [SerializeField] private GameEvent tableFull;

    private Text moneyText;
    private Text timeText;
    private Text truckText;
    private Text messageText;
    private Coroutine messageRoutine;

    private void Awake()
    {
        Canvas canvas = UiFactory.CreateCanvas("HUD Canvas", CanvasSortingOrder);
        canvas.transform.SetParent(transform, false);

        moneyText = UiFactory.CreateTitle(canvas.transform, "Money", TopLeft, new Vector2(300f, 70f), 56, UiTheme.Accent);
        moneyText.rectTransform.anchoredPosition = new Vector2(40f, -20f);
        moneyText.alignment = TextAnchor.MiddleLeft;

        timeText = UiFactory.CreateTitle(canvas.transform, "Time", TopCenter, new Vector2(300f, 70f), 56, UiTheme.TextColor);
        timeText.rectTransform.anchoredPosition = new Vector2(0f, -20f);

        truckText = UiFactory.CreateText(canvas.transform, "Truck", TopCenter, new Vector2(500f, 40f), 26, TextAnchor.MiddleCenter);
        truckText.rectTransform.anchoredPosition = new Vector2(0f, -85f);
        truckText.color = UiTheme.Muted;
        truckText.text = "The first truck is on its way...";

        messageText = UiFactory.CreateText(canvas.transform, "Message", BottomCenter, new Vector2(1300f, 50f), 30, TextAnchor.MiddleCenter);
        messageText.rectTransform.anchoredPosition = new Vector2(0f, 200f);
        messageText.color = UiTheme.Accent;
        messageText.text = "";

        CreateTargetCard(canvas.transform);
    }

    private void OnEnable()
    {
        wallet.Changed += ShowMoney;
        dumper.TruckStarted += ShowTruck;
        shop.Refused += ShowMessage;
        wrongPick.Subscribe(ShowWrongPick);
        tableFull.Subscribe(ShowTableFull);
    }

    private void OnDisable()
    {
        wallet.Changed -= ShowMoney;
        dumper.TruckStarted -= ShowTruck;
        shop.Refused -= ShowMessage;
        wrongPick.Unsubscribe(ShowWrongPick);
        tableFull.Unsubscribe(ShowTableFull);
    }

    private void Start()
    {
        ShowMoney(wallet.Money);
    }

    private void Update()
    {
        int seconds = Mathf.CeilToInt(timer.SecondsLeft);
        timeText.text = $"{seconds / 60}:{seconds % 60:00}";
        timeText.color = seconds <= settings.warningSeconds ? UiTheme.Danger : UiTheme.TextColor;
    }

    private void CreateTargetCard(Transform canvas)
    {
        Image card = UiFactory.CreatePanel(canvas, "Target Card", "Panel");
        UiFactory.Place(card.rectTransform, TopRight, new Vector2(-30f, -20f), new Vector2(480f, 120f));

        Text label = UiFactory.CreateText(card.transform, "Label", TopLeft, new Vector2(440f, 30f), 22, TextAnchor.MiddleLeft);
        label.rectTransform.anchoredPosition = new Vector2(24f, -14f);
        label.color = UiTheme.Muted;
        label.text = "TODAY'S TARGET";

        Text description = UiFactory.CreateText(card.transform, "Description", TopLeft, new Vector2(440f, 60f), 26, TextAnchor.UpperLeft);
        description.rectTransform.anchoredPosition = new Vector2(24f, -48f);
        description.text = settings.target.description;
    }

    private void ShowMoney(int money)
    {
        moneyText.text = "$" + money;
    }

    private void ShowTruck(int number)
    {
        truckText.text = $"Truck {number} of {dumper.TruckCount} is dumping";
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
