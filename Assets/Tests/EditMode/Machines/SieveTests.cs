using NUnit.Framework;
using UnityEngine;

public class SieveTests
{
    private TestUtils utils;
    private BoxCollider2D belt;
    private Sieve sieve;

    [SetUp]
    public void SetUp()
    {
        utils = new TestUtils();
        belt = utils.NewObject("Belt").AddComponent<BoxCollider2D>();
        MachineSlot slot = utils.Component<MachineSlot>(("belt", belt), ("emptySign", utils.NewObject("Sign")));
        slot.gameObject.SetActive(true);

        var sieveObject = new GameObject("Sieve");
        sieveObject.transform.SetParent(slot.transform, false);
        sieveObject.AddComponent<BoxCollider2D>().isTrigger = true;
        sieve = sieveObject.AddComponent<Sieve>();
        TestUtils.Call(sieve, "Awake");
    }

    [TearDown]
    public void TearDown()
    {
        utils.DestroyAll();
    }

    private Collider2D Pass(TrashItemData data)
    {
        Collider2D collider = utils.Item(data).GetComponent<Collider2D>();
        TestUtils.Call(sieve, "OnTriggerEnter2D", collider);
        return collider;
    }

    [Test]
    public void Awake_FindsTheBeltOfItsSlot()
    {
        Assert.AreSame(belt, TestUtils.GetField<Collider2D>(sieve, "belt"));
    }

    [Test]
    public void ASmallItem_StopsTouchingTheBeltAndFallsThrough()
    {
        Collider2D dirt = Pass(utils.ItemData("Dirt", 0, true));

        Assert.IsTrue(Physics2D.GetIgnoreCollision(dirt, belt));
    }

    [Test]
    public void ABigItem_StaysOnTheBelt()
    {
        Collider2D bottle = Pass(utils.ItemData("Bottle", 0, false));

        Assert.IsFalse(Physics2D.GetIgnoreCollision(bottle, belt));
    }

    [Test]
    public void SomethingThatIsNotAnItem_IsIgnored()
    {
        BoxCollider2D wall = utils.NewObject("Wall").AddComponent<BoxCollider2D>();

        Assert.DoesNotThrow(() => TestUtils.Call(sieve, "OnTriggerEnter2D", wall));
        Assert.IsFalse(Physics2D.GetIgnoreCollision(wall, belt));
    }
}
