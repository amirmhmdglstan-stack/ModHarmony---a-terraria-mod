using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using ModHarmony.Common.Utilities;
using Terraria.GameContent.UI.Elements;
using Terraria.UI;

namespace ModHarmony.UI;

/// <summary>
/// Shared scaffold for tabs: title, optional toolbar row, scrollable main area,
/// status line at the bottom.
/// </summary>
public abstract class TabBase : UIElement
{
	protected readonly Action<MHTab> Navigate;

	protected UIText TitleText;
	protected UIElement Toolbar;
	protected MHScrollPanel List;
	protected UIText StatusText;

	private readonly List<UIElement> _toolbarChildren = new();
	private bool _toolbarDirty;

	protected TabBase(Action<MHTab> navigate)
	{
		Navigate = navigate;
		Width = StyleDimension.Fill;
		Height = StyleDimension.Fill;

		TitleText = new UIText("", 1.05f, true) {
			TextColor = MHColors.Text,
			TextOriginX = 0f,
			Top = new StyleDimension(0, 0f)
		};
		Append(TitleText);

		Toolbar = new UIElement {
			Top = new StyleDimension(34, 0f),
			Width = StyleDimension.Fill,
			Height = new StyleDimension(34, 0f)
		};
		Append(Toolbar);

		List = new MHScrollPanel {
			Top = new StyleDimension(72, 0f),
			Width = StyleDimension.Fill,
			Height = new StyleDimension(-100, 1f)
		};
		Append(List);

		StatusText = new UIText("", 0.7f) {
			TextColor = MHColors.TextDim,
			VAlign = 1f,
			TextOriginX = 0f,
			Width = StyleDimension.Fill
		};
		Append(StatusText);
	}

	protected void SetTitle(string text) => TitleText.SetText(text);

	protected void SetStatus(string text) => StatusText.SetText(text);

	/// <summary>Adds a toolbar child at a fixed X position; call RebuildToolbar() when done.</summary>
	protected void AddToolbar(UIElement element, float x)
	{
		element.Left = new StyleDimension(x, 0f);
		element.Top = new StyleDimension(2, 0f);
		_toolbarChildren.Add(element);
		_toolbarDirty = true;
	}

	protected void RebuildToolbar()
	{
		if (!_toolbarDirty)
			return;
		_toolbarDirty = false;
		Toolbar.RemoveAllChildren();
		foreach (var child in _toolbarChildren)
			Toolbar.Append(child);
		_toolbarChildren.Clear();
	}

	protected static UIElement Spacer(float height) => new UIElement {
		Width = StyleDimension.Fill,
		Height = new StyleDimension(height, 0f)
	};
}
