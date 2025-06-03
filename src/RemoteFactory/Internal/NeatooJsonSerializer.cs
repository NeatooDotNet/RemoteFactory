using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using System.Text.Json;

namespace Neatoo.RemoteFactory.Internal;

public interface INeatooJsonSerializer
{
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
   private readonly GetServiceImplementationTypes getServiceImplementationTypes;

   JsonSerializerOptions Options { get; }

	private NeatooReferenceHandler ReferenceHandler { get; } = new NeatooReferenceHandler();

	public NeatooJsonSerializer(IEnumerable<NeatooJsonConverterFactory> neatooJsonConverterFactories, IServiceAssemblies serviceAssemblies, NeatooJsonTypeInfoResolver neatooDefaultJsonTypeInfoResolver, GetServiceImplementationTypes getServiceImplementationTypes)
	{
		ArgumentNullException.ThrowIfNull(neatooJsonConverterFactories, nameof(neatooJsonConverterFactories));

		this.Options = new JsonSerializerOptions
		{
			ReferenceHandler = this.ReferenceHandler,
			TypeInfoResolver = neatooDefaultJsonTypeInfoResolver,
			WriteIndented = true,
			IncludeFields = true
		};

		foreach (var neatooJsonConverterFactory in neatooJsonConverterFactories)
		{
			this.Options.Converters.Add(neatooJsonConverterFactory);
		}

		this.serviceAssemblies = serviceAssemblies;
	  this.getServiceImplementationTypes = getServiceImplementationTypes;
   }


	public string? Serialize(object? target)
	{
		if (target == null)
		{
			return null;
		}

		using var rr = new NeatooReferenceResolver();

		this.ReferenceHandler.ReferenceResolver.Value = rr;

		return JsonSerializer.Serialize(target, this.Options);
	}

	public string? Serialize(object? target, Type targetType)
	{
		if (target == null)
		{
			return null;
		}

		using var rr = new NeatooReferenceResolver();

		this.ReferenceHandler.ReferenceResolver.Value = rr;

		return JsonSerializer.Serialize(target, targetType, this.Options);
	}

	public T? Deserialize<T>(string? json)
	{
		if (string.IsNullOrEmpty(json))
		{
			return default;
		}

		using var rr = new NeatooReferenceResolver();
		this.ReferenceHandler.ReferenceResolver.Value = rr;

		return JsonSerializer.Deserialize<T>(json, this.Options);
	}

	public object? Deserialize(string? json, Type type)
	{
		if (string.IsNullOrEmpty(json))
		{
			return null;
		}

		using var rr = new NeatooReferenceResolver();
		this.ReferenceHandler.ReferenceResolver.Value = rr;

		return JsonSerializer.Deserialize(json, type, this.Options);
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

		object? target = null;
		List<object?>? parameters = null;

		var delegateType = this.serviceAssemblies.FindType(remoteDelegateRequest.DelegateAssemblyType);

		if (delegateType == null)
		{
			throw new MissingDelegateException($"Cannot find delegate type {remoteDelegateRequest.DelegateAssemblyType} in the registered assemblies");
		}

		var invokeMethod = delegateType.GetMethod("Invoke")!;
		var delegateParameters = invokeMethod.GetParameters();

		if(delegateParameters.Length != remoteDelegateRequest.Parameters?.Count)
		{
			throw new ArgumentException($"The number of parameters in the request does not match the delegate type {delegateType.FullName}");
		}




		if (remoteDelegateRequest.Target != null && !string.IsNullOrEmpty(remoteDelegateRequest.Target.Json))
		{
			target = this.FromObjectJson(remoteDelegateRequest.Target);
		}

		if (remoteDelegateRequest.Parameters != null)
		{
			var i = 0;
			var enumerableParameters = remoteDelegateRequest.Parameters.GetEnumerator();
			parameters = new List<object?>(remoteDelegateRequest.Parameters.Count);

			while (enumerableParameters.MoveNext())
			{
				var parameter = enumerableParameters.Current;
				var parameterType = delegateParameters[i].ParameterType;

				if (parameterType.IsInterface)
				{
					var possibleTypes = this.getServiceImplementationTypes(parameterType.FullName!);
					if (!possibleTypes.Any())
					{
						throw new MissingDelegateException($"Cannot find implementation for interface {parameterType.FullName} in the registered assemblies");
					}

					parameterType = possibleTypes.First();
				}

				parameters.Add(this.FromObjectJson(parameter, parameterType));
				i++;
			}
		}



		var result = new RemoteRequest()
		{
			DelegateType = delegateType,
			Parameters = parameters,
			Target = target
		};

		return result;
	}

	public T? DeserializeRemoteResponse<T>(RemoteResponseDto remoteResponse)
	{
		if (remoteResponse?.Json == null)
		{
			return default;
		}

		return this.Deserialize<T>(remoteResponse.Json);
	}

	public object? FromObjectJson(ObjectJson? objectTypeJson, Type? targetType = null)
	{
		if (objectTypeJson == null)
		{
			return null;
		}

		targetType = targetType ?? this.serviceAssemblies.FindType(objectTypeJson.AssemblyType);
		ArgumentNullException.ThrowIfNull(targetType, nameof(objectTypeJson.AssemblyType));
		return this.Deserialize(objectTypeJson.Json, targetType);
	}

}


[Serializable]
public class MissingDelegateException : Exception
{
	public MissingDelegateException() { }
	public MissingDelegateException(string message) : base(message) { }
	public MissingDelegateException(string message, Exception inner) : base(message, inner) { }
}