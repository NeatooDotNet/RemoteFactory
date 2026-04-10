using Neatoo.RemoteFactory;

namespace Person.DomainModel;

public record PersonCreatedEvent(int Id) : FactoryEventBase;
public record PersonUpdatedEvent(int Id) : FactoryEventBase;
public record PersonDeletedEvent(int Id) : FactoryEventBase;
