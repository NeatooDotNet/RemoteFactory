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

	JsonSerializerOptions Options { get; }

	private NeatooReferenceHandler ReferenceHandler { get; } = new NeatooReferenceHandler();

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
		ILogger<NeatooJsonSerializer>? logger)
	{
		ArgumentNullException.ThrowIfNull(neatooJsonConverterFactories, nameof(neatooJsonConverterFactories));
		ArgumentNullException.ThrowIfNull(serializationOptions, nameof(serializationOptions));

		this.serializationOptions = serializationOptions;
		this.serviceAssemblies = serviceAssemblies;
		this.logger = logger ?? NullLogger<NeatooJsonSerializer>.Instance;

		this.Options = new JsonSerializerOptions
		{
			ReferenceHandler = this.ReferenceHandler,
			TypeInfoResolver = neatooDefaultJsonTypeInfoResolver,
			WriteIndented = serializationOptions.Format == SerializationFormat.Named, // Only indent for named format (debugging)
			IncludeFields = true
		};

		// Add ordinal converter factory first if using ordinal format
		if (serializationOptions.Format == SerializationFormat.Ordinal)
		{
			this.Options.Converters.Add(new NeatooOrdinalConverterFactory(serializationOptions));
		}

		foreach (var neatooJsonConverterFactory in neatooJsonConverterFactories)
		{
			this.Options.Converters.Add(neatooJsonConverterFactory);
		}
	}


	public string? Serialize(object? target)
	{
		if (target == null)
		{
			return null;
		}

		var typeName = target.GetType().Name;
		logger.SerializingObject(typeName, this.Format);

		var sw = Stopwatch.StartNew();
		try
		{
			using var rr = new NeatooReferenceResolver();
			this.ReferenceHandler.ReferenceResolver.Value = rr;

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
	}

	public string? Serialize(object? target, Type targetType)
	{
		ArgumentNullException.ThrowIfNull(targetType);

		if (target == null)
		{
			return null;
		}

		var typeName = targetType.Name;
		logger.SerializingObject(typeName, this.Format);

		var sw = Stopwatch.StartNew();
		try
		{
			using var rr = new NeatooReferenceResolver();
			this.ReferenceHandler.ReferenceResolver.Value = rr;

			var result = JsonSerializer.Serialize(target, targetType, this.Options);

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
	}

	public T? Deserialize<T>(string? json)
	{
		if (string.IsNullOrEmpty(json))
		{
			return default;
		}

		var typeName = typeof(T).Name;
		var sw = Stopwatch.StartNew();

		try
		{
			using var rr = new NeatooReferenceResolver();
			this.ReferenceHandler.ReferenceResolver.Value = rr;

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

		try
		{
			using var rr = new NeatooReferenceResolver();
			this.ReferenceHandler.ReferenceResolver.Value = rr;

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
			DelegateAssemblyType = delegateType.FullName,
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
			DelegateAssemblyType = delegateType.FullName,
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

		var delegateTypeName = remoteDelegateRequest.DelegateAssemblyType ?? "unknown";
		logger.DeserializingRemoteRequest(delegateTypeName);

		var sw = Stopwatch.StartNew();

		try
		{
			object? target = null;
			IReadOnlyCollection<object?>? parameters = null;

			if (remoteDelegateRequest.Target != null && !string.IsNullOrEmpty(remoteDelegateRequest.Target.Json))
			{
				target = this.FromObjectJson(remoteDelegateRequest.Target);
			}
			if (remoteDelegateRequest.Parameters != null)
			{
				parameters = remoteDelegateRequest.Parameters.Select(c => this.FromObjectJson(c)).ToImmutableList();
			}

			var delegateType = this.serviceAssemblies.FindType(remoteDelegateRequest.DelegateAssemblyType!);

			if (delegateType == null)
			{
				throw new MissingDelegateException($"Cannot find delegate type {remoteDelegateRequest.DelegateAssemblyType} in the registered assemblies");
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

		var targetType = this.serviceAssemblies.FindType(objectTypeJson.AssemblyType);
		ArgumentNullException.ThrowIfNull(targetType, nameof(objectTypeJson.AssemblyType));
		return this.Deserialize(objectTypeJson.Json, targetType);
	}

}
