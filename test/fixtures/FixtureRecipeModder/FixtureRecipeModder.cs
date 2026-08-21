using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace FixtureRecipeModder;

/// <summary>Test fixture: recipe-heavy mod that also carries an IL-patch-style class.</summary>
public sealed class FixtureRecipeModder : Mod
{
}

public sealed class RecipeSystem : ModSystem
{
	public static RecipeGroup AnyBarGroup;

	public override void AddRecipeGroups()
	{
		// A group whose items come from multiple mods (vanilla + modded bars).
		AnyBarGroup = new RecipeGroup(() => $"{Language.GetTextValue("LegacyMisc.37")} Bar",
			ItemID.CopperBar, ItemID.IronBar, ItemID.GoldBar);
		RecipeGroup.RegisterGroup("FixtureRecipeModder:AnyBar", AnyBarGroup);
	}

	public override void AddRecipes()
	{
		Recipe.Create(ItemID.CopperBar)
			.AddIngredient(ItemID.CopperOre, 1)
			.AddTile(TileID.Furnaces)
			.Register();

		Recipe.Create(ItemID.WoodenSword)
			.AddIngredient(ItemID.Wood, 8)
			.AddRecipeGroup("FixtureRecipeModder:AnyBar", 1)
			.Register();
	}

	public override void PostAddRecipes()
	{
		// Cross-mod recipe editing signal (ModSystemOverlap → recipe.modify).
		for (int i = 0; i < Recipe.numRecipes; i++) {
			var r = Main.recipe[i];
			if (r.createItem.type == ItemID.WoodenSword && r.requiredTile.Contains(TileID.Furnaces))
				r.DisableDecraft();
		}
	}
}

// IL-patch convention signal: namespace "IL." is one of the conventions
// ModHarmony's ILHookDetector looks for.
namespace IL.Terraria
{
	public static class Main
	{
		public static void Update() { }
	}
}
