using System.ComponentModel.DataAnnotations;

namespace Neatoo.RemoteFactory.DocsExamples.Infrastructure;

#region docs-quick-start-person-entity
/// <summary>
/// Entity class for database persistence.
/// </summary>
public class PersonEntity
{
	[Key]
	public int Id { get; set; }
	public string? FirstName { get; set; }
	public string? LastName { get; set; }
	public string? Email { get; set; }
}
#endregion docs-quick-start-person-entity

#region docs-quick-start-person-context-interface
/// <summary>
/// Data access interface for Person entities.
/// </summary>
public interface IPersonContext
{
	IPersonDbSet Persons { get; }
	Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
#endregion docs-quick-start-person-context-interface

/// <summary>
/// In-memory implementation of IPersonContext for testing.
/// Uses shared static storage so data persists across DI scopes.
/// </summary>
public class InMemoryPersonContext : IPersonContext
{
	// Static shared storage - data persists across all instances
	private static readonly SharedPersonDataStore _sharedStore = new();

	private readonly List<PersonEntity> _pendingAdds = new();
	private readonly List<PersonEntity> _pendingRemoves = new();

	public IPersonDbSet Persons { get; }

	public InMemoryPersonContext()
	{
		Persons = new InMemoryPersonDbSet(_sharedStore, _pendingAdds, _pendingRemoves);
	}

	/// <summary>
	/// Clears all data from the shared store. Call this between tests.
	/// </summary>
	public static void ClearStore() => _sharedStore.Clear();

	public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
	{
		// Assign IDs to new entities
		foreach (var entity in _pendingAdds)
		{
			if (entity.Id == 0)
			{
				entity.Id = _sharedStore.NextId();
			}
		}

		// Commit changes to shared store
		_sharedStore.CommitChanges(_pendingAdds, _pendingRemoves);
		_pendingAdds.Clear();
		_pendingRemoves.Clear();

		return Task.FromResult(_sharedStore.Count);
	}
}

/// <summary>
/// Simplified DbSet interface for documentation examples.
/// </summary>
public interface IPersonDbSet
{
	void Add(PersonEntity entity);
	void Remove(PersonEntity entity);
	ValueTask<PersonEntity?> FindAsync(int id);
}

/// <summary>
/// Static shared data store for in-memory testing.
/// </summary>
internal class SharedPersonDataStore
{
	private readonly List<PersonEntity> _data = new();
	private int _nextId = 1;

	public int Count => _data.Count;

	public int NextId() => _nextId++;

	public PersonEntity? Find(int id)
	{
		lock (_data)
		{
			return _data.FirstOrDefault(p => p.Id == id);
		}
	}

	public void CommitChanges(List<PersonEntity> adds, List<PersonEntity> removes)
	{
		lock (_data)
		{
			foreach (var entity in adds)
			{
				_data.Add(entity);
			}
			foreach (var entity in removes)
			{
				_data.Remove(entity);
			}
		}
	}

	public void Clear()
	{
		lock (_data)
		{
			_data.Clear();
			_nextId = 1;
		}
	}
}

/// <summary>
/// In-memory implementation of person storage.
/// </summary>
public class InMemoryPersonDbSet : IPersonDbSet
{
	private readonly SharedPersonDataStore _store;
	private readonly List<PersonEntity> _pendingAdds;
	private readonly List<PersonEntity> _pendingRemoves;

	internal InMemoryPersonDbSet(SharedPersonDataStore store, List<PersonEntity> pendingAdds, List<PersonEntity> pendingRemoves)
	{
		_store = store;
		_pendingAdds = pendingAdds;
		_pendingRemoves = pendingRemoves;
	}

	public void Add(PersonEntity entity)
	{
		_pendingAdds.Add(entity);
	}

	public void Remove(PersonEntity entity)
	{
		_pendingRemoves.Add(entity);
	}

	public ValueTask<PersonEntity?> FindAsync(int id)
	{
		var entity = _store.Find(id);
		return new ValueTask<PersonEntity?>(entity);
	}
}

/// <summary>
/// Generic service interface for simple dependency injection examples.
/// </summary>
public interface IService { }

/// <summary>
/// Generic service implementation for simple dependency injection examples.
/// </summary>
public class Service : IService { }
