using System;
using System.Collections.Generic;
using System.Linq;
using ModHarmony.Common.Core;
using ModHarmony.Content.Config;

namespace ModHarmony.Common.Detection;

/// <summary>
/// Dependency-related interactions that are actually visible from the public
/// API (AssemblyManager.GetDependencies) plus best-effort .tmod metadata:
///  • dependency cycles among loaded mods (load order between them is decided
///    by tModLoader heuristics and can change between versions);
///  • declared optional (weak) dependencies that are not loaded.
/// Missing hard dependencies and unmet version requirements are NOT reported:
/// tModLoader refuses to load a mod in either situation, so they cannot occur
/// for loaded mods.
/// </summary>
public sealed class DependencyDetector : IConflictDetector
{
	public string Id => "Dependency";
	public string NameKey => "Detectors.Dependency.Name";
	public string DescriptionKey => "Detectors.Dependency.Description";

	public bool IsEnabled(ModHarmonyConfig config) => config.ScanDependencies;

	public List<Conflict> Detect(DetectorContext context)
	{
		var result = new List<Conflict>();

		// --- Dependency cycles ---------------------------------------------
		foreach (var cycle in FindCycles(context)) {
			var mods = cycle.OrderBy(m => m, StringComparer.OrdinalIgnoreCase).ToList();
			var conflict = new Conflict {
				Id = Conflict.MakeStableId(Id, "dependency.cycle", mods),
				DetectorId = Id,
				SystemId = "dependency.cycle",
				Severity = Severity.Low,
				Confidence = Confidence.Confirmed,
				Mods = mods
			};
			conflict.Evidence.Add(new Evidence(EvidenceKind.Dependency, null,
				"Dependency.Cycle", string.Join(" -> ", mods)));
			conflict.Evidence[0].DevDetail = "cycle: " + string.Join(" -> ", mods);
			result.Add(conflict);
			context.LoadOrderWarnings.Add($"dependency cycle: {string.Join(", ", mods)}");
		}

		// NOTE: unmet hard version requirements are NOT reported here because
		// tModLoader refuses to load a mod whose declared version requirements
		// are not met (ModOrganizer.EnsureTargetVersionsMet). Such a situation
		// cannot exist among loaded mods, so reporting it would be dishonest.

		// --- Missing optional (weak) dependencies --------------------------
		foreach (var mod in context.ExceptSelf()) {
			foreach (var missing in mod.MissingOptionalDependencies) {
				var conflict = new Conflict {
					Id = Conflict.MakeStableId(Id, "dependency.optional", new[] { mod.Name, missing }),
					DetectorId = Id,
					SystemId = "dependency.optional",
					Severity = Severity.Info,
					Confidence = Confidence.Confirmed,
					Mods = new List<string> { mod.Name, missing },
					IsConditional = true
				};
				conflict.Evidence.Add(new Evidence(EvidenceKind.Dependency, null,
					"Dependency.MissingOptional", mod.Name, missing));
				result.Add(conflict);
			}
		}

		return result;
	}

	/// <summary>Iterative DFS cycle detection over the loaded dependency graph.</summary>
	private static IEnumerable<List<string>> FindCycles(DetectorContext ctx)
	{
		var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		var stack = new List<string>();
		var inStack = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		var reported = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

		foreach (var mod in ctx.ExceptSelf()) {
			if (visited.Contains(mod.Name))
				continue;
			foreach (var cycle in Dfs(mod.Name, ctx, visited, stack, inStack, reported))
				yield return cycle;
		}
	}

	private static IEnumerable<List<string>> Dfs(string node, DetectorContext ctx, HashSet<string> visited,
		List<string> stack, HashSet<string> inStack, HashSet<string> reported)
	{
		if (inStack.Contains(node)) {
			// Found a cycle: extract it from the stack.
			int idx = stack.IndexOf(node);
			if (idx < 0)
				yield break;
			var cycle = stack.Skip(idx).Append(node).ToList();
			var key = string.Join(",", cycle.OrderBy(x => x, StringComparer.OrdinalIgnoreCase));
			if (reported.Add(key))
				yield return cycle;
			yield break;
		}
		if (visited.Contains(node))
			yield break;

		visited.Add(node);
		stack.Add(node);
		inStack.Add(node);

		var facts = ctx.Get(node);
		if (facts != null) {
			foreach (var dep in facts.Dependencies) {
				if (ctx.Get(dep) == null)
					continue; // only follow loaded mods
				foreach (var cycle in Dfs(dep, ctx, visited, stack, inStack, reported))
					yield return cycle;
			}
		}

		inStack.Remove(node);
		stack.RemoveAt(stack.Count - 1);
	}
}
