using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using ModHarmony.Common.Core;
using ModHarmony.Common.Utilities;
using ModHarmony.Content.Config;

namespace ModHarmony.Common.Detection;

/// <summary>
/// Registry + runner for conflict detectors. Detectors are pluggable: add a new
/// implementation of <see cref="IConflictDetector"/> and register it here.
/// Every detector runs in its own try/catch — a failure logs, marks the
/// detector as failed, and lets the scan continue.
/// </summary>
public sealed class DetectorManager
{
	private readonly List<IConflictDetector> _detectors = new();

	public DetectorManager()
	{
		// Order matters for reporting (most fundamental signals first).
		Register(new HookOverlapDetector());
		Register(new GlobalClassOverlapDetector());
		Register(new ModPlayerOverlapDetector());
		Register(new ModSystemOverlapDetector());
		Register(new RecipeDetector());
		Register(new DependencyDetector());
		Register(new ILHookDetector());
		Register(new AssetDetector());
	}

	public void Register(IConflictDetector detector)
	{
		if (_detectors.Any(d => d.Id == detector.Id))
			throw new InvalidOperationException($"Duplicate detector id '{detector.Id}'");
		_detectors.Add(detector);
	}

	public IReadOnlyList<IConflictDetector> Detectors => _detectors;

	/// <summary>Runs all enabled detectors against the context. Never throws.</summary>
	public List<Conflict> RunAll(DetectorContext context, ConflictStore store, ModHarmonyConfig config)
	{
		var conflicts = new List<Conflict>();
		var sw = System.Diagnostics.Stopwatch.StartNew();

		foreach (var detector in _detectors) {
			store.SetDetectorStatus(detector.Id, DetectorStatus.Running);
			if (!detector.IsEnabled(config)) {
				store.SetDetectorStatus(detector.Id, DetectorStatus.Disabled);
				continue;
			}
			try {
				var found = detector.Detect(context) ?? new List<Conflict>();
				foreach (var c in found)
					c.SortWeight = (int)c.Severity * 100 + (int)c.Confidence;
				conflicts.AddRange(found);
				store.SetDetectorStatus(detector.Id, DetectorStatus.Completed);
				Log.Debug($"Detector {detector.Id} found {found.Count} conflicts");
			}
			catch (Exception e) {
				store.SetDetectorStatus(detector.Id, DetectorStatus.Failed, e.Message);
				Log.Error($"Detector {detector.Id} failed: {e.Message}");
				Log.Debug(e.ToString());
			}
		}

		sw.Stop();
		Log.Info($"Scan completed in {sw.ElapsedMilliseconds} ms — {conflicts.Count} conflicts, {_detectors.Count} detectors");
		return conflicts;
	}
}
