
namespace Neatoo.RemoteFactory;


public abstract class FactoryBase
{
   protected virtual T DoFactoryMethodCall<T>(FactoryOperation operation, Func<T> mapperMethodCall)
   {
	  ArgumentNullException.ThrowIfNull(mapperMethodCall, nameof(mapperMethodCall));

	  return mapperMethodCall();
   }


	protected virtual Task<T> DoFactoryMethodCallAsync<T>(FactoryOperation operation, Func<Task<T>> mapperMethodCall)
	{
		ArgumentNullException.ThrowIfNull(mapperMethodCall, nameof(mapperMethodCall));

		return mapperMethodCall();
	}

	protected virtual T DoFactoryMethodCall<T>(T target, FactoryOperation operation, Action mapperMethodCall)
   {
	  ArgumentNullException.ThrowIfNull(mapperMethodCall, nameof(mapperMethodCall));

	  mapperMethodCall();

	  return target;
   }

   protected virtual T? DoFactoryMethodCallBool<T>(T target, FactoryOperation operation, Func<bool> mapperMethodCall)
   {
	  ArgumentNullException.ThrowIfNull(mapperMethodCall, nameof(mapperMethodCall));

	  var succeeded = mapperMethodCall();

	  if (!succeeded)
	  {
		 return default;
	  }

	  return target;
   }

   protected virtual async Task<T> DoFactoryMethodCallAsync<T>(T target, FactoryOperation operation, Func<Task> mapperMethodCall)
   {
	  ArgumentNullException.ThrowIfNull(mapperMethodCall, nameof(mapperMethodCall));

	  await mapperMethodCall();

	  return target;
   }

   protected virtual async Task<T?> DoFactoryMethodCallBoolAsync<T>(T target, FactoryOperation operation, Func<Task<bool>> mapperMethodCall)
   {
	  ArgumentNullException.ThrowIfNull(mapperMethodCall, nameof(mapperMethodCall));

	  var succeeded = await mapperMethodCall();

	  if (!succeeded)
	  {
		 return default;
	  }

	  return target;
   }
}
