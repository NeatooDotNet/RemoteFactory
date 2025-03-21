using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neatoo.RemoteFactory.Internal;

public interface IFactoryCore<T>
{
	T DoFactoryMethodCall(FactoryOperation operation, Func<T> factoryMethodCall);
	Task<T> DoFactoryMethodCallAsync(FactoryOperation operation, Func<Task<T>> factoryMethodCall);
	Task<T?> DoFactoryMethodCallAsyncNullable(FactoryOperation operation, Func<Task<T?>> factoryMethodCall);
	T DoFactoryMethodCall(T target, FactoryOperation operation, Action factoryMethodCall);
	T? DoFactoryMethodCallBool(T target, FactoryOperation operation, Func<bool> factoryMethodCall);
	Task<T> DoFactoryMethodCallAsync(T target, FactoryOperation operation, Func<Task> factoryMethodCall);
	Task<T?> DoFactoryMethodCallBoolAsync(T target, FactoryOperation operation, Func<Task<bool>> factoryMethodCall);
}

/// <summary>
/// This is a wrapper so that Factory logic can be added
/// for a specific type by registering a specific IFactoryCore<MyType> implementation
/// or for in general by registering a new IFactoryCore<T> implementation
/// Without need to Inheritance from FactoryBase<T> for each type
/// The goal is to work around the tight coupling of a base class
/// </summary>
/// <typeparam name="T"></typeparam>
public class FactoryCore<T> : IFactoryCore<T>
{
	public virtual T DoFactoryMethodCall(FactoryOperation operation, Func<T> factoryMethodCall)
	{
		ArgumentNullException.ThrowIfNull(factoryMethodCall, nameof(factoryMethodCall));

		var target = factoryMethodCall();

		if (target is IFactoryOnComplete factoryOnComplete)
		{
			factoryOnComplete.FactoryComplete(operation);
		}

		return target;
	}

	public virtual async Task<T> DoFactoryMethodCallAsync(FactoryOperation operation, Func<Task<T>> factoryMethodCall)
	{
		ArgumentNullException.ThrowIfNull(factoryMethodCall, nameof(factoryMethodCall));

		var target = await factoryMethodCall();

		if (target is IFactoryOnComplete factoryOnComplete)
		{
			factoryOnComplete.FactoryComplete(operation);
		}

		return target;
	}

	public virtual Task<T?> DoFactoryMethodCallAsyncNullable(FactoryOperation operation, Func<Task<T?>> factoryMethodCall)
	{
		ArgumentNullException.ThrowIfNull(factoryMethodCall, nameof(factoryMethodCall));

		var target = factoryMethodCall();

		if (target is IFactoryOnComplete factoryOnComplete)
		{
			factoryOnComplete.FactoryComplete(operation);
		}

		return target;
	}

	public virtual T DoFactoryMethodCall(T target, FactoryOperation operation, Action factoryMethodCall)
	{
		ArgumentNullException.ThrowIfNull(factoryMethodCall, nameof(factoryMethodCall));

		if(target is IFactoryOnStart factoryOnStart)
		{
			factoryOnStart.FactoryStart(operation);
		}

		factoryMethodCall();

		if(target is IFactoryOnComplete factoryOnComplete)
		{
			factoryOnComplete.FactoryComplete(operation);
		}

		return target;
	}

	public virtual T? DoFactoryMethodCallBool(T target, FactoryOperation operation, Func<bool> factoryMethodCall)
	{
		ArgumentNullException.ThrowIfNull(factoryMethodCall, nameof(factoryMethodCall));

		if (target is IFactoryOnStart factoryOnStart)
		{
			factoryOnStart.FactoryStart(operation);
		}

		var succeeded = factoryMethodCall();

		if (!succeeded)
		{
			return default;
		}

		if (target is IFactoryOnComplete factoryOnComplete)
		{
			factoryOnComplete.FactoryComplete(operation);
		}

		return target;
	}

	public virtual async Task<T> DoFactoryMethodCallAsync(T target, FactoryOperation operation, Func<Task> factoryMethodCall)
	{
		ArgumentNullException.ThrowIfNull(factoryMethodCall, nameof(factoryMethodCall));

		if (target is IFactoryOnStart factoryOnStart)
		{
			factoryOnStart.FactoryStart(operation);
		}

		if(target is IFactoryOnStartAsync factoryOnStartAsync)
		{
			await factoryOnStartAsync.FactoryStartAsync(operation);
		}

		await factoryMethodCall();

		if (target is IFactoryOnComplete factoryOnComplete)
		{
			factoryOnComplete.FactoryComplete(operation);
		}

		if (target is IFactoryOnCompleteAsync factoryOnCompleteAsync)
		{
			await factoryOnCompleteAsync.FactoryCompleteAsync(operation);
		}

		return target;
	}

	public virtual async Task<T?> DoFactoryMethodCallBoolAsync(T target, FactoryOperation operation, Func<Task<bool>> factoryMethodCall)
	{
		ArgumentNullException.ThrowIfNull(factoryMethodCall, nameof(factoryMethodCall));

		if (target is IFactoryOnStart factoryOnStart)
		{
			factoryOnStart.FactoryStart(operation);
		}

		if (target is IFactoryOnStartAsync factoryOnStartAsync)
		{
			await factoryOnStartAsync.FactoryStartAsync(operation);
		}

		var succeeded = await factoryMethodCall();

		if (!succeeded)
		{
			return default;
		}

		if (target is IFactoryOnComplete factoryOnComplete)
		{
			factoryOnComplete.FactoryComplete(operation);
		}

		if (target is IFactoryOnCompleteAsync factoryOnCompleteAsync)
		{
			await factoryOnCompleteAsync.FactoryCompleteAsync(operation);
		}

		return target;
	}
}
