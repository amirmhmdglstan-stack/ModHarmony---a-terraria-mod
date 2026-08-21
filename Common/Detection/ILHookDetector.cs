using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using ModHarmony.Common.Core;
using ModHarmony.Content.Config;
using Terraria.ModLoader.Core;

namespace ModHarmony.Common.Detection;

/// <summary>
/// Runtime-patching detection. IL edits and detours (MonoMod) are among the
/// most common causes of hard-to-diagnose mod interactions, so we look for
/// static, verifiable signs that a mod patches game code:
///  • types in "IL." or "On." namespaces (the tModLoader/MonoMod conventions);
///  • methods named IL_Terraria_* / On_Terraria_*;
///  • assembly references to MonoMod / Mono.Cecil (required to compile IL
///    patches against the MonoModHooks API).
/// We deliberately do NOT attempt to read or intercept other mods' patches; we
/// cannot know which specific methods two mods patch, and the report says so.
/// </summary>
public sealed class ILHookDetector : IConflictDetector
{
	public string Id => "ILHooks";
	public string NameKey => "Detectors.ILHooks.Name";
	public string DescriptionKey => "Detectors.ILHooks.Description";

	public bool IsEnabled(ModHarmonyConfig config) => config.ScanILHooks;

	private static readonly string[] PatchNamespacePrefixes = { "IL.", "On." };
	private static readonly string[] PatchAssemblyNames = { "MonoMod.RuntimeDetour", "MonoMod.Utils", "Mono.Cecil" };

	public List<Conflict> Detect(DetectorContext context)
	{
		var result = new List<Conflict>();
		var patchers = new List<string>();

		foreach (var mod in context.ExceptSelf()) {
			var signals = ScanAssembly(mod);
			if (signals.Count == 0)
				continue;
			mod.PatchSignals.AddRange(signals);
			patchers.Add(mod.Name);

			// Single-mod informational: this mod patches game code.
			var conflict = new Conflict {
				Id = Conflict.MakeStableId(Id, "runtime.patch", new[] { mod.Name }),
				DetectorId = Id,
				SystemId = "runtime.patch",
				Severity = Severity.Info,
				Confidence = signals.Any(s => s.StartsWith("convention", StringComparison.Ordinal)) ? Confidence.Strong : Confidence.Possible,
				Mods = new List<string> { mod.Name }
			};
			conflict.Evidence.Add(new Evidence(EvidenceKind.RuntimePatch, null,
				"ILHooks.SingleMod", mod.Name, string.Join(", ", signals.Take(4))));
			result.Add(conflict);
		}

		// Pairwise: two or more mods patch game code. We cannot determine whether
		// they patch the *same* method, so confidence stays Possible.
		if (patchers.Count >= 2) {
			var patchersSorted = patchers.OrderBy(m => m, StringComparer.OrdinalIgnoreCase).ToList();
			for (int i = 0; i < patchersSorted.Count; i++) {
				for (int j = i + 1; j < patchersSorted.Count; j++) {
					var a = patchersSorted[i];
					var b = patchersSorted[j];
					var conflict = new Conflict {
						Id = Conflict.MakeStableId(Id, "runtime.patch", new[] { a, b }),
						DetectorId = Id,
						SystemId = "runtime.patch",
						Severity = Severity.Medium,
						Confidence = Confidence.Possible,
						Mods = new List<string> { a, b }
					};
					conflict.Evidence.Add(new Evidence(EvidenceKind.RuntimePatch, null,
						"ILHooks.Pair", a, b));
					result.Add(conflict);
				}
			}
		}

		return result;
	}

	private static List<string> ScanAssembly(ModFacts mod)
	{
		var signals = new List<string>();
		try {
			var code = GetModCode(mod);
			if (code == null)
				return signals;

			var types = AssemblyManager.GetLoadableTypes(code);
			if (types == null)
				return signals;

			foreach (var type in types) {
				if (type == null)
					continue;
				var ns = type.Namespace ?? "";
				foreach (var prefix in PatchNamespacePrefixes) {
					if (ns.StartsWith(prefix, StringComparison.Ordinal)) {
						signals.Add($"convention:{prefix}");
						break;
					}
				}
				// Method-name conventions (avoid scanning types that cannot hold patches).
				if (signals.Any(s => s.StartsWith("convention", StringComparison.Ordinal)))
					break;
			}

			foreach (var type in types) {
				if (type == null || type.IsAbstract || type.IsInterface)
					continue;
				MethodInfo[] methods;
				try {
					methods = type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly);
				}
				catch {
					continue;
				}
				foreach (var m in methods) {
					if (m.Name.StartsWith("IL_Terraria_", StringComparison.Ordinal) || m.Name.StartsWith("On_Terraria_", StringComparison.Ordinal)) {
						signals.Add($"method:{m.Name}");
						if (signals.Count >= 8)
							break;
					}
				}
			}

			try {
				var refs = code.GetReferencedAssemblies();
				foreach (var r in refs) {
					foreach (var patchAsm in PatchAssemblyNames) {
						if (r.Name.StartsWith(patchAsm, StringComparison.Ordinal)) {
							signals.Add($"reference:{r.Name}");
							break;
						}
					}
				}
			}
			catch {
				// Referenced-assembly inspection failed; the other signals still stand.
			}
		}
		catch (Exception) {
			// Assembly inspection failed for this mod; skip it quietly.
		}

		return signals.Distinct().ToList();
	}

	private static Assembly GetModCode(ModFacts mod)
	{
		// ModFacts does not keep the Assembly reference (it is scan-time only);
		// resolve it through the currently loaded mod.
		if (Terraria.ModLoader.ModLoader.TryGetMod(mod.Name, out var m))
			return m.Code;
		return null;
	}
}
