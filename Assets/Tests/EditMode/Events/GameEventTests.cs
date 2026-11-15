using NUnit.Framework;

public class GameEventTests
{
    private TestUtils utils;
    private GameEvent gameEvent;
    private int calls;

    [SetUp]
    public void SetUp()
    {
        utils = new TestUtils();
        gameEvent = utils.Event();
        calls = 0;
    }

    [TearDown]
    public void TearDown()
    {
        utils.DestroyAll();
    }

    private void Count()
    {
        calls++;
    }

    [Test]
    public void Raise_WithoutListeners_DoesNothing()
    {
        Assert.DoesNotThrow(() => gameEvent.Raise());
    }

    [Test]
    public void Raise_CallsEverySubscribedListenerOnce()
    {
        int other = 0;
        gameEvent.Subscribe(Count);
        gameEvent.Subscribe(() => other++);

        gameEvent.Raise();

        Assert.AreEqual(1, calls);
        Assert.AreEqual(1, other);
    }

    [Test]
    public void Raise_TwiceCallsTheListenerTwice()
    {
        gameEvent.Subscribe(Count);

        gameEvent.Raise();
        gameEvent.Raise();

        Assert.AreEqual(2, calls);
    }

    [Test]
    public void Unsubscribe_StopsTheListener()
    {
        gameEvent.Subscribe(Count);
        gameEvent.Unsubscribe(Count);

        gameEvent.Raise();

        Assert.AreEqual(0, calls);
    }

    [Test]
    public void Unsubscribe_OfAListenerThatNeverSubscribed_IsHarmless()
    {
        Assert.DoesNotThrow(() => gameEvent.Unsubscribe(Count));
    }

    [Test]
    public void TwoAssets_AreSeparateEvents()
    {
        GameEvent other = utils.Event();
        gameEvent.Subscribe(Count);

        other.Raise();

        Assert.AreEqual(0, calls);
    }
}
