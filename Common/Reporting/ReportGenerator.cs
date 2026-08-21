using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using ModHarmony.Common.Arbitration;
using ModHarmony.Common.Core;
using ModHarmony.Common.Diagnostics;
using ModHarmony.Common.Detection;
using ModHarmony.Common.Utilities;

namespace ModHarmony.Common.Reporting;

/// <summary>
/// Generates the human-readable reports:
///  • Full compatibility report (exported to a text file);
///  • "Analyze Current Situation" investigation report;
///  • Compact community summary (for Discord/GitHub issues).
/// All text goes through localization where a key exists; the bulk is assembled
/// from structured data so it stays accurate.
/// </summary>
public static class ReportGenerator
{
	// ---------------------------------------------------------------- full report

	public static string BuildFullReport(DetectorContext ctx, ConflictStore store, HealthCalculator.Result health, ModpackSnapshot snapshot, ArbitrationStateData arbitration)
	{
		var sb = new StringBuilder(16 * 1024);

		sb.AppendLine(L10n.Text("Report.Title"));
		sb.AppendLine(new string('=', 60));
		sb.AppendLine(L10n.Text("Report.Generated", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")));
		sb.AppendLine(L10n.Text("Report.GameVersions", SafeMainVersion(), Terraria.ModLoader.ModLoader.versionedName));
		sb.AppendLine(L10n.Text("Report.Session", snapshot?.SessionId ?? "?"));
		sb.AppendLine();

		AppendModsSection(sb, ctx);
		AppendConflictsSection(sb, store);
		AppendSystemsSection(sb, ctx);
		AppendArbitrationSection(sb, arbitration.Groups);
		AppendRuntimeSection(sb, ctx, store);
		AppendHealthSection(sb, health);
		AppendRecommendations(sb, ctx, store, health);

		return sb.ToString();
	}

	private static void AppendModsSection(StringBuilder sb, DetectorContext ctx)
	{
		sb.AppendLine(L10n.Text("Report.SectionMods", ctx.Mods.Count.ToString()));
		sb.AppendLine(new string('-', 60));
		foreach (var mod in ctx.Mods.OrderBy(m => m.LoadIndex)) {
			var line = $"#{mod.LoadIndex,-3} {mod.Name} (\"{mod.DisplayNameSafe}\") v{mod.Version}";
			if (!string.IsNullOrEmpty(mod.Author))
				line += $" — {mod.Author}";
			sb.AppendLine(line);
			if (mod.Dependencies.Count > 0)
				sb.AppendLine("    " + L10n.Text("Report.Dependencies", string.Join(", ", mod.Dependencies)));
			if (mod.WeakDependencies.Count > 0)
				sb.AppendLine("    " + L10n.Text("Report.WeakDependencies", string.Join(", ", mod.WeakDependencies)));
			if (mod.MissingOptionalDependencies.Count > 0)
				sb.AppendLine("    " + L10n.Text("Report.MissingOptional", string.Join(", ", mod.MissingOptionalDependencies)));
			if (mod.TotalHooks > 0)
				sb.AppendLine("    " + L10n.Text("Report.Hooks", mod.TotalHooks.ToString(),
					string.Join(", ", mod.HookCounts.OrderByDescending(kv => kv.Value).Take(8).Select(kv => $"{kv.Key}({kv.Value})"))));
			if (mod.ContentCounts.Count > 0)
				sb.AppendLine("    " + L10n.Text("Report.Content", string.Join(", ",
					mod.ContentCounts.OrderByDescending(kv => kv.Value).Take(8).Select(kv => $"{kv.Key}: {kv.Value}"))));
			if (mod.PatchSignals.Count > 0)
				sb.AppendLine("    " + L10n.Text("Report.PatchSignals", string.Join(", ", mod.PatchSignals)));
			if (mod.CodeUnavailable)
				sb.AppendLine("    " + L10n.Text("Report.CodeUnavailable"));
			sb.AppendLine();
		}
	}

	private static void AppendConflictsSection(StringBuilder sb, ConflictStore store)
	{
		var conflicts = store.GetAll();
		sb.AppendLine(L10n.Text("Report.SectionConflicts", conflicts.Count.ToString()));
		sb.AppendLine(new string('-', 60));

		if (conflicts.Count == 0) {
			sb.AppendLine(L10n.Text("Report.NoConflicts"));
			sb.AppendLine();
			return;
		}

		foreach (var c in conflicts.Take(500)) {
			sb.AppendLine($"[{L10n.Text("Severity." + c.Severity.LocalizationSuffix() + ".Name").ToUpperInvariant()} / {L10n.Text("Confidence." + c.Confidence.LocalizationSuffix() + ".Name")}] {string.Join(" ↔ ", c.Mods.Select(DisplayName))}");
			sb.AppendLine("    " + L10n.Text("Report.ConflictSystem", SafeSystemName(c.SystemId), c.DetectorId, c.Id));
			foreach (var e in c.Evidence)
				sb.AppendLine("    • " + EvidenceText(e));
			if (c.ArbitrationGroupId.Length > 0) {
				var group = ArbitrationState.Get(c.ArbitrationGroupId);
				sb.AppendLine("    " + (group?.CanResolve == true
					? L10n.Text("Report.ArbitrationResolvable", group.Strategy.ToString(), group.ResolvedWinner)
					: L10n.Text("Report.ArbitrationUnavailable")));
			}
			sb.AppendLine();
		}
		if (conflicts.Count > 500)
			sb.AppendLine(L10n.Text("Report.ConflictsTruncated", (conflicts.Count - 500).ToString()));
	}

	private static void AppendSystemsSection(StringBuilder sb, DetectorContext ctx)
	{
		sb.AppendLine(L10n.Text("Report.SectionSystems"));
		sb.AppendLine(new string('-', 60));
		foreach (var kv in ctx.SystemOverlapCounts.OrderByDescending(kv => kv.Value).Take(15))
			sb.AppendLine($"    {SafeSystemName(kv.Key)}: {kv.Value} {L10n.Text("Report.ModsTouching")}");
		sb.AppendLine();
	}

	private static void AppendArbitrationSection(StringBuilder sb, List<ArbitrationGroup> groups)
	{
		sb.AppendLine(L10n.Text("Report.SectionArbitration", groups.Count.ToString()));
		sb.AppendLine(new string('-', 60));
		foreach (var g in groups) {
			var status = g.CanResolve ? L10n.Text("Report.ArbitrationResolved", g.ResolvedWinner) : g.DecisionLog;
			sb.AppendLine($"    {g.GroupId} [{g.Strategy}] → {status}");
		}
		sb.AppendLine();
	}

	private static void AppendRuntimeSection(StringBuilder sb, DetectorContext ctx, ConflictStore store)
	{
		sb.AppendLine(L10n.Text("Report.SectionRuntime"));
		sb.AppendLine(new string('-', 60));
		var events = RuntimeMonitor.GetEvents();
		if (events.Count == 0) {
			sb.AppendLine(L10n.Text("Report.NoRuntimeEvents"));
		}
		else {
			foreach (var e in events.Take(50)) {
				var mods = e.InvolvedMods.Count > 0 ? string.Join(", ", e.InvolvedMods) : L10n.Text("Report.NoModAttribution");
				sb.AppendLine($"    {e.Timestamp:HH:mm:ss} {e.Type} (x{e.Occurrences}) — {e.Message}");
				sb.AppendLine($"      {L10n.Text("Report.InvolvedMods", mods)}");
				foreach (var frame in e.StackFrames.Take(4))
					sb.AppendLine($"      {frame}");
			}
			if (events.Count > 50)
				sb.AppendLine(L10n.Text("Report.EventsTruncated", (events.Count - 50).ToString()));
		}

		sb.AppendLine("    " + L10n.Text("Report.Performance", PerformanceTracker.Summary()));
		sb.AppendLine();

		var detectorStatuses = store.GetDetectorStatuses();
		var failures = store.GetDetectorFailures();
		if (failures.Count > 0) {
			sb.AppendLine(L10n.Text("Report.DetectorFailures", failures.Count.ToString()));
			foreach (var f in failures.Take(10))
				sb.AppendLine("    " + f);
			sb.AppendLine();
		}
	}

	private static void AppendHealthSection(StringBuilder sb, HealthCalculator.Result health)
	{
		sb.AppendLine(L10n.Text("Report.SectionHealth", health.Score.ToString()));
		sb.AppendLine(new string('-', 60));
		sb.AppendLine("    " + L10n.Text("Health.Disclaimer"));
		foreach (var item in health.Breakdown)
			sb.AppendLine($"    −{item.Points}: {L10n.Text(item.ReasonKey, item.Args)}");
		sb.AppendLine();
	}

	private static void AppendRecommendations(StringBuilder sb, DetectorContext ctx, ConflictStore store, HealthCalculator.Result health)
	{
		sb.AppendLine(L10n.Text("Report.SectionRecommendations"));
		sb.AppendLine(new string('-', 60));

		var high = store.GetAll().Where(c => c.Severity == Severity.High || c.Severity == Severity.Significant).ToList();
		if (high.Count == 0) {
			sb.AppendLine("    " + L10n.Text("Report.NoHighRisk"));
		}
		else {
			sb.AppendLine("    " + L10n.Text("Report.HighRiskRecommendation", high.Count.ToString()));
			foreach (var c in high.Take(10))
				sb.AppendLine($"      • {string.Join(" ↔ ", c.Mods.Select(DisplayName))} ({SafeSystemName(c.SystemId)})");
		}

		var patchers = ctx.ExceptSelf().Where(m => m.PatchSignals.Count > 0).ToList();
		if (patchers.Count >= 2)
			sb.AppendLine("    " + L10n.Text("Report.ILRecommendation", string.Join(", ", patchers.Select(m => m.Name))));

		if (health.Score >= 90)
			sb.AppendLine("    " + L10n.Text("Report.GoodHealth"));
		else
			sb.AppendLine("    " + L10n.Text("Report.BadHealth"));

		sb.AppendLine();
		sb.AppendLine(L10n.Text("Report.Footer"));
	}

	// ---------------------------------------------------------------- investigation

	public static string BuildInvestigationReport(DetectorContext ctx, ConflictStore store, HealthCalculator.Result health)
	{
		var sb = new StringBuilder(6 * 1024);
		sb.AppendLine(L10n.Text("Report.InvestigationTitle"));
		sb.AppendLine(new string('=', 60));
		sb.AppendLine(L10n.Text("Report.Generated", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")));

		try {
			sb.AppendLine(L10n.Text("Report.InvestigationContext",
				MainSavePathProvider.Get(),
				Terraria.Main.worldName,
				Terraria.Main.netMode == 2 ? L10n.Text("Report.ModeServer") : L10n.Text("Report.ModeClient")));
		}
		catch {
			// No world loaded; context line is skipped.
		}
		sb.AppendLine();

		sb.AppendLine(L10n.Text("Report.InvestigationMods", ctx.Mods.Count.ToString()));
		sb.AppendLine(new string('-', 60));

		// Most relevant mods: involved in runtime errors first, then high-risk conflicts.
		var interesting = new List<string>();
		foreach (var (mod, count) in RuntimeMonitor.MostInvolvedMods(8))
			interesting.Add($"{mod} ({L10n.Text("Report.ErrorCount", count.ToString())})");
		if (interesting.Count > 0)
			sb.AppendLine("    " + L10n.Text("Report.InvestigationTopMods", string.Join(", ", interesting)));

		var involved = RuntimeMonitor.MostInvolvedMods(5).Select(m => m.mod).ToHashSet(StringComparer.OrdinalIgnoreCase);
		foreach (var c in store.GetAll().Where(c => c.Mods.Any(m => involved.Contains(m))).Take(10))
			sb.AppendLine($"    • {string.Join(" ↔ ", c.Mods.Select(DisplayName))} — {SafeSystemName(c.SystemId)} [{L10n.Text("Severity." + c.Severity.LocalizationSuffix() + ".Name")}]");

		sb.AppendLine();
		AppendRuntimeSection(sb, ctx, store);

		sb.AppendLine(L10n.Text("Report.InvestigationSteps"));
		sb.AppendLine(new string('-', 60));
		sb.AppendLine("    1. " + L10n.Text("Report.Step1"));
		sb.AppendLine("    2. " + L10n.Text("Report.Step2"));
		sb.AppendLine("    3. " + L10n.Text("Report.Step3"));
		sb.AppendLine("    4. " + L10n.Text("Report.Step4"));
		sb.AppendLine();

		return sb.ToString();
	}

	// ---------------------------------------------------------------- community summary

	public static string BuildCommunitySummary(DetectorContext ctx, ConflictStore store)
	{
		var sb = new StringBuilder(2 * 1024);
		sb.AppendLine(L10n.Text("Report.CommunityTitle"));
		sb.AppendLine(new string('-', 60));
		sb.AppendLine(L10n.Text("Report.GameVersions", SafeMainVersion(), Terraria.ModLoader.ModLoader.versionedName));
		sb.AppendLine();

		sb.AppendLine(L10n.Text("Report.CommunityMods", ctx.Mods.Count.ToString()));
		sb.AppendLine("    " + string.Join(", ", ctx.Mods.OrderBy(m => m.LoadIndex)
			.Select(m => $"{m.Name} v{m.Version}")));
		sb.AppendLine();

		var conflicts = store.GetAll();
		sb.AppendLine(L10n.Text("Report.CommunityConflicts", conflicts.Count.ToString()));
		foreach (var c in conflicts.Where(c => c.Severity == Severity.High || c.Severity == Severity.Significant).Take(10))
			sb.AppendLine($"    • {string.Join(" ↔ ", c.Mods.Select(DisplayName))} — {SafeSystemName(c.SystemId)} [{L10n.Text("Severity." + c.Severity.LocalizationSuffix() + ".Name")}/{L10n.Text("Confidence." + c.Confidence.LocalizationSuffix() + ".Name")}]");
		sb.AppendLine();

		var events = RuntimeMonitor.GetEvents();
		sb.AppendLine(L10n.Text("Report.CommunityErrors", events.Count.ToString()));
		foreach (var e in events.Take(5))
			sb.AppendLine($"    • {e.Timestamp:HH:mm:ss} {e.Type}: {e.Message}");
		sb.AppendLine();

		sb.AppendLine(L10n.Text("Report.CommunitySystems"));
		sb.AppendLine("    " + string.Join(", ", ctx.SystemOverlapCounts.OrderByDescending(kv => kv.Value).Take(8).Select(kv => SafeSystemName(kv.Key))));
		sb.AppendLine();
		sb.AppendLine(L10n.Text("Report.CommunityFooter"));

		return sb.ToString();
	}

	// ---------------------------------------------------------------- export

	public static string ExportFullReport(string content)
	{
		var dir = Path.Combine(MainSavePathProvider.Get(), "ModHarmony", "reports");
		Directory.CreateDirectory(dir);
		var path = Path.Combine(dir, $"ModHarmonyReport_{DateTime.Now:yyyyMMdd_HHmmss}.txt");
		File.WriteAllText(path, content);
		Log.Info($"Report exported to {path}");
		return path;
	}

	// ---------------------------------------------------------------- helpers

	private static string SafeMainVersion()
	{
		try { return Terraria.Main.versionNumber; }
		catch { return "?"; }
	}

	private static string SafeSystemName(string systemId)
	{
		try { return L10n.Text(SystemRegistry.Get(systemId).NameKey); }
		catch { return systemId; }
	}

	private static string DisplayName(string modName)
	{
		var facts = ModHarmony.ScanState?.Context?.Get(modName);
		return facts != null ? facts.DisplayNameSafe : modName;
	}

	private static string EvidenceText(Evidence e)
	{
		try {
			var text = L10n.EvidenceText(e.Key, e.Args ?? Array.Empty<string>());
			return string.IsNullOrEmpty(text) || text.StartsWith("Mods.ModHarmony.Evidence.") ? e.Key + ": " + string.Join(", ", e.Args ?? Array.Empty<string>()) : text;
		}
		catch {
			return e.Key + ": " + string.Join(", ", e.Args ?? Array.Empty<string>());
		}
	}
}

/// <summary>Carrier for arbitration state passed to the report builder.</summary>
public sealed class ArbitrationStateData
{
	public List<ArbitrationGroup> Groups { get; set; } = new();
}
