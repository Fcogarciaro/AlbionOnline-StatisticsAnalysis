# Feature: Avalonia Linux Port

## Objective
Port the WPF app `src/StatisticsAnalysisTool` to Avalonia 11 so it runs natively on Linux with functionality matching the original.

## Problem / Why
The app targets `net10.0-windows` (WPF + WinForms) and uses Windows-only APIs: Npcap/SIO_RCVALL capture, Registry, user32, NotifyIcon, Ookii dialogs and NetSparkle WPF. The core libraries (Network, PhotonPackageParser, Protocol18, Abstractions, Diagnostics, Extractor) already build on Linux.

## Scope
- New `StatisticsAnalysisTool.Core` (net10.0) holding the non-UI logic.
- New `StatisticsAnalysisTool.Avalonia` (net10.0) app with views ported 1:1.
- A Linux platform layer: libpcap capture, XDG paths, tray, autostart, sound, dialogs and notifications.
- Unit tests running on Linux.
- Packaging docs (setcap, libpcap).

## Constraints
- GPL-3.0 fork. Keep the upstream remote for syncing.
- The WPF project stays untouched where possible, so upstream merges remain feasible.
- Artifacts are in English.
- TDD: strict mode is enabled (session config). Runner: `dotnet test` (NUnit). Applies to the logic code. XAML views are verified by build plus a manual run.

## Delivery
- Branch: `feat/avalonia-linux-port`.
- Remote: `origin` = Fcogarciaro/AlbionOnline-StatisticsAnalysis (fork).
- Forecast: far above 400 authored lines (~36k XAML lines to port). Strategy: ask-on-risk. Chain choice: `feature-branch-chain` (picked by Claude at user's request). PR slices target `feat/avalonia-linux-port`, which merges once to `main`.
- Fork created: https://github.com/Fcogarciaro/AlbionOnline-StatisticsAnalysis (public, GPL-3.0).

## Key Decision: Source-linking + WPF compat shim
The Avalonia project links (`<Compile Include>`) the non-view `.cs` files from `src/StatisticsAnalysisTool` instead of moving them. A small compat layer replaces the WPF types used by logic and ViewModels (Visibility, Dispatcher, Application.Current, Brushes, ...). Only views (XAML + code-behind) and Windows-only services are rewritten.
- **Why:** This gives maximum behavior parity, because the same logic code runs on both. Upstream merges also stay cheap, since the upstream files stay untouched.
- **Tradeoff:** The compat shim must track the WPF API surface used by the logic. T02 replaces the old "extract Core" plan.

## Tasks
Each task records its route and trigger.

- [x] T01 Avalonia skeleton project + solution entry (`StatisticsAnalysisTool.Linux.sln`). Route: delegated (writer; writer trigger: 10 files). Commit f9b047e4.
  - Evidence: the Linux sln builds with 0 errors (writer + parent spot check). Spike with LinkWpfSources=true: 468 unique errors (675 before the shims).
  - Assess: high. RDD off, so an independent verifier ran: PASS on build, csproj and collisions.
  - Carried into T02: `InvokeAsync` must return `DispatcherOperation` (BaseViewModel.cs:43,57 uses `.Task`); `BeginInvoke` must unwrap `TargetInvocationException`; touch `UIThread` in Main.
- [x] T02 Complete the WPF compat shim until `LinkWpfSources=true` builds. Covers: DispatcherOperation, Brush/Color, GridLength, IValueConverter, ICollectionView/ListCollectionView, BitmapImage facade, DispatcherTimer and the Properties.Settings shape. Package swap: LiveCharts Avalonia. Stub interfaces for NetSparkle, Ookii, MediaPlayer, Notification.Wpf and Management. Add a test project with TDD on the shim logic. Route: delegated (writer).
  - Commits: 31a48684, f8ababf7, c6c94027, 9cd26d28, 71602d9d.
  - Evidence: Linux sln builds with 0 errors; tests 78/78 (re-run by the parent); upstream diff is empty.
  - Verifier: no build- or test-breaking defects. Scoped correction T02c queued:
    - `IsRoot` should use `Environment.IsPrivilegedProcess`.
    - `BitmapImage.LoadTask` should complete after `Finish`; `ShowDialog` hangs when the dialog task faults.
    - `ShowDialog` should return null when closed without a result.
    - `ListCollectionView` should emit incremental events (no full reset) when unsorted and unfiltered.
- [x] T02c Verifier correction (see above). Route: delegated (writer, together with T03). Commit 70a5c8e0. The 6 RED tests now pass.
- [x] T03 Platform abstractions + Linux impls (tray, autostart, dialogs, sound, flash, notifications, process monitor). Route: delegated (writer).
  - Commits: ef44e024, 1fa12448, 0c84102f, 357bb3d2.
  - Evidence: tests 176/176 (re-run by the parent); upstream diff is empty. Behavior differences are listed in PORTING.md.
  - Open: wire `SystemTrayService` in T09; live desktop verification is pending (T20).
- [x] T03c Verifier correction (commit 54844e73):
  - SoundController can play twice (Exited disposes the process before `Begin*ReadLine`), at SoundController.cs:98-103.
  - PickerHost.FindOwner reads windows off the UI thread.
  - The autostart Exec line needs a double backslash escape; add a `Path=` line and an atomic write.
  - notify-send markup: escape it or pass `--`.
  - Document the logging of notification content.
  - Route: delegated (writer, after T04/T06).
- [x] T04 XDG paths + settings and resources without resx/Settings.settings. Commits 6ee27c55, d641b965.
  - Changes: JSON settings provider; case-insensitive game files.
  - Extractor: its hard-coded `\` paths were fixed (12+/17-). This is a small upstream-adjacent edit with identical behavior on Windows, accepted because extraction fails on Linux without it.
- [ ] T05 Unit tests on net10.0 against Core, green on Linux.
- [x] T06 Linux network capture (libpcap default, capability check, first-run guidance). Commits 4b1ab6cd, 075792d2, 9f74c7d0.
  - Capture: libpcap resolved via `ResolvingUnmanagedDll`; Ethernet, VLAN, SLL, SLL2 and raw IP decoding; a CapEff capability check with setcap guidance.
  - Evidence: tests 243/243 (re-run by the parent). Live capture is untested, because there is no sudo here.
  - Open for T07/T10: the Npcap link in App startup, the provider labels in Settings, and a `CaptureGuidance.PermissionMessage` call in the first-start view.
- [x] T06c Verifier correction, no blocking defects. Commit 25668e70. Route: delegated (writer, after T07).
  - The capability gate requires only CAP_NET_RAW, so a user with only cap_net_raw is no longer blocked. NET_ADMIN is optional; the setcap text keeps both, as Wireshark's dumpcap does.
  - `JsonSettingsProvider.SetPropertyValues` catches and logs I/O errors.
  - `GameFile` resolves case ambiguity deterministically.
- [x] T07 App shell: App.axaml, startup flow, single instance, exception handling. Commits 986b16e8, 1ee55666, b7ff7810.
  - Evidence: tests 257/257 (re-run by the parent). A headless RunAsync smoke test reaches the real MainWindowViewModel and TrackingController. There is no display in the agent shell, so the live run is pending with the user.
  - Deviations: a single-instance lock file is added (upstream has none); the Npcap dialog is replaced by a capture-guidance MessageBox.
  - Open: T08 must define the `Color.*` resources (TrackingController NREs without them); the T10 placeholders are FirstStartGuide, GameDataPreparation, LanguageSelection, Dialog, ItemAlert and EventValidation.
- [x] T08 Theme: Colors, MainStyles, shared styles; DataTriggers → classes; converters; custom controls. Commits 2f25d09c, 3f97bb9a, e0d66226.
  - Convention (see PORTING.md): keyed Style → ControlTheme with `Theme={StaticResource}`; triggers → pseudoclasses; DataTriggers → `Classes.x` with ObjectConverters.Equal; Visibility → VisibilityToBoolConverter.
  - Feature dictionaries are ported with their views.
- [x] T09 MainWindow + navigation, footer, bars, tray, window chrome. Commit 3a2a5c07.
- [x] T10 Secondary windows: Settings, FirstStartGuide, Language, GameDataPreparation, Dialog, Update. Commits 6b04180d, 92f63d85, 20421c20, a04c99c3.
  - Evidence: tests 497/497 (re-run by the parent); upstream diff is empty. Gaps are listed in PORTING.md.
  - Open:
    - live Wayland/Hyprland chrome test (user);
    - Settings still uses Windows wording ("Start with Windows");
    - one flaky "Dispatcher shut down" test cascade was observed once.
- [x] T11 Simple tabs: Guild, Party, StorageHistory, MapHistory, ItemSearch, Donation, Thanks, PlayerInformation. Commits 46ef5cd3..0ccc77a8.
  - Evidence: tests 556/556 (re-run by the parent).
  - Changes: ItemSearch uses a DataGrid (Avalonia.Controls.DataGrid 11.3.12); ItemDetails stays a placeholder until T15.
- [x] T12 Logging + EventValidation (virtualized lists). Commit 1f5d2e51; a test with 5000 events realizes fewer than 200 rows.
- [x] T13 Dashboard, DashboardLoot, Gathering, Crafting (LiveCharts Avalonia). Commits 64c234e8, 8335c7f0, 304b36ff.
  - Evidence: tests 579/579 (re-run by the parent).
  - LossExplorer moved to T14.
- [x] T14 DamageMeter (+ window), Dungeon, LossExplorer. Commits c785ea20, 9b3ddd6f.
- [x] T15 TradeMonitoring (+ Skia heatmap), ItemDetails. Commits 073f2ffa, 9fcdf892.
  - Evidence: tests 603/603 (re-run by the parent); zero placeholders remain; upstream diff is empty.
  - Open:
    - an intermittent full-run flake;
    - the ItemDetails test fires real network price requests.
- [x] T16 Icon packs (FontAwesome, GameIcons). Commits e3b67184, 30485bf0.
  - Evidence: tests 398/398 (re-run by the parent); upstream diff is empty.
  - Licence files must ship in T19.
- [x] T17 Notifications, sounds, alert windows, clipboard. Commits df24e9d6, 6a2985bc. The updater is notify-only (reasons in PORTING.md).
- [x] T18 Linux integration: autostart .desktop, Proton/Wine process detection, attention request. Commit 5aceacb1 (Linux wording).
- [x] T19 Packaging (PKGBUILD/AppImage), README-linux, CI. Commits 0fa54d7f, 1908539b; test hygiene in 4f88da54 and cbf38bac.
  - Evidence: tests 665/665 across 5 runs (writer) and 1 run (parent); the tarball builds and its sha256 verifies.
  - Open: the CI run is unverified; the icons are upscaled.
- [x] T19c Verifier correction: install.sh rejects a dangerous INSTALL_DIR (/, $HOME, a relative path, sed metacharacters, a path inside the tarball, a non-empty foreign directory). Verified with a sandbox HOME run. Other findings are residual: actions pinned to @v4, PKGBUILD sha SKIP until the first tag, unguarded async void handlers that mirror upstream.
- CI: run 42058043189 succeeded (build + tests on GitHub Actions).
- [ ] T20 Parity pass versus the original screens. Needs the live user test on Hyprland.
  - Release linux-v0.1.0 (prerelease) crashed on start: Program.Main touched Dispatcher.UIThread before platform init (PlatformNotSupportedException in MainLoop). Fixed in 9e90977f; reproduced and verified against a live X display (:0); tests 665/665. Re-released as linux-v0.1.1 (prerelease); the downloaded tarball verifies and starts to the first-start guide.

## Acceptance Criteria
- `dotnet build` and `dotnet test` succeed on Linux.
- The app launches, captures Albion traffic via libpcap and shows every original tab with working data.

## Progress / Evidence
- Baseline: the 6 core libs build on Linux with SDK 10.0.403 (mise).

## Next Step
T01.
