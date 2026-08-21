using System;
using System.Collections.Generic;
using System.Linq;

namespace ModHarmony.Common.Core;

/// <summary>
/// Computes the "Modpack Health" heuristic score. The score is explicitly NOT an
/// objective measurement — it is a transparent, repeatable heuristic based only on
/// detected issues. Every point removed is listed in <see cref="Breakdown"/> so the
/// player can see exactly how the number was calculated.
/// </summary>
public static class HealthCalculator
{
	public sealed class BreakdownItem
	{
		public string ReasonKey { get; set; } = "";   // localization key (Mods.ModHarmony.Health.{reason})
		public string[] Args { get; set; } = Array.Empty<string>();
		public int Points { get; set; }
	}

	public sealed class Result
	{
		public int Score { get; set; }
		public List<BreakdownItem> Breakdown { get; set; } = new();
	}

	public static Result Calculate(IReadOnlyList<Conflict> conflicts,
		IReadOnlyDictionary<string, int> systemOverlapCounts,
		IReadOnlyList<string> loadOrderWarnings,
		IReadOnlyDictionary<string, DetectorStatus> detectorStatuses)
	{
		var breakdown = new List<BreakdownItem>();
		var score = 100f;

		foreach (var group in conflicts.GroupBy(c => (c.Severity, c.Confidence))) {
			var severity = group.Key.Severity;
			var confidence = group.Key.Confidence;
			int count = group.Count();
			if (count == 0 || severity == Severity.Info)
				continue;

			float per = severity.SeverityWeight() * confidence.ConfidenceFactor();
			float total = per * count;
			score -= total;
			breakdown.Add(new BreakdownItem {
				ReasonKey = "Health.DeductionConflicts",
				Args = new[] { count.ToString(), severity.LocalizationSuffix(), confidence.LocalizationSuffix() },
				Points = (int)Math.Round(total)
			});
		}

		// Heavy system overlap: 5+ mods touching the same system.
		foreach (var kv in systemOverlapCounts.Where(kv => kv.Value >= 5)) {
			score -= 2;
			breakdown.Add(new BreakdownItem {
				ReasonKey = "Health.DeductionOverlap",
				Args = new[] { kv.Value.ToString(), kv.Key },
				Points = 2
			});
		}

		// Load order warnings (dependency cycles etc.).
		foreach (var w in loadOrderWarnings) {
			score -= 2;
			breakdown.Add(new BreakdownItem {
				ReasonKey = "Health.DeductionLoadOrder",
				Args = new[] { w },
				Points = 2
			});
		}

		// Detector failures mean the scan was incomplete; be transparent about it.
		int failed = detectorStatuses.Values.Count(s => s == DetectorStatus.Failed);
		if (failed > 0) {
			score -= 3;
			breakdown.Add(new BreakdownItem {
				ReasonKey = "Health.DeductionDetectorFailure",
				Args = new[] { failed.ToString() },
				Points = 3
			});
		}

		score = Math.Clamp(score, 0f, 100f);

		// Only include deduction entries that actually happened; keep the list compact.
		// (Entries are only added when points were deducted, so nothing more to filter.)

		return new Result {
			Score = (int)Math.Round(score),
			Breakdown = breakdown
		};
	}
}
