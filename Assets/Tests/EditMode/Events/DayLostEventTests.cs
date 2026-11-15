using System.Collections.Generic;
using NUnit.Framework;

public class DayLostEventTests
{
    private TestUtils utils;
    private DayLostEvent dayLost;
    private List<LossReason> received;

    [SetUp]
    public void SetUp()
    {
        utils = new TestUtils();
        dayLost = utils.Asset<DayLostEvent>();
        received = new List<LossReason>();
    }

    [TearDown]
    public void TearDown()
    {
        utils.DestroyAll();
    }

    private void Receive(LossReason reason)
    {
        received.Add(reason);
    }

    [Test]
    public void Raise_WithoutListeners_DoesNothing()
    {
        Assert.DoesNotThrow(() => dayLost.Raise(LossReason.TimeUp));
    }

    [TestCase(LossReason.TargetBuried)]
    [TestCase(LossReason.TimeUp)]
    public void Raise_GivesTheReasonToTheListener(LossReason reason)
    {
        dayLost.Subscribe(Receive);

        dayLost.Raise(reason);

        CollectionAssert.AreEqual(new[] { reason }, received);
    }

    [Test]
    public void Unsubscribe_StopsTheListener()
    {
        dayLost.Subscribe(Receive);
        dayLost.Unsubscribe(Receive);

        dayLost.Raise(LossReason.TimeUp);

        Assert.IsEmpty(received);
    }
}
