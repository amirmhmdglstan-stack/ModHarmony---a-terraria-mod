using System.Collections.Generic;
using Microsoft.Xna.Framework;
using ModHarmony.UI;
using Terraria;
using Terraria.ModLoader;
using Terraria.UI;

namespace ModHarmony.Systems;

/// <summary>
/// Client-only ModSystem that owns the ModHarmony UI lifecycle: creates the
/// state, updates and draws it, and registers the interface layer.
/// </summary>
[Autoload(Side = ModSide.Client)]
public sealed class ModHarmonyUISystem : ModSystem
{
	public override void PostSetupContent()
	{
		UIHelper.EnsureInitialized();
	}

	public override void UpdateUI(GameTime gameTime)
	{
		UIHelper.Update(gameTime);
	}

	public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers)
	{
		int mouseTextIndex = layers.FindIndex(layer => layer.Name.Equals("Vanilla: Mouse Text"));
		if (mouseTextIndex == -1)
			mouseTextIndex = layers.Count;

		layers.Insert(mouseTextIndex, new LegacyGameInterfaceLayer(
			"ModHarmony: Main UI",
			delegate {
				UIHelper.Draw(Main.spriteBatch, new GameTime());
				return true;
			},
			InterfaceScaleType.UI)
		);
	}
}
