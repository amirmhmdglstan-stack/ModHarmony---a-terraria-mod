using System;
using System.Collections.Generic;
using System.Linq;
using ModHarmony.Common.Arbitration;
using ModHarmony.Common.Core;
using ModHarmony.Common.Utilities;
using ModHarmony.Systems;
using Terraria.GameContent.UI.Elements;
using Terraria.ModLoader;
using Terraria.UI;

namespace ModHarmony.UI;

/// <summary>
/// Settings tab: read-only summary of the active configuration, detector
/// health, and paths. The actual editable settings live in the tModLoader
/// config UI (Mods → ModHarmony → Config).
/// </summary>
public sealed class TabSettings : TabBase
{
	public TabSettings(Action<MHTab> navigate) : base(navigate)
	{
		SetTitle(L10n.Text("UI.Tab.Settings"));
		Build();
	}

	private void Build()
	{
		var config = ScanState.Context?.Config ?? ModContent.GetInstance<Content.Config.ModHarmonyConfig>();
		var items = new List<UIElement>();

		items.Add(new UIText(L10n.Text("UI.Settings.Title"), 0.95f, true) { TextColor = MHColors.Accent, TextOriginX = 0f });
		items.Add(new MHBodyText(L10n.Text("UI.Settings.ConfigHint")));

		var openConfig = new MHButton(L10n.Text("UI.Settings.OpenConfig"), 0.8f) {
			Width = new StyleDimension(280, 0f),
			Height = new StyleDimension(30, 0f),
			HAlign = 0f
		};
		openConfig.OnLeftClick += (_, _) => {
			if (config != null) {
				UIHelper.Hide();
				config.Open();
			}
		};
		items.Add(openConfig);

		var rescan = new MHButton(L10n.Text("UI.Overview.Rescan"), 0.8f) {
			Width = new StyleDimension(200, 0f),
			Height = new StyleDimension(30, 0f),
			Left = new StyleDimension(290, 0f)
		};
		rescan.OnLeftClick += (_, _) => {
			ModHarmonySystem.QueueRescan();
			SetStatus(L10n.Text("UI.Overview.RescanQueued"));
		};
		items.Add(rescan);
		items.Add(Spacer(8));

		if (config != null) {
			items.Add(new UIText(L10n.Text("UI.Settings.CurrentTitle"), 0.9f, true) { TextColor = MHColors.Accent, TextOriginX = 0f });
			AddSetting(items, L10n.Text("UI.Settings.Enabled"), config.EnableModHarmony ? L10n.Text("UI.Mods.Yes") : L10n.Text("UI.Mods.No"));
			AddSetting(items, L10n.Text("UI.Settings.SafeMode"), config.SafeDiagnosticsMode ? L10n.Text("UI.Mods.Yes") : L10n.Text("UI.Mods.No"));
			AddSetting(items, L10n.Text("UI.Settings.Arbitration"), config.ArbitrationActive ? L10n.Text("UI.Mods.Yes") : L10n.Text("UI.Mods.No"));
			AddSetting(items, L10n.Text("UI.Settings.DefaultStrategy"), L10n.Text("Arbitration.Strategy." + config.DefaultStrategy.LocalizationSuffix() + ".Name"));
			AddSetting(items, L10n.Text("UI.Settings.RuntimeMonitoring"), config.RuntimeMonitoring ? L10n.Text("UI.Mods.Yes") : L10n.Text("UI.Mods.No"));
			AddSetting(items, L10n.Text("UI.Settings.InvestigationActive"), Common.Diagnostics.RuntimeMonitor.Active ? L10n.Text("UI.Mods.Yes") : L10n.Text("UI.Mods.No"));
			AddSetting(items, L10n.Text("UI.Settings.DeveloperMode"), config.DeveloperMode ? L10n.Text("UI.Mods.Yes") : L10n.Text("UI.Mods.No"));
		}
		items.Add(Spacer(8));

		// Detector health.
		items.Add(new UIText(L10n.Text("UI.Settings.DetectorsTitle"), 0.9f, true) { TextColor = MHColors.Accent, TextOriginX = 0f });
		var statuses = ScanState.Store.GetDetectorStatuses();
		if (statuses.Count == 0) {
			items.Add(new MHBodyText(L10n.Text("UI.Settings.NoScanYet")));
		}
		else {
			foreach (var kv in statuses.OrderBy(kv => kv.Key)) {
				items.Add(new MHBodyText($"  • {kv.Key}: {L10n.Text("Detector.Status." + kv.Value)}", 0.75f));
			}
			var failures = ScanState.Store.GetDetectorFailures();
			foreach (var failure in failures.Take(5))
				items.Add(new MHBodyText("    " + failure, 0.65f, MHColors.Danger));
		}
		items.Add(Spacer(8));

		// Paths.
		items.Add(new UIText(L10n.Text("UI.Settings.PathsTitle"), 0.9f, true) { TextColor = MHColors.Accent, TextOriginX = 0f });
		AddSetting(items, L10n.Text("UI.Settings.PathSave"), MainSavePathProvider.Get());
		AddSetting(items, L10n.Text("UI.Settings.PathMods"), Terraria.ModLoader.ModLoader.ModPath);
		AddSetting(items, L10n.Text("UI.Settings.PathData"), Common.Arbitration.ArbitrationStore.SaveDirectory);

		List.SetItems(items);
	}

	private static void AddSetting(List<UIElement> items, string label, string value)
	{
		var row = new UIElement {
			Width = StyleDimension.Fill,
			Height = new StyleDimension(20, 0f)
		};
		row.Append(new UIText(label + ":", 0.75f) {
			TextColor = MHColors.TextDim,
			TextOriginX = 0f,
			Width = new StyleDimension(220, 0f)
		});
		row.Append(new UIText(value, 0.75f) {
			TextColor = MHColors.Text,
			TextOriginX = 0f,
			Left = new StyleDimension(230, 0f),
			Width = new StyleDimension(-240, 1f)
		});
		items.Add(row);
	}
}
