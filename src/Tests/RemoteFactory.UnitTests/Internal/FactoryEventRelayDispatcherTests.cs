using Neatoo.RemoteFactory;
using Neatoo.RemoteFactory.Internal;

namespace RemoteFactory.UnitTests.Internal;

public class FactoryEventRelayDispatcherTests
{
    private record TestRelayEvt(string Value) : FactoryEventBase;

    private class TestHandler
    {
        public List<TestRelayEvt> Received { get; } = new();

        public Task HandleFactoryEvent(TestRelayEvt factoryEvent)
        {
            Received.Add(factoryEvent);
            return Task.CompletedTask;
        }
    }

    private class ThrowingHandler
    {
        public Task HandleFactoryEvent(TestRelayEvt factoryEvent)
        {
            throw new InvalidOperationException("boom");
        }
    }

    /// <summary>
    /// Minimal serializer stub that returns a fixed event for Deserialize&lt;T&gt;.
    /// </summary>
    private class StubSerializer : INeatooJsonSerializer
    {
        private readonly object _returnValue;
        public StubSerializer(object returnValue) => _returnValue = returnValue;

        public SerializationFormat Format => SerializationFormat.Ordinal;
        public T? Deserialize<T>(string json) => (T)_returnValue;
        public object? Deserialize(string json, Type type) => throw new NotImplementedException();
        public T? DeserializeRemoteResponse<T>(RemoteResponseDto remoteResponse) => throw new NotImplementedException();
        public RemoteRequest DeserializeRemoteDelegateRequest(RemoteRequestDto remoteRequestDto) => throw new NotImplementedException();
        public string? Serialize(object? target) => throw new NotImplementedException();
        public string? Serialize(object? target, Type targetType) => throw new NotImplementedException();
        public RemoteRequestDto ToRemoteDelegateRequest(Type delegateType, params object?[]? parameters) => throw new NotImplementedException();
        public RemoteRequestDto ToRemoteDelegateRequest(Type delegateType, object saveTarget, params object?[]? parameters) => throw new NotImplementedException();
    }

    public FactoryEventRelayDispatcherTests()
    {
        FactoryEventRelayRegistry.Clear();
        // Register handler types for dispatch
        FactoryEventRelayRegistry.RegisterHandlerType(
            typeof(TestHandler),
            typeof(TestRelayEvt).FullName!,
            (handler, evt) => ((TestHandler)handler).HandleFactoryEvent((TestRelayEvt)evt),
            (json, serializer) => serializer.Deserialize<TestRelayEvt>(json)!);
        FactoryEventRelayRegistry.RegisterHandlerType(
            typeof(ThrowingHandler),
            typeof(TestRelayEvt).FullName!,
            (handler, evt) => ((ThrowingHandler)handler).HandleFactoryEvent((TestRelayEvt)evt),
            (json, serializer) => serializer.Deserialize<TestRelayEvt>(json)!);
    }

    [Fact]
    public async Task DispatchRelayedEvents_RegisteredHandler_ReceivesEvent()
    {
        var dispatcher = new FactoryEventRelayDispatcher();
        var handler = new TestHandler();
        dispatcher.Register(handler);

        var serializer = new StubSerializer(new TestRelayEvt("hello"));

        var events = new List<RelayedFactoryEvent>
        {
            new() { TypeFullName = typeof(TestRelayEvt).FullName!, Json = "{}" }
        };

        await dispatcher.DispatchRelayedEvents(events, serializer);

        Assert.Single(handler.Received);
        Assert.Equal("hello", handler.Received[0].Value);
    }

    [Fact]
    public async Task DispatchRelayedEvents_UnregisteredHandler_NoDelivery()
    {
        var dispatcher = new FactoryEventRelayDispatcher();
        var handler = new TestHandler();
        dispatcher.Register(handler);
        dispatcher.Unregister(handler);

        var serializer = new StubSerializer(new TestRelayEvt("hello"));

        var events = new List<RelayedFactoryEvent>
        {
            new() { TypeFullName = typeof(TestRelayEvt).FullName!, Json = "{}" }
        };

        await dispatcher.DispatchRelayedEvents(events, serializer);

        Assert.Empty(handler.Received);
    }

    [Fact]
    public async Task DispatchRelayedEvents_HandlerThrows_ExceptionSwallowed()
    {
        var dispatcher = new FactoryEventRelayDispatcher();
        var throwing = new ThrowingHandler();
        var good = new TestHandler();
        dispatcher.Register(throwing);
        dispatcher.Register(good);

        var serializer = new StubSerializer(new TestRelayEvt("test"));

        var events = new List<RelayedFactoryEvent>
        {
            new() { TypeFullName = typeof(TestRelayEvt).FullName!, Json = "{}" }
        };

        // Should not throw
        await dispatcher.DispatchRelayedEvents(events, serializer);

        // Good handler still received the event
        Assert.Single(good.Received);
    }

    [Fact]
    public async Task DispatchRelayedEvents_NoDispatcherRegistered_SilentDrop()
    {
        FactoryEventRelayRegistry.Clear(); // Remove all dispatch entries

        var dispatcher = new FactoryEventRelayDispatcher();
        var handler = new TestHandler();
        dispatcher.Register(handler);

        var serializer = new StubSerializer(new TestRelayEvt("test"));

        var events = new List<RelayedFactoryEvent>
        {
            new() { TypeFullName = "SomeUnknownType", Json = "{}" }
        };

        // Should not throw
        await dispatcher.DispatchRelayedEvents(events, serializer);

        Assert.Empty(handler.Received);
    }

    [Fact]
    public async Task DispatchRelayedEvents_MultipleHandlers_AllInvoked()
    {
        var dispatcher = new FactoryEventRelayDispatcher();
        var handler1 = new TestHandler();
        var handler2 = new TestHandler();
        dispatcher.Register(handler1);
        dispatcher.Register(handler2);

        var serializer = new StubSerializer(new TestRelayEvt("both"));

        var events = new List<RelayedFactoryEvent>
        {
            new() { TypeFullName = typeof(TestRelayEvt).FullName!, Json = "{}" }
        };

        await dispatcher.DispatchRelayedEvents(events, serializer);

        Assert.Single(handler1.Received);
        Assert.Single(handler2.Received);
    }
}
