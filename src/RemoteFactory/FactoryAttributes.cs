#if NETSTANDARD
namespace Neatoo.RemoteFactory.FactoryGenerator;

#else
namespace Neatoo.RemoteFactory;
#endif

#pragma warning disable CA1813

[System.AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface, Inherited = true, AllowMultiple = false)]
public sealed class FactoryAttribute : Attribute
{
	public FactoryAttribute()
	{
	}
}

[System.AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface, Inherited = true, AllowMultiple = false)]
public sealed class SuppressFactoryAttribute : Attribute
{
	public SuppressFactoryAttribute()
	{
	}
}

[System.AttributeUsage(AttributeTargets.Method, Inherited = true, AllowMultiple = false)]
public sealed class RemoteAttribute : Attribute
{
	public RemoteAttribute()
	{
	}
}

[System.AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
public class FactoryOperationAttribute : Attribute
{
	public FactoryOperation Operation { get; }

	public FactoryOperationAttribute(FactoryOperation operation)
	{
		this.Operation = operation;
	}
}

[System.AttributeUsage(AttributeTargets.Parameter, Inherited = false, AllowMultiple = false)]
public sealed class ServiceAttribute : Attribute
{
	public ServiceAttribute()
	{
	}
}

[System.AttributeUsage(AttributeTargets.Method | AttributeTargets.Constructor, Inherited = false, AllowMultiple = false)]
public sealed class CreateAttribute : Attribute
{
	public FactoryOperation Operation { get; }

	public CreateAttribute()
	{
		this.Operation = FactoryOperation.Create;
	}
}

[System.AttributeUsage(AttributeTargets.Method | AttributeTargets.Constructor, Inherited = false, AllowMultiple = false)]
public sealed class FetchAttribute : Attribute
{
	public FactoryOperation Operation { get; }
	public FetchAttribute()
	{
		this.Operation = FactoryOperation.Fetch;
	}
}

public sealed class InsertAttribute : FactoryOperationAttribute
{
	public InsertAttribute() : base(FactoryOperation.Insert) { }
}

public sealed class UpdateAttribute : FactoryOperationAttribute
{
	public UpdateAttribute() : base(FactoryOperation.Update) { }
}

public sealed class DeleteAttribute : FactoryOperationAttribute
{
	public DeleteAttribute() : base(FactoryOperation.Delete) { }
}

public sealed class ExecuteAttribute : FactoryOperationAttribute
{
	public ExecuteAttribute() : base(FactoryOperation.Execute) { }
}

[System.AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface, Inherited = false, AllowMultiple = false)]
public sealed class AuthorizeFactoryAttribute<T> : Attribute
{
	public AuthorizeFactoryAttribute() { }
}

[System.AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
public sealed class AuthorizeFactoryAttribute : Attribute
{
	public AuthorizeFactoryOperation Operation { get; }
	public AuthorizeFactoryAttribute(AuthorizeFactoryOperation operation)
	{
		this.Operation = operation;
	}
}

[System.AttributeUsage(AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
public sealed class MapperIgnoreAttribute : Attribute
{
	public MapperIgnoreAttribute()
	{
	}
}