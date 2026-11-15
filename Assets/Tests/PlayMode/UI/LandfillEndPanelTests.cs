using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

public class LandfillEndPanelTests
{
    private TestUtils utils;
    private GameEvent dayWon;
    private DayLostEvent dayLost;
    private LandfillEndPanel panel;

    [SetUp]
    public void SetUp()
    {
        utils = new TestUtils();
        dayWon = utils.Event();
        dayLost = utils.Asset<DayLostEvent>();
        panel = TestUtils.Activate(utils.Component<LandfillEndPanel>(("dayWon", dayWon), ("dayLost", dayLost)));
    }

    [TearDown]
    public void TearDown()
    {
        utils.DestroyAll();
        UiTestUtils.DestroyRootUi();
    }

    private static string Title => UiTestUtils.Text(UiTestUtils.ResultScreen(), "Title").text;
    private static string Message => UiTestUtils.Text(UiTestUtils.ResultScreen(), "Message").text;

    [Test]
    public void NothingIsShownBeforeTheDayEnds()
    {
        Assert.IsNull(UiTestUtils.ResultScreen());
    }

    [Test]
    public void Winning_ShowsFoundIt()
    {
        dayWon.Raise();

        Assert.AreEqual("Found it!", Title);
        StringAssert.Contains("The target is back with its owner.", Message);
        Assert.AreEqual(UiTheme.Accent, UiTestUtils.Text(UiTestUtils.ResultScreen(), "Title").color);
    }

    [TestCase(LossReason.TargetBuried, "The target went to the waste pile. It is buried again.")]
    [TestCase(LossReason.TimeUp, "The work day is over, and the target is still somewhere in the trash.")]
    [TestCase((LossReason)99, "The day is over.")]
    public void Losing_ShowsTheReasonInWords(LossReason reason, string words)
    {
        dayLost.Raise(reason);

        Assert.AreEqual("Day failed", Title);
        Assert.AreEqual(words, Message);
        Assert.AreEqual(UiTheme.Danger, UiTestUtils.Text(UiTestUtils.ResultScreen(), "Title").color);
    }

    [Test]
    public void TheResultScreen_BlocksClicksAndHasPlayAgain()
    {
        dayLost.Raise(LossReason.TimeUp);
        Canvas screen = UiTestUtils.ResultScreen();

        Image cover = screen.GetComponentsInChildren<Image>().Single(image => image.name == "Cover");
        Assert.IsTrue(cover.raycastTarget);
        Assert.IsNotNull(UiTestUtils.Button(screen, "Play again Button"));
        Assert.IsNotNull(Object.FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>(), "The button needs an EventSystem");
    }

    [Test]
    public void ATurnedOffPanel_ShowsNothing()
    {
        panel.enabled = false;

        dayWon.Raise();
        dayLost.Raise(LossReason.TimeUp);

        Assert.IsNull(UiTestUtils.ResultScreen());
    }
}
