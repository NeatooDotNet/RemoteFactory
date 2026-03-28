using Neatoo.RemoteFactory;
using RemoteFactory.UnitTests.TestTargets.LazyLoad;

namespace RemoteFactory.UnitTests.LazyLoad;

/// <summary>
/// Tests for ordinal serialization of LazyLoad&lt;T&gt; properties on [Factory] classes.
/// Validates the two-slot encoding: each LazyLoad property occupies two consecutive
/// ordinal array slots (Value + IsLoaded).
/// </summary>
public class LazyLoadOrdinalTests
{
    // TS-LL-015: Ordinal format loaded round-trip
    [Fact]
    public void OrdinalFormat_Loaded_RoundTrip()
    {
        // Arrange: Factory class with a loaded LazyLoad property
        var original = new LazyLoadOrdinalTarget
        {
            Name = "TestOrder",
            Lines = new LazyLoad<string>("line-data")
        };

        // Act: Convert to ordinal array and back
        var ordinalArray = original.ToOrdinalArray();
        var restored = LazyLoadOrdinalTarget.FromOrdinalArray(ordinalArray);

        // Assert: Round-trip preserves all values
        Assert.NotNull(restored);
        var target = Assert.IsType<LazyLoadOrdinalTarget>(restored);
        Assert.Equal("TestOrder", target.Name);
        Assert.NotNull(target.Lines);
        Assert.Equal("line-data", target.Lines.Value);
        Assert.True(target.Lines.IsLoaded);
    }

    // TS-LL-016: Ordinal format unloaded round-trip
    [Fact]
    public void OrdinalFormat_Unloaded_RoundTrip()
    {
        // Arrange: Factory class with an unloaded LazyLoad property
        var original = new LazyLoadOrdinalTarget
        {
            Name = "EmptyOrder",
            Lines = new LazyLoad<string>()
        };

        // Act
        var ordinalArray = original.ToOrdinalArray();
        var restored = LazyLoadOrdinalTarget.FromOrdinalArray(ordinalArray);

        // Assert
        Assert.NotNull(restored);
        var target = Assert.IsType<LazyLoadOrdinalTarget>(restored);
        Assert.Equal("EmptyOrder", target.Name);
        Assert.NotNull(target.Lines);
        Assert.Null(target.Lines.Value);
        Assert.False(target.Lines.IsLoaded);
    }

    // TS-LL-017: PropertyNames/PropertyTypes arrays verification
    [Fact]
    public void OrdinalMetadata_PropertyNamesAndTypes()
    {
        // Act
        var propertyNames = LazyLoadOrdinalTarget.PropertyNames;
        var propertyTypes = LazyLoadOrdinalTarget.PropertyTypes;

        // Assert PropertyNames: "Lines" and "Lines__IsLoaded" for the LazyLoad property,
        // plus "Name" for the regular property. Sorted alphabetically: Lines, Lines__IsLoaded, Name.
        Assert.Equal(3, propertyNames.Length);
        Assert.Equal("Lines", propertyNames[0]);
        Assert.Equal("Lines__IsLoaded", propertyNames[1]);
        Assert.Equal("Name", propertyNames[2]);

        // Assert PropertyTypes: typeof(string) and typeof(bool) for the LazyLoad,
        // plus typeof(string) for the regular property.
        Assert.Equal(3, propertyTypes.Length);
        Assert.Equal(typeof(string), propertyTypes[0]);
        Assert.Equal(typeof(bool), propertyTypes[1]);
        Assert.Equal(typeof(string), propertyTypes[2]);
    }

    [Fact]
    public void ToOrdinalArray_Loaded_HasCorrectSlotValues()
    {
        // Arrange
        var target = new LazyLoadOrdinalTarget
        {
            Name = "SlotTest",
            Lines = new LazyLoad<string>("loaded-value")
        };

        // Act
        var array = target.ToOrdinalArray();

        // Assert: 3 slots total (Lines.Value, Lines.IsLoaded, Name)
        Assert.Equal(3, array.Length);
        Assert.Equal("loaded-value", array[0]); // Lines.Value
        Assert.Equal(true, array[1]);            // Lines.IsLoaded
        Assert.Equal("SlotTest", array[2]);      // Name
    }

    [Fact]
    public void ToOrdinalArray_Unloaded_HasCorrectSlotValues()
    {
        // Arrange
        var target = new LazyLoadOrdinalTarget
        {
            Name = "UnloadedTest",
            Lines = new LazyLoad<string>()
        };

        // Act
        var array = target.ToOrdinalArray();

        // Assert
        Assert.Equal(3, array.Length);
        Assert.Null(array[0]);                   // Lines.Value (null for unloaded)
        Assert.Equal(false, array[1]);            // Lines.IsLoaded
        Assert.Equal("UnloadedTest", array[2]);   // Name
    }

    [Fact]
    public void PropertyNames_MatchesPropertyTypes_Count()
    {
        // The counts must match since they represent the same ordinal array positions
        Assert.Equal(
            LazyLoadOrdinalTarget.PropertyNames.Length,
            LazyLoadOrdinalTarget.PropertyTypes.Length);
    }
}
