using System;
using System.Collections.Generic;
using System.Linq;
using ModHarmony.Common.Core;
using ModHarmony.Content.Config;

namespace ModHarmony.Common.Detection;

/// <summary>Snapshot of the game's recipe database taken right after all mods added their recipes.</summary>
public sealed class RecipeSnapshot
{
	public sealed class RecipeEntry
	{
		public int ResultType;
		public string ResultName;
		public string OwnerMod;          // internal name or "Terraria"
		public int IngredientCount;
		public int TileCount;
		public int GroupCount;
		public int ConditionCount;
		public bool DecraftDisabled;
		public bool HasOrdering;         // used SortBefore/SortAfter
		public bool Empty;               // default/empty recipe slots are skipped
	}

	public List<RecipeEntry> Recipes { get; } = new();
	public Dictionary<int, List<RecipeEntry>> ByResult { get; } = new();

	/// <summary>recipe group id → set of mod names that contributed valid items.</summary>
	public Dictionary<int, HashSet<string>> GroupContributorMods { get; } = new();

	public IEnumerable<RecipeEntry> RecipesForResult(int itemType) =>
		ByResult.TryGetValue(itemType, out var list) ? list : Enumerable.Empty<RecipeEntry>();

	public int TotalCount => Recipes.Count;
}

/// <summary>
/// Everything a scan pass needs: per-mod facts, the recipe snapshot, installed
/// mod file metadata and shared scan options. Detectors read from this and
/// produce <see cref="Conflict"/>s; they may also append load-order warnings.
/// </summary>
public sealed class DetectorContext
{
	public List<ModFacts> Mods { get; } = new();
	public Dictionary<string, ModFacts> ByName { get; } = new(StringComparer.OrdinalIgnoreCase);

	/// <summary>Metadata parsed from .tmod files found in the mod folder (may be partial).</summary>
	public List<InstalledModInfo> InstalledMods { get; } = new();

	public RecipeSnapshot Recipes { get; set; } = new();

	/// <summary>Warnings that should feed the health score (e.g. dependency cycles).</summary>
	public List<string> LoadOrderWarnings { get; } = new();

	/// <summary>Mods that loaded even though their metadata was unreadable.</summary>
	public List<string> MetadataUnreadable { get; } = new();

	public ModHarmonyConfig Config { get; set; }
	public bool SafeDiagnosticsMode => Config?.SafeDiagnosticsMode ?? false;

	public IEnumerable<ModFacts> ExceptSelf() => Mods.Where(m => !m.IsModHarmony);

	public ModFacts Get(string modName) => ByName.TryGetValue(modName, out var f) ? f : null;

	/// <summary>System id → number of distinct mods that override hooks on that system.</summary>
	public Dictionary<string, int> SystemOverlapCounts { get; } = new();

	public void RebuildSystemOverlapCounts()
	{
		SystemOverlapCounts.Clear();
		foreach (var mod in ExceptSelf()) {
			foreach (var system in mod.HookCounts.Keys) {
				SystemOverlapCounts.TryGetValue(system, out var n);
				SystemOverlapCounts[system] = n + 1;
			}
		}
	}
}

/// <summary>Metadata parsed from an installed .tmod file (best effort).</summary>
public sealed class InstalledModInfo
{
	public string FileName { get; set; } = "";
	public string Name { get; set; } = "";
	public string DisplayName { get; set; } = "";
	public string Author { get; set; } = "";
	public string Homepage { get; set; } = "";
	public string Description { get; set; } = "";
	public string Version { get; set; } = "";
	public bool IsTranslationMod { get; set; }
	public List<string> ModReferences { get; set; } = new();
	public List<string> WeakReferences { get; set; } = new();
	public List<string> SortAfter { get; set; } = new();
	public List<string> SortBefore { get; set; } = new();
	public bool Loaded { get; set; }
	public bool ParseFailed { get; set; }
}
