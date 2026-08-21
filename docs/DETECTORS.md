# Detectors

All detectors implement `IConflictDetector` and run inside the
`DetectorManager`, isolated from each other. Each returns a list of
`Conflict`s, and each conflict carries:

- `Id` — stable, derived from detector + system + involved mods;
- `SystemId` — a registered game system (see `SystemRegistry`);
- `Severity` + `Confidence` — always read together;
- `Mods` — the involved mods (internal names);
- `Evidence` — per-mod, localized, with developer detail;
- `ArbitrationGroupId` — set when the system has an arbitration group.

## Detector reference

| Detector | What it detects | How | Confidence | Notes |
|---|---|---|---|---|
| `HookOverlap` | Mods overriding hooks in the same system (Global* classes) | Reflection over overridden virtual methods | Strong | Pairwise ≤ 8 mods, aggregated above that. Severity by system; escalates on 3+ shared hooks |
| `GlobalClassOverlap` | Mods registering the same Global* base class | Content registry + reflection | Confirmed | Informational |
| `ModPlayerOverlap` | Overlapping ModPlayer hooks | Same hook analysis on ModPlayer | Strong | |
| `ModSystemOverlap` | Overlapping ModSystem hooks | Same hook analysis on ModSystem | Strong | World gen/recipe modify weigh more |
| `RecipeOverlap` | Multiple mods crafting the same item; shared recipe groups | `Main.recipe` + `Recipe.Mod` at load; `RecipeGroup.recipeGroups` | Confirmed | Alternate recipes are common → Low |
| `Dependency` | Dependency cycles; missing optional deps | `AssemblyManager.GetDependencies` + `.tmod` metadata | Confirmed | Missing hard deps / unmet versions can't exist among loaded mods (tModLoader refuses to load them) |
| `ILHooks` | Runtime patching signals | `IL.`/`On.` namespaces, `IL_Terraria_*`/`On_Terraria_*` methods, MonoMod/Cecil assembly refs | Strong (convention) / Possible (reference) | Cannot know which methods two mods patch — stated in evidence |
| `Asset` | Duplicate `.tmod` files with the same internal name | `ModPath` scan + Info parsing | Confirmed | In-content asset collisions are prevented by tModLoader namespacing |

## What is deliberately NOT detected

- Which specific NPCs/items/projectiles a mod affects (not exposed by hooks).
- Whether two IL patches touch the same method.
- "Incompatibility" verdicts of any kind.
- Behavior of mods that are not loaded (they are listed in metadata when their
  `.tmod` is readable, but not scanned).

## How to add a detector

1. Create `Common/Detection/MyDetector.cs`:

```csharp
public sealed class MyDetector : IConflictDetector
{
    public string Id => "MyDetector";
    public string NameKey => "Detectors.MyDetector.Name";
    public string DescriptionKey => "Detectors.MyDetector.Description";
    public bool IsEnabled(ModHarmonyConfig config) => config.ScanMyThing;
    public List<Conflict> Detect(DetectorContext context) { /* ... */ }
}
```

2. Register it in `DetectorManager`'s constructor.
3. Add a `ScanMyThing` bool to `ModHarmonyConfig` (and its label/tooltip keys).
4. Add localization: `Detectors.MyDetector.Name/Description`, `Evidence.*`,
   `UI.Conflicts.Why.MyDetector`, `Systems.*` if you register a new system.
5. Add a test in `test/TestHarness/Program.cs`; optionally a fixture mod in
   `test/fixtures/`.
6. If the detector maps hooks to systems, extend `HookCatalog`.

Use `Conflict.MakeStableId(Id, systemId, mods)` for conflict ids, add evidence
via `new Evidence(kind, modName, "Evidence.Key", args...)`, and set
`ArbitrationGroupId = $"system.{systemId}"` when the conflict lives on an
arbitrable system so the Arbitration tab picks it up.
