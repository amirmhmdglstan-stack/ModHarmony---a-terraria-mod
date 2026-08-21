# ModHarmony

**A multi-mod compatibility analyzer for tModLoader — made so normal players can actually use it.**

When you run a big Terraria modpack, mods constantly bump into each other: two
mods both change enemy behavior, both add recipes for the same item, both tweak
how damage works. Most of the time nothing bad happens. But when a mysterious
bug shows up, it is really hard to tell *which mods could be involved*.

ModHarmony answers that question for you:

> **Detect → Explain → Investigate → Suggest → (sometimes) Resolve**

It shows you **what** is happening between your mods, **why** it was flagged, in
plain language, and **what you can do about it**. It never pretends an
interaction is a bug, and it never changes your mods without you asking.

---

## Quick start (2 minutes)

1. Download **`ModHarmony-v0.1.4.zip`** from the
   [releases page](https://github.com/amirmhmdglstan-stack/ModHarmony---a-terraria-mod/releases)
   (or from the repository at
   `ModHarmony-v0.1.4.zip` on the `arena/01a023f9-modharmony-a-terraria-mod` branch).
2. Extract it — the folder inside is **already named `ModHarmony`** (tModLoader
   needs the folder name to match the mod's name, so don't rename it).
3. Delete any older `ModHarmony*` folders from
   `Documents\My Games\Terraria\tModLoader\ModSources\`, then copy the
   `ModHarmony` folder there.
4. Start tModLoader → **Workshop** (paint roller) → **Develop Mods** → click
   **Build & Reload** on ModHarmony.
5. Enter a world and press **`N`** to open ModHarmony.

> New here? Start with the **Overview** tab — it tells you exactly what to look
> at first.

---

## The main screen, feature by feature

ModHarmony has **8 tabs**. This is what each one is for and how to use it.

### 1. Overview — "is my modpack OK?"

**What it is:** Your dashboard. Shows the **Modpack Health** score (a rough
guide, not a measurement), the things that need your attention, what changed
since your last session, and quick actions.

**How to use it:**
- Look at the **verdict line**: *"Looking good!"*, *"A few things to check."*,
  or *"Several things to investigate."* — that's the simple answer.
- Click **"Show how this is calculated"** if you want to see every point that
  was deducted and why (so the score is never a mystery).
- Check **"Things that need your attention:"** — this counts the red/orange
  flags. If it says 0 but you still have a bug, go to the **Investigate** tab.
- **"What changed since your last session?"** — added/updated/removed mods and
  new flags. Extremely useful right after you add a mod and a bug appears.
- **Quick actions** at the bottom: *Analyze Current Situation* (jumps to
  Investigate), *Save Full Report*, *Save Short Summary*, *Manage Arbitration*,
  *Scan Again* (re-run the scan without reloading mods).

### 2. Conflicts — "what's actually flagged?"

**What it is:** Every detected interaction between your mods, sorted by
importance. This is the heart of ModHarmony.

**How to use it:**
- Each card shows: a colored **severity label** (High risk / Concerning / Watch
  out / Low / Informational), **which mods** are involved, and **which part of
  the game** (e.g. "Enemy behavior").
- Under the title you get two plain-language lines:
  - **a summary** — e.g. *"Both mods change the same part of the game."*
  - **"What you can do:"** — a concrete suggestion, e.g. *"If you see bugs with
    this part of the game, disable one of these mods and test again."*
- Click **"Technical details"** if you want the raw evidence (hook names, etc.).
  Most people never need this.
- **Hide this** on a card hides it permanently (and unhide it later if you
  change your mind).
- **Filters** along the top: by severity, by certainty, by "fixable or not",
  plus a search box. There's also a toggle to show/hide the quiet (low/info)
  items — they are hidden by default so you're not overwhelmed.

**Rule of thumb:** start at the top — red and orange cards are the ones most
likely to explain a real bug.

### 3. Mods — "what is each mod doing?"

**What it is:** A list of all your loaded mods, each with a **risk dot**
(green → red based on its flags) so you can spot the troublemakers at a glance.

**How to use it:**
- Click any mod to open its detail screen:
  - **metadata** — internal name, version, author, load order, dependencies,
    optional dependencies (and which ones are missing);
  - **"What this mod changes"** — its hooks grouped by game system;
  - **"What this mod adds"** — its content counts;
  - **"Flags involving this mod"** — its conflicts, so you can see one mod's
    whole story in one place.
- Use the filters (Flagged / Red-orange / Deep code changes) and the search box
  to find what you need.

### 4. Systems — "which parts of the game have several mods?"

**What it is:** The game systems (Enemy behavior, NPC spawning, Recipes, World
generation, UI, …) with a count of how many mods touch each one.

**How to use it:** click a system to see *which mods* touch it, *which flags*
exist on it, and whether ModHarmony can arbitrate it. Great when you want to
know "who's touching enemy spawning?" before hunting a spawn-related bug.

### 5. Investigate — "I have a bug, help me find it"

**What it is:** **Investigation Mode** — ModHarmony watches while you play and
collects two useful things:
- **errors** whose technical stack traces point at your mods (with a note that
  this is a hint, not a verdict);
- **performance** — slow-frame sampling.

**How to use it (the bug-hunting workflow):**
1. Click **"Turn on Investigation Mode"** (it costs a little performance, so
   it's off by default).
2. Go play and **reproduce your bug**.
3. Come back → the **"Errors captured"** list shows what happened and which
   mods may be involved.
4. Click **"Analyze Current Situation"** → ModHarmony builds a report combining
   your mod list, the captured errors, the mods involved, and step-by-step
   recommendations.
5. Click **"Save report to file"** and share it when you ask for help.

**How to use it (the "is my game just slow?" check):** turn on Investigation
Mode, play a bit, then read the **Performance** line. It shows your average
frame time and how many slow frames happened. (It can't name which mod is slow —
it's an estimate of pacing only.)

### 6. Arbitration — "when two mods fight over a setting, who wins?"

**What it is:** For a small number of systems, ModHarmony can let you **pick
which mod wins** when several mods compete. It is **off by default** and only
affects mods that opt in — ModHarmony never patches other mods.

**How to use it:**
1. Open the **Arbitration** tab. "Systems you can control" are the fixable ones;
   "Seen but not fixable" are just listed for transparency.
2. Click the **Strategy** button to cycle through the options:
   - **Disabled** — do nothing (default).
   - **Manual priority** — you rank the mods (▲/▼ buttons); highest wins.
   - **Load order** — the mod that loads first wins.
   - **Random (seeded)** — a fair coin flip, but **stable**: the result is
     locked until you click **Roll Again**.
   - **Weighted random (seeded)** — like random, but each mod can have a
     percentage chance (adjust with +/−; the chance is shown next to each mod).
   - **First / Last registered** — the first or last mod in the list wins.
3. When you like a result, click **Lock This Choice** so it can't change.
4. Everything is remembered between sessions.

> Random choices never change during combat or gameplay — they are decided once
> and stay stable until you explicitly re-roll.

### 7. Reports — "save proof for when you ask for help"

**What it is:** Two export options, both saved to your computer as text files
(no internet needed):
- **Save Full Report** — everything: mod list with versions, all flags with
  evidence, arbitration state, runtime errors, health calculation, and "what to
  try".
- **Save Short Summary** — a compact block: versions, mod list, worth-attention
  flags, errors, systems. Perfect to paste into a Discord/GitHub issue.

**Where files go:** `Documents\My Games\Terraria\tModLoader\ModHarmony\reports\`
(inside your tModLoader save folder; the exact path is shown after saving).

**How to use it:** after a bug-hunting session (see Investigate), save the full
report and attach it when you ask for help — helpers instantly see your exact
mod list and versions.

### 8. Settings — "what's the current state?"

**What it is:** A summary of your configuration plus **scanner health** (which
scanners ran, which failed) and the paths ModHarmony uses.

**How to use it:** click **"Open ModHarmony Config"** to open the real settings
screen (see **Configuration** below).

---

## Severity & certainty — what the colors mean

Every flag has two labels:

**Severity** = how much this *could* matter if it becomes a real problem:

| Label | What it means |
|---|---|
| 🟢 Informational | Just information. No risk implied. |
| 🔵 Low | Unlikely to matter, shown for transparency. |
| 🟡 Watch out | Could interact in ways that matter. |
| 🟠 Concerning | Real chance of an observable problem. |
| 🔴 High risk | The most likely candidates for hard-to-explain bugs. |
| ⚫ Unknown | Something was detected, but its meaning can't be assessed. |

**Certainty** = how sure ModHarmony is that the interaction *exists* (not
whether it causes a bug):
- **Confirmed** — read directly from tModLoader.
- **Strong** — direct, unambiguous evidence.
- **Possible** — indirect evidence.
- **Unknown** — can't be assessed.

Colors are never the only indicator — every item has its text label too.

---

## Configuration

Open it from **Mods screen → ModHarmony → gear icon** (or the Settings tab in
ModHarmony). Organized into sections:

- **General** — enable ModHarmony, scan on load, Investigation Mode, Safe
  Diagnostics Mode.
- **Scanning** — individually turn scanner types on/off (hook overlap, recipes,
  dependencies, deep code changes, …).
- **Performance** — max captured errors, slow-frame threshold.
- **Arbitration** — enable arbitration, default strategy, master random seed,
  persist decisions.
- **UI** — show informational/low items, Developer Mode, compact mode.

**Safe Diagnostics Mode** (recommended when troubleshooting): only detect and
explain — never changes anything.

**Developer Mode**: adds raw technical detail (type names, hook names, ids) to
the UI for power users.

---

## What ModHarmony does NOT do (important)

- It does **not** claim two mods are "definitely incompatible" because they
  touch the same thing. Overlap is a *hint*, not a verdict — the UI says so.
- It does **not** inspect or modify other mods' code. It only reads public
  metadata, and never patches anything.
- It does **not** need an internet connection. Everything is local.
- It does **not** invent detections it can't back up.
- It does **not** require other mods — it adds no content and no dependencies.

---

## Performance

- The full scan runs **once when mods load**, and again only when you click
  "Scan Again" or change settings. Nothing scans every frame.
- Large modpacks stay readable: when many mods touch the same system, they are
  grouped instead of producing thousands of pairs.
- Investigation Mode is off by default and keeps only a bounded list of errors.

## Limitations (honesty section)

- ModHarmony only sees what tModLoader exposes. It can't know which exact item
  or NPC a mod changes, and it can't tell whether two "deep code changes"
  touch the exact same place — reports say so instead of guessing.
- Author/homepage/optional-dependency info is read from local mod files
  (best effort); workshop-hosted files may not include it.
- The health score is a rough guide, and says so.
- Arbitration only affects mods that opt in; there is no safe way to force a
  resolution on other mods.

## Troubleshooting

| Problem | Fix |
|---|---|
| ModHarmony doesn't open | Press `N` in a world (the UI is in-game only). Check the keybind in Mod Settings → keybinds. |
| Nothing is detected | Click **Scan Again** on the Overview tab; mods must be enabled to be scanned. |
| A flag is wrong | Click **Hide this** on the card (or **Show again** later). |
| "Namespace and Folder name do not match" | The source folder isn't named exactly `ModHarmony` — use the release zip (folder pre-named). |
| Build fails | The error is on the Develop Mods screen and in `Documents\My Games\Terraria\tModLoader\Logs\client.log`. |
| Where are my reports? | `Documents\My Games\Terraria\tModLoader\ModHarmony\reports\` (path shown after saving). |

## Developer information

Architecture and extension docs live in [`docs/`](docs/):

- [`docs/ARCHITECTURE.md`](docs/ARCHITECTURE.md) — module layout and data flow.
- [`docs/DETECTORS.md`](docs/DETECTORS.md) — detector list, evidence model, and how to add a new detector.
- [`docs/ARBITRATION.md`](docs/ARBITRATION.md) — arbitration framework, RNG semantics, persistence, and the opt-in API for other mods.
- [`docs/REPORTING.md`](docs/REPORTING.md) — reports, snapshots, "what changed".
- [`docs/TESTING.md`](docs/TESTING.md) — test strategy, fixtures, harness.
- [`docs/BUILDING.md`](docs/BUILDING.md) — how to build the `.tmod`.

## License

MIT.
