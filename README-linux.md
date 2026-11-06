# Statistics Analysis Tool for Linux

A native Linux (Avalonia) port of the **Albion Online Statistics Analysis Tool** by [Triky313](https://github.com/Triky313/AlbionOnline-StatisticsAnalysis).
This fork keeps the original logic and screens and replaces the Windows-only parts (Npcap, registry, tray, dialogs, updater) with Linux ones.
Licensed under **GPL-3.0**, like the original. All credit for the tool itself goes to Triky313 and the upstream contributors.

The tool reads Albion Online network traffic (like a packet sniffer, no game files are modified) to show the damage meter, loot, market data,
dungeons, trading and more.

## Requirements

- Linux x86_64 with a desktop session (X11, or Wayland through XWayland).
- `libpcap` for packet capture (Arch: `libpcap`, Debian/Ubuntu: `libpcap0.8`, Fedora: `libpcap`).
- `zenity` (or `kdialog`) for file and folder pickers, see [Capture permissions](#capture-permissions-setcap).
- Runtime libraries Avalonia needs: `fontconfig`, `libX11`, `libICE`, `libSM` (present on any desktop install).
- The release tarball is self-contained: **no .NET installation is needed**.
- Optional: `pw-play` (pipewire) or `paplay`/`aplay` for alert sounds, `notify-send` (libnotify) for desktop notifications, `xdg-utils`,
  `wmctrl` (X11 attention request).

## Install

### Tarball (any distribution)

1. Install the dependencies:

   ```sh
   sudo pacman -S --needed libpcap zenity                # Arch / Manjaro / EndeavourOS / Omarchy
   sudo apt install libpcap0.8 zenity                    # Debian / Ubuntu / Mint / Pop!_OS
   sudo dnf install libpcap zenity                       # Fedora
   ```

2. Download the latest release and verify it:

   ```sh
   mkdir -p ~/Downloads/sat && cd ~/Downloads/sat
   base=https://github.com/Fcogarciaro/AlbionOnline-StatisticsAnalysis/releases/latest/download
   curl -LO "$base/StatisticsAnalysisTool-linux-x64.tar.gz"
   curl -LO "$base/StatisticsAnalysisTool-linux-x64.tar.gz.sha256"
   sha256sum -c StatisticsAnalysisTool-linux-x64.tar.gz.sha256      # must print "OK"
   ```

   `-L` is required: GitHub redirects the download. The files are also on the
   [releases page](https://github.com/Fcogarciaro/AlbionOnline-StatisticsAnalysis/releases/latest).

3. Extract and install:

   ```sh
   tar xzf StatisticsAnalysisTool-linux-x64.tar.gz
   cd StatisticsAnalysisTool
   ./install.sh --setcap      # asks for your sudo password once, for the packet capture permission
   ```

4. Start it from your application menu ("Statistics Analysis Tool") or a terminal:

   ```sh
   statisticsanalysistool     # or ~/.local/opt/StatisticsAnalysisTool/StatisticsAnalysisTool if ~/.local/bin is not on PATH
   ```

5. The first start guide asks for the language and the game folder (it is usually detected, see [Game folder](#game-folder)).
   Then start Albion Online and enter a map: the dashboard and damage meter fill from the live traffic.

`install.sh` copies the tool to `~/.local/opt/StatisticsAnalysisTool`, links `~/.local/bin/statisticsanalysistool`, and installs the desktop entry
and icons under `~/.local/share`. Run `./install.sh --help` for the options.

### Update

Repeat steps 2 and 3 with the new release. `--setcap` is required again: replacing the binary removes its capture permission.
Settings and data are kept.

### Uninstall

```sh
./install.sh --uninstall   # from any extracted release; settings and data stay in ~/.config and ~/.local/share
```

### Arch Linux (PKGBUILD)

```sh
cd packaging/arch
makepkg -si
```

It builds from source (needs `dotnet-sdk` and network access for NuGet), installs to `/opt/statisticsanalysistool`, and the `.install` hook runs
`setcap` after every install and upgrade.

## Capture permissions (setcap)

Reading raw network traffic needs the `CAP_NET_RAW` capability. Grant it once to the binary instead of running the whole tool as root:

```sh
sudo setcap cap_net_raw,cap_net_admin=eip ~/.local/opt/StatisticsAnalysisTool/StatisticsAnalysisTool
```

- **Why not root?** A root process would be a large graphical .NET application with full privileges, and it would write root-owned files into
  your settings folders. File capabilities limit the extra power to packet capture. (`CAP_NET_ADMIN` is optional; it is granted too, as Wireshark does.)
- **Reapply after every update.** Replacing the file removes its capabilities. `install.sh --setcap` and the Arch package do this for you.
- With capabilities the process runs in "secure execution" mode: `LD_LIBRARY_PATH` is ignored for it.
- File capabilities need a filesystem that supports them and no `nosuid` mount on the install directory.
- The tool shows the exact command for your installation when it cannot capture.

## Wayland / Hyprland

- **Tray icon:** needs a StatusNotifierItem host. Waybar: enable the `tray` module. GNOME needs the AppIndicator extension. Without a tray host,
  do not enable "Hide in the system tray when minimized".
- **Window rules:** the overlay windows (damage meter, dashboard, item alerts) are normal top-level windows whose titles are localized,
  so read the real class and title with `hyprctl clients` and match those. Example (replace the placeholders):

  ```
  windowrule = float, class:^(<class from hyprctl>)$, title:^(<damage meter title>)$
  windowrule = pin, class:^(<class from hyprctl>)$, title:^(<damage meter title>)$
  ```

  The exact rule syntax depends on your Hyprland version; see the Hyprland wiki for window rules.
- **Attention requests** (`FlashWindow` on alerts) work through `wmctrl` on X11 only; on Wayland use the desktop notification.
- **Clipboard** works while one of the tool's windows exists.
- **File and folder pickers** run through `zenity` (or `kdialog`) because the portal rejects processes with `setcap` capabilities. Install
  one of them (e.g. `sudo pacman -S zenity`) if the dialogs do not open.

## Game folder

The first start guide and the game-data window look for the game and propose the first valid folder. Both the native Linux client and
Steam/Proton or Wine installs are supported:

| Install | Folder to pick |
|---|---|
| Native Linux client | `~/albiononline` (contains `game_x64`) |
| Steam (native or Proton) | `~/.local/share/Steam/steamapps/common/Albion Online`, or the same path in another Steam library |
| Flatpak Steam | `~/.var/app/com.valvesoftware.Steam/.local/share/Steam/steamapps/common/Albion Online` |
| Standalone launcher under Wine | `<prefix>/drive_c/Program Files (x86)/AlbionOnline` (`WINEPREFIX`, `~/.wine`, `~/Games/*`, Bottles) |

Pick the folder that contains `game` (Windows/Steam layout) or `game_x64` (native Linux layout). The running game is detected both as the
native `Albion-Online` process and under Wine/Proton (`Albion-Online.exe`).

## Troubleshooting

| Symptom | Fix |
|---|---|
| No data while playing | Check the capture permission: `getcap ~/.local/opt/StatisticsAnalysisTool/StatisticsAnalysisTool` must show `cap_net_admin,cap_net_raw=eip`. Re-run `./install.sh --setcap` after every update. |
| "Select folder" does nothing or warns about zenity | Install `zenity` (or `kdialog`). |
| Game folder rejected | Pick the folder that contains `game` or `game_x64`, not those folders themselves. |
| No tray icon | Your desktop needs a StatusNotifierItem host (see below). |
| Anything else | Start it from a terminal to see the log, and look at `~/.local/share/StatisticsAnalysisTool/` (logs). |

## Data locations (XDG)

| What | Where |
|---|---|
| `Settings.json` | `$XDG_CONFIG_HOME/StatisticsAnalysisTool` (default `~/.config/StatisticsAnalysisTool`) |
| Backups, user data, logs, game files | `$XDG_DATA_HOME/StatisticsAnalysisTool` (default `~/.local/share/StatisticsAnalysisTool`) |
| Image cache, temp | `$XDG_CACHE_HOME/StatisticsAnalysisTool` (default `~/.cache/StatisticsAnalysisTool`, safe to delete) |
| Autostart entry | `$XDG_CONFIG_HOME/autostart/StatisticsAnalysisTool.desktop` |

## Known differences from the Windows version

See [`src/StatisticsAnalysisTool.Avalonia/PORTING.md`](src/StatisticsAnalysisTool.Avalonia/PORTING.md) for the complete list. In short:
libpcap replaces Npcap and raw sockets, the updater only notifies (it opens the release page), sounds and notifications use external helpers,
and some texts are English only.

## Updates

The tool checks the releases of this fork (tags `linux-v*`) and shows a badge and the release notes. Update by installing the new tarball
(`install.sh`) or rebuilding the package; remember `setcap` (done by `install.sh --setcap` and the Arch package).

## Building from source

```sh
dotnet build src/StatisticsAnalysisTool.Linux.sln
dotnet test src/StatisticsAnalysisTool.Avalonia.Tests        # headless, no display needed
dotnet run --project src/StatisticsAnalysisTool.Avalonia
packaging/linux/build-tarball.sh [VERSION]                   # writes artifacts/StatisticsAnalysisTool-linux-x64.tar.gz(+.sha256)
```

Needs the .NET 10 SDK. The Avalonia project compiles the non-view sources of `src/StatisticsAnalysisTool` in place, so upstream files are not edited.

## Syncing with upstream

```sh
git remote add upstream https://github.com/Triky313/AlbionOnline-StatisticsAnalysis.git   # once
git fetch upstream
git merge upstream/main
```

New upstream source files are linked automatically (the project globs them). Check the build for new Windows-only APIs and update
`PORTING.md` when a file needs a Linux replacement. Upstream Windows workflows stay untouched; `.github/workflows/linux.yml` is the Linux CI.

## Releases

Push a tag `linux-vX.Y.Z`. CI builds `StatisticsAnalysisTool-linux-x64.tar.gz` and its `.sha256` and attaches them to the release.
The version must be numeric (`linux-v0.2.0`, not `linux-v0.2.0-beta`) because the update check compares versions; mark a test build as a
pre-release on GitHub instead. Update `pkgver` and run `updpkgsums` in `packaging/arch` after tagging.
