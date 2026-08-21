using System;
using System.Collections.Generic;
using System.Linq;
using ModHarmony.Common.Core;

namespace ModHarmony.Common.Arbitration;

/// <summary>One mod participating in an arbitration group.</summary>
public sealed class ArbitrationCandidate
{
	public string ModName { get; set; } = "";

	/// <summary>Weight used by WeightedRandom (0..100 recommended; validated at resolve time).</summary>
	public float Weight { get; set; } = 1f;

	/// <summary>Higher wins under ManualPriority.</summary>
	public int ManualPriority { get; set; }

	/// <summary>Load index captured when the group was created (for LoadOrder strategy).</summary>
	public int LoadIndex { get; set; }

	/// <summary>Optional registered value/description from a cooperating mod (dev detail).</summary>
	public string RegisteredValue { get; set; } = "";
}

/// <summary>
/// An arbitration group: a contested system plus its candidates and the
/// strategy that decides the winner. Groups are persisted by stable id so
/// decisions survive sessions. A group with no resolvable mechanism still
/// exists (for "Detection available — automatic resolution unavailable"), but
/// <see cref="CanResolve"/> is false then.
/// </summary>
public sealed class ArbitrationGroup
{
	/// <summary>Stable group id, e.g. "system.npc.spawn".</summary>
	public string GroupId { get; set; } = "";

	/// <summary>Game system this group governs.</summary>
	public string SystemId { get; set; } = "";

	/// <summary>Persisted as a string for forward compatibility.</summary>
	public string StrategyName { get; set; } = ArbitrationStrategy.Disabled.ToString();

	/// <summary>-1 means "auto": seed is derived deterministically from the master config seed.</summary>
	public int Seed { get; set; } = -1;

	/// <summary>When locked, the current winner is kept until the player changes it.</summary>
	public bool Locked { get; set; }

	public List<ArbitrationCandidate> Candidates { get; set; } = new();

	// --- Runtime state (not persisted) ---
	[NonSerialized]
	public string ResolvedWinner = "";

	[NonSerialized]
	public string DecisionLog = "";

	[NonSerialized]
	public bool MechanismAvailable;

	public ArbitrationStrategy Strategy {
		get => Enum.TryParse<ArbitrationStrategy>(StrategyName, true, out var s) ? s : ArbitrationStrategy.Disabled;
		set => StrategyName = value.ToString();
	}

	public bool CanResolve => MechanismAvailable && Strategy != ArbitrationStrategy.Disabled && Candidates.Count >= 1;

	public ArbitrationCandidate GetCandidate(string modName) =>
		Candidates.FirstOrDefault(c => string.Equals(c.ModName, modName, StringComparison.OrdinalIgnoreCase));

	public void EnsureCandidate(string modName, int loadIndex)
	{
		if (GetCandidate(modName) != null)
			return;
		Candidates.Add(new ArbitrationCandidate { ModName = modName, LoadIndex = loadIndex, ManualPriority = Candidates.Count });
	}

	public void RemoveCandidate(string modName) => Candidates.RemoveAll(c => string.Equals(c.ModName, modName, StringComparison.OrdinalIgnoreCase));

	public ArbitrationGroup Clone() => new() {
		GroupId = GroupId,
		SystemId = SystemId,
		StrategyName = StrategyName,
		Seed = Seed,
		Locked = Locked,
		Candidates = Candidates.Select(c => new ArbitrationCandidate {
			ModName = c.ModName,
			Weight = c.Weight,
			ManualPriority = c.ManualPriority,
			LoadIndex = c.LoadIndex,
			RegisteredValue = c.RegisteredValue
		}).ToList(),
		ResolvedWinner = ResolvedWinner,
		DecisionLog = DecisionLog,
		MechanismAvailable = MechanismAvailable
	};
}
