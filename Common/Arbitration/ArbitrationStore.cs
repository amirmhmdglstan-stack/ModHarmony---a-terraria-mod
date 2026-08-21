using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using ModHarmony.Common.Utilities;

namespace ModHarmony.Common.Arbitration;

/// <summary>
/// Persists arbitration groups (strategies, seeds, weights, manual priorities,
/// locks and resolved winners) to
/// <c>{Main.SavePath}/ModHarmony/arbitration.json</c> using stable group ids.
/// </summary>
public static class ArbitrationStore
{
	private const string FileName = "arbitration.json";

	private sealed class FileModel
	{
		public int Version { get; set; } = 1;
		public List<ArbitrationGroup> Groups { get; set; } = new();
	}

	private static readonly JsonSerializerOptions JsonOptions = new() {
		WriteIndented = true,
		ReadCommentHandling = JsonCommentHandling.Skip,
		AllowTrailingCommas = true
	};

	public static string SaveDirectory {
		get {
			var dir = Path.Combine(MainSavePath, "ModHarmony");
			Directory.CreateDirectory(dir);
			return dir;
		}
	}

	private static string MainSavePath => MainSavePathProvider.Get();

	public static List<ArbitrationGroup> Load()
	{
		try {
			var path = Path.Combine(SaveDirectory, FileName);
			if (!File.Exists(path))
				return new List<ArbitrationGroup>();
			var json = File.ReadAllText(path);
			var model = JsonSerializer.Deserialize<FileModel>(json, JsonOptions);
			return model?.Groups ?? new List<ArbitrationGroup>();
		}
		catch (Exception e) {
			Log.Warn($"Could not load arbitration data: {e.Message}");
			return new List<ArbitrationGroup>();
		}
	}

	public static void Save(IEnumerable<ArbitrationGroup> groups)
	{
		try {
			var model = new FileModel { Groups = new List<ArbitrationGroup>(groups) };
			var path = Path.Combine(SaveDirectory, FileName);
			var tmp = path + ".tmp";
			File.WriteAllText(tmp, JsonSerializer.Serialize(model, JsonOptions));
			if (File.Exists(path))
				File.Delete(path);
			File.Move(tmp, path);
		}
		catch (Exception e) {
			Log.Warn($"Could not save arbitration data: {e.Message}");
		}
	}
}

/// <summary>Indirection so the save path can be resolved lazily (Main.SavePath is available in-game only).</summary>
public static class MainSavePathProvider
{
	public static Func<string> Get = () => Terraria.Main.SavePath;
}
