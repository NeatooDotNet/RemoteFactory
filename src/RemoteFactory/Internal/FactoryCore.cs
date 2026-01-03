using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

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
/// for a specific type by registering a specific IFactoryCore{T} implementation
/// or for in general by registering a new IFactoryCore{T} implementation
/// Without need to Inheritance from FactoryBase{T} for each type
/// The goal is to work around the tight coupling of a base class
/// </summary>
/// <typeparam name="T">The domain type this factory core handles.</typeparam>
public class FactoryCore<T> : IFactoryCore<T>
{
	private readonly ILogger logger;
	private static readonly string TypeName = typeof(T).Name;

	public FactoryCore()
		: this(null)
	{
	}

	public FactoryCore(ILogger<FactoryCore<T>>? logger)
	{
		this.logger = logger ?? NullLogger<FactoryCore<T>>.Instance;
	}

	public virtual T DoFactoryMethodCall(FactoryOperation operation, Func<T> factoryMethodCall)
	{
		ArgumentNullException.ThrowIfNull(factoryMethodCall, nameof(factoryMethodCall));

		var correlationId = CorrelationContext.CorrelationId;
		logger.FactoryOperationStarted(correlationId, operation, TypeName);
		var sw = Stopwatch.StartNew();

		try
		{
			var target = factoryMethodCall();

			if (target is IFactoryOnComplete factoryOnComplete)
			{
				logger.InvokingFactoryOnComplete(TypeName);
				factoryOnComplete.FactoryComplete(operation);
			}

			sw.Stop();
			logger.FactoryOperationCompleted(correlationId, operation, TypeName, sw.ElapsedMilliseconds);
			return target;
		}
		catch (Exception ex)
		{
			sw.Stop();
			logger.FactoryOperationFailed(correlationId, operation, TypeName, ex.Message, ex);
			throw;
		}
	}

	public virtual async Task<T> DoFactoryMethodCallAsync(FactoryOperation operation, Func<Task<T>> factoryMethodCall)
	{
		ArgumentNullException.ThrowIfNull(factoryMethodCall, nameof(factoryMethodCall));

		var correlationId = CorrelationContext.CorrelationId;
		logger.FactoryOperationStarted(correlationId, operation, TypeName);
		var sw = Stopwatch.StartNew();

		try
		{
			var target = await factoryMethodCall();

			if (target is IFactoryOnComplete factoryOnComplete)
			{
				logger.InvokingFactoryOnComplete(TypeName);
				factoryOnComplete.FactoryComplete(operation);
			}

			sw.Stop();
			logger.FactoryOperationCompleted(correlationId, operation, TypeName, sw.ElapsedMilliseconds);
			return target;
		}
		catch (Exception ex)
		{
			sw.Stop();
			logger.FactoryOperationFailed(correlationId, operation, TypeName, ex.Message, ex);
			throw;
		}
	}

	public virtual async Task<T?> DoFactoryMethodCallAsyncNullable(FactoryOperation operation, Func<Task<T?>> factoryMethodCall)
	{
		ArgumentNullException.ThrowIfNull(factoryMethodCall, nameof(factoryMethodCall));

		var correlationId = CorrelationContext.CorrelationId;
		logger.FactoryOperationStarted(correlationId, operation, TypeName);
		var sw = Stopwatch.StartNew();

		try
		{
			var target = await factoryMethodCall();

			if (target is IFactoryOnComplete factoryOnComplete)
			{
				logger.InvokingFactoryOnComplete(TypeName);
				factoryOnComplete.FactoryComplete(operation);
			}

			sw.Stop();
			logger.FactoryOperationCompleted(correlationId, operation, TypeName, sw.ElapsedMilliseconds);
			return target;
		}
		catch (Exception ex)
		{
			sw.Stop();
			logger.FactoryOperationFailed(correlationId, operation, TypeName, ex.Message, ex);
			throw;
		}
	}

	public virtual T DoFactoryMethodCall(T target, FactoryOperation operation, Action factoryMethodCall)
	{
		ArgumentNullException.ThrowIfNull(factoryMethodCall, nameof(factoryMethodCall));

		var correlationId = CorrelationContext.CorrelationId;
		logger.FactoryOperationStarted(correlationId, operation, TypeName);
		var sw = Stopwatch.StartNew();

		try
		{
			if (target is IFactoryOnStart factoryOnStart)
			{
				logger.InvokingFactoryOnStart(TypeName);
				factoryOnStart.FactoryStart(operation);
			}

			factoryMethodCall();

			if (target is IFactoryOnComplete factoryOnComplete)
			{
				logger.InvokingFactoryOnComplete(TypeName);
				factoryOnComplete.FactoryComplete(operation);
			}

			sw.Stop();
			logger.FactoryOperationCompleted(correlationId, operation, TypeName, sw.ElapsedMilliseconds);
			return target;
		}
		catch (Exception ex)
		{
			sw.Stop();
			logger.FactoryOperationFailed(correlationId, operation, TypeName, ex.Message, ex);
			throw;
		}
	}

	public virtual T? DoFactoryMethodCallBool(T target, FactoryOperation operation, Func<bool> factoryMethodCall)
	{
		ArgumentNullException.ThrowIfNull(factoryMethodCall, nameof(factoryMethodCall));

		var correlationId = CorrelationContext.CorrelationId;
		logger.FactoryOperationStarted(correlationId, operation, TypeName);
		var sw = Stopwatch.StartNew();

		try
		{
			if (target is IFactoryOnStart factoryOnStart)
			{
				logger.InvokingFactoryOnStart(TypeName);
				factoryOnStart.FactoryStart(operation);
			}

			var succeeded = factoryMethodCall();

			if (!succeeded)
			{
				sw.Stop();
				logger.FactoryOperationCompleted(correlationId, operation, TypeName, sw.ElapsedMilliseconds);
				return default;
			}

			if (target is IFactoryOnComplete factoryOnComplete)
			{
				logger.InvokingFactoryOnComplete(TypeName);
				factoryOnComplete.FactoryComplete(operation);
			}

			sw.Stop();
			logger.FactoryOperationCompleted(correlationId, operation, TypeName, sw.ElapsedMilliseconds);
			return target;
		}
		catch (Exception ex)
		{
			sw.Stop();
			logger.FactoryOperationFailed(correlationId, operation, TypeName, ex.Message, ex);
			throw;
		}
	}

	public virtual async Task<T> DoFactoryMethodCallAsync(T target, FactoryOperation operation, Func<Task> factoryMethodCall)
	{
		ArgumentNullException.ThrowIfNull(factoryMethodCall, nameof(factoryMethodCall));

		var correlationId = CorrelationContext.CorrelationId;
		logger.FactoryOperationStarted(correlationId, operation, TypeName);
		var sw = Stopwatch.StartNew();

		try
		{
			if (target is IFactoryOnStart factoryOnStart)
			{
				logger.InvokingFactoryOnStart(TypeName);
				factoryOnStart.FactoryStart(operation);
			}

			if (target is IFactoryOnStartAsync factoryOnStartAsync)
			{
				await factoryOnStartAsync.FactoryStartAsync(operation);
			}

			await factoryMethodCall();

			if (target is IFactoryOnComplete factoryOnComplete)
			{
				logger.InvokingFactoryOnComplete(TypeName);
				factoryOnComplete.FactoryComplete(operation);
			}

			if (target is IFactoryOnCompleteAsync factoryOnCompleteAsync)
			{
				await factoryOnCompleteAsync.FactoryCompleteAsync(operation);
			}

			sw.Stop();
			logger.FactoryOperationCompleted(correlationId, operation, TypeName, sw.ElapsedMilliseconds);
			return target;
		}
		catch (Exception ex)
		{
			sw.Stop();
			logger.FactoryOperationFailed(correlationId, operation, TypeName, ex.Message, ex);
			throw;
		}
	}

	public virtual async Task<T?> DoFactoryMethodCallBoolAsync(T target, FactoryOperation operation, Func<Task<bool>> factoryMethodCall)
	{
		ArgumentNullException.ThrowIfNull(factoryMethodCall, nameof(factoryMethodCall));

		var correlationId = CorrelationContext.CorrelationId;
		logger.FactoryOperationStarted(correlationId, operation, TypeName);
		var sw = Stopwatch.StartNew();

		try
		{
			if (target is IFactoryOnStart factoryOnStart)
			{
				logger.InvokingFactoryOnStart(TypeName);
				factoryOnStart.FactoryStart(operation);
			}

			if (target is IFactoryOnStartAsync factoryOnStartAsync)
			{
				await factoryOnStartAsync.FactoryStartAsync(operation);
			}

			var succeeded = await factoryMethodCall();

			if (!succeeded)
			{
				sw.Stop();
				logger.FactoryOperationCompleted(correlationId, operation, TypeName, sw.ElapsedMilliseconds);
				return default;
			}

			if (target is IFactoryOnComplete factoryOnComplete)
			{
				logger.InvokingFactoryOnComplete(TypeName);
				factoryOnComplete.FactoryComplete(operation);
			}

			if (target is IFactoryOnCompleteAsync factoryOnCompleteAsync)
			{
				await factoryOnCompleteAsync.FactoryCompleteAsync(operation);
			}

			sw.Stop();
			logger.FactoryOperationCompleted(correlationId, operation, TypeName, sw.ElapsedMilliseconds);
			return target;
		}
		catch (Exception ex)
		{
			sw.Stop();
			logger.FactoryOperationFailed(correlationId, operation, TypeName, ex.Message, ex);
			throw;
		}
	}
}
