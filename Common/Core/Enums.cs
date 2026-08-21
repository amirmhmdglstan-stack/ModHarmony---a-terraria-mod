using System;

namespace ModHarmony.Common.Core;

/// <summary>
/// Severity of a detected interaction. These mirror the ModHarmony UI legend:
/// green/blue/yellow/orange/red/black. Severity expresses how much risk the
/// interaction carries if it turns out to be a real problem, NOT proof that a
/// problem exists. Always read severity together with <see cref="Confidence"/>.
/// </summary>
public enum Severity
{
	/// <summary>🟢 Informational — an interaction was detected but no risk is implied.</summary>
	Info = 0,

	/// <summary>🔵 Low risk — unlikely to matter, but flagged for transparency.</summary>
	Low = 1,

	/// <summary>🟡 Potential conflict — could interact in ways that matter.</summary>
	Medium = 2,

	/// <summary>🟠 Significant conflict — a real chance of observable interference.</summary>
	Significant = 3,

	/// <summary>🔴 High-risk conflict — the most likely candidates for hard-to-explain bugs.</summary>
	High = 4,

	/// <summary>⚫ Unknown — something was detected but its meaning cannot be assessed.</summary>
	Unknown = 5
}

/// <summary>
/// How sure we are that the described interaction actually exists.
/// This is about *detection certainty*, not about whether the interaction
/// causes a bug.
/// </summary>
public enum Confidence
{
	/// <summary>Unknown / cannot be assessed with the available information.</summary>
	Unknown = 0,

	/// <summary>Possible — the evidence is indirect and could be coincidental.</summary>
	Possible = 1,

	/// <summary>Strongly suspected — direct, unambiguous evidence of the interaction.</summary>
	Strong = 2,

	/// <summary>Confirmed — tModLoader exposed the information directly and we read it.</summary>
	Confirmed = 3
}

/// <summary>Lifecycle state of a conflict detector during the most recent scan.</summary>
public enum DetectorStatus
{
	Pending,
	Running,
	Completed,
	Failed,
	Disabled
}

/// <summary>How an arbitration group decides which candidate wins.</summary>
public enum ArbitrationStrategy
{
	/// <summary>Arbitration is disabled for this group; nothing is applied.</summary>
	Disabled,

	/// <summary>The player ordered the candidates manually; highest priority wins.</summary>
	ManualPriority,

	/// <summary>The mod loaded first (lowest load index) wins.</summary>
	LoadOrder,

	/// <summary>A controlled, seeded random choice. Stable until re-rolled.</summary>
	Random,

	/// <summary>Seeded random choice weighted per candidate. Stable until re-rolled.</summary>
	WeightedRandom,

	/// <summary>The first registered candidate wins.</summary>
	FirstRegistered,

	/// <summary>The last registered candidate wins.</summary>
	LastRegistered
}

public static class EnumExtensions
{
	public static string LocalizationSuffix(this Severity s) => s switch {
		Severity.Info => "Info",
		Severity.Low => "Low",
		Severity.Medium => "Medium",
		Severity.Significant => "Significant",
		Severity.High => "High",
		Severity.Unknown => "Unknown",
		_ => "Unknown"
	};

	public static string LocalizationSuffix(this Confidence c) => c switch {
		Confidence.Confirmed => "Confirmed",
		Confidence.Strong => "Strong",
		Confidence.Possible => "Possible",
		Confidence.Unknown => "Unknown",
		_ => "Unknown"
	};

	public static string LocalizationSuffix(this ArbitrationStrategy s) => s switch {
		ArbitrationStrategy.Disabled => "Disabled",
		ArbitrationStrategy.ManualPriority => "ManualPriority",
		ArbitrationStrategy.LoadOrder => "LoadOrder",
		ArbitrationStrategy.Random => "Random",
		ArbitrationStrategy.WeightedRandom => "WeightedRandom",
		ArbitrationStrategy.FirstRegistered => "FirstRegistered",
		ArbitrationStrategy.LastRegistered => "LastRegistered",
		_ => "Disabled"
	};

	public static float ConfidenceFactor(this Confidence c) => c switch {
		Confidence.Confirmed => 1f,
		Confidence.Strong => 0.8f,
		Confidence.Possible => 0.5f,
		_ => 0.25f
	};

	public static int SeverityWeight(this Severity s) => s switch {
		Severity.Info => 0,
		Severity.Low => 1,
		Severity.Medium => 3,
		Severity.Significant => 6,
		Severity.High => 12,
		Severity.Unknown => 2,
		_ => 0
	};
}
