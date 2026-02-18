using Neatoo.RemoteFactory;
using RemoteFactory.IntegrationTests.Shared;

namespace RemoteFactory.IntegrationTests.TestTargets.Execute;

/// <summary>
/// [Execute] on a non-static [Factory] class with [Remote] for client-server round-trip.
/// </summary>
[Factory]
public partial class ClassExecRemote
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;

    public ClassExecRemote() { }

    [Remote, Create]
    public Task Create(string name, [Service] IService service)
    {
        Id = 1;
        Name = name;
        return Task.CompletedTask;
    }

    [Remote, Execute]
    public static Task<ClassExecRemote> Run(
        string input, [Service] IService service)
    {
        if (service == null)
            throw new InvalidOperationException("Service was not injected");
        var instance = new ClassExecRemote();
        instance.Id = 99;
        instance.Name = $"Remote: {input}";
        return Task.FromResult(instance);
    }
}

/// <summary>
/// [Execute] on a class factory with multiple services and no service parameters.
/// </summary>
[Factory]
public partial class ClassExecMulti
{
    public string Result { get; set; } = string.Empty;

    public ClassExecMulti() { }

    [Remote, Execute]
    public static Task<ClassExecMulti> RunSvc(
        string input,
        [Service] IService svc1,
        [Service] IService2 svc2)
    {
        if (svc1 == null)
            throw new InvalidOperationException("Service1 was not injected");
        if (svc2 == null)
            throw new InvalidOperationException("Service2 was not injected");
        var instance = new ClassExecMulti();
        instance.Result = $"{input}-{svc2.GetValue()}";
        return Task.FromResult(instance);
    }

    [Remote, Execute]
    public static Task<ClassExecMulti> RunNoSvc(string input)
    {
        var instance = new ClassExecMulti();
        instance.Result = $"NoSvc: {input}";
        return Task.FromResult(instance);
    }
}

/// <summary>
/// Interface for ClassExecRemoteWithInterface. When a [Factory] class implements
/// a matching I{ClassName} interface, the factory interface methods should
/// return the interface type instead of the concrete type.
/// </summary>
public interface IClassExecRemoteWithInterface
{
    int Id { get; set; }
    string Name { get; set; }
}

/// <summary>
/// [Execute] on a class factory that implements a matching interface, with [Remote]
/// for client-server round-trip. Verifies that the generated factory returns
/// IClassExecRemoteWithInterface through serialization.
/// </summary>
[Factory]
public partial class ClassExecRemoteWithInterface : IClassExecRemoteWithInterface
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;

    public ClassExecRemoteWithInterface() { }

    [Remote, Create]
    public Task Create(string name, [Service] IService service)
    {
        Id = 1;
        Name = name;
        return Task.CompletedTask;
    }

    [Remote, Execute]
    public static Task<ClassExecRemoteWithInterface> RunWithInterface(
        string input, [Service] IService service)
    {
        if (service == null)
            throw new InvalidOperationException("Service was not injected");
        var instance = new ClassExecRemoteWithInterface();
        instance.Id = 77;
        instance.Name = $"Interface: {input}";
        return Task.FromResult(instance);
    }
}
