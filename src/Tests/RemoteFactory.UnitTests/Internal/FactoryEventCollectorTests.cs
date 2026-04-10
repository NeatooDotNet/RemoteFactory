using Neatoo.RemoteFactory;
using Neatoo.RemoteFactory.Internal;

namespace RemoteFactory.UnitTests.Internal;

public class FactoryEventCollectorTests
{
    private record TestEvent(string Value) : FactoryEventBase;
    private record TestEventB(int Number) : FactoryEventBase;

    [Fact]
    public void Collect_SingleEvent_ReturnsIt()
    {
        var collector = new FactoryEventCollector();
        var evt = new TestEvent("hello");
        collector.Collect(evt);

        var events = collector.GetCollectedEvents();
        Assert.Single(events);
        Assert.Same(evt, events[0]);
    }

    [Fact]
    public void Collect_MultipleEvents_PreservesOrder()
    {
        var collector = new FactoryEventCollector();
        var evt1 = new TestEvent("first");
        var evt2 = new TestEventB(42);
        var evt3 = new TestEvent("third");

        collector.Collect(evt1);
        collector.Collect(evt2);
        collector.Collect(evt3);

        var events = collector.GetCollectedEvents();
        Assert.Equal(3, events.Count);
        Assert.Same(evt1, events[0]);
        Assert.Same(evt2, events[1]);
        Assert.Same(evt3, events[2]);
    }

    [Fact]
    public void GetCollectedEvents_NoEvents_ReturnsEmpty()
    {
        var collector = new FactoryEventCollector();
        var events = collector.GetCollectedEvents();
        Assert.Empty(events);
    }
}
