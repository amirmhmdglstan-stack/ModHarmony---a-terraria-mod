using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.UI;

namespace ModHarmony.UI;

/// <summary>
/// Client-side singleton managing the ModHarmony in-game UI state.
/// </summary>
public static class UIHelper
{
	private static UserInterface _userInterface;
	private static UIModHarmony _state;

	public static bool IsOpen => _userInterface?.CurrentState != null;

	public static void EnsureInitialized()
	{
		if (_userInterface == null) {
			_userInterface = new UserInterface();
			_state = new UIModHarmony();
			_state.Activate();
		}
	}

	public static void Show()
	{
		EnsureInitialized();
		_userInterface.SetState(_state);
		_state.OnShown();
		Main.MouseText = "";
	}

	public static void Hide()
	{
		_userInterface?.SetState(null);
	}

	public static void Toggle()
	{
		if (IsOpen)
			Hide();
		else
			Show();
	}

	public static void Update(GameTime gameTime)
	{
		if (IsOpen)
			_userInterface?.Update(gameTime);
	}

	public static void Draw(SpriteBatch spriteBatch, GameTime gameTime)
	{
		if (IsOpen)
			_userInterface?.Draw(spriteBatch, gameTime);
	}
}
