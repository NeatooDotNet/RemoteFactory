/// <summary>
/// Tests for the FactoryOperations samples.
/// These tests verify that the documented code examples actually work.
/// </summary>

using RemoteFactory.Samples.DomainModel.FactoryOperations;
using Xunit;

namespace RemoteFactory.Samples.DomainModel.Tests;

public class FactoryOperationsTests
{
    [Fact]
    public void PersonModel_Create_SetsIsNewToTrue()
    {
        // Arrange & Act
        var person = new PersonModel();

        // Assert
        Assert.True(person.IsNew);
        Assert.False(person.IsDeleted);
    }

    [Fact]
    public void PersonModel_SetProperties_ValuesAreStored()
    {
        // Arrange
        var person = new PersonModel();

        // Act
        person.FirstName = "John";
        person.LastName = "Doe";
        person.Email = "john.doe@example.com";

        // Assert
        Assert.Equal("John", person.FirstName);
        Assert.Equal("Doe", person.LastName);
        Assert.Equal("john.doe@example.com", person.Email);
    }

    [Fact]
    public void PersonEntity_Properties_AreReadWrite()
    {
        // Arrange & Act
        var entity = new PersonEntity
        {
            Id = 1,
            FirstName = "Jane",
            LastName = "Smith",
            Email = "jane.smith@example.com"
        };

        // Assert
        Assert.Equal(1, entity.Id);
        Assert.Equal("Jane", entity.FirstName);
        Assert.Equal("Smith", entity.LastName);
        Assert.Equal("jane.smith@example.com", entity.Email);
    }
}
