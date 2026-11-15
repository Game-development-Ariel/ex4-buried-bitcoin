using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class WastePileTests
{
    private TestUtils utils;
    private TrashItemData target;
    private GameEvent targetLost;
    private int lost;

    [SetUp]
    public void SetUp()
    {
        utils = new TestUtils();
        target = utils.ItemData("Red toolbox", 0, false, 3f);
        targetLost = utils.Event();
        lost = 0;
        targetLost.Subscribe(() => lost++);

        // WastePile needs a collider, and it must be there before the pile is added.
        GameObject ground = utils.NewObject("Waste Pile", false);
        ground.transform.position = new Vector3(0f, -2f, 0f);
        ground.transform.localScale = new Vector3(10f, 1f, 1f);
        ground.AddComponent<BoxCollider2D>();
        WastePile pile = ground.AddComponent<WastePile>();
        TestUtils.SetField(pile, "settings", utils.Settings(target));
        TestUtils.SetField(pile, "targetLost", targetLost);
        ground.SetActive(true);
    }

    [TearDown]
    public void TearDown()
    {
        utils.DestroyAll();
    }

    [UnityTest]
    public IEnumerator AnItemThatHitsThePile_IsBuried()
    {
        TrashItem item = utils.Item(utils.ItemData("Can"), new Vector2(0f, 0f));

        yield return LandfillScene.WaitUntil(() => item == null, 2f, "the item to be buried");

        Assert.AreEqual(0, lost);
    }

    [UnityTest]
    public IEnumerator TheTargetHittingThePile_LosesTheTargetOnce()
    {
        TrashItem item = utils.Item(target, new Vector2(0f, 0f));

        yield return LandfillScene.WaitUntil(() => item == null, 2f, "the target to be buried");
        yield return new WaitForFixedUpdate();

        Assert.AreEqual(1, lost);
    }

    [UnityTest]
    public IEnumerator SomethingThatIsNotAnItem_StaysOnThePile()
    {
        GameObject box = utils.NewObject("Box");
        box.transform.position = Vector3.zero;
        box.AddComponent<Rigidbody2D>();
        box.AddComponent<BoxCollider2D>();

        yield return new WaitForSeconds(0.6f);

        Assert.IsTrue(box != null);
        Assert.AreEqual(-1f, box.transform.position.y, 0.1f, "It lies on top of the pile");
    }
}
