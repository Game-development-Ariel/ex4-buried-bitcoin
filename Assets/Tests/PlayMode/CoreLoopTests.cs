using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

/// <summary>
/// Plays the real Landfill scene. Any error or exception logged while a test runs fails it (LogAssert),
/// so these also catch a broken scene. Items are placed by the test where it matters, so the results don't
/// depend on where the random truck throws land.
/// </summary>
public class CoreLoopTests
{
    private LandfillScene game;
    private int wins;
    private List<LossReason> losses;

    [UnitySetUp]
    public IEnumerator LoadTheScene()
    {
        game = new LandfillScene();
        yield return LandfillScene.Load(game);
        wins = 0;
        losses = new List<LossReason>();
        game.DayWon.Subscribe(CountWin);
        game.DayLost.Subscribe(losses.Add);
    }

    [UnityTearDown]
    public IEnumerator TearDown()
    {
        game.DayWon.Unsubscribe(CountWin);
        game.DayLost.Unsubscribe(losses.Add);
        yield return LandfillScene.Unload();
    }

    private void CountWin()
    {
        wins++;
    }

    private TrashItemData Target => game.Settings.target;

    private TrashItemData Ordinary => game.ItemFromTrucks(item => item != Target && !item.isSmall, item => item.size);

    private static string ResultTitle()
    {
        GameObject canvas = GameObject.Find("Result Canvas");
        return canvas == null ? null : canvas.GetComponentsInChildren<Text>().Single(text => text.name == "Title").text;
    }

    // ---------- Smoke ----------

    [UnityTest]
    public IEnumerator TheScene_RunsForThreeSecondsWithoutErrors()
    {
        float start = game.Timer.SecondsLeft;

        yield return new WaitForSeconds(3f);

        Assert.Less(game.Timer.SecondsLeft, start - 2.5f, "The clock runs");
        Assert.AreEqual(game.Settings.startMoney, game.Wallet.Money);
        Assert.AreEqual(game.Settings.beltSpeed, game.BeltSurface.speed);
        Assert.AreEqual(0, wins + losses.Count);
        LogAssert.NoUnexpectedReceived();
    }

    [UnityTest]
    public IEnumerator TheTarget_IsCalledOutOnTheTargetCard()
    {
        yield return null;

        Text description = Object.FindObjectsByType<Text>(FindObjectsSortMode.None).Single(text => text.name == "Description" && text.text == Target.description);
        Assert.IsNotNull(description);
    }

    // ---------- Trucks and the belt ----------

    [UnityTest]
    public IEnumerator Trucks_DumpItemsThatRideTheBeltToTheTable()
    {
        var trucks = new List<int>();
        game.Dumper.TruckStarted += trucks.Add;
        Time.timeScale = 4f;

        yield return LandfillScene.WaitUntil(() => game.Table.ItemCount > 0, 40f, "an item from the truck on the table");

        CollectionAssert.AreEqual(new[] { 1 }, trucks);
        Assert.AreEqual(0, wins + losses.Count);
        game.Dumper.TruckStarted -= trucks.Add;
    }

    [UnityTest]
    public IEnumerator Belt_CarriesAnItemToTheRight()
    {
        TrashItem item = game.Spawn(Ordinary, new Vector2(-6f, 1.3f));
        yield return new WaitForSeconds(0.3f);
        float start = item.transform.position.x;

        yield return new WaitForSeconds(1f);

        Assert.Greater(item.transform.position.x, start + 1f);
    }

    // ---------- A full table stops the line ----------

    private IEnumerator FillTheTable(List<TrashItem> items)
    {
        int capacity = game.Settings.tableCapacity;
        TrashItemData small = game.ItemFromTrucks(item => item != Target, item => item.size);
        for (int i = 0; i < capacity; i++)
        {
            float x = 4.3f + (i % 7) * 0.55f;
            float y = -1.15f + (i / 7) * 0.55f;
            items.Add(game.Spawn(small, new Vector2(x, y)));
        }
        yield return LandfillScene.WaitUntil(() => game.Table.ItemCount == capacity, 3f, "a full table");
    }

    private void Waste(TrashItem item)
    {
        item.OnClicked();
        Assert.AreSame(item, game.Decisions.Selected);
        game.Decisions.ThrowSelectedToWaste();
    }

    [UnityTest]
    public IEnumerator FullTable_StopsTheBeltAndTheTruck()
    {
        var items = new List<TrashItem>();
        yield return FillTheTable(items);

        Assert.AreEqual(0f, game.BeltSurface.speed);
        Assert.IsTrue(game.TruckIsWaiting);
    }

    [UnityTest]
    public IEnumerator Hysteresis_SortingOneItemIsNotEnough_SortingTwoRestartsTheLine()
    {
        var items = new List<TrashItem>();
        yield return FillTheTable(items);

        Waste(items[0]);
        yield return new WaitForFixedUpdate();
        Assert.AreEqual(0f, game.BeltSurface.speed, "One free place: still stopped");
        Assert.IsTrue(game.TruckIsWaiting);

        Waste(items[1]);
        yield return new WaitForFixedUpdate();
        Assert.AreEqual(game.Settings.beltSpeed, game.BeltSurface.speed, "Two free places: running again");
        Assert.IsFalse(game.TruckIsWaiting);
        Assert.AreEqual(game.Settings.tableCapacity - 2, game.Table.ItemCount);
    }

    [UnityTest]
    public IEnumerator Selling_AnItemOnTheTable_PaysItsPrice()
    {
        TrashItemData valuable = game.ItemFromTrucks(item => item != Target && item.sellPrice > 0);
        TrashItem item = game.Spawn(valuable, LandfillScene.OnTheTable);
        yield return game.PutOnTable(item);

        item.OnClicked();
        game.Decisions.SellSelected();
        yield return null;

        Assert.AreEqual(game.Settings.startMoney + valuable.sellPrice, game.Wallet.Money);
        Assert.IsTrue(item == null, "A sold item is destroyed");
        Assert.AreEqual(0, game.Table.ItemCount);
    }

    // ---------- Winning ----------

    [UnityTest]
    public IEnumerator FindingTheTarget_WinsAndStopsTheLineAndTheClock()
    {
        TrashItem target = game.Spawn(Target, LandfillScene.OnTheTable);
        yield return game.PutOnTable(target);

        target.OnClicked();
        game.Decisions.MarkSelectedAsTarget();
        float timeAtTheEnd = game.Timer.SecondsLeft;
        yield return new WaitForSeconds(0.5f);

        Assert.AreEqual(1, wins);
        Assert.IsEmpty(losses);
        Assert.AreEqual(0f, game.BeltSurface.speed);
        Assert.IsFalse(game.Belt.enabled);
        Assert.IsFalse(game.Dumper.enabled);
        Assert.IsFalse(game.Timer.enabled);
        Assert.IsFalse(game.Clicker.enabled);
        Assert.AreEqual(timeAtTheEnd, game.Timer.SecondsLeft, "The clock stopped");
        Assert.AreEqual("Found it!", ResultTitle());
    }

    [UnityTest]
    public IEnumerator Regression_AfterWinning_TheBeltDoesNotPushItemsAnymore()
    {
        TrashItem target = game.Spawn(Target, LandfillScene.OnTheTable);
        yield return game.PutOnTable(target);
        target.OnClicked();
        game.Decisions.MarkSelectedAsTarget();

        TrashItem item = game.Spawn(Ordinary, new Vector2(-6f, 1.3f));
        yield return new WaitForSeconds(0.5f);
        float start = item.transform.position.x;
        yield return new WaitForSeconds(1f);

        Assert.AreEqual(start, item.transform.position.x, 0.05f);
    }

    // ---------- Losing ----------

    [UnityTest]
    public IEnumerator TimeRunningOut_LosesTheDay()
    {
        game.Timer.Subtract(game.Timer.SecondsLeft - 0.3f);

        yield return LandfillScene.WaitUntil(() => losses.Count > 0, 2f, "the day to be lost");
        yield return null;

        CollectionAssert.AreEqual(new[] { LossReason.TimeUp }, losses);
        Assert.AreEqual(0, wins);
        Assert.AreEqual("Day failed", ResultTitle());
        Assert.IsFalse(game.Belt.enabled);
    }

    [UnityTest]
    public IEnumerator AWrongPick_CostsThePenaltySeconds()
    {
        TrashItem item = game.Spawn(Ordinary, LandfillScene.OnTheTable);
        yield return game.PutOnTable(item);
        float before = game.Timer.SecondsLeft;

        item.OnClicked();
        game.Decisions.MarkSelectedAsTarget();

        Assert.AreEqual(before - game.Settings.wrongPickPenaltySeconds, game.Timer.SecondsLeft, 0.1f);
        Assert.IsNull(game.Decisions.Selected);
        Assert.AreEqual(0, wins + losses.Count);
    }

    [UnityTest]
    public IEnumerator SellingTheTarget_LosesTheDay()
    {
        TrashItem target = game.Spawn(Target, LandfillScene.OnTheTable);
        yield return game.PutOnTable(target);

        target.OnClicked();
        game.Decisions.SellSelected();
        yield return null;

        CollectionAssert.AreEqual(new[] { LossReason.TargetBuried }, losses);
    }

    [UnityTest]
    public IEnumerator TheTargetFallingIntoTheWastePit_LosesTheDay()
    {
        TrashItem target = game.Spawn(Target, new Vector2(-2f, -1.6f));

        yield return LandfillScene.WaitUntil(() => target == null, 3f, "the pit to bury the target");

        CollectionAssert.AreEqual(new[] { LossReason.TargetBuried }, losses);
    }

    [UnityTest]
    public IEnumerator Regression_AnItemThatEscapesThePlant_IsBuriedByTheBorders()
    {
        TrashItem escaped = game.Spawn(Ordinary, new Vector2(12f, -6f));

        yield return LandfillScene.WaitUntil(() => escaped == null, 3f, "the bottom border to bury the item");

        Assert.IsEmpty(losses, "An ordinary item escaping is not a loss");
    }

    [UnityTest]
    public IEnumerator Regression_TheTargetEscapingThePlant_LosesTheDayInsteadOfFallingForever()
    {
        TrashItem target = game.Spawn(Target, new Vector2(13f, 1f));
        target.GetComponent<Rigidbody2D>().linearVelocity = new Vector2(20f, 0f);

        yield return LandfillScene.WaitUntil(() => target == null, 3f, "the right border to bury the target");

        CollectionAssert.AreEqual(new[] { LossReason.TargetBuried }, losses);
    }

    [UnityTest]
    public IEnumerator AfterLosing_FindingTheTargetDoesNotWin()
    {
        game.Timer.Subtract(game.Timer.SecondsLeft);
        TrashItem target = game.Spawn(Target, LandfillScene.OnTheTable);
        yield return game.PutOnTable(target);

        target.OnClicked();
        game.Decisions.MarkSelectedAsTarget();

        Assert.AreEqual(0, wins);
        CollectionAssert.AreEqual(new[] { LossReason.TimeUp }, losses);
    }

    // ---------- Play again ----------

    [UnityTest]
    public IEnumerator PlayAgain_ReloadsTheSceneWithAFreshDay()
    {
        PlayModeTestEnvironment.UseRealFrameTime();
        game.Wallet.Add(500);
        game.Timer.Subtract(game.Timer.SecondsLeft);
        yield return null;
        LandfillRound oldRound = game.Round;
        int loads = 0;
        UnityEngine.Events.UnityAction<Scene, LoadSceneMode> countLoad = (scene, mode) => loads++;
        SceneManager.sceneLoaded += countLoad;

        Button again = Object.FindObjectsByType<Button>(FindObjectsSortMode.None).Single(button => button.name == "Play again Button");
        again.onClick.Invoke();
        again.onClick.Invoke(); // A double click must not load the scene twice.

        yield return LandfillScene.WaitUntil(() => GameObject.Find("Screen Fader") == null, 5f, "the fade to finish");
        SceneManager.sceneLoaded -= countLoad;
        game.DayWon.Unsubscribe(CountWin);
        game.DayLost.Unsubscribe(losses.Add);
        game.FindParts();
        game.DayWon.Subscribe(CountWin);
        game.DayLost.Subscribe(losses.Add);

        Assert.AreEqual(1, loads);
        Assert.IsTrue(oldRound == null, "The old scene is gone");
        Assert.AreEqual(game.Settings.startMoney, game.Wallet.Money);
        Assert.Greater(game.Timer.SecondsLeft, game.Settings.timeLimitSeconds - 2f);
        Assert.IsTrue(game.Belt.enabled);
        Assert.IsNull(GameObject.Find("Result Canvas"));
        Assert.IsFalse(TestUtils.GetStaticField<bool>(typeof(ScreenFader), "isFading"));
    }
}
