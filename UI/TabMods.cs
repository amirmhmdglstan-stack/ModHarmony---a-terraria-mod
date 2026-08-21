using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using ModHarmony.Common.Core;
using ModHarmony.Common.Utilities;
using Terraria.GameContent.UI.Elements;
using Terraria.UI;

namespace ModHarmony.UI;

/// <summary>
/// Mods tab: searchable/filterable list of loaded mods; clicking a mod opens a
/// detail view (metadata, dependencies, detected hooks/systems, conflicts).
/// </summary>
public sealed class TabMods : TabBase
{
	private enum Filter { All, Conflicts, HighRisk, Patchers }

	private readonly MHTextField _search = new(L10n.Text("UI.Mods.SearchHint"));
	private readonly Dictionary<Filter, MHButton> _filterButtons = new();
	private Filter _filter = Filter.All;
	private string _detailModName;

	public TabMods(Action<MHTab> navigate) : base(navigate)
	{
		SetTitle(L10n.Text("UI.Tab.Mods"));
		BuildToolbar();
		BuildList();
	}

	private void BuildToolbar()
	{
		_search.Width = new StyleDimension(260, 0f);
		_search.Left = new StyleDimension(0, 0f);
		_search.Top = new StyleDimension(2, 0f);
		_search.OnTextChanged += () => BuildList();
		AddToolbar(_search, 0f);

		float x = 270f;
		foreach (Filter filter in Enum.GetValues(typeof(Filter))) {
			var button = new MHButton(L10n.Text($"UI.Mods.Filter.{filter}"), 0.7f) {
				Width = new StyleDimension(110, 0f),
				Height = new StyleDimension(28, 0f)
			};
			var captured = filter;
			button.OnLeftClick += (_, _) => {
				_filter = captured;
				BuildToolbar();
				BuildList();
			};
			_filterButtons[filter] = button;
			AddToolbar(button, x);
			x += 116f;
		}
		RebuildToolbar();

		foreach (var kv in _filterButtons)
			kv.Value.BackgroundColor = kv.Key == _filter ? MHColors.Accent : MHColors.AccentDark;
	}

	private void BuildList()
	{
		if (_detailModName != null) {
			BuildDetail(_detailModName);
			return;
		}

		var ctx = ScanState.Context;
		if (ctx == null || ctx.Mods.Count == 0) {
			ListPanel.SetItems(new[] { new MHBodyText(L10n.Text("UI.NoScan")) });
			SetStatus("");
			return;
		}

		var query = _search.Text.Trim().ToLowerInvariant();
		var store = ScanState.Store;
		var rows = new List<UIElement>();

		foreach (var mod in ctx.Mods.OrderBy(m => m.LoadIndex)) {
			if (query.Length > 0) {
				var hay = (mod.Name + " " + mod.DisplayNameSafe + " " + mod.Author).ToLowerInvariant();
				if (!hay.Contains(query))
					continue;
			}

			var conflicts = store.GetForMod(mod.Name);
			switch (_filter) {
				case Filter.Conflicts when conflicts.Count == 0:
					continue;
				case Filter.HighRisk when conflicts.Count(c => c.Severity == Severity.High || c.Severity == Severity.Significant) == 0:
					continue;
				case Filter.Patchers when mod.PatchSignals.Count == 0:
					continue;
			}

			rows.Add(BuildRow(mod, conflicts.Count));
		}

		if (rows.Count == 0)
			rows.Add(new MHBodyText(L10n.Text("UI.Mods.NoResults")));

		ListPanel.SetItems(rows);
		SetStatus(L10n.Text("UI.Mods.Count", rows.Count.ToString(), ctx.Mods.Count.ToString()));
	}

	private UIElement BuildRow(ModFacts mod, int conflictCount)
	{
		var row = new MHClickableRow {
			Width = StyleDimension.Fill,
			Height = new StyleDimension(40, 0f)
		};

		var nameText = mod.DisplayNameSafe;
		if (mod.PatchSignals.Count > 0)
			nameText += $" [{L10n.Text("UI.Mods.PatchTag")}]";

		// Small risk dot: worst severity among this mod's conflicts.
		var modConflicts = ScanState.Store.GetForMod(mod.Name);
		var worst = modConflicts.Count > 0 ? modConflicts.Max(c => c.Severity) : (Severity?)null;
		var dotColor = worst == null ? MHColors.TextDim
			: worst == Severity.High || worst == Severity.Significant ? MHColors.Danger
			: worst == Severity.Medium ? MHColors.Medium
			: worst == Severity.Low ? MHColors.Accent
			: MHColors.Success;

		row.Append(new UIText("●", 0.9f) {
			TextColor = dotColor,
			TextOriginX = 0f,
			Left = new StyleDimension(2, 0f),
			Width = new StyleDimension(18, 0f)
		});
		row.Append(new UIText(nameText, 0.85f) {
			TextColor = MHColors.Text,
			TextOriginX = 0f,
			Left = new StyleDimension(22, 0f),
			Width = new StyleDimension(-280, 1f)
		});
		row.Append(new UIText($"v{mod.Version}", 0.7f) {
			TextColor = MHColors.TextDim,
			HAlign = 0.82f
		});
		row.Append(new UIText(L10n.Text("UI.Mods.HooksCount", mod.TotalHooks.ToString()), 0.7f) {
			TextColor = MHColors.TextDim,
			HAlign = 0.92f
		});

		var captured = mod.Name;
		row.OnLeftClick += (_, _) => {
			_detailModName = captured;
			BuildList();
		};

		return row;
	}

	private void BuildDetail(string modName)
	{
		var ctx = ScanState.Context;
		var mod = ctx?.Get(modName);
		var items = new List<UIElement>();

		var back = new MHButton(L10n.Text("UI.Mods.Back"), 0.75f) {
			Width = new StyleDimension(120, 0f),
			Height = new StyleDimension(28, 0f),
			HAlign = 0f
		};
		back.OnLeftClick += (_, _) => {
			_detailModName = null;
			BuildList();
		};
		items.Add(back);
		items.Add(Spacer(6));

		if (mod == null) {
			items.Add(new MHBodyText(L10n.Text("UI.Mods.NotFound")));
			ListPanel.SetItems(items);
			SetStatus("");
			return;
		}

		items.Add(new UIText(mod.DisplayNameSafe, 1.1f, true) { TextColor = MHColors.Text, TextOriginX = 0f });

		AddMeta(items, L10n.Text("UI.Mods.MetaInternalName"), mod.Name);
		AddMeta(items, L10n.Text("UI.Mods.MetaVersion"), mod.Version?.ToString() ?? "?");
		if (!string.IsNullOrEmpty(mod.TModLoaderVersion))
			AddMeta(items, L10n.Text("UI.Mods.MetaTMLVersion"), mod.TModLoaderVersion);
		if (!string.IsNullOrEmpty(mod.Author))
			AddMeta(items, L10n.Text("UI.Mods.MetaAuthor"), mod.Author);
		if (!string.IsNullOrEmpty(mod.Homepage))
			AddMeta(items, L10n.Text("UI.Mods.MetaHomepage"), mod.Homepage);
		AddMeta(items, L10n.Text("UI.Mods.MetaSide"), mod.Side);
		AddMeta(items, L10n.Text("UI.Mods.MetaLoadOrder"), $"#{mod.LoadIndex}");

		if (mod.Dependencies.Count > 0)
			AddMeta(items, L10n.Text("UI.Mods.MetaDependencies"), string.Join(", ", mod.Dependencies));
		if (mod.WeakDependencies.Count > 0)
			AddMeta(items, L10n.Text("UI.Mods.MetaOptional"), string.Join(", ", mod.WeakDependencies));
		if (mod.MissingOptionalDependencies.Count > 0)
			AddMeta(items, L10n.Text("UI.Mods.MetaMissingOptional"), string.Join(", ", mod.MissingOptionalDependencies), MHColors.Danger);
		foreach (var expectation in mod.VersionExpectations.Where(e => !e.IsMet)) {
			AddMeta(items, L10n.Text("UI.Mods.MetaVersionExpectation"),
				$"{expectation.ModName} >= {expectation.RequiredVersion}", MHColors.Medium);
		}
		if (mod.SortAfter.Count > 0)
			AddMeta(items, L10n.Text("UI.Mods.MetaSortAfter"), string.Join(", ", mod.SortAfter));
		if (mod.SortBefore.Count > 0)
			AddMeta(items, L10n.Text("UI.Mods.MetaSortBefore"), string.Join(", ", mod.SortBefore));
		if (mod.CodeUnavailable)
			AddMeta(items, L10n.Text("UI.Mods.MetaCodeUnavailable"), L10n.Text("UI.Mods.Yes"), MHColors.Danger);

		items.Add(Spacer(8));

		// Detected hooks by system.
		items.Add(new UIText(L10n.Text("UI.Mods.HooksTitle", mod.TotalHooks.ToString()), 0.9f, true) { TextColor = MHColors.Accent, TextOriginX = 0f });
		if (mod.HookCounts.Count == 0) {
			items.Add(new MHBodyText(L10n.Text("UI.Mods.NoHooks")));
		}
		else {
			foreach (var kv in mod.HookCounts.OrderByDescending(kv => kv.Value)) {
				var sys = SystemRegistry.Get(kv.Key);
				items.Add(new MHBodyText($"  • {SafeName(sys.NameKey)} — {kv.Value}", 0.75f));
			}
			if (ScanState.Context?.Config?.DeveloperMode == true) {
				foreach (var hook in mod.Hooks.Take(40)) {
					items.Add(new MHBodyText($"      {hook.BaseTypeName}.{hook.MethodName} ({hook.DeclaringTypeFullName})", 0.65f, MHColors.TextDim));
				}
			}
		}

		items.Add(Spacer(8));

		// Content counts.
		if (mod.ContentCounts.Count > 0) {
			items.Add(new UIText(L10n.Text("UI.Mods.ContentTitle"), 0.9f, true) { TextColor = MHColors.Accent, TextOriginX = 0f });
			foreach (var kv in mod.ContentCounts.OrderByDescending(kv => kv.Value).Take(12))
				items.Add(new MHBodyText($"  • {kv.Key}: {kv.Value}", 0.75f));
		}

		items.Add(Spacer(8));

		// Conflicts involving this mod.
		var store = ScanState.Store;
		var conflicts = store.GetForMod(modName);
		items.Add(new UIText(L10n.Text("UI.Mods.ConflictsTitle", conflicts.Count.ToString()), 0.9f, true) { TextColor = MHColors.Accent, TextOriginX = 0f });
		if (conflicts.Count == 0) {
			items.Add(new MHBodyText(L10n.Text("UI.Mods.NoConflicts")));
		}
		else {
			foreach (var c in conflicts.Take(25)) {
				var other = string.Join(" ↔ ", c.Mods.Where(m => m != modName).Select(DisplayNameOf));
				items.Add(new MHBodyText(
					$"[{L10n.Text("Severity." + c.Severity.LocalizationSuffix() + ".Name")}] {other} — {SafeName(SystemRegistry.Get(c.SystemId).NameKey)} ({L10n.Text("Confidence." + c.Confidence.LocalizationSuffix() + ".Name")})",
					0.75f, MHColors.SeverityColor(c.Severity)));
			}
		}

		ListPanel.SetItems(items);
		SetStatus(L10n.Text("UI.Mods.DetailStatus", mod.DisplayNameSafe));
	}

	private static void AddMeta(List<UIElement> items, string label, string value, Color? color = null)
	{
		// Rough wrap estimate: ~100 chars per line at 0.75 scale in the value column.
		int lines = Math.Max(1, (value.Length + 99) / 100);
		var row = new UIElement {
			Width = StyleDimension.Fill,
			Height = new StyleDimension(22 * lines, 0f)
		};
		row.Append(new UIText(label + ":", 0.75f) {
			TextColor = MHColors.TextDim,
			TextOriginX = 0f,
			Width = new StyleDimension(180, 0f)
		});
		// Wrap long values (homepages, dependency lists) so they never spill
		// off the right edge of the screen.
		row.Append(new MHBodyText(value, 0.75f, color ?? MHColors.Text) {
			Left = new StyleDimension(190, 0f),
			Width = new StyleDimension(-200, 1f)
		});
		items.Add(row);
	}

	private static string SafeName(string key)
	{
		try { return L10n.Text(key); }
		catch { return key; }
	}

	private static string DisplayNameOf(string modName)
	{
		var facts = ScanState.Context?.Get(modName);
		return facts != null ? facts.DisplayNameSafe : modName;
	}
}
