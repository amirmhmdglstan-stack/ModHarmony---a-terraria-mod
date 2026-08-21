# Building ModHarmony into a .tmod

The whole process: **copy the project into the Mod Sources folder → click Build in
tModLoader → enable the mod**. That's it. The game compiles the mod and installs
the resulting `ModHarmony.tmod` into your Mods folder automatically.

> **Important:** the mod's internal name comes from the *folder name* of the
> source. This repository's folder is named `ModHarmony---a-terraria-mod`
> (hyphens are not valid in mod names), so you **must rename it to `ModHarmony`**
> when copying. The code, assembly and namespace are all already named
> `ModHarmony`, so everything lines up once the folder is named correctly.

---

## Option A — Build inside tModLoader (easiest, no IDE)

1. **Copy the project** from this repository to your Mod Sources folder and
   rename the folder to `ModHarmony`:

   ```
   Windows: Documents\My Games\Terraria\tModLoader\ModSources\ModHarmony\
   macOS:   ~/Library/Application Support/Terraria/tModLoader/ModSources/ModHarmony/
   Linux:   ~/.local/share/Terraria/tModLoader/ModSources/ModHarmony/
   ```

   The folder must contain `ModHarmony.csproj`, `ModHarmony.cs`, `build.txt`,
   `Localization\`, etc. (You may delete `.git`, `docs` and `test` — they are
   ignored by the build anyway.)

   If `ModSources` doesn't exist yet, launch tModLoader once and open the
   Mod Sources screen; the game creates the folder (with the required
   `tModLoader.targets` inside).

2. **Launch tModLoader** (the modded launcher, not Terraria itself). Use the
   current **stable 1.4.4** version.

3. Main menu → **Workshop** (paint-roller icon) → **Develop Mods**.
   This opens the Mod Sources screen. ModHarmony appears in the list.

4. Click **Build** on the ModHarmony row (or **Build & Reload** to build and
   load it in one go). Watch the build output at the bottom; when it finishes,
   `ModHarmony.tmod` has been written to your Mods folder
   (`Documents\My Games\Terraria\tModLoader\Mods\`).

5. Open the **Mods** screen (puzzle icon), find **ModHarmony**, toggle it **ON**,
   then click **Reload Mods** (skip this if you used Build & Reload).

6. Start/enter a world and press **N** — the ModHarmony UI opens.

## Option B — Build from the command line

1. Same as Option A step 1 (copy + rename folder into Mod Sources).
2. Install the **.NET 8 SDK** if you don't have it.
3. Open a terminal in the Mod Sources folder and run:

   ```
   dotnet build ModHarmony\ModHarmony.csproj -c Release
   ```

   This uses the `tModLoader.targets` the game placed in Mod Sources, compiles
   the mod, and installs `ModHarmony.tmod` into the Mods folder.
4. In-game: enable ModHarmony in the Mods screen → **Reload Mods**.

(Equivalent: open `ModHarmony.csproj` in Visual Studio 2022 and press Build.)

---

## After making code changes

Just click **Build** (or Build & Reload) again — no need to re-copy anything.
The new `.tmod` replaces the old one in the Mods folder.

## Sharing

The `ModHarmony.tmod` file in your Mods folder is the finished mod. Friends can
install it by dropping it into their own Mods folder and enabling it in-game.

## Troubleshooting

| Problem | Fix |
|---|---|
| Build fails with errors | Read the output shown on the Mod Sources screen; details are also in `Documents\My Games\Terraria\tModLoader\Logs\client.log`. |
| Mod doesn't appear in Mod Sources | Make sure the folder is directly inside `ModSources` and named exactly `ModHarmony`, then click the refresh icon. |
| "tModLoader.targets not found" | The game generates it in Mod Sources — open the Mod Sources screen once, or copy `tModLoader.targets` from any other working mod source. |
| Works on stable but not preview | ModHarmony targets the stable 1.4.4 release (e.g. v2026.06.x); use stable. |
