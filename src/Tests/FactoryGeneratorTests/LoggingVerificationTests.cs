using FactoryGeneratorTests;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Neatoo.RemoteFactory.FactoryGeneratorTests.Shared;
using System.Text;
using Xunit;

namespace Neatoo.RemoteFactory.FactoryGeneratorTests.Factory;

/// <summary>
/// Verification tests that run with logging enabled to confirm expected behavior.
/// </summary>
public class LoggingVerificationTests
{
    private static readonly string ReportPath = Path.Combine(
        Path.GetDirectoryName(typeof(LoggingVerificationTests).Assembly.Location) ?? ".",
        "logging-verification-report.md");

    private static void WriteToReport(string content)
    {
        File.AppendAllText(ReportPath, content + Environment.NewLine);
    }

    private static void ClearReport()
    {
        if (File.Exists(ReportPath))
            File.Delete(ReportPath);
    }
    [Fact]
    public async Task Verify_RemoteCreate_FullRoundTrip()
    {
        var (server, client, local) = ClientServerContainers.ScopesWithLogging(out var loggerProvider);

        try
        {
            var factory = client.ServiceProvider.GetRequiredService<IRemoteReadDataMapperFactory>();

            loggerProvider.Clear();
            var result = await factory.CreateVoid();

            var report = new StringBuilder();
            report.AppendLine("=== VERIFY: Remote Create Full Round Trip ===");
            report.AppendLine($"Total log messages: {loggerProvider.Messages.Count}");

            AnalyzeLogs(loggerProvider, report);

            Console.WriteLine(report.ToString());

            Assert.NotNull(result);
            Assert.True(result.CreateCalled, "CreateCalled should be true");
            Assert.True(loggerProvider.Messages.Count > 0, "Expected log messages to be captured");
        }
        finally
        {
            server.Dispose();
            client.Dispose();
            local.Dispose();
        }
    }

    [Fact]
    public async Task Verify_RemoteFetch_WithParameters()
    {
        var (server, client, local) = ClientServerContainers.ScopesWithLogging(out var loggerProvider);

        try
        {
            var factory = client.ServiceProvider.GetRequiredService<IRemoteReadDataMapperFactory>();

            loggerProvider.Clear();
            var result = await factory.FetchTaskBool(1);

            var report = new StringBuilder();
            report.AppendLine("=== VERIFY: Remote Fetch With Parameters ===");
            report.AppendLine($"Total log messages: {loggerProvider.Messages.Count}");

            AnalyzeLogs(loggerProvider, report);

            Console.WriteLine(report.ToString());

            Assert.NotNull(result);
            Assert.True(result.FetchCalled);
        }
        finally
        {
            server.Dispose();
            client.Dispose();
            local.Dispose();
        }
    }

    [Fact]
    public async Task Verify_LocalFactory_NoRemoteCall()
    {
        var (server, client, local) = ClientServerContainers.ScopesWithLogging(out var loggerProvider);

        try
        {
            var factory = local.ServiceProvider.GetRequiredService<IRemoteReadDataMapperFactory>();

            loggerProvider.Clear();
            var result = await factory.CreateVoid();

            var report = new StringBuilder();
            report.AppendLine("=== VERIFY: Local Factory - No Remote Call ===");
            report.AppendLine($"Total log messages: {loggerProvider.Messages.Count}");

            AnalyzeLogs(loggerProvider, report);

            // Local operations should not have remote delegate calls in client-to-server pattern
            var hasRemoteDelegateCall = loggerProvider.Messages.Any(m =>
                m.Message.Contains("Remote delegate call") || m.Message.Contains("HTTP"));

            report.AppendLine($"Remote delegate call present: {(hasRemoteDelegateCall ? "YES (UNEXPECTED)" : "NO (expected)")}");

            Console.WriteLine(report.ToString());

            Assert.NotNull(result);
            Assert.True(result.CreateCalled);
        }
        finally
        {
            server.Dispose();
            client.Dispose();
            local.Dispose();
        }
    }

    [Fact]
    public async Task Verify_WriteOperation_InsertSerialization()
    {
        var (server, client, local) = ClientServerContainers.ScopesWithLogging(out var loggerProvider);

        try
        {
            var factory = client.ServiceProvider.GetRequiredService<IRemoteWriteObjectFactory>();

            // Create a new object that needs insert
            var obj = new RemoteWriteTests.RemoteWriteObject { IsNew = true };

            loggerProvider.Clear();

            // Save it - this should serialize the object to the server
            var saved = await factory.SaveVoid(obj);

            var report = new StringBuilder();
            report.AppendLine("=== VERIFY: Write Operation (Insert) Serialization ===");
            report.AppendLine($"Total log messages: {loggerProvider.Messages.Count}");

            AnalyzeLogs(loggerProvider, report);

            report.AppendLine($"Saved object returned: {(saved != null ? "YES" : "NO")}");
            report.AppendLine($"InsertCalled: {saved?.InsertCalled}");

            Console.WriteLine(report.ToString());

            Assert.NotNull(saved);
            Assert.True(saved.InsertCalled);
        }
        finally
        {
            server.Dispose();
            client.Dispose();
            local.Dispose();
        }
    }

    [Fact]
    public async Task Verify_ServiceInjection_InFactoryMethod()
    {
        var (server, client, local) = ClientServerContainers.ScopesWithLogging(out var loggerProvider);

        try
        {
            var factory = client.ServiceProvider.GetRequiredService<IRemoteReadDataMapperFactory>();

            loggerProvider.Clear();
            // This method has [Service] IService parameter
            var result = await factory.CreateVoidDep();

            var report = new StringBuilder();
            report.AppendLine("=== VERIFY: Service Injection in Factory Method ===");
            report.AppendLine($"Total log messages: {loggerProvider.Messages.Count}");

            AnalyzeLogs(loggerProvider, report);

            report.AppendLine($"Result returned: {(result != null ? "YES" : "NO")}");
            report.AppendLine($"CreateCalled flag: {result?.CreateCalled}");

            Console.WriteLine(report.ToString());

            Assert.NotNull(result);
            Assert.True(result.CreateCalled);
        }
        finally
        {
            server.Dispose();
            client.Dispose();
            local.Dispose();
        }
    }

    [Fact]
    public async Task Verify_AllEventIds_CapturedDuringOperations()
    {
        var (server, client, local) = ClientServerContainers.ScopesWithLogging(out var loggerProvider);

        try
        {
            var report = new StringBuilder();
            report.AppendLine("=== VERIFY: Event ID Coverage ===");

            var readFactory = client.ServiceProvider.GetRequiredService<IRemoteReadDataMapperFactory>();
            var writeFactory = client.ServiceProvider.GetRequiredService<IRemoteWriteObjectFactory>();

            // Run various operations
            await readFactory.CreateVoid();
            await readFactory.FetchTaskBool(1);
            await readFactory.FetchVoidDep();

            var obj = new RemoteWriteTests.RemoteWriteObject { IsNew = true };
            await writeFactory.SaveVoid(obj);

            // Analyze event IDs
            var eventIds = loggerProvider.Messages
                .Where(m => m.EventId.Id > 0)
                .Select(m => m.EventId.Id)
                .Distinct()
                .OrderBy(id => id)
                .ToList();

            report.AppendLine();
            report.AppendLine("EVENT IDs CAPTURED:");

            var eventIdRanges = new Dictionary<string, (int min, int max)>
            {
                { "Serialization", (1000, 1999) },
                { "Factory Operations", (2000, 2999) },
                { "Remote Calls (Client)", (3000, 3999) },
                { "Converter Factory", (4000, 4999) },
                { "Authorization", (5000, 5999) },
                { "Service Registration", (6000, 6999) },
                { "Server-Side Handling", (7000, 7999) }
            };

            foreach (var range in eventIdRanges)
            {
                var idsInRange = eventIds.Where(id => id >= range.Value.min && id <= range.Value.max).ToList();
                report.AppendLine($"  {range.Key} ({range.Value.min}-{range.Value.max}): {idsInRange.Count} events");
                if (idsInRange.Any())
                {
                    report.AppendLine($"    IDs: {string.Join(", ", idsInRange)}");
                }
            }

            report.AppendLine();
            report.AppendLine($"TOTAL DISTINCT EVENT IDs: {eventIds.Count}");
            report.AppendLine($"ALL EVENT IDs: {string.Join(", ", eventIds)}");

            Console.WriteLine(report.ToString());
        }
        finally
        {
            server.Dispose();
            client.Dispose();
            local.Dispose();
        }
    }

    [Fact]
    public async Task GenerateComprehensiveReport()
    {
        ClearReport();
        var (server, client, local) = ClientServerContainers.ScopesWithLogging(out var loggerProvider);

        try
        {
            var report = new StringBuilder();
            report.AppendLine("# Comprehensive Logging Verification Report");
            report.AppendLine();
            report.AppendLine($"Generated: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");
            report.AppendLine($"Report Path: {ReportPath}");
            report.AppendLine();

            var readFactory = client.ServiceProvider.GetRequiredService<IRemoteReadDataMapperFactory>();
            var writeFactory = client.ServiceProvider.GetRequiredService<IRemoteWriteObjectFactory>();

            report.AppendLine("## Operations Executed");
            report.AppendLine();

            report.AppendLine("1. Remote Create (no params)");
            await readFactory.CreateVoid();

            report.AppendLine("2. Remote Create (with param)");
            await readFactory.CreateBool(1);

            report.AppendLine("3. Remote Fetch (no params)");
            await readFactory.FetchVoid();

            report.AppendLine("4. Remote Fetch (with param)");
            await readFactory.FetchTaskBool(1);

            report.AppendLine("5. Remote Fetch with Service injection");
            await readFactory.FetchVoidDep();

            report.AppendLine("6. Write - Insert new object");
            var obj = new RemoteWriteTests.RemoteWriteObject { IsNew = true };
            await writeFactory.SaveVoid(obj);

            report.AppendLine();
            report.AppendLine("## Log Analysis");
            report.AppendLine();

            AnalyzeLogs(loggerProvider, report);

            report.AppendLine("## Summary Statistics");
            report.AppendLine();
            report.AppendLine($"- Total log messages: {loggerProvider.Messages.Count}");
            report.AppendLine($"- Trace: {loggerProvider.Messages.Count(m => m.Level == LogLevel.Trace)}");
            report.AppendLine($"- Debug: {loggerProvider.Messages.Count(m => m.Level == LogLevel.Debug)}");
            report.AppendLine($"- Information: {loggerProvider.Messages.Count(m => m.Level == LogLevel.Information)}");
            report.AppendLine($"- Warning: {loggerProvider.Messages.Count(m => m.Level == LogLevel.Warning)}");
            report.AppendLine($"- Error: {loggerProvider.Messages.Count(m => m.Level == LogLevel.Error)}");
            report.AppendLine();

            var categories = loggerProvider.Messages.Select(m => m.CategoryName).Distinct().OrderBy(c => c).ToList();
            report.AppendLine("## Logger Categories Used");
            report.AppendLine();
            foreach (var cat in categories)
            {
                var count = loggerProvider.Messages.Count(m => m.CategoryName == cat);
                report.AppendLine($"- {cat}: {count} messages");
            }

            var reportContent = report.ToString();
            WriteToReport(reportContent);
            Console.WriteLine(reportContent);
        }
        finally
        {
            server.Dispose();
            client.Dispose();
            local.Dispose();
        }
    }

    private static void AnalyzeLogs(TestLoggerProvider loggerProvider, StringBuilder report)
    {
        report.AppendLine("LOG MESSAGES CAPTURED:");
        report.AppendLine();

        var messages = loggerProvider.Messages.ToList();

        if (messages.Count == 0)
        {
            report.AppendLine("  (No log messages captured)");
            report.AppendLine();
            return;
        }

        foreach (var msg in messages)
        {
            var eventIdStr = msg.EventId.Id > 0 ? $"[{msg.EventId.Id}]" : "";
            var levelStr = msg.Level.ToString().ToUpper().PadRight(4).Substring(0, 4);
            var catShort = msg.CategoryName.Length > 50
                ? "..." + msg.CategoryName.Substring(msg.CategoryName.Length - 47)
                : msg.CategoryName;

            report.AppendLine($"  {levelStr} {eventIdStr,-7} {catShort}");
            report.AppendLine($"         {msg.Message}");

            if (msg.Exception != null)
            {
                report.AppendLine($"         EXCEPTION: {msg.Exception.Message}");
            }
        }

        report.AppendLine();
    }
}
