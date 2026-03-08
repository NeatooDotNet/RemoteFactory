// =============================================================================
// DESIGN SOURCE OF TRUTH: [Execute] on Class Factory
// =============================================================================
//
// This file demonstrates [Execute] static methods on non-static [Factory] classes.
// This pattern keeps orchestration logic co-located with the aggregate it operates on.
//
// =============================================================================

using Neatoo.RemoteFactory;
using Design.Domain.FactoryPatterns;

namespace Design.Domain.FactoryPatterns;

/// <summary>
/// Demonstrates: [Execute] on a non-static [Factory] class.
///
/// Key points:
/// - [Execute] methods must be public static
/// - Must return the containing type's service type (the interface if available)
/// - [Service] parameters are injected by the generated factory
/// - The generated factory interface includes the Execute method
/// - Callers use the factory method, not the static method directly
/// </summary>
/// <remarks>
/// DESIGN DECISION: Execute on class factory generates factory interface methods
///
/// Unlike static factory [Execute] (which generates delegate types), class factory
/// [Execute] generates proper factory methods on the IXxxFactory interface. This
/// keeps the calling pattern consistent:
///   var result = await factory.StartForPatient(patientId);
///
/// DESIGN DECISION: Return type must be the containing type
///
/// This keeps the factory interface cohesive -- every method on IXxxFactory
/// deals with the same type. If you need to return a different type, use a
/// static class [Execute] instead.
/// </remarks>
[Factory]
public partial class ClassExecuteDemo
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;

    public ClassExecuteDemo() { }

    /// <summary>
    /// Standard Create method -- establishes that this is a class factory.
    /// </summary>
    [Remote, Create]
    internal Task Create(string name, [Service] IExampleService service)
    {
        Id = service.GenerateId();
        Name = name;
        return Task.CompletedTask;
    }

    /// <summary>
    /// Execute method on a non-static class.
    /// This is the key feature: [Execute] on a class factory.
    /// Returns the containing type (ClassExecuteDemo via the factory interface).
    /// </summary>
    /// <remarks>
    /// GENERATOR BEHAVIOR: For this method, the generator creates:
    /// - Interface method: IClassExecuteDemoFactory.RunCommand(string input)
    /// - Factory implementation with Local/Remote method pair
    /// - Delegate for remote execution
    /// </remarks>
    [Remote, Execute]
    public static async Task<ClassExecuteDemo> RunCommand(
        string input, [Service] IExampleService service)
    {
        var instance = new ClassExecuteDemo();
        instance.Id = service.GenerateId();
        instance.Name = $"Executed: {input}";
        return instance;
    }
}
