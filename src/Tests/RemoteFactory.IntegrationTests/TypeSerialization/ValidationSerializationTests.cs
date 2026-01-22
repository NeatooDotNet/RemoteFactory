using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.DependencyInjection;
using RemoteFactory.IntegrationTests.TestContainers;
using RemoteFactory.IntegrationTests.TestTargets.TypeSerialization;

namespace RemoteFactory.IntegrationTests.TypeSerialization;

/// <summary>
/// Tests for serialization round-trip of validation errors.
/// Uses DataAnnotations with Validator to generate ValidationResult objects
/// and verifies they serialize properly from server to client.
/// </summary>
public class ValidationSerializationTests
{
    // ============================================================================
    // Fetch Tests - Validation Errors from Server
    // ============================================================================

    [Fact]
    public async Task Fetch_WithValidationErrors_SerializesToClient()
    {
        // Arrange
        var scopes = ClientServerContainers.Scopes();
        var factory = scopes.client.ServiceProvider.GetRequiredService<IValidatedEntityFactory>();
        var testId = Guid.NewGuid();

        // Act - Client fetches entity, server triggers validation and populates errors
        var entity = await factory.Fetch(testId, triggerValidationError: true);

        // Assert
        Assert.NotNull(entity);
        Assert.NotNull(entity.ValidationErrors);
        Assert.NotEmpty(entity.ValidationErrors);
        Assert.Equal(2, entity.ValidationErrors.Count); // Name required + Value range
    }

    [Fact]
    public async Task Fetch_WithValidationErrors_PreservesErrorMessage()
    {
        // Arrange
        var scopes = ClientServerContainers.Scopes();
        var factory = scopes.client.ServiceProvider.GetRequiredService<IValidatedEntityFactory>();

        // Act
        var entity = await factory.Fetch(Guid.NewGuid(), triggerValidationError: true);

        // Assert
        Assert.NotNull(entity?.ValidationErrors);

        var nameError = entity.ValidationErrors.FirstOrDefault(e =>
            e.MemberNames?.Contains("Name") == true);
        Assert.NotNull(nameError);
        Assert.Equal("Name is required", nameError.ErrorMessage);

        var valueError = entity.ValidationErrors.FirstOrDefault(e =>
            e.MemberNames?.Contains("Value") == true);
        Assert.NotNull(valueError);
        Assert.Equal("Value must be between 1 and 100", valueError.ErrorMessage);
    }

    [Fact]
    public async Task Fetch_WithValidationErrors_PreservesMemberNames()
    {
        // Arrange
        var scopes = ClientServerContainers.Scopes();
        var factory = scopes.client.ServiceProvider.GetRequiredService<IValidatedEntityFactory>();

        // Act
        var entity = await factory.Fetch(Guid.NewGuid(), triggerValidationError: true);

        // Assert
        Assert.NotNull(entity?.ValidationErrors);
        Assert.All(entity.ValidationErrors, error =>
        {
            Assert.NotNull(error.MemberNames);
            Assert.NotEmpty(error.MemberNames);
        });
    }

    [Fact]
    public async Task Fetch_NoValidationErrors_EmptyCollection()
    {
        // Arrange
        var scopes = ClientServerContainers.Scopes();
        var factory = scopes.client.ServiceProvider.GetRequiredService<IValidatedEntityFactory>();

        // Act - Fetch without triggering validation
        var entity = await factory.Fetch(Guid.NewGuid(), triggerValidationError: false);

        // Assert
        Assert.NotNull(entity);
        Assert.Null(entity.ValidationErrors);
    }

    [Fact]
    public async Task Fetch_ValidEntity_NoValidationErrors()
    {
        // Arrange
        var scopes = ClientServerContainers.Scopes();
        var factory = scopes.client.ServiceProvider.GetRequiredService<IValidatedEntityFactory>();

        // Act - Fetch with valid values and run validation
        var entity = await factory.FetchValid(Guid.NewGuid(), "ValidName", 50, runValidation: true);

        // Assert
        Assert.NotNull(entity);
        Assert.Null(entity.ValidationErrors); // No errors when entity is valid
    }

    // ============================================================================
    // Save Tests - Validation Errors from Client to Server
    // ============================================================================

    [Fact]
    public async Task Save_WithValidationErrors_ServerReceivesErrors()
    {
        // Arrange
        var scopes = ClientServerContainers.Scopes();
        var factory = scopes.client.ServiceProvider.GetRequiredService<IValidatedEntityFactory>();
        var entity = await factory.Create();

        // Client validates and populates errors
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(entity, new ValidationContext(entity), results, validateAllProperties: true);
        entity.ValidationErrors = results.Select(r => new ValidationError(r)).ToList();

        // Act - Send to server via Save
        var saved = await factory.Save(entity);

        // Assert - Validation errors should survive round-trip
        Assert.NotNull(saved);
        Assert.NotNull(saved.ValidationErrors);
        Assert.Equal(entity.ValidationErrors.Count, saved.ValidationErrors.Count);
    }

    [Fact]
    public async Task Save_ModifyValidationErrors_PreservesChanges()
    {
        // Arrange
        var scopes = ClientServerContainers.Scopes();
        var factory = scopes.client.ServiceProvider.GetRequiredService<IValidatedEntityFactory>();
        var entity = await factory.Create();

        // Add custom validation error on client
        entity.ValidationErrors = new List<ValidationError>
        {
            new ValidationError
            {
                ErrorMessage = "Custom client-side error",
                MemberNames = new List<string> { "CustomField", "AnotherField" }
            }
        };

        // Act
        var saved = await factory.Save(entity);

        // Assert
        Assert.NotNull(saved);
        Assert.NotNull(saved.ValidationErrors);
        Assert.Single(saved.ValidationErrors);
        Assert.Equal("Custom client-side error", saved.ValidationErrors[0].ErrorMessage);
        Assert.NotNull(saved.ValidationErrors[0].MemberNames);
        Assert.Equal(2, saved.ValidationErrors[0].MemberNames!.Count);
        Assert.Contains("CustomField", saved.ValidationErrors[0].MemberNames!);
        Assert.Contains("AnotherField", saved.ValidationErrors[0].MemberNames!);
    }

    // ============================================================================
    // Server Direct Tests - Verify Same Behavior
    // ============================================================================

    [Fact]
    public async Task Fetch_Server_PopulatesValidationErrors()
    {
        // Arrange
        var scopes = ClientServerContainers.Scopes();
        var factory = scopes.server.ServiceProvider.GetRequiredService<IValidatedEntityFactory>();

        // Act - Server fetches directly
        var entity = await factory.Fetch(Guid.NewGuid(), triggerValidationError: true);

        // Assert
        Assert.NotNull(entity?.ValidationErrors);
        Assert.Equal(2, entity.ValidationErrors.Count);
    }

    [Fact]
    public async Task Save_Server_PreservesValidationErrors()
    {
        // Arrange
        var scopes = ClientServerContainers.Scopes();
        var factory = scopes.server.ServiceProvider.GetRequiredService<IValidatedEntityFactory>();
        var entity = await factory.Create();
        entity.ValidationErrors = new List<ValidationError>
        {
            new ValidationError { ErrorMessage = "Server test error", MemberNames = new List<string> { "TestField" } }
        };

        // Act
        var saved = await factory.Save(entity);

        // Assert
        Assert.NotNull(saved?.ValidationErrors);
        Assert.Single(saved.ValidationErrors);
        Assert.Equal("Server test error", saved.ValidationErrors[0].ErrorMessage);
    }

    // ============================================================================
    // Edge Cases
    // ============================================================================

    [Fact]
    public async Task Fetch_EmptyMemberNames_Serializes()
    {
        // Arrange
        var scopes = ClientServerContainers.Scopes();
        var factory = scopes.client.ServiceProvider.GetRequiredService<IValidatedEntityFactory>();
        var entity = await factory.Create();

        // Add error with empty MemberNames
        entity.ValidationErrors = new List<ValidationError>
        {
            new ValidationError { ErrorMessage = "Global error", MemberNames = new List<string>() }
        };

        // Act
        var saved = await factory.Save(entity);

        // Assert
        Assert.NotNull(saved);
        Assert.NotNull(saved.ValidationErrors);
        Assert.Single(saved.ValidationErrors);
        Assert.NotNull(saved.ValidationErrors[0].MemberNames);
        Assert.Empty(saved.ValidationErrors[0].MemberNames!);
    }

    [Fact]
    public async Task Fetch_NullMemberNames_Serializes()
    {
        // Arrange
        var scopes = ClientServerContainers.Scopes();
        var factory = scopes.client.ServiceProvider.GetRequiredService<IValidatedEntityFactory>();
        var entity = await factory.Create();

        // Add error with null MemberNames
        entity.ValidationErrors = new List<ValidationError>
        {
            new ValidationError { ErrorMessage = "Orphan error", MemberNames = null }
        };

        // Act
        var saved = await factory.Save(entity);

        // Assert
        Assert.NotNull(saved?.ValidationErrors);
        Assert.Single(saved.ValidationErrors);
        Assert.Equal("Orphan error", saved.ValidationErrors[0].ErrorMessage);
        Assert.Null(saved.ValidationErrors[0].MemberNames);
    }

    [Fact]
    public async Task MultipleValidationErrors_AllPreserved()
    {
        // Arrange
        var scopes = ClientServerContainers.Scopes();
        var factory = scopes.client.ServiceProvider.GetRequiredService<IValidatedEntityFactory>();
        var entity = await factory.Create();

        // Add multiple validation errors
        entity.ValidationErrors = new List<ValidationError>
        {
            new ValidationError { ErrorMessage = "Error 1", MemberNames = new List<string> { "Field1" } },
            new ValidationError { ErrorMessage = "Error 2", MemberNames = new List<string> { "Field2" } },
            new ValidationError { ErrorMessage = "Error 3", MemberNames = new List<string> { "Field3", "Field4" } }
        };

        // Act
        var saved = await factory.Save(entity);

        // Assert
        Assert.NotNull(saved?.ValidationErrors);
        Assert.Equal(3, saved.ValidationErrors.Count);

        Assert.Equal("Error 1", saved.ValidationErrors[0].ErrorMessage);
        Assert.Equal("Error 2", saved.ValidationErrors[1].ErrorMessage);
        Assert.Equal("Error 3", saved.ValidationErrors[2].ErrorMessage);

        Assert.Single(saved.ValidationErrors[0].MemberNames!);
        Assert.Single(saved.ValidationErrors[1].MemberNames!);
        Assert.Equal(2, saved.ValidationErrors[2].MemberNames!.Count);
    }
}
