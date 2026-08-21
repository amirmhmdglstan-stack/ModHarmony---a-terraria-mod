using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.ExceptionServices;
using ModHarmony.Common.Core;
using ModHarmony.Common.Utilities;

namespace ModHarmony.Common.Diagnostics;

/// <summary>
/// Investigation Mode's exception observer. While active it subscribes to
/// <see cref="AppDomain.FirstChanceException"/> and records exceptions whose
/// stack traces involve a loaded mod, into a bounded ring buffer. This is
/// deliberately gated (off by default), because first-chance observation has a
/// cost; the capture work is minimal string analysis and only runs while
/// Investigation Mode is on.
/// </summary>
public static class RuntimeMonitor
{
	public sealed class CapturedException
	{
		public DateTime Timestamp = DateTime.Now;
		public string Type = "";
		public string Message = "";
		public string[] StackFrames = Array.Empty<string>();
		public List<string> InvolvedMods = new();
		public int Occurrences = 1;
	}

	private static readonly List<CapturedException> Events = new();
	private static readonly Dictionary<string, int> DedupeIndex = new();
	private static bool _subscribed;
	private static readonly object Lock = new();

	public static bool Active { get; private set; }
	public static int MaxEvents { get; set; } = 200;
	public static bool CaptureVanilla { get; set; }

	public static void SetActive(bool active)
	{
		if (Active == active)
			return;
		Active = active;
		if (active) {
			if (!_subscribed) {
				AppDomain.CurrentDomain.FirstChanceException += OnFirstChanceException;
				AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
				_subscribed = true;
			}
			Log.Info("Investigation Mode: runtime monitoring enabled");
		}
		else {
			Log.Info("Investigation Mode: runtime monitoring disabled");
		}
	}

	public static void Clear()
	{
		lock (Lock) {
			Events.Clear();
			DedupeIndex.Clear();
		}
	}

	public static IReadOnlyList<CapturedException> GetEvents()
	{
		lock (Lock) return Events.ToArray();
	}

	public static int Count {
		get { lock (Lock) return Events.Count; }
	}

	private static void OnFirstChanceException(object sender, FirstChanceExceptionEventArgs e)
	{
		if (!Active)
			return;
		Capture(e.Exception);
	}

	private static void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
	{
		if (!Active)
			return;
		if (e.ExceptionObject is Exception ex)
			Capture(ex);
	}

	private static void Capture(Exception ex)
	{
		if (ex == null)
			return;

		try {
			var mods = ErrorCorrelator.InvolvedMods(ex);
			if (mods.Count == 0 && !CaptureVanilla)
				return;

			// Trim message to avoid unbounded growth.
			var message = (ex.Message ?? "").Replace('\r', ' ').Replace('\n', ' ').Trim();
			if (message.Length > 300)
				message = message.Substring(0, 300) + "…";

			var key = ex.GetType().Name + "|" + (ex.StackTrace ?? "").Split('\n').FirstOrDefault()?.Trim();

			lock (Lock) {
				if (DedupeIndex.TryGetValue(key, out var idx) && idx >= 0 && idx < Events.Count) {
					Events[idx].Occurrences++;
					Events[idx].Timestamp = DateTime.Now;
					return;
				}

				var entry = new CapturedException {
					Timestamp = DateTime.Now,
					Type = ex.GetType().FullName ?? ex.GetType().Name,
					Message = message,
					StackFrames = ErrorCorrelator.Frames(ex, 12),
					InvolvedMods = mods
				};

				// Ring buffer with dedupe bookkeeping.
				if (Events.Count >= MaxEvents) {
					DedupeIndex[Events[0].Type + "|" + (Events[0].StackFrames.FirstOrDefault() ?? "")] = -1;
					Events.RemoveAt(0);
					RebuildDedupe();
				}
				Events.Add(entry);
				DedupeIndex[key] = Events.Count - 1;
			}

			Log.Debug($"RuntimeMonitor captured {ex.GetType().Name}: {message}");
		}
		catch {
			// Never let observation break gameplay.
		}
	}

	private static void RebuildDedupe()
	{
		DedupeIndex.Clear();
		for (int i = 0; i < Events.Count; i++) {
			var e = Events[i];
			var key = e.Type + "|" + (e.StackFrames.FirstOrDefault() ?? "");
			DedupeIndex[key] = i;
		}
	}

	/// <summary>Mods that appear in recent captured exceptions, most frequent first.</summary>
	public static List<(string mod, int count)> MostInvolvedMods(int topN)
	{
		var counts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
		lock (Lock) {
			foreach (var e in Events) {
				foreach (var m in e.InvolvedMods) {
					counts.TryGetValue(m, out var n);
					counts[m] = n + 1;
				}
			}
		}
		return counts.OrderByDescending(kv => kv.Value).Take(topN)
			.Select(kv => (kv.Key, kv.Value)).ToList();
	}

	public static void Reset()
	{
		Active = false;
		lock (Lock) {
			Events.Clear();
			DedupeIndex.Clear();
		}
	}
}
