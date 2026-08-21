# Testing

## Automated harness (logic tests)

`test/TestHarness` compiles the pure data-processing parts of ModHarmony
(Core, Detection, ArbitrationManager — no UI, no game I/O) and runs assertions
against synthetic modpacks. It uses the real detector code, so it tests the
actual implementation, not a re-implementation.

Run on a machine with a .NET 8 SDK and tModLoader installed:

```
# from the repo root, with a tModLoader.targets available (copy the one from
# Mod Sources next to the harness, or pass -p:tMLPath=...):
dotnet run --project test/TestHarness
```

Covered cases:

- empty modpack → no conflicts
- hook overlap (pairwise, Medium/Strong)
- severity escalation on 3+ shared hooks
- aggregation for >8 contestants (no N×N explosion)
- global class overlap (informational)
- ModPlayer overlap, ModSystem overlap (world gen)
- recipe overlap (Low/Confirmed) and shared recipe groups (Info)
- dependency cycles (+load-order warning) and missing optional deps
- asset duplicates
- health score deduction + breakdown
- stable conflict ids (order-independent, distinct)
- arbitration: seeded-random determinism, manual priority, weight validation
  (including distribution over 2000 seeded rolls), winner stability

## Manual fixture mods

`test/fixtures/` contains five small mods that intentionally create the
conflicts ModHarmony detects (see `test/README.md` for the full checklist):

- **FixtureCombatOverhaul** — GlobalNPC AI/damage/spawn hooks, ModPlayer
  damage, recipe + world gen; registers an `npc.spawn` arbitrable value.
- **FixtureWorldTweaks** — overlaps CombatOverhaul on `npc.ai`, `world.gen`,
  `recipe.add`; UI layers + tile wires.
- **FixtureRecipeModder** — more recipe overlap, a shared recipe group, an
  `IL.Terraria` class (runtime-patch signal).
- **FixtureDependencyBase** + **FixtureDependencyUser** — dependency edge and
  a missing optional dependency.

## In-game test plan (manual)

1. Load with **zero** other mods → clean load, `[ModHarmony]` log lines, no
   errors; UI opens with `N`.
2. Small pack (ModHarmony + 2–3 fixtures) → conflicts appear instantly; UI
   stays smooth.
3. Recipe overlap visible (Copper Bar from 3 mods); IL signal on
   FixtureRecipeModder; missing optional dependency on FixtureDependencyUser.
4. Arbitration: enable in config → `npc.spawn` resolvable; pick Random,
   Regenerate several times (winner changes, then stays); restart the game →
   winner persists (`arbitration.json`); enable Safe Diagnostics Mode → winner
   no longer applied (spawn rate unchanged).
5. Investigation Mode: enable, play, open Investigation tab → events + frame
   summary render; Analyze → report preview; Export → file exists.
6. Reports: export full report; verify all sections; copy community summary.
7. "What changed": modify the fixture set, reload → Overview shows the diff.
8. Large pack sanity: with 50+ mods the Conflicts tab aggregates big systems;
   search/filter respond instantly.
