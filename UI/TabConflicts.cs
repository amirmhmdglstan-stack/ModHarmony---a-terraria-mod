using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using ModHarmony.Common.Arbitration;
using ModHarmony.Common.Core;
using ModHarmony.Common.Reporting;
using ModHarmony.Common.Utilities;
using Terraria.GameContent.UI.Elements;
using Terraria.UI;

namespace ModHarmony.UI;

/// <summary>
/// Conflicts tab: search + severity/certainty/fixable filters over all flagged
/// interactions. Each card shows a plain-language summary and a "what you can
/// do" line; technical evidence is tucked behind a "Technical details" toggle
/// so normal users are not overwhelmed.
/// </summary>
public sealed class TabConflicts : TabBase
{
	private enum SevFilter { All, High, Significant, Medium, Low, Info }
	private enum ConfFilter { All, Confirmed, Strong, Possible, Unknown }
	private enum ResFilter { All, Resolvable, Unresolvable }

	private readonly MHTextField _search = new(L10n.Text("UI.Conflicts.SearchHint"));
	private readonly Dictionary<SevFilter, MHButton> _sevButtons = new();
	private readonly Dictionary<ConfFilter, MHButton> _confButtons = new();
	private readonly Dictionary<ResFilter, MHButton> _resButtons = new();
	private bool _showMuted;
	private bool _includeQuiet = true;

	private SevFilter _sev = SevFilter.All;
	private ConfFilter _conf = ConfFilter.All;
	private ResFilter _res = ResFilter.All;
	private readonly HashSet<string> _expanded = new();

	public TabConflicts(Action<MHTab> navigate) : base(navigate)
	{
		SetTitle(L10n.Text("UI.Tab.Conflicts"));
		BuildToolbar();
		BuildList();
	}

	private void BuildToolbar()
	{
		_search.Width = new StyleDimension(200, 0f);
		_search.OnTextChanged += () => BuildList();
		AddToolbar(_search, 0f);

		float x = 210f;
		foreach (SevFilter f in Enum.GetValues(typeof(SevFilter))) {
			var b = new MHButton(L10n.Text($"UI.Conflicts.Sev.{f}"), 0.65f) {
				Width = new StyleDimension(76, 0f),
				Height = new StyleDimension(26, 0f)
			};
			var cap = f;
			b.OnLeftClick += (_, _) => { _sev = cap; BuildToolbar(); BuildList(); };
			_sevButtons[f] = b;
			AddToolbar(b, x);
			x += 82f;
		}

		foreach (ConfFilter f in Enum.GetValues(typeof(ConfFilter))) {
			var b = new MHButton(L10n.Text($"UI.Conflicts.Conf.{f}"), 0.6f) {
				Width = new StyleDimension(70, 0f),
				Height = new StyleDimension(26, 0f)
			};
			var cap = f;
			b.OnLeftClick += (_, _) => { _conf = cap; BuildToolbar(); BuildList(); };
			_confButtons[f] = b;
			AddToolbar(b, x);
			x += 76f;
		}

		foreach (ResFilter f in Enum.GetValues(typeof(ResFilter))) {
			var b = new MHButton(L10n.Text($"UI.Conflicts.Res.{f}"), 0.6f) {
				Width = new StyleDimension(86, 0f),
				Height = new StyleDimension(26, 0f)
			};
			var cap = f;
			b.OnLeftClick += (_, _) => { _res = cap; BuildToolbar(); BuildList(); };
			_resButtons[f] = b;
			AddToolbar(b, x);
			x += 92f;
		}

		var mutedToggle = new MHButton(L10n.Text(_showMuted ? "UI.Conflicts.HideMuted" : "UI.Conflicts.ShowMuted"), 0.6f) {
			Width = new StyleDimension(92, 0f),
			Height = new StyleDimension(26, 0f)
		};
		mutedToggle.OnLeftClick += (_, _) => { _showMuted = !_showMuted; BuildToolbar(); BuildList(); };
		AddToolbar(mutedToggle, x);
		x += 98f;

		var quietToggle = new MHButton(L10n.Text(_includeQuiet ? "UI.Conflicts.HideQuiet" : "UI.Conflicts.IncludeQuiet"), 0.6f) {
			Width = new StyleDimension(120, 0f),
			Height = new StyleDimension(26, 0f)
		};
		quietToggle.OnLeftClick += (_, _) => { _includeQuiet = !_includeQuiet; BuildList(); };
		AddToolbar(quietToggle, x);

		RebuildToolbar();

		foreach (var kv in _sevButtons)
			kv.Value.BackgroundColor = kv.Key == _sev ? MHColors.Accent : MHColors.AccentDark;
		foreach (var kv in _confButtons)
			kv.Value.BackgroundColor = kv.Key == _conf ? MHColors.Accent : MHColors.AccentDark;
		foreach (var kv in _resButtons)
			kv.Value.BackgroundColor = kv.Key == _res ? MHColors.Accent : MHColors.AccentDark;
	}

	private void BuildList()
	{
		var store = ScanState.Store;
		var conflicts = store.GetAll();
		var query = _search.Text.Trim().ToLowerInvariant();
		var config = ScanState.Context?.Config;
		var items = new List<UIElement>();
		int shown = 0;

		var sevFilter = _sev switch {
			SevFilter.High => (Severity?)Severity.High,
			SevFilter.Significant => Severity.Significant,
			SevFilter.Medium => Severity.Medium,
			SevFilter.Low => Severity.Low,
			SevFilter.Info => Severity.Info,
			_ => null
		};
		var confFilter = _conf switch {
			ConfFilter.Confirmed => (Confidence?)Confidence.Confirmed,
			ConfFilter.Strong => Confidence.Strong,
			ConfFilter.Possible => Confidence.Possible,
			ConfFilter.Unknown => Confidence.Unknown,
			_ => null
		};

		foreach (var c in conflicts) {
			bool muted = ConflictPrefs.IsMuted(c.Id);
			if (muted && !_showMuted)
				continue;
			if (sevFilter != null && c.Severity != sevFilter.Value)
				continue;
			if (confFilter != null && c.Confidence != confFilter.Value)
				continue;
			if (_res == ResFilter.Resolvable && c.ArbitrationGroupId.Length == 0)
				continue;
			if (_res == ResFilter.Unresolvable && c.ArbitrationGroupId.Length > 0)
				continue;

			// Quiet items are hidden unless the user asks for them (or filters by them).
			bool quiet = c.Severity == Severity.Info || (c.Severity == Severity.Low && !(config?.ShowLowRisk ?? true));
			if (quiet && !_includeQuiet && sevFilter == null && _conf == ConfFilter.All)
				continue;

			if (query.Length > 0) {
				var hay = string.Join(" ", c.Mods) + " " + c.SystemId + " " + c.DetectorId;
				if (!hay.ToLowerInvariant().Contains(query))
					continue;
			}

			items.Add(BuildCard(c, muted));
			shown++;
		}

		if (shown == 0)
			items.Add(new MHBodyText(L10n.Text("UI.Conflicts.NoResults")));

		ListPanel.SetItems(items);
		SetStatus(L10n.Text("UI.Conflicts.Count", shown.ToString(), conflicts.Count.ToString()));
	}

	private UIElement BuildCard(Conflict c, bool muted)
	{
		var card = new UIPanel {
			Width = StyleDimension.Fill,
			BackgroundColor = new Color(20, 24, 33, 230),
			BorderColor = MHColors.SeverityColor(c.Severity) * 0.6f
		};
		card.SetPadding(6);

		bool expanded = _expanded.Contains(c.Id);
		float bodyHeight = expanded ? EstimateExpandedHeight(c) : 0f;

		// ---- Header: severity + mods + system ----------------------------
		var header = new UIElement {
			Width = StyleDimension.Fill,
			Height = new StyleDimension(26, 0f)
		};

		var severityLabel = L10n.Text("Severity." + c.Severity.LocalizationSuffix() + ".Name");
		header.Append(new UIText(severityLabel, 0.7f) {
			TextColor = MHColors.SeverityColor(c.Severity),
			TextOriginX = 0f,
			Width = new StyleDimension(110, 0f)
		});

		var modNames = c.Mods.Select(DisplayNameOf).ToList();
		var mods = string.Join(" ↔ ", modNames.Take(4));
		if (modNames.Count > 4)
			mods += $" (+{modNames.Count - 4})";
		header.Append(new UIText(mods, 0.85f) {
			TextColor = MHColors.Text,
			TextOriginX = 0f,
			Left = new StyleDimension(118, 0f),
			Width = new StyleDimension(-420, 1f)
		});

		header.Append(new UIText(SafeSystemName(c.SystemId), 0.7f) {
			TextColor = MHColors.TextDim,
			HAlign = 1f,
			Width = new StyleDimension(150, 0f)
		});

		card.Append(header);

		// ---- Summary + action (always visible) ---------------------------
		float y = 30f;
		var summary = SummaryText(c);
		card.Append(WrappedLine(summary, y, 0.75f, MHColors.Text));
		y += 18f;

		var action = L10n.Text($"UI.Conflicts.Action.{c.DetectorId}");
		card.Append(WrappedLine(L10n.Text("UI.Conflicts.ActionTitle") + " " + action, y, 0.75f, MHColors.Success));
		y += 18f;

		// ---- Details toggle ----------------------------------------------
		var detailsButton = new MHButton(L10n.Text(expanded ? "UI.Conflicts.HideDetails" : "UI.Conflicts.Details"), 0.65f) {
			Top = new StyleDimension(y, 0f),
			Width = new StyleDimension(150, 0f),
			Height = new StyleDimension(24, 0f),
			HAlign = 0f
		};
		detailsButton.OnLeftClick += (_, _) => {
			if (_expanded.Contains(c.Id))
				_expanded.Remove(c.Id);
			else
				_expanded.Add(c.Id);
			BuildList();
		};
		card.Append(detailsButton);
		y += 30f;

		if (expanded) {
			var body = new UIElement {
				Top = new StyleDimension(62, 0f),
				Width = StyleDimension.Fill,
				Height = new StyleDimension(bodyHeight, 0f)
			};

			float by = 0f;
			body.Append(WrappedLine(L10n.Text("UI.Conflicts.WhyHeader") + " " + WhyText(c), by, 0.7f, MHColors.Text));
			by += 18f;

			foreach (var e in c.Evidence) {
				body.Append(WrappedLine(EvidenceText(e), by, 0.7f));
				by += 18f;
			}

			string arbitrationLine;
			if (c.ArbitrationGroupId.Length > 0) {
				var group = ArbitrationState.Get(c.ArbitrationGroupId);
				arbitrationLine = group?.CanResolve == true
					? L10n.Text("UI.Conflicts.ArbitrationActive", group.Strategy.ToString(), group.ResolvedWinner)
					: L10n.Text("UI.Conflicts.ArbitrationUnavailable");
			}
			else {
				arbitrationLine = L10n.Text("UI.Conflicts.ArbitrationUnavailable");
			}
			body.Append(WrappedLine(arbitrationLine, by, 0.7f, MHColors.Medium));
			by += 20f;

			if (ScanState.Context?.Config?.DeveloperMode == true) {
				body.Append(WrappedLine($"id={c.Id} detector={c.DetectorId} system={c.SystemId}", by, 0.65f, MHColors.TextDim));
				by += 18f;
			}

			var muteButton = new MHButton(L10n.Text(muted ? "UI.Conflicts.Unmute" : "UI.Conflicts.Mute"), 0.7f) {
				Top = new StyleDimension(by, 0f),
				Width = new StyleDimension(120, 0f),
				Height = new StyleDimension(26, 0f),
				HAlign = 0f
			};
			muteButton.OnLeftClick += (_, _) => {
				ConflictPrefs.SetMuted(c.Id, !muted);
				BuildList();
			};
			body.Append(muteButton);

			card.Append(body);
		}

		card.Height = new StyleDimension(30 + bodyHeight + 42, 0f);
		return card;
	}

	private static float EstimateExpandedHeight(Conflict c) => 24f + c.Evidence.Count * 18f + 70f;

	private static UIElement WrappedLine(string text, float top, float scale, Color? color = null)
	{
		return new MHBodyText(text, scale, color) {
			Top = new StyleDimension(top, 0f),
			Width = StyleDimension.Fill
		};
	}

	private static string SummaryText(Conflict c)
	{
		try {
			return L10n.Text($"UI.Conflicts.Summary.{c.DetectorId}");
		}
		catch {
			return "";
		}
	}

	private static string WhyText(Conflict c)
	{
		try {
			return L10n.Text($"UI.Conflicts.Why.{c.DetectorId}");
		}
		catch {
			return "";
		}
	}

	private static string EvidenceText(Evidence e)
	{
		try {
			return "• " + L10n.EvidenceText(e.Key, e.Args ?? Array.Empty<string>());
		}
		catch {
			return "• " + e.Key;
		}
	}

	private static string SafeSystemName(string systemId)
	{
		try { return L10n.Text(SystemRegistry.Get(systemId).NameKey); }
		catch { return systemId; }
	}

	private static string DisplayNameOf(string modName)
	{
		var facts = ScanState.Context?.Get(modName);
		return facts != null ? facts.DisplayNameSafe : modName;
	}
}
