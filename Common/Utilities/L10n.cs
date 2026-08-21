using Terraria.Localization;

namespace ModHarmony.Common.Utilities;

/// <summary>Localization helpers. All user-facing ModHarmony text lives under Mods.ModHarmony.*.</summary>
public static class L10n
{
	public const string Prefix = "Mods.ModHarmony.";

	/// <summary>Key under Mods.ModHarmony.* (do not include the prefix).</summary>
	public static string Key(string suffix) => Prefix + suffix;

	public static string Text(string suffix) => Language.GetTextValue(Key(suffix));

	public static string Text(string suffix, params object[] args) => Language.GetTextValue(Key(suffix), args);

	public static LocalizedText Localized(string suffix) => Language.GetText(Key(suffix));

	/// <summary>Evidence keys live under Mods.ModHarmony.Evidence.*.</summary>
	public static string EvidenceText(string key, params object[] args) => Language.GetTextValue(Prefix + "Evidence." + key, args);
}
