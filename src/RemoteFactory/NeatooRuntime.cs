using System.Diagnostics.CodeAnalysis;

namespace Neatoo.RemoteFactory;

/// <summary>
/// Runtime feature switches for IL trimming support.
/// When <see cref="IsServerRuntime"/> is set to false via RuntimeHostConfigurationOption,
/// the IL trimmer treats guarded server-only code paths as dead code and removes them.
/// </summary>
public static class NeatooRuntime
{
#if NET9_0_OR_GREATER
	[FeatureSwitchDefinition("Neatoo.RemoteFactory.IsServerRuntime")]
#endif
	public static bool IsServerRuntime =>
		AppContext.TryGetSwitch("Neatoo.RemoteFactory.IsServerRuntime", out bool isEnabled)
			? isEnabled
			: true; // Default: server runtime (no behavioral change without explicit opt-in)
}
