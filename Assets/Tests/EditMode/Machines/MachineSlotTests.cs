using NUnit.Framework;
using UnityEngine;

public class MachineSlotTests
{
    private TestUtils utils;
    private MachineSlot slot;
    private GameObject emptySign;
    private BoxCollider2D belt;
    private MachineData machine;

    [SetUp]
    public void SetUp()
    {
        utils = new TestUtils();
        emptySign = utils.NewObject("Empty Sign");
        belt = utils.NewObject("Belt").AddComponent<BoxCollider2D>();
        slot = utils.Component<MachineSlot>(("emptySign", emptySign), ("belt", belt));
        slot.gameObject.SetActive(true);
        machine = utils.Machine("Blower", 50, utils.NewObject("Blower Prefab"));
    }

    [TearDown]
    public void TearDown()
    {
        utils.DestroyAll();
    }

    [Test]
    public void ANewSlot_IsEmptyAndShowsTheSign()
    {
        Assert.IsTrue(slot.IsEmpty);
        Assert.IsNull(slot.Machine);
        Assert.IsTrue(emptySign.activeSelf);
    }

    [Test]
    public void Belt_IsTheBeltUnderTheSlot()
    {
        Assert.AreSame(belt, slot.Belt);
    }

    [Test]
    public void Install_CreatesTheMachineInsideTheSlotAndHidesTheSign()
    {
        slot.Install(machine);

        Assert.IsFalse(slot.IsEmpty);
        Assert.AreSame(machine, slot.Machine);
        Assert.IsFalse(emptySign.activeSelf);
        Assert.AreEqual(1, slot.transform.childCount);
        Assert.AreEqual("Blower Prefab(Clone)", slot.transform.GetChild(0).name);
    }

    [Test]
    public void Clear_EmptiesTheSlotAndShowsTheSign()
    {
        slot.Install(machine);

        EditModeLog.ExpectDestroy();
        slot.Clear();

        Assert.IsTrue(slot.IsEmpty);
        Assert.IsTrue(emptySign.activeSelf);
    }

    [Test]
    public void OnClicked_SendsClickedWithTheSlot()
    {
        MachineSlot received = null;
        slot.Clicked += clicked => received = clicked;

        slot.OnClicked();

        Assert.AreSame(slot, received);
    }

    [Test]
    public void OnClicked_WithoutListeners_DoesNothing()
    {
        Assert.DoesNotThrow(slot.OnClicked);
    }
}
