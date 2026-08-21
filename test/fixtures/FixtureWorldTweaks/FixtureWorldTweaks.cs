using System.Collections.Generic;
using Terraria;
using Terraria.GameContent.UI.Elements;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.UI;
using Terraria.WorldBuilding;

namespace FixtureWorldTweaks;

/// <summary>Test fixture: world/spawn/UI tweaks that intentionally overlap with FixtureCombatOverhaul and FixtureRecipeModder.</summary>
public sealed class FixtureWorldTweaks : Mod
{
}

public sealed class WorldGlobalNPC : GlobalNPC
{
	public override void AI(NPC npc) { }

	public override void EditSpawnPool(IDictionary<int, float> pool, NPCSpawnInfo spawnInfo) { }

	public override void ModifyNPCLoot(NPC npc, NPCLoot npcLoot) { }
}

public sealed class WorldGlobalTile : GlobalTile
{
	public override void HitWire(int i, int j, int type) { }
}

public sealed class WorldSystem : ModSystem
{
	public override void ModifyWorldGenTasks(List<GenPass> tasks, ref double totalWeight) { }

	public override void AddRecipes()
	{
		// Same result as FixtureCombatOverhaul and FixtureRecipeModder → recipe overlap.
		Recipe.Create(ItemID.CopperBar)
			.AddIngredient(ItemID.CopperOre, 2)
			.AddTile(TileID.Furnaces)
			.Register();
	}

	public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers) { }
}
