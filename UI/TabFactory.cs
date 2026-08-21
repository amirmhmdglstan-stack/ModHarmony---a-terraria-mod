using System;
using Terraria.UI;

namespace ModHarmony.UI;

/// <summary>Builds the content panel for a tab. Rebuilt on every activation.</summary>
public static class TabFactory
{
	public static UIElement Build(MHTab tab, Action<MHTab> navigate) => tab switch {
		MHTab.Overview => new TabOverview(navigate),
		MHTab.Mods => new TabMods(navigate),
		MHTab.Conflicts => new TabConflicts(navigate),
		MHTab.Systems => new TabSystems(navigate),
		MHTab.Investigation => new TabInvestigation(navigate),
		MHTab.Arbitration => new TabArbitration(navigate),
		MHTab.Reports => new TabReports(navigate),
		MHTab.Settings => new TabSettings(navigate),
		_ => new UIElement()
	};
}
