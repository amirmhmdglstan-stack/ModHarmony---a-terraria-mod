using ModHarmony.Common.Core;

// Test-only stubs. These mirror the small surface of ModHarmony's own types
// that the pure detection code references, so the harness can run without the
// full mod class hierarchy (which would drag in the game UI). The real types
// are compiled in the main ModHarmony project.

namespace ModHarmony
{
	/// <summary>Stub of the mod entry class — only the constant is needed by the detection code.</summary>
	public sealed class ModHarmony
	{
		public const string ModName = "ModHarmony";
	}
}

namespace ModHarmony.Content.Config
{
	/// <summary>Stub of ModHarmonyConfig — same field surface the detectors read.</summary>
	public class ModHarmonyConfig
	{
		public bool EnableModHarmony = true;
		public bool EnableDiagnostics = true;
		public bool RunStartupScan = true;
		public bool RuntimeMonitoring;
		public LogLevelSetting LogLevel = LogLevelSetting.Info;
		public bool SafeDiagnosticsMode;
		public bool ScanHooks = true;
		public bool ScanGlobalClasses = true;
		public bool ScanRecipes = true;
		public bool ScanAssets = true;
		public bool ScanDependencies = true;
		public bool ScanILHooks = true;
		public int MaxRetainedEvents = 200;
		public int FrameSpikeThresholdMs = 33;
		public bool EnableArbitration;
		public ArbitrationStrategy DefaultStrategy = ArbitrationStrategy.Disabled;
		public int RandomSeed;
		public bool PersistDecisions = true;
		public bool ShowInformational;
		public bool ShowLowRisk = true;
		public bool DeveloperMode;
		public bool CompactMode;

		public bool ArbitrationActive => EnableArbitration && !SafeDiagnosticsMode;
	}

	public enum LogLevelSetting
	{
		Trace,
		Debug,
		Info,
		Warn,
		Error
	}
}
