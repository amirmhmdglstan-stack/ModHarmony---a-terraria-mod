using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using ModHarmony.Common.Arbitration;
using ModHarmony.Common.Core;
using ModHarmony.Common.Utilities;
using ModHarmony.Content.Config;
using Terraria.GameContent.UI.Elements;
using Terraria.ModLoader;
using Terraria.UI;

namespace ModHarmony.UI;

/// <summary>
/// Arbitration tab: per-system arbitration groups with strategy selection,
/// deterministic seeded random/weighted controls, manual priority ordering and
/// lock/regenerate actions. Systems without a technically safe mechanism are
/// listed as detection-only.
/// </summary>
public sealed class TabArbitration : TabBase
{
	public TabArbitration(Action<MHTab> navigate) : base(navigate)
	{
		SetTitle(L10n.Text("UI.Tab.Arbitration"));
		Build();
	}

	private void Build()
	{
		var config = ScanState.Context?.Config ?? ModContent.GetInstance<ModHarmonyConfig>();
		var items = new List<UIElement>();

		bool enabled = config?.ArbitrationActive ?? false;
		items.Add(new UIText(L10n.Text(enabled ? "UI.Arbitration.EnabledTitle" : "UI.Arbitration.DisabledTitle"), 0.95f, true) {
			TextColor = enabled ? MHColors.Success : MHColors.Medium,
			TextOriginX = 0f
		});
		items.Add(new MHBodyText(L10n.Text("UI.Arbitration.Explanation")));
		items.Add(new MHBodyText(L10n.Text("UI.Arbitration.SafetyNote")));
		items.Add(Spacer(10));

		var groups = ArbitrationState.Groups.OrderBy(g => !g.MechanismAvailable).ThenBy(g => g.GroupId).ToList();

		if (groups.Count == 0) {
			items.Add(new MHBodyText(L10n.Text("UI.Arbitration.NoGroups")));
		}

		// --- Resolvable groups ---------------------------------------------
		var resolvable = groups.Where(g => g.MechanismAvailable).ToList();
		if (resolvable.Count > 0) {
			items.Add(new UIText(L10n.Text("UI.Arbitration.ResolvableTitle"), 0.9f, true) { TextColor = MHColors.Accent, TextOriginX = 0f });
			foreach (var group in resolvable)
				BuildGroupCard(items, group, config);
		}

		// --- Detection-only groups -----------------------------------------
		var detectionOnly = groups.Where(g => !g.MechanismAvailable).ToList();
		if (detectionOnly.Count > 0) {
			items.Add(new UIText(L10n.Text("UI.Arbitration.DetectionOnlyTitle", detectionOnly.Count.ToString()), 0.9f, true) { TextColor = MHColors.TextDim, TextOriginX = 0f });
			items.Add(new MHBodyText(L10n.Text("UI.Arbitration.DetectionOnlyNote")));
			foreach (var group in detectionOnly) {
				var card = new UIPanel {
					Width = StyleDimension.Fill,
					BackgroundColor = new Color(22, 25, 33, 230),
					BorderColor = MHColors.PanelBorder
				};
				card.SetPadding(6);
				card.Append(new UIText($"{SafeSystemName(group.SystemId)} — {L10n.Text("UI.Arbitration.Unavailable")}", 0.75f) {
					TextColor = MHColors.TextDim,
					TextOriginX = 0f
				});
				items.Add(card);
				items.Add(Spacer(4));
			}
		}

		List.SetItems(items);
		SetStatus(L10n.Text("UI.Arbitration.Status", groups.Count.ToString()));
	}

	private void BuildGroupCard(List<UIElement> items, ArbitrationGroup group, ModHarmonyConfig config)
	{
		var card = new UIPanel {
			Width = StyleDimension.Fill,
			BackgroundColor = new Color(22, 28, 38, 235),
			BorderColor = group.Locked ? MHColors.Accent : MHColors.PanelBorder
		};
		card.SetPadding(8);

		card.Append(new UIText(SafeSystemName(group.SystemId), 0.9f, true) {
			TextColor = MHColors.Text,
			TextOriginX = 0f
		});

		var point = ArbitrationPoints.Find(group.SystemId);
		if (point != null) {
			card.Append(new MHBodyText(SafeName(point.DescriptionKey)));
		}

		// Strategy selector (cycle through strategies).
		var strategies = new[] {
			ArbitrationStrategy.Disabled, ArbitrationStrategy.ManualPriority, ArbitrationStrategy.LoadOrder,
			ArbitrationStrategy.Random, ArbitrationStrategy.WeightedRandom,
			ArbitrationStrategy.FirstRegistered, ArbitrationStrategy.LastRegistered
		};

		var strategyButton = new MHButton(L10n.Text("UI.Arbitration.Strategy", L10n.Text("Arbitration.Strategy." + group.Strategy.LocalizationSuffix() + ".Name")), 0.75f) {
			Width = new StyleDimension(360, 0f),
			Height = new StyleDimension(28, 0f),
			Top = new StyleDimension(4, 0f),
			HAlign = 0f
		};
		strategyButton.OnLeftClick += (_, _) => {
			int idx = Array.IndexOf(strategies, group.Strategy);
			group.Strategy = strategies[(idx + 1) % strategies.Length];
			SaveAndRebuild(group, config);
		};
		card.Append(strategyButton);

		// Winner line.
		var winner = group.CanResolve && !string.IsNullOrEmpty(group.ResolvedWinner)
			? L10n.Text("UI.Arbitration.Winner", DisplayNameOf(group.ResolvedWinner))
			: L10n.Text("UI.Arbitration.NoWinner");
		card.Append(new UIText(winner, 0.8f) {
			TextColor = group.CanResolve ? MHColors.Success : MHColors.TextDim,
			Top = new StyleDimension(38, 0f),
			TextOriginX = 0f
		});

		// Candidates.
		float y = 64f;
		foreach (var candidate in group.Candidates) {
			var row = new UIElement {
				Top = new StyleDimension(y, 0f),
				Width = StyleDimension.Fill,
				Height = new StyleDimension(24, 0f)
			};
			row.Append(new UIText(DisplayNameOf(candidate.ModName), 0.75f) {
				TextColor = MHColors.Text,
				TextOriginX = 0f,
				Left = new StyleDimension(6, 0f),
				Width = new StyleDimension(-240, 1f)
			});

			switch (group.Strategy) {
				case ArbitrationStrategy.ManualPriority:
					var up = MakeMiniButton("^", 0.7f, 0f);
					var down = MakeMiniButton("v", 0.7f, 28f);
					up.OnLeftClick += (_, _) => {
						candidate.ManualPriority++;
						SaveAndRebuild(group, config);
					};
					down.OnLeftClick += (_, _) => {
						candidate.ManualPriority--;
						SaveAndRebuild(group, config);
					};
					row.Append(up);
					row.Append(down);
					row.Append(new UIText(L10n.Text("UI.Arbitration.Priority", candidate.ManualPriority.ToString()), 0.7f) {
						TextColor = MHColors.TextDim,
						Left = new StyleDimension(56, 0f),
						HAlign = 0.95f
					});
					break;

				case ArbitrationStrategy.WeightedRandom:
					var minus = MakeMiniButton("-", 0.7f, 0f);
					var plus = MakeMiniButton("+", 0.7f, 28f);
					minus.OnLeftClick += (_, _) => {
						candidate.Weight = Math.Max(0f, candidate.Weight - 5f);
						SaveAndRebuild(group, config);
					};
					plus.OnLeftClick += (_, _) => {
						candidate.Weight += 5f;
						SaveAndRebuild(group, config);
					};
					row.Append(minus);
					row.Append(plus);
					row.Append(new UIText(L10n.Text("UI.Arbitration.Weight", candidate.Weight.ToString("0")), 0.7f) {
						TextColor = MHColors.TextDim,
						Left = new StyleDimension(56, 0f),
						HAlign = 0.95f
					});
					break;
			}

			if (!string.IsNullOrEmpty(candidate.RegisteredValue)) {
				row.Append(new UIText(candidate.RegisteredValue, 0.65f) {
					TextColor = MHColors.TextDim,
					Left = new StyleDimension(6, 0f),
					Top = new StyleDimension(14, 0f)
				});
			}

			card.Append(row);
			y += 26f;
		}

		// Seed / regenerate / lock controls.
		var controlsY = y + 4f;
		if (group.Strategy == ArbitrationStrategy.Random || group.Strategy == ArbitrationStrategy.WeightedRandom) {
			var seedText = new UIText(L10n.Text("UI.Arbitration.Seed", group.Seed >= 0 ? group.Seed.ToString() : L10n.Text("UI.Arbitration.SeedAuto")), 0.7f) {
				TextColor = MHColors.TextDim,
				Top = new StyleDimension(controlsY, 0f),
				TextOriginX = 0f
			};
			card.Append(seedText);

			var regenerate = new MHButton(L10n.Text("UI.Arbitration.Regenerate"), 0.7f) {
				Width = new StyleDimension(160, 0f),
				Height = new StyleDimension(26, 0f),
				Top = new StyleDimension(controlsY, 0f),
				Left = new StyleDimension(150, 0f)
			};
			regenerate.OnLeftClick += (_, _) => {
				ArbitrationManager.Regenerate(group, config);
				SaveAndRebuild(group, config);
			};
			card.Append(regenerate);

			var lockButton = new MHButton(L10n.Text(group.Locked ? "UI.Arbitration.Unlock" : "UI.Arbitration.Lock"), 0.7f) {
				Width = new StyleDimension(140, 0f),
				Height = new StyleDimension(26, 0f),
				Top = new StyleDimension(controlsY, 0f),
				Left = new StyleDimension(320, 0f)
			};
			lockButton.OnLeftClick += (_, _) => {
				group.Locked = !group.Locked;
				if (group.Locked && string.IsNullOrEmpty(group.ResolvedWinner))
					ArbitrationManager.Resolve(group, config);
				SaveAndRebuild(group, config);
			};
			card.Append(lockButton);

			card.Append(new MHBodyText(L10n.Text("UI.Arbitration.StabilityNote")) { Top = new StyleDimension(controlsY + 26, 0f) });
			y = controlsY + 52f;
		}
		else {
			card.Append(new MHBodyText(L10n.Text("UI.Arbitration.StabilityNote")) { Top = new StyleDimension(controlsY, 0f) });
			y = controlsY + 24f;
		}

		card.Height = new StyleDimension(y + 8, 0f);
		items.Add(card);
		items.Add(Spacer(8));
	}

	private static MHButton MakeMiniButton(string text, float scale, float left)
	{
		return new MHButton(text, scale) {
			Width = new StyleDimension(24, 0f),
			Height = new StyleDimension(22, 0f),
			Left = new StyleDimension(left, 0f)
		};
	}

	private void SaveAndRebuild(ArbitrationGroup group, ModHarmonyConfig config)
	{
		ArbitrationManager.Resolve(group, config);
		ArbitrationState.RebuildIndex();
		if (config?.PersistDecisions ?? true)
			ArbitrationStore.Save(ArbitrationState.Groups);
		Build();
	}

	private static string SafeName(string key)
	{
		try { return L10n.Text(key); }
		catch { return key; }
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
