using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ModHarmony.Common.Core;
using ModHarmony.Content.Config;
using Terraria.ModLoader;

namespace ModHarmony.Common.Detection;

/// <summary>
/// Asset/resource interactions that are honestly detectable:
///  • duplicate .tmod files claiming the same internal mod name in the mod
///    folder (a real install-level resource collision that confuses updates);
///  • informational note that in-content asset namespaces are per-mod, so
///    tModLoader itself prevents most content/resource name collisions.
/// Cross-mod "same texture name" conflicts cannot occur through supported APIs
/// and are therefore never reported.
/// </summary>
public sealed class AssetDetector : IConflictDetector
{
	public string Id => "Asset";
	public string NameKey => "Detectors.Asset.Name";
	public string DescriptionKey => "Detectors.Asset.Description";

	public bool IsEnabled(ModHarmonyConfig config) => config.ScanAssets;

	public List<Conflict> Detect(DetectorContext context)
	{
		var result = new List<Conflict>();

		// Group installed .tmod metadata by internal name; duplicates are real.
		var byName = new Dictionary<string, List<InstalledModInfo>>(StringComparer.OrdinalIgnoreCase);
		foreach (var info in context.InstalledMods) {
			if (string.IsNullOrEmpty(info.Name) || info.ParseFailed)
				continue;
			if (!byName.TryGetValue(info.Name, out var list)) {
				list = new List<InstalledModInfo>();
				byName[info.Name] = list;
			}
			list.Add(info);
		}

		foreach (var kv in byName.Where(kv => kv.Value.Count > 1)) {
			var names = kv.Value.Select(i => i.FileName).OrderBy(f => f, StringComparer.OrdinalIgnoreCase).ToList();
			var conflict = new Conflict {
				Id = Conflict.MakeStableId(Id, "asset.duplicate", new[] { kv.Key }),
				DetectorId = Id,
				SystemId = "asset.duplicate",
				Severity = Severity.Medium,
				Confidence = Confidence.Confirmed,
				Mods = new List<string> { kv.Key },
				IsConditional = true
			};
			conflict.Evidence.Add(new Evidence(EvidenceKind.Asset, null,
				"Asset.DuplicateFiles", kv.Key, string.Join(", ", names)));
			result.Add(conflict);
		}

		return result;
	}
}
