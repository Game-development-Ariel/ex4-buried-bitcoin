using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

public class ButtonFeelTests
{
    private Canvas canvas;
    private ButtonFeel feel;

    [SetUp]
    public void SetUp()
    {
        canvas = UiFactory.CreateCanvas("Test Canvas", 0);
        Button button = UiFactory.CreateButton(canvas.transform, "Test", Vector2.zero, new Vector2(100f, 40f), 20, Color.white);
        feel = button.GetComponent<ButtonFeel>();
    }

    [TearDown]
    public void TearDown()
    {
        UiTestUtils.DestroyRootUi();
    }

    private IEnumerator Settle()
    {
        yield return new WaitForSecondsRealtime(0.6f);
    }

    [UnityTest]
    public IEnumerator Hover_GrowsTheButtonALittle()
    {
        feel.OnPointerEnter(null);
        yield return Settle();

        Assert.AreEqual(1.04f, feel.transform.localScale.x, 0.01f);
    }

    [UnityTest]
    public IEnumerator Press_ShrinksItAndClicks()
    {
        feel.OnPointerDown(null);
        yield return Settle();

        Assert.AreEqual(0.97f, feel.transform.localScale.x, 0.01f);
        Assert.IsNotNull(GameObject.Find("Sound Effects"), "The click sound played");
    }

    [UnityTest]
    public IEnumerator Release_GoesBackToHover_AndLeavingGoesBackToNormal()
    {
        feel.OnPointerDown(null);
        feel.OnPointerUp(null);
        yield return Settle();
        Assert.AreEqual(1.04f, feel.transform.localScale.x, 0.01f);

        feel.OnPointerExit(null);
        yield return Settle();
        Assert.AreEqual(1f, feel.transform.localScale.x, 0.01f);
    }

    [UnityTest]
    public IEnumerator TheAnimation_IgnoresTheGameSpeed_SoItWorksWhenTheGameIsPaused()
    {
        Time.timeScale = 0f;
        feel.OnPointerEnter(null);
        yield return Settle();
        Time.timeScale = 1f;

        Assert.AreEqual(1.04f, feel.transform.localScale.x, 0.01f);
    }
}
