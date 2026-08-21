using System;
using System.Collections.Generic;

namespace ModHarmony.Common.Core;

/// <summary>
/// Maps every hook method of every "global" / ModPlayer / ModSystem base class to
/// the game system it influences. The tables were compiled from the current
/// tModLoader stable API (v2026.06, 1.4.4 branch). Hooks that could not be
/// classified confidently fall back to the containing type's default system.
/// </summary>
public static class HookCatalog
{
	/// <summary>base type name → hook method name → system id</summary>
	private static readonly Dictionary<string, Dictionary<string, string>> Tables = new(StringComparer.Ordinal);

	/// <summary>base type name → default system id when no explicit mapping exists</summary>
	private static readonly Dictionary<string, string> Defaults = new(StringComparer.Ordinal);

	static HookCatalog()
	{
		Add("GlobalItem", "item.modification",
			"OnCreated", "OnSpawn", "ChoosePrefix", "PrefixChance", "AllowPrefix", "ApplyPrefix",
			"CanCatchNPC", "OnCatchNPC", "CanStack", "CanStackInWorld", "OnStack", "SplitStack",
			"ReforgePrice", "CanReforge", "PreReforge", "PostReforge",
			"ExtractinatorUse", "CaughtFishStack", "IsAnglerQuestAvailable", "AnglerChat",
			"SaveData", "LoadData", "NetSend", "NetReceive");
		Add("GlobalItem", "item.use",
			"CanUseItem", "CanAutoReuseItem", "UseStyle", "HoldStyle", "HoldItem",
			"UseTimeMultiplier", "UseAnimationMultiplier", "UseSpeedMultiplier",
			"GetHealLife", "GetHealMana", "ModifyManaCost", "OnMissingMana", "OnConsumeMana",
			"ModifyPotionDelay", "ApplyPotionDelay", "CanRightClick", "RightClick",
			"NeedsAmmo", "PickAmmo", "CanChooseAmmo", "CanBeChosenAsAmmo",
			"CanConsumeAmmo", "CanBeConsumedAsAmmo", "OnConsumeAmmo", "OnConsumedAsAmmo",
			"CanShoot", "ModifyShootStats", "Shoot", "UseItemHitbox", "MeleeEffects",
			"ModifyItemScale", "UseItem", "UseAnimation", "ConsumeItem", "OnConsumeItem",
			"AltFunctionUse", "UseItemFrame", "HoldItemFrame");
		Add("GlobalItem", "item.damage",
			"ModifyWeaponDamage", "ModifyWeaponKnockback", "ModifyWeaponCrit",
			"CanHitNPC", "CanMeleeAttackCollideWithNPC", "ModifyHitNPC", "OnHitNPC",
			"CanHitPvp", "ModifyHitPvp", "OnHitPvp");
		Add("GlobalItem", "item.inventory",
			"UpdateInventory", "UpdateInfoAccessory", "UpdateEquip", "UpdateAccessory",
			"UpdateVanity", "UpdateVisibleAccessory", "UpdateItemDye",
			"IsArmorSet", "UpdateArmorSet", "IsVanitySet", "PreUpdateVanitySet",
			"UpdateVanitySet", "ArmorSetShadows", "SetMatch",
			"CanEquipAccessory", "CanAccessoryBeEquippedWith",
			"Update", "PostUpdate", "GrabRange", "GrabStyle", "CanPickup", "OnPickup", "ItemSpace");
		Add("GlobalItem", "item.tooltip",
			"PreDrawTooltip", "PostDrawTooltip", "PreDrawTooltipLine", "PostDrawTooltipLine", "ModifyTooltips");
		Add("GlobalItem", "item.draw",
			"DrawArmorColor", "ArmorArmGlowMask", "VerticalWingSpeeds", "HorizontalWingSpeeds", "WingUpdate",
			"GetAlpha", "PreDrawInWorld", "PostDrawInWorld", "PreDrawInInventory", "PostDrawInInventory",
			"PreModifyItemDraw", "PostModifyItemDraw", "HoldoutOffset", "HoldoutOrigin");
		Add("GlobalItem", "recipe.add", "AddRecipes");
		Add("GlobalItem", "item.loot", "ModifyItemLoot");

		Add("GlobalNPC", "npc.modification",
			"SetDefaultsFromNetId", "SetBestiary", "ModifyTypeName", "ModifyHoverBoundingBox",
			"PreHoverInteract", "ModifyTownNPCProfile", "ModifyNPCNameList", "SendExtraAI", "ReceiveExtraAI",
			"CanFallThroughPlatforms", "CanBeCaughtBy", "OnCaughtBy",
			"ModifyDeathMessage", "ModifyCollisionData", "NeedSaving", "OnChatButtonClicked", "ModifyNPCHappiness");
		Add("GlobalNPC", "npc.ai", "PreAI", "AI", "PostAI", "FindFrame");
		Add("GlobalNPC", "npc.spawn", "OnSpawn", "EditSpawnRate", "EditSpawnRange", "EditSpawnPool", "SpawnNPC");
		Add("GlobalNPC", "npc.stats",
			"ApplyDifficultyAndPlayerScaling", "ResetEffects", "UpdateLifeRegen", "CheckActive", "CheckDead",
			"PreKill", "OnKill", "SpecialOnKill", "BuffTownNPC",
			"TownNPCAttackStrength", "TownNPCAttackCooldown", "TownNPCAttackProj",
			"TownNPCAttackProjSpeed", "TownNPCAttackShoot", "TownNPCAttackMagic", "TownNPCAttackSwing");
		Add("GlobalNPC", "npc.damage",
			"CanHitPlayer", "ModifyHitPlayer", "OnHitPlayer",
			"CanHitNPC", "CanBeHitByNPC", "ModifyHitNPC", "OnHitNPC",
			"CanBeHitByItem", "ModifyHitByItem", "OnHitByItem",
			"CanBeHitByProjectile", "ModifyHitByProjectile", "OnHitByProjectile",
			"ModifyIncomingHit");
		Add("GlobalNPC", "npc.loot", "ModifyNPCLoot", "ModifyGlobalLoot");
		Add("GlobalNPC", "npc.shop", "ModifyShop", "ModifyActiveShop", "SetupTravelShop");
		Add("GlobalNPC", "npc.chat", "CanChat", "GetChat", "PreChatButtonClicked");
		Add("GlobalNPC", "npc.draw",
			"BossHeadSlot", "BossHeadRotation", "BossHeadSpriteEffects", "GetAlpha", "DrawEffects",
			"PreDraw", "PostDraw", "DrawBehind", "DrawHealthBar", "DrawTownAttackGun", "DrawTownAttackSwing");
		Add("GlobalNPC", "npc.collision", "ModifyCollisionData", "CanCollideWithPlayerMeleeAttack");

		Add("GlobalProjectile", "projectile.modification",
			"OnSpawn", "SendExtraAI", "ReceiveExtraAI", "PreKill", "OnKill", "Kill",
			"CanUseGrapple", "UseGrapple", "NumGrappleHooks", "GrappleRetreatSpeed", "GrapplePullSpeed",
			"GrappleTargetPoint", "GrappleCanLatchOnTo", "PrepareBombToBlow", "EmitEnchantmentVisualsAt",
			"FlailStats", "FlailSpinCollisionRange", "MinionContactDamage");
		Add("GlobalProjectile", "projectile.ai", "PreAI", "AI", "PostAI");
		Add("GlobalProjectile", "projectile.collision",
			"ShouldUpdatePosition", "TileCollideStyle", "OnTileCollide", "CanCutTiles", "CutTiles", "Colliding");
		Add("GlobalProjectile", "projectile.damage",
			"CanDamage", "ModifyDamageHitbox", "CanHitNPC", "ModifyHitNPC", "OnHitNPC",
			"CanHitPvp", "CanHitPlayer", "ModifyHitPlayer", "OnHitPlayer");
		Add("GlobalProjectile", "projectile.draw", "GetAlpha", "PreDrawExtras", "PreDraw", "PostDraw", "DrawBehind");

		Add("GlobalBuff", "buff.update", "Update", "ReApply", "RightClick");
		Add("GlobalBuff", "buff.tooltip", "ModifyBuffText", "CustomBuffTipSize");
		Add("GlobalBuff", "buff.draw", "DrawCustomBuffTip", "PreDraw", "PostDraw");

		Add("GlobalTile", "tile.modification",
			"DropCritterChance", "CanDrop", "Drop", "CanKillTile", "KillTile", "NearbyEffects",
			"IsTileDangerous", "IsTileBiomeSightable", "IsTileSpelunkable",
			"AdjTiles", "RightClick", "MouseOver", "MouseOverFar", "AutoSelect",
			"CanReplace", "ReplaceTile", "PostSetupTileMerge", "PreShakeTree", "ShakeTree", "OnTileConverted");
		Add("GlobalTile", "tile.draw",
			"SetSpriteEffects", "AnimateTile", "DrawEffects", "EmitParticles", "SpecialDraw",
			"PreDrawPlacementPreview", "PostDrawPlacementPreview", "TileFrame", "FloorVisuals", "ChangeWaterfallStyle");
		Add("GlobalTile", "tile.wire", "PreHitWire", "HitWire", "HitSwitch", "SwitchTiles", "Slope");

		Add("GlobalWall", "tile.modification", "Drop", "KillWall", "CanBeTeleportedTo", "OnWallConverted");
		Add("GlobalWall", "tile.draw", "WallFrame");

		Add("ModPlayer", "player.stats",
			"Initialize", "ResetEffects", "ResetInfoAccessories", "RefreshInfoAccessoriesFromTeamPlayers",
			"ModifyMaxStats", "UpdateDead", "UpdateBadLifeRegen", "UpdateLifeRegen", "NaturalLifeRegen",
			"ArmorSetBonusActivated", "ArmorSetBonusHeld", "ImmuneTo", "ModifyNurseHeal", "ModifyNursePrice",
			"PostNurseHeal", "CanBeTeleportedTo",
			"ModifyFishingAttempt", "CatchFish", "ModifyCaughtFish", "CanConsumeBait", "GetFishingLevel",
			"AnglerQuestReward", "GetDyeTraderReward");
		Add("ModPlayer", "player.update",
			"UpdateAutopause", "PreUpdate", "ProcessTriggers", "SetControls", "PreUpdateBuffs", "PostUpdateBuffs",
			"UpdateEquips", "PostUpdateEquips", "UpdateVisibleAccessories", "UpdateVisibleVanityAccessories",
			"UpdateDyes", "PostUpdateMiscEffects", "PostUpdateRunSpeeds", "PreUpdateMovement", "PostUpdate",
			"FrameEffects", "PreItemCheck", "PostItemCheck",
			"ModifyExtraJumpDurationMultiplier", "CanStartExtraJump", "OnExtraJumpStarted", "OnExtraJumpEnded",
			"OnExtraJumpRefreshed", "ExtraJumpVisuals", "CanShowExtraJumpVisuals", "OnExtraJumpCleared");
		Add("ModPlayer", "player.damage",
			"FreeDodge", "ConsumableDodge", "ModifyHurt", "OnHurt", "PostHurt", "PreKill", "Kill",
			"PreModifyLuck", "ModifyLuck",
			"OnHitAnything", "CanHitNPC", "CanMeleeAttackCollideWithNPC", "ModifyHitNPC", "OnHitNPC",
			"CanHitNPCWithItem", "ModifyHitNPCWithItem", "OnHitNPCWithItem",
			"CanHitNPCWithProj", "ModifyHitNPCWithProj", "OnHitNPCWithProj",
			"CanHitPvp", "CanHitPvpWithProj", "CanBeHitByNPC", "ModifyHitByNPC", "OnHitByNPC",
			"CanBeHitByProjectile", "ModifyHitByProjectile", "OnHitByProjectile");
		Add("ModPlayer", "player.inventory",
			"ShiftClickSlot", "HoverSlot", "PostSellItem", "CanSellItem", "PostBuyItem", "CanBuyItem",
			"AddStartingItems", "ModifyStartingInventory", "AddMaterialsForCrafting", "OnPickup",
			"OnEquipmentLoadoutSwitched");
		Add("ModPlayer", "player.save",
			"PreSaveCustomData", "SaveData", "LoadData", "PreSavePlayer", "PostSavePlayer",
			"CopyClientState", "SyncPlayer", "SendClientChanges");
		Add("ModPlayer", "player.draw",
			"DrawEffects", "ModifyDrawInfo", "TransformDrawData", "ModifyDrawLayerOrdering",
			"HideDrawLayers", "ModifyScreenPosition", "ModifyZoom", "DrawPlayer");
		Add("ModPlayer", "player.lifecycle", "PlayerConnect", "PlayerDisconnect", "OnEnterWorld", "OnRespawn");
		Add("ModPlayer", "item.use",
			"UseTimeMultiplier", "UseAnimationMultiplier", "UseSpeedMultiplier",
			"GetHealLife", "GetHealMana", "ModifyManaCost", "OnMissingMana", "OnConsumeMana",
			"ApplyPotionDelay", "CanConsumeAmmo", "OnConsumeAmmo", "CanShoot", "ModifyShootStats",
			"Shoot", "MeleeEffects", "EmitEnchantmentVisualsAt", "CanCatchNPC", "OnCatchNPC",
			"ModifyItemScale", "CanUseItem", "CanAutoReuseItem");
		Add("ModPlayer", "item.damage",
			"ModifyWeaponDamage", "ModifyWeaponKnockback", "ModifyWeaponCrit");

		Add("ModSystem", "world.update",
			"PreUpdateEntities", "PreUpdatePlayers", "PostUpdatePlayers", "PreUpdateNPCs", "PostUpdateNPCs",
			"PreUpdateGores", "PostUpdateGores", "PreUpdateProjectiles", "PostUpdateProjectiles",
			"PreUpdateItems", "PostUpdateItems", "PreUpdateDusts", "PostUpdateDusts",
			"PreUpdateTime", "PostUpdateTime", "PreUpdateWorld", "PostUpdateWorld",
			"PreUpdateInvasions", "PostUpdateInvasions", "PostUpdateEverything");
		Add("ModSystem", "world.gen", "PreWorldGen", "ModifyWorldGenTasks", "PostWorldGen", "ModifyHardmodeTasks", "ResetNearbyTileEffects");
		Add("ModSystem", "world.save", "SaveWorldData", "LoadWorldData", "SaveWorldHeader", "CanWorldBePlayed", "WorldCanBePlayedRejectionMessage", "PreSaveAndQuit");
		Add("ModSystem", "world.time", "ModifyTimeRate");
		Add("ModSystem", "world.lifecycle", "OnWorldLoad", "PostWorldLoad", "OnWorldUnload", "ClearWorld");
		Add("ModSystem", "recipe.add", "AddRecipes", "AddRecipeGroups");
		Add("ModSystem", "recipe.modify", "PostAddRecipes", "PostSetupRecipes");
		Add("ModSystem", "ui.layers", "ModifyInterfaceLayers", "ModifyGameTipVisibility");
		Add("ModSystem", "ui.update", "UpdateUI");
		Add("ModSystem", "ui.input", "PostUpdateInput");
		Add("ModSystem", "rendering.draw",
			"ModifyScreenPosition", "ModifyTransformMatrix", "PostDrawInterface", "PostDrawTiles",
			"PostDrawFullscreenMap", "PreDrawMapIconOverlay", "PostDrawMapIconOverlay");
		Add("ModSystem", "net.packets", "NetSend", "NetReceive");
		Add("ModSystem", "net.hijack", "HijackGetData", "HijackSendData");
		Add("ModSystem", "content.load", "OnModLoad", "OnModUnload", "PostSetupContent", "OnLocalizationsLoaded");

		// Defaults for anything not explicitly listed above.
		Defaults["GlobalItem"] = "item.modification";
		Defaults["GlobalNPC"] = "npc.modification";
		Defaults["GlobalProjectile"] = "projectile.modification";
		Defaults["GlobalBuff"] = "buff.update";
		Defaults["GlobalTile"] = "tile.modification";
		Defaults["GlobalWall"] = "tile.modification";
		Defaults["ModPlayer"] = "player.update";
		Defaults["ModSystem"] = "world.update";
		Defaults["ModItem"] = "item.modification";
		Defaults["ModNPC"] = "npc.modification";
		Defaults["ModProjectile"] = "projectile.modification";
		Defaults["ModBuff"] = "buff.update";
		Defaults["ModTile"] = "tile.modification";
		Defaults["ModWall"] = "tile.modification";
	}

	private static void Add(string baseType, string systemId, params string[] hookNames)
	{
		if (!Tables.TryGetValue(baseType, out var table)) {
			table = new Dictionary<string, string>(StringComparer.Ordinal);
			Tables[baseType] = table;
		}
		foreach (var hook in hookNames)
			table[hook] = systemId;
	}

	/// <summary>Returns the system id for a hook, or null if the hook is unknown.</summary>
	public static string Categorize(string baseType, string hookName)
	{
		if (Tables.TryGetValue(baseType, out var table) && table.TryGetValue(hookName, out var system))
			return system;
		return Defaults.TryGetValue(baseType, out var def) ? def : null;
	}

	public static bool IsKnownHook(string baseType, string hookName) => Categorize(baseType, hookName) != null;

	/// <summary>All registered hook names for a base type.</summary>
	public static IEnumerable<string> HooksFor(string baseType) => Tables.TryGetValue(baseType, out var table) ? table.Keys : Array.Empty<string>();
}
