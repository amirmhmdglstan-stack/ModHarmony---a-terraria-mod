using System.Collections.Generic;

namespace ModHarmony.Common.Core;

/// <summary>
/// Broad functional areas the game systems belong to. Used to group systems in
/// the "Systems" tab and to derive explanations.
/// </summary>
public enum SystemCategory
{
	Content,     // registration of content/global classes
	Item,
	Npc,
	Projectile,
	Player,
	Buff,
	Tile,
	Recipe,
	World,
	Ui,
	Networking,
	Dependency,
	Runtime,     // IL patches, detours, errors
	Asset,
	Rendering
}

/// <summary>
/// A named game system that mods can interact with. Systems are the central
/// vocabulary of ModHarmony: detectors report conflicts "on a system", the
/// Systems tab groups mods by system, and arbitration groups are per system.
/// </summary>
public sealed class GameSystem
{
	/// <summary>Stable identifier, e.g. "npc.ai". Never localized, never renamed.</summary>
	public string Id { get; }

	/// <summary>Functional area this system belongs to.</summary>
	public SystemCategory Category { get; }

	/// <summary>Localization key for the display name (without the Mods.ModHarmony. prefix).</summary>
	public string NameKey { get; }

	/// <summary>Localization key for a short "what this system is" explanation.</summary>
	public string DescriptionKey { get; }

	public GameSystem(string id, SystemCategory category, string nameKey = null, string descriptionKey = null)
	{
		Id = id;
		Category = category;
		NameKey = nameKey ?? $"Systems.{id.Replace('.', '_')}.Name";
		DescriptionKey = descriptionKey ?? $"Systems.{id.Replace('.', '_')}.Description";
	}
}

/// <summary>
/// Central, extensible registry of detectable game systems. New detectors can
/// register additional systems without touching the rest of the mod.
/// </summary>
public static class SystemRegistry
{
	private static readonly Dictionary<string, GameSystem> _systems = new();
	private static readonly List<GameSystem> _order = new();

	public static GameSystem Register(string id, SystemCategory category)
	{
		if (_systems.TryGetValue(id, out var existing))
			return existing;

		var system = new GameSystem(id, category);
		_systems[id] = system;
		_order.Add(system);
		return system;
	}

	public static GameSystem Get(string id) => _systems.TryGetValue(id, out var s) ? s : Register(id, SystemCategory.Content);

	public static IReadOnlyCollection<GameSystem> All => _order;

	public static bool Exists(string id) => _systems.ContainsKey(id);

	// ------------------------------------------------------------------
	// Built-in system definitions. IDs are stable identifiers.
	// ------------------------------------------------------------------
	public static readonly GameSystem ItemUse = Register("item.use", SystemCategory.Item);
	public static readonly GameSystem ItemDamage = Register("item.damage", SystemCategory.Item);
	public static readonly GameSystem ItemTooltip = Register("item.tooltip", SystemCategory.Item);
	public static readonly GameSystem ItemInventory = Register("item.inventory", SystemCategory.Item);
	public static readonly GameSystem ItemDraw = Register("item.draw", SystemCategory.Item);
	public static readonly GameSystem ItemLoot = Register("item.loot", SystemCategory.Item);
	public static readonly GameSystem ItemModification = Register("item.modification", SystemCategory.Item);

	public static readonly GameSystem NpcAi = Register("npc.ai", SystemCategory.Npc);
	public static readonly GameSystem NpcDamage = Register("npc.damage", SystemCategory.Npc);
	public static readonly GameSystem NpcSpawn = Register("npc.spawn", SystemCategory.Npc);
	public static readonly GameSystem NpcLoot = Register("npc.loot", SystemCategory.Npc);
	public static readonly GameSystem NpcStats = Register("npc.stats", SystemCategory.Npc);
	public static readonly GameSystem NpcShop = Register("npc.shop", SystemCategory.Npc);
	public static readonly GameSystem NpcChat = Register("npc.chat", SystemCategory.Npc);
	public static readonly GameSystem NpcDraw = Register("npc.draw", SystemCategory.Npc);
	public static readonly GameSystem NpcCollision = Register("npc.collision", SystemCategory.Npc);
	public static readonly GameSystem NpcModification = Register("npc.modification", SystemCategory.Npc);

	public static readonly GameSystem ProjectileAi = Register("projectile.ai", SystemCategory.Projectile);
	public static readonly GameSystem ProjectileDamage = Register("projectile.damage", SystemCategory.Projectile);
	public static readonly GameSystem ProjectileCollision = Register("projectile.collision", SystemCategory.Projectile);
	public static readonly GameSystem ProjectileDraw = Register("projectile.draw", SystemCategory.Projectile);
	public static readonly GameSystem ProjectileModification = Register("projectile.modification", SystemCategory.Projectile);

	public static readonly GameSystem PlayerUpdate = Register("player.update", SystemCategory.Player);
	public static readonly GameSystem PlayerStats = Register("player.stats", SystemCategory.Player);
	public static readonly GameSystem PlayerDamage = Register("player.damage", SystemCategory.Player);
	public static readonly GameSystem PlayerInventory = Register("player.inventory", SystemCategory.Player);
	public static readonly GameSystem PlayerSave = Register("player.save", SystemCategory.Player);
	public static readonly GameSystem PlayerDraw = Register("player.draw", SystemCategory.Player);
	public static readonly GameSystem PlayerLifecycle = Register("player.lifecycle", SystemCategory.Player);

	public static readonly GameSystem BuffUpdate = Register("buff.update", SystemCategory.Buff);
	public static readonly GameSystem BuffTooltip = Register("buff.tooltip", SystemCategory.Buff);
	public static readonly GameSystem BuffDraw = Register("buff.draw", SystemCategory.Buff);

	public static readonly GameSystem TileModification = Register("tile.modification", SystemCategory.Tile);
	public static readonly GameSystem TileDraw = Register("tile.draw", SystemCategory.Tile);
	public static readonly GameSystem TileWire = Register("tile.wire", SystemCategory.Tile);

	public static readonly GameSystem RecipeAdd = Register("recipe.add", SystemCategory.Recipe);
	public static readonly GameSystem RecipeModify = Register("recipe.modify", SystemCategory.Recipe);
	public static readonly GameSystem RecipeRemove = Register("recipe.remove", SystemCategory.Recipe);
	public static readonly GameSystem RecipeGroup = Register("recipe.group", SystemCategory.Recipe);

	public static readonly GameSystem WorldGen = Register("world.gen", SystemCategory.World);
	public static readonly GameSystem WorldUpdate = Register("world.update", SystemCategory.World);
	public static readonly GameSystem WorldSave = Register("world.save", SystemCategory.World);
	public static readonly GameSystem WorldTime = Register("world.time", SystemCategory.World);
	public static readonly GameSystem WorldLifecycle = Register("world.lifecycle", SystemCategory.World);

	public static readonly GameSystem UiLayers = Register("ui.layers", SystemCategory.Ui);
	public static readonly GameSystem UiUpdate = Register("ui.update", SystemCategory.Ui);
	public static readonly GameSystem UiInput = Register("ui.input", SystemCategory.Ui);

	public static readonly GameSystem RenderingDraw = Register("rendering.draw", SystemCategory.Rendering);

	public static readonly GameSystem NetPackets = Register("net.packets", SystemCategory.Networking);
	public static readonly GameSystem NetHijack = Register("net.hijack", SystemCategory.Networking);

	public static readonly GameSystem DependencyCycle = Register("dependency.cycle", SystemCategory.Dependency);
	public static readonly GameSystem DependencyVersion = Register("dependency.version", SystemCategory.Dependency);
	public static readonly GameSystem DependencyMissing = Register("dependency.missing", SystemCategory.Dependency);
	public static readonly GameSystem DependencyOptional = Register("dependency.optional", SystemCategory.Dependency);

	public static readonly GameSystem RuntimePatch = Register("runtime.patch", SystemCategory.Runtime);
	public static readonly GameSystem RuntimeErrors = Register("runtime.errors", SystemCategory.Runtime);

	public static readonly GameSystem AssetDuplicate = Register("asset.duplicate", SystemCategory.Asset);

	public static readonly GameSystem ContentGlobalItem = Register("content.globalItem", SystemCategory.Content);
	public static readonly GameSystem ContentGlobalNpc = Register("content.globalNPC", SystemCategory.Content);
	public static readonly GameSystem ContentGlobalProjectile = Register("content.globalProjectile", SystemCategory.Content);
	public static readonly GameSystem ContentGlobalBuff = Register("content.globalBuff", SystemCategory.Content);
	public static readonly GameSystem ContentGlobalTile = Register("content.globalTile", SystemCategory.Content);
	public static readonly GameSystem ContentGlobalWall = Register("content.globalWall", SystemCategory.Content);
	public static readonly GameSystem ContentModPlayer = Register("content.modPlayer", SystemCategory.Content);
	public static readonly GameSystem ContentModSystem = Register("content.modSystem", SystemCategory.Content);
	public static readonly GameSystem ContentLoad = Register("content.load", SystemCategory.Content);
}
