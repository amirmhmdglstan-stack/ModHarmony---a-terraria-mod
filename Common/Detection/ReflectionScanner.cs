using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using ModHarmony.Common.Core;
using ModHarmony.Common.Utilities;
using Terraria.ModLoader;
using Terraria.ModLoader.Core;

namespace ModHarmony.Common.Detection;

/// <summary>
/// Safely inspects a loaded mod's assembly and produces its <see cref="ModFacts"/>.
/// Uses only the public tModLoader APIs intended for this purpose:
/// <see cref="AssemblyManager.GetLoadableTypes"/> (safe against weak-reference
/// load failures) and <see cref="Mod.GetContent{T}"/> for registered content.
/// Runs once at load time and caches the result.
/// </summary>
public static class ReflectionScanner
{
	// Base class names we care about for hook scanning. Types outside this list
	// are skipped after a cheap IsSubclassOf check.
	private static readonly string[] HookBaseTypes = {
		"GlobalItem", "GlobalNPC", "GlobalProjectile", "GlobalBuff",
		"GlobalTile", "GlobalWall", "ModPlayer", "ModSystem"
	};

	private static readonly string[] GlobalBaseTypes = {
		"GlobalItem", "GlobalNPC", "GlobalProjectile", "GlobalBuff", "GlobalTile",
		"GlobalWall", "GlobalBossBar", "GlobalInfoDisplay", "GlobalPylon",
		"GlobalEmoteBubble", "GlobalBuilderToggle", "GlobalLoot"
	};

	/// <summary>Base types whose registered content is counted per mod.</summary>
	private static readonly string[] ContentBaseTypes = {
		"ModItem", "ModNPC", "ModProjectile", "ModBuff", "ModTile", "ModWall",
		"ModMount", "ModDust", "ModGore", "ModRarity", "ModPrefix", "ModKeybind",
		"ModCommand", "ModConfig", "ModTileEntity", "ModBiome", "ModSceneEffect",
		"ModMenu", "ModPylon", "ModWaterStyle", "ModBackgroundStyle", "ModBossBar",
		"ModCloud", "ModHair", "ModEmoteBubble", "ModMapLayer", "ModResourceDisplaySet",
		"ModResourceOverlay", "ModAchievement", "ModAccessorySlot", "ModSystem",
		"ModPlayer"
	};

	public static ModFacts Scan(Mod mod, int loadIndex, InstalledModInfo meta)
	{
		var facts = new ModFacts {
			Name = mod.Name,
			DisplayName = mod.DisplayNameClean,
			Version = mod.Version ?? new Version(1, 0),
			TModLoaderVersion = mod.TModLoaderVersion?.ToString() ?? "",
			Side = mod.Side.ToString(),
			LoadIndex = loadIndex,
			IsModHarmony = mod.Name == ModHarmony.ModName
		};

		if (meta != null) {
			facts.Author = meta.Author;
			facts.Homepage = meta.Homepage;
			facts.Description = meta.Description;
			facts.IsTranslationMod = meta.IsTranslationMod;
			facts.SortAfter = meta.SortAfter;
			facts.SortBefore = meta.SortBefore;
		}

		if (mod.Code != null) {
			try {
				var types = AssemblyManager.GetLoadableTypes(mod.Code);
				if (types != null)
					ScanTypes(facts, types);
			}
			catch (Exception e) {
				facts.CodeUnavailable = true;
				Log.Warn($"Failed to inspect assembly of {mod.Name}: {e.Message}");
			}
		}
		else {
			facts.CodeUnavailable = true;
		}

		// Content counts come from the authoritative content registry.
		try {
			foreach (var loadable in mod.GetContent()) {
				var type = loadable.GetType();
				if (type == null) continue;
				var baseName = FindBaseTypeName(type, ContentBaseTypes);
				if (baseName == null) continue;
				facts.ContentCounts.TryGetValue(baseName, out var n);
				facts.ContentCounts[baseName] = n + 1;
				if (GlobalBaseTypes.Contains(baseName) || baseName == "ModPlayer" || baseName == "ModSystem")
					facts.GlobalClasses.Add(baseName);
			}
		}
		catch (Exception e) {
			Log.Warn($"Failed to enumerate content of {mod.Name}: {e.Message}");
		}

		// Dependencies via the public API.
		try {
			foreach (var dep in AssemblyManager.GetDependencies(mod)) {
				if (dep != null && !string.IsNullOrEmpty(dep.Name))
					facts.Dependencies.Add(dep.Name);
			}
		}
		catch (Exception e) {
			Log.Debug($"Could not resolve dependencies of {mod.Name}: {e.Message}");
		}

		// Weak dependencies from metadata.
		if (meta != null) {
			foreach (var weak in meta.WeakReferences) {
				facts.WeakDependencies.Add(weak);
				if (!ModLoader.HasMod(weak)) {
					facts.MissingOptionalDependencies.Add(weak);
					facts.VersionExpectations.Add(new DependencyExpectation(weak, null, true, false));
				}
			}
			// Version expectations from modReferences are only checked when the target is loaded.
			foreach (var refName in meta.ModReferences) {
				var at = refName.IndexOf('@');
				if (at <= 0) continue;
				var name = refName.Substring(0, at);
				if (Version.TryParse(refName.Substring(at + 1), out var required)) {
					var isMet = ModLoader.TryGetMod(name, out var target) && target.Version != null && target.Version >= required;
					facts.VersionExpectations.Add(new DependencyExpectation(name, required, false, isMet));
				}
			}
		}

		return facts;
	}

	private static void ScanTypes(ModFacts facts, Type[] types)
	{
		foreach (var type in types) {
			if (type == null || type.IsAbstract || type.IsInterface || type.ContainsGenericParameters || type.IsEnum)
				continue;

			var baseName = FindBaseTypeName(type, HookBaseTypes);
			if (baseName == null)
				continue;

			facts.GlobalClasses.Add(baseName);

			MethodInfo[] methods;
			try {
				methods = type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
			}
			catch (Exception) {
				continue; // Malformed type; skip it rather than failing the whole mod.
			}

			foreach (var m in methods) {
				if (m == null || !m.IsVirtual)
					continue;

				var systemId = HookCatalog.Categorize(baseName, m.Name);
				if (systemId == null)
					continue;

				facts.Hooks.Add(new HookUse(baseName, m.Name, systemId, type.FullName ?? type.Name));
				facts.HookCounts.TryGetValue(systemId, out var count);
				facts.HookCounts[systemId] = count + 1;
			}
		}
	}

	private static string FindBaseTypeName(Type type, string[] candidates)
	{
		for (var current = type.BaseType; current != null && current != typeof(object); current = current.BaseType) {
			var name = current.Name;
			if (candidates.Contains(name))
				return name;
			if (current.Namespace != "Terraria.ModLoader")
				continue; // Only tModLoader base classes count.
		}
		return null;
	}

	/// <summary>Used by detectors to check whether a mod overrides a specific hook.</summary>
	public static bool Overrides(ModFacts facts, string baseType, string hook) =>
		facts.Hooks.Any(h => h.BaseTypeName == baseType && h.MethodName == hook);
}
