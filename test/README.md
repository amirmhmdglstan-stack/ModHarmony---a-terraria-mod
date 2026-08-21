# ModHarmony test fixtures

These are **development-only** fixture mods used to exercise ModHarmony's
detectors. They are excluded from the ModHarmony build (`test\**` is removed
from the main project and listed in `buildIgnore`), so nothing here ships in
the release `.tmod`.

## What each fixture simulates

| Fixture | Detector targets |
|---|---|
| `FixtureCombatOverhaul` | hook overlap (GlobalNPC.AI, damage hooks, spawn), ModPlayer overlap, recipe overlap, world gen overlap |
| `FixtureWorldTweaks` | hook overlap with CombatOverhaul, ModSystem overlap (world gen, recipes, UI layers), tile wire overlap |
| `FixtureRecipeModder` | recipe overlap (same results as the other fixtures), recipe groups, IL/runtime-patch signal (namespace `IL.Terraria`) |
| `FixtureDependencyBase` | dependency base (no conflicts by itself) |
| `FixtureDependencyUser` | dependency on Base; declares `weakReferences = FixtureOptionalFriend` (not installed → missing-optional-dependency informational conflict) |

## How to test

1. Copy each fixture folder into `Mod Sources/` (next to `tModLoader.targets`).
2. Reload the mod list and enable all fixtures together with ModHarmony.
3. Open ModHarmony (default hotkey `N`) and check:
   - **Conflicts tab**: pairs like `FixtureCombatOverhaul ↔ FixtureWorldTweaks`
     on `npc.ai` (both override `GlobalNPC.AI`), `world.gen` (both override
     `ModifyWorldGenTasks`), `recipe.add` (both craft Copper Bar);
   - **Mods tab**: each fixture shows its detected hooks/content; `FixtureDependencyUser`
     lists the missing optional dependency;
   - **Systems tab**: `npc.ai`, `world.gen`, `recipe.add` show multiple mods;
   - **Arbitration tab**: `npc.spawn` appears as resolvable (FixtureCombatOverhaul
     registers a spawn factor via Mod.Call — see its Mod class);
   - **Reports tab**: export the full report and verify the sections.

## Manual test checklist

- [ ] ModHarmony loads with **zero** other mods (no errors in client.log)
- [ ] Small pack (ModHarmony + 2 fixtures) — conflicts appear, UI is fast
- [ ] Recipe overlap detected (Copper Bar crafted by 3 mods)
- [ ] IL signal detected for FixtureRecipeModder
- [ ] Missing optional dependency detected for FixtureDependencyUser
- [ ] Arbitration: enable it in config, pick Random for `npc.spawn`, regenerate
      the seed a few times, verify the winner changes and stays stable between
      sessions (check `{SavePath}/ModHarmony/arbitration.json`)
- [ ] Safe Diagnostics Mode: with it on, arbitration winners are never applied
- [ ] Export report → file appears in `{SavePath}/ModHarmony/reports/`
- [ ] Change the mod list, reload, open Overview → "What changed" shows the diff
- [ ] Enable Investigation Mode, kill some enemies, then open the Investigation
      tab — events list and performance summary render

## Automated tests

ModHarmony's scan logic is pure C# over plain data structures; the test
harness in this repository can be run on a machine with a .NET SDK:

```
dotnet run --project test/TestHarness
```

It feeds synthetic `ModFacts`/`DetectorContext` data into each detector and
asserts on the resulting conflicts (severity, confidence, evidence). See
`TestHarness/Program.cs`.
