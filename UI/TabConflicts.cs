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
/// Conflicts tab: search + severity/confidence/resolvability filters over all
/// detected conflicts. Each card expands to show evidence, the "why is this
/// here" explanation, arbitration status and controls, and mute/unmute.
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
		_search.Width = new StyleDimension(220, 0f);
		_search.OnTextChanged += () => BuildList();
		AddToolbar(_search, 0f);

		float x = 230f;
		foreach (SevFilter f in Enum.GetValues(typeof(SevFilter))) {
			var b = new MHButton(L10n.Text($"UI.Conflicts.Sev.{f}"), 0.65f) {
				Width = new StyleDimension(78, 0f),
				Height = new StyleDimension(26, 0f)
			};
			var cap = f;
			b.OnLeftClick += (_, _) => { _sev = cap; BuildToolbar(); BuildList(); };
			_sevButtons[f] = b;
			AddToolbar(b, x);
			x += 84f;
		}

		foreach (ConfFilter f in Enum.GetValues(typeof(ConfFilter))) {
			var b = new MHButton(L10n.Text($"UI.Conflicts.Conf.{f}"), 0.6f) {
				Width = new StyleDimension(72, 0f),
				Height = new StyleDimension(26, 0f)
			};
			var cap = f;
			b.OnLeftClick += (_, _) => { _conf = cap; BuildToolbar(); BuildList(); };
			_confButtons[f] = b;
			AddToolbar(b, x);
			x += 78f;
		}

		foreach (ResFilter f in Enum.GetValues(typeof(ResFilter))) {
			var b = new MHButton(L10n.Text($"UI.Conflicts.Res.{f}"), 0.6f) {
				Width = new StyleDimension(88, 0f),
				Height = new StyleDimension(26, 0f)
			};
			var cap = f;
			b.OnLeftClick += (_, _) => { _res = cap; BuildToolbar(); BuildList(); };
			_resButtons[f] = b;
			AddToolbar(b, x);
			x += 94f;
		}

		var mutedToggle = new MHButton(L10n.Text(_showMuted ? "UI.Conflicts.HideMuted" : "UI.Conflicts.ShowMuted"), 0.65f) {
			Width = new StyleDimension(110, 0f),
			Height = new StyleDimension(26, 0f)
		};
		mutedToggle.OnLeftClick += (_, _) => { _showMuted = !_showMuted; BuildToolbar(); BuildList(); };
		AddToolbar(mutedToggle, x);

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

		List.SetItems(items);
		SetStatus(L10n.Text("UI.Conflicts.Count", shown.ToString(), conflicts.Count.ToString()));
	}

	private UIElement BuildCard(Conflict c, bool muted)
	{
		var card = new UIPanel {
			Width = StyleDimension.Fill,
			BackgroundColor = new Color(20, 24, 33, 230),
			BorderColor = MHColors.Severity(c.Severity) * 0.6f
		};
		card.SetPadding(6);

		bool expanded = _expanded.Contains(c.Id);
		float bodyHeight = expanded ? EstimateExpandedHeight(c) : 0f;

		var header = new UIElement {
			Width = StyleDimension.Fill,
			Height = new StyleDimension(30, 0f)
		};

		var severityLabel = L10n.Text("Severity." + c.Severity.LocalizationSuffix() + ".Name").ToUpperInvariant();
		var sevText = new UIText(severityLabel, 0.7f) {
			TextColor = MHColors.Severity(c.Severity),
			TextOriginX = 0f,
			Width = new StyleDimension(120, 0f)
		};
		header.Append(sevText);

		var mods = string.Join(" ↔ ", c.Mods.Select(DisplayNameOf));
		header.Append(new UIText(mods, 0.85f) {
			TextColor = MHColors.Text,
			TextOriginX = 0f,
			Left = new StyleDimension(130, 0f),
			Width = new StyleDimension(-500, 1f)
		});

		header.Append(new UIText(SafeSystemName(c.SystemId), 0.7f) {
			TextColor = MHColors.TextDim,
			HAlign = 0.88f,
			Width = new StyleDimension(150, 0f)
		});
		header.Append(new UIText(L10n.Text("Confidence." + c.Confidence.LocalizationSuffix() + ".Name"), 0.7f) {
			TextColor = MHColors.Confidence(c.Confidence),
			HAlign = 1f
		});

		card.Append(header);

		if (expanded) {
			var body = new UIElement {
				Top = new StyleDimension(32, 0f),
				Width = StyleDimension.Fill,
				Height = new StyleDimension(bodyHeight, 0f)
			};

			float y = 0f;
			foreach (var e in c.Evidence) {
				var line = EvidenceText(e);
				body.Append(WrappedLine(line, y, 0.7f));
				y += 20f;
			}

			var why = WrappedLine(L10n.Text("UI.Conflicts.WhyHeader") + " " + WhyText(c), y, 0.7f, MHColors.Text);
			body.Append(why);
			y += 20f * EstimateWrappedLines(L10n.Text("UI.Conflicts.WhyHeader") + " " + WhyText(c));

			// Arbitration status.
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
			body.Append(WrappedLine(arbitrationLine, y, 0.7f, MHColors.Medium));
			y += 22f;

			if (ScanState.Context?.Config?.DeveloperMode == true) {
				body.Append(WrappedLine($"id={c.Id} detector={c.DetectorId} system={c.SystemId}", y, 0.65f, MHColors.TextDim));
				y += 18f;
			}

			var muteButton = new MHButton(L10n.Text(muted ? "UI.Conflicts.Unmute" : "UI.Conflicts.Mute"), 0.7f) {
				Top = new StyleDimension(y, 0f),
				Width = new StyleDimension(120, 0f),
				Height = new StyleDimension(26, 0f),
				HAlign = 0f
			};
			muteButton.OnLeftClick += (_, _) => {
				ConflictPrefs.SetMuted(c.Id, !muted);
				BuildList();
			};
			body.Append(muteButton);
			y += 32f;

			card.Append(body);
		}

		var id = c.Id;
		card.OnLeftClick += (_, _) => {
			if (_expanded.Contains(id))
				_expanded.Remove(id);
			else
				_expanded.Add(id);
			BuildList();
		};

		// Keep the whole card at a fixed height by wrapping header + body.
		card.Height = new StyleDimension(30 + bodyHeight + 12, 0f);
		return card;
	}

	private static float EstimateExpandedHeight(Conflict c) => 34f + c.Evidence.Count * 20f + 40f;

	private static int EstimateWrappedLines(string text)
	{
		int len = text?.Length ?? 0;
		return Math.Max(1, (len + 95) / 95); // ~95 chars per wrapped line at 0.7 scale
	}

	private static UIElement WrappedLine(string text, float top, float scale, Color? color = null)
	{
		return new MHBodyText(text, scale, color) {
			Top = new StyleDimension(top, 0f),
			Width = StyleDimension.Fill
		};
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
