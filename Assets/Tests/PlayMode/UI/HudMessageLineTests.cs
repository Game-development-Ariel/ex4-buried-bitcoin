using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

public class HudMessageLineTests
{
    private TestUtils utils;
    private Wallet wallet;
    private MachineShop shop;
    private MachineSlot slot;
    private GameEvent wrongPick;
    private GameEvent tableFull;
    private HudMessageLine line;

    [SetUp]
    public void SetUp()
    {
        utils = new TestUtils();
        LandfillDaySettings settings = utils.Settings();
        settings.startMoney = 10;
        settings.wrongPickPenaltySeconds = 20f;
        wallet = TestUtils.Activate(utils.Component<Wallet>(("settings", settings)));
        slot = TestUtils.Activate(utils.Component<MachineSlot>(("emptySign", utils.NewObject("Sign"))));
        shop = TestUtils.Activate(utils.Component<MachineShop>(("wallet", wallet), ("slots", new[] { slot })));
        wrongPick = utils.Event();
        tableFull = utils.Event();
        line = TestUtils.Activate(utils.Component<HudMessageLine>(("settings", settings), ("shop", shop), ("wrongPick", wrongPick), ("tableFull", tableFull)));
    }

    [TearDown]
    public void TearDown()
    {
        Time.timeScale = 1f;
        utils.DestroyAll();
    }

    private Text Message => UiTestUtils.Text(line, "Message");

    [Test]
    public void StartsEmpty()
    {
        Assert.AreEqual("", Message.text);
    }

    [Test]
    public void Refusal_NothingPicked_AsksToPickAMachine()
    {
        slot.OnClicked();

        Assert.AreEqual("Pick a machine in the shop first.", Message.text);
    }

    [Test]
    public void Refusal_NotEnoughMoney_NamesTheMachine()
    {
        shop.Select(utils.Machine("Sieve", 100, utils.NewObject("Prefab")));

        slot.OnClicked();

        Assert.AreEqual("Not enough money for the Sieve. Sell something first.", Message.text);
    }

    [Test]
    public void WrongPick_TellsThePenalty()
    {
        wrongPick.Raise();

        Assert.AreEqual("That's not it. Compare with the target card. (-20 seconds)", Message.text);
    }

    [Test]
    public void TableFull_SaysWhyTheBeltStopped()
    {
        tableFull.Raise();

        Assert.AreEqual("The table is full, so the belt stopped. Sort some items.", Message.text);
    }

    [Test]
    public void ANewMessage_ReplacesTheOldOne()
    {
        tableFull.Raise();
        wrongPick.Raise();

        StringAssert.StartsWith("That's not it.", Message.text);
    }

    [UnityTest]
    public IEnumerator TheMessage_DisappearsAfterAFewSeconds()
    {
        Time.timeScale = 10f;
        tableFull.Raise();

        yield return new WaitForSeconds(2f);
        Assert.AreNotEqual("", Message.text, "Still shown after 2 seconds");
        yield return new WaitForSeconds(2f);
        Assert.AreEqual("", Message.text);
    }

    [UnityTest]
    public IEnumerator ANewMessage_GetsTheFullTimeAgain()
    {
        Time.timeScale = 10f;
        tableFull.Raise();
        yield return new WaitForSeconds(3f);

        wrongPick.Raise();
        yield return new WaitForSeconds(1f);

        Assert.AreNotEqual("", Message.text, "The first message's timer must not clear the second message");
    }
}
