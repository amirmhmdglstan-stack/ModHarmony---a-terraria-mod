# Arbitration

Arbitration answers "**which mod should win?**" when several mods compete over
a supported system. It is strictly **opt-in** and **never touches third-party
code**. Detection always runs; nothing is applied unless the player enables
arbitration and resolves a group.

## Safety model

- Only systems with a `ModHarmony`-owned, technically defensible mechanism are
  resolvable (built-in `ArbitrationPoint`s).
- Points only affect mods that register a value through the Mod.Call API.
- All other contested systems are shown as
  **"Detection available — automatic resolution unavailable."**
- Safe Diagnostics Mode forces detection-only.

## Group model

A group is per system (`system.{systemId}`) and holds:

- `Strategy` — Disabled / ManualPriority / LoadOrder / Random / WeightedRandom /
  FirstRegistered / LastRegistered;
- `Seed` — explicit or `-1` (auto);
- `Locked` — keeps the current winner;
- `Candidates` — mods with `Weight`, `ManualPriority`, `LoadIndex`,
  optional registered value.

## Winner resolution

| Strategy | Winner |
|---|---|
| ManualPriority | Highest `ManualPriority` (ties → list order) |
| LoadOrder | Lowest load index |
| FirstRegistered / LastRegistered | First / last candidate in list order |
| Random | Seeded `System.Random` pick |
| WeightedRandom | Seeded roll over `weight / Σweights`; invalid weights (any negative, all zero) fall back to uniform |

### Determinism and stability (random/weighted)

- `Random(seed)` is constructed from the group seed: same seed + same candidate
  list ⇒ same winner, across sessions.
- Auto seed (`-1`) = `SHA256("ModHarmony-arbitration|{groupId}|{masterSeed}")`
  truncated to a positive int — deterministic until the master seed changes.
- Resolution happens once per scan / interaction, **never per frame** — the
  winner cannot change during combat.
- **Regenerate Selection** stores a fresh random seed and unlocks.
- **Lock** freezes the winner (persisted).
- Decisions are logged to the tModLoader log with the `[ModHarmony]` prefix.

## Persistence

`{SavePath}/ModHarmony/arbitration.json`:

```json
{
  "Version": 1,
  "Groups": [{
    "GroupId": "system.npc.spawn",
    "SystemId": "npc.spawn",
    "StrategyName": "WeightedRandom",
    "Seed": 12345,
    "Locked": false,
    "Candidates": [
      { "ModName": "ModA", "Weight": 60.0, "ManualPriority": 0, "LoadIndex": 3 },
      { "ModName": "ModB", "Weight": 40.0, "ManualPriority": 0, "LoadIndex": 7 }
    ]
  }]
}
```

Groups are keyed by stable ids, so decisions survive rescans and mod updates.
Candidates whose mod is no longer loaded are dropped (they cannot silently win).

## Opt-in API for other mods

Call the ModHarmony mod instance:

```csharp
// Register your influence on an arbitrable system. value > 0; 1 = "no change".
ModHarmony.Call("RegisterArbitrableValue", "npc.spawn", Mod.Name, 0.75f, "fewer spawns");

// Query the current winner's value (1 when arbitration is off/unresolved):
float factor = (float)ModHarmony.Call("GetArbitratedValue", "npc.spawn");

// Query the winning mod's internal name ("" when none):
string winner = (string)ModHarmony.Call("GetArbitrationWinner", "npc.spawn");

// Diagnostics:
int conflicts = (int)ModHarmony.Call("GetConflictCount");
int health = (int)ModHarmony.Call("GetModHealth");
ModHarmony.Call("ForceRescan");
```

Errors return a string describing the problem instead of throwing.

## Built-in points

| Point | Applied in | Semantics |
|---|---|---|
| `npc.spawn` | `ArbitrationGlobalNPC.EditSpawnRate` | winner's factor scales spawn rate (inverted, since lower rate = more spawns) |
| `npc.damage` | `ArbitrationGlobalNPC.ModifyHitByItem/Projectile` | winner's multiplier applied to `HitModifiers.FinalDamage` |

Both are no-ops (factor 1) unless a group resolves with a registered value.

## Per-conflict configuration

Every conflict on an arbitrable system exposes its group's controls in the
Conflicts tab detail view (strategy, regenerate, lock) and in the Arbitration
tab. Conflicts on non-arbitrable systems show the "unavailable" note only —
there is deliberately no way to force a resolution on those.
