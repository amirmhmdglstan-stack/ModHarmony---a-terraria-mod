using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using ModHarmony.Common.Arbitration;
using ModHarmony.Common.Core;
using ModHarmony.Common.Utilities;

namespace ModHarmony.Common.Reporting;

/// <summary>
/// Persists scan snapshots under {Main.SavePath}/ModHarmony/snapshots/:
///  • latest.json — the most recent scan;
///  • session-{SessionId}.json — history used by "What changed?" (pruned).
/// Everything is local; no network access is ever used.
/// </summary>
public static class SnapshotStore
{
	private static readonly JsonSerializerOptions JsonOptions = new() {
		WriteIndented = true,
		ReadCommentHandling = JsonCommentHandling.Skip,
		AllowTrailingCommas = true
	};

	public const int MaxHistoryFiles = 5;

	public static string SnapshotDir {
		get {
			var dir = Path.Combine(MainSavePathProvider.Get(), "ModHarmony", "snapshots");
			Directory.CreateDirectory(dir);
			return dir;
		}
	}

	public static void Save(ModpackSnapshot snapshot)
	{
		try {
			var json = JsonSerializer.Serialize(snapshot, JsonOptions);

			var latest = Path.Combine(SnapshotDir, "latest.json");
			File.WriteAllText(latest + ".tmp", json);
			if (File.Exists(latest))
				File.Delete(latest);
			File.Move(latest + ".tmp", latest);

			var historyPath = Path.Combine(SnapshotDir, $"session-{snapshot.SessionId}.json");
			File.WriteAllText(historyPath + ".tmp", json);
			if (File.Exists(historyPath))
				File.Delete(historyPath);
			File.Move(historyPath + ".tmp", historyPath);

			Prune();
		}
		catch (Exception e) {
			Log.Warn($"Could not save scan snapshot: {e.Message}");
		}
	}

	public static ModpackSnapshot LoadLatest()
	{
		try {
			var path = Path.Combine(SnapshotDir, "latest.json");
			if (!File.Exists(path))
				return null;
			return JsonSerializer.Deserialize<ModpackSnapshot>(File.ReadAllText(path), JsonOptions);
		}
		catch (Exception e) {
			Log.Warn($"Could not load previous snapshot: {e.Message}");
			return null;
		}
	}

	private static void Prune()
	{
		try {
			var files = Directory.GetFiles(SnapshotDir, "session-*.json")
				.OrderByDescending(f => File.GetLastWriteTimeUtc(f))
				.ToList();
			for (int i = MaxHistoryFiles; i < files.Count; i++) {
				try { File.Delete(files[i]); } catch { /* ignore */ }
			}
		}
		catch {
			// Pruning is best-effort.
		}
	}
}
