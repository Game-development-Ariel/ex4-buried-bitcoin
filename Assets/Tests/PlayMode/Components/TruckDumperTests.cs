using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class TruckDumperTests
{
    private TestUtils utils;
    private LandfillDaySettings settings;
    private TrashItemData dirt;
    private TrashItemData toolbox;
    private GameEvent tableFull;
    private GameEvent tableHasRoom;
    private TruckDumper dumper;
    private List<int> trucksStarted;
    private int allDone;

    [SetUp]
    public void SetUp()
    {
        utils = new TestUtils();
        dirt = utils.ItemData("Dirt");
        toolbox = utils.ItemData("Toolbox");
        settings = utils.Settings(toolbox);
        settings.trucks = new[] { TestUtils.Load((dirt, 3)), TestUtils.Load((dirt, 2), (toolbox, 1)) };
        settings.secondsBeforeTruck = 0.05f;
        settings.dumpInterval = 0.02f;
        tableFull = utils.Event();
        tableHasRoom = utils.Event();

        // The template stays far away and doesn't fall. The dumped copies don't fall either.
        TrashItem template = utils.Item(dirt, new Vector2(100f, 100f), 0f);
        dumper = utils.Component<TruckDumper>(("settings", settings), ("itemPrefab", template), ("tableFull", tableFull), ("tableHasRoom", tableHasRoom));
        trucksStarted = new List<int>();
        allDone = 0;
        dumper.TruckStarted += trucksStarted.Add;
        dumper.AllTrucksDone += () => allDone++;
    }

    [TearDown]
    public void TearDown()
    {
        foreach (TrashItem item in Dumped())
        {
            Object.Destroy(item.gameObject);
        }
        utils.DestroyAll();
    }

    private TrashItem[] Dumped()
    {
        return Object.FindObjectsByType<TrashItem>(FindObjectsSortMode.None).Where(item => item.transform.position.y < 50f).ToArray();
    }

    [Test]
    public void TruckCount_IsTheNumberOfTrucksInTheSettings()
    {
        Assert.AreEqual(2, dumper.TruckCount);
    }

    [UnityTest]
    public IEnumerator TheDumper_DumpsEveryItemOfEveryTruckAndTheTargetOnce()
    {
        TestUtils.Activate(dumper);

        yield return LandfillScene.WaitUntil(() => allDone > 0, 3f, "all the trucks");

        TrashItem[] items = Dumped();
        Assert.AreEqual(6, items.Length);
        Assert.AreEqual(1, items.Count(item => item.Data == toolbox));
        Assert.AreEqual(5, items.Count(item => item.Data == dirt));
    }

    [UnityTest]
    public IEnumerator TheTrucks_StartInOrder_AndAllTrucksDoneComesOnceAtTheEnd()
    {
        TestUtils.Activate(dumper);

        yield return LandfillScene.WaitUntil(() => allDone > 0, 3f, "all the trucks");
        yield return new WaitForSeconds(0.2f);

        CollectionAssert.AreEqual(new[] { 1, 2 }, trucksStarted);
        Assert.AreEqual(1, allDone);
    }

    [UnityTest]
    public IEnumerator DumpedItems_AreSetUpFromTheirDataAndThrown()
    {
        settings.throwVelocity = new Vector2(2f, 0f);
        TestUtils.Activate(dumper);

        yield return LandfillScene.WaitUntil(() => Dumped().Length > 0, 2f, "the first item");
        yield return new WaitForFixedUpdate();

        TrashItem item = Dumped()[0];
        Assert.AreEqual(item.Data.displayName, item.name);
        Assert.Greater(item.GetComponent<Rigidbody2D>().linearVelocity.x, 1f);
    }

    [UnityTest]
    public IEnumerator AFullTable_MakesTheTruckWait_AndRoomLetsItGoOn()
    {
        settings.secondsBeforeTruck = 0.2f;
        TestUtils.Activate(dumper);
        tableFull.Raise();

        yield return new WaitForSeconds(0.6f);
        Assert.AreEqual(0, Dumped().Length, "Nothing is dumped while the table is full");
        CollectionAssert.AreEqual(new[] { 1 }, trucksStarted, "The truck arrived, and waits");

        tableHasRoom.Raise();
        yield return LandfillScene.WaitUntil(() => allDone > 0, 3f, "all the trucks");
        Assert.AreEqual(6, Dumped().Length);
    }

    [UnityTest]
    public IEnumerator ATurnedOffDumper_StopsDumping()
    {
        settings.dumpInterval = 0.2f;
        TestUtils.Activate(dumper);
        yield return LandfillScene.WaitUntil(() => Dumped().Length > 0, 2f, "the first item");

        dumper.enabled = false;
        int dumped = Dumped().Length;
        yield return new WaitForSeconds(1f);

        Assert.AreEqual(dumped, Dumped().Length);
        Assert.AreEqual(0, allDone);
    }

    [UnityTest]
    public IEnumerator Regression_WithNoTrucks_AllTrucksDoneStillComes()
    {
        settings.trucks = new TruckLoad[0];
        TestUtils.Activate(dumper);

        yield return null;

        Assert.AreEqual(1, allDone);
        Assert.IsEmpty(trucksStarted);
    }
}
