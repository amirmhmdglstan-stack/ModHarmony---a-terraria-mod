using System;
using System.Collections.Generic;
using ModHarmony.Common.Arbitration;
using ModHarmony.Common.Reporting;
using ModHarmony.Common.Utilities;
using ModHarmony.Systems;
using Terraria.GameContent.UI.Elements;
using Terraria.UI;

namespace ModHarmony.UI;

/// <summary>
/// Reports tab: export the full compatibility report to a text file, copy the
/// community summary to the clipboard, and preview the last generated report.
/// </summary>
public sealed class TabReports : TabBase
{
	public static string PendingPreview { get; set; }

	private string _preview;

	public TabReports(Action<MHTab> navigate) : base(navigate)
	{
		SetTitle(L10n.Text("UI.Tab.Reports"));
		if (!string.IsNullOrEmpty(PendingPreview)) {
			_preview = PendingPreview;
			PendingPreview = null;
		}
		Build();
	}

	private void Build()
	{
		var items = new List<UIElement>();

		items.Add(new UIText(L10n.Text("UI.Reports.Title"), 0.95f, true) { TextColor = MHColors.Accent, TextOriginX = 0f });
		items.Add(new MHBodyText(L10n.Text("UI.Reports.Explanation")));
		items.Add(Spacer(6));

		var export = new MHButton(L10n.Text("UI.Reports.ExportFull"), 0.8f) {
			Width = new StyleDimension(280, 0f),
			Height = new StyleDimension(30, 0f),
			HAlign = 0f
		};
		export.OnLeftClick += (_, _) => {
			if (!ScanState.HasScan) {
				SetStatus(L10n.Text("UI.NoScan"));
				return;
			}
			var report = ReportGenerator.BuildFullReport(ScanState.Context, ScanState.Store, ScanState.Health,
				ScanState.Snapshot, new ArbitrationStateData { Groups = ArbitrationState.Groups });
			_preview = report;
			var path = ReportGenerator.ExportFullReport(report);
			SetStatus(L10n.Text("UI.Reports.ExportedTo", path));
			Build();
		};
		items.Add(export);

		var copy = new MHButton(L10n.Text("UI.Reports.SaveSummary"), 0.8f) {
			Width = new StyleDimension(280, 0f),
			Height = new StyleDimension(30, 0f),
			Left = new StyleDimension(290, 0f)
		};
		copy.OnLeftClick += (_, _) => {
			if (!ScanState.HasScan) {
				SetStatus(L10n.Text("UI.NoScan"));
				return;
			}
			var summary = ReportGenerator.BuildCommunitySummary(ScanState.Context, ScanState.Store);
			var path = ReportGenerator.ExportCommunitySummary(summary);
			SetStatus(L10n.Text("UI.Reports.SavedTo", path));
		};
		items.Add(copy);

		var rescan = new MHButton(L10n.Text("UI.Overview.Rescan"), 0.8f) {
			Width = new StyleDimension(200, 0f),
			Height = new StyleDimension(30, 0f),
			Left = new StyleDimension(580, 0f)
		};
		rescan.OnLeftClick += (_, _) => {
			ModHarmonySystem.QueueRescan();
			SetStatus(L10n.Text("UI.Overview.RescanQueued"));
		};
		items.Add(rescan);

		items.Add(Spacer(10));

		items.Add(new UIText(L10n.Text("UI.Reports.PreviewTitle"), 0.9f, true) { TextColor = MHColors.Accent, TextOriginX = 0f });

		if (string.IsNullOrEmpty(_preview)) {
			items.Add(new MHBodyText(L10n.Text("UI.Reports.NoPreview")));
		}
		else {
			foreach (var line in _preview.Split('\n'))
				items.Add(new MHBodyText(line, 0.7f, MHColors.Text));
		}

		ListPanel.SetItems(items);
	}
}
