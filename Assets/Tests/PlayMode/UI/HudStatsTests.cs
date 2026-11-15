using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

public class HudStatsTests
{
    private TestUtils utils;
    private LandfillDaySettings settings;
    private Wallet wallet;
    private DayTimer timer;
    private TruckDumper dumper;
    private HudStats hud;

    [SetUp]
    public void SetUp()
    {
        utils = new TestUtils();
        TrashItemData can = utils.ItemData("Can");
        settings = utils.Settings();
        settings.startMoney = 120;
        settings.timeLimitSeconds = 150f;
        settings.warningSeconds = 20f;
        settings.trucks = new[] { TestUtils.Load((can, 1)), TestUtils.Load((can, 1)) };
        settings.secondsBeforeTruck = 0.3f;
        settings.dumpInterval = 0.3f;

        wallet = TestUtils.Activate(utils.Component<Wallet>(("settings", settings)));
        timer = TestUtils.Activate(utils.Component<DayTimer>(("settings", settings)));
        timer.enabled = false; // The test sets the time itself.
        TrashItem template = utils.Item(can, new Vector2(100f, 100f), 0f);
        dumper = utils.Component<TruckDumper>(("settings", settings), ("itemPrefab", template), ("tableFull", utils.Event()), ("tableHasRoom", utils.Event()));
        hud = utils.Component<HudStats>(("settings", settings), ("wallet", wallet), ("timer", timer), ("dumper", dumper));
    }

    [TearDown]
    public void TearDown()
    {
        foreach (TrashItem item in Object.FindObjectsByType<TrashItem>(FindObjectsSortMode.None))
        {
            Object.DestroyImmediate(item.gameObject);
        }
        utils.DestroyAll();
    }

    private Text Money => UiTestUtils.Text(hud, "Money");
    private Text Clock => UiTestUtils.Text(hud, "Time");
    private Text Truck => UiTestUtils.Text(hud, "Truck");

    [UnityTest]
    public IEnumerator ShowsTheStartMoney()
    {
        TestUtils.Activate(hud);
        yield return null;

        Assert.AreEqual("$120", Money.text);
    }

    [UnityTest]
    public IEnumerator ShowsTheMoneyEveryTimeItChanges()
    {
        TestUtils.Activate(hud);
        yield return null;

        wallet.Add(30);
        Assert.AreEqual("$150", Money.text);
        wallet.TrySpend(150);
        Assert.AreEqual("$0", Money.text);
    }

    [TestCase(0f, "2:30")]
    [TestCase(0.5f, "2:30")]
    [TestCase(1f, "2:29")]
    [TestCase(90f, "1:00")]
    [TestCase(141f, "0:09")]
    [TestCase(150f, "0:00")]
    public void ShowsTheTimeLeftAsMinutesAndSeconds(float passed, string shown)
    {
        TestUtils.Activate(hud);
        timer.Subtract(passed);

        TestUtils.Call(hud, "Update");

        Assert.AreEqual(shown, Clock.text);
    }

    [TestCase(129f, false)]
    [TestCase(130f, true)]
    [TestCase(149f, true)]
    public void TheClockTurnsRed_WhenTheWarningSecondsAreLeft(float passed, bool red)
    {
        TestUtils.Activate(hud);
        timer.Subtract(passed);

        TestUtils.Call(hud, "Update");

        Assert.AreEqual(red ? UiTheme.Danger : UiTheme.TextColor, Clock.color);
    }

    [UnityTest]
    public IEnumerator ShowsWhichTruckIsDumping_AndWhenThereAreNoMore()
    {
        TestUtils.Activate(hud);
        Assert.AreEqual("The first truck is on its way...", Truck.text);

        TestUtils.Activate(dumper);
        yield return LandfillScene.WaitUntil(() => Truck.text == "Truck 1 of 2 is dumping", 2f, "truck 1");
        yield return LandfillScene.WaitUntil(() => Truck.text == "Truck 2 of 2 is dumping", 2f, "truck 2");
        yield return LandfillScene.WaitUntil(() => Truck.text == "No more trucks today", 2f, "the last truck");
    }

    [UnityTest]
    public IEnumerator ATurnedOffHud_StopsListening()
    {
        TestUtils.Activate(hud);
        yield return null;

        hud.enabled = false;
        wallet.Add(5);

        Assert.AreEqual("$120", Money.text);
    }
}
