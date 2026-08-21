# CI build workflow

This repository includes a GitHub Actions workflow that builds the mod against
the current stable tModLoader release. It is not enabled in this repository
because the automation token used during development does not have the
`workflows` permission (GitHub blocks GitHub Apps without that permission from
creating workflow files). To enable it, add the workflow below with your own
token (a personal access token, or a GitHub App with the `workflows`
permission), or push it from a repository you own.

`.github/workflows/build.yml`:

```yaml
name: Build ModHarmony

on:
  push:
    branches: ['**']
  workflow_dispatch:

jobs:
  build:
    runs-on: ubuntu-latest
    steps:
      - name: Checkout
        uses: actions/checkout@v4

      - name: Setup .NET SDK
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '8.0.x'

      - name: Download tModLoader stable release
        run: |
          curl -sL -o tml.zip "https://github.com/tModLoader/tModLoader/releases/download/v2026.06.3.6/tModLoader.zip"
          unzip -q tml.zip -d tml

      - name: Create local tModLoader.targets shim
        run: |
          cat > tModLoader.targets <<'EOF'
          <Project>
            <Import Project="$(tMLPath)/tMLMod.targets" />
          </Project>
          EOF

      - name: Compile mod
        run: dotnet build ModHarmony.csproj -c Release -p:tMLPath=$PWD/tml -p:BuildMod=true

      - name: Upload .tmod artifact
        uses: actions/upload-artifact@v4
        with:
          name: ModHarmony-tmod
          path: bin/Release/**/ModHarmony.tmod
          if-no-files-found: error
```

Notes:

- Pin `v2026.06.3.6` (or whatever stable release you target) — update the URL
  when tModLoader publishes a new stable.
- The build runs `dotnet tModLoader.dll -build` (the game's headless build
  server) which produces the final `ModHarmony.tmod`.
- The same technique works locally: extract a tModLoader release, create the
  shim `tModLoader.targets` next to the csproj, and build with
  `-p:tMLPath=<extracted folder>`.
