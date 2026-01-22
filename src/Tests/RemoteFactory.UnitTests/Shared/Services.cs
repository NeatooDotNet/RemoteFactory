namespace RemoteFactory.UnitTests.Shared;

/// <summary>
/// Test service interface for verifying [Service] parameter injection.
/// </summary>
public interface IService
{
}

/// <summary>
/// Default implementation of IService for testing.
/// </summary>
public class Service : IService
{
}

/// <summary>
/// Second service interface for testing multiple service injection.
/// </summary>
public interface IService2
{
    int GetValue();
}

/// <summary>
/// Implementation of IService2.
/// </summary>
public class Service2 : IService2
{
    public int GetValue() => 42;
}

/// <summary>
/// Third service interface for testing multiple service injection.
/// </summary>
public interface IService3
{
    string GetName();
}

/// <summary>
/// Implementation of IService3.
/// </summary>
public class Service3 : IService3
{
    public string GetName() => "Service3";
}

/// <summary>
/// Secondary service interface for multiple service injection tests.
/// </summary>
public interface ISecondaryService
{
    string Name { get; }
}

/// <summary>
/// Default implementation of ISecondaryService.
/// </summary>
public class SecondaryService : ISecondaryService
{
    public string Name => "SecondaryService";
}

/// <summary>
/// Server-only service interface - only available in Server/Logical modes.
/// Used for testing scenarios where a service is only resolved on the server side.
/// </summary>
public interface IServerOnlyService
{
    string ServerOnlyValue { get; }
}

/// <summary>
/// Server-only service implementation.
/// </summary>
public class ServerOnlyService : IServerOnlyService
{
    public string ServerOnlyValue => "ServerOnly";
}
