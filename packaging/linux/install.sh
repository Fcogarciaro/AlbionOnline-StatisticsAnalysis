#!/usr/bin/env bash
# Installs Statistics Analysis Tool for the current user from the extracted release tarball.
#
#   ./install.sh [--setcap] [--uninstall]
#
#   --setcap     also run `sudo setcap` on the installed binary (asks for your sudo password)
#   --uninstall  remove what this script installed (settings and data stay in ~/.config and ~/.local/share)
#
# Installs to ~/.local/opt/StatisticsAnalysisTool, a launcher link in ~/.local/bin, a desktop entry and the icons under
# ${XDG_DATA_HOME:-~/.local/share}. Override the target with INSTALL_DIR.
set -euo pipefail

here="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
install_dir="${INSTALL_DIR:-$HOME/.local/opt/StatisticsAnalysisTool}"
data_home="${XDG_DATA_HOME:-$HOME/.local/share}"
bin_dir="$HOME/.local/bin"
binary="$install_dir/StatisticsAnalysisTool"
caps="cap_net_raw,cap_net_admin=eip"

do_setcap=0
uninstall=0
for arg in "$@"; do
  case "$arg" in
    --setcap) do_setcap=1 ;;
    --uninstall) uninstall=1 ;;
    -h|--help) sed -n '2,10p' "${BASH_SOURCE[0]}" | sed 's/^# \{0,1\}//'; exit 0 ;;
    *) echo "unknown option: $arg" >&2; exit 2 ;;
  esac
done

refresh_caches() {
  command -v update-desktop-database >/dev/null 2>&1 && update-desktop-database "$data_home/applications" 2>/dev/null || true
  command -v gtk-update-icon-cache >/dev/null 2>&1 && gtk-update-icon-cache -q -t "$data_home/icons/hicolor" 2>/dev/null || true
}

if [[ $uninstall -eq 1 ]]; then
  rm -rf "$install_dir"
  rm -f "$bin_dir/statisticsanalysistool" "$data_home/applications/statisticsanalysistool.desktop"
  rm -f "$data_home"/icons/hicolor/*/apps/statisticsanalysistool.png
  refresh_caches
  echo "Removed $install_dir. Settings and data were left in place."
  exit 0
fi

if [[ ! -x "$here/StatisticsAnalysisTool" ]]; then
  echo "error: run this script from the extracted tarball (StatisticsAnalysisTool binary not found next to it)" >&2
  exit 1
fi

# Replace the previous install as a unit. A replaced binary loses its file capabilities, so setcap runs again below.
staging="$install_dir.new"
rm -rf "$staging"
mkdir -p "$(dirname "$install_dir")"
cp -a "$here" "$staging"
rm -rf "$staging/share" "$staging/install.sh"
rm -rf "$install_dir"
mv "$staging" "$install_dir"

mkdir -p "$bin_dir" "$data_home/applications"
ln -sf "$binary" "$bin_dir/statisticsanalysistool"

# The packaged entry launches `statisticsanalysistool` from PATH; pin the absolute path so it works without ~/.local/bin on PATH.
sed -e "s|^Exec=.*|Exec=\"$binary\"|" -e "/^Path=/d" -e "/^Exec=/a Path=$install_dir" \
  "$here/share/applications/statisticsanalysistool.desktop" > "$data_home/applications/statisticsanalysistool.desktop"

for size_dir in "$here"/share/icons/hicolor/*/; do
  size="$(basename "$size_dir")"
  mkdir -p "$data_home/icons/hicolor/$size/apps"
  cp "$size_dir/apps/statisticsanalysistool.png" "$data_home/icons/hicolor/$size/apps/"
done
refresh_caches

echo "Installed to $install_dir"

if [[ $do_setcap -eq 1 ]]; then
  sudo setcap "$caps" "$binary"
  echo "Capture permission granted to $binary"
else
  cat <<MSG

Packet capture needs a one-time permission on the binary (instead of running the whole tool as root):

  sudo setcap $caps "$binary"

Run it again after every update or re-install: replacing the file removes the capability.
(or re-run this script with --setcap). Details: README-linux.md.
MSG
fi
