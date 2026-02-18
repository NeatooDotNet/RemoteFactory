using Neatoo.RemoteFactory;
using RemoteFactory.UnitTests.Shared;

namespace RemoteFactory.UnitTests.TestTargets.Execute;

/// <summary>
/// [Execute] on a non-static [Factory] class with a [Service] parameter.
/// Verifies that class factory Execute generates factory interface methods.
/// </summary>
[Factory]
public partial class ClassExecTarget
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;

    public ClassExecTarget() { }

    [Create]
    public Task Create(string name)
    {
        Name = name;
        return Task.CompletedTask;
    }

    [Remote, Execute]
    public static async Task<ClassExecTarget> RunWithSvc(
        string input, [Service] IService service)
    {
        if (service == null)
            throw new InvalidOperationException("Service was not injected");
        var instance = new ClassExecTarget();
        instance.Name = $"Executed: {input}";
        return instance;
    }
}

/// <summary>
/// [Execute] on a class factory with no service parameters.
/// </summary>
[Factory]
public partial class ClassExecNoSvc
{
    public string Value { get; set; } = string.Empty;

    public ClassExecNoSvc() { }

    [Create]
    public Task Create(string value)
    {
        Value = value;
        return Task.CompletedTask;
    }

    [Remote, Execute]
    public static Task<ClassExecNoSvc> RunNoService(string input)
    {
        var instance = new ClassExecNoSvc();
        instance.Value = $"NoSvc: {input}";
        return Task.FromResult(instance);
    }
}

/// <summary>
/// [Execute] on a class factory with multiple [Service] parameters.
/// </summary>
[Factory]
public partial class ClassExecMultiSvc
{
    public string Result { get; set; } = string.Empty;

    public ClassExecMultiSvc() { }

    [Remote, Execute]
    public static Task<ClassExecMultiSvc> RunMulti(
        string input,
        [Service] IService svc1,
        [Service] IService2 svc2)
    {
        if (svc1 == null)
            throw new InvalidOperationException("Service1 was not injected");
        if (svc2 == null)
            throw new InvalidOperationException("Service2 was not injected");
        var instance = new ClassExecMultiSvc();
        instance.Result = $"{input}-{svc2.GetValue()}";
        return Task.FromResult(instance);
    }
}
