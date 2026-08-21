using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.WorldBuilding;

namespace FixtureCombatOverhaul;

/// <summary>Test fixture: simulates a mod that heavily edits combat (NPC AI, damage, spawns, player damage).</summary>
public sealed class FixtureCombatOverhaul : Mod
{
	public override void Load()
	{
		// Opt in to ModHarmony arbitration for the NPC spawn point.
		Call("RegisterArbitrableValue", "npc.spawn", Name, 0.75f, "fixture: 25% fewer spawns");
	}
}

public sealed class CombatGlobalNPC : GlobalNPC
{
	public override void AI(NPC npc) { }

	public override void ModifyHitByProjectile(NPC npc, Projectile projectile, ref NPC.HitModifiers modifiers)
		=> modifiers.FinalDamage *= 1.1f;

	public override void ModifyHitByItem(NPC npc, Player player, Item item, ref NPC.HitModifiers modifiers)
		=> modifiers.FinalDamage *= 1.05f;

	public override void OnHitByProjectile(NPC npc, Projectile projectile, NPC.HitInfo hit, int damageDone) { }

	public override void EditSpawnRate(Player player, ref int spawnRate, ref int maxSpawns)
		=> spawnRate = (int)(spawnRate * 1.5f);
}

public sealed class CombatGlobalItem : GlobalItem
{
	public override void ModifyWeaponDamage(Item item, Player player, ref StatModifier damage) { }
}

public sealed class CombatPlayer : ModPlayer
{
	public override void ModifyWeaponDamage(Item item, ref StatModifier damage) { }
}

public sealed class CombatSystem : ModSystem
{
	public override void AddRecipes()
	{
		Recipe.Create(ItemID.CopperBar)
			.AddIngredient(ItemID.CopperOre, 3)
			.AddTile(TileID.Furnaces)
			.Register();
	}

	public override void ModifyWorldGenTasks(List<GenPass> tasks, ref double totalWeight) { }
}
