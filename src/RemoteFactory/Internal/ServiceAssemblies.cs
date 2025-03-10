using System.Reflection;

namespace Neatoo.RemoteFactory.Internal;

public interface IServiceAssemblies
{
	bool HasType(Type type);
	Type? FindType(string fullName);
}

public class ServiceAssemblies : IServiceAssemblies
{
	private List<Assembly> Assemblies { get; } = new List<Assembly>();
	public ServiceAssemblies(Assembly[] assemblies)
	{
		this.Assemblies.AddRange(assemblies);

		var foundTypes = assemblies.SelectMany(a => a.GetTypes())
		.Where(a => a != null && a.FullName != null);

		lock (this.lockObject)
		{
			foreach (var type in foundTypes)
			{
				this.TypeCache[type.FullName!] = type;
			}
		}

	}

	private Dictionary<string, Type?> TypeCache { get; set; } = [];

	private object lockObject = new object();

	public bool HasType(Type type)
	{
		ArgumentNullException.ThrowIfNull(type, nameof(type));

		return this.Assemblies.Contains(type.Assembly);
	}

	public Type? FindType(string fullName)
	{
		lock (this.lockObject)
		{
			if (this.TypeCache.TryGetValue(fullName, out var t))
			{
				return t;
			}
		}

		var foundType = Type.GetType(fullName);

		if (foundType == null)
		{
			return null;
		}

		lock (this.lockObject)
		{
			this.TypeCache[foundType.FullName!] = foundType;
		}

		return foundType;
	}
}
