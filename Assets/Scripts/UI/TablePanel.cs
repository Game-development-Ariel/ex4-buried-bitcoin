using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// The three choices for the item selected on the sorting table: waste, sell, or "this is the target".
/// The buttons work only while an item is selected.
/// </summary>
public class TablePanel : MonoBehaviour
{
    private const int CanvasSortingOrder = 10;

    private static readonly Vector2 BottomRight = new Vector2(1f, 0f);

    [SerializeField] private SortingTable table;

    private Text status;
    private Text sellLabel;
    private CanvasGroup buttons;

    private void Awake()
    {
        UiFactory.EnsureEventSystem();
        Canvas canvas = UiFactory.CreateCanvas("Table Canvas", CanvasSortingOrder);
        canvas.transform.SetParent(transform, false);

        status = UiFactory.CreateText(canvas.transform, "Status", BottomRight, new Vector2(760f, 36f), 24, TextAnchor.MiddleRight);
        status.rectTransform.anchoredPosition = new Vector2(-200f, 150f);
        status.color = UiTheme.Muted;

        // The buttons share one CanvasGroup, so they are dimmed and turned off together.
        var group = new GameObject("Buttons", typeof(RectTransform), typeof(CanvasGroup));
        group.transform.SetParent(canvas.transform, false);
        UiFactory.Stretch((RectTransform)group.transform);
        buttons = group.GetComponent<CanvasGroup>();

        AddButton("Waste", -905f, 190f).onClick.AddListener(table.ThrowSelectedToWaste);
        Button sell = AddButton("Sell", -705f, 190f);
        sell.onClick.AddListener(table.SellSelected);
        sellLabel = sell.GetComponentInChildren<Text>();
        AddButton("This is the target!", -410f, 380f).onClick.AddListener(table.MarkSelectedAsTarget);
    }

    private void OnEnable()
    {
        table.Changed += Refresh;
    }

    private void OnDisable()
    {
        table.Changed -= Refresh;
    }

    private void Start()
    {
        Refresh();
    }

    private Button AddButton(string label, float x, float width)
    {
        Button button = UiFactory.CreateButton(buttons.transform, label, BottomRight, new Vector2(width, 70f), 32, Color.white);
        ((RectTransform)button.transform).anchoredPosition = new Vector2(x, 100f);
        return button;
    }

    private void Refresh()
    {
        TrashItem selected = table.Selected;
        buttons.interactable = selected != null;
        buttons.alpha = selected != null ? 1f : 0.35f;

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
