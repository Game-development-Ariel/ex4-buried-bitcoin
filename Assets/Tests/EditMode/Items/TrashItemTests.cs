using NUnit.Framework;
using UnityEngine;

public class TrashItemTests
{
    private TestUtils utils;
    private TrashItemData scrap;

    [SetUp]
    public void SetUp()
    {
        utils = new TestUtils();
        scrap = utils.ItemData("Copper scrap", 30, false, 2f, 0.6f);
        scrap.color = new Color(0.8f, 0.5f, 0.2f);
    }

    [TearDown]
    public void TearDown()
    {
        utils.DestroyAll();
    }

    [Test]
    public void Setup_TakesTheNameSizeColorAndMassFromTheData()
    {
        TrashItem item = utils.Item(scrap);

        Assert.AreSame(scrap, item.Data);
        Assert.AreEqual("Copper scrap", item.name);
        Assert.AreEqual(Vector3.one * 0.6f, item.transform.localScale);
        Assert.AreEqual(scrap.color, item.GetComponent<SpriteRenderer>().color);
        Assert.AreEqual(2f, item.GetComponent<Rigidbody2D>().mass, 0.0001f);
    }

    [Test]
    public void Setup_StartsWithoutTheHighlight()
    {
        TrashItem item = utils.Item();
        TestUtils.GetField<GameObject>(item, "outline").SetActive(true);

        item.Setup(scrap);

        Assert.IsFalse(TestUtils.GetField<GameObject>(item, "outline").activeSelf);
    }

    [TestCase(true)]
    [TestCase(false)]
    public void SetHighlighted_ShowsOrHidesTheOutline(bool highlighted)
    {
        TrashItem item = utils.Item(scrap);

        item.SetHighlighted(highlighted);

        Assert.AreEqual(highlighted, TestUtils.GetField<GameObject>(item, "outline").activeSelf);
    }

    [Test]
    public void OnClicked_SendsClickedWithTheItem()
    {
        TrashItem item = utils.Item(scrap);
        TrashItem received = null;
        item.Clicked += clicked => received = clicked;

        item.OnClicked();

        Assert.AreSame(item, received);
    }

    [Test]
    public void OnClicked_WithoutListeners_DoesNothing()
    {
        TrashItem item = utils.Item(scrap);

        Assert.DoesNotThrow(item.OnClicked);
    }

    [Test]
    public void TrashItem_IsClickable()
    {
        Assert.IsInstanceOf<IClickable>(utils.Item(scrap));
    }
}
