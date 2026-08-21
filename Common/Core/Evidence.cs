using System;

namespace ModHarmony.Common.Core;

/// <summary>What kind of technical fact an evidence item represents.</summary>
public enum EvidenceKind
{
	HookOverride,
	ContentRegistration,
	Recipe,
	Dependency,
	Asset,
	RuntimePatch,
	RuntimeError,
	Configuration,
	General
}

/// <summary>
/// One piece of evidence backing up a <see cref="Conflict"/>. Evidence is stored
/// as a localization key plus arguments so it renders in the player's language
/// both in the UI and in exported reports. Structured details are preserved in
/// <see cref="DevDetail"/> for Developer Mode.
/// </summary>
public sealed class Evidence
{
	public EvidenceKind Kind { get; set; } = EvidenceKind.General;

	/// <summary>Internal mod name this evidence refers to, or null when it refers to the situation as a whole.</summary>
	public string ModName { get; set; }

	/// <summary>Localization key (suffix of Mods.ModHarmony.Evidence.{key}).</summary>
	public string Key { get; set; } = "";

	/// <summary>Arguments for the localized string.</summary>
	public string[] Args { get; set; } = Array.Empty<string>();

	/// <summary>Raw technical detail shown only in Developer Mode (type names, hook names, ids...).</summary>
	public string DevDetail { get; set; } = "";

	public Evidence() { }

	public Evidence(EvidenceKind kind, string modName, string key, params string[] args)
	{
		Kind = kind;
		ModName = modName;
		Key = key;
		Args = args ?? Array.Empty<string>();
	}
}
