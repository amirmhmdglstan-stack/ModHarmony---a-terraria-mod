using System;
using System.Collections.Generic;

namespace ModHarmony.Common.Core;

/// <summary>A single overridden hook found on a mod class.</summary>
public sealed class HookUse
{
	public string BaseTypeName { get; set; } = "";  // e.g. "GlobalNPC"
	public string MethodName { get; set; } = "";    // e.g. "AI"
	public string SystemId { get; set; } = "";      // resolved via HookCatalog
	public string DeclaringTypeFullName { get; set; } = ""; // developer detail

	public HookUse() { }

	public HookUse(string baseType, string method, string system, string declaringType)
	{
		BaseTypeName = baseType;
		MethodName = method;
		SystemId = system;
		DeclaringTypeFullName = declaringType;
	}
}

/// <summary>
/// Everything ModHarmony learned about one loaded mod during a scan.
/// This is the per-mod "mod information screen" backing model.
/// </summary>
public sealed class ModFacts
{
	/// <summary>Internal name — the stable identifier of the mod.</summary>
	public string Name { get; set; } = "";

	public string DisplayName { get; set; } = "";
	public Version Version { get; set; } = new(1, 0);
	public string TModLoaderVersion { get; set; } = "";
	public string Author { get; set; } = "";
	public string Homepage { get; set; } = "";
	public string Description { get; set; } = "";
	public string Side { get; set; } = "";
	public bool IsTranslationMod { get; set; }
	public bool IsModHarmony { get; set; }

	/// <summary>Position in the load order (index in ModLoader.Mods). Lower = loaded earlier.</summary>
	public int LoadIndex { get; set; }

	/// <summary>Loaded dependencies (internal names), in dependency resolution order.</summary>
	public List<string> Dependencies { get; set; } = new();

	/// <summary>Optional (weak) dependencies declared in the mod file, when readable.</summary>
	public List<string> WeakDependencies { get; set; } = new();

	/// <summary>Optional dependency not currently loaded.</summary>
	public List<string> MissingOptionalDependencies { get; set; } = new();

	/// <summary>Version expectations from modReferences ("name@version").</summary>
	public List<DependencyExpectation> VersionExpectations { get; set; } = new();

	public List<string> SortAfter { get; set; } = new();
	public List<string> SortBefore { get; set; } = new();

	/// <summary>base type name → number of registered content instances (e.g. ModItem: 12).</summary>
	public Dictionary<string, int> ContentCounts { get; set; } = new();

	/// <summary>system id → number of overridden hooks.</summary>
	public Dictionary<string, int> HookCounts { get; set; } = new();

	/// <summary>All detected hook overrides ("BaseType.MethodName"), for overlap analysis.</summary>
	public List<HookUse> Hooks { get; set; } = new();

	/// <summary>Names of global/content base classes this mod registers (GlobalNPC, GlobalItem, ModPlayer, ModSystem, ModItem, ...).</summary>
	public HashSet<string> GlobalClasses { get; set; } = new();

	/// <summary>Runtime patching signals found in the assembly (IL./On. namespaces, MonoMod references).</summary>
	public List<string> PatchSignals { get; set; } = new();

	/// <summary>True when the mod assembly could not be inspected (e.g. no code).</summary>
	public bool CodeUnavailable { get; set; }

	public string DisplayNameSafe => string.IsNullOrEmpty(DisplayName) ? Name : DisplayName;

	/// <summary>All systems this mod touches (union of HookCounts keys and content systems).</summary>
	public IEnumerable<string> Systems => HookCounts.Keys;

	public int TotalHooks {
		get {
			int n = 0;
			foreach (var kv in HookCounts) n += kv.Value;
			return n;
		}
	}

	public ModFacts Clone() => new() {
		Name = Name,
		DisplayName = DisplayName,
		Version = Version,
		TModLoaderVersion = TModLoaderVersion,
		Author = Author,
		Homepage = Homepage,
		Description = Description,
		Side = Side,
		IsTranslationMod = IsTranslationMod,
		IsModHarmony = IsModHarmony,
		LoadIndex = LoadIndex,
		Dependencies = new List<string>(Dependencies),
		WeakDependencies = new List<string>(WeakDependencies),
		MissingOptionalDependencies = new List<string>(MissingOptionalDependencies),
		VersionExpectations = new List<DependencyExpectation>(VersionExpectations),
		SortAfter = new List<string>(SortAfter),
		SortBefore = new List<string>(SortBefore),
		ContentCounts = new Dictionary<string, int>(ContentCounts),
		HookCounts = new Dictionary<string, int>(HookCounts),
		Hooks = new List<HookUse>(Hooks),
		GlobalClasses = new HashSet<string>(GlobalClasses),
		PatchSignals = new List<string>(PatchSignals),
		CodeUnavailable = CodeUnavailable
	};
}

/// <summary>A declared dependency with an optional version requirement.</summary>
public sealed class DependencyExpectation
{
	public string ModName { get; set; } = "";
	public Version RequiredVersion { get; set; }
	public bool IsWeak { get; set; }
	public bool IsMet { get; set; }

	public DependencyExpectation() { }

	public DependencyExpectation(string modName, Version requiredVersion, bool isWeak, bool isMet)
	{
		ModName = modName;
		RequiredVersion = requiredVersion;
		IsWeak = isWeak;
		IsMet = isMet;
	}
}
