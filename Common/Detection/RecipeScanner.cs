using System;
using System.Collections.Generic;
using System.Linq;
using ModHarmony.Common.Utilities;
using Terraria;
using Terraria.Localization;
using Terraria.ModLoader;

namespace ModHarmony.Common.Detection;

/// <summary>
/// Snapshot of the recipe database taken right after every mod finished adding
/// recipes. Runs once per load from <c>ModSystem.PostSetupRecipes</c> and caches
/// the result in the <see cref="DetectorContext"/>. Only reads public state
/// (<see cref="Main.recipe"/>, <see cref="Recipe.numRecipes"/>,
/// <see cref="Recipe.Mod"/>...) and is fully read-only.
/// </summary>
public static class RecipeScanner
{
	public static RecipeSnapshot Build()
	{
		var snapshot = new RecipeSnapshot();

		try {
			int count = Math.Min(Recipe.numRecipes, Main.recipe?.Length ?? 0);
			for (int i = 0; i < count; i++) {
				var recipe = Main.recipe[i];
				if (recipe == null || recipe.createItem == null || recipe.createItem.type <= 0)
					continue; // empty/uninitialized recipe slot

				string owner;
				try {
					owner = recipe.Mod?.Name ?? "Terraria";
				}
				catch {
					owner = "Terraria";
				}

				string resultName;
				try {
					resultName = Lang.GetItemNameValue(recipe.createItem.type);
				}
				catch {
					resultName = recipe.createItem.type.ToString();
				}

				var entry = new RecipeSnapshot.RecipeEntry {
					ResultType = recipe.createItem.type,
					ResultName = resultName,
					OwnerMod = owner,
					IngredientCount = recipe.requiredItem?.Count ?? 0,
					TileCount = recipe.requiredTile?.Count ?? 0,
					GroupCount = recipe.acceptedGroups?.Count ?? 0,
					ConditionCount = recipe.Conditions?.Count ?? 0,
					DecraftDisabled = recipe.DecraftDisabled,
					HasOrdering = recipe.Ordering.target != null
				};

				snapshot.Recipes.Add(entry);
				if (!snapshot.ByResult.TryGetValue(entry.ResultType, out var list)) {
					list = new List<RecipeSnapshot.RecipeEntry>();
					snapshot.ByResult[entry.ResultType] = list;
				}
				list.Add(entry);
			}
		}
		catch (Exception e) {
			Log.Warn($"Recipe snapshot incomplete: {e.Message}");
		}

		// Recipe groups: which mods contribute items to each shared group.
		try {
			foreach (var kv in RecipeGroup.recipeGroups) {
				var contributors = new HashSet<string>();
				foreach (var itemType in kv.Value.ValidItems) {
					var modItem = ModContent.GetModItem(itemType);
					if (modItem?.Mod != null && modItem.Mod.Name != "ModLoader")
						contributors.Add(modItem.Mod.Name);
				}
				if (contributors.Count > 0)
					snapshot.GroupContributorMods[kv.Key] = contributors;
			}
		}
		catch (Exception e) {
			Log.Debug($"Recipe group snapshot incomplete: {e.Message}");
		}

		return snapshot;
	}
}
