---
title: Events
redirect_to: factory-events.md
---

# Events

> **This page has been removed in v1.5.0.**
>
> The `[Event]` method attribute API — including the `[Event]` attribute itself, `IEventTracker`, `EventTrackerHostedService`, `IEventScopeInitializer`, and `AddRemoteFactoryEventScopeInitializer` — was removed in v1.5.0. See the [v1.5.0 release notes](release-notes/v1.5.0.md) for the migration guide (manual `Task.Run` + `IServiceScopeFactory.CreateScope()` pattern with explicit copying of ambient state such as correlation ID and tenant context).
>
> For the factory-event mediator + client relay surface (`IFactoryEvents.Raise<T>`, `[FactoryEventHandler<T>]`, `IFactoryEventRelay`), which is unchanged and remains fully supported, see [Factory Events](factory-events.md).
