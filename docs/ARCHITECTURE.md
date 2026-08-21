# ModHarmony architecture

Targets the current stable tModLoader (1.4.4 branch, .NET 8, v2026.06.x).
All APIs used are public and verified against the tModLoader source
(`patches/tModLoader/Terraria/ModLoader/*.cs`).

## Module layout

```
ModHarmony.cs                      Mod entry point; opt-in Mod.Call API
ScanState.cs                       Latest scan results shared by UI/reports
Common/
  Core/                            Data model (no tModLoader dependencies)
    Enums.cs                       Severity, Confidence, ArbitrationStrategy, ...
    SystemRegistry.cs              Stable registry of ~50 game systems
    HookCatalog.cs                 Hook method → game system tables
    ModFacts.cs                    Everything learned about one loaded mod
    Conflict.cs / Evidence.cs      Report model with stable ids
    ConflictStore.cs               In-memory conflict database
    ModpackSnapshot.cs             Serializable scan + ChangeSet (what changed)
    HealthCalculator.cs            Itemized heuristic health score
  Detection/                       The scan pipeline
    ReflectionScanner.cs           Safe assembly inspection → ModFacts
    RecipeScanner.cs               Snapshot of Main.recipe at load time
    ModMetadataReader.cs           Best-effort .tmod "Info" stream parser
    IConflictDetector.cs           Detector contract
    DetectorManager.cs             Registry + isolated runner
    *Detector.cs                   The detectors
  Diagnostics/                     Investigation Mode
    RuntimeMonitor.cs              Bounded first-chance exception capture
    ErrorCorrelator.cs             Stack trace → mod attribution
    PerformanceTracker.cs          Frame-time sampling
  Arbitration/                     Opt-in conflict resolution
    ArbitrationGroup.cs            Group model (candidates, strategy, seed, lock)
    ArbitrationManager.cs          Deterministic winner resolution
    ArbitrationStore.cs            JSON persistence
    ArbitrationPoints.cs           Built-in safe arbitration points
    ArbitrationState.cs            Runtime state incl. opt-in values
  Reporting/
    SnapshotStore.cs               Scan history on disk
    ConflictPrefs.cs               Per-conflict mute preferences
    ReportGenerator.cs             Full/investigation/community reports
Content/Config/ModHarmonyConfig.cs Client-side config
Systems/
  ModHarmonySystem.cs              Orchestrates the scan + runtime monitoring
  ModHarmonyUISystem.cs            Client UI lifecycle (state, layer)
Global/ArbitrationGlobalNPC.cs     Applies built-in arbitration points
UI/                                The in-game interface (8 tabs)
Localization/en-US*.hjson          All user-facing text
test/                              Fixture mods + automated harness
```

## Scan pipeline (ModSystem.PostSetupRecipes)

```
installed .tmod metadata (ModPath scan, best effort)
        │
        ▼
per-mod facts (ReflectionScanner over AssemblyManager.GetLoadableTypes
              + Mod.GetContent for registered content counts)
        │
        ▼
recipe snapshot (RecipeScanner: Main.recipe + RecipeGroup contributors)
        │
        ▼
DetectorManager.RunAll: every detector in its own try/catch,
status recorded per detector (completed/failed/disabled)
        │
        ▼
ConflictStore.ReplaceAll  →  HealthCalculator  →  snapshot saved
        │
        ▼
arbitration group sync (persisted config + fresh candidates) → resolve winners
```

Runs during the loading screen; re-runs on demand ("Rescan Now") and after
config changes. Nothing in this pipeline runs per-frame.

## Design rules

1. **Detection only through public API.** Reflection reads metadata; nothing is
   invoked, hooked, or patched.
2. **Isolation.** A throwing detector never stops the others; a malformed mod
   never crashes the scan.
3. **Stable ids.** Mods are identified by internal name; conflicts by
   deterministic `SHA256`-derived ids; systems/strategies by enum-ish strings.
   Display names are never used as keys.
4. **Honesty.** If tModLoader does not expose a fact, we do not claim it.
   Every report says what is *not* provable (e.g. stack-trace presence ≠ blame).
5. **Bounded work.** One scan per load; cached facts; aggregated conflicts for
   huge mod sets; ring buffers for runtime events.
