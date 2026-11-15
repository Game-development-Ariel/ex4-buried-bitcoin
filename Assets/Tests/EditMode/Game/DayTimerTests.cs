using NUnit.Framework;

public class DayTimerTests
{
    private TestUtils utils;
    private DayTimer timer;
    private int ranOut;

    [SetUp]
    public void SetUp()
    {
        utils = new TestUtils();
        LandfillDaySettings settings = utils.Settings();
        settings.timeLimitSeconds = 150f;
        timer = utils.Component<DayTimer>(("settings", settings));
        TestUtils.Call(timer, "Awake");
        ranOut = 0;
        timer.RanOut += () => ranOut++;
    }

    [TearDown]
    public void TearDown()
    {
        utils.DestroyAll();
    }

    [Test]
    public void Awake_StartsWithTheTimeLimitFromTheSettings()
    {
        Assert.AreEqual(150f, timer.SecondsLeft);
    }

    [TestCase(0f, 150f)]
    [TestCase(0.5f, 149.5f)]
    [TestCase(20f, 130f)]
    [TestCase(149.99f, 0.01f)]
    public void Subtract_TakesTheSecondsOff(float seconds, float left)
    {
        timer.Subtract(seconds);

        Assert.AreEqual(left, timer.SecondsLeft, 0.0001f);
        Assert.AreEqual(0, ranOut);
    }

    [TestCase(150f)]
    [TestCase(151f)]
    [TestCase(10000f)]
    public void Subtract_ToZeroOrBelow_StopsAtZeroAndRunsOutOnce(float seconds)
    {
        timer.Subtract(seconds);

        Assert.AreEqual(0f, timer.SecondsLeft);
        Assert.AreEqual(1, ranOut);
    }

    [Test]
    public void Subtract_AfterRunningOut_DoesNotRunOutAgain()
    {
        timer.Subtract(150f);
        timer.Subtract(1f);
        timer.Subtract(150f);

        Assert.AreEqual(0f, timer.SecondsLeft);
        Assert.AreEqual(1, ranOut);
    }

    [Test]
    public void Subtract_InSmallSteps_RunsOutExactlyOnce()
    {
        for (int i = 0; i < 400; i++)
        {
            timer.Subtract(0.5f);
        }

        Assert.AreEqual(1, ranOut);
    }

    [Test]
    public void WrongPickPenalty_IsTakenOffLikePassingTime()
    {
        timer.Subtract(100f);
        timer.Subtract(20f);

        Assert.AreEqual(30f, timer.SecondsLeft, 0.0001f);
    }
}
