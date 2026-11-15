using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

public class TableContentsTests
{
    private TestUtils utils;
    private LandfillDaySettings settings;
    private TableContents table;
    private GameEvent tableFull;
    private GameEvent tableHasRoom;
    private int fullCount;
    private int roomCount;
    private int changes;
    private List<TrashItem> clicked;
    private List<TrashItem> removed;
    private TrashItemData can;

    [SetUp]
    public void SetUp()
    {
        utils = new TestUtils();
        settings = utils.Settings();
        settings.tableCapacity = 4;
        settings.freePlacesToRestart = 2;
        tableFull = utils.Event();
        tableHasRoom = utils.Event();
        table = utils.Component<TableContents>(("settings", settings), ("tableFull", tableFull), ("tableHasRoom", tableHasRoom));

        fullCount = 0;
        roomCount = 0;
        changes = 0;
        tableFull.Subscribe(() => fullCount++);
        tableHasRoom.Subscribe(() => roomCount++);
        table.Changed += () => changes++;
        clicked = new List<TrashItem>();
        removed = new List<TrashItem>();
        table.ItemClicked += clicked.Add;
        table.ItemRemoved += removed.Add;
        can = utils.ItemData("Tin can", 10);
    }

    [TearDown]
    public void TearDown()
    {
        utils.DestroyAll();
    }

    private TrashItem Enter()
    {
        TrashItem item = utils.Item(can);
        TestUtils.Call(table, "OnTriggerEnter2D", item.GetComponent<Collider2D>());
        return item;
    }

    private static void Exit(TableContents table, TrashItem item)
    {
        TestUtils.Call(table, "OnTriggerExit2D", item.GetComponent<Collider2D>());
    }

    private List<TrashItem> Fill(int count)
    {
        var items = new List<TrashItem>();
        for (int i = 0; i < count; i++)
        {
            items.Add(Enter());
        }
        return items;
    }

    [Test]
    public void Capacity_ComesFromTheSettings()
    {
        Assert.AreEqual(4, table.Capacity);
    }

    [TestCase(0)]
    [TestCase(1)]
    [TestCase(3)]
    public void ItemCount_CountsTheItemsThatEntered(int count)
    {
        Fill(count);

        Assert.AreEqual(count, table.ItemCount);
        Assert.AreEqual(count, changes);
    }

    [Test]
    public void Enter_TheSameItemTwice_CountsItOnce()
    {
        TrashItem item = Enter();
        TestUtils.Call(table, "OnTriggerEnter2D", item.GetComponent<Collider2D>());

        Assert.AreEqual(1, table.ItemCount);
        Assert.AreEqual(1, changes);
    }

    [Test]
    public void Enter_OfSomethingThatIsNotAnItem_IsIgnored()
    {
        BoxCollider2D wall = utils.NewObject("Wall").AddComponent<BoxCollider2D>();

        TestUtils.Call(table, "OnTriggerEnter2D", wall);

        Assert.AreEqual(0, table.ItemCount);
        Assert.AreEqual(0, changes);
    }

    [Test]
    public void OneBelowCapacity_IsNotFull()
    {
        Fill(3);

        Assert.AreEqual(0, fullCount);
    }

    [Test]
    public void AtCapacity_RaisesTableFullOnce()
    {
        Fill(4);

        Assert.AreEqual(1, fullCount);
    }

    [Test]
    public void AboveCapacity_DoesNotRaiseTableFullAgain()
    {
        Fill(6);

        Assert.AreEqual(1, fullCount);
        Assert.AreEqual(6, table.ItemCount);
    }

    [Test]
    public void TableHasRoom_IsNotRaisedBeforeTheTableWasFull()
    {
        List<TrashItem> items = Fill(3);

        Exit(table, items[0]);
        Exit(table, items[1]);

        Assert.AreEqual(0, roomCount);
    }

    [Test]
    public void Hysteresis_OneFreePlaceIsNotEnoughToRestart()
    {
        List<TrashItem> items = Fill(4);

        Exit(table, items[0]);

        Assert.AreEqual(0, roomCount, "An item bouncing at the edge must not start and stop the line again and again");
    }

    [Test]
    public void Hysteresis_TheLineRestartsWhenFreePlacesToRestartAreFree()
    {
        List<TrashItem> items = Fill(4);

        Exit(table, items[0]);
        Exit(table, items[1]);

        Assert.AreEqual(1, roomCount);
    }

    [Test]
    public void Hysteresis_AfterRestarting_FillingAgainRaisesTableFullAgain()
    {
        List<TrashItem> items = Fill(4);
        Exit(table, items[0]);
        Exit(table, items[1]);

        Fill(2);

        Assert.AreEqual(2, fullCount);
    }

    [Test]
    public void Hysteresis_AboveCapacity_CountsFromCapacityNotFromThePeak()
    {
        List<TrashItem> items = Fill(6);

        Exit(table, items[0]);
        Exit(table, items[1]);
        Exit(table, items[2]);
        Assert.AreEqual(0, roomCount, "3 items left is still above capacity - 2");

        Exit(table, items[3]);
        Assert.AreEqual(1, roomCount);
    }

    [TestCase(1)]
    [TestCase(2)]
    [TestCase(4)]
    public void Hysteresis_FollowsFreePlacesToRestartFromTheSettings(int freePlaces)
    {
        settings.freePlacesToRestart = freePlaces;
        List<TrashItem> items = Fill(4);

        for (int i = 0; i < freePlaces - 1; i++)
        {
            Exit(table, items[i]);
        }
        Assert.AreEqual(0, roomCount);

        Exit(table, items[freePlaces - 1]);
        Assert.AreEqual(1, roomCount);
    }

    [Test]
    public void Exit_RemovesTheItemAndTellsTheListeners()
    {
        TrashItem item = Enter();

        Exit(table, item);

        Assert.AreEqual(0, table.ItemCount);
        CollectionAssert.AreEqual(new[] { item }, removed);
        Assert.AreEqual(2, changes);
    }

    [Test]
    public void Exit_OfAnItemThatIsNotOnTheTable_IsIgnored()
    {
        TrashItem stranger = utils.Item(can);

        Exit(table, stranger);

        Assert.IsEmpty(removed);
        Assert.AreEqual(0, changes);
    }

    [Test]
    public void ClickOnAnItemOnTheTable_IsForwardedAsItemClicked()
    {
        TrashItem item = Enter();

        item.OnClicked();

        CollectionAssert.AreEqual(new[] { item }, clicked);
    }

    [Test]
    public void ClickOnAnItemThatBouncedOff_IsNoLongerForwarded()
    {
        TrashItem item = Enter();
        Exit(table, item);

        item.OnClicked();

        Assert.IsEmpty(clicked);
        Assert.AreEqual(0, TestUtils.ListenerCount(item, "Clicked"));
    }

    [Test]
    public void Discard_RemovesTheItemAndStopsListeningToIt()
    {
        TrashItem item = Enter();

        table.Discard(item);

        Assert.AreEqual(0, table.ItemCount);
        CollectionAssert.AreEqual(new[] { item }, removed);
        Assert.AreEqual(0, TestUtils.ListenerCount(item, "Clicked"));
        Assert.IsTrue(item == null, "The item's object is destroyed");
    }

    [Test]
    public void Discard_CanRestartTheLineLikeAnItemLeaving()
    {
        List<TrashItem> items = Fill(4);

        table.Discard(items[0]);
        table.Discard(items[1]);

        Assert.AreEqual(1, roomCount);
    }

    [Test]
    public void OnDestroy_UnsubscribesFromEveryItemStillOnTheTable()
    {
        List<TrashItem> items = Fill(3);

        TestUtils.Call(table, "OnDestroy");

        foreach (TrashItem item in items)
        {
            Assert.AreEqual(0, TestUtils.ListenerCount(item, "Clicked"));
        }
        Assert.AreEqual(0, table.ItemCount);
    }

    [Test]
    public void OnDestroy_SkipsItemsThatWereAlreadyDestroyed()
    {
        List<TrashItem> items = Fill(2);
        Object.DestroyImmediate(items[0].gameObject);

        Assert.DoesNotThrow(() => TestUtils.Call(table, "OnDestroy"));
        Assert.AreEqual(0, TestUtils.ListenerCount(items[1], "Clicked"));
    }
}
