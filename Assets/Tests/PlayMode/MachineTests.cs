using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

/// <summary>
/// Buys machines in the real scene and puts chosen items over them, so the physics is tested
/// with the real belt, prefabs and settings.
/// </summary>
public class MachineTests
{
    private LandfillScene game;
    private MachineData sieve;
    private MachineData blower;
    private List<MachineShop.Refusal> refusals;

    [UnitySetUp]
    public IEnumerator LoadTheScene()
    {
        game = new LandfillScene();
        yield return LandfillScene.Load(game);
        MachineData[] machines = TestUtils.GetField<MachineData[]>(Object.FindAnyObjectByType<ShopBar>(), "machines");
        sieve = machines.Single(machine => machine.prefab.GetComponent<Sieve>() != null);
        blower = machines.Single(machine => machine.prefab.GetComponent<AreaEffector2D>() != null);
        refusals = new List<MachineShop.Refusal>();
        game.Shop.Refused += refusals.Add;
    }

    [UnityTearDown]
    public IEnumerator TearDown()
    {
        game.Shop.Refused -= refusals.Add;
        yield return LandfillScene.Unload();
    }

    private void Buy(MachineData machine, MachineSlot slot)
    {
        game.Shop.Select(machine);
        slot.OnClicked();
    }

    /// <summary>A point just above the belt, over the middle of the slot.</summary>
    private static Vector2 Over(MachineSlot slot)
    {
        return new Vector2(slot.transform.position.x, 1.3f);
    }

    [Test]
    public void BuyingASieve_SpendsItsPriceAndPutsItInTheSlot()
    {
        Buy(sieve, game.Slots[0]);

        Assert.AreEqual(game.Settings.startMoney - sieve.price, game.Wallet.Money);
        Assert.AreSame(sieve, game.Slots[0].Machine);
        Assert.IsNotNull(game.Slots[0].GetComponentInChildren<Sieve>());
        Assert.IsNull(game.Shop.Selected);
    }

    [UnityTest]
    public IEnumerator TheSieve_DropsASmallItemThroughTheBelt()
    {
        Buy(sieve, game.Slots[0]);
        TrashItem dirt = game.Spawn(game.ItemFromTrucks(item => item.isSmall), Over(game.Slots[0]));

        yield return LandfillScene.WaitUntil(() => dirt == null || dirt.transform.position.y < 0.3f, 2f, "the small item to fall through");

        Assert.IsTrue(dirt == null || dirt.transform.position.y < 0.3f);
    }

    [UnityTest]
    public IEnumerator TheSieve_LetsABigItemRideOn()
    {
        Buy(sieve, game.Slots[0]);
        TrashItemData big = game.ItemFromTrucks(item => !item.isSmall && item != game.Settings.target);
        TrashItem item = game.Spawn(big, Over(game.Slots[0]));

        yield return new WaitForSeconds(0.8f);

        Assert.IsTrue(item != null);
        Assert.Greater(item.transform.position.y, 0.95f, "Still on top of the belt");
    }

    [UnityTest]
    public IEnumerator WithoutASieve_ASmallItemStaysOnTheBelt()
    {
        TrashItem dirt = game.Spawn(game.ItemFromTrucks(item => item.isSmall), Over(game.Slots[0]));

        yield return new WaitForSeconds(0.8f);

        Assert.Greater(dirt.transform.position.y, 0.95f);
    }

    [UnityTest]
    public IEnumerator TheBlower_LiftsALightItemIntoTheHood()
    {
        Buy(blower, game.Slots[1]);
        TrashItemData lightest = game.ItemFromTrucks(item => true, item => item.mass);
        TrashItem bag = game.Spawn(lightest, Over(game.Slots[1]));

        yield return LandfillScene.WaitUntil(() => bag == null, 3f, "the light item to be blown into the waste hood");

        Assert.AreEqual(game.Settings.startMoney - blower.price, game.Wallet.Money);
    }

    [UnityTest]
    public IEnumerator TheBlower_DoesNotLiftAHeavyItem()
    {
        Buy(blower, game.Slots[1]);
        TrashItemData heaviest = game.ItemFromTrucks(item => item != game.Settings.target, item => -item.mass);
        TrashItem item = game.Spawn(heaviest, Over(game.Slots[1]));

        float highest = 0f;
        for (float t = 0f; t < 1f; t += Time.deltaTime)
        {
            yield return null;
            highest = Mathf.Max(highest, item.transform.position.y);
        }

        Assert.Less(highest, 1.6f);
    }

    [UnityTest]
    public IEnumerator SellingAMachine_RefundsHalfAndRemovesIt()
    {
        Buy(sieve, game.Slots[0]);

        game.Slots[0].OnClicked();
        yield return null;

        Assert.AreEqual(game.Settings.startMoney - sieve.price + sieve.price / 2, game.Wallet.Money);
        Assert.IsTrue(game.Slots[0].IsEmpty);
        Assert.IsNull(game.Slots[0].GetComponentInChildren<Sieve>());
    }

    [Test]
    public void AfterBuyingASieve_ThereIsNotEnoughMoneyForTheBlower()
    {
        Assume.That(game.Settings.startMoney - sieve.price, Is.LessThan(blower.price), "Day 1 money buys one machine");
        Buy(sieve, game.Slots[0]);

        Buy(blower, game.Slots[1]);

        CollectionAssert.AreEqual(new[] { MachineShop.Refusal.NotEnoughMoney }, refusals);
        Assert.IsTrue(game.Slots[1].IsEmpty);
    }
}
