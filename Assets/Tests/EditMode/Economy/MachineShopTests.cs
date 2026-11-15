using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

public class MachineShopTests
{
    private TestUtils utils;
    private Wallet wallet;
    private MachineShop shop;
    private MachineSlot[] slots;
    private MachineData sieve;
    private List<MachineShop.Refusal> refusals;
    private int selectionChanges;

    [SetUp]
    public void SetUp()
    {
        utils = new TestUtils();
        LandfillDaySettings settings = utils.Settings();
        settings.startMoney = 100;
        wallet = utils.Component<Wallet>(("settings", settings));
        TestUtils.Call(wallet, "Awake");

        slots = new[] { NewSlot(), NewSlot() };
        shop = utils.Component<MachineShop>(("wallet", wallet), ("slots", slots));
        TestUtils.Call(shop, "OnEnable");

        sieve = utils.Machine("Sieve", 100, utils.NewObject("Sieve Prefab"));
        refusals = new List<MachineShop.Refusal>();
        shop.Refused += refusals.Add;
        selectionChanges = 0;
        shop.SelectionChanged += () => selectionChanges++;
    }

    [TearDown]
    public void TearDown()
    {
        utils.DestroyAll();
    }

    private MachineSlot NewSlot()
    {
        MachineSlot slot = utils.Component<MachineSlot>(("emptySign", utils.NewObject("Empty Sign")));
        slot.gameObject.SetActive(true);
        return slot;
    }

    [Test]
    public void Select_RemembersTheMachineAndSendsSelectionChanged()
    {
        shop.Select(sieve);

        Assert.AreSame(sieve, shop.Selected);
        Assert.AreEqual(1, selectionChanges);
    }

    [Test]
    public void Select_Null_ClearsTheSelection()
    {
        shop.Select(sieve);
        shop.Select(null);

        Assert.IsNull(shop.Selected);
        Assert.AreEqual(2, selectionChanges);
    }

    [Test]
    public void ClickOnEmptySlot_WithNothingPicked_IsRefusedWithNothingPicked()
    {
        slots[0].OnClicked();

        CollectionAssert.AreEqual(new[] { MachineShop.Refusal.NothingPicked }, refusals);
        Assert.IsTrue(slots[0].IsEmpty);
        Assert.AreEqual(100, wallet.Money);
    }

    [TestCase(100, 0)]
    [TestCase(40, 60)]
    [TestCase(1, 99)]
    public void ClickOnEmptySlot_WithEnoughMoney_InstallsTheMachineAndSpendsThePrice(int price, int left)
    {
        sieve.price = price;
        shop.Select(sieve);

        slots[0].OnClicked();

        Assert.AreSame(sieve, slots[0].Machine);
        Assert.AreEqual(left, wallet.Money);
        Assert.IsEmpty(refusals);
    }

    [Test]
    public void ClickOnEmptySlot_AfterBuying_ClearsTheSelectionSoTheNextClickDoesNotBuyAgain()
    {
        sieve.price = 10;
        shop.Select(sieve);

        slots[0].OnClicked();
        slots[1].OnClicked();

        Assert.IsNull(shop.Selected);
        Assert.IsTrue(slots[1].IsEmpty);
        Assert.AreEqual(90, wallet.Money);
        CollectionAssert.AreEqual(new[] { MachineShop.Refusal.NothingPicked }, refusals);
    }

    [TestCase(101)]
    [TestCase(500)]
    public void ClickOnEmptySlot_WithoutEnoughMoney_IsRefusedAndKeepsTheSelection(int price)
    {
        sieve.price = price;
        shop.Select(sieve);

        slots[0].OnClicked();

        CollectionAssert.AreEqual(new[] { MachineShop.Refusal.NotEnoughMoney }, refusals);
        Assert.IsTrue(slots[0].IsEmpty);
        Assert.AreEqual(100, wallet.Money);
        Assert.AreSame(sieve, shop.Selected, "The player can sell something and try the same machine again");
    }

    [TestCase(100, 50)]
    [TestCase(50, 25)]
    [TestCase(30, 15)]
    public void ClickOnBusySlot_SellsTheMachineForHalfThePrice(int price, int refund)
    {
        sieve.price = price;
        shop.Select(sieve);
        slots[0].OnClicked();
        int afterBuying = wallet.Money;

        slots[0].OnClicked();

        Assert.IsTrue(slots[0].IsEmpty);
        Assert.AreEqual(afterBuying + refund, wallet.Money);
    }

    [Test]
    public void ClickOnBusySlot_WithAnotherMachinePicked_SellsAndDoesNotBuy()
    {
        sieve.price = 60;
        shop.Select(sieve);
        slots[0].OnClicked();
        MachineData blower = utils.Machine("Blower", 20, utils.NewObject("Blower Prefab"));
        shop.Select(blower);

        slots[0].OnClicked();

        Assert.IsTrue(slots[0].IsEmpty);
        Assert.AreEqual(40 + 30, wallet.Money);
        Assert.AreSame(blower, shop.Selected);
    }

    [Test]
    public void ClickOnSlot_AfterTheShopIsDisabled_DoesNothing()
    {
        TestUtils.Call(shop, "OnDisable");
        shop.Select(sieve);

        slots[0].OnClicked();

        Assert.IsTrue(slots[0].IsEmpty);
        Assert.AreEqual(100, wallet.Money);
        Assert.IsEmpty(refusals);
    }

    [Test]
    public void TheShop_ListensToEverySlot()
    {
        shop.Select(sieve);
        sieve.price = 0;

        slots[1].OnClicked();

        Assert.IsFalse(slots[1].IsEmpty);
        Assert.IsTrue(slots[0].IsEmpty);
    }

    [Test]
    public void RefundShare_IsHalf()
    {
        Assert.AreEqual(0.5f, TestUtils.GetField<float>(shop, "refundShare"));
    }

    [Test]
    public void Install_PutsTheMachinePrefabInsideTheSlot()
    {
        shop.Select(sieve);

        slots[0].OnClicked();

        Assert.AreEqual(1, CountChildrenNamed(slots[0].transform, "Sieve Prefab(Clone)"));
    }

    private static int CountChildrenNamed(Transform parent, string name)
    {
        int count = 0;
        foreach (Transform child in parent)
        {
            if (child.name == name)
            {
                count++;
            }
        }
        return count;
    }
}
