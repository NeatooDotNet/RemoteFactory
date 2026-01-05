using Microsoft.Extensions.DependencyInjection;
using Neatoo.RemoteFactory.DocsExamples.Infrastructure;

namespace Neatoo.RemoteFactory.DocsExamples.GettingStarted;

#region docs-quick-start-person-interface
/// <summary>
/// Interface for the PersonModel domain object.
/// Implements IFactorySaveMeta to support Save operations.
/// </summary>
public interface IPersonModel : IFactorySaveMeta
{
	int Id { get; set; }
	string? FirstName { get; set; }
	string? LastName { get; set; }
	string? Email { get; set; }
	new bool IsNew { get; set; }
	new bool IsDeleted { get; set; }
}
#endregion docs-quick-start-person-interface

#region docs-quick-start-person-model
/// <summary>
/// Person domain model with Create, Fetch, and Save operations.
/// The [Factory] attribute triggers code generation of IPersonModelFactory.
/// </summary>
[Factory]
public class PersonModel : IPersonModel
{
	/// <summary>
	/// [Create] marks this constructor for the Create factory method.
	/// </summary>
	[Create]
	public PersonModel()
	{
		IsNew = true;
	}

	public int Id { get; set; }
	public string? FirstName { get; set; }
	public string? LastName { get; set; }
	public string? Email { get; set; }
	public bool IsNew { get; set; } = true;
	public bool IsDeleted { get; set; }

	/// <summary>
	/// [Remote] indicates this method executes on the server.
	/// [Fetch] marks this as the Fetch factory method.
	/// [Service] parameters are resolved from DI on the server.
	/// </summary>
	[Remote]
	[Fetch]
	public async Task<bool> Fetch(int id, [Service] IPersonContext context)
	{
		var entity = await context.Persons.FindAsync(id);
		if (entity == null) return false;

		// Map entity properties to domain model
		this.Id = entity.Id;
		this.FirstName = entity.FirstName;
		this.LastName = entity.LastName;
		this.Email = entity.Email;
		IsNew = false;
		return true;
	}

	/// <summary>
	/// Combined Insert and Update using both [Insert] and [Update] attributes.
	/// This creates an "upsert" pattern where the same method handles both cases.
	/// </summary>
	[Remote]
	[Insert]
	[Update]
	public async Task Save([Service] IPersonContext context)
	{
		PersonEntity entity;

		if (IsNew)
		{
			entity = new PersonEntity();
			context.Persons.Add(entity);
		}
		else
		{
			entity = await context.Persons.FindAsync(Id)
				?? throw new InvalidOperationException("Person not found");
		}

		// Map domain model properties to entity
		entity.FirstName = this.FirstName;
		entity.LastName = this.LastName;
		entity.Email = this.Email;
		await context.SaveChangesAsync();

		Id = entity.Id;
		IsNew = false;
	}

	/// <summary>
	/// Delete operation - removes the entity from the database.
	/// </summary>
	[Remote]
	[Delete]
	public async Task Delete([Service] IPersonContext context)
	{
		var entity = await context.Persons.FindAsync(Id);
		if (entity != null)
		{
			context.Persons.Remove(entity);
			await context.SaveChangesAsync();
		}
	}
}
#endregion docs-quick-start-person-model

/// <summary>
/// Tests for Quick Start documentation examples.
/// These tests verify that all code snippets in quick-start.md work correctly.
/// </summary>
public class QuickStartTests : DocsTestBase<IPersonModelFactory>
{
	[Fact]
	public void Create_ReturnsNewPerson_WithIsNewTrue()
	{
		// The generated Create() method calls the [Create] constructor
		var person = factory.Create();

		Assert.NotNull(person);
		Assert.True(person.IsNew);
		Assert.False(person.IsDeleted);
		Assert.Null(person.FirstName);
		Assert.Null(person.LastName);
		Assert.Null(person.Email);
	}

	[Fact]
	public async Task Fetch_NonExistentPerson_ReturnsNull()
	{
		// Fetch returns null when the entity doesn't exist
		var person = await factory.Fetch(999);

		Assert.Null(person);
	}

	[Fact]
	public async Task Fetch_ExistingPerson_ReturnsPopulatedModel()
	{
		// Arrange - create and save a person via factory (full round-trip test)
		var newPerson = factory.Create();
		newPerson.FirstName = "John";
		newPerson.LastName = "Doe";
		newPerson.Email = "john@example.com";
		var saved = await factory.Save(newPerson);
		var id = saved!.Id;

		// Act - fetch the person through the factory
		var person = await factory.Fetch(id);

		// Assert - all properties should be mapped
		Assert.NotNull(person);
		Assert.Equal(id, person.Id);
		Assert.Equal("John", person.FirstName);
		Assert.Equal("Doe", person.LastName);
		Assert.Equal("john@example.com", person.Email);
		Assert.False(person.IsNew);
	}

	[Fact]
	public async Task Save_NewPerson_InsertsAndAssignsId()
	{
		// Arrange - create a new person
		var person = factory.Create();
		person.FirstName = "Jane";
		person.LastName = "Smith";
		person.Email = "jane@example.com";

		// Act - save the person
		var saved = await factory.Save(person);

		// Assert - ID should be assigned and IsNew should be false
		Assert.NotNull(saved);
		Assert.True(saved.Id > 0);
		Assert.False(saved.IsNew);
		Assert.Equal("Jane", saved.FirstName);
	}

	[Fact]
	public async Task Save_ExistingPerson_Updates()
	{
		// Arrange - create and save initial person
		var person = factory.Create();
		person.FirstName = "Original";
		person.LastName = "Name";
		var saved = await factory.Save(person);
		var id = saved!.Id;

		// Act - fetch, modify, and save again
		var fetched = await factory.Fetch(id);
		fetched!.FirstName = "Updated";
		fetched.LastName = "Person";
		var updated = await factory.Save(fetched);

		// Assert - verify the update persisted
		var verification = await factory.Fetch(id);
		Assert.NotNull(verification);
		Assert.Equal("Updated", verification.FirstName);
		Assert.Equal("Person", verification.LastName);
	}

	// Note: TrySave is only available when using [AuthorizeFactory<T>] attribute.
	// See Authorization section for examples of TrySave.

	[Fact]
	public async Task Delete_RemovesPersonFromDatabase()
	{
		// Arrange - create and save a person
		var person = factory.Create();
		person.FirstName = "ToDelete";
		var saved = await factory.Save(person);
		var id = saved!.Id;

		// Verify it exists
		var exists = await factory.Fetch(id);
		Assert.NotNull(exists);

		// Act - mark for deletion and save
		exists.IsDeleted = true;
		await factory.Save(exists);

		// Assert - should no longer exist
		var deleted = await factory.Fetch(id);
		Assert.Null(deleted);
	}
}
