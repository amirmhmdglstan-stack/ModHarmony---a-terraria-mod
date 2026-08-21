using System;
using System.Collections.Generic;
using System.Linq;

namespace ModHarmony.Common.Core;

/// <summary>
/// In-memory database of all detected conflicts plus detector health.
/// Scans replace the content wholesale (preserving nothing per-conflict —
/// player arbitration decisions are stored separately by id), runtime events
/// append. Reads from the UI are done through snapshot getters so concurrent
/// mutation can never tear the collection.
/// </summary>
public sealed class ConflictStore
{
	private readonly object _lock = new();
	private readonly List<Conflict> _all = new();
	private readonly Dictionary<string, Conflict> _byId = new();
	private readonly Dictionary<string, List<Conflict>> _byMod = new();
	private readonly Dictionary<string, List<Conflict>> _bySystem = new();
	private readonly Dictionary<string, DetectorStatus> _detectorStatus = new();
	private readonly List<string> _detectorFailures = new();

	public void ReplaceAll(IEnumerable<Conflict> conflicts)
	{
		lock (_lock) {
			_all.Clear();
			_byId.Clear();
			_byMod.Clear();
			_bySystem.Clear();
			foreach (var c in conflicts) {
				_all.Add(c);
				_byId[c.Id] = c;
				foreach (var mod in c.Mods) {
					if (!_byMod.TryGetValue(mod, out var list)) {
						list = new List<Conflict>();
						_byMod[mod] = list;
					}
					list.Add(c);
				}
				if (!_bySystem.TryGetValue(c.SystemId, out var sys)) {
					sys = new List<Conflict>();
					_bySystem[c.SystemId] = sys;
				}
				sys.Add(c);
			}
			_all.Sort((a, b) => b.SortWeight.CompareTo(a.SortWeight));
		}
	}

	public IReadOnlyList<Conflict> GetAll()
	{
		lock (_lock) return _all.ToArray();
	}

	public IReadOnlyList<Conflict> GetForMod(string modName)
	{
		lock (_lock) return _byMod.TryGetValue(modName, out var list) ? list.ToArray() : Array.Empty<Conflict>();
	}

	public IReadOnlyList<Conflict> GetForSystem(string systemId)
	{
		lock (_lock) return _bySystem.TryGetValue(systemId, out var list) ? list.ToArray() : Array.Empty<Conflict>();
	}

	public Conflict GetById(string id)
	{
		lock (_lock) return _byId.TryGetValue(id, out var c) ? c : null;
	}

	public int Count => _all.Count;

	public int CountWithSeverity(Severity severity)
	{
		lock (_lock) return _all.Count(c => c.Severity == severity);
	}

	public int CountHighRisk => CountWithSeverity(Severity.High) + CountWithSeverity(Severity.Significant);

	public void SetDetectorStatus(string detectorId, DetectorStatus status, string failure = null)
	{
		lock (_lock) {
			_detectorStatus[detectorId] = status;
			if (!string.IsNullOrEmpty(failure))
				_detectorFailures.Add($"{detectorId}: {failure}");
		}
	}

	public DetectorStatus GetDetectorStatus(string detectorId)
	{
		lock (_lock) return _detectorStatus.TryGetValue(detectorId, out var s) ? s : DetectorStatus.Pending;
	}

	public IReadOnlyDictionary<string, DetectorStatus> GetDetectorStatuses()
	{
		lock (_lock) return new Dictionary<string, DetectorStatus>(_detectorStatus);
	}

	public IReadOnlyList<string> GetDetectorFailures()
	{
		lock (_lock) return _detectorFailures.ToArray();
	}
}
