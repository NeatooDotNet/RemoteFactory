using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Neatoo.RemoteFactory.Internal;

public interface INeatooJsonSerializer
{
	/// <summary>
	/// Gets the current serialization format.
	/// </summary>
	SerializationFormat Format { get; }

	string? Serialize(object? target);
	string? Serialize(object? target, Type targetType);
	T? Deserialize<T>(string json);
	object? Deserialize(string json, Type type);
	RemoteRequestDto ToRemoteDelegateRequest(Type delegateType, params object?[]? parameters);
	RemoteRequestDto ToRemoteDelegateRequest(Type delegateType, object saveTarget, params object?[]? parameters);
	RemoteRequest DeserializeRemoteDelegateRequest(RemoteRequestDto remoteDelegateRequest);
	T? DeserializeRemoteResponse<T>(RemoteResponseDto remoteResponse);
}

public class NeatooJsonSerializer : INeatooJsonSerializer
{
	private readonly IServiceAssemblies serviceAssemblies;
	private readonly NeatooSerializationOptions serializationOptions;
	private readonly ILogger<NeatooJsonSerializer> logger;
	private readonly ILogger trace;

	JsonSerializerOptions Options { get; }

	/// <summary>
	/// Gets the current serialization format.
	/// </summary>
	public SerializationFormat Format => serializationOptions.Format;

	public NeatooJsonSerializer(IEnumerable<NeatooJsonConverterFactory> neatooJsonConverterFactories, IServiceAssemblies serviceAssemblies, NeatooJsonTypeInfoResolver neatooDefaultJsonTypeInfoResolver)
		: this(neatooJsonConverterFactories, serviceAssemblies, neatooDefaultJsonTypeInfoResolver, new NeatooSerializationOptions(), null)
	{
	}

	public NeatooJsonSerializer(IEnumerable<NeatooJsonConverterFactory> neatooJsonConverterFactories, IServiceAssemblies serviceAssemblies, NeatooJsonTypeInfoResolver neatooDefaultJsonTypeInfoResolver, NeatooSerializationOptions serializationOptions)
		: this(neatooJsonConverterFactories, serviceAssemblies, neatooDefaultJsonTypeInfoResolver, serializationOptions, null)
	{
	}

	public NeatooJsonSerializer(
		IEnumerable<NeatooJsonConverterFactory> neatooJsonConverterFactories,
		IServiceAssemblies serviceAssemblies,
		NeatooJsonTypeInfoResolver neatooDefaultJsonTypeInfoResolver,
		NeatooSerializationOptions serializationOptions,
		ILogger<NeatooJsonSerializer>? logger,
		ILoggerFactory? loggerFactory = null)
	{
		ArgumentNullException.ThrowIfNull(neatooJsonConverterFactories, nameof(neatooJsonConverterFactories));
		ArgumentNullException.ThrowIfNull(serializationOptions, nameof(serializationOptions));

		this.serializationOptions = serializationOptions;
		this.serviceAssemblies = serviceAssemblies;
		this.logger = logger ?? NullLogger<NeatooJsonSerializer>.Instance;
		this.trace = loggerFactory?.CreateLogger("RemoteFactory.Trace")
			?? NullLoggerFactory.Instance.CreateLogger("RemoteFactory.Trace");

		var converterFactoryList = neatooJsonConverterFactories.ToList();

		this.Options = new JsonSerializerOptions
		{
			TypeInfoResolver = neatooDefaultJsonTypeInfoResolver,
			ReferenceHandler = new NeatooPreserveReferenceHandler(),
			WriteIndented = serializationOptions.Format == SerializationFormat.Named, // Only indent for named format (debugging)
			IncludeFields = true
		};

		// Add ordinal converter factory first if using ordinal format
		if (serializationOptions.Format == SerializationFormat.Ordinal)
		{
			this.Options.Converters.Add(new NeatooOrdinalConverterFactory(serializationOptions));
		}

		// Neatoo converters get first priority -- they claim interfaces, abstract types,
		// and IOrdinalSerializable types that have purpose-built converters.
		foreach (var neatooJsonConverterFactory in converterFactoryList)
		{
			this.Options.Converters.Add(neatooJsonConverterFactory);
		}

		// LazyLoadJsonConverterFactory claims LazyLoad<T> types and serializes them
		// in named format as {"value": ..., "isLoaded": bool}. Placed after Neatoo
		// converters (which claim interfaces/abstract types) and before
		// RecordBypassConverterFactory (which would not claim LazyLoad<T> anyway
		// since it has a parameterless constructor).
		this.Options.Converters.Add(new LazyLoadJsonConverterFactory());

		// RecordBypassConverterFactory goes AFTER Neatoo converters. It claims types
		// with parameterized constructors (records) and delegates to inner options
		// without ReferenceHandler, preventing STJ's NotSupportedException for
		// reference metadata on parameterized-constructor types.
		this.Options.Converters.Add(new RecordBypassConverterFactory());
	}


	public string? Serialize(object? target)
	{
		if (target == null)
		{
			return null;
		}

		var targetType = target.GetType();
		var typeName = targetType.Name;
		logger.SerializingObject(typeName, this.Format);

		var sw = Stopwatch.StartNew();
		using var rr = new NeatooReferenceResolver();
		NeatooReferenceResolver.Current = rr;
		try
		{
			var result = JsonSerializer.Serialize(target, this.Options);

			sw.Stop();
			logger.SerializedObject(typeName, sw.ElapsedMilliseconds);

			return result;
		}
		catch (Exception ex)
		{
			sw.Stop();
			logger.SerializationFailed(typeName, ex.Message, ex);
			throw;
		}
		finally
		{
			NeatooReferenceResolver.Current = null;
		}
	}

	public string? Serialize(object? target, Type targetType)
	{
		ArgumentNullException.ThrowIfNull(targetType);

		if (target == null)
		{
			return null;
		}

		var typeName = targetType.Name;
		var runtimeTypeName = target.GetType().Name;
		logger.SerializingObject(typeName, this.Format);
		trace.TraceSerializeStarted(typeName, runtimeTypeName);

		var sw = Stopwatch.StartNew();
		using var rr = new NeatooReferenceResolver();
		NeatooReferenceResolver.Current = rr;
		try
		{
			var result = JsonSerializer.Serialize(target, targetType, this.Options);

			sw.Stop();
			trace.TraceSerializeCompleted(typeName, sw.ElapsedMilliseconds, result?.Length ?? 0);
			logger.SerializedObject(typeName, sw.ElapsedMilliseconds);

			return result;
		}
		catch (Exception ex)
		{
			sw.Stop();
			logger.SerializationFailed(typeName, ex.Message, ex);
			throw;
		}
		finally
		{
			NeatooReferenceResolver.Current = null;
		}
	}

	public T? Deserialize<T>(string? json)
	{
		if (string.IsNullOrEmpty(json))
		{
			return default;
		}

		var typeName = typeof(T).Name;
		var sw = Stopwatch.StartNew();

		using var rr = new NeatooReferenceResolver();
		NeatooReferenceResolver.Current = rr;
		try
		{
			var result = JsonSerializer.Deserialize<T>(json, this.Options);

			sw.Stop();
			logger.DeserializedObject(typeName, sw.ElapsedMilliseconds);

			return result;
		}
		catch (Exception ex)
		{
			sw.Stop();
			logger.DeserializationFailed(typeName, ex.Message, ex);
			throw;
		}
		finally
		{
			NeatooReferenceResolver.Current = null;
		}
	}

	public object? Deserialize(string? json, Type type)
	{
		ArgumentNullException.ThrowIfNull(type);

		if (string.IsNullOrEmpty(json))
		{
			return null;
		}

		var typeName = type.Name;
		var sw = Stopwatch.StartNew();

		using var rr = new NeatooReferenceResolver();
		NeatooReferenceResolver.Current = rr;
		try
		{
			var result = JsonSerializer.Deserialize(json, type, this.Options);

			sw.Stop();
			logger.DeserializedObject(typeName, sw.ElapsedMilliseconds);

			return result;
		}
		catch (Exception ex)
		{
			sw.Stop();
			logger.DeserializationFailed(typeName, ex.Message, ex);
			throw;
		}
		finally
		{
			NeatooReferenceResolver.Current = null;
		}
	}

	public RemoteRequestDto ToRemoteDelegateRequest(Type delegateType, params object?[]? parameters)
	{
		ArgumentNullException.ThrowIfNull(delegateType, nameof(delegateType));
		ArgumentNullException.ThrowIfNull(delegateType.FullName, nameof(delegateType.FullName));

		List<ObjectJson?>? parameterJson = null;

		if (parameters != null)
		{
			parameterJson = parameters.Select(c => this.ToObjectJson(c)).ToList();
		}

		return new RemoteRequestDto
		{
			DelegateFullName = delegateType.FullName,
			Parameters = parameterJson
		};
	}

	public RemoteRequestDto ToRemoteDelegateRequest(Type delegateType, object saveTarget, params object?[]? parameters)
	{
		ArgumentNullException.ThrowIfNull(delegateType, nameof(delegateType));
		ArgumentNullException.ThrowIfNull(delegateType.FullName, nameof(delegateType.FullName));
		ArgumentNullException.ThrowIfNull(saveTarget, nameof(saveTarget));

		List<ObjectJson?>? parameterJson = null;

		if (parameters != null)
		{
			if (parameters.Any(p => p == null))
			{
				throw new ArgumentNullException(nameof(parameters));
			}

			parameterJson = [.. parameters.Select(c => this.ToObjectJson(c))];
		}
		return new RemoteRequestDto
		{
			DelegateFullName = delegateType.FullName,
			Parameters = parameterJson,
			Target = this.ToObjectJson(saveTarget)
		};
	}

	public ObjectJson? ToObjectJson(object? target)
	{
		if (target == null)
		{
			return null;
		}
		var targetType = target.GetType();
		ArgumentNullException.ThrowIfNull(target, nameof(target));
		ArgumentNullException.ThrowIfNull(targetType.FullName, nameof(Type.FullName));
		return new ObjectJson(this.Serialize(target)!, targetType.FullName);
	}

	public RemoteRequest DeserializeRemoteDelegateRequest(RemoteRequestDto remoteDelegateRequest)
	{
		ArgumentNullException.ThrowIfNull(remoteDelegateRequest, nameof(remoteDelegateRequest));

		var delegateTypeName = remoteDelegateRequest.DelegateFullName ?? "unknown";
		logger.DeserializingRemoteRequest(delegateTypeName);

		var sw = Stopwatch.StartNew();

		try
		{
			object? target = null;
			IReadOnlyCollection<object?>? parameters = null;

			if (remoteDelegateRequest.Target != null && !string.IsNullOrEmpty(remoteDelegateRequest.Target.Json))
			{
				trace.TraceDeserializingTarget(remoteDelegateRequest.Target.TypeFullName, remoteDelegateRequest.Target.Json.Length);
				var targetSw = Stopwatch.StartNew();
				target = this.FromObjectJson(remoteDelegateRequest.Target);
				targetSw.Stop();
				trace.TraceTargetDeserialized(targetSw.ElapsedMilliseconds);
			}
			if (remoteDelegateRequest.Parameters != null)
			{
				var paramIndex = 0;
				var paramList = new List<object?>();
				foreach (var p in remoteDelegateRequest.Parameters)
				{
					var paramTypeName = p?.TypeFullName ?? "null";
					var paramJsonLength = p?.Json?.Length ?? 0;
					trace.TraceDeserializingParam(paramIndex, paramTypeName, paramJsonLength);
					var paramSw = Stopwatch.StartNew();
					paramList.Add(this.FromObjectJson(p));
					paramSw.Stop();
					trace.TraceParamDeserialized(paramIndex, paramSw.ElapsedMilliseconds);
					paramIndex++;
				}
				parameters = paramList.ToImmutableList();
			}

			var delegateType = this.serviceAssemblies.FindType(remoteDelegateRequest.DelegateFullName!);

			if (delegateType == null)
			{
				throw new MissingDelegateException($"Cannot find delegate type {remoteDelegateRequest.DelegateFullName} in the registered assemblies");
			}

			var result = new RemoteRequest()
			{
				DelegateType = delegateType,
				Parameters = parameters,
				Target = target
			};

			sw.Stop();
			logger.DeserializedObject(delegateTypeName, sw.ElapsedMilliseconds);

			return result;
		}
		catch (Exception ex)
		{
			sw.Stop();
			logger.DeserializationFailed(delegateTypeName, ex.Message, ex);
			throw;
		}
	}

	public T? DeserializeRemoteResponse<T>(RemoteResponseDto remoteResponse)
	{
		if (remoteResponse?.Json == null)
		{
			return default;
		}

		return this.Deserialize<T>(remoteResponse.Json);
	}

	public object? FromObjectJson(ObjectJson? objectTypeJson)
	{
		if (objectTypeJson == null)
		{
			return null;
		}

		var targetType = this.serviceAssemblies.FindType(objectTypeJson.TypeFullName);
		ArgumentNullException.ThrowIfNull(targetType, nameof(objectTypeJson.TypeFullName));
		return this.Deserialize(objectTypeJson.Json, targetType);
	}

}
