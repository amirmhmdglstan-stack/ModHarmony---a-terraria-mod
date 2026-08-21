using ModHarmony.Common.Arbitration;
using Terraria;
using Terraria.ModLoader;

namespace ModHarmony.Global;

/// <summary>
/// Applies built-in arbitration points that reach into NPC behavior. Every hook
/// is a no-op (factor 1) unless a group is enabled AND a cooperating mod has
/// registered a value, so ModHarmony changes nothing by default.
/// </summary>
public sealed class ArbitrationGlobalNPC : GlobalNPC
{
	public override bool IsLoadingEnabled(Mod mod) => mod.Name == ModHarmony.ModName;

	public override void EditSpawnRate(Player player, ref int spawnRate, ref int maxSpawns)
	{
		float factor = ArbitrationState.WinnerFactor("npc.spawn");
		if (factor <= 0f || factor == 1f || spawnRate <= 0)
			return;
		// Lower spawnRate = more spawns, so invert.
		spawnRate = (int)(spawnRate / factor);
		if (spawnRate < 1)
			spawnRate = 1;
	}

	public override void ModifyHitByItem(NPC npc, Player player, Item item, ref NPC.HitModifiers modifiers)
	{
		ApplyDamageFactor(ref modifiers);
	}

	public override void ModifyHitByProjectile(NPC npc, Projectile projectile, ref NPC.HitModifiers modifiers)
	{
		ApplyDamageFactor(ref modifiers);
	}

	private static void ApplyDamageFactor(ref NPC.HitModifiers modifiers)
	{
		float factor = ArbitrationState.WinnerFactor("npc.damage");
		if (factor <= 0f || factor == 1f)
			return;
		modifiers.FinalDamage *= factor;
	}
}
