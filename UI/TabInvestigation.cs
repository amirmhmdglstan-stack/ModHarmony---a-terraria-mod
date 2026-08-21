using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using ModHarmony.Common.Diagnostics;
using ModHarmony.Common.Reporting;
using ModHarmony.Common.Utilities;
using ModHarmony.Systems;
using Terraria.GameContent.UI.Elements;
using Terraria.UI;

namespace ModHarmony.UI;

/// <summary>
/// Investigation tab: enables/disables Investigation Mode, shows captured
/// runtime events and performance sampling, and produces an "Analyze Current
/// Situation" report.
/// </summary>
public sealed class TabInvestigation : TabBase
{
	private bool _showStacks;
	private string _preview;

	public static string PendingPreview { get; set; }

	public TabInvestigation(Action<MHTab> navigate) : base(navigate)
	{
		SetTitle(L10n.Text("UI.Tab.Investigation"));
		if (!string.IsNullOrEmpty(PendingPreview)) {
			_preview = PendingPreview;
			PendingPreview = null;
		}
		Build();
	}

	private void Build()
	{
		var items = new List<UIElement>();
		bool active = RuntimeMonitor.Active;

		items.Add(new UIText(L10n.Text(active ? "UI.Investigation.ActiveTitle" : "UI.Investigation.InactiveTitle"), 0.95f, true) {
			TextColor = active ? MHColors.Success : MHColors.Text,
			TextOriginX = 0f
		});
		items.Add(new MHBodyText(L10n.Text("UI.Investigation.Explanation")));

		var toggle = new MHButton(L10n.Text(active ? "UI.Investigation.Disable" : "UI.Investigation.Enable"), 0.8f) {
			Width = new StyleDimension(200, 0f),
			Height = new StyleDimension(30, 0f),
			HAlign = 0f
		};
		toggle.OnLeftClick += (_, _) => {
			bool next = !RuntimeMonitor.Active;
			RuntimeMonitor.SetActive(next);
			PerformanceTracker.SetActive(next);
			Build();
		};
		items.Add(toggle);

		var clear = new MHButton(L10n.Text("UI.Investigation.Clear"), 0.8f) {
			Width = new StyleDimension(150, 0f),
			Height = new StyleDimension(30, 0f),
			Left = new StyleDimension(210, 0f)
		};
		clear.OnLeftClick += (_, _) => {
			RuntimeMonitor.Clear();
			Build();
		};
		items.Add(clear);

		var analyze = new MHButton(L10n.Text("UI.Investigation.Analyze"), 0.8f) {
			Width = new StyleDimension(240, 0f),
			Height = new StyleDimension(30, 0f),
			Left = new StyleDimension(370, 0f)
		};
		analyze.OnLeftClick += (_, _) => {
			_preview = ReportGenerator.BuildInvestigationReport(ScanState.Context, ScanState.Store, ScanState.Health);
			Build();
		};
		items.Add(analyze);

		var stacks = new MHButton(L10n.Text(_showStacks ? "UI.Investigation.HideStacks" : "UI.Investigation.ShowStacks"), 0.8f) {
			Width = new StyleDimension(200, 0f),
			Height = new StyleDimension(30, 0f),
			Left = new StyleDimension(620, 0f)
		};
		stacks.OnLeftClick += (_, _) => {
			_showStacks = !_showStacks;
			Build();
		};
		items.Add(stacks);

		items.Add(Spacer(8));

		// Performance summary.
		items.Add(new UIText(L10n.Text("UI.Investigation.PerformanceTitle"), 0.9f, true) { TextColor = MHColors.Accent, TextOriginX = 0f });
		items.Add(new MHBodyText(L10n.Text("UI.Investigation.PerformanceSummary", PerformanceTracker.Summary())));
		items.Add(new MHBodyText(L10n.Text("UI.Investigation.PerformanceCaveat")));
		items.Add(Spacer(8));

		// Captured events.
		var events = RuntimeMonitor.GetEvents();
		items.Add(new UIText(L10n.Text("UI.Investigation.EventsTitle", events.Count.ToString()), 0.9f, true) { TextColor = MHColors.Accent, TextOriginX = 0f });
		if (events.Count == 0) {
			items.Add(new MHBodyText(L10n.Text("UI.Investigation.NoEvents")));
		}
		else {
			foreach (var e in events.Take(100)) {
				var mods = e.InvolvedMods.Count > 0 ? string.Join(", ", e.InvolvedMods) : L10n.Text("UI.Investigation.NoModAttribution");
				var line = $"{e.Timestamp:HH:mm:ss} {e.Type} (x{e.Occurrences}) — {e.Message}";
				items.Add(new MHBodyText(line, 0.75f, e.InvolvedMods.Count > 0 ? MHColors.Medium : MHColors.TextDim));
				items.Add(new MHBodyText(L10n.Text("UI.Investigation.InvolvedMods", mods), 0.7f));
				if (_showStacks) {
					foreach (var frame in e.StackFrames.Take(8))
						items.Add(new MHBodyText("    " + frame, 0.6f, MHColors.TextDim));
				}
			}
		}

		items.Add(Spacer(8));

		// Report preview.
		if (!string.IsNullOrEmpty(_preview)) {
			items.Add(new UIText(L10n.Text("UI.Investigation.PreviewTitle"), 0.9f, true) { TextColor = MHColors.Accent, TextOriginX = 0f });
			foreach (var line in _preview.Split('\n')) {
				items.Add(new MHBodyText(line, 0.7f, MHColors.Text));
			}

			var export = new MHButton(L10n.Text("UI.Investigation.Export"), 0.8f) {
				Width = new StyleDimension(220, 0f),
				Height = new StyleDimension(30, 0f),
				HAlign = 0f
			};
			export.OnLeftClick += (_, _) => {
				var path = ReportGenerator.ExportFullReport(_preview);
				SetStatus(L10n.Text("UI.Reports.ExportedTo", path));
			};
			items.Add(export);
		}

		List.SetItems(items);
		SetStatus(L10n.Text("UI.Investigation.Status", RuntimeMonitor.Count.ToString(), PerformanceTracker.SpikeCount.ToString()));
	}
}
