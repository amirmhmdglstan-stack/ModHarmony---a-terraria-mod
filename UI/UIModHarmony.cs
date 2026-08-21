using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using ModHarmony.Common.Utilities;
using Terraria;
using Terraria.GameContent.UI.Elements;
using Terraria.UI;

namespace ModHarmony.UI;

public enum MHTab
{
	Overview,
	Mods,
	Conflicts,
	Systems,
	Investigation,
	Arbitration,
	Reports,
	Settings
}

/// <summary>
/// Main ModHarmony screen. Header + tab bar + scrollable content area.
/// Tabs are rebuilt each time they are activated, so the screen always shows
/// fresh scan data. All text is localized (Mods.ModHarmony.UI.*).
/// </summary>
public sealed class UIModHarmony : UIState
{
	private UIPanel _mainPanel;
	private UIPanel _contentPanel;
	private readonly Dictionary<MHTab, MHButton> _tabButtons = new();
	private MHTab _activeTab = MHTab.Overview;
	private bool _escDown;
	private UIText _footerText;

	public static UIModHarmony Current { get; private set; }

	public UIModHarmony()
	{
		Current = this;
	}

	public override void OnInitialize()
	{
		_mainPanel = new UIPanel {
			HAlign = 0.5f,
			VAlign = 0.5f,
			Width = new StyleDimension(0, 0.9f),
			Height = new StyleDimension(0, 0.86f),
			BackgroundColor = MHColors.PanelBg,
			BorderColor = MHColors.PanelBorder
		};
		_mainPanel.SetPadding(10);
		Append(_mainPanel);

		BuildHeader();
		BuildTabs();
		BuildContent();
		BuildFooter();

		ShowTab(MHTab.Overview);
	}

	public void OnShown()
	{
		// Refresh whatever tab is active when the UI opens.
		ShowTab(_activeTab);
	}

	private void BuildHeader()
	{
		var header = new UIElement {
			Width = StyleDimension.Fill,
			Height = new StyleDimension(40, 0)
		};

		var title = new UIText(L10n.Text("UI.Title"), 1.25f, true) {
			TextColor = MHColors.Accent,
			Left = new StyleDimension(0, 0f),
			Top = new StyleDimension(4, 0f)
		};
		header.Append(title);

		var subtitle = new UIText($"{ModHarmony.ModName} v{Terraria.ModLoader.ModLoader.versionedName}", 0.7f) {
			TextColor = MHColors.TextDim,
			Left = new StyleDimension(4, 0f),
			Top = new StyleDimension(34, 0f)
		};
		header.Append(subtitle);

		var close = new MHButton(L10n.Text("UI.Close"), 0.85f) {
			HAlign = 1f,
			Width = new StyleDimension(30, 0f),
			Height = new StyleDimension(28, 0f)
		};
		close.OnLeftClick += (_, _) => UIHelper.Hide();
		header.Append(close);

		_mainPanel.Append(header);
	}

	private void BuildTabs()
	{
		var tabBar = new UIElement {
			Top = new StyleDimension(46, 0f),
			Width = StyleDimension.Fill,
			Height = new StyleDimension(36, 0f)
		};

		float x = 0f;
		foreach (MHTab tab in Enum.GetValues(typeof(MHTab))) {
			var button = new MHButton(L10n.Text($"UI.Tab.{tab}"), 0.75f) {
				Left = new StyleDimension(x, 0f),
				Top = new StyleDimension(2, 0f),
				Width = new StyleDimension(108, 0f),
				Height = new StyleDimension(30, 0f)
			};
			var captured = tab;
			button.OnLeftClick += (_, _) => ShowTab(captured);
			_tabButtons[tab] = button;
			tabBar.Append(button);
			x += 114f;
		}

		_mainPanel.Append(tabBar);
	}

	private void BuildContent()
	{
		_contentPanel = new UIPanel {
			Top = new StyleDimension(88, 0f),
			Width = StyleDimension.Fill,
			Height = new StyleDimension(-104, 1f),
			BackgroundColor = new Color(16, 19, 26, 220),
			BorderColor = MHColors.PanelBorder
		};
		_contentPanel.SetPadding(10);
		_mainPanel.Append(_contentPanel);
	}

	private void BuildFooter()
	{
		_footerText = new UIText("", 0.7f) {
			TextColor = MHColors.TextDim,
			HAlign = 0.5f,
			VAlign = 1f,
			Top = new StyleDimension(-4, 0f)
		};
		_mainPanel.Append(_footerText);
	}

	public void SetFooter(string text) => _footerText?.SetText(text);

	public void ShowTab(MHTab tab)
	{
		_activeTab = tab;
		foreach (var kv in _tabButtons)
			kv.Value.BackgroundColor = kv.Key == tab ? MHColors.Accent : MHColors.AccentDark;

		_contentPanel.RemoveAllChildren();

		UIElement content;
		try {
			content = TabFactory.Build(tab, NavigateTo);
		}
		catch (Exception e) {
			Log.Error($"UI tab {tab} failed to build: {e.Message}");
			content = new MHBodyText(L10n.Text("UI.TabBuildFailed", tab.ToString()));
		}

		if (content == null) {
			content = new MHBodyText(L10n.Text("UI.TabEmpty"));
		}

		content.Width = StyleDimension.Fill;
		content.Height = StyleDimension.Fill;
		_contentPanel.Append(content);

		SetFooter(L10n.Text("UI.Footer", ScanState.SessionId, ScanState.ScanRunCount.ToString()));
	}

	private static void NavigateTo(MHTab tab) => Current?.ShowTab(tab);

	public override void Update(GameTime gameTime)
	{
		base.Update(gameTime);

		bool esc = Main.keyState.IsKeyDown(Keys.Escape);
		if (esc && !_escDown) {
			UIHelper.Hide();
		}
		_escDown = esc;
	}
}
