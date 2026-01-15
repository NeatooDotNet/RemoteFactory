// src/Generator/Builder/FactoryModelBuilder.cs
// Transforms TypeInfo (from transform phase) into model types (for rendering phase).

#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using Neatoo.RemoteFactory.FactoryGenerator;
using Neatoo.RemoteFactory.Generator.Model;
using static Neatoo.Factory;

namespace Neatoo.RemoteFactory.Generator.Builder;

/// <summary>
/// Transforms TypeInfo extracted from source code into model types for code generation.
/// </summary>
internal static class FactoryModelBuilder
{
    /// <summary>
    /// Builds a FactoryGenerationUnit from TypeInfo.
    /// </summary>
    public static FactoryGenerationUnit Build(TypeInfo typeInfo)
    {
        if (typeInfo.IsStatic)
        {
            return BuildStaticFactory(typeInfo);
        }
        else if (typeInfo.IsInterface)
        {
            return BuildInterfaceFactory(typeInfo);
        }
        else
        {
            return BuildClassFactory(typeInfo);
        }
    }

    /// <summary>
    /// Builds a FactoryGenerationUnit for a static class with [Execute] or [Event] methods.
    /// </summary>
    private static FactoryGenerationUnit BuildStaticFactory(TypeInfo typeInfo)
    {
        var delegates = new List<ExecuteDelegateModel>();
        var events = new List<EventMethodModel>();

        foreach (var method in typeInfo.FactoryMethods)
        {
            if (method.FactoryOperation == FactoryOperation.Event)
            {
                events.Add(BuildEventMethod(method, typeInfo.ImplementationTypeName, isStaticClass: true));
            }
            else if (method.FactoryOperation == FactoryOperation.Execute)
            {
                delegates.Add(BuildExecuteDelegate(method));
            }
        }

        var staticFactory = new StaticFactoryModel(
            typeName: typeInfo.Name,
            signatureText: typeInfo.SignatureText,
            isPartial: typeInfo.IsPartial,
            delegates: delegates,
            events: events);

        return new FactoryGenerationUnit(
            @namespace: typeInfo.Namespace,
            usings: typeInfo.UsingStatements.ToList(),
            mode: typeInfo.FactoryMode,
            hintName: typeInfo.SafeHintName,
            diagnostics: typeInfo.Diagnostics.ToList(),
            staticFactory: staticFactory);
    }

    /// <summary>
    /// Builds a FactoryGenerationUnit for an interface with [Factory] attribute.
    /// </summary>
    private static FactoryGenerationUnit BuildInterfaceFactory(TypeInfo typeInfo)
    {
        var methods = new List<InterfaceMethodModel>();
        var methodNames = new List<string>();

        foreach (var method in typeInfo.FactoryMethods)
        {
            var interfaceMethod = BuildInterfaceMethod(method, typeInfo);
            var uniqueName = AssignUniqueName(interfaceMethod.Name, methodNames);

            // Create new model with unique name if needed
            if (uniqueName != interfaceMethod.Name)
            {
                interfaceMethod = new InterfaceMethodModel(
                    name: interfaceMethod.Name,
                    uniqueName: uniqueName,
                    returnType: interfaceMethod.ReturnType,
                    serviceType: interfaceMethod.ServiceType,
                    implementationType: interfaceMethod.ImplementationType,
                    operation: interfaceMethod.Operation,
                    isRemote: interfaceMethod.IsRemote,
                    isTask: interfaceMethod.IsTask,
                    isAsync: interfaceMethod.IsAsync,
                    isNullable: interfaceMethod.IsNullable,
                    parameters: interfaceMethod.Parameters,
                    authorization: interfaceMethod.Authorization);
            }

            methods.Add(interfaceMethod);
            methodNames.Add(uniqueName);
        }

        var interfaceFactory = new InterfaceFactoryModel(
            serviceTypeName: typeInfo.ServiceTypeName,
            implementationTypeName: typeInfo.ImplementationTypeName,
            methods: methods);

        return new FactoryGenerationUnit(
            @namespace: typeInfo.Namespace,
            usings: typeInfo.UsingStatements.ToList(),
            mode: typeInfo.FactoryMode,
            hintName: typeInfo.SafeHintName,
            diagnostics: typeInfo.Diagnostics.ToList(),
            interfaceFactory: interfaceFactory);
    }

    /// <summary>
    /// Builds a FactoryGenerationUnit for a class with [Factory] attribute.
    /// </summary>
    private static FactoryGenerationUnit BuildClassFactory(TypeInfo typeInfo)
    {
        var factoryMethods = new List<FactoryMethodModel>();
        var events = new List<EventMethodModel>();
        var writeMethodsForGrouping = new List<WriteMethodModel>();

        // First pass: separate events and build initial method list
        foreach (var method in typeInfo.FactoryMethods)
        {
            if (method.FactoryOperation == FactoryOperation.Event)
            {
                events.Add(BuildEventMethod(method, typeInfo.ImplementationTypeName, isStaticClass: false));
                continue;
            }

            if (method.IsSave)
            {
                var writeMethod = BuildWriteMethod(method, typeInfo);
                writeMethodsForGrouping.Add(writeMethod);
                factoryMethods.Add(writeMethod);
            }
            else
            {
                factoryMethods.Add(BuildReadMethod(method, typeInfo));
            }
        }

        // Group write methods by parameter signature to create SaveMethodModels
        var saveMethods = BuildSaveMethods(writeMethodsForGrouping, typeInfo);
        factoryMethods.AddRange(saveMethods);

        // Add CanXxx methods for methods with authorization (when auth doesn't have target param)
        AddCanMethods(factoryMethods, typeInfo);

        // Assign unique names to methods with same name but different signatures
        AssignUniqueNames(factoryMethods);

        // Determine if there's a default Save method (no additional parameters beyond target)
        var hasDefaultSave = saveMethods.Any(s => s.IsDefault);

        // Determine if entity registration is needed
        var requiresEntityRegistration = factoryMethods.OfType<ReadMethodModel>()
            .Any(f => !f.IsConstructor && !f.IsStaticFactory);

        // Determine if ordinal converter should be registered
        var registerOrdinalConverter = typeInfo.OrdinalProperties.Any() &&
            typeInfo.IsPartial &&
            !typeInfo.IsNested &&
            (!typeInfo.IsRecord || typeInfo.HasPrimaryConstructor) &&
            !typeInfo.RequiresServiceInstantiation;

        // Build ordinal serialization model if applicable
        OrdinalSerializationModel? ordinalSerialization = null;
        if (registerOrdinalConverter)
        {
            ordinalSerialization = BuildOrdinalSerializationModel(typeInfo);
        }

        var classFactory = new ClassFactoryModel(
            typeName: typeInfo.Name,
            serviceTypeName: typeInfo.ServiceTypeName,
            implementationTypeName: typeInfo.ImplementationTypeName,
            isPartial: typeInfo.IsPartial,
            methods: factoryMethods,
            events: events,
            ordinalSerialization: ordinalSerialization,
            hasDefaultSave: hasDefaultSave,
            requiresEntityRegistration: requiresEntityRegistration,
            registerOrdinalConverter: registerOrdinalConverter);

        return new FactoryGenerationUnit(
            @namespace: typeInfo.Namespace,
            usings: typeInfo.UsingStatements.ToList(),
            mode: typeInfo.FactoryMode,
            hintName: typeInfo.SafeHintName,
            diagnostics: typeInfo.Diagnostics.ToList(),
            classFactory: classFactory);
    }

    #region Method Builders

    private static ReadMethodModel BuildReadMethod(TypeFactoryMethodInfo method, TypeInfo typeInfo)
    {
        var parameters = BuildParameters(method.Parameters);
        var authorization = BuildAuthorization(method);

        // Calculate derived properties
        var isRemote = method.IsRemote ||
                       method.AuthMethodInfos.Any(m => m.IsRemote) ||
                       method.AspAuthorizeCalls.Any();
        var isTask = isRemote ||
                     method.IsTask ||
                     method.AuthMethodInfos.Any(m => m.IsTask) ||
                     method.AspAuthorizeCalls.Any();
        var isAsync = (authorization != null && authorization.HasAuth && method.IsTask) ||
                      method.AuthMethodInfos.Any(m => m.IsTask) ||
                      method.AspAuthorizeCalls.Any();
        var isNullable = method.IsNullable ||
                         (authorization != null && authorization.HasAuth) ||
                         method.IsBool;

        return new ReadMethodModel(
            name: method.Name,
            uniqueName: method.Name,
            returnType: typeInfo.ServiceTypeName,
            serviceType: typeInfo.ServiceTypeName,
            implementationType: typeInfo.ImplementationTypeName,
            operation: method.FactoryOperation,
            isRemote: isRemote,
            isTask: isTask,
            isAsync: isAsync,
            isNullable: isNullable,
            parameters: parameters,
            authorization: authorization,
            isConstructor: method.IsConstructor,
            isStaticFactory: method.IsStaticFactory,
            isBool: method.IsBool);
    }

    private static WriteMethodModel BuildWriteMethod(TypeFactoryMethodInfo method, TypeInfo typeInfo)
    {
        // Add target parameter as first parameter for write methods
        var allParameters = new List<ParameterModel>
        {
            new ParameterModel(
                name: "target",
                type: typeInfo.ServiceTypeName,
                isService: false,
                isTarget: true)
        };
        allParameters.AddRange(BuildParameters(method.Parameters));

        var authorization = BuildAuthorization(method);

        // Calculate derived properties
        var isRemote = method.IsRemote ||
                       method.AuthMethodInfos.Any(m => m.IsRemote) ||
                       method.AspAuthorizeCalls.Any();
        var isTask = isRemote ||
                     method.IsTask ||
                     method.AuthMethodInfos.Any(m => m.IsTask) ||
                     method.AspAuthorizeCalls.Any();
        var isAsync = (authorization != null && authorization.HasAuth && method.IsTask) ||
                      method.AuthMethodInfos.Any(m => m.IsTask) ||
                      method.AspAuthorizeCalls.Any();
        var isNullable = method.IsNullable ||
                         (authorization != null && authorization.HasAuth) ||
                         method.IsBool;

        return new WriteMethodModel(
            name: method.Name,
            uniqueName: method.Name,
            returnType: typeInfo.ServiceTypeName,
            serviceType: typeInfo.ServiceTypeName,
            implementationType: typeInfo.ImplementationTypeName,
            operation: method.FactoryOperation,
            isRemote: isRemote,
            isTask: isTask,
            isAsync: isAsync,
            isNullable: isNullable,
            parameters: allParameters,
            authorization: authorization);
    }

    private static InterfaceMethodModel BuildInterfaceMethod(TypeFactoryMethodInfo method, TypeInfo typeInfo)
    {
        var parameters = BuildParameters(method.Parameters);
        var authorization = BuildAuthorization(method, aspForbid: true);

        // Interface methods are always remote
        var isRemote = true;
        var isTask = method.IsTask ||
                     method.AuthMethodInfos.Any(m => m.IsTask) ||
                     method.AspAuthorizeCalls.Any();
        var isAsync = method.AuthMethodInfos.Any(m => m.IsTask) ||
                      method.AspAuthorizeCalls.Any();

        return new InterfaceMethodModel(
            name: method.Name,
            uniqueName: method.Name,
            returnType: method.ReturnType ?? typeInfo.ServiceTypeName,
            serviceType: typeInfo.ServiceTypeName,
            implementationType: typeInfo.ImplementationTypeName,
            operation: method.FactoryOperation,
            isRemote: isRemote,
            isTask: isTask,
            isAsync: isAsync,
            isNullable: method.IsNullable,
            parameters: parameters,
            authorization: authorization);
    }

    private static EventMethodModel BuildEventMethod(TypeFactoryMethodInfo method, string containingTypeName, bool isStaticClass)
    {
        var parameters = method.Parameters
            .Where(p => !p.IsService)
            .Select(p => new ParameterModel(p.Name, p.Type, p.IsService, p.IsTarget, p.IsCancellationToken, p.IsParams))
            .ToList();

        var serviceParameters = method.Parameters
            .Where(p => p.IsService)
            .Select(p => new ParameterModel(p.Name, p.Type, p.IsService, p.IsTarget, p.IsCancellationToken, p.IsParams))
            .ToList();

        var delegateName = method.Name;
        if (delegateName.StartsWith("On"))
        {
            delegateName = delegateName.Substring(2);
        }
        if (delegateName.StartsWith("_"))
        {
            delegateName = delegateName.Substring(1);
        }

        return new EventMethodModel(
            name: method.Name,
            delegateName: delegateName + "Delegate",
            isAsync: method.IsTask,
            parameters: parameters,
            serviceParameters: serviceParameters,
            containingTypeName: containingTypeName,
            isStaticClass: isStaticClass);
    }

    private static ExecuteDelegateModel BuildExecuteDelegate(TypeFactoryMethodInfo method)
    {
        var parameters = method.Parameters
            .Where(p => !p.IsService && !p.IsCancellationToken)
            .Select(p => new ParameterModel(p.Name, p.Type, p.IsService, p.IsTarget, p.IsCancellationToken, p.IsParams))
            .ToList();

        var serviceParameters = method.Parameters
            .Where(p => p.IsService)
            .Select(p => new ParameterModel(p.Name, p.Type, p.IsService, p.IsTarget, p.IsCancellationToken, p.IsParams))
            .ToList();

        var delegateName = method.Name;
        if (delegateName.StartsWith("Execute"))
        {
            delegateName = delegateName.Substring("Execute".Length);
        }
        if (delegateName.StartsWith("_"))
        {
            delegateName = delegateName.Substring(1);
        }

        // Extract return type from Task<T> if present
        var returnType = method.ReturnType ?? "void";

        return new ExecuteDelegateModel(
            name: method.Name,
            delegateName: delegateName + "Delegate",
            returnType: returnType,
            isNullable: method.IsNullable,
            parameters: parameters,
            serviceParameters: serviceParameters);
    }

    private static CanMethodModel BuildCanMethod(string methodName, TypeInfo typeInfo, IReadOnlyList<AuthMethodCall> authMethods, IReadOnlyList<AspAuthorizeCall> aspAuthorize)
    {
        // Collect parameters from auth methods
        var parameters = new List<ParameterModel>();
        foreach (var authMethod in authMethods)
        {
            foreach (var param in authMethod.Parameters)
            {
                if (!parameters.Any(p => p.Name == param.Name && p.Type == param.Type))
                {
                    parameters.Add(param);
                }
            }
        }

        var isTask = authMethods.Any(m => m.IsTask) || aspAuthorize.Count > 0;
        var isRemote = aspAuthorize.Count > 0;
        var isAsync = authMethods.Any(m => m.IsTask) || aspAuthorize.Count > 0;

        var authorization = new AuthorizationModel(
            authMethods: authMethods,
            aspAuthorize: aspAuthorize,
            aspForbid: false);

        return new CanMethodModel(
            name: $"Can{methodName}",
            uniqueName: $"Can{methodName}",
            returnType: "Authorized",
            serviceType: typeInfo.ServiceTypeName,
            implementationType: typeInfo.ImplementationTypeName,
            operation: FactoryOperation.None,
            isRemote: isRemote,
            isTask: isTask,
            isAsync: isAsync,
            isNullable: false,
            parameters: parameters,
            authorization: authorization);
    }

    #endregion

    #region Save Method Grouping

    private static List<SaveMethodModel> BuildSaveMethods(List<WriteMethodModel> writeMethods, TypeInfo typeInfo)
    {
        var saveMethods = new List<SaveMethodModel>();

        // Group write methods by parameter signature (excluding target, service, and cancellation token)
        var grouped = writeMethods
            .GroupBy(m => GetParameterSignature(m.Parameters))
            .ToList();

        string? nameOverride = grouped.Count == 1 ? "Save" : null;

        foreach (var group in grouped)
        {
            var methodsInGroup = group.ToList();

            // Check for multiple methods of same operation type within the group
            var insertMethods = methodsInGroup.Where(m => m.Operation == FactoryOperation.Insert).ToList();
            var updateMethods = methodsInGroup.Where(m => m.Operation == FactoryOperation.Update).ToList();
            var deleteMethods = methodsInGroup.Where(m => m.Operation == FactoryOperation.Delete).ToList();

            if (insertMethods.Count > 1 || updateMethods.Count > 1 || deleteMethods.Count > 1)
            {
                // Group by name postfix when there are conflicts
                var byNamePostfix = methodsInGroup.GroupBy(m => GetNamePostfix(m.Name, m.Operation)).ToList();
                foreach (var nameGroup in byNamePostfix)
                {
                    saveMethods.Add(BuildSaveMethodFromGroup(nameGroup.ToList(), typeInfo, nameOverride));
                }
            }
            else
            {
                saveMethods.Add(BuildSaveMethodFromGroup(methodsInGroup, typeInfo, nameOverride));
            }
        }

        // Mark the default save method (no additional parameters beyond target)
        var defaultSave = saveMethods
            .FirstOrDefault(s => s.Parameters.Count(p => !p.IsTarget && !p.IsService && !p.IsCancellationToken) == 0 &&
                                 s.Parameters.Any(p => p.IsTarget));
        if (defaultSave != null)
        {
            // Create new SaveMethodModel with IsDefault = true
            var index = saveMethods.IndexOf(defaultSave);
            saveMethods[index] = new SaveMethodModel(
                name: defaultSave.Name,
                uniqueName: defaultSave.UniqueName,
                returnType: defaultSave.ReturnType,
                serviceType: defaultSave.ServiceType,
                implementationType: defaultSave.ImplementationType,
                operation: defaultSave.Operation,
                isRemote: defaultSave.IsRemote,
                isTask: defaultSave.IsTask,
                isAsync: defaultSave.IsAsync,
                isNullable: defaultSave.IsNullable,
                parameters: defaultSave.Parameters,
                authorization: defaultSave.Authorization,
                insertMethod: defaultSave.InsertMethod,
                updateMethod: defaultSave.UpdateMethod,
                deleteMethod: defaultSave.DeleteMethod,
                isDefault: true);
        }

        return saveMethods;
    }

    private static SaveMethodModel BuildSaveMethodFromGroup(List<WriteMethodModel> methods, TypeInfo typeInfo, string? nameOverride)
    {
        var insertMethod = methods.FirstOrDefault(m => m.Operation == FactoryOperation.Insert);
        var updateMethod = methods.FirstOrDefault(m => m.Operation == FactoryOperation.Update);
        var deleteMethod = methods.FirstOrDefault(m => m.Operation == FactoryOperation.Delete);

        // Use the method with highest operation priority for naming
        var primaryMethod = methods.OrderByDescending(m => (int)m.Operation).First();
        var namePostfix = GetNamePostfix(primaryMethod.Name, primaryMethod.Operation);
        var name = nameOverride ?? $"Save{namePostfix}";

        // Merge parameters from first method (they should all have same signature)
        var parameters = methods.First().Parameters;

        // Merge authorization from all methods
        var allAuthMethods = methods
            .Where(m => m.Authorization != null)
            .SelectMany(m => m.Authorization!.AuthMethods)
            .Distinct()
            .ToList();
        var allAspAuthorize = methods
            .Where(m => m.Authorization != null)
            .SelectMany(m => m.Authorization!.AspAuthorize)
            .Distinct()
            .ToList();

        AuthorizationModel? authorization = null;
        if (allAuthMethods.Count > 0 || allAspAuthorize.Count > 0)
        {
            authorization = new AuthorizationModel(allAuthMethods, allAspAuthorize, aspForbid: false);
        }

        // Calculate combined properties
        var isRemote = methods.Any(m => m.IsRemote);
        var isTask = isRemote || methods.Any(m => m.IsTask);
        var isAsync = methods.Any(m => m.IsAsync);
        var isNullable = methods.Any(m => m.Operation == FactoryOperation.Delete || m.IsNullable);
        var hasAuth = methods.Any(m => m.HasAuth);

        return new SaveMethodModel(
            name: name,
            uniqueName: name,
            returnType: typeInfo.ServiceTypeName,
            serviceType: typeInfo.ServiceTypeName,
            implementationType: typeInfo.ImplementationTypeName,
            operation: FactoryOperation.None, // Save is a composite operation
            isRemote: isRemote,
            isTask: isTask,
            isAsync: isAsync,
            isNullable: isNullable,
            parameters: parameters,
            authorization: authorization,
            insertMethod: insertMethod,
            updateMethod: updateMethod,
            deleteMethod: deleteMethod,
            isDefault: false);
    }

    private static string GetParameterSignature(IReadOnlyList<ParameterModel> parameters)
    {
        var relevantParams = parameters
            .Where(p => !p.IsTarget && !p.IsService && !p.IsCancellationToken)
            .Select(p => p.Type);
        return string.Join(",", relevantParams);
    }

    private static string GetNamePostfix(string methodName, FactoryOperation operation)
    {
        var opName = operation.ToString();
        if (methodName.StartsWith(opName))
        {
            return methodName.Substring(opName.Length);
        }
        return methodName;
    }

    #endregion

    #region CanMethod Generation

    private static void AddCanMethods(List<FactoryMethodModel> methods, TypeInfo typeInfo)
    {
        var canMethodsToAdd = new List<CanMethodModel>();
        var existingMethodNames = new HashSet<string>(methods.Select(m => m.Name));

        foreach (var method in methods)
        {
            if (!method.HasAuth)
            {
                continue;
            }

            // Only add CanXxx if auth methods don't have a target parameter
            var authMethodsHaveTarget = method.Authorization?.AuthMethods
                .Any(am => am.Parameters.Any(p => p.IsTarget)) ?? false;

            if (authMethodsHaveTarget)
            {
                continue;
            }

            var canMethodName = $"Can{method.Name}";
            if (existingMethodNames.Contains(canMethodName))
            {
                continue;
            }

            var canMethod = BuildCanMethod(
                method.Name,
                typeInfo,
                method.Authorization?.AuthMethods ?? Array.Empty<AuthMethodCall>(),
                method.Authorization?.AspAuthorize ?? Array.Empty<AspAuthorizeCall>());

            canMethodsToAdd.Add(canMethod);
            existingMethodNames.Add(canMethodName);
        }

        methods.AddRange(canMethodsToAdd);
    }

    #endregion

    #region Unique Name Assignment

    private static void AssignUniqueNames(List<FactoryMethodModel> methods)
    {
        var methodNames = new List<string>();

        // Sort by parameter count to ensure consistent naming
        var sortedMethods = methods.OrderBy(m => m.Parameters.Count).ToList();

        for (int i = 0; i < sortedMethods.Count; i++)
        {
            var method = sortedMethods[i];
            var uniqueName = AssignUniqueName(method.Name, methodNames);

            if (uniqueName != method.UniqueName)
            {
                // Replace the method with updated unique name
                var index = methods.IndexOf(method);
                methods[index] = CreateMethodWithUniqueName(method, uniqueName);
            }

            methodNames.Add(uniqueName);
        }
    }

    private static string AssignUniqueName(string name, List<string> existingNames)
    {
        if (!existingNames.Contains(name))
        {
            return name;
        }

        int count = 1;
        while (existingNames.Contains($"{name}{count}"))
        {
            count++;
        }
        return $"{name}{count}";
    }

    private static FactoryMethodModel CreateMethodWithUniqueName(FactoryMethodModel method, string uniqueName)
    {
        return method switch
        {
            ReadMethodModel rm => new ReadMethodModel(
                rm.Name, uniqueName, rm.ReturnType, rm.ServiceType, rm.ImplementationType, rm.Operation,
                rm.IsRemote, rm.IsTask, rm.IsAsync, rm.IsNullable, rm.Parameters, rm.Authorization,
                rm.IsConstructor, rm.IsStaticFactory, rm.IsBool),
            WriteMethodModel wm => new WriteMethodModel(
                wm.Name, uniqueName, wm.ReturnType, wm.ServiceType, wm.ImplementationType, wm.Operation,
                wm.IsRemote, wm.IsTask, wm.IsAsync, wm.IsNullable, wm.Parameters, wm.Authorization),
            SaveMethodModel sm => new SaveMethodModel(
                sm.Name, uniqueName, sm.ReturnType, sm.ServiceType, sm.ImplementationType, sm.Operation,
                sm.IsRemote, sm.IsTask, sm.IsAsync, sm.IsNullable, sm.Parameters, sm.Authorization,
                sm.InsertMethod, sm.UpdateMethod, sm.DeleteMethod, sm.IsDefault),
            CanMethodModel cm => new CanMethodModel(
                cm.Name, uniqueName, cm.ReturnType, cm.ServiceType, cm.ImplementationType, cm.Operation,
                cm.IsRemote, cm.IsTask, cm.IsAsync, cm.IsNullable, cm.Parameters, cm.Authorization),
            _ => method
        };
    }

    #endregion

    #region Parameter and Authorization Builders

    private static IReadOnlyList<ParameterModel> BuildParameters(IEnumerable<MethodParameterInfo> parameters)
    {
        return parameters
            .Select(p => new ParameterModel(
                name: p.Name,
                type: p.Type,
                isService: p.IsService,
                isTarget: p.IsTarget,
                isCancellationToken: p.IsCancellationToken,
                isParams: p.IsParams))
            .ToList();
    }

    private static AuthorizationModel? BuildAuthorization(TypeFactoryMethodInfo method, bool aspForbid = false)
    {
        var authMethods = method.AuthMethodInfos
            .Select(am => new AuthMethodCall(
                className: am.ClassName,
                methodName: am.Name,
                isTask: am.IsTask,
                parameters: BuildParameters(am.Parameters)))
            .ToList();

        var aspAuthorize = method.AspAuthorizeCalls
            .Select(asp => new AspAuthorizeCall(
                constructorArgs: asp.ConstructorArguments.ToList(),
                namedArgs: asp.NamedArguments.ToList()))
            .ToList();

        if (authMethods.Count == 0 && aspAuthorize.Count == 0)
        {
            return null;
        }

        return new AuthorizationModel(authMethods, aspAuthorize, aspForbid);
    }

    #endregion

    #region Ordinal Serialization

    private static OrdinalSerializationModel BuildOrdinalSerializationModel(TypeInfo typeInfo)
    {
        var properties = typeInfo.OrdinalProperties
            .Select(p => new OrdinalPropertyModel(
                name: p.Name,
                type: p.Type,
                isNullable: p.IsNullable))
            .ToList();

        var constructorParameterNames = typeInfo.PrimaryConstructorParameterNames.ToList();

        return new OrdinalSerializationModel(
            typeName: typeInfo.ImplementationTypeName,
            fullTypeName: $"{typeInfo.Namespace}.{typeInfo.ImplementationTypeName}",
            @namespace: typeInfo.Namespace,
            isRecord: typeInfo.IsRecord,
            hasPrimaryConstructor: typeInfo.HasPrimaryConstructor,
            properties: properties,
            constructorParameterNames: constructorParameterNames,
            usings: typeInfo.UsingStatements.ToList());
    }

    #endregion
}
