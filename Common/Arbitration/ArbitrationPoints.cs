using System.Collections.Generic;
using System.Linq;

namespace ModHarmony.Common.Arbitration;

/// <summary>
/// A supported arbitration mechanism for one game system. A point defines what
/// the "winner's influence" means for that system and (via the implementing
/// hooks) how it is applied. Points only affect mods that explicitly register
/// values through the ModHarmony API — ModHarmony never touches third-party code.
/// </summary>
public abstract class ArbitrationPoint
{
	public abstract string SystemId { get; }
	public abstract string NameKey { get; }
	public abstract string DescriptionKey { get; }
	public string GroupId => $"system.{SystemId}";
}

/// <summary>Registry of all built-in arbitration points.</summary>
public static class ArbitrationPoints
{
	private static readonly List<ArbitrationPoint> Points = new() {
		new SpawnRatePoint(),
		new NpcDamagePoint()
	};

	public static IReadOnlyList<ArbitrationPoint> All => Points;

	public static ArbitrationPoint Find(string systemId) => Points.FirstOrDefault(p => p.SystemId == systemId);

	public static bool HasPoint(string systemId) => Find(systemId) != null;
}

/// <summary>
/// npc.spawn — the winner's registered factor is applied to NPC spawn rate in
/// <see cref="Global.ArbitrationGlobalNPC.EditSpawnRate"/>.
/// A registered factor below 1 means "fewer spawns"; above 1 "more spawns";
/// 1 = no change.
/// </summary>
public sealed class SpawnRatePoint : ArbitrationPoint
{
	public override string SystemId => "npc.spawn";
	public override string NameKey => "Arbitration.Points.SpawnRate.Name";
	public override string DescriptionKey => "Arbitration.Points.SpawnRate.Description";
}

/// <summary>
/// npc.damage — the winner's registered multiplier is applied to damage dealt
/// to NPCs in <see cref="Global.ArbitrationGlobalNPC"/> (FinalDamage).
/// A registered multiplier below 1 means "NPCs take less damage"; above 1
/// "more damage"; 1 = no change.
/// </summary>
public sealed class NpcDamagePoint : ArbitrationPoint
{
	public override string SystemId => "npc.damage";
	public override string NameKey => "Arbitration.Points.NpcDamage.Name";
	public override string DescriptionKey => "Arbitration.Points.NpcDamage.Description";
}
