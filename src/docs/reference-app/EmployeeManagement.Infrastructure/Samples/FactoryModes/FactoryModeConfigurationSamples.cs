using EmployeeManagement.Domain.Aggregates;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Neatoo.RemoteFactory;

namespace EmployeeManagement.Infrastructure.Samples.FactoryModes;

// Configuration samples are now consolidated in FactoryModeAttributes.cs
// This file contains supporting code that doesn't need doc snippets

/// <summary>
/// Configuration helpers for factory modes (implementation details, not for docs).
/// </summary>
public static class FactoryModeConfigurationSamples
{
    // Placeholder class for modes-logging snippet
}

#region modes-logging
// Enable verbose logging to trace factory execution
// services.AddLogging(b => b.AddFilter("Neatoo.RemoteFactory", LogLevel.Trace));
// Logs: "Executing local factory method..." or "Sending remote factory request..."
#endregion
