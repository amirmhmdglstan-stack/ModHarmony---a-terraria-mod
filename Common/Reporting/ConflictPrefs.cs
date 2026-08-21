using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using ModHarmony.Common.Arbitration;
using ModHarmony.Common.Utilities;

namespace ModHarmony.Common.Reporting;

/// <summary>
/// Player preferences about individual conflicts (currently: mute). Keyed by the
/// stable conflict id, so preferences survive rescans and mod updates. Persisted
/// to {Main.SavePath}/ModHarmony/prefs.json.
/// </summary>
public static class ConflictPrefs
{
	private sealed class Model
	{
		public int Version { get; set; } = 1;
		public List<string> MutedConflictIds { get; set; } = new();
	}

	private static readonly HashSet<string> Muted = new(StringComparer.OrdinalIgnoreCase);
	private static readonly JsonSerializerOptions JsonOptions = new() {
		WriteIndented = true
	};

	private static string PrefsPath {
		get {
			var dir = System.IO.Path.Combine(MainSavePathProvider.Get(), "ModHarmony");
			Directory.CreateDirectory(dir);
			return System.IO.Path.Combine(dir, "prefs.json");
		}
	}

	public static void Load()
	{
		try {
			if (!File.Exists(PrefsPath))
				return;
			var model = JsonSerializer.Deserialize<Model>(File.ReadAllText(PrefsPath), JsonOptions);
			Muted.Clear();
			if (model?.MutedConflictIds != null) {
				foreach (var id in model.MutedConflictIds)
					Muted.Add(id);
			}
		}
		catch (Exception e) {
			Log.Warn($"Could not load conflict preferences: {e.Message}");
		}
	}

	public static void Save()
	{
		try {
			var model = new Model { MutedConflictIds = new List<string>(Muted) };
			File.WriteAllText(PrefsPath, JsonSerializer.Serialize(model, JsonOptions));
		}
		catch (Exception e) {
			Log.Warn($"Could not save conflict preferences: {e.Message}");
		}
	}

	public static bool IsMuted(string conflictId) => Muted.Contains(conflictId);

	public static void SetMuted(string conflictId, bool muted)
	{
		if (muted)
			Muted.Add(conflictId);
		else
			Muted.Remove(conflictId);
		Save();
	}
}
