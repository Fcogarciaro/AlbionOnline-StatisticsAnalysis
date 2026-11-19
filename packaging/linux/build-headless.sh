#!/usr/bin/env bash
# Publishes the headless event daemon as one self-contained linux-x64 file: artifacts/headless/sat-headless.
#
#   packaging/linux/build-headless.sh
#
# Environment: OUT_DIR (default <repo>/artifacts), DOTNET (default "dotnet"; use "mise exec -- dotnet" with mise).
# libpcap is not bundled: the binary loads the system library (libpcap.so.1) at run time.
set -euo pipefail

root="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
project="$root/src/StatisticsAnalysisTool.Headless/StatisticsAnalysisTool.Headless.csproj"
out_dir="${OUT_DIR:-$root/artifacts}/headless"
dotnet_cmd="${DOTNET:-dotnet}"

echo "Publishing sat-headless (self-contained, single file, linux-x64, Release)"
# shellcheck disable=SC2086 # DOTNET may be a multi-word command such as "mise exec -- dotnet".
$dotnet_cmd publish "$project" \
  -c Release -r linux-x64 --self-contained true \
  -p:PublishSingleFile=true -p:EnableCompressionInSingleFile=true -p:DebugType=none -p:DebugSymbols=false \
  -o "$out_dir" --nologo

[[ -x "$out_dir/sat-headless" ]] || { echo "error: $out_dir/sat-headless was not produced" >&2; exit 1; }

echo "Wrote $out_dir/sat-headless ($(du -h "$out_dir/sat-headless" | cut -f1))"
echo "Grant capture permission once: sudo setcap cap_net_raw,cap_net_admin=eip $out_dir/sat-headless"
