using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Terraria;

namespace ModHarmony.Common.Diagnostics;

/// <summary>
/// Lightweight frame-time sampling used by Investigation Mode. Samples the main
/// loop delta once per frame, keeps a bounded spike log, and reports an
/// approximate average. It measures ModHarmony's view of frame pacing only — it
/// cannot attribute a slowdown to any specific mod, and the report says so.
/// </summary>
public static class PerformanceTracker
{
	public sealed class FrameSpike
	{
		public uint GameUpdateCount;
		public double FrameMs;
		public string WorldTime;
	}

	private static readonly List<FrameSpike> Spikes = new();
	private static long _lastTicks;
	private static double _emaFrameMs;
	private static bool _initialized;
	private static long _samples;

	public static bool Active { get; private set; }
	public static int MaxSpikes { get; set; } = 50;

	/// <summary>Frame times above this (ms) are recorded as spikes.</summary>
	public static double SpikeThresholdMs { get; set; } = 33.3;

	public static void SetActive(bool active)
	{
		if (Active == active)
			return;
		Active = active;
		_lastTicks = 0;
		_initialized = false;
		_samples = 0;
		if (!active)
			Spikes.Clear();
	}

	public static void Tick()
	{
		if (!Active)
			return;

		var now = Stopwatch.GetTimestamp();
		if (_initialized) {
			double ms = (now - _lastTicks) * 1000.0 / Stopwatch.Frequency;
			_samples++;
			_emaFrameMs = _emaFrameMs <= 0 ? ms : _emaFrameMs * 0.95 + ms * 0.05;

			if (ms > SpikeThresholdMs) {
				var spike = new FrameSpike {
					GameUpdateCount = Main.GameUpdateCount,
					FrameMs = ms,
					WorldTime = ""
				};
				try { spike.WorldTime = (Main.ActiveWorldFileData?.Name ?? "?") + " @ " + (Main.dayTime ? "day" : "night"); }
				catch { /* best effort */ }

				if (Spikes.Count >= MaxSpikes)
					Spikes.RemoveAt(0);
				Spikes.Add(spike);
			}
		}
		_lastTicks = now;
		_initialized = true;
	}

	public static double AverageFrameMs => _samples > 0 ? _emaFrameMs : 0;

	public static int SpikeCount => Spikes.Count;

	public static IReadOnlyList<FrameSpike> GetSpikes() => Spikes.ToArray();

	public static string Summary()
	{
		if (!Active || _samples == 0)
			return "not active";
		var avg = AverageFrameMs;
		var worst = Spikes.Count > 0 ? Spikes.Max(s => s.FrameMs) : 0;
		return $"~{avg:0.0} ms/frame avg, {SpikeCount} spike(s) > {SpikeThresholdMs:0} ms (worst {worst:0.0} ms)";
	}
}
