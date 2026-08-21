using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace ModHarmony.Common.Core;

/// <summary>
/// A detected interaction between two or more mods (or a mod and the game state).
/// Conflicts are always *reports*, never verdicts: severity + confidence tell the
/// player how seriously to take them, and the evidence explains why they exist.
/// </summary>
public sealed class Conflict
{
	public string Id { get; set; } = "";
	public string DetectorId { get; set; } = "";
	public string SystemId { get; set; } = "content.modification";
	public Severity Severity { get; set; } = Severity.Unknown;
	public Confidence Confidence { get; set; } = Confidence.Unknown;

	/// <summary>Involved mods, in stable (internal-name) form. Two or more for pair conflicts.</summary>
	public List<string> Mods { get; set; } = new();

	/// <summary>Human-readable evidence items.</summary>
	public List<Evidence> Evidence { get; set; } = new();

	/// <summary>
	/// Null/empty: "detection only". "resolvable": a ModHarmony arbitration point
	/// exists. Otherwise the arbitration group id that governs this conflict.
	/// </summary>
	public string ArbitrationGroupId { get; set; } = "";

	/// <summary>Set when the conflict only appears under certain conditions (e.g. optional dependency present).</summary>
	public bool IsConditional { get; set; }

	public DateTime DetectedAt { get; set; } = DateTime.UtcNow;

	[NonSerialized]
	public int SortWeight;

	// ----------------------------------------------------------------

	/// <summary>
	/// Deterministic stable id derived from detector, system and involved mods.
	/// The same situation always produces the same id, so per-conflict player
	/// configuration can follow conflicts across sessions and mod versions.
	/// </summary>
	public static string MakeStableId(string detectorId, string systemId, IEnumerable<string> mods)
	{
		var material = detectorId + "|" + systemId + "|" + string.Join(",", mods.OrderBy(m => m, StringComparer.OrdinalIgnoreCase));
		var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(material));
		// Use the first 10 bytes -> 13 base32 chars, human-typable.
		var sb = new StringBuilder(13);
		foreach (var b in bytes.Take(10)) {
			sb.Append(ToBase32(b >> 3 & 31));
			sb.Append(ToBase32(b & 31));
		}
		return sb.ToString().Substring(0, 13).ToUpperInvariant();
	}

	private static char ToBase32(int v) => v < 26 ? (char)('A' + v) : (char)('2' + v - 26);

	public override string ToString() => $"[{Severity}/{Confidence}] {string.Join(" <-> ", Mods)} ({SystemId})";

	public Conflict Clone() => new() {
		Id = Id,
		DetectorId = DetectorId,
		SystemId = SystemId,
		Severity = Severity,
		Confidence = Confidence,
		Mods = new List<string>(Mods),
		Evidence = Evidence.Select(e => new Evidence {
			Kind = e.Kind,
			ModName = e.ModName,
			Key = e.Key,
			Args = e.Args?.ToArray() ?? Array.Empty<string>(),
			DevDetail = e.DevDetail
		}).ToList(),
		ArbitrationGroupId = ArbitrationGroupId,
		IsConditional = IsConditional,
		DetectedAt = DetectedAt
	};
}
