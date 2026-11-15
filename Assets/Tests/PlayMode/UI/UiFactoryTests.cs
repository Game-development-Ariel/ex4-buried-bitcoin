using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.TestTools;
using UnityEngine.UI;

public class UiFactoryTests
{
    [TearDown]
    public void TearDown()
    {
        Time.timeScale = 1f;
        UiTestUtils.DestroyRootUi();
    }

    [Test]
    public void CreateCanvas_ScalesFromFullHd()
    {
        Canvas canvas = UiFactory.CreateCanvas("Test Canvas", 7);

        Assert.AreEqual(7, canvas.sortingOrder);
        Assert.AreEqual(RenderMode.ScreenSpaceOverlay, canvas.renderMode);
        CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
        Assert.AreEqual(CanvasScaler.ScaleMode.ScaleWithScreenSize, scaler.uiScaleMode);
        Assert.AreEqual(new Vector2(1920f, 1080f), scaler.referenceResolution);
        Assert.IsNotNull(canvas.GetComponent<GraphicRaycaster>());
    }

    [Test]
    public void CreateText_UsesTheThemeAndDoesNotCatchClicks()
    {
        Canvas canvas = UiFactory.CreateCanvas("Test Canvas", 0);

        Text text = UiFactory.CreateText(canvas.transform, "Line", Vector2.zero, new Vector2(100f, 30f), 24, TextAnchor.MiddleLeft);

        Assert.AreEqual(UiTheme.TextFont, text.font);
        Assert.IsNotNull(text.font, "The font is in Assets/Resources/Fonts");
        Assert.AreEqual(24, text.fontSize);
        Assert.IsFalse(text.raycastTarget);
        Assert.IsNotNull(text.GetComponent<Shadow>());
    }

    [Test]
    public void CreateTitle_UsesTheTitleFontAndColor()
    {
        Canvas canvas = UiFactory.CreateCanvas("Test Canvas", 0);

        Text title = UiFactory.CreateTitle(canvas.transform, "Title", Vector2.zero, new Vector2(100f, 30f), 50, Color.red);

        Assert.AreEqual(UiTheme.TitleFont, title.font);
        Assert.IsNotNull(title.font);
        Assert.AreEqual(Color.red, title.color);
    }

    [Test]
    public void CreateButton_CatchesClicksHasALabelAndFeelsAlive()
    {
        Canvas canvas = UiFactory.CreateCanvas("Test Canvas", 0);

        Button button = UiFactory.CreateButton(canvas.transform, "Buy", Vector2.zero, new Vector2(100f, 40f), 20, Color.white);

        Assert.AreEqual("Buy Button", button.name);
        Assert.IsTrue(button.GetComponent<Image>().raycastTarget);
        Assert.IsNotNull(button.GetComponent<Image>().sprite, "The button sprite is in Assets/Resources/UI");
        Assert.IsNotNull(button.GetComponent<ButtonFeel>());
        Assert.AreEqual("Buy", button.GetComponentInChildren<Text>().text);
    }

    [Test]
    public void CreatePanel_IsASlicedSpriteFromTheTheme()
    {
        Canvas canvas = UiFactory.CreateCanvas("Test Canvas", 0);

        Image panel = UiFactory.CreatePanel(canvas.transform, "Card", "Panel");

        Assert.AreEqual(Image.Type.Sliced, panel.type);
        Assert.AreEqual(UiTheme.Sprite("Panel"), panel.sprite);
        Assert.IsNotNull(panel.sprite);
    }

    [Test]
    public void EnsureEventSystem_CreatesOneForTheInputSystem_AndOnlyOnce()
    {
        UiFactory.EnsureEventSystem();
        UiFactory.EnsureEventSystem();

        EventSystem[] systems = Object.FindObjectsByType<EventSystem>(FindObjectsSortMode.None);
        Assert.AreEqual(1, systems.Length);
        Assert.IsNotNull(systems[0].GetComponent<InputSystemUIInputModule>());
    }

    [Test]
    public void Place_AndStretch_SetTheRect()
    {
        RectTransform rect = new GameObject("Rect", typeof(RectTransform)).GetComponent<RectTransform>();

        UiFactory.Place(rect, new Vector2(1f, 0f), new Vector2(-5f, 6f), new Vector2(30f, 40f));
        Assert.AreEqual(new Vector2(1f, 0f), rect.anchorMin);
        Assert.AreEqual(new Vector2(1f, 0f), rect.pivot);
        Assert.AreEqual(new Vector2(-5f, 6f), rect.anchoredPosition);
        Assert.AreEqual(new Vector2(30f, 40f), rect.sizeDelta);

        UiFactory.Stretch(rect);
        Assert.AreEqual(Vector2.zero, rect.anchorMin);
        Assert.AreEqual(Vector2.one, rect.anchorMax);
        Assert.AreEqual(Vector2.zero, rect.offsetMin);
        Object.DestroyImmediate(rect.gameObject);
    }

    [UnityTest]
    public IEnumerator UiAnimFade_ReachesTheGoalAfterTheTime_EvenWhenTheGameIsPaused()
    {
        var group = new GameObject("Group", typeof(CanvasGroup)).GetComponent<CanvasGroup>();
        group.alpha = 0f;
        ClickCounter host = group.gameObject.AddComponent<ClickCounter>();
        Time.timeScale = 0f;

        host.StartCoroutine(UiAnim.Fade(group, 1f, 0.2f));
        yield return UiTestUtils.WaitUnscaled(0.4f);

        Assert.AreEqual(1f, group.alpha);
        Object.DestroyImmediate(group.gameObject);
    }

    [UnityTest]
    public IEnumerator UiAnimFade_WithZeroSeconds_JumpsToTheGoal()
    {
        var group = new GameObject("Group", typeof(CanvasGroup)).GetComponent<CanvasGroup>();
        ClickCounter host = group.gameObject.AddComponent<ClickCounter>();

        host.StartCoroutine(UiAnim.Fade(group, 0.25f, 0f));
        yield return null;

        Assert.AreEqual(0.25f, group.alpha);
        Object.DestroyImmediate(group.gameObject);
    }
}
