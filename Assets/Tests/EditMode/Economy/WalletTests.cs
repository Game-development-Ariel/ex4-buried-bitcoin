using System.Collections.Generic;
using NUnit.Framework;

public class WalletTests
{
    private TestUtils utils;
    private Wallet wallet;
    private List<int> changes;

    [SetUp]
    public void SetUp()
    {
        utils = new TestUtils();
        LandfillDaySettings settings = utils.Settings();
        settings.startMoney = 100;
        wallet = utils.Component<Wallet>(("settings", settings));
        TestUtils.Call(wallet, "Awake");
        changes = new List<int>();
        wallet.Changed += changes.Add;
    }

    [TearDown]
    public void TearDown()
    {
        utils.DestroyAll();
    }

    [Test]
    public void Awake_StartsWithTheMoneyFromTheSettings()
    {
        Assert.AreEqual(100, wallet.Money);
    }

    [TestCase(0)]
    [TestCase(1)]
    [TestCase(30)]
    public void Add_IncreasesTheMoneyAndSendsTheNewAmount(int amount)
    {
        wallet.Add(amount);

        Assert.AreEqual(100 + amount, wallet.Money);
        CollectionAssert.AreEqual(new[] { 100 + amount }, changes);
    }

    [TestCase(0, 100)]
    [TestCase(1, 99)]
    [TestCase(99, 1)]
    [TestCase(100, 0)]
    public void TrySpend_WithEnoughMoney_PaysAndReturnsTrue(int price, int left)
    {
        Assert.IsTrue(wallet.TrySpend(price));

        Assert.AreEqual(left, wallet.Money);
        CollectionAssert.AreEqual(new[] { left }, changes);
    }

    [TestCase(101)]
    [TestCase(1000)]
    public void TrySpend_WithoutEnoughMoney_TakesNothingAndReturnsFalse(int price)
    {
        Assert.IsFalse(wallet.TrySpend(price));

        Assert.AreEqual(100, wallet.Money);
        Assert.IsEmpty(changes, "Nothing changed, so Changed should not be sent");
    }

    [Test]
    public void TrySpend_ManyTimes_NeverGoesBelowZero()
    {
        for (int i = 0; i < 10; i++)
        {
            wallet.TrySpend(30);
            Assert.GreaterOrEqual(wallet.Money, 0);
        }
        Assert.AreEqual(10, wallet.Money);
    }

    [Test]
    public void TrySpend_TheSamePriceAgainAfterMoneyRanOut_Fails()
    {
        Assert.IsTrue(wallet.TrySpend(100));
        Assert.IsFalse(wallet.TrySpend(100));
        Assert.AreEqual(0, wallet.Money);
    }
}
