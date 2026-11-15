using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

public class TruckLoadTests
{
    private TestUtils utils;
    private TrashItemData dirt;
    private TrashItemData can;
    private TrashItemData toolbox;

    [SetUp]
    public void SetUp()
    {
        utils = new TestUtils();
        dirt = utils.ItemData("Dirt");
        can = utils.ItemData("Can");
        toolbox = utils.ItemData("Toolbox");
        UnityEngine.Random.InitState(1234);
    }

    [TearDown]
    public void TearDown()
    {
        utils.DestroyAll();
    }

    [Test]
    public void ShuffledItems_OfAnEmptyTruck_IsEmpty()
    {
        var load = new TruckLoad { entries = new TruckLoad.Entry[0] };

        Assert.IsEmpty(load.ShuffledItems());
    }

    [Test]
    public void ShuffledItems_WithCountZero_LeavesTheItemOut()
    {
        TruckLoad load = TestUtils.Load((dirt, 0), (can, 2));

        List<TrashItemData> items = load.ShuffledItems();

        CollectionAssert.DoesNotContain(items, dirt);
        Assert.AreEqual(2, items.Count);
    }

    [TestCase(1, 1, 1)]
    [TestCase(9, 2, 1)]
    [TestCase(13, 4, 0)]
    public void ShuffledItems_HasEveryItemExactlyCountTimes(int dirtCount, int canCount, int toolboxCount)
    {
        TruckLoad load = TestUtils.Load((dirt, dirtCount), (can, canCount), (toolbox, toolboxCount));

        List<TrashItemData> items = load.ShuffledItems();

        Assert.AreEqual(dirtCount + canCount + toolboxCount, items.Count);
        Assert.AreEqual(dirtCount, items.Count(i => i == dirt));
        Assert.AreEqual(canCount, items.Count(i => i == can));
        Assert.AreEqual(toolboxCount, items.Count(i => i == toolbox));
    }

    [Test]
    public void ShuffledItems_TheSameItemInTwoEntries_IsCountedFromBoth()
    {
        TruckLoad load = TestUtils.Load((dirt, 2), (dirt, 3));

        Assert.AreEqual(5, load.ShuffledItems().Count(i => i == dirt));
    }

    [Test]
    public void Regression_TheTargetIsNotAlwaysInTheSamePlace()
    {
        TruckLoad load = TestUtils.Load((dirt, 10), (toolbox, 1));
        var places = new HashSet<int>();

        for (int seed = 0; seed < 30; seed++)
        {
            UnityEngine.Random.InitState(seed);
            places.Add(load.ShuffledItems().IndexOf(toolbox));
        }

        Assert.Greater(places.Count, 3, "The shuffle should move the target around");
    }

    [Test]
    public void ShuffledItems_WithTheSameSeed_GivesTheSameOrder()
    {
        TruckLoad load = TestUtils.Load((dirt, 5), (can, 5), (toolbox, 1));

        UnityEngine.Random.InitState(42);
        List<TrashItemData> first = load.ShuffledItems();
        UnityEngine.Random.InitState(42);
        List<TrashItemData> second = load.ShuffledItems();

        CollectionAssert.AreEqual(first, second);
    }

    [Test]
    public void ShuffledItems_ReturnsANewListEveryTime()
    {
        TruckLoad load = TestUtils.Load((dirt, 2));

        Assert.AreNotSame(load.ShuffledItems(), load.ShuffledItems());
    }
}
