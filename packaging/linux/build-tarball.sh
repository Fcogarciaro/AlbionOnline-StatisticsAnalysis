#!/usr/bin/env bash
# Builds the self-contained linux-x64 release tarball and its SHA256 file.
#
#   packaging/linux/build-tarball.sh [VERSION]
#
# VERSION (for example 0.1.0, without the "linux-v" prefix) defaults to the <Version> of the Avalonia project. It is
# compiled into the assembly, so the update check can compare it with the newest `linux-v*` GitHub release.
# Environment: OUT_DIR (default <repo>/artifacts), DOTNET (default "dotnet"; use "mise exec -- dotnet" with mise).
set -euo pipefail

root="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
project="$root/src/StatisticsAnalysisTool.Avalonia/StatisticsAnalysisTool.Avalonia.csproj"
out_dir="${OUT_DIR:-$root/artifacts}"
dotnet_cmd="${DOTNET:-dotnet}"
name="StatisticsAnalysisTool"
asset="$name-linux-x64.tar.gz"

version="${1:-${VERSION:-}}"
if [[ -z "$version" ]]; then
  version="$(sed -n 's:.*<Version>\(.*\)</Version>.*:\1:p' "$project" | head -n 1)"
fi
version="${version#linux-v}"
if [[ ! "$version" =~ ^[0-9]+(\.[0-9]+){1,3}$ ]]; then
  echo "error: version '$version' is not numeric (expected e.g. 0.1.0)" >&2
  exit 1
fi

stage="$(mktemp -d)"
trap 'rm -rf "$stage"' EXIT
app="$stage/$name"

echo "Publishing $name $version (self-contained, linux-x64, Release)"
# shellcheck disable=SC2086 # DOTNET may be a multi-word command such as "mise exec -- dotnet".
$dotnet_cmd publish "$project" \
  -c Release -r linux-x64 --self-contained true \
  -p:Version="$version" -p:DebugType=none -p:DebugSymbols=false \
  -o "$app" --nologo

# The licence files are copied by the project (LICENSE, licenses/*). Fail loudly when one is missing.
for required in "$name" LICENSE licenses/FontAwesome-LICENSE.txt licenses/GameIcons-NOTICE.txt Localization/localization.json; do
  [[ -e "$app/$required" ]] || { echo "error: $required is missing from the publish output" >&2; exit 1; }
done

# Installer, desktop entry, icons and the readme travel with the binary.
install -m 0755 "$root/packaging/linux/install.sh" "$app/install.sh"
install -D -m 0644 "$root/packaging/linux/statisticsanalysistool.desktop" "$app/share/applications/statisticsanalysistool.desktop"
cp -r "$root/packaging/linux/icons" "$app/share/icons"
install -m 0644 "$root/README-linux.md" "$app/README-linux.md"

mkdir -p "$out_dir"
tarball="$out_dir/$asset"
tar --sort=name --owner=0 --group=0 --numeric-owner -C "$stage" -czf "$tarball" "$name"
(cd "$out_dir" && sha256sum "$asset" > "$asset.sha256")

echo "Wrote $tarball ($(du -h "$tarball" | cut -f1))"
echo "Wrote $tarball.sha256: $(cut -d' ' -f1 "$tarball.sha256")"
