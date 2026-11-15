using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// The three choices for the item selected on the sorting table: waste, sell, or "this is the target".
/// The buttons work only while an item is selected.
/// </summary>
public class TablePanel : MonoBehaviour
{
    private const int StatusFontSize = 24;
    private const int ButtonFontSize = 32;
    private const float ButtonHeight = 70f;
    private const float ButtonY = 100f;

    // How see-through the buttons are while nothing is selected.
    private const float DimmedAlpha = 0.35f;

    // The buttons are placed from the right edge of the screen: x of the center, and width.
    private const float WasteX = -905f;
    private const float WasteWidth = 190f;
    private const float SellX = -705f;
    private const float SellWidth = 190f;
    private const float TargetX = -410f;
    private const float TargetWidth = 380f;

    private static readonly Vector2 BottomRight = new Vector2(1f, 0f);
    private static readonly Vector2 StatusSize = new Vector2(760f, 36f);
    private static readonly Vector2 StatusPosition = new Vector2(-200f, 150f);

    [SerializeField] private TableContents table;
    [SerializeField] private SortingDecisions decisions;

    private Text status;
    private Text sellLabel;
    private CanvasGroup buttons;

    private void Awake()
    {
        UiFactory.EnsureEventSystem();
        Canvas canvas = UiFactory.CreateCanvas("Table Canvas", UiTheme.HudLayer);
        canvas.transform.SetParent(transform, false);

        status = UiFactory.CreateText(canvas.transform, "Status", BottomRight, StatusSize, StatusFontSize, TextAnchor.MiddleRight);
        status.rectTransform.anchoredPosition = StatusPosition;
        status.color = UiTheme.Muted;

        // The buttons share one CanvasGroup, so they are dimmed and turned off together.
        var group = new GameObject("Buttons", typeof(RectTransform), typeof(CanvasGroup));
        group.transform.SetParent(canvas.transform, false);
        UiFactory.Stretch((RectTransform)group.transform);
        buttons = group.GetComponent<CanvasGroup>();

        AddButton("Waste", WasteX, WasteWidth).onClick.AddListener(decisions.ThrowSelectedToWaste);
        Button sell = AddButton("Sell", SellX, SellWidth);
        sell.onClick.AddListener(decisions.SellSelected);
        sellLabel = sell.GetComponentInChildren<Text>();
        AddButton("This is the target!", TargetX, TargetWidth).onClick.AddListener(decisions.MarkSelectedAsTarget);
    }

    private void OnEnable()
    {
        table.Changed += Refresh;
        decisions.SelectionChanged += Refresh;
    }

    private void OnDisable()
    {
        table.Changed -= Refresh;
        decisions.SelectionChanged -= Refresh;
    }

    private void Start()
    {
        Refresh();
    }

    private Button AddButton(string label, float x, float width)
    {
        Button button = UiFactory.CreateButton(buttons.transform, label, BottomRight, new Vector2(width, ButtonHeight), ButtonFontSize, Color.white);
        ((RectTransform)button.transform).anchoredPosition = new Vector2(x, ButtonY);
        return button;
    }

    private void Refresh()
    {
        TrashItem selected = decisions.Selected;
        buttons.interactable = selected != null;
        buttons.alpha = selected != null ? 1f : DimmedAlpha;

        string count = $"Table: {table.ItemCount} of {table.Capacity}";
        if (selected == null)
        {
            status.text = count + ". Click an item on the table.";
            sellLabel.text = "Sell";
        }
        else
        {
            status.text = $"{count}. Selected: {selected.Data.displayName}";
            sellLabel.text = "Sell $" + selected.Data.sellPrice;
        }
    }
}
