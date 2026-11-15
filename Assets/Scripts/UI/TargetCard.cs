using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Shows the card at the top right with the description of today's target.
/// </summary>
public class TargetCard : MonoBehaviour
{
    private const int LabelFontSize = 22;
    private const int DescriptionFontSize = 26;

    private static readonly Vector2 TopLeft = new Vector2(0f, 1f);
    private static readonly Vector2 TopRight = new Vector2(1f, 1f);
    private static readonly Vector2 CardOffset = new Vector2(-30f, -20f);
    private static readonly Vector2 CardSize = new Vector2(480f, 120f);
    private static readonly Vector2 LabelSize = new Vector2(440f, 30f);
    private static readonly Vector2 LabelPosition = new Vector2(24f, -14f);
    private static readonly Vector2 DescriptionSize = new Vector2(440f, 60f);
    private static readonly Vector2 DescriptionPosition = new Vector2(24f, -48f);

    [SerializeField] private LandfillDaySettings settings;

    private void Awake()
    {
        Canvas canvas = UiFactory.CreateCanvas("Target Canvas", UiTheme.HudLayer);
        canvas.transform.SetParent(transform, false);

        Image card = UiFactory.CreatePanel(canvas.transform, "Target Card", "Panel");
        UiFactory.Place(card.rectTransform, TopRight, CardOffset, CardSize);

        Text label = UiFactory.CreateText(card.transform, "Label", TopLeft, LabelSize, LabelFontSize, TextAnchor.MiddleLeft);
        label.rectTransform.anchoredPosition = LabelPosition;
        label.color = UiTheme.Muted;
        label.text = "TODAY'S TARGET";

        Text description = UiFactory.CreateText(card.transform, "Description", TopLeft, DescriptionSize, DescriptionFontSize, TextAnchor.UpperLeft);
        description.rectTransform.anchoredPosition = DescriptionPosition;
        description.text = settings.target.description;
    }
}
