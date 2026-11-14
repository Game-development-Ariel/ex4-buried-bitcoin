using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// The shop at the bottom left: one button for each machine. The picked machine is marked,
/// and the next click on an empty slot buys it.
/// </summary>
public class ShopBar : MonoBehaviour
{
    private const int CanvasSortingOrder = 10;
    private const float ButtonWidth = 400f;

    private static readonly Vector2 BottomLeft = new Vector2(0f, 0f);
    private static readonly Color PickedColor = new Color(1f, 0.8f, 0.45f);

    [SerializeField] private MachineShop shop;
    [SerializeField] private MachineData[] machines;

    private Image[] buttonImages;

    private void Awake()
    {
        UiFactory.EnsureEventSystem();
        Canvas canvas = UiFactory.CreateCanvas("Shop Canvas", CanvasSortingOrder);
        canvas.transform.SetParent(transform, false);

        Text help = UiFactory.CreateText(canvas.transform, "Help", BottomLeft, new Vector2(900f, 36f), 24, TextAnchor.MiddleLeft);
        help.rectTransform.anchoredPosition = new Vector2(40f, 150f);
        help.color = UiTheme.Muted;
        help.text = "SHOP: pick a machine, then click an empty slot above the belt.";

        buttonImages = new Image[machines.Length];
        for (int i = 0; i < machines.Length; i++)
        {
            MachineData machine = machines[i]; // A copy for the click handler below.
            string label = $"{machine.displayName}  ${machine.price}";
            Button button = UiFactory.CreateButton(canvas.transform, label, BottomLeft, new Vector2(ButtonWidth, 70f), 34, Color.white);
            RectTransform rect = (RectTransform)button.transform;
            rect.anchoredPosition = new Vector2(40f + ButtonWidth / 2f + i * (ButtonWidth + 20f), 100f);
            button.onClick.AddListener(() => shop.Select(machine));
            buttonImages[i] = button.GetComponent<Image>();

            Text description = UiFactory.CreateText(rect, "Description", new Vector2(0.5f, 0f), new Vector2(ButtonWidth, 30f), 20, TextAnchor.MiddleCenter);
            description.rectTransform.pivot = new Vector2(0.5f, 1f);
            description.rectTransform.anchoredPosition = new Vector2(0f, -6f);
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
