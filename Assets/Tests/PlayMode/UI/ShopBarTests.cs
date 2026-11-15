using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

public class ShopBarTests
{
    private TestUtils utils;
    private MachineShop shop;
    private MachineData sieve;
    private MachineData blower;
    private ShopBar bar;

    [SetUp]
    public void SetUp()
    {
        utils = new TestUtils();
        Wallet wallet = TestUtils.Activate(utils.Component<Wallet>(("settings", utils.Settings())));
        shop = TestUtils.Activate(utils.Component<MachineShop>(("wallet", wallet), ("slots", new MachineSlot[0])));
        sieve = utils.Machine("Sieve", 100, utils.NewObject("Sieve Prefab"));
        blower = utils.Machine("Blower", 50, utils.NewObject("Blower Prefab"));
        bar = TestUtils.Activate(utils.Component<ShopBar>(("shop", shop), ("machines", new[] { sieve, blower })));
    }

    [TearDown]
    public void TearDown()
    {
        utils.DestroyAll();
        UiTestUtils.DestroyRootUi();
    }

    private Button[] Buttons => bar.GetComponentsInChildren<Button>();

    [Test]
    public void TheBar_HasOneButtonPerMachine_WithItsNameAndPrice()
    {
        string[] labels = Buttons.Select(button => button.GetComponentInChildren<Text>().text).ToArray();

        CollectionAssert.AreEqual(new[] { "Sieve  $100", "Blower  $50" }, labels);
    }

    [Test]
    public void EveryButton_ShowsTheMachinesDescription()
    {
        CollectionAssert.AreEqual(new[] { sieve.description, blower.description }, Buttons.Select(button => UiTestUtils.Text(button, "Description").text).ToArray());
    }

    [TestCase(0)]
    [TestCase(1)]
    public void ClickingAButton_PicksThatMachine(int index)
    {
        Buttons[index].onClick.Invoke();

        Assert.AreSame(index == 0 ? sieve : blower, shop.Selected);
    }

    [Test]
    public void ThePickedMachine_IsMarked_AndTheOthersAreNot()
    {
        Buttons[1].onClick.Invoke();

        Assert.AreNotEqual(Color.white, Buttons[1].GetComponent<Image>().color);
        Assert.AreEqual(Color.white, Buttons[0].GetComponent<Image>().color);
    }

    [Test]
    public void AfterBuying_NoButtonIsMarked()
    {
        Buttons[0].onClick.Invoke();

        shop.Select(null);

        Assert.IsTrue(Buttons.All(button => button.GetComponent<Image>().color == Color.white));
    }

    [Test]
    public void AnEmptyShop_HasNoButtons()
    {
        ShopBar empty = TestUtils.Activate(utils.Component<ShopBar>(("shop", shop), ("machines", new MachineData[0])));

        Assert.IsEmpty(empty.GetComponentsInChildren<Button>());
    }

    [Test]
    public void TheShop_NeedsAnEventSystemForItsButtons()
    {
        Assert.IsNotNull(Object.FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>());
    }
}
