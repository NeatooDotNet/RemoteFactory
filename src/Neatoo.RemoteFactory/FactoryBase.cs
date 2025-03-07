
namespace Neatoo.RemoteFactory;


public abstract class FactoryBase
{
	protected virtual T DoMapperMethodCall<T>(T target, FactoryOperation operation, Action mapperMethodCall)
	{
		ArgumentNullException.ThrowIfNull(mapperMethodCall, nameof(mapperMethodCall));

		mapperMethodCall();

		return target;
	}

	protected virtual T? DoMapperMethodCallBool<T>(T target, FactoryOperation operation, Func<bool> mapperMethodCall)
	{
		ArgumentNullException.ThrowIfNull(mapperMethodCall, nameof(mapperMethodCall));

		var succeeded = mapperMethodCall();

		if (!succeeded)
		{
			return default;
		}

		return target;
	}

	protected virtual async Task<T> DoMapperMethodCallAsync<T>(T target, FactoryOperation operation, Func<Task> mapperMethodCall)
	{
		ArgumentNullException.ThrowIfNull(mapperMethodCall, nameof(mapperMethodCall));

		await mapperMethodCall();

		return target;
	}

	protected virtual async Task<T?> DoMapperMethodCallBoolAsync<T>(T target, FactoryOperation operation, Func<Task<bool>> mapperMethodCall)
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
