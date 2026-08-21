using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using ModHarmony.Common.Core;
using ModHarmony.Common.Utilities;
using Terraria;
using Terraria.GameContent;
using Terraria.GameContent.UI.Elements;
using Terraria.UI;

namespace ModHarmony.UI;

/// <summary>Low-level drawing helpers using the vanilla white pixel texture.</summary>
public static class MHDraw
{
	public static void Rect(SpriteBatch sb, Rectangle rect, Color color)
	{
		var pixel = Terraria.GameContent.TextureAssets.MagicPixel.Value;
		if (pixel == null)
			return;
		sb.Draw(pixel, rect, color);
	}

	public static void Border(SpriteBatch sb, Rectangle rect, Color color, int thickness = 1)
	{
		Rect(sb, new Rectangle(rect.X, rect.Y, rect.Width, thickness), color);
		Rect(sb, new Rectangle(rect.X, rect.Y + rect.Height - thickness, rect.Width, thickness), color);
		Rect(sb, new Rectangle(rect.X, rect.Y, thickness, rect.Height), color);
		Rect(sb, new Rectangle(rect.X + rect.Width - thickness, rect.Y, thickness, rect.Height), color);
	}
}

/// <summary>ModHarmony UI palette. Text is never the only severity indicator — chips always include a label.</summary>
public static class MHColors
{
	public static readonly Color PanelBg = new(24, 28, 38, 235);
	public static readonly Color PanelBgAlt = new(31, 37, 50, 235);
	public static readonly Color PanelBorder = new(90, 105, 130, 255);
	public static readonly Color Accent = new(110, 170, 255, 255);
	public static readonly Color AccentDark = new(60, 95, 150, 255);
	public static readonly Color Text = new(225, 230, 240, 255);
	public static readonly Color TextDim = new(160, 170, 185, 255);
	public static readonly Color Danger = new(240, 90, 90, 255);
	public static readonly Color Success = new(110, 200, 130, 255);
	public static readonly Color Medium = new(240, 210, 90, 255);

	// Note: named *Color to avoid the type-name clash between the methods and
	// the Severity/Confidence enums (a method named "Severity" would shadow the
	// enum type inside this class and break switch expressions).

	public static Color SeverityColor(Severity s) => s switch {
		Severity.Info => new Color(110, 200, 130, 255),
		Severity.Low => new Color(110, 170, 255, 255),
		Severity.Medium => new Color(240, 210, 90, 255),
		Severity.Significant => new Color(250, 150, 60, 255),
		Severity.High => new Color(235, 80, 80, 255),
		_ => new Color(160, 160, 170, 255)
	};

	public static Color ConfidenceColor(Confidence c) => c switch {
		Confidence.Confirmed => new Color(130, 220, 140, 255),
		Confidence.Strong => new Color(140, 190, 255, 255),
		Confidence.Possible => new Color(235, 210, 110, 255),
		_ => new Color(170, 170, 180, 255)
	};
}

/// <summary>A labeled panel button.</summary>
public sealed class MHButton : UIPanel
{
	private readonly UIText _label;
	private readonly Color _baseColor;
	private readonly Color _hoverColor;

	public MHButton(string text, float scale = 0.85f, Color? baseColor = null)
	{
		_baseColor = baseColor ?? MHColors.AccentDark;
		_hoverColor = Color.Lerp(_baseColor, Color.White, 0.25f);
		BackgroundColor = _baseColor;
		BorderColor = MHColors.PanelBorder;
		SetPadding(0);

		_label = new UIText(text, scale) {
			HAlign = 0.5f,
			VAlign = 0.5f,
			TextColor = MHColors.Text
		};
		Append(_label);

		// OnMouseOver/OnMouseOut are events, not virtual methods.
		OnMouseOver += (_, _) => BackgroundColor = _hoverColor;
		OnMouseOut += (_, _) => BackgroundColor = _baseColor;
	}

	public void SetText(string text) => _label.SetText(text);
}

/// <summary>A colored severity chip: colored background + explicit text label.</summary>
public sealed class MHSeverityChip : UIElement
{
	public MHSeverityChip(string label, Color color, float scale = 0.75f)
	{
		Width.Set(0, 1f);
		Height.Set(24, 0);

		var text = new UIText(label, scale) {
			HAlign = 0.5f,
			VAlign = 0.5f,
			TextColor = color
		};
		Append(text);
	}

	protected override void DrawSelf(SpriteBatch spriteBatch)
	{
		var dims = GetDimensions();
		var rect = new Rectangle((int)dims.X, (int)dims.Y, (int)dims.Width, (int)dims.Height);
		MHDraw.Rect(spriteBatch, rect, MHColors.PanelBgAlt);
	}
}

/// <summary>
/// A minimal text input field for search boxes. Handles letters, digits, space
/// and common punctuation, backspace, enter and escape. Fully self-contained —
/// no reliance on vanilla text input internals.
/// </summary>
public sealed class MHTextField : UIElement
{
	public event Action OnTextChanged;

	public string Text { get; private set; } = "";

	private readonly string _hint;
	private readonly HashSet<Keys> _prevKeys = new();
	private bool _focused;
	private const int MaxLength = 48;

	public MHTextField(string hint = "")
	{
		_hint = hint;
		Width.Set(0, 1f);
		Height.Set(34, 0);

		// OnLeftClick is an event, not a virtual method.
		OnLeftClick += (_, _) => _focused = true;
	}

	public void SetText(string text)
	{
		Text = text ?? "";
		if (Text.Length > MaxLength)
			Text = Text.Substring(0, MaxLength);
		OnTextChanged?.Invoke();
	}

	public void Unfocus() => _focused = false;

	public override void Update(GameTime gameTime)
	{
		base.Update(gameTime);

		if (!_focused)
			return;

		var ks = Main.keyState;
		var pressed = ks.GetPressedKeys();
		var newPresses = pressed.Where(k => !_prevKeys.Contains(k)).ToList();

		foreach (var key in newPresses)
			HandleKey(key, ks);

		_prevKeys.Clear();
		foreach (var k in pressed)
			_prevKeys.Add(k);
	}

	private void HandleKey(Keys key, KeyboardState ks)
	{
		bool shift = ks.IsKeyDown(Keys.LeftShift) || ks.IsKeyDown(Keys.RightShift);

		if (key == Keys.Back && Text.Length > 0) {
			Text = Text.Substring(0, Text.Length - 1);
			OnTextChanged?.Invoke();
			return;
		}
		if (key == Keys.Enter || key == Keys.Escape) {
			_focused = false;
			return;
		}

		char? c = KeyToChar(key, shift);
		if (c != null) {
			if (Text.Length >= MaxLength)
				return;
			Text += c.Value;
			OnTextChanged?.Invoke();
		}
	}

	private static char? KeyToChar(Keys key, bool shift)
	{
		if (key >= Keys.A && key <= Keys.Z)
			return shift ? (char)('A' + (key - Keys.A)) : (char)('a' + (key - Keys.A));
		if (key >= Keys.D0 && key <= Keys.D9)
			return shift ? ")!@#$%^&*("[key - Keys.D0] : (char)('0' + (key - Keys.D0));
		if (key >= Keys.NumPad0 && key <= Keys.NumPad9)
			return (char)('0' + (key - Keys.NumPad0));
		switch (key) {
			case Keys.Space: return ' ';
			case Keys.OemPeriod: return shift ? '>' : '.';
			case Keys.OemComma: return shift ? '<' : ',';
			case Keys.OemMinus: return shift ? '_' : '-';
			case Keys.OemPlus: return shift ? '+' : '=';
			case Keys.OemQuestion: return shift ? '?' : '/';
			case Keys.OemOpenBrackets: return shift ? '{' : '[';
			case Keys.OemCloseBrackets: return shift ? '}' : ']';
			case Keys.OemQuotes: return shift ? '"' : '\'';
			case Keys.OemSemicolon: return shift ? ':' : ';';
			default: return null;
		}
	}

	protected override void DrawSelf(SpriteBatch spriteBatch)
	{
		var dims = GetDimensions();
		var rect = new Rectangle((int)dims.X, (int)dims.Y, (int)dims.Width, (int)dims.Height);

		MHDraw.Rect(spriteBatch, rect, _focused ? MHColors.AccentDark : MHColors.PanelBgAlt);
		var borderColor = _focused ? MHColors.Accent : MHColors.PanelBorder;
		MHDraw.Rect(spriteBatch, new Rectangle(rect.X, rect.Y, rect.Width, 1), borderColor);
		MHDraw.Rect(spriteBatch, new Rectangle(rect.X, rect.Y + rect.Height - 1, rect.Width, 1), borderColor);

		var font = FontAssets.MouseText.Value;
		var display = Text;
		if (display.Length == 0 && !_focused)
			display = _hint;

		var color = Text.Length == 0 && !_focused ? MHColors.TextDim : MHColors.Text;
		var pos = new Vector2(dims.X + 8, dims.Y + (dims.Height - font.MeasureString(display).Y) / 2f);
		Utils.DrawBorderString(spriteBatch, display, pos, color);

		if (_focused) {
			// Simple caret.
			var caretX = dims.X + 8 + font.MeasureString(Text).X;
			MHDraw.Rect(spriteBatch, new Rectangle((int)caretX, (int)(dims.Y + 6), 2, (int)(dims.Height - 12)), MHColors.Accent);
		}
	}
}

/// <summary>A scrollable vertical list (UIList + UIScrollbar) inside a bordered panel.</summary>
public sealed class MHScrollPanel : UIPanel
{
	private readonly UIList _list;
	private readonly UIScrollbar _scrollbar;

	public float ListPadding { get => _list.ListPadding; set => _list.ListPadding = value; }

	public MHScrollPanel()
	{
		BackgroundColor = MHColors.PanelBg;
		BorderColor = MHColors.PanelBorder;
		PaddingTop = 8;
		PaddingBottom = 8;
		PaddingLeft = 8;
		PaddingRight = 8;

		_list = new UIList {
			Width = StyleDimension.Fill,
			Height = StyleDimension.Fill,
			ListPadding = 6
		};

		_scrollbar = new UIScrollbar {
			Height = StyleDimension.Fill,
			HAlign = 1f
		};
		_scrollbar.SetView(100f, 1000f);

		_list.SetScrollbar(_scrollbar);
		Append(_list);
		Append(_scrollbar);
	}

	public void SetItems(IEnumerable<UIElement> items)
	{
		_list.Clear();
		foreach (var item in items)
			_list.Add(item);
		_scrollbar.ViewPosition = 0f;
	}

	public void ScrollToTop() => _scrollbar.ViewPosition = 0f;
}

/// <summary>A clickable row panel with a subtle hover highlight.</summary>
public sealed class MHClickableRow : UIPanel
{
	private readonly Color _hoverColor = new(MHColors.AccentDark.R, MHColors.AccentDark.G, MHColors.AccentDark.B, 120);

	public MHClickableRow()
	{
		BackgroundColor = Color.Transparent;
		BorderColor = Color.Transparent;
		SetPadding(0);
	}

	public override void Update(GameTime gameTime)
	{
		base.Update(gameTime);
		BackgroundColor = IsMouseHovering ? _hoverColor : Color.Transparent;
	}
}

/// <summary>Wrapped, dim label used for evidence/body text.</summary>
public sealed class MHBodyText : UIText
{
	public MHBodyText(string text, float scale = 0.75f, Color? color = null) : base(text, scale)
	{
		Width = StyleDimension.Fill;
		IsWrapped = true;
		MinWidth = StyleDimension.Empty;
		TextOriginX = 0f;
		TextColor = color ?? MHColors.TextDim;
	}

	public void Set(string text) => SetText(text);
}

/// <summary>Helper to create a plain labeled line inside a list.</summary>
public static class MHRow
{
	public static UIElement Label(string text, float scale = 0.85f, Color? color = null)
	{
		return new UIText(text, scale) {
			TextColor = color ?? MHColors.Text,
			TextOriginX = 0f,
			Width = StyleDimension.Fill
		};
	}
}
