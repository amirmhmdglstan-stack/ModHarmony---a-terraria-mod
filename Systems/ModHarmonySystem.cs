using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Microsoft.Xna.Framework;
using ModHarmony.Common.Arbitration;
using ModHarmony.Common.Core;
using ModHarmony.Common.Diagnostics;
using ModHarmony.Common.Detection;
using ModHarmony.Common.Reporting;
using ModHarmony.Common.Utilities;
using ModHarmony.Content.Config;
using Terraria;
using Terraria.ModLoader;

namespace ModHarmony.Systems;

/// <summary>
/// Core ModSystem: orchestrates the scan pipeline (metadata → mod facts →
/// recipe snapshot → detectors → store → health → snapshot → arbitration sync),
/// drives Investigation Mode's frame sampling, and exposes the UI hotkey.
/// </summary>
public sealed class ModHarmonySystem : ModSystem
{
	public static ModKeybind OpenUIKeybind { get; private set; }

	private static bool _rescanQueued;

	public override void Load()
	{
		ConflictPrefs.Load();
		Log.Info("ModHarmonySystem loaded");

		if (!Main.dedServ) {
			OpenUIKeybind = KeybindLoader.RegisterKeybind(Mod, "OpenUI", "N");
		}
	}

	public override void PostSetupRecipes()
	{
		var config = ModContent.GetInstance<ModHarmonyConfig>();
		if (config == null || !config.EnableModHarmony)
			return;
		if (config.RunStartupScan)
			RunScan();
	}

	public override void UpdateUI(GameTime gameTime)
	{
		PerformanceTracker.Tick();

		if (OpenUIKeybind?.JustPressed == true && !Main.dedServ) {
			UI.UIHelper.Toggle();
		}

		if (_rescanQueued) {
			_rescanQueued = false;
			RunScan();
		}
	}

	/// <summary>Queues a rescan to happen on the next UI update (main thread).</summary>
	public static void QueueRescan() => _rescanQueued = true;

	/// <summary>Runs the full scan pipeline. Safe to call again later (recipes are re-snapshotted).</summary>
	public static void RunScan()
	{
		if (ScanState.IsScanning)
			return;
		ScanState.IsScanning = true;

		try {
			var config = ModContent.GetInstance<ModHarmonyConfig>();
			if (config == null)
				return;
			if (!config.EnableModHarmony) {
				Log.Debug("Scan skipped: ModHarmony disabled in config");
				return;
			}

			Log.SetLevel(config.LogLevel);

			var ctx = new DetectorContext { Config = config };
			var sw = System.Diagnostics.Stopwatch.StartNew();

			// 1. Installed mod file metadata (best effort).
			ScanModFolder(ctx);

			// 2. Per-mod facts via safe reflection + content registry.
			var mods = ModLoader.Mods;
			for (int i = 0; i < mods.Length; i++) {
				var mod = mods[i];
				// Skip tModLoader's built-in "ModLoader" mod (index 0): its
				// assembly is tML itself and must never appear in the analysis.
				if (string.Equals(mod.Name, "ModLoader", StringComparison.OrdinalIgnoreCase))
					continue;
				var meta = ctx.InstalledMods.FirstOrDefault(m => string.Equals(m.Name, mod.Name, StringComparison.OrdinalIgnoreCase));
				var facts = ReflectionScanner.Scan(mod, i, meta);
				ctx.Mods.Add(facts);
				ctx.ByName[facts.Name] = facts;
				if (mod.Code != null && meta == null && !facts.CodeUnavailable)
					ctx.MetadataUnreadable.Add(mod.Name);
			}

			// 3. Recipe snapshot (safe even in-game: recipes are stable between reloads).
			ctx.Recipes = RecipeScanner.Build();

			ctx.RebuildSystemOverlapCounts();
			ErrorCorrelator.RebuildIndex(ctx);

			// 4. Run detectors (each isolated).
			var conflicts = new DetectorManager().RunAll(ctx, ScanState.Store, config);

			// 5. Arbitration groups: merge persisted config with freshly detected systems.
			SyncArbitrationGroups(ctx, config, conflicts);

			// 6. Publish results.
			ScanState.Store.ReplaceAll(conflicts);
			ScanState.Context = ctx;
			ScanState.LastScanTime = DateTime.Now;
			ScanState.ScanRunCount++;

			// 7. Health score (heuristic, fully itemized).
			ScanState.Health = HealthCalculator.Calculate(
				conflicts,
				ctx.SystemOverlapCounts,
				ctx.LoadOrderWarnings,
				ScanState.Store.GetDetectorStatuses());

			// 8. Snapshot + "what changed".
			var snapshot = BuildSnapshot(ctx, conflicts);
			var previous = SnapshotStore.LoadLatest();
			ScanState.ChangeSet = ChangeSet.Compare(previous, snapshot);
			ScanState.Snapshot = snapshot;
			SnapshotStore.Save(snapshot);

			// 9. Runtime monitoring state from config.
			bool investigate = config.RuntimeMonitoring && !Main.dedServ;
			RuntimeMonitor.MaxEvents = config.MaxRetainedEvents;
			PerformanceTracker.SpikeThresholdMs = config.FrameSpikeThresholdMs;
			RuntimeMonitor.SetActive(investigate);
			PerformanceTracker.SetActive(investigate && !Main.dedServ);

			sw.Stop();
			Log.Info($"Scan finished in {sw.ElapsedMilliseconds} ms — {ctx.Mods.Count} mods, {conflicts.Count} conflicts, health {ScanState.Health.Score}");
			if (ScanState.ChangeSet?.HasChanges == true)
				Log.Info($"What changed since last session: {ScanState.ChangeSet.ChangedModCount} mods, {ScanState.ChangeSet.NewConflicts.Count} new conflicts");
		}
		catch (Exception e) {
			Log.Error("ModHarmony scan failed", e);
		}
		finally {
			ScanState.IsScanning = false;
		}
	}

	/// <summary>Re-persists the snapshot (with accumulated runtime errors) at save-and-quit.</summary>
	public override void PreSaveAndQuit()
	{
		if (ScanState.Snapshot != null) {
			ScanState.Snapshot.RuntimeErrors = CaptureErrors();
			SnapshotStore.Save(ScanState.Snapshot);
		}
	}

	public override void OnWorldUnload()
	{
		if (ScanState.Snapshot != null) {
			ScanState.Snapshot.RuntimeErrors = CaptureErrors();
			SnapshotStore.Save(ScanState.Snapshot);
		}
	}

	private static List<ErrorSnapshotEntry> CaptureErrors()
	{
		var result = new List<ErrorSnapshotEntry>();
		foreach (var e in RuntimeMonitor.GetEvents().Take(50)) {
			result.Add(new ErrorSnapshotEntry {
				Timestamp = e.Timestamp.ToString("yyyy-MM-dd HH:mm:ss"),
				Type = e.Type,
				Message = e.Message,
				InvolvedMods = e.InvolvedMods
			});
		}
		return result;
	}

	private static void ScanModFolder(DetectorContext ctx)
	{
		try {
			var modPath = ModLoader.ModPath;
			if (string.IsNullOrEmpty(modPath) || !Directory.Exists(modPath))
				return;

			foreach (var file in Directory.GetFiles(modPath, "*.tmod")) {
				var info = ModMetadataReader.Read(file);
				if (info != null) {
					info.Loaded = ModLoader.HasMod(info.Name);
					ctx.InstalledMods.Add(info);
				}
			}
		}
		catch (Exception e) {
			Log.Debug($"Mod folder scan incomplete: {e.Message}");
		}
	}

	private static void SyncArbitrationGroups(DetectorContext ctx, ModHarmonyConfig config, List<Conflict> freshConflicts)
	{
		var groups = config.PersistDecisions ? ArbitrationStore.Load() : new List<ArbitrationGroup>();
		var byId = groups.ToDictionary(g => g.GroupId, g => g, StringComparer.OrdinalIgnoreCase);

		// Merge newly detected contested systems into persisted groups.
		foreach (var conflict in freshConflicts) {
			if (string.IsNullOrEmpty(conflict.ArbitrationGroupId))
				continue;
			if (!byId.TryGetValue(conflict.ArbitrationGroupId, out var group)) {
				group = new ArbitrationGroup {
					GroupId = conflict.ArbitrationGroupId,
					SystemId = conflict.SystemId,
					Strategy = config.DefaultStrategy
				};
				groups.Add(group);
				byId[group.GroupId] = group;
			}
			group.MechanismAvailable = ArbitrationPoints.HasPoint(group.SystemId);
			foreach (var modName in conflict.Mods) {
				var facts = ctx.Get(modName);
				group.EnsureCandidate(modName, facts?.LoadIndex ?? int.MaxValue);
			}
		}

		// Arbitrable systems touched by any loaded mod get a group too, so
		// single-mod opt-in arbitration (via the Mod.Call API) works.
		foreach (var point in ArbitrationPoints.All) {
			var touching = ctx.ExceptSelf().Any(m => m.HookCounts.ContainsKey(point.SystemId));
			if (!touching)
				continue;
			if (!byId.TryGetValue(point.GroupId, out var group)) {
				group = new ArbitrationGroup {
					GroupId = point.GroupId,
					SystemId = point.SystemId,
					Strategy = config.DefaultStrategy
				};
				groups.Add(group);
				byId[group.GroupId] = group;
			}
			group.MechanismAvailable = true;
			foreach (var mod in ctx.ExceptSelf().Where(m => m.HookCounts.ContainsKey(point.SystemId))) {
				var facts = ctx.Get(mod.Name);
				group.EnsureCandidate(mod.Name, facts?.LoadIndex ?? int.MaxValue);
			}
		}

		// Drop candidates that are no longer loaded so they cannot silently win.
		foreach (var group in groups)
			group.Candidates.RemoveAll(c => !ModLoader.HasMod(c.ModName));

		// Arbitration must be opted in; safe mode forces detection-only.
		ArbitrationState.Enabled = config.ArbitrationActive;

		ArbitrationManager.ResolveAll(groups, config);
		ArbitrationState.ReplaceGroups(groups);
		if (config.PersistDecisions)
			ArbitrationStore.Save(groups);
	}

	private static ModpackSnapshot BuildSnapshot(DetectorContext ctx, List<Conflict> conflicts)
	{
		var snapshot = new ModpackSnapshot {
			SessionId = ScanState.SessionId,
			CreatedAt = DateTime.UtcNow,
			TModLoaderVersion = ModLoader.versionedName,
			TerrariaVersion = SafeVersion(),
			ModCount = ctx.Mods.Count,
			ConflictCount = conflicts.Count,
			HealthScore = ScanState.Health?.Score ?? -1
		};

		foreach (var mod in ctx.Mods) {
			snapshot.Mods.Add(new ModSnapshotEntry {
				Name = mod.Name,
				DisplayName = mod.DisplayNameSafe,
				Version = mod.Version?.ToString() ?? "?",
				LoadIndex = mod.LoadIndex,
				Dependencies = mod.Dependencies.ToList(),
				WeakDependencies = mod.WeakDependencies.ToList(),
				Author = mod.Author,
				HookCount = mod.TotalHooks,
				Systems = mod.HookCounts.Keys.ToList(),
				PatchSignals = mod.PatchSignals.ToList()
			});
		}

		foreach (var c in conflicts) {
			snapshot.Conflicts.Add(new ConflictSnapshotEntry {
				Id = c.Id,
				DetectorId = c.DetectorId,
				SystemId = c.SystemId,
				Severity = c.Severity.ToString(),
				Confidence = c.Confidence.ToString(),
				Mods = c.Mods.ToList()
			});
		}

		snapshot.RuntimeErrors = CaptureErrors();
		return snapshot;
	}

	private static string SafeVersion()
	{
		try { return Main.versionNumber; }
		catch { return "?"; }
	}

	public static void Reset()
	{
		OpenUIKeybind = null;
		_rescanQueued = false;
	}
}
