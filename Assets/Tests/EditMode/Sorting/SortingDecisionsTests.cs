using NUnit.Framework;
using UnityEngine;

public class SortingDecisionsTests
{
    private TestUtils utils;
    private TableContents table;
    private Wallet wallet;
    private SortingDecisions decisions;
    private TrashItemData toolbox;
    private TrashItemData can;
    private GameEvent targetFound;
    private GameEvent targetLost;
    private GameEvent wrongPick;
    private int found;
    private int lost;
    private int wrong;
    private int selectionChanges;

    [SetUp]
    public void SetUp()
    {
        utils = new TestUtils();
        toolbox = utils.ItemData("Red toolbox", 0);
        can = utils.ItemData("Tin can", 10);
        LandfillDaySettings settings = utils.Settings(toolbox);
        settings.startMoney = 100;
        settings.tableCapacity = 10;

        table = utils.Component<TableContents>(("settings", settings), ("tableFull", utils.Event()), ("tableHasRoom", utils.Event()));
        wallet = utils.Component<Wallet>(("settings", settings));
        TestUtils.Call(wallet, "Awake");
        targetFound = utils.Event();
        targetLost = utils.Event();
        wrongPick = utils.Event();
        decisions = utils.Component<SortingDecisions>(
            ("settings", settings),
            ("table", table),
            ("wallet", wallet),
            ("targetFound", targetFound),
            ("targetLost", targetLost),
            ("wrongPick", wrongPick));
        TestUtils.Call(decisions, "OnEnable");

        found = 0;
        lost = 0;
        wrong = 0;
        selectionChanges = 0;
        targetFound.Subscribe(() => found++);
        targetLost.Subscribe(() => lost++);
        wrongPick.Subscribe(() => wrong++);
        decisions.SelectionChanged += () => selectionChanges++;
    }

    [TearDown]
    public void TearDown()
    {
        TestUtils.Call(decisions, "OnDisable");
        utils.DestroyAll();
    }

    private TrashItem PutOnTable(TrashItemData data)
    {
        TrashItem item = utils.Item(data);
        TestUtils.Call(table, "OnTriggerEnter2D", item.GetComponent<Collider2D>());
        return item;
    }

    private TrashItem Pick(TrashItemData data)
    {
        TrashItem item = PutOnTable(data);
        item.OnClicked();
        return item;
    }

    private static bool IsHighlighted(TrashItem item)
    {
        return TestUtils.GetField<GameObject>(item, "outline").activeSelf;
    }

    // ---------- Selecting ----------

    [Test]
    public void NothingIsSelectedAtTheStart()
    {
        Assert.IsNull(decisions.Selected);
    }

    [Test]
    public void ClickingAnItemOnTheTable_SelectsAndHighlightsIt()
    {
        TrashItem item = Pick(can);

        Assert.AreSame(item, decisions.Selected);
        Assert.IsTrue(IsHighlighted(item));
        Assert.AreEqual(1, selectionChanges);
    }

    [Test]
    public void ClickingAnotherItem_MovesTheHighlight()
    {
        TrashItem first = Pick(can);
        TrashItem second = Pick(can);

        Assert.AreSame(second, decisions.Selected);
        Assert.IsFalse(IsHighlighted(first));
        Assert.IsTrue(IsHighlighted(second));
    }

    [Test]
    public void TheSelectedItemLeavingTheTable_ClearsTheSelection()
    {
        TrashItem item = Pick(can);

        TestUtils.Call(table, "OnTriggerExit2D", item.GetComponent<Collider2D>());

        Assert.IsNull(decisions.Selected);
        Assert.IsFalse(IsHighlighted(item));
    }

    [Test]
    public void AnotherItemLeavingTheTable_KeepsTheSelection()
    {
        TrashItem selected = Pick(can);
        TrashItem other = PutOnTable(can);

        TestUtils.Call(table, "OnTriggerExit2D", other.GetComponent<Collider2D>());

        Assert.AreSame(selected, decisions.Selected);
    }

    [Test]
    public void WithNothingSelected_TheThreeButtonsDoNothing()
    {
        PutOnTable(toolbox);

        decisions.ThrowSelectedToWaste();
        decisions.SellSelected();
        decisions.MarkSelectedAsTarget();

        Assert.AreEqual(100, wallet.Money);
        Assert.AreEqual(1, table.ItemCount);
        Assert.AreEqual(0, found + lost + wrong);
        Assert.AreEqual(0, selectionChanges);
    }

    // ---------- Waste ----------

    [Test]
    public void Waste_TakesTheItemOffTheTableForNoMoney()
    {
        Pick(can);

        EditModeLog.ExpectDestroy();
        decisions.ThrowSelectedToWaste();

        Assert.AreEqual(0, table.ItemCount);
        Assert.AreEqual(100, wallet.Money);
        Assert.IsNull(decisions.Selected);
        Assert.AreEqual(0, lost);
    }

    [Test]
    public void Waste_OfTheTarget_LosesTheTarget()
    {
        Pick(toolbox);

        EditModeLog.ExpectDestroy();
        decisions.ThrowSelectedToWaste();

        Assert.AreEqual(1, lost);
        Assert.AreEqual(0, found);
    }

    // ---------- Sell ----------

    [TestCase(0)]
    [TestCase(10)]
    [TestCase(30)]
    public void Sell_AddsTheItemsValueAndTakesItOffTheTable(int price)
    {
        can.sellPrice = price;
        Pick(can);

        EditModeLog.ExpectDestroy();
        decisions.SellSelected();

        Assert.AreEqual(100 + price, wallet.Money);
        Assert.AreEqual(0, table.ItemCount);
        Assert.IsNull(decisions.Selected);
        Assert.AreEqual(0, lost);
    }

    [Test]
    public void Sell_OfTheTarget_LosesTheTarget()
    {
        Pick(toolbox);

        EditModeLog.ExpectDestroy();
        decisions.SellSelected();

        Assert.AreEqual(1, lost);
        Assert.AreEqual(0, found);
    }

    // ---------- This is the target ----------

    [Test]
    public void Mark_OfTheTarget_FindsIt()
    {
        Pick(toolbox);

        decisions.MarkSelectedAsTarget();

        Assert.AreEqual(1, found);
        Assert.AreEqual(0, wrong);
        Assert.AreEqual(0, lost);
    }

    [Test]
    public void Mark_OfTheTarget_LeavesItOnTheTable()
    {
        Pick(toolbox);

        decisions.MarkSelectedAsTarget();

        Assert.AreEqual(1, table.ItemCount);
    }

    [Test]
    public void Mark_OfAWrongItem_IsAWrongPickAndClearsTheSelection()
    {
        TrashItem item = Pick(can);

        decisions.MarkSelectedAsTarget();

        Assert.AreEqual(1, wrong);
        Assert.AreEqual(0, found);
        Assert.IsNull(decisions.Selected);
        Assert.IsFalse(IsHighlighted(item));
        Assert.AreEqual(1, table.ItemCount, "A wrong pick only costs time, the item stays");
    }

    [Test]
    public void Regression_DoubleClickOnThisIsTheTarget_GivesThePenaltyOnlyOnce()
    {
        Pick(can);

        decisions.MarkSelectedAsTarget();
        decisions.MarkSelectedAsTarget();

        Assert.AreEqual(1, wrong);
    }

    [Test]
    public void Mark_OfALookalikeWithTheSameName_IsStillAWrongPick()
    {
        TrashItemData copy = utils.ItemData("Red toolbox", 0);
        Pick(copy);

        decisions.MarkSelectedAsTarget();

        Assert.AreEqual(1, wrong, "The target is the asset in the settings, not any item with its name");
        Assert.AreEqual(0, found);
    }

    [Test]
    public void OnDisable_StopsListeningToTheTable()
    {
        TestUtils.Call(decisions, "OnDisable");

        PutOnTable(can).OnClicked();

        Assert.IsNull(decisions.Selected);
        TestUtils.Call(decisions, "OnEnable"); // TearDown disables it again.
    }
}
