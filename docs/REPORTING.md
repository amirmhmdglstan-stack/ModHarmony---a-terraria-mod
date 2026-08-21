# Reporting, snapshots, "what changed"

## Full report

`ReportGenerator.BuildFullReport` produces a text report with:

1. **Game versions** — Terraria + tModLoader versions, session id, timestamp.
2. **Loaded mods** — in load order with versions, authors (when readable),
   dependencies, missing optional dependencies, hook counts per system,
   content counts, runtime-patch signals.
3. **Detected conflicts** — severity/confidence, involved mods, system,
   detector + stable conflict id, evidence lines, arbitration status.
4. **Systems with most overlap.**
5. **Arbitration groups** and their current winners.
6. **Runtime diagnostics** — captured events (investigation), performance
   summary, detector failures.
7. **Modpack health** — heuristic score + itemized deductions.
8. **Recommended investigation** — concrete next steps.

Exported to `{SavePath}/ModHarmony/reports/ModHarmonyReport_yyyyMMdd_HHmmss.txt`
and previewed in the Reports tab.

## Community summary

A compact block designed for Discord/GitHub issues: versions, mod list with
versions, high-risk interactions, runtime errors, relevant systems. Copied to
the clipboard with one click. No paths or save data are included.

## Snapshots & "What changed?"

Every scan is saved to `{SavePath}/ModHarmony/snapshots/`:

- `latest.json` — most recent scan;
- `session-{id}.json` — history (pruned to 5).

On the next scan, `ChangeSet.Compare` produces:

- added / removed mods,
- version changes,
- load-order changes,
- dependency changes,
- new / resolved conflicts,
- severity changes,
- new runtime errors (captured during the previous session and merged into the
  snapshot at save-and-quit / world unload).

Shown on the Overview tab and logged with the `[ModHarmony]` prefix.

## Investigation reports

`BuildInvestigationReport` ("Analyze Current Situation") combines the current
scan with live data: top mods from captured errors, conflicts involving those
mods, runtime events, performance summary, and step-by-step recommendations.

## Privacy

Reports contain mod names/versions, technical hook/system names and (when
Investigation Mode captured them) exception types/messages. They do not contain
player names, world seeds or cloud credentials. The community summary is the
most minimal export.
