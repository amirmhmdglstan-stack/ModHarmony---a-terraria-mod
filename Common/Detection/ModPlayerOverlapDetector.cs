using System.Collections.Generic;
using ModHarmony.Common.Core;
using ModHarmony.Content.Config;

namespace ModHarmony.Common.Detection;

/// <summary>
/// Detects overlapping ModPlayer systems: multiple mods attaching behavior to
/// the player (stat changes, damage handling, update loops) through the same
/// ModPlayer hooks.
/// </summary>
public sealed class ModPlayerOverlapDetector : IConflictDetector
{
	public string Id => "ModPlayerOverlap";
	public string NameKey => "Detectors.ModPlayerOverlap.Name";
	public string DescriptionKey => "Detectors.ModPlayerOverlap.Description";

	public bool IsEnabled(ModHarmonyConfig config) => config.ScanGlobalClasses;

	public List<Conflict> Detect(DetectorContext context)
	{
		var options = new OverlapAnalyzer.Options {
			DetectorId = Id,
			BaseTypes = new[] { "ModPlayer" },
			Confidence = Confidence.Strong,
			DefaultSeverity = Severity.Low,
			EvidenceModHooksKey = "ModPlayerOverlap.ModHooks",
			EvidenceAggregateKey = "ModPlayerOverlap.Aggregate",
			EvidenceWhyKey = "ModPlayerOverlap.Why",
			SeverityBySystem = new Dictionary<string, Severity> {
				{ "player.damage", Severity.Medium },
				{ "item.damage", Severity.Medium },
				{ "player.stats", Severity.Low },
				{ "player.update", Severity.Low },
				{ "player.inventory", Severity.Low },
				{ "player.save", Severity.Low },
				{ "player.lifecycle", Severity.Low },
				{ "item.use", Severity.Low },
				{ "player.draw", Severity.Info }
			}
		};

		return OverlapAnalyzer.Analyze(context, options);
	}
}
