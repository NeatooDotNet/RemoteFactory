using RemoteFactory.UnitTests.TestTargets.Core;

namespace RemoteFactory.UnitTests.FactoryGenerator.Ordinal;

/// <summary>
/// Tests for ordinal serialization metadata generation (PropertyNames, FromOrdinalArray).
/// These tests fill mutation testing gaps #38 and #40 from OrdinalRenderer.cs.
/// </summary>
/// <remarks>
/// Gap #38: PropertyNames - Validates that PropertyNames contains correct property names in ordinal order.
/// Gap #40: FromOrdinalArray - Validates that FromOrdinalArray correctly deserializes objects.
/// </remarks>
public class OrdinalMetadataTests
{
    // ============================================================================
    // Gap #38 - PropertyNames validation
    // ============================================================================

    [Fact]
    public void PropertyNames_ContainsCorrectPropertyNamesInOrder()
    {
        // Arrange & Act
        var propertyNames = CtorTarget_SimpleDto.PropertyNames;

        // Assert - Properties should be in alphabetical order: Name, Value
        Assert.Equal(2, propertyNames.Length);
        Assert.Equal("Name", propertyNames[0]);
        Assert.Equal("Value", propertyNames[1]);
    }

    [Fact]
    public void PropertyNames_MatchesPropertyTypes_Count()
    {
        // Arrange & Act
        var propertyNames = CtorTarget_SimpleDto.PropertyNames;
        var propertyTypes = CtorTarget_SimpleDto.PropertyTypes;

        // Assert
        Assert.Equal(propertyNames.Length, propertyTypes.Length);
    }

    [Fact]
    public void PropertyNames_ExplicitCtor_ContainsCorrectPropertyNamesInOrder()
    {
        // Arrange & Act
        var propertyNames = CtorTarget_ExplicitCtor.PropertyNames;

        // Assert - Properties should be in alphabetical order: Name, Value
        Assert.Equal(2, propertyNames.Length);
        Assert.Equal("Name", propertyNames[0]);
        Assert.Equal("Value", propertyNames[1]);
    }

    [Fact]
    public void PropertyNames_WithDefaults_ContainsCorrectPropertyNamesInOrder()
    {
        // Arrange & Act
        var propertyNames = CtorTarget_WithDefaults.PropertyNames;

        // Assert - Properties should be in alphabetical order: Name, Value
        Assert.Equal(2, propertyNames.Length);
        Assert.Equal("Name", propertyNames[0]);
        Assert.Equal("Value", propertyNames[1]);
    }

    // ============================================================================
    // Gap #40 - FromOrdinalArray validation
    // ============================================================================

    [Fact]
    public void FromOrdinalArray_CreatesInstanceWithCorrectValues()
    {
        // Arrange
        var values = new object?[] { "TestName", 42 };

        // Act
        var result = CtorTarget_SimpleDto.FromOrdinalArray(values);

        // Assert
        Assert.NotNull(result);
        var dto = Assert.IsType<CtorTarget_SimpleDto>(result);
        Assert.Equal("TestName", dto.Name);
        Assert.Equal(42, dto.Value);
    }

    [Fact]
    public void FromOrdinalArray_ExplicitCtor_CreatesInstanceWithCorrectValues()
    {
        // Arrange
        var values = new object?[] { "TestName", 99 };

        // Act
        var result = CtorTarget_ExplicitCtor.FromOrdinalArray(values);

        // Assert
        Assert.NotNull(result);
        var dto = Assert.IsType<CtorTarget_ExplicitCtor>(result);
        Assert.Equal("TestName", dto.Name);
        Assert.Equal(99, dto.Value);
    }

    [Fact]
    public void FromOrdinalArray_WithDefaults_CreatesInstanceWithCorrectValues()
    {
        // Arrange
        var values = new object?[] { "CustomName", 123 };

        // Act
        var result = CtorTarget_WithDefaults.FromOrdinalArray(values);

        // Assert
        Assert.NotNull(result);
        var dto = Assert.IsType<CtorTarget_WithDefaults>(result);
        Assert.Equal("CustomName", dto.Name);
        Assert.Equal(123, dto.Value);
    }

    // ============================================================================
    // Round-trip validation (ToOrdinalArray -> FromOrdinalArray)
    // ============================================================================

    [Fact]
    public void OrdinalArray_RoundTrip_PreservesValues()
    {
        // Arrange
        var original = new CtorTarget_SimpleDto { Name = "RoundTrip", Value = 777 };

        // Act
        var ordinalArray = original.ToOrdinalArray();
        var restored = CtorTarget_SimpleDto.FromOrdinalArray(ordinalArray);

        // Assert
        Assert.NotNull(restored);
        var dto = Assert.IsType<CtorTarget_SimpleDto>(restored);
        Assert.Equal(original.Name, dto.Name);
        Assert.Equal(original.Value, dto.Value);
    }

    [Fact]
    public void OrdinalArray_PropertyNamesMatchArrayPositions()
    {
        // Arrange
        var dto = new CtorTarget_SimpleDto { Name = "Test", Value = 100 };
        var propertyNames = CtorTarget_SimpleDto.PropertyNames;

        // Act
        var ordinalArray = dto.ToOrdinalArray();

        // Assert - Verify values at positions match property name order
        // PropertyNames[0] = "Name", so ordinalArray[0] should be Name value
        // PropertyNames[1] = "Value", so ordinalArray[1] should be Value value
        Assert.Equal(dto.Name, ordinalArray[0]);
        Assert.Equal(dto.Value, ordinalArray[1]);
    }
}
