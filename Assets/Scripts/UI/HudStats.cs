using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Shows the numbers at the top of the screen: the money, the time left and which truck is dumping.
/// </summary>
public class HudStats : MonoBehaviour
{
    private const int CanvasSortingOrder = 10;
    private const int BigFontSize = 56;
    private const int TruckFontSize = 26;

    private static readonly Vector2 TopLeft = new Vector2(0f, 1f);
    private static readonly Vector2 TopCenter = new Vector2(0.5f, 1f);
    private static readonly Vector2 BigTextSize = new Vector2(300f, 70f);
    private static readonly Vector2 MoneyPosition = new Vector2(40f, -20f);
    private static readonly Vector2 TimePosition = new Vector2(0f, -20f);
    private static readonly Vector2 TruckSize = new Vector2(500f, 40f);
    private static readonly Vector2 TruckPosition = new Vector2(0f, -85f);

    [SerializeField] private LandfillDaySettings settings;
    [SerializeField] private Wallet wallet;
    [SerializeField] private DayTimer timer;
    [SerializeField] private TruckDumper dumper;

    private Text moneyText;
    private Text timeText;
    private Text truckText;

    private void Awake()
    {
        Canvas canvas = UiFactory.CreateCanvas("Stats Canvas", CanvasSortingOrder);
        canvas.transform.SetParent(transform, false);

        moneyText = UiFactory.CreateTitle(canvas.transform, "Money", TopLeft, BigTextSize, BigFontSize, UiTheme.Accent);
        moneyText.rectTransform.anchoredPosition = MoneyPosition;
        moneyText.alignment = TextAnchor.MiddleLeft;

        timeText = UiFactory.CreateTitle(canvas.transform, "Time", TopCenter, BigTextSize, BigFontSize, UiTheme.TextColor);
        timeText.rectTransform.anchoredPosition = TimePosition;

        truckText = UiFactory.CreateText(canvas.transform, "Truck", TopCenter, TruckSize, TruckFontSize, TextAnchor.MiddleCenter);
        truckText.rectTransform.anchoredPosition = TruckPosition;
        truckText.color = UiTheme.Muted;
        truckText.text = "The first truck is on its way...";
    }

    private void OnEnable()
    {
        wallet.Changed += ShowMoney;
        dumper.TruckStarted += ShowTruck;
        dumper.AllTrucksDone += ShowNoMoreTrucks;
    }

    private void OnDisable()
    {
        wallet.Changed -= ShowMoney;
        dumper.TruckStarted -= ShowTruck;
        dumper.AllTrucksDone -= ShowNoMoreTrucks;
    }

    private void Start()
    {
        ShowMoney(wallet.Money);
    }

    // The time is read every frame and not sent as an event: it changes all the time anyway.
    private void Update()
    {
        int seconds = Mathf.CeilToInt(timer.SecondsLeft);
        timeText.text = $"{seconds / 60}:{seconds % 60:00}";
        timeText.color = seconds <= settings.warningSeconds ? UiTheme.Danger : UiTheme.TextColor;
    }

    private void ShowMoney(int money)
    {
        moneyText.text = "$" + money;
    }

    private void ShowTruck(int number)
    {
        truckText.text = $"Truck {number} of {dumper.TruckCount} is dumping";
    }

    private void ShowNoMoreTrucks()
    {
        truckText.text = "No more trucks today";
    }
}
