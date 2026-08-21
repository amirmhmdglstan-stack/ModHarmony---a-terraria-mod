using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using ModHarmony.Common.Detection;
using ModHarmony.Common.Utilities;

namespace ModHarmony.Common.Diagnostics;

/// <summary>
/// Maps exceptions (by stack trace) to the mods that are most likely involved.
/// Uses prefix matching against the scanned type namespaces of each loaded mod.
/// IMPORTANT: an exception appearing in a mod's stack trace does NOT prove the
/// mod is at fault — it may simply be where the crash surfaced. Reports say so.
/// </summary>
public static class ErrorCorrelator
{
	private static readonly List<string> ModNamePrefixes = new();
	private static readonly List<string> TypePrefixes = new();

	public static void RebuildIndex(DetectorContext ctx)
	{
		ModNamePrefixes.Clear();
		TypePrefixes.Clear();

		foreach (var mod in ctx.ExceptSelf()) {
			ModNamePrefixes.Add(mod.Name);
			// Namespace prefixes from scanned types (capped to keep memory sane).
			var namespaces = mod.Hooks
				.Select(h => h.DeclaringTypeFullName)
				.Where(n => !string.IsNullOrEmpty(n))
				.Select(n => n.Split('.')[0])
				.Distinct()
				.Take(8);
			TypePrefixes.AddRange(namespaces);
		}
		Log.Debug($"Error correlator index built: {ModNamePrefixes.Count} mods");
	}

	private static readonly Regex FrameRegex = new(@"\bat\s+([A-Za-z_][A-Za-z0-9_\.]*)\.",
		RegexOptions.Compiled | RegexOptions.CultureInvariant);

	/// <summary>Mods whose name or type namespace appears in the stack trace.</summary>
	public static List<string> InvolvedMods(Exception ex)
	{
		var result = new List<string>();
		if (ex == null)
			return result;

		var haystack = (ex.StackTrace ?? "") + "\n" + (ex.GetType().FullName ?? "");
		foreach (var prefix in ModNamePrefixes) {
			if (haystack.Contains(prefix, StringComparison.Ordinal))
				result.Add(prefix);
		}
		foreach (var prefix in TypePrefixes) {
			if (haystack.Contains(prefix, StringComparison.Ordinal) && !result.Contains(prefix))
				result.Add(prefix);
		}
		return result;
	}

	public static string[] Frames(Exception ex, int maxFrames)
	{
		if (string.IsNullOrEmpty(ex?.StackTrace))
			return Array.Empty<string>();
		var frames = ex.StackTrace.Split('\n')
			.Select(f => f.Trim())
			.Where(f => f.Length > 0)
			.Take(maxFrames)
			.ToArray();
		return frames;
	}
}
