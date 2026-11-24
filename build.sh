#!/usr/bin/env bash
set -euo pipefail
cd "$(dirname "$0")"
: "${VINTAGE_STORY:?Set VINTAGE_STORY to the Vintage Story install directory}"
dotnet build SyntacticWizardry.csproj -c Release
mkdir -p dist
python3 - <<'PY'
import pathlib,zipfile
p=pathlib.Path('.')
with zipfile.ZipFile('dist/vs_syntactic_wizardry_0.02-mod.zip','w',zipfile.ZIP_DEFLATED) as z:
 for f in [p/'assets',*sorted(p.glob('assets/**/*'))]:
  if f.is_dir():z.write(f,f.as_posix()+'/')
 for f in [p/'modinfo.json',p/'modicon.png',*sorted(p.glob('assets/**/*'))]:
  if f.is_file():z.write(f,f.as_posix())
 z.write('bin/Release/net10.0/SyntacticWizardry.dll','SyntacticWizardry.dll')
PY
