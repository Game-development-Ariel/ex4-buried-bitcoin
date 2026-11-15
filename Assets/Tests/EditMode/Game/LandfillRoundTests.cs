using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

public class LandfillRoundTests
{
    private TestUtils utils;
    private LandfillDaySettings settings;
    private DayTimer timer;
    private Behaviour otherPart;
    private LandfillRound round;
    private GameEvent targetFound;
    private GameEvent targetLost;
    private GameEvent wrongPick;
    private GameEvent dayWon;
    private DayLostEvent dayLost;
    private int wins;
    private List<LossReason> losses;

    [SetUp]
    public void SetUp()
    {
        utils = new TestUtils();
        settings = utils.Settings();
        settings.timeLimitSeconds = 150f;
        settings.wrongPickPenaltySeconds = 20f;
        timer = utils.Component<DayTimer>(("settings", settings));
        TestUtils.Call(timer, "Awake");
        otherPart = utils.Component<Wallet>();
        otherPart.enabled = true;

        targetFound = utils.Event();
        targetLost = utils.Event();
        wrongPick = utils.Event();
        dayWon = utils.Event();
        dayLost = utils.Asset<DayLostEvent>();

        round = utils.Component<LandfillRound>(
            ("settings", settings),
            ("timer", timer),
            ("stopWhenDayEnds", new Behaviour[] { timer, otherPart }),
            ("targetFound", targetFound),
            ("targetLost", targetLost),
            ("wrongPick", wrongPick),
            ("dayWon", dayWon),
            ("dayLost", dayLost));
        TestUtils.Call(round, "OnEnable");

        wins = 0;
        losses = new List<LossReason>();
        dayWon.Subscribe(() => wins++);
        dayLost.Subscribe(losses.Add);
    }

    [TearDown]
    public void TearDown()
    {
        TestUtils.Call(round, "OnDisable");
        utils.DestroyAll();
    }

    [Test]
    public void TargetFound_WinsTheDayOnce()
    {
        targetFound.Raise();

        Assert.AreEqual(1, wins);
        Assert.IsEmpty(losses);
    }

    [Test]
    public void TargetFound_TurnsOffEveryPartThatStopsWhenTheDayEnds()
    {
        targetFound.Raise();

        Assert.IsFalse(timer.enabled);
        Assert.IsFalse(otherPart.enabled);
    }

    [Test]
    public void TargetLost_LosesTheDayBecauseTheTargetWasBuried()
    {
        targetLost.Raise();

        CollectionAssert.AreEqual(new[] { LossReason.TargetBuried }, losses);
        Assert.AreEqual(0, wins);
        Assert.IsFalse(timer.enabled);
    }

    [Test]
    public void TimeRunningOut_LosesTheDayBecauseTheTimeIsUp()
    {
        timer.Subtract(150f);

        CollectionAssert.AreEqual(new[] { LossReason.TimeUp }, losses);
        Assert.AreEqual(0, wins);
    }

    [Test]
    public void WrongPick_TakesThePenaltyOffTheClock()
    {
        wrongPick.Raise();

        Assert.AreEqual(130f, timer.SecondsLeft, 0.0001f);
        Assert.IsEmpty(losses);
    }

    [Test]
    public void WrongPick_WithLessTimeLeftThanThePenalty_LosesBecauseTheTimeIsUp()
    {
        timer.Subtract(140f);

        wrongPick.Raise();

        CollectionAssert.AreEqual(new[] { LossReason.TimeUp }, losses);
    }

    [Test]
    public void AfterWinning_NothingElseCounts()
    {
        targetFound.Raise();

        targetFound.Raise();
        targetLost.Raise();
        timer.Subtract(150f);

        Assert.AreEqual(1, wins);
        Assert.IsEmpty(losses, "A day can't be both won and lost");
    }

    [Test]
    public void AfterLosing_NothingElseCounts()
    {
        targetLost.Raise();

        targetFound.Raise();
        targetLost.Raise();
        timer.Subtract(150f);

        Assert.AreEqual(0, wins, "A day can't be both lost and won");
        CollectionAssert.AreEqual(new[] { LossReason.TargetBuried }, losses);
    }

    [Test]
    public void AfterTheTimeIsUp_FindingTheTargetDoesNotWin()
    {
        timer.Subtract(150f);

        targetFound.Raise();

        Assert.AreEqual(0, wins);
        CollectionAssert.AreEqual(new[] { LossReason.TimeUp }, losses);
    }

    [Test]
    public void OnDisable_StopsListening()
    {
        TestUtils.Call(round, "OnDisable");

        targetFound.Raise();
        targetLost.Raise();
        wrongPick.Raise();
        timer.Subtract(150f);

        Assert.AreEqual(0, wins);
        Assert.IsEmpty(losses);
        TestUtils.Call(round, "OnEnable"); // TearDown disables it again.
    }
}
