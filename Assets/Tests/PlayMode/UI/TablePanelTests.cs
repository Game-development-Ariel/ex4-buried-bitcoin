using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

public class TablePanelTests
{
    private TestUtils utils;
    private TableContents table;
    private SortingDecisions decisions;
    private Wallet wallet;
    private TablePanel panel;
    private TrashItemData can;

    [SetUp]
    public void SetUp()
    {
        utils = new TestUtils();
        can = utils.ItemData("Tin can", 10);
        LandfillDaySettings settings = utils.Settings(utils.ItemData("Toolbox"));
        settings.tableCapacity = 14;
        table = TestUtils.Activate(utils.Component<TableContents>(("settings", settings), ("tableFull", utils.Event()), ("tableHasRoom", utils.Event())));
        wallet = TestUtils.Activate(utils.Component<Wallet>(("settings", settings)));
        decisions = TestUtils.Activate(utils.Component<SortingDecisions>(
            ("settings", settings), ("table", table), ("wallet", wallet),
            ("targetFound", utils.Event()), ("targetLost", utils.Event()), ("wrongPick", utils.Event())));
        panel = TestUtils.Activate(utils.Component<TablePanel>(("table", table), ("decisions", decisions)));
    }

    [TearDown]
    public void TearDown()
    {
        utils.DestroyAll();
        UiTestUtils.DestroyRootUi();
    }

    private CanvasGroup Buttons => panel.GetComponentInChildren<CanvasGroup>();
    private Text Status => UiTestUtils.Text(panel, "Status");

    private TrashItem PutOnTable()
    {
        TrashItem item = utils.Item(can, new Vector2(50f, 50f), 0f);
        TestUtils.Call(table, "OnTriggerEnter2D", item.GetComponent<Collider2D>());
        return item;
    }

    [UnityTest]
    public IEnumerator WithNothingSelected_TheButtonsAreDimmedAndOff()
    {
        yield return null;

        Assert.IsFalse(Buttons.interactable);
        Assert.Less(Buttons.alpha, 1f);
        Assert.AreEqual("Table: 0 of 14. Click an item on the table.", Status.text);
    }

    [UnityTest]
    public IEnumerator TheStatus_CountsTheItemsOnTheTable()
    {
        yield return null;

        PutOnTable();
        PutOnTable();

        Assert.AreEqual("Table: 2 of 14. Click an item on the table.", Status.text);
    }

    [UnityTest]
    public IEnumerator SelectingAnItem_TurnsTheButtonsOnAndShowsItsPrice()
    {
        yield return null;

        PutOnTable().OnClicked();

        Assert.IsTrue(Buttons.interactable);
        Assert.AreEqual(1f, Buttons.alpha);
        Assert.AreEqual("Table: 1 of 14. Selected: Tin can", Status.text);
        Assert.AreEqual("Sell $10", UiTestUtils.Text(UiTestUtils.Button(panel, "Sell Button"), "Label").text);
    }

    [UnityTest]
    public IEnumerator TheSellButton_SellsTheSelectedItem()
    {
        yield return null;
        PutOnTable().OnClicked();

        UiTestUtils.Button(panel, "Sell Button").onClick.Invoke();

        Assert.AreEqual(110, wallet.Money);
        Assert.AreEqual(0, table.ItemCount);
        Assert.IsFalse(Buttons.interactable, "Nothing is selected anymore");
        Assert.AreEqual("Sell", UiTestUtils.Text(UiTestUtils.Button(panel, "Sell Button"), "Label").text);
    }

    [UnityTest]
    public IEnumerator TheWasteButton_ThrowsTheSelectedItemAway()
    {
        yield return null;
        PutOnTable().OnClicked();

        UiTestUtils.Button(panel, "Waste Button").onClick.Invoke();

        Assert.AreEqual(100, wallet.Money);
        Assert.AreEqual(0, table.ItemCount);
    }

    [UnityTest]
    public IEnumerator TheTargetButton_MarksTheSelectedItem()
    {
        yield return null;
        PutOnTable().OnClicked();

        UiTestUtils.Button(panel, "This is the target! Button").onClick.Invoke();

        Assert.IsNull(decisions.Selected, "A wrong pick clears the selection");
        Assert.IsFalse(Buttons.interactable);
    }
}
