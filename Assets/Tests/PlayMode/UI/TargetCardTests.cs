using NUnit.Framework;

public class TargetCardTests
{
    private TestUtils utils;

    [SetUp]
    public void SetUp()
    {
        utils = new TestUtils();
    }

    [TearDown]
    public void TearDown()
    {
        utils.DestroyAll();
    }

    [Test]
    public void TheCard_ShowsTheDescriptionOfTheDaysTarget()
    {
        TrashItemData target = utils.ItemData("Red toolbox");
        target.description = "A red toolbox. Big, heavy, metal.";

        TargetCard card = TestUtils.Activate(utils.Component<TargetCard>(("settings", utils.Settings(target))));

        Assert.AreEqual("A red toolbox. Big, heavy, metal.", UiTestUtils.Text(card, "Description").text);
        Assert.AreEqual("TODAY'S TARGET", UiTestUtils.Text(card, "Label").text);
    }
}
