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
- [ ] T02 Complete the WPF compat shim until `LinkWpfSources=true` builds. Covers: DispatcherOperation, Brush/Color, GridLength, IValueConverter, ICollectionView/ListCollectionView, BitmapImage facade, DispatcherTimer and the Properties.Settings shape. Package swap: LiveCharts Avalonia. Stub interfaces for NetSparkle, Ookii, MediaPlayer, Notification.Wpf and Management. Add a test project with TDD on the shim logic. Route: delegated (writer).
- [ ] T03 Platform abstractions + Linux impls (tray, autostart, dialogs, sound, flash, notifications, process monitor).
- [ ] T04 XDG paths + settings and resources without resx/Settings.settings.
- [ ] T05 Unit tests on net10.0 against Core, green on Linux.
- [ ] T06 Linux network capture (libpcap default, capability check, first-run guidance).
- [ ] T07 App shell: App.axaml, startup flow, single instance, exception handling.
- [ ] T08 Theme: Colors, MainStyles, shared styles; DataTriggers → classes; converters; custom controls.
- [ ] T09 MainWindow + navigation, footer, bars, tray, window chrome.
- [ ] T10 Secondary windows: Settings, FirstStartGuide, Language, GameDataPreparation, Dialog, Update.
- [ ] T11 Simple tabs: Guild, Party, StorageHistory, MapHistory, ItemSearch, Donation, Thanks, PlayerInformation.
- [ ] T12 Logging + EventValidation (virtualized lists).
- [ ] T13 Dashboard, DashboardLoot, Gathering, Crafting (LiveCharts Avalonia).
- [ ] T14 DamageMeter (+ window), Dungeon, LossExplorer.
- [ ] T15 TradeMonitoring (+ Skia heatmap), ItemDetails.
- [ ] T16 Icon packs (FontAwesome, GameIcons).
- [ ] T17 Notifications, sounds, alert windows, clipboard.
- [ ] T18 Linux integration: autostart .desktop, Proton/Wine process detection, attention request.
- [ ] T19 Packaging (PKGBUILD/AppImage), README-linux, CI.
- [ ] T20 Parity pass versus the original screens.

## Acceptance Criteria
- `dotnet build` and `dotnet test` succeed on Linux.
- The app launches, captures Albion traffic via libpcap and shows every original tab with working data.

## Progress / Evidence
- Baseline: the 6 core libs build on Linux with SDK 10.0.403 (mise).

## Next Step
T01.
