using Neatoo.RemoteFactory;

namespace RemoteFactory.UnitTests.BugScenarios;

#region Test Target Classes

/// <summary>
/// Outer container class for nested factory inheritance test.
/// </summary>
public class HasBaseClassFactoryAttributeTestsBase
{
    /// <summary>
    /// Base class with [Factory] attribute.
    /// </summary>
    [Factory]
    public class BaseClass
    {
        public int BaseProperty { get; set; }
    }

    /// <summary>
    /// Derived class that also has [Factory] attribute.
    /// The generator should correctly handle inheritance.
    /// </summary>
    [Factory]
    public partial class DerivedClass : BaseClass
    {
        public int DerivedProperty { get; set; }
    }
}

#endregion

/// <summary>
/// Tests that derived classes with [Factory] attribute are properly generated.
/// Bug: Derived classes where the base also has [Factory] may not generate correctly.
/// </summary>
public class HasBaseClassFactoryAttributeTests
{
    [Fact]
    public void DerivedClass_ShouldHaveFactoryGeneratedCode()
    {
        // The test passes if the code compiles - this verifies that
        // DerivedClassFactory is generated even when BaseClass has [Factory]
        DerivedClassFactory? factory = null;
        Assert.Null(factory);
    }

    [Fact]
    public void BaseClass_ShouldHaveFactoryGeneratedCode()
    {
        // Also verify the base class factory is generated
        BaseClassFactory? factory = null;
        Assert.Null(factory);
    }
}
