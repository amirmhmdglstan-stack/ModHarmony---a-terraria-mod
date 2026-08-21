using System;
using System.Linq;
using ModHarmony.Common.Core;
using ModHarmony.Common.Reporting;
using ModHarmony.Common.Utilities;
using ModHarmony.Systems;
using Terraria.GameContent.UI.Elements;
using Terraria.UI;

namespace ModHarmony.UI;

/// <summary>Overview: health score with breakdown, quick stats, what-changed, actions.</summary>
public sealed class TabOverview : TabBase
{
	private bool _showBreakdown;

	public TabOverview(Action<MHTab> navigate) : base(navigate)
	{
		SetTitle(L10n.Text("UI.Tab.Overview"));
		Build();
	}

	private void Build()
	{
		var items = new System.Collections.Generic.List<UIElement>();

		// --- Health card ---------------------------------------------------
		items.Add(new UIText(L10n.Text("UI.Overview.HealthTitle"), 0.95f, true) { TextColor = MHColors.Accent, TextOriginX = 0f });

		var health = ScanState.Health;
		var score = ScanState.HasScan ? health?.Score ?? -1 : -1;
		var scoreText = ScanState.HasScan ? $"{score}/100" : L10n.Text("UI.Overview.NoScanYet");
		var verdict = score >= 70 ? L10n.Text("Health.Good") : score >= 40 ? L10n.Text("Health.Ok") : L10n.Text("Health.Bad");
		var scoreRow = new UIElement {
			Width = StyleDimension.Fill,
			Height = new StyleDimension(30, 0f)
		};
		scoreRow.Append(new UIText(scoreText, 1.6f, true) {
			TextColor = score >= 70 ? MHColors.Success : score >= 40 ? MHColors.Medium : MHColors.Danger,
			TextOriginX = 0f,
			Width = new StyleDimension(150, 0f)
		});
		scoreRow.Append(new UIText(verdict, 1.0f, true) {
			TextColor = MHColors.Text,
			TextOriginX = 0f,
			Left = new StyleDimension(160, 0f),
			VAlign = 0.5f
		});
		items.Add(scoreRow);

		items.Add(new MHBodyText(L10n.Text("Health.Disclaimer")));
		items.Add(new MHBodyText(L10n.Text("Health.Explanation")));

		var breakdownButton = new MHButton(L10n.Text(_showBreakdown ? "UI.Overview.HideBreakdown" : "UI.Overview.ShowBreakdown"), 0.75f) {
			Width = new StyleDimension(220, 0f),
			Height = new StyleDimension(28, 0f),
			HAlign = 0f
		};
		breakdownButton.OnLeftClick += (_, _) => {
			_showBreakdown = !_showBreakdown;
			Build();
		};
		items.Add(breakdownButton);

		if (_showBreakdown && ScanState.HasScan) {
			foreach (var item in health?.Breakdown ?? Enumerable.Empty<HealthCalculator.BreakdownItem>()) {
				items.Add(new MHBodyText($"−{item.Points}: {L10n.Text(item.ReasonKey, item.Args)}", 0.75f));
			}
			if ((health?.Breakdown?.Count ?? 0) == 0)
				items.Add(new MHBodyText(L10n.Text("Health.NoDeductions")));
		}
		items.Add(Spacer(10));

		// --- Attention -----------------------------------------------------
		items.Add(new UIText(L10n.Text("UI.Overview.AttentionTitle"), 0.95f, true) { TextColor = MHColors.Accent, TextOriginX = 0f });
		var attention = ScanState.Store.CountWithSeverity(Severity.High) + ScanState.Store.CountWithSeverity(Severity.Significant);
		items.Add(new MHBodyText(attention > 0
			? L10n.Text("UI.Overview.AttentionCount", attention.ToString())
			: L10n.Text("UI.Overview.AttentionNone"), 0.8f, attention > 0 ? MHColors.Medium : MHColors.Success));
		items.Add(Spacer(10));

		// --- Start here (first-run guide) -----------------------------------
		items.Add(new UIText(L10n.Text("UI.Overview.StartHereTitle"), 0.95f, true) { TextColor = MHColors.Accent, TextOriginX = 0f });
		items.Add(new MHBodyText(L10n.Text("UI.Overview.StartHere1")));
		items.Add(new MHBodyText(L10n.Text("UI.Overview.StartHere2")));
		items.Add(new MHBodyText(L10n.Text("UI.Overview.StartHere3")));
		items.Add(Spacer(10));

		// --- Quick stats ---------------------------------------------------
		items.Add(new UIText(L10n.Text("UI.Overview.StatsTitle"), 0.95f, true) { TextColor = MHColors.Accent, TextOriginX = 0f });
		var store = ScanState.Store;
		items.Add(StatLine(L10n.Text("UI.Overview.StatMods"), ScanState.Context?.Mods.Count ?? 0));
		items.Add(StatLine(L10n.Text("UI.Overview.StatConflicts"), store.Count));
		items.Add(StatLine(L10n.Text("UI.Overview.StatHighRisk"), store.CountHighRisk));
		items.Add(StatLine(L10n.Text("UI.Overview.StatSystemsOverlap"), ScanState.Context?.SystemOverlapCounts.Count(kv => kv.Value >= 2) ?? 0));
		items.Add(StatLine(L10n.Text("UI.Overview.StatArbitration"), Common.Arbitration.ArbitrationState.Groups.Count));
		var failed = store.GetDetectorStatuses().Values.Count(s => s == DetectorStatus.Failed);
		items.Add(StatLine(L10n.Text("UI.Overview.StatDetectorsFailed"), failed));
		items.Add(Spacer(10));

		// --- What changed --------------------------------------------------
		items.Add(new UIText(L10n.Text("UI.Overview.ChangedTitle"), 0.95f, true) { TextColor = MHColors.Accent, TextOriginX = 0f });
		var changes = ScanState.ChangeSet;
		if (changes == null || !changes.HasChanges) {
			items.Add(new MHBodyText(L10n.Text("UI.Overview.NoChanges")));
		}
		else {
			if (changes.AddedMods.Count > 0)
				items.Add(new MHBodyText(L10n.Text("UI.Overview.ChangedAdded", string.Join(", ", changes.AddedMods.Select(m => m.Name)))));
			if (changes.RemovedMods.Count > 0)
				items.Add(new MHBodyText(L10n.Text("UI.Overview.ChangedRemoved", string.Join(", ", changes.RemovedMods.Select(m => m.Name)))));
			if (changes.VersionChanges.Count > 0)
				items.Add(new MHBodyText(L10n.Text("UI.Overview.ChangedVersions",
					string.Join(", ", changes.VersionChanges.Select(v => $"{v.name} {v.oldVersion}→{v.newVersion}")))));
			if (changes.NewConflicts.Count > 0)
				items.Add(new MHBodyText(L10n.Text("UI.Overview.ChangedNewConflicts", changes.NewConflicts.Count.ToString())));
			if (changes.ResolvedConflicts.Count > 0)
				items.Add(new MHBodyText(L10n.Text("UI.Overview.ChangedResolved", changes.ResolvedConflicts.Count.ToString())));
			if (changes.SeverityChanges.Count > 0)
				items.Add(new MHBodyText(L10n.Text("UI.Overview.ChangedSeverity", changes.SeverityChanges.Count.ToString())));
			if (changes.LoadOrderChanges.Count > 0)
				items.Add(new MHBodyText(L10n.Text("UI.Overview.ChangedLoadOrder", changes.LoadOrderChanges.Count.ToString())));
			if (changes.DependencyChanges.Count > 0)
				items.Add(new MHBodyText(L10n.Text("UI.Overview.ChangedDependencies", changes.DependencyChanges.Count.ToString())));
			if (changes.NewErrors.Count > 0)
				items.Add(new MHBodyText(L10n.Text("UI.Overview.ChangedErrors", changes.NewErrors.Count.ToString())));
		}
		items.Add(Spacer(10));

		// --- Actions -------------------------------------------------------
		items.Add(new UIText(L10n.Text("UI.Overview.ActionsTitle"), 0.95f, true) { TextColor = MHColors.Accent, TextOriginX = 0f });
		items.Add(MakeButton(L10n.Text("UI.Overview.Analyze"), () => {
			var report = ReportGenerator.BuildInvestigationReport(ScanState.Context, ScanState.Store, ScanState.Health);
			TabInvestigation.PendingPreview = report;
			Navigate(MHTab.Investigation);
		}));
		items.Add(MakeButton(L10n.Text("UI.Overview.Export"), () => {
			var report = ReportGenerator.BuildFullReport(ScanState.Context, ScanState.Store, ScanState.Health, ScanState.Snapshot, new ArbitrationStateData { Groups = Common.Arbitration.ArbitrationState.Groups });
			var path = ReportGenerator.ExportFullReport(report);
			SetStatus(L10n.Text("UI.Reports.ExportedTo", path));
		}));
		items.Add(MakeButton(L10n.Text("UI.Overview.SaveSummary"), () => {
			var summary = ReportGenerator.BuildCommunitySummary(ScanState.Context, ScanState.Store);
			var path = ReportGenerator.ExportCommunitySummary(summary);
			SetStatus(L10n.Text("UI.Reports.SavedTo", path));
		}));
		items.Add(MakeButton(L10n.Text("UI.Overview.OpenArbitration"), () => Navigate(MHTab.Arbitration)));
		items.Add(MakeButton(L10n.Text("UI.Overview.Rescan"), () => {
			ModHarmonySystem.QueueRescan();
			SetStatus(L10n.Text("UI.Overview.RescanQueued"));
		}));

		ListPanel.SetItems(items);
		SetStatus(L10n.Text("UI.Footer", ScanState.SessionId, ScanState.ScanRunCount.ToString()));
	}

	private static UIElement StatLine(string label, int value)
	{
		var row = new UIElement {
			Width = StyleDimension.Fill,
			Height = new StyleDimension(22, 0f)
		};
		row.Append(new UIText(label, 0.8f) {
			TextColor = MHColors.Text,
			TextOriginX = 0f,
			Left = new StyleDimension(0, 0f)
		});
		row.Append(new UIText(value.ToString(), 0.8f) {
			TextColor = MHColors.TextDim,
			HAlign = 1f
		});
		return row;
	}

	private static MHButton MakeButton(string text, Action onClick)
	{
		var button = new MHButton(text, 0.75f) {
			Width = new StyleDimension(240, 0f),
			Height = new StyleDimension(30, 0f),
			HAlign = 0f
		};
		button.OnLeftClick += (_, _) => onClick();
		return button;
	}

}
