using System.Collections.Generic;
using ModHarmony.Content.Config;

namespace ModHarmony.Common.Detection;

/// <summary>
/// A pluggable conflict detector. Detectors are pure analysis passes over a
/// <see cref="DetectorContext"/>: they return conflicts and never modify the game.
/// New detectors can be added by implementing this interface and registering the
/// implementation in <see cref="DetectorManager"/>. Each detector runs isolated —
/// a throw inside one detector cannot stop the others.
/// </summary>
public interface IConflictDetector
{
	/// <summary>Stable detector id (e.g. "HookOverlap"). Used in conflict ids and reports.</summary>
	string Id { get; }

	/// <summary>Localization key suffix for the detector's display name (under Mods.ModHarmony.Detectors.).</summary>
	string NameKey { get; }

	/// <summary>Localization key suffix for the detector's description.</summary>
	string DescriptionKey { get; }

	/// <summary>Whether this detector should run based on the current configuration.</summary>
	bool IsEnabled(ModHarmonyConfig config);

	/// <summary>Runs the analysis and returns detected conflicts (may be empty).</summary>
	List<Common.Core.Conflict> Detect(DetectorContext context);
}
