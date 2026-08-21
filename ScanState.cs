using System;
using ModHarmony.Common.Core;
using ModHarmony.Common.Detection;

namespace ModHarmony;

/// <summary>
/// Latest scan results shared between the scan pipeline, the UI and the report
/// generators. Rebuilt on every mod load; mutated during scans and by the
/// investigation monitor. All reads happen on the main thread.
/// </summary>
public static class ScanState
{
	/// <summary>Unique id for this game session (used for snapshot history).</summary>
	public static readonly string SessionId = Guid.NewGuid().ToString("N").Substring(0, 12);

	public static DetectorContext Context { get; set; }
	public static ConflictStore Store { get; } = new();
	public static HealthCalculator.Result Health { get; set; } = new();
	public static ModpackSnapshot Snapshot { get; set; }
	public static ChangeSet ChangeSet { get; set; }
	public static bool IsScanning { get; set; }
	public static DateTime LastScanTime { get; set; }
	public static int ScanRunCount { get; set; }

	public static bool HasScan => Context != null && Context.Mods.Count > 0;
}
