using System;
using System.Collections.Generic;
using ModHarmony.Common.Arbitration;
using ModHarmony.Common.Utilities;
using Terraria.ModLoader;

namespace ModHarmony;

/// <summary>
/// ModHarmony — a multi-mod compatibility analyzer and conflict manager for
/// tModLoader. See README.md and docs/ for a full description of what this mod
/// detects, explains, and (optionally) arbitrates — and what it deliberately
/// does NOT do.
/// </summary>
public sealed class ModHarmony : Mod
{
	public const string ModName = "ModHarmony";

	public static ModHarmony Instance { get; private set; }

	public ModHarmony()
	{
		Instance = this;
	}

	public override void Load()
	{
		Log.Init(this);
		Log.Info($"ModHarmony {Version} loading (tModLoader {ModLoader.versionedName})");
	}

	public override void Unload()
	{
		Common.Diagnostics.RuntimeMonitor.SetActive(false);
		Common.Diagnostics.PerformanceTracker.SetActive(false);
		Log.Info("ModHarmony unloaded");
		Instance = null;
	}

	/// <summary>
	/// Opt-in API for other mods. Documented in docs/ARBITRATION.md.
	///  • ("RegisterArbitrableValue", systemId, modName, float value, [description])
	///  • ("GetArbitratedValue", systemId) → float
	///  • ("GetArbitrationWinner", systemId) → string
	///  • ("GetConflictCount") → int
	///  • ("GetModHealth") → int
	///  • ("ForceRescan") → null (queues a rescan)
	/// Unknown calls return an error string; handled calls return null or a value.
	/// </summary>
	public override object Call(params object[] args)
	{
		try {
			if (args == null || args.Length == 0)
				return "usage: Call(string operation, ...)";

			switch (args[0] as string) {
				case "RegisterArbitrableValue":
				case "GetArbitratedValue":
				case "GetArbitrationWinner":
					return ArbitrationRuntime.HandleCall(this, args);

				case "GetConflictCount":
					return ScanState.Store.Count;

				case "GetModHealth":
					return ScanState.Health?.Score ?? -1;

				case "ForceRescan":
					Systems.ModHarmonySystem.QueueRescan();
					return null;

				default:
					return $"unknown operation '{args[0]}'";
			}
		}
		catch (Exception e) {
			Log.Error($"Mod.Call failed: {e.Message}");
			return $"ModHarmony.Call failed: {e.Message}";
		}
	}
}
