using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class DayTimerPlayTests
{
    private TestUtils utils;
    private LandfillDaySettings settings;
    private DayTimer timer;
    private int ranOut;

    [SetUp]
    public void SetUp()
    {
        utils = new TestUtils();
        settings = utils.Settings();
        timer = utils.Component<DayTimer>(("settings", settings));
        ranOut = 0;
        timer.RanOut += () => ranOut++;
    }

    [TearDown]
    public void TearDown()
    {
        utils.DestroyAll();
    }

    [UnityTest]
    public IEnumerator TheClock_CountsDownWithTheGameTime()
    {
        settings.timeLimitSeconds = 10f;
        TestUtils.Activate(timer);

        yield return new WaitForSeconds(0.5f);

        Assert.That(timer.SecondsLeft, Is.InRange(9f, 9.6f));
    }

    [UnityTest]
    public IEnumerator TheClock_RunsOutOnceByItself()
    {
        settings.timeLimitSeconds = 0.2f;
        TestUtils.Activate(timer);

        yield return new WaitForSeconds(0.5f);

        Assert.AreEqual(0f, timer.SecondsLeft);
        Assert.AreEqual(1, ranOut);
    }

    [UnityTest]
    public IEnumerator ATurnedOffClock_Stops()
    {
        settings.timeLimitSeconds = 10f;
        TestUtils.Activate(timer);
        yield return null;

        timer.enabled = false;
        float left = timer.SecondsLeft;
        yield return new WaitForSeconds(0.3f);

        Assert.AreEqual(left, timer.SecondsLeft);
    }
}
