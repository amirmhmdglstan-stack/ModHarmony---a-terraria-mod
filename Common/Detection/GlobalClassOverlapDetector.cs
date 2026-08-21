using System;
using System.Collections.Generic;
using System.Linq;
using ModHarmony.Common.Core;
using ModHarmony.Content.Config;

namespace ModHarmony.Common.Detection;

/// <summary>
/// Informational detector: reports mods that register the same Global* base
/// class at all (GlobalNPC, GlobalItem, ...), even when they share no specific
/// hook. This answers "which mods add behavior to NPCs as a whole?" without
/// claiming that such co-existence is a problem.
/// </summary>
public sealed class GlobalClassOverlapDetector : IConflictDetector
{
	private static readonly string[] Classes = {
		"GlobalItem", "GlobalNPC", "GlobalProjectile", "GlobalBuff",
		"GlobalTile", "GlobalWall", "GlobalBossBar", "GlobalInfoDisplay", "GlobalLoot"
	};

	public string Id => "GlobalClassOverlap";
	public string NameKey => "Detectors.GlobalClassOverlap.Name";
	public string DescriptionKey => "Detectors.GlobalClassOverlap.Description";

	public bool IsEnabled(ModHarmonyConfig config) => config.ScanGlobalClasses;

	public List<Conflict> Detect(DetectorContext context)
	{
		var result = new List<Conflict>();
		const int maxPairMods = 8;

		foreach (var className in Classes) {
			var involved = context.ExceptSelf()
				.Where(m => m.GlobalClasses.Contains(className))
				.Select(m => m.Name)
				.OrderBy(m => m, StringComparer.OrdinalIgnoreCase)
				.ToArray();
			if (involved.Length < 2)
				continue;

			var systemId = $"content.{className}";
			var systemName = SafeSystemName(systemId);

			if (involved.Length <= maxPairMods) {
				for (int i = 0; i < involved.Length; i++) {
					for (int j = i + 1; j < involved.Length; j++) {
						var a = involved[i];
						var b = involved[j];
						var conflict = new Conflict {
							Id = Conflict.MakeStableId(Id, systemId, new[] { a, b }),
							DetectorId = Id,
							SystemId = systemId,
							Severity = Severity.Info,
							Confidence = Confidence.Confirmed,
							Mods = new List<string> { a, b }
						};
						conflict.Evidence.Add(new Evidence(EvidenceKind.ContentRegistration, null,
							"GlobalClassOverlap.Pair", className, a, b));
						result.Add(conflict);
					}
				}
			}
			else {
				var top = involved.Take(20).ToList();
				var conflict = new Conflict {
					Id = Conflict.MakeStableId(Id, systemId, top),
					DetectorId = Id,
					SystemId = systemId,
					Severity = Severity.Info,
					Confidence = Confidence.Confirmed,
					Mods = top
				};
				conflict.Evidence.Add(new Evidence(EvidenceKind.ContentRegistration, null,
					"GlobalClassOverlap.Aggregate", className, involved.Length.ToString()));
				result.Add(conflict);
			}
		}

		return result;
	}

	private static string SafeSystemName(string systemId)
	{
		try {
			return Common.Utilities.L10n.Text(SystemRegistry.Get(systemId).NameKey);
		}
		catch {
			return systemId;
		}
	}
}
