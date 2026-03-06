using Neatoo.RemoteFactory;

namespace RemoteFactory.TrimmingTests;

/// <summary>
/// Tests that the feature switch constant folding works directly.
/// If the trimmer constant-folds NeatooRuntime.IsServerRuntime to false,
/// then the ServerOnlyDirect class should be trimmed away.
/// </summary>
public static class DirectFeatureSwitchTest
{
    public static void Run()
    {
        if (NeatooRuntime.IsServerRuntime)
        {
            // This code path should be dead when IsServerRuntime is constant-folded to false.
            var helper = new ServerOnlyDirect();
            Console.WriteLine(helper.Marker);
        }
        else
        {
            Console.WriteLine("Client mode - server-only code trimmed.");
        }
    }
}

/// <summary>
/// A type that is only referenced inside the NeatooRuntime.IsServerRuntime guard.
/// If constant folding works, this type should be entirely removed by the trimmer.
/// The "ServerOnlyDirect_MARKER" string should be absent from the trimmed output.
/// </summary>
public class ServerOnlyDirect
{
    public string Marker => "ServerOnlyDirect_MARKER";
}
