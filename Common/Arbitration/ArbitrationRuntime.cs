using System;
using ModHarmony.Common.Core;
using ModHarmony.Common.Utilities;

namespace ModHarmony.Common.Arbitration;

/// <summary>
/// Handles the opt-in Mod.Call API that other mods use to participate in
/// arbitration. Everything here is additive and safe: values are only stored,
/// and they only take effect at the built-in points when a group resolves.
/// </summary>
public static class ArbitrationRuntime
{
	/// <summary>
	/// Returns null when the call was handled; otherwise an error string.
	/// </summary>
	public static string HandleCall(Mod caller, params object[] args)
	{
		if (args == null || args.Length == 0)
			return "empty call";

		var op = args[0] as string;
		switch (op) {
			case "RegisterArbitrableValue": {
				if (args.Length < 4 || args[1] is not string systemId || args[2] is not string modName || args[3] is not float value)
					return "usage: RegisterArbitrableValue(string systemId, string modName, float value, [string description])";

				if (!ArbitrationState.IsSystemArbitrable(systemId))
					return $"system '{systemId}' has no arbitration point";

				if (value <= 0f)
					return "value must be > 0 (1 = no change)";

				ArbitrationState.RegisterValue(systemId, modName, value);
				var group = ArbitrationState.Get($"system.{systemId}");
				if (group != null) {
					group.EnsureCandidate(modName, LoadIndexOf(modName));
					group.MechanismAvailable = true;
					if (args.Length >= 5 && args[4] is string desc)
						group.GetCandidate(modName).RegisteredValue = $"{value:0.###} ({desc})";
				}
				Log.Info($"Mod {modName} registered arbitrable value for {systemId}: {value}");
				return null;
			}

			case "GetArbitratedValue": {
				if (args.Length < 2 || args[1] is not string systemId)
					return "usage: GetArbitratedValue(string systemId)";
				// Return the value through out-style object box: Mod.Call returns object.
				return ArbitrationState.WinnerFactor(systemId);
			}

			case "GetArbitrationWinner": {
				if (args.Length < 2 || args[1] is not string systemId)
					return "usage: GetArbitrationWinner(string systemId)";
				var group = ArbitrationState.Get($"system.{systemId}");
				return group?.ResolvedWinner ?? "";
			}

			default:
				return $"unknown operation '{op}'";
		}
	}

	private static int LoadIndexOf(string modName)
	{
		if (Terraria.ModLoader.ModLoader.TryGetMod(modName, out var mod)) {
			var mods = Terraria.ModLoader.ModLoader.Mods;
			for (int i = 0; i < mods.Length; i++) {
				if (string.Equals(mods[i].Name, modName, StringComparison.OrdinalIgnoreCase))
					return i;
			}
		}
		return int.MaxValue;
	}
}
