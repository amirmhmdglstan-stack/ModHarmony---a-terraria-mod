using System;
using System.Collections.Generic;
using System.Linq;
using ModHarmony.Common.Core;
using ModHarmony.Common.Utilities;

namespace ModHarmony.Common.Detection;

/// <summary>
/// Shared logic for the hook-overlap style detectors. Given the scanned mod
/// facts, it finds mods that override hooks in the same game system and turns
/// them into conflicts:
///  • small contestant sets (≤ maxPairMods) → one conflict per mod pair,
///    listing the shared hooks as evidence;
///  • large contestant sets → one aggregated conflict per system, because N×N
///    pair rows would drown the player in noise.
/// </summary>
public static class OverlapAnalyzer
{
	public sealed class Options
	{
		public string DetectorId = "";
		public string[] BaseTypes = Array.Empty<string>();
		public Dictionary<string, Severity> SeverityBySystem = new();
		public Severity DefaultSeverity = Severity.Low;
		public Confidence Confidence = Confidence.Strong;
		public int MaxPairMods = 8;
		public string EvidenceModHooksKey = "HookOverlap.ModHooks";
		public string EvidenceAggregateKey = "HookOverlap.Aggregate";
		public string EvidenceWhyKey = "HookOverlap.Why";
		/// <summary>Only report pairs that share at least one *specific* hook.</summary>
		public bool RequireSharedSpecificHook = true;
	}

	public static List<Conflict> Analyze(DetectorContext ctx, Options options)
	{
		var result = new List<Conflict>();

		// systemId → (modName → set of "BaseType.Hook" strings)
		var bySystem = new Dictionary<string, Dictionary<string, HashSet<string>>>();
		var self = ctx.Get(ModHarmony.ModName);

		foreach (var mod in ctx.ExceptSelf()) {
			foreach (var hook in mod.Hooks) {
				if (!options.BaseTypes.Contains(hook.BaseTypeName))
					continue;
				if (!bySystem.TryGetValue(hook.SystemId, out var mods)) {
					mods = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
					bySystem[hook.SystemId] = mods;
				}
				if (!mods.TryGetValue(mod.Name, out var hooks)) {
					hooks = new HashSet<string>(StringComparer.Ordinal);
					mods[mod.Name] = hooks;
				}
				hooks.Add($"{hook.BaseTypeName}.{hook.MethodName}");
			}
		}

		foreach (var kv in bySystem) {
			var systemId = kv.Key;
			var mods = kv.Value;
			var involved = mods.Keys.OrderBy(m => m, StringComparer.OrdinalIgnoreCase).ToArray();
			if (involved.Length < 2)
				continue;

			var severity = options.SeverityBySystem.TryGetValue(systemId, out var s) ? s : options.DefaultSeverity;
			var systemName = SafeSystemName(systemId);

			if (involved.Length <= options.MaxPairMods) {
				for (int i = 0; i < involved.Length; i++) {
					for (int j = i + 1; j < involved.Length; j++) {
						var a = involved[i];
						var b = involved[j];
						var shared = new HashSet<string>(mods[a], StringComparer.Ordinal);
						shared.IntersectWith(mods[b]);

						if (options.RequireSharedSpecificHook && shared.Count == 0)
							continue;

						var pairSeverity = severity;
						if (shared.Count >= 3 && pairSeverity < Severity.Significant)
							pairSeverity = (Severity)Math.Min((int)pairSeverity + 1, (int)Severity.Significant);

						var conflict = new Conflict {
							Id = Conflict.MakeStableId(options.DetectorId, systemId, new[] { a, b }),
							DetectorId = options.DetectorId,
							SystemId = systemId,
							Severity = pairSeverity,
							Confidence = options.Confidence,
							Mods = new List<string> { a, b }
						};

						conflict.Evidence.Add(new Evidence(EvidenceKind.HookOverride, a,
							options.EvidenceModHooksKey, a, systemName, string.Join(", ", mods[a].OrderBy(x => x))));
						conflict.Evidence.Add(new Evidence(EvidenceKind.HookOverride, b,
							options.EvidenceModHooksKey, b, systemName, string.Join(", ", mods[b].OrderBy(x => x))));
						conflict.Evidence.Add(new Evidence(EvidenceKind.General, null,
							options.EvidenceWhyKey, systemName));

						// Developer detail: exact shared hook names.
						conflict.Evidence[0].DevDetail = string.Join(", ", shared.OrderBy(x => x));
						conflict.ArbitrationGroupId = ArbitrationGroupIdFor(systemId);
						result.Add(conflict);
					}
				}
			}
			else {
				// Large contestant set: aggregate.
				var cap = Math.Min(involved.Length, 20);
				var top = involved.Take(cap).ToList();
				var conflict = new Conflict {
					Id = Conflict.MakeStableId(options.DetectorId, systemId, top),
					DetectorId = options.DetectorId,
					SystemId = systemId,
					Severity = severity,
					Confidence = options.Confidence,
					Mods = top
				};
				var names = string.Join(", ", top.Select(n => DisplayNameOf(ctx, n)));
				if (involved.Length > cap)
					names += $" (+{involved.Length - cap})";
				conflict.Evidence.Add(new Evidence(EvidenceKind.General, null,
					options.EvidenceAggregateKey, involved.Length.ToString(), systemName, names));
				conflict.ArbitrationGroupId = ArbitrationGroupIdFor(systemId);
				result.Add(conflict);
			}
		}

		return result;
	}

	/// <summary>Arbitration groups are keyed by system id; systems we can arbitrate expose a point.</summary>
	public static string ArbitrationGroupIdFor(string systemId) => $"system.{systemId}";

	private static string SafeSystemName(string systemId)
	{
		try {
			return L10n.Text(SystemRegistry.Get(systemId).NameKey);
		}
		catch {
			return systemId;
		}
	}

	private static string DisplayNameOf(DetectorContext ctx, string modName)
	{
		var facts = ctx.Get(modName);
		return facts != null ? facts.DisplayNameSafe : modName;
	}
}
