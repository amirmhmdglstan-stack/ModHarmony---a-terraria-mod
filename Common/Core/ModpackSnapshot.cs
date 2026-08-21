using System;
using System.Collections.Generic;
using System.Linq;

namespace ModHarmony.Common.Core;

/// <summary>
/// Serializable snapshot of one full scan, persisted to disk so later sessions
/// can compute "what changed" (mods added/removed/updated, new conflicts,
/// severity changes, load order changes, new runtime errors...).
/// </summary>
public sealed class ModpackSnapshot
{
	public string FormatVersion { get; set; } = "1";
	public string SessionId { get; set; } = "";
	public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
	public string TModLoaderVersion { get; set; } = "";
	public string TerrariaVersion { get; set; } = "";
	public int ModCount { get; set; }
	public int ConflictCount { get; set; }
	public int HealthScore { get; set; } = -1;
	public List<ModSnapshotEntry> Mods { get; set; } = new();
	public List<ConflictSnapshotEntry> Conflicts { get; set; } = new();
	public List<ErrorSnapshotEntry> RuntimeErrors { get; set; } = new();

	public ModSnapshotEntry GetMod(string internalName) => Mods.Find(m => m.Name == internalName);
}

public sealed class ModSnapshotEntry
{
	public string Name { get; set; } = "";
	public string DisplayName { get; set; } = "";
	public string Version { get; set; } = "";
	public int LoadIndex { get; set; }
	public List<string> Dependencies { get; set; } = new();
	public List<string> WeakDependencies { get; set; } = new();
	public string Author { get; set; } = "";
	public int HookCount { get; set; }
	public List<string> Systems { get; set; } = new();
	public List<string> PatchSignals { get; set; } = new();
}

public sealed class ConflictSnapshotEntry
{
	public string Id { get; set; } = "";
	public string DetectorId { get; set; } = "";
	public string SystemId { get; set; } = "";
	public string Severity { get; set; } = "";
	public string Confidence { get; set; } = "";
	public List<string> Mods { get; set; } = new();
}

public sealed class ErrorSnapshotEntry
{
	public string Timestamp { get; set; } = "";
	public string Type { get; set; } = "";
	public string Message { get; set; } = "";
	public List<string> InvolvedMods { get; set; } = new();
}

/// <summary>Result of comparing two snapshots ("What changed?" feature).</summary>
public sealed class ChangeSet
{
	public bool HasChanges => AddedMods.Count > 0 || RemovedMods.Count > 0 || VersionChanges.Count > 0
		|| NewConflicts.Count > 0 || ResolvedConflicts.Count > 0 || SeverityChanges.Count > 0
		|| LoadOrderChanges.Count > 0 || DependencyChanges.Count > 0 || NewErrors.Count > 0;

	public List<ModSnapshotEntry> AddedMods { get; set; } = new();
	public List<ModSnapshotEntry> RemovedMods { get; set; } = new();
	public List<(string name, string oldVersion, string newVersion)> VersionChanges { get; set; } = new();
	public List<ConflictSnapshotEntry> NewConflicts { get; set; } = new();
	public List<ConflictSnapshotEntry> ResolvedConflicts { get; set; } = new();
	public List<(string conflictId, string oldSeverity, string newSeverity)> SeverityChanges { get; set; } = new();
	public List<(string name, int oldIndex, int newIndex)> LoadOrderChanges { get; set; } = new();
	public List<(string name, string dependency, bool added)> DependencyChanges { get; set; } = new();
	public List<ErrorSnapshotEntry> NewErrors { get; set; } = new();

	/// <summary>How many mods were involved in the change (for the UI headline).</summary>
	public int ChangedModCount => AddedMods.Count + RemovedMods.Count + VersionChanges.Count + LoadOrderChanges.Count;

	public static ChangeSet Compare(ModpackSnapshot before, ModpackSnapshot after)
	{
		var result = new ChangeSet();
		if (before == null || after == null)
			return result;

		var beforeMods = new Dictionary<string, ModSnapshotEntry>(StringComparer.OrdinalIgnoreCase);
		foreach (var m in before.Mods) beforeMods[m.Name] = m;

		var afterMods = new Dictionary<string, ModSnapshotEntry>(StringComparer.OrdinalIgnoreCase);
		foreach (var m in after.Mods) afterMods[m.Name] = m;

		foreach (var m in after.Mods) {
			if (!beforeMods.TryGetValue(m.Name, out var old)) {
				result.AddedMods.Add(m);
				continue;
			}
			if (old.Version != m.Version)
				result.VersionChanges.Add((m.Name, old.Version, m.Version));
			if (old.LoadIndex != m.LoadIndex)
				result.LoadOrderChanges.Add((m.Name, old.LoadIndex, m.LoadIndex));

			// dependencies added/removed
			var oldDeps = new HashSet<string>(old.Dependencies, StringComparer.OrdinalIgnoreCase);
			var newDeps = new HashSet<string>(m.Dependencies, StringComparer.OrdinalIgnoreCase);
			foreach (var d in newDeps)
				if (!oldDeps.Contains(d)) result.DependencyChanges.Add((m.Name, d, true));
			foreach (var d in oldDeps)
				if (!newDeps.Contains(d)) result.DependencyChanges.Add((m.Name, d, false));
		}
		foreach (var m in before.Mods)
			if (!afterMods.ContainsKey(m.Name))
				result.RemovedMods.Add(m);

		var beforeConflicts = new Dictionary<string, ConflictSnapshotEntry>(StringComparer.OrdinalIgnoreCase);
		foreach (var c in before.Conflicts) beforeConflicts[c.Id] = c;
		var afterConflicts = new Dictionary<string, ConflictSnapshotEntry>(StringComparer.OrdinalIgnoreCase);
		foreach (var c in after.Conflicts) afterConflicts[c.Id] = c;

		foreach (var c in after.Conflicts) {
			if (!beforeConflicts.TryGetValue(c.Id, out var old)) {
				result.NewConflicts.Add(c);
				continue;
			}
			if (old.Severity != c.Severity)
				result.SeverityChanges.Add((c.Id, old.Severity, c.Severity));
		}
		foreach (var c in before.Conflicts)
			if (!afterConflicts.ContainsKey(c.Id))
				result.ResolvedConflicts.Add(c);

		var beforeErrors = new HashSet<string>(before.RuntimeErrors.Select(e => e.Timestamp + e.Type + e.Message), StringComparer.Ordinal);
		foreach (var e in after.RuntimeErrors)
			if (!beforeErrors.Contains(e.Timestamp + e.Type + e.Message))
				result.NewErrors.Add(e);

		return result;
	}
}
