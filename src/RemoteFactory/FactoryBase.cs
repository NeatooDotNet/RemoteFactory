
using Neatoo.RemoteFactory.Internal;

namespace Neatoo.RemoteFactory;


public abstract class FactoryBase<T>
{
   protected IFactoryCore<T> FactoryCore { get; }

   protected FactoryBase(IFactoryCore<T> factoryCore)
	{
	  this.FactoryCore = factoryCore;
   }

   protected virtual T DoFactoryMethodCall(FactoryOperation operation, Func<T> factoryMethodCall)
   {
	  ArgumentNullException.ThrowIfNull(factoryMethodCall, nameof(factoryMethodCall));

	  return this.FactoryCore.DoFactoryMethodCall(operation, factoryMethodCall);
	}

	protected virtual Task<T> DoFactoryMethodCallAsync(FactoryOperation operation, Func<Task<T>> factoryMethodCall)
	{
		ArgumentNullException.ThrowIfNull(factoryMethodCall, nameof(factoryMethodCall));

		return this.FactoryCore.DoFactoryMethodCallAsync(operation, factoryMethodCall);
	}

	protected virtual Task<T?> DoFactoryMethodCallAsyncNullable(FactoryOperation operation, Func<Task<T?>> factoryMethodCall)
	{
		ArgumentNullException.ThrowIfNull(factoryMethodCall, nameof(factoryMethodCall));

		return this.FactoryCore.DoFactoryMethodCallAsyncNullable(operation, factoryMethodCall);
	}

	protected virtual T DoFactoryMethodCall(T target, FactoryOperation operation, Action factoryMethodCall)
   {
	  ArgumentNullException.ThrowIfNull(factoryMethodCall, nameof(factoryMethodCall));

		return this.FactoryCore.DoFactoryMethodCall(target, operation, factoryMethodCall);
   }

   protected virtual T? DoFactoryMethodCallBool(T target, FactoryOperation operation, Func<bool> factoryMethodCall)
   {
	  ArgumentNullException.ThrowIfNull(factoryMethodCall, nameof(factoryMethodCall));

		return this.FactoryCore.DoFactoryMethodCallBool(target, operation, factoryMethodCall);
	}

	protected virtual Task<T> DoFactoryMethodCallAsync(T target, FactoryOperation operation, Func<Task> factoryMethodCall)
   {
		return this.FactoryCore.DoFactoryMethodCallAsync(target, operation, factoryMethodCall);
	}

	protected virtual Task<T?> DoFactoryMethodCallBoolAsync(T target, FactoryOperation operation, Func<Task<bool>> factoryMethodCall)
   {
	  return this.FactoryCore.DoFactoryMethodCallBoolAsync(target, operation, factoryMethodCall);
	}
}
