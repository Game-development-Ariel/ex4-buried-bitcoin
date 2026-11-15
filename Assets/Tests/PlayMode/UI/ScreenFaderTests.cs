using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

/// <summary>The fader's guard against double loads. Loading the real scene is in CoreLoopTests.PlayAgain.</summary>
public class ScreenFaderTests
{
    [TearDown]
    public void TearDown()
    {
        LandfillScene.CleanUpLeftovers();
    }

    private static bool IsFading
    {
        get => TestUtils.GetStaticField<bool>(typeof(ScreenFader), "isFading");
        set => TestUtils.SetStaticField(typeof(ScreenFader), "isFading", value);
    }

    [Test]
    public void Regression_ResetStatics_ClearsAFlagLeftFromThePreviousPlay()
    {
        IsFading = true;

        TestUtils.CallStatic(typeof(ScreenFader), "ResetStatics");

        Assert.IsFalse(IsFading);
    }

    [Test]
    public void FadeAndLoad_WhileAlreadyFading_DoesNothing()
    {
        IsFading = true;

        ScreenFader.FadeAndLoad("Landfill");

        Assert.IsNull(GameObject.Find("Screen Fader"));
    }

    [UnityTest]
    public IEnumerator FadeAndLoad_CoversTheScreenWithBlackThatSurvivesTheLoad()
    {
        ScreenFader.FadeAndLoad("Landfill");
        GameObject fader = GameObject.Find("Screen Fader");

        Assert.IsNotNull(fader);
        Assert.IsTrue(IsFading);
        Assert.AreEqual("DontDestroyOnLoad", fader.scene.name);
        Assert.AreEqual(0f, fader.GetComponent<CanvasGroup>().alpha, 0.01f, "It starts see-through and fades in");
        Assert.AreEqual(1000, fader.GetComponent<Canvas>().sortingOrder, "Above every other canvas");

        yield return LandfillScene.WaitUntil(() => fader == null, 5f, "the fade to finish");
        Assert.IsFalse(IsFading);
    }
}
