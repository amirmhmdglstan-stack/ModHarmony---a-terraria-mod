using System.Collections.Generic;
using System.Linq;
using ModHarmony.Common.Core;

namespace ModHarmony.Common.Arbitration;

/// <summary>
/// Mutable arbitration state shared between the scan pipeline, the UI and the
/// runtime application hooks. Not persisted directly — persistence is handled by
/// <see cref="ArbitrationStore"/>.
/// </summary>
public static class ArbitrationState
{
	public static List<ArbitrationGroup> Groups { get; private set; } = new();
	private static Dictionary<string, ArbitrationGroup> _byId = new();

	/// <summary>Master switch: true only when the player opted into arbitration and safe mode is off.</summary>
	public static bool Enabled;

	/// <summary>groupId + '\u0001' + modName → registered value (opt-in API).</summary>
	public static readonly Dictionary<string, float> RegisteredValues = new();

	public static void ReplaceGroups(IEnumerable<ArbitrationGroup> groups)
	{
		Groups = groups.ToList();
		RebuildIndex();
	}

	public static void RebuildIndex()
	{
		_byId = Groups.ToDictionary(g => g.GroupId, g => g, System.StringComparer.OrdinalIgnoreCase);
	}

	public static ArbitrationGroup Get(string groupId) => _byId.TryGetValue(groupId, out var g) ? g : null;

	/// <summary>Creates a group for an arbitrable system if it does not exist yet (used by the opt-in API before the first scan).</summary>
	public static ArbitrationGroup EnsureGroup(string systemId, ArbitrationStrategy strategy)
	{
		var groupId = $"system.{systemId}";
		var existing = Get(groupId);
		if (existing != null)
			return existing;

		var created = new ArbitrationGroup {
			GroupId = groupId,
			SystemId = systemId,
			Strategy = strategy,
			MechanismAvailable = ArbitrationPoints.HasPoint(systemId)
		};
		Groups.Add(created);
		RebuildIndex();
		return created;
	}

	public static bool HasGroup(string systemId) => Get($"system.{systemId}") != null;

	/// <summary>The winner's registered value for an arbitration point, or 1 (no-op) when none.</summary>
	public static float WinnerFactor(string systemId)
	{
		if (!Enabled)
			return 1f;
		var group = Get($"system.{systemId}");
		if (group == null || !group.CanResolve || string.IsNullOrEmpty(group.ResolvedWinner))
			return 1f;
		return RegisteredValue(systemId, group.ResolvedWinner);
	}

	public static float RegisteredValue(string systemId, string modName)
	{
		return RegisteredValues.TryGetValue(systemId + "\u0001" + modName, out var v) ? v : 1f;
	}

	public static void RegisterValue(string systemId, string modName, float value)
	{
		RegisteredValues[systemId + "\u0001" + modName] = value;
	}

	public static bool IsSystemArbitrable(string systemId) => ArbitrationPoints.Find(systemId) != null;
}
