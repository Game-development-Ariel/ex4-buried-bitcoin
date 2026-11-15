using NUnit.Framework;
using UnityEngine;

public class ConveyorBeltTests
{
    private TestUtils utils;
    private ConveyorBelt belt;
    private SurfaceEffector2D effector;
    private GameEvent tableFull;
    private GameEvent tableHasRoom;

    [SetUp]
    public void SetUp()
    {
        utils = new TestUtils();
        LandfillDaySettings settings = utils.Settings();
        settings.beltSpeed = 3f;
        tableFull = utils.Event();
        tableHasRoom = utils.Event();
        belt = utils.Component<ConveyorBelt>(("settings", settings), ("tableFull", tableFull), ("tableHasRoom", tableHasRoom));
        effector = belt.GetComponent<SurfaceEffector2D>();
        effector.speed = 0f;
        TestUtils.Call(belt, "Awake");
        TestUtils.Call(belt, "OnEnable");
    }

    [TearDown]
    public void TearDown()
    {
        TestUtils.Call(belt, "OnDisable");
        utils.DestroyAll();
    }

    [Test]
    public void TheBelt_NeedsASurfaceEffector()
    {
        Assert.IsNotNull(effector, "RequireComponent adds the effector");
    }

    [Test]
    public void Awake_RunsAtTheSpeedFromTheSettings()
    {
        Assert.AreEqual(3f, effector.speed);
    }

    [Test]
    public void TableFull_StopsTheBelt()
    {
        tableFull.Raise();

        Assert.AreEqual(0f, effector.speed);
    }

    [Test]
    public void TableHasRoom_StartsItAgain()
    {
        tableFull.Raise();
        tableHasRoom.Raise();

        Assert.AreEqual(3f, effector.speed);
    }

    [Test]
    public void Regression_TheBeltStopsWhenTheDayEndsAndTurnsItOff()
    {
        TestUtils.Call(belt, "OnDisable");

        Assert.AreEqual(0f, effector.speed, "A turned-off belt must not keep pushing the items");
    }

    [Test]
    public void ATurnedOffBelt_IgnoresTableHasRoom()
    {
        TestUtils.Call(belt, "OnDisable");

        tableHasRoom.Raise();

        Assert.AreEqual(0f, effector.speed);
    }
}
