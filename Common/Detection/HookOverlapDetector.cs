using System.Collections.Generic;
using ModHarmony.Common.Core;
using ModHarmony.Content.Config;

namespace ModHarmony.Common.Detection;

/// <summary>
/// Flags pairs (or groups) of mods whose Global* classes override hooks that run
/// in the same game system (e.g. both mods override <c>GlobalNPC.AI</c>). This is
/// the most common — and most honest — signal ModHarmony can produce: it proves
/// both mods' code runs in the same phase for the same entities. It does NOT
/// prove incompatibility; the explanation text says so explicitly.
/// </summary>
public sealed class HookOverlapDetector : IConflictDetector
{
	public string Id => "HookOverlap";
	public string NameKey => "Detectors.HookOverlap.Name";
	public string DescriptionKey => "Detectors.HookOverlap.Description";

	public bool IsEnabled(ModHarmonyConfig config) => config.ScanHooks;

	public List<Conflict> Detect(DetectorContext context)
	{
		var options = new OverlapAnalyzer.Options {
			DetectorId = Id,
			BaseTypes = new[] { "GlobalItem", "GlobalNPC", "GlobalProjectile", "GlobalBuff", "GlobalTile", "GlobalWall" },
			Confidence = Confidence.Strong,
			DefaultSeverity = Severity.Low,
			EvidenceModHooksKey = "HookOverlap.ModHooks",
			EvidenceAggregateKey = "HookOverlap.Aggregate",
			EvidenceWhyKey = "HookOverlap.Why",
			SeverityBySystem = new Dictionary<string, Severity> {
				{ "npc.ai", Severity.Medium },
				{ "npc.damage", Severity.Medium },
				{ "npc.spawn", Severity.Medium },
				{ "npc.loot", Severity.Medium },
				{ "projectile.ai", Severity.Medium },
				{ "projectile.damage", Severity.Medium },
				{ "item.damage", Severity.Medium },
				{ "player.damage", Severity.Medium },
				{ "world.gen", Severity.Medium },
				{ "recipe.modify", Severity.Medium },
				{ "npc.stats", Severity.Low },
				{ "npc.shop", Severity.Low },
				{ "npc.collision", Severity.Low },
				{ "item.use", Severity.Low },
				{ "item.inventory", Severity.Low },
				{ "projectile.collision", Severity.Low },
				{ "buff.update", Severity.Low },
				{ "tile.wire", Severity.Low },
				{ "tile.modification", Severity.Low },
				{ "npc.draw", Severity.Info },
				{ "item.draw", Severity.Info },
				{ "item.tooltip", Severity.Info },
				{ "projectile.draw", Severity.Info },
				{ "buff.tooltip", Severity.Info },
				{ "buff.draw", Severity.Info },
				{ "tile.draw", Severity.Info }
			}
		};

		return OverlapAnalyzer.Analyze(context, options);
	}
}
