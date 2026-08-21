using System.Collections.Generic;
using ModHarmony.Common.Core;
using ModHarmony.Content.Config;

namespace ModHarmony.Common.Detection;

/// <summary>
/// Detects overlapping ModSystem usage: multiple mods registering systems that
/// run update loops, world generation, UI layers or recipe phases. Heavy
/// overlap on world generation or recipe modification is a genuine risk;
/// overlapping UpdateUI or update loops is common and mostly harmless — the
/// severity table reflects that.
/// </summary>
public sealed class ModSystemOverlapDetector : IConflictDetector
{
	public string Id => "ModSystemOverlap";
	public string NameKey => "Detectors.ModSystemOverlap.Name";
	public string DescriptionKey => "Detectors.ModSystemOverlap.Description";

	public bool IsEnabled(ModHarmonyConfig config) => config.ScanGlobalClasses;

	public List<Conflict> Detect(DetectorContext context)
	{
		var options = new OverlapAnalyzer.Options {
			DetectorId = Id,
			BaseTypes = new[] { "ModSystem" },
			Confidence = Confidence.Strong,
			DefaultSeverity = Severity.Low,
			EvidenceModHooksKey = "ModSystemOverlap.ModHooks",
			EvidenceAggregateKey = "ModSystemOverlap.Aggregate",
			EvidenceWhyKey = "ModSystemOverlap.Why",
			SeverityBySystem = new Dictionary<string, Severity> {
				{ "world.gen", Severity.Medium },
				{ "recipe.add", Severity.Low },
				{ "recipe.modify", Severity.Medium },
				{ "net.hijack", Severity.Medium },
				{ "world.update", Severity.Low },
				{ "world.save", Severity.Low },
				{ "world.time", Severity.Low },
				{ "world.lifecycle", Severity.Low },
				{ "ui.layers", Severity.Low },
				{ "ui.input", Severity.Info },
				{ "ui.update", Severity.Info },
				{ "rendering.draw", Severity.Info },
				{ "content.load", Severity.Info }
			}
		};

		return OverlapAnalyzer.Analyze(context, options);
	}
}
