#!/usr/bin/env bash
# Installs the headless daemon (sat-headless) for the current user and prints the one-time setcap command.
#
#   packaging/linux/install-headless.sh [--uninstall]
#
# Run build-headless.sh first (or put a sat-headless binary next to this script). Installs to
# ~/.local/opt/StatisticsAnalysisTool/sat-headless and links it from ~/.local/bin/sat-headless. Override the
# directory with INSTALL_DIR. This script never runs sudo.
# Note: install.sh of the desktop app replaces that directory as a unit, so run this script again after it.
set -euo pipefail

here="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
root="$(cd "$here/../.." && pwd)"
install_dir="${INSTALL_DIR:-$HOME/.local/opt/StatisticsAnalysisTool}"
bin_dir="$HOME/.local/bin"
target="$install_dir/sat-headless"
caps="cap_net_raw,cap_net_admin=eip"

if [[ "$install_dir" != /* ]]; then
  echo "error: INSTALL_DIR must be an absolute path" >&2; exit 2
fi

case "${1:-}" in
  "") ;;
  --uninstall)
    rm -f "$target" "$bin_dir/sat-headless"
    echo "Removed $target"
    exit 0 ;;
  -h|--help) sed -n '2,9p' "${BASH_SOURCE[0]}" | sed 's/^# \{0,1\}//'; exit 0 ;;
  *) echo "unknown option: $1" >&2; exit 2 ;;
esac

source_binary=""
for candidate in "$here/sat-headless" "${OUT_DIR:-$root/artifacts}/headless/sat-headless"; do
  if [[ -x "$candidate" ]]; then source_binary="$candidate"; break; fi
done
if [[ -z "$source_binary" ]]; then
  echo "error: sat-headless not found; run packaging/linux/build-headless.sh first" >&2
  exit 1
fi

mkdir -p "$install_dir" "$bin_dir"
# Install through a temporary name so a running daemon is never overwritten in place. A replaced file loses its capabilities.
install -m 0755 "$source_binary" "$target.new"
mv -f "$target.new" "$target"
ln -sf "$target" "$bin_dir/sat-headless"

echo "Installed to $target (linked from $bin_dir/sat-headless)"
cat <<MSG

Packet capture needs a one-time permission on the binary:

  sudo setcap $caps "$target"

Run it again after every re-install: replacing the file removes the capability.
Then start it with: sat-headless   (JSON lines on stdout, diagnostics on stderr)
MSG
