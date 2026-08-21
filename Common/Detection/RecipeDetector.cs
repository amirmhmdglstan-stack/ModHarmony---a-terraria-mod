using System;
using System.Collections.Generic;
using System.Linq;
using ModHarmony.Common.Core;
using ModHarmony.Content.Config;

namespace ModHarmony.Common.Detection;

/// <summary>
/// Recipe interactions that are actually observable from the public API:
///  • multiple mods registering recipes that produce the same result item
///    (common and usually intentional — severity stays low, the explanation
///    says so; the conflict matters when the recipes compete for ingredients
///    or have very different costs);
///  • recipe groups whose item pool is contributed to by several mods
///    (tModLoader merges same-name groups; informational).
/// tModLoader exposes the registering mod on every recipe (Recipe.Mod), so both
/// detections are Confirmed.
/// </summary>
public sealed class RecipeDetector : IConflictDetector
{
	public string Id => "RecipeOverlap";
	public string NameKey => "Detectors.RecipeOverlap.Name";
	public string DescriptionKey => "Detectors.RecipeOverlap.Description";

	public bool IsEnabled(ModHarmonyConfig config) => config.ScanRecipes;

	public List<Conflict> Detect(DetectorContext context)
	{
		var result = new List<Conflict>();
		const int maxPairMods = 6;

		// --- Same result, multiple recipe owners ----------------------------
		foreach (var kv in context.Recipes.ByResult) {
			var owners = kv.Value
				.Where(r => r.OwnerMod != "Terraria" && r.OwnerMod != ModHarmony.ModName)
				.Select(r => r.OwnerMod)
				.Distinct(StringComparer.OrdinalIgnoreCase)
				.OrderBy(m => m, StringComparer.OrdinalIgnoreCase)
				.ToArray();
			if (owners.Length < 2)
				continue;

			var resultName = kv.Value.First().ResultName;
			var resultType = kv.Key;
			var systemId = "recipe.add";

			if (owners.Length <= maxPairMods) {
				for (int i = 0; i < owners.Length; i++) {
					for (int j = i + 1; j < owners.Length; j++) {
						var a = owners[i];
						var b = owners[j];
						var conflict = new Conflict {
							Id = Conflict.MakeStableId(Id, systemId, new[] { a, b }),
							DetectorId = Id,
							SystemId = systemId,
							Severity = Severity.Low,
							Confidence = Confidence.Confirmed,
							Mods = new List<string> { a, b }
						};
						var aRecipes = kv.Value.Count(r => r.OwnerMod == a);
						var bRecipes = kv.Value.Count(r => r.OwnerMod == b);
						conflict.Evidence.Add(new Evidence(EvidenceKind.Recipe, a,
							"RecipeOverlap.ModRecipes", a, resultName, aRecipes.ToString()));
						conflict.Evidence.Add(new Evidence(EvidenceKind.Recipe, b,
							"RecipeOverlap.ModRecipes", b, resultName, bRecipes.ToString()));
						conflict.Evidence.Add(new Evidence(EvidenceKind.General, null,
							"RecipeOverlap.Why", resultName));
						conflict.Evidence[0].DevDetail = $"resultItemType={resultType}";
						result.Add(conflict);
					}
				}
			}
			else {
				var top = owners.Take(12).ToList();
				var conflict = new Conflict {
					Id = Conflict.MakeStableId(Id, systemId, top),
					DetectorId = Id,
					SystemId = systemId,
					Severity = Severity.Low,
					Confidence = Confidence.Confirmed,
					Mods = top
				};
				conflict.Evidence.Add(new Evidence(EvidenceKind.Recipe, null,
					"RecipeOverlap.Aggregate", resultName, owners.Length.ToString()));
				result.Add(conflict);
			}
		}

		// --- Shared recipe groups -------------------------------------------
		foreach (var kv in context.Recipes.GroupContributorMods) {
			var contributors = kv.Value
				.Where(m => m != ModHarmony.ModName)
				.OrderBy(m => m, StringComparer.OrdinalIgnoreCase)
				.ToList();
			if (contributors.Count < 2)
				continue;

			var conflict = new Conflict {
				Id = Conflict.MakeStableId(Id, "recipe.group", contributors),
				DetectorId = Id,
				SystemId = "recipe.group",
				Severity = Severity.Info,
				Confidence = Confidence.Confirmed,
				Mods = contributors
			};
			conflict.Evidence.Add(new Evidence(EvidenceKind.Recipe, null,
				"RecipeOverlap.GroupShared", contributors.Count.ToString()));
			result.Add(conflict);
		}

		return result;
	}
}
