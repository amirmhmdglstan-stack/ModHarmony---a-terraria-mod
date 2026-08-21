using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using ModHarmony.Common.Arbitration;
using ModHarmony.Common.Core;
using ModHarmony.Common.Utilities;
using Terraria.GameContent.UI.Elements;
using Terraria.UI;

namespace ModHarmony.UI;

/// <summary>
/// Systems tab: the registry of game systems, how many mods touch each one,
/// and (on click) which mods and conflicts are associated with it.
/// </summary>
public sealed class TabSystems : TabBase
{
	private string _detailSystem;

	public TabSystems(Action<MHTab> navigate) : base(navigate)
	{
		SetTitle(L10n.Text("UI.Tab.Systems"));
		BuildList();
	}

	private void BuildList()
	{
		if (_detailSystem != null) {
			BuildDetail(_detailSystem);
			return;
		}

		var ctx = ScanState.Context;
		if (ctx == null) {
			ListPanel.SetItems(new[] { new MHBodyText(L10n.Text("UI.NoScan")) });
			SetStatus("");
			return;
		}

		var items = new List<UIElement>();
		items.Add(new MHBodyText(L10n.Text("UI.Systems.Explanation")));
		items.Add(Spacer(4));

		foreach (var system in SystemRegistry.All.OrderByDescending(s => ctx.SystemOverlapCounts.TryGetValue(s.Id, out var n) ? n : 0).ThenBy(s => s.Id)) {
			ctx.SystemOverlapCounts.TryGetValue(system.Id, out var count);
			if (count == 0)
				continue;
			items.Add(BuildRow(system, count));
		}

		ListPanel.SetItems(items);
		SetStatus(L10n.Text("UI.Systems.Count", ctx.SystemOverlapCounts.Count.ToString()));
	}

	private UIElement BuildRow(GameSystem system, int count)
	{
		var row = new MHClickableRow {
			Width = StyleDimension.Fill,
			Height = new StyleDimension(34, 0f)
		};
		row.Append(new UIText(SafeName(system.NameKey), 0.8f) {
			TextColor = MHColors.Text,
			TextOriginX = 0f,
			Left = new StyleDimension(4, 0f),
			Width = new StyleDimension(-160, 1f)
		});
		row.Append(new UIText(L10n.Text("UI.Systems.ModsCount", count.ToString()), 0.7f) {
			TextColor = count >= 5 ? MHColors.Medium : MHColors.TextDim,
			HAlign = 1f
		});

		var captured = system.Id;
		row.OnLeftClick += (_, _) => {
			_detailSystem = captured;
			BuildList();
		};
		return row;
	}

	private void BuildDetail(string systemId)
	{
		var ctx = ScanState.Context;
		var system = SystemRegistry.Get(systemId);
		var items = new List<UIElement>();

		var back = new MHButton(L10n.Text("UI.Mods.Back"), 0.75f) {
			Width = new StyleDimension(120, 0f),
			Height = new StyleDimension(28, 0f),
			HAlign = 0f
		};
		back.OnLeftClick += (_, _) => {
			_detailSystem = null;
			BuildList();
		};
		items.Add(back);
		items.Add(Spacer(6));

		items.Add(new UIText(SafeName(system.NameKey), 1.05f, true) { TextColor = MHColors.Text, TextOriginX = 0f });
		items.Add(new MHBodyText(SafeDesc(system.DescriptionKey)));
		items.Add(Spacer(6));

		// Mods touching this system.
		ctx.SystemOverlapCounts.TryGetValue(systemId, out var modCount);
		items.Add(new UIText(L10n.Text("UI.Systems.ModsTitle", modCount.ToString()), 0.9f, true) { TextColor = MHColors.Accent, TextOriginX = 0f });

		var touching = ctx.ExceptSelf()
			.Where(m => m.HookCounts.ContainsKey(systemId))
			.OrderByDescending(m => m.HookCounts[systemId])
			.ToList();

		if (touching.Count == 0) {
			items.Add(new MHBodyText(L10n.Text("UI.Systems.NoMods")));
		}
		else {
			foreach (var mod in touching) {
				var hooks = string.Join(", ", mod.Hooks.Where(h => h.SystemId == systemId).Select(h => h.MethodName).Distinct().Take(6));
				items.Add(new MHBodyText($"  • {mod.DisplayNameSafe} — {mod.HookCounts[systemId]} ({hooks}{(mod.Hooks.Count(h => h.SystemId == systemId) > 6 ? "…" : "")})", 0.75f));
			}
		}

		items.Add(Spacer(8));

		// Conflicts on this system.
		var conflicts = ScanState.Store.GetForSystem(systemId);
		items.Add(new UIText(L10n.Text("UI.Systems.ConflictsTitle", conflicts.Count.ToString()), 0.9f, true) { TextColor = MHColors.Accent, TextOriginX = 0f });
		if (conflicts.Count == 0) {
			items.Add(new MHBodyText(L10n.Text("UI.Systems.NoConflicts")));
		}
		else {
			foreach (var c in conflicts.Take(15)) {
				items.Add(new MHBodyText(
					$"[{L10n.Text("Severity." + c.Severity.LocalizationSuffix() + ".Name")}] {string.Join(" ↔ ", c.Mods.Select(DisplayNameOf))}",
					0.75f, MHColors.SeverityColor(c.Severity)));
			}
		}

		items.Add(Spacer(8));

		// Arbitration availability.
		items.Add(new UIText(L10n.Text("UI.Systems.ArbitrationTitle"), 0.9f, true) { TextColor = MHColors.Accent, TextOriginX = 0f });
		if (ArbitrationPoints.HasPoint(systemId)) {
			var point = ArbitrationPoints.Find(systemId);
			items.Add(new MHBodyText(L10n.Text("UI.Systems.ArbitrationAvailable")));
			items.Add(new MHBodyText(SafeName(point.NameKey)));
			items.Add(new MHBodyText(SafeDesc(point.DescriptionKey)));
		}
		else {
			items.Add(new MHBodyText(L10n.Text("UI.Systems.ArbitrationUnavailable")));
		}

		ListPanel.SetItems(items);
		SetStatus(systemId);
	}

	private static string SafeName(string key)
	{
		try { return L10n.Text(key); }
		catch { return key; }
	}

	private static string SafeDesc(string key)
	{
		try { return L10n.Text(key); }
		catch { return ""; }
	}

	private static string DisplayNameOf(string modName)
	{
		var facts = ScanState.Context?.Get(modName);
		return facts != null ? facts.DisplayNameSafe : modName;
	}
}
