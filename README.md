# ModHarmony

**A multi-mod compatibility analyzer and conflict manager for tModLoader.**

When you run a large Terraria modpack, mods constantly interact: two mods both
patch NPC AI, both edit recipes for the same item, both register global hooks
that change how damage is calculated. Most of the time nothing bad happens —
but sometimes it does, and when a mysterious bug shows up it is very hard to
tell *which mods could be involved*.

ModHarmony gives you a much better answer to that question:

> **Detect → Explain → Investigate → Suggest → Safely Resolve when possible.**

It does **not** pretend every interaction is a bug, and it does **not** magically
"fix" mod conflicts. Every finding carries a severity, a confidence level, and
plain-language evidence explaining exactly why it was flagged and what it does
— and does not — prove.

---

## What ModHarmony does

- **Scans your loaded mods** once per load (and on demand) using only public,
  safe tModLoader APIs:
  - **Hook overlap** — multiple mods overriding hooks in the same game system
    (e.g. both override `GlobalNPC.AI`).
  - **Global class overlap** — mods registering the same `Global*` base class.
  - **ModPlayer overlap** — overlapping player behavior.
  - **ModSystem overlap** — overlapping update loops, world generation, UI
    layers, recipe phases.
  - **Recipe interactions** — multiple mods crafting the same item; shared
    recipe groups (both *confirmed* — tModLoader exposes the registering mod).
  - **Dependency analysis** — dependency cycles; missing optional dependencies.
    (Missing hard dependencies and unmet version requirements are *not*
    reported: tModLoader refuses to load such mods, so they cannot occur.)
  - **IL / runtime patching** — static, verifiable signals that a mod patches
    game code (IL./On. namespaces, `IL_Terraria_*` methods, MonoMod/Cecil
    references).
  - **Asset/install collisions** — duplicate `.tmod` files claiming the same
    internal name.
- **Explains** every finding with evidence and a "why is this here?" section.
- **Investigation Mode** — optional runtime diagnostics: captures exceptions
  whose stack traces involve loaded mods (bounded ring buffer, gated by config)
  and samples frame times. One click produces an "Analyze Current Situation"
  report.
- **Reports** — full compatibility report exported to a text file, plus a
  compact "community summary" for Discord/GitHub issues. Fully offline.
- **"What changed?"** — snapshots of every scan are stored locally; the next
  session shows added/removed/updated mods, new or resolved conflicts, severity
  changes, load-order changes, dependency changes and new runtime errors.
- **Modpack Health** — a transparent *heuristic* score with an itemized
  breakdown of exactly how it was calculated (never presented as objective).
- **Arbitration (opt-in)** — for supported systems, lets you choose which mod
  "wins" when several mods compete: manual priority, load order, seeded random,
  weighted random, first/last registered. Deterministic, persisted, locked
  until you change it.
- **Safe Diagnostics Mode** — detection only, no arbitration, maximum
  diagnostics. The recommended mode when troubleshooting.

## What ModHarmony does NOT do

- It does **not** claim two mods are "definitely incompatible" because they
  override the same hook. Hook overlap is evidence of *interaction*, not proof
  of *incompatibility* — the UI and reports say so explicitly.
- It does **not** inspect, hook, detour, or modify other mods' code. Reflection
  is used only to *read* public metadata; nothing is injected anywhere.
- It does **not** need an internet connection. Everything is local.
- It does **not** invent detections it cannot back up. If a situation cannot be
  observed through the public API, it is not reported (and the surrounding
  architecture documents the limitation instead).
- It does **not** require other mods to install it (it adds no content).

---

## Installation

1. Copy the `ModHarmony` folder into your tModLoader **Mod Sources** folder
   (`Documents/My Games/Terraria/tModLoader/ModSources/ModHarmony`).
2. Open `ModHarmony.csproj` in Visual Studio (or run
   `dotnet build ModHarmony.csproj` with the tModLoader .NET 8 SDK setup).
3. Build, then reload mods in tModLoader and enable **ModHarmony**.
4. Open the ModHarmony UI with the default hotkey **`N`** (configurable under
   Mod Settings → ModHarmony → keybinds).

> The project targets the current stable tModLoader (1.4.4 branch, .NET 8,
> e.g. v2026.06.x). It requires the `tModLoader.targets` file that the game
> places in the Mod Sources folder (the csproj imports it automatically).

## Features

### Mods tab
Every loaded mod shows: internal name (stable id), display name, version,
built-for tML version, author/homepage (when readable from the local mod file),
side, load-order position, dependencies, optional dependencies, detected hooks
grouped by game system, registered content counts, runtime-patch signals, and
the conflicts that involve it.

### Conflicts tab
Search and filter by severity, confidence, and resolvability. Each conflict
card expands to show evidence per mod, the "why is this here?" explanation,
arbitration status, developer details (in Developer Mode) and a mute toggle.

### Systems tab
The registry of game systems (NPC AI, item damage, world generation, …), which
mods touch each one, which conflicts exist on it, and whether ModHarmony can
arbitrate it.

### Investigation tab
Enables/disables Investigation Mode, lists captured runtime events with likely
involved mods (stack-trace presence ≠ blame, the UI says so), shows frame-time
sampling, and generates "Analyze Current Situation" reports.

### Arbitration tab
Per-system arbitration groups. Only systems with a technically safe mechanism
are resolvable; everything else is listed as
"Detection available — automatic resolution unavailable."

### Reports tab
Export the full report to `{SavePath}/ModHarmony/reports/`, copy the community
summary to the clipboard, preview the last report.

## Conflict severity & confidence

Severity (what could happen if this becomes a real problem):

| Legend | Meaning |
|---|---|
| 🟢 Informational | Interaction detected; no risk implied |
| 🔵 Low | Unlikely to matter, flagged for transparency |
| 🟡 Potential conflict | Could interact in ways that matter |
| 🟠 Significant conflict | Real chance of observable interference |
| 🔴 High risk | Most likely candidates for hard-to-explain bugs |
| ⚫ Unknown | Something detected, meaning not assessable |

Confidence (how sure we are the *interaction* exists — not whether it causes a
bug): **Confirmed** (read directly from tModLoader), **Strong** (direct
unambiguous evidence), **Possible** (indirect evidence), **Unknown**.

Colors are never the only indicator: every chip includes its text label.

## Arbitration

Arbitration is strictly opt-in. Default behavior is *detect and explain*; no
behavior is modified until you:

1. enable **Enable arbitration** in the ModHarmony config,
2. choose a strategy for a group in the Arbitration tab.

Supported strategies: **Manual priority**, **Load order**, **Random (seeded)**,
**Weighted random (seeded)**, **First registered**, **Last registered**,
**Disabled**.

### Random arbitration
- Uses a controlled `System.Random` with a fixed seed.
- Seed `auto` derives deterministically from the group id + master config seed,
  so the same pack + config always rolls the same winner.
- The winner is resolved once, **never per frame**; gameplay cannot randomly
  change during combat.
- **Regenerate Selection** rolls a new seed; **Lock** freezes the current
  decision. Both are persisted.

### Weighted random
Each candidate has a weight (default 100). Weights are validated (no negatives,
not all zero); invalid weights fall back to uniform selection. The effective
probability is `weight / Σweights` and is shown next to each candidate.

### What can actually be arbitrated
ModHarmony only arbitrates systems where it owns a technically defensible
mechanism. Built-in arbitration points:

| Point | Effect |
|---|---|
| `npc.spawn` | The winning mod's registered factor is applied to NPC spawn rate (factor < 1 = fewer spawns, 1 = no change). |
| `npc.damage` | The winning mod's registered multiplier is applied to damage dealt to NPCs (1 = no change). |

These points only affect mods that **opt in** by registering a value:

```csharp
ModHarmony.Call("RegisterArbitrableValue", "npc.spawn", Mod.Name, 0.75f, "25% fewer spawns");
// query the current winner's value:
float factor = (float)ModHarmony.Call("GetArbitratedValue", "npc.spawn");
// or the winning mod's name:
string winner = (string)ModHarmony.Call("GetArbitrationWinner", "npc.spawn");
```

ModHarmony never patches or injects into third-party code, so for every other
system the Arbitration tab shows
**"Detection available — automatic resolution unavailable."**

## Configuration

Client-side config via the tModLoader config UI (Mods → ModHarmony → gear icon),
organized into **General / Scanning / Performance / Arbitration / UI** sections.

Highlights:
- **Safe Diagnostics Mode** — detection only, no arbitration, maximum diagnostics.
- **Runtime monitoring** — Investigation Mode toggle.
- **Scan \*** — individually disable detector families.
- **Enable arbitration / Default strategy / Master random seed / Persist decisions.**
- **Developer Mode** — shows raw technical detail (type names, hook names,
  detector ids, conflict ids, raw evidence).

## Performance

- The full scan runs **once at load time** (during the loading screen) and
  only again on demand or after config changes. Nothing scans every frame.
- Reflection results are cached per load; `AssemblyManager.GetLoadableTypes`
  is used (the safe API tModLoader recommends) instead of raw `GetTypes()`.
- Large contestant sets (>8 mods on one system) are aggregated into one
  conflict instead of N×N pairs, so huge modpacks stay readable.
- Investigation Mode's exception observer is **off by default** and uses a
  bounded ring buffer (`MaxRetainedEvents`).
- The Settings tab shows detector health; the log shows scan duration.

## Limitations (read this)

- Detection is limited to what tModLoader's public API exposes. We cannot know
  *which specific NPCs/items* a mod affects from hooks alone, cannot see other
  mods' runtime state, and cannot determine whether two IL patches touch the
  *same method* — reports say so instead of guessing.
- Author/homepage/optional-dependency metadata is read best-effort from local
  `.tmod` files; workshop-hosted mod files may not expose it.
- The health score is a heuristic and says so.
- Arbitration points only affect cooperating mods; there is no safe way to
  arbitrate non-cooperating third-party code.

## Troubleshooting

- **ModHarmony doesn't open** — check the keybind (default `N`), and that you
  are in a world (the UI is in-game only).
- **Nothing is detected** — rescan from the Overview tab; verify the scan ran
  (Settings tab → detector health). Mods must be *loaded* (enabled) to be
  scanned.
- **I found a false positive** — mute the conflict (right side of its card);
  muted conflicts are hidden but stay in reports marked muted.
- **Logs** — ModHarmony logs with the `[ModHarmony]` prefix to the normal
  tModLoader client log.

## Developer information

Architecture and extension docs live in [`docs/`](docs/):

- [`docs/ARCHITECTURE.md`](docs/ARCHITECTURE.md) — module layout and data flow.
- [`docs/DETECTORS.md`](docs/DETECTORS.md) — detector list, evidence model,
  and **how to add a new detector**.
- [`docs/ARBITRATION.md`](docs/ARBITRATION.md) — arbitration framework, RNG
  semantics, persistence, and the opt-in API for other mods.
- [`docs/REPORTING.md`](docs/REPORTING.md) — reports, snapshots, "what changed".
- [`docs/TESTING.md`](docs/TESTING.md) — test strategy, fixtures, harness.
- [`docs/CI.md`](docs/CI.md) — GitHub Actions build workflow (enable it in
  your fork/repo with a token that has `workflows` permission).

### How to add a detector

1. Implement `IConflictDetector` (see `Common/Detection/`).
2. Register it in `DetectorManager`'s constructor.
3. Add a config gate (`Scan*` field in `ModHarmonyConfig`) if appropriate.
4. Add localization keys (`Detectors.{Id}.Name/Description`,
   `Evidence.{...}`, `UI.Conflicts.Why.{Id}`).
5. Add a test case to `test/TestHarness/Program.cs` and (optionally) a fixture
   mod under `test/fixtures/`.

## License

All original code in this repository is released under the MIT License.
