using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// The shop at the bottom left: one button for each machine. The picked machine is marked,
/// and the next click on an empty slot buys it.
/// </summary>
public class ShopBar : MonoBehaviour
{
    private const int HelpFontSize = 24;
    private const int ButtonFontSize = 34;
    private const int DescriptionFontSize = 20;
    private const float ButtonWidth = 400f;
    private const float ButtonHeight = 70f;
    private const float ButtonGap = 20f;
    private const float LeftMargin = 40f;
    private const float ButtonY = 100f;

    private static readonly Vector2 BottomLeft = new Vector2(0f, 0f);
    private static readonly Vector2 BottomCenter = new Vector2(0.5f, 0f);
    private static readonly Vector2 TopCenter = new Vector2(0.5f, 1f);
    private static readonly Vector2 HelpSize = new Vector2(900f, 36f);
    private static readonly Vector2 HelpPosition = new Vector2(LeftMargin, 150f);
    private static readonly Vector2 DescriptionSize = new Vector2(ButtonWidth, 30f);
    private static readonly Vector2 DescriptionOffset = new Vector2(0f, -6f);
    private static readonly Color PickedColor = new Color(1f, 0.8f, 0.45f);

    [SerializeField] private MachineShop shop;
    [SerializeField] private MachineData[] machines;

    private Image[] buttonImages;

    private void Awake()
    {
        UiFactory.EnsureEventSystem();
        Canvas canvas = UiFactory.CreateCanvas("Shop Canvas", UiTheme.HudLayer);
        canvas.transform.SetParent(transform, false);

        Text help = UiFactory.CreateText(canvas.transform, "Help", BottomLeft, HelpSize, HelpFontSize, TextAnchor.MiddleLeft);
        help.rectTransform.anchoredPosition = HelpPosition;
        help.color = UiTheme.Muted;
        help.text = "SHOP: pick a machine, then click an empty slot above the belt.";

        buttonImages = new Image[machines.Length];
        for (int i = 0; i < machines.Length; i++)
        {
            MachineData machine = machines[i]; // A copy for the click handler below.
            string label = $"{machine.displayName}  ${machine.price}";
            Button button = UiFactory.CreateButton(canvas.transform, label, BottomLeft, new Vector2(ButtonWidth, ButtonHeight), ButtonFontSize, Color.white);
            RectTransform rect = (RectTransform)button.transform;
            rect.anchoredPosition = new Vector2(LeftMargin + ButtonWidth / 2f + i * (ButtonWidth + ButtonGap), ButtonY);
            button.onClick.AddListener(() => shop.Select(machine));
            buttonImages[i] = button.GetComponent<Image>();

            Text description = UiFactory.CreateText(rect, "Description", BottomCenter, DescriptionSize, DescriptionFontSize, TextAnchor.MiddleCenter);
            description.rectTransform.pivot = TopCenter;
            description.rectTransform.anchoredPosition = DescriptionOffset;
            description.color = UiTheme.Muted;
            description.text = machine.description;
        }
    }

    private void OnEnable()
    {
        shop.SelectionChanged += MarkPicked;
    }

    private void OnDisable()
    {
        shop.SelectionChanged -= MarkPicked;
    }

    private void MarkPicked()
    {
        for (int i = 0; i < machines.Length; i++)
        {
            buttonImages[i].color = machines[i] == shop.Selected ? PickedColor : Color.white;
        }
    }
}
