# Building ModHarmony into a .tmod

The whole process: **copy the `ModHarmony` folder into Mod Sources → click Build
in tModLoader → enable the mod**. That's it. The game compiles the mod and
installs the resulting `ModHarmony.tmod` into your Mods folder automatically.

> **⚠️ The one thing that matters most:** tModLoader derives the mod's internal
> name from the **source folder name** and requires the assembly name and the
> top-level namespace to match it. If the folder is named anything other than
> exactly **`ModHarmony`** (for example the repository folder name
> `ModHarmony---a-terraria-mod`, or a name GitHub adds like
> `ModHarmony---a-terraria-mod-0.1.2`), you get:
>
> **"Namespace and Folder name do not match. The top level namespace must match
> the folder name."**
>
> The release zip (below) already has the folder named `ModHarmony`, so you
> cannot get this wrong if you use it.

---

## Recommended: use the release zip

1. Download **`ModHarmony-v0.1.2.zip`** from the
   [releases page](https://github.com/amirmhmdglstan-stack/ModHarmony---a-terraria-mod/releases).
2. Extract it. The top-level folder inside is **already named `ModHarmony`** —
   do not rename it.
3. **Delete any older `ModHarmony*` folders** from your Mod Sources folder:
   `Documents\My Games\Terraria\tModLoader\ModSources\`
4. Copy the whole `ModHarmony` folder into Mod Sources.
5. Launch tModLoader → **Workshop → Develop Mods** → click **Build** (or
   **Build & Reload**) on the ModHarmony row.
6. `ModHarmony.tmod` lands in your Mods folder; enable it and reload if needed.

## Alternative: clone the branch yourself

```
git clone -b arena/01a023f9-modharmony-a-terraria-mod https://github.com/amirmhmdglstan-stack/ModHarmony---a-terraria-mod.git
# then RENAME the folder to "ModHarmony" before copying into Mod Sources
```

The repository folder name contains hyphens and will fail the namespace check —
**you must rename it to `ModHarmony`**.

## Building from the command line (optional)

Same folder requirement. With the .NET 8 SDK:

```
cd "Documents\My Games\Terraria\tModLoader\ModSources"
dotnet build ModHarmony\ModHarmony.csproj -c Release
```

The csproj now contains a guard that fails with a clear message if the folder
name is wrong.

## After making code changes

Just click **Build** (or Build & Reload) again — the new `.tmod` replaces the
old one.

## Troubleshooting

| Problem | Fix |
|---|---|
| "Namespace and Folder name do not match" | The folder is not named exactly `ModHarmony` — use the release zip (folder pre-named), or rename the folder. |
| "Mod name X does not match assembly name Y" | Same cause: folder name ≠ `ModHarmony`. |
| Build fails with `error CS...` | Read the lines after "Compilation finished with N errors" on the Mod Sources screen, or in `Documents\My Games\Terraria\tModLoader\Logs\client.log`. |
| Mod doesn't appear in Mod Sources | Make sure the folder is directly inside `ModSources` and named `ModHarmony`, then press the refresh icon. |
| Works on stable but not preview | Both are supported (APIs verified on 1.4.4 stable and 2026 preview); if a specific preview build misbehaves, tell us the version. |
