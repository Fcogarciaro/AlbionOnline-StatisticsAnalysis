# Porting notes: WPF to Avalonia (Linux)

`StatisticsAnalysisTool.Avalonia` compiles the non-view `.cs` files of `src/StatisticsAnalysisTool` in place
(`LinkWpfSources=true`, the default). Upstream WPF files are never edited, so upstream merges stay cheap.
Anything the Linux build cannot use is excluded in the csproj and replaced by a file with the same public
surface under `Linux/`, or covered by a WPF-shaped shim under `Compat/` (namespaces match WPF).

## Excluded files and their Linux replacements

| WPF file (excluded) | Reason | Replacement | Behavior difference |
|---|---|---|---|
| `Updater/AutoUpdateController.cs`, `Updater/UpdateDownloadSession.cs` | NetSparkleUpdater.UI.WPF, Authenticode, WPF update window | `Linux/Updater/AutoUpdateController.cs` | No update check or notification. TODO(T03/T17): GitHub releases check. |
| `Updater/AuthenticodeSignatureVerifier.cs` | Windows Authenticode | none (only used by the excluded controller) | n/a |
| generated `AutoUpdateSecurityBuildSettings.g.cs` | MSBuild-generated in the WPF project | `Linux/Updater/AutoUpdateSecurityBuildSettings.cs` | All flags off. |
| `Common/AppDataPaths.cs` | `LocalApplicationData` is the only root; everything (incl. `Settings.json`) sits in it | `Linux/Common/AppDataPaths.cs` (copy, keep in sync) | Same members and `Instances/<id>/...` sub-structure. `Settings.json` goes to `$XDG_CONFIG_HOME/StatisticsAnalysisTool` (default `~/.config`); `Backups`, `UserData-*`, `logs`, `GameFiles` to `$XDG_DATA_HOME/StatisticsAnalysisTool` (default `~/.local/share`, where .NET already pointed `LocalApplicationData`); `temp`, `ImageResources`, `SpellImageResources` to `$XDG_CACHE_HOME/StatisticsAnalysisTool` (default `~/.cache`, safe to delete). Relative XDG values are ignored per spec. Copy the folders between machines by recreating the same `Instances/<id>` layout. Upstream has no portable mode, so none is offered. Extra member `UserSettingsFile`. |
| `Network/PacketProviders/SocketsPacketProvider.cs` | Raw sockets with Windows `SIO_RCVALL` | `Linux/Network/SocketsPacketProvider.cs` (subclass of the libpcap provider) | The `Sockets` choice (enum value 1, may come from a Windows settings file) runs libpcap. The default `PacketProvider` is already `Npcap` = libpcap. The settings UI still lists both options (`Sockets (must be run as admin)` / `Npcap`); T10 should relabel them. |
| `Network/PacketProviders/LibpcapPacketProvider.cs` | Frame parsing knows only Ethernet and raw IP; opens every up non-loopback device | `Linux/Network/LibpcapPacketProvider.cs` (copy, keep in sync) | `TryReadNetworkLayer` delegates to `Linux/Capture/LinkLayerDecoder.cs`: Ethernet (with 802.1Q/QinQ tags), Linux cooked SLL (113) and SLL2 (276), raw IP. The `any` pseudo device (cooked headers) is only opened when no concrete interface opened, and is not listed as a selectable device, to avoid every packet arriving twice. Upstream device filtering (skip loopback and down devices, honor `NetworkDevices` selection) works unchanged on Linux: there are no NPF names. The BPF default filter (UDP 5055/5056/5058 plus IPv4 fragments) compiles for every link type. Log text still says "Npcap". |
| `Core/ApplicationCore.cs` | `WindowsPrincipal` (throws `PlatformNotSupportedException` on Linux) | `Linux/Core/ApplicationCore.cs` | `IsAppStartedAsAdministrator()` means root, or `CAP_NET_RAW` in `CapEff` (`CAP_NET_ADMIN` is optional; the setcap text grants both like Wireshark dumpcap) of `/proc/self/status` (`Linux/Capture/CapturePrivilege.cs`). |
| `Localization/LocalizationController.cs` | Npcap texts | `Linux/Localization/LocalizationController.cs` (copy, one added call) | `Translation()` asks `CaptureGuidance` first. Overridden keys (English only): `NPCAP_INFO_DIALOG_TITLE/MESSAGE/LINK_TEXT`, `ERR_NPCAP_DLL_MISSING`, `MAKE_SURE_YOU_HAVE_INSTALLED_NPCAP`, and, when the process lacks the capture capabilities, `NO_LISTENING_ADAPTERS`, `ERR_NPCAP_OPEN_FAILED`, `START_APPLICATION_AS_ADMINISTRATOR`. These carry the exact `sudo setcap cap_net_raw,cap_net_admin=eip <path>` command for the real process (under `dotnet X.dll` the path is the apphost next to the dll, never the dotnet host). |
| `Common/AlbionGameProcessMonitor.cs` | `Process.GetProcessesByName` misses the game under Wine/Proton (15-char `comm` such as `Albion-Online.e`, wine loader as the exe) | `Linux/Common/AlbionGameProcessMonitor.cs` | Same class, events and 2 s poll; detection scans `/proc/<pid>/comm` and `argv[0]` for `Albion-Online` / `Albion-Online.exe` (`AlbionProcessMatcher`). Processes of other users are matched too (comm and cmdline are world-readable) |
| `Common/SoundController.cs` | `System.Windows.Media.MediaPlayer` | `Linux/Common/SoundController.cs` | Sound discovery identical (`Sounds/*.wav` are copied to the output from the WPF project). Playback runs the first available of `pw-play`, `paplay`, `aplay` as a detached process (output is not redirected). Volume is applied by `pw-play` and `paplay`; `aplay` cannot scale volume. Volume 0 plays nothing (as in WPF). With no player installed a single warning is logged. |
| `Common/SystemInfo.cs` | WMI (System.Management), `WindowsPrincipal` | `Linux/Common/SystemInfo.cs` | CPU from `/proc/cpuinfo`, admin means root, GPU unknown. TODO(T03). |
| `Common/Utilities.cs` | user32 `FlashWindowEx`, `WindowInteropHelper`, `SystemParameters` | `Linux/Common/Utilities.cs` (copy, keep in sync) | `FlashWindow` asks the X11 window manager for `_NET_WM_STATE_DEMANDS_ATTENTION` through `wmctrl` (skipped when the window is active); a no-op on Wayland or without `wmctrl` (Avalonia has no urgency API). `count` is ignored; `StopFlashingWindow` clears the state. Centering uses Avalonia screens. |
| `Notification/SatNotificationManager.cs` | Notification.Wpf toasts | `Linux/Notification/SatNotificationManager.cs` | Same API and the same setting gates (`IsNotificationTrackingStatusActive` for success/error/tracking, `IsNotificationFilterTradeActive` for trades). The title and message of every notification are written to the log (`Notification [kind] title: message`); the body is markup-escaped (`&`, `<`, `>`) before it reaches `notify-send`, because notification servers may render it. Toasts are Avalonia `WindowNotificationManager` cards (bottom right, 5 s, the Notification.Wpf default) while the main window is visible. When the window is hidden or unfocused and `notify-send` exists, a desktop notification is sent too (error = critical urgency). Differences: no per-row text trimming (`RowsCount`), colors come from the Avalonia theme instead of the `SolidColorBrush.Notification.Background.*` keys. |
| `Localization/Culture.cs` | `FrameworkElement.LanguageProperty` (xml:lang) | `Linux/Localization/Culture.cs` | Sets thread cultures only; Avalonia has no per-element language. |
| `Common/WindowsStartupService.cs` | HKCU Run registry key | `Linux/Common/WindowsStartupService.cs` | Writes (atomically, temp file + move) or removes `$XDG_CONFIG_HOME/autostart/StatisticsAnalysisTool.desktop`, with `Path=` set to the executable's directory (default `~/.config/autostart`, relative XDG values ignored per spec). One fixed file name, whereas Windows used one registry value per install path (`InstanceId`); two installs share the entry. `Exec` is the process path, or `dotnet <dll>` when launched through the dotnet host. |
| `SystemTrayService.cs`, `SystemTrayMenuRenderer.cs` | WinForms `NotifyIcon` | `Linux/Common/SystemTrayService.cs` (renderer dropped) | Same menu (Open, Activate/Deactivate tracking, Exit) and minimize-to-tray logic on an Avalonia `TrayIcon` (needs a StatusNotifierItem host; GNOME needs an extension). Takes `Avalonia.Controls.Window`. Single click restores (no double-click event). The first-hide hint is a `notify-send` notification (no balloon-tip click). Native menus are not themed. The service needs `MainWindowViewModel`, so it is constructed by the real `MainWindow` (T09), not by `LinuxApp`. Icon: `sat-icon.ico` as Avalonia resource |
| `Shortcut/**` | Windows shell | none | n/a |
| `Common/Controls/**` (including `FontAwesomeImage`), `TextBoxHelper.cs`, `ScrollViewerExtensions.cs`, `ProgressBarSmoother.cs`, `CultureAwareBinding.cs` | WPF controls and attached properties | `Linux/Controls/{DigitsTextBox,ResponsiveUniformGrid,SpellDescriptionTextBlock,FontAwesomeImage}.cs`, `Linux/Common/{TextBoxHelper,NumericInputRules,ScrollViewerExtensions,ProgressBarSmoother,CultureAwareBinding}.cs` (same names and namespaces) | See "Converters and controls" and "Icons" below. |
| `Common/Validation/**`, `Converters/ValidationErrorConverter.cs` | WPF validation classes (`ValidationError`, `IDataErrorInfo` templates) | none yet | T10 (settings window) re-creates validation with Avalonia `DataValidationErrors`. |
| `Views/**`, `UserControls/**`, `*.xaml.cs`, `App.xaml.cs`, `Properties/AssemblyInfo.cs`, `Properties/Resources.Designer.cs` | Views, application shell, resx image resources | ported views (T07-T15) | See view placeholders below. |
| `Models/StatPricesTotal.cs`, `Network/Handler/JoinHandler.cs`, `Network/SilverCountUpTimer.cs` | Excluded since T01 | n/a | n/a |

## Single instance

Upstream has no single-instance check (and no activation of an existing instance); `AppInstance.InstanceId` (hash of the
install directory, `Debug` in debug builds) only separates data directories. Two processes with one id would write the
same data, so `Program.Main` takes an exclusive lock file `instance-<id>.lock` in `$XDG_RUNTIME_DIR/StatisticsAnalysisTool`
(absolute value only, else the cache root) through `Linux/Startup/SingleInstanceLock.cs` (`FileShare.None`, kernel flock,
released when the process dies). The second process prints a message on stderr and exits with code 1; it does not
activate the first window. Different installs (different ids) can run together, as on Windows. If the lock file cannot be
created the tool starts without the guard.

## Package and API replacements

| WPF | Linux |
|---|---|
| `LiveChartsCore.SkiaSharpView.WPF` 2.0.5 | `LiveChartsCore.SkiaSharpView.Avalonia` 2.0.5 (same release) |
| `SkiaSharp` 4.151.1 | `SkiaSharp` 3.119.0 + `SkiaSharp.NativeAssets.Linux`: LiveCharts 2.0.5 requires 3.119.0 and Avalonia 11.3 supports 3.x |
| `DSaladin.FontAwesome.WPF` `FontAwesomeIcon` | `Linux/Icons/FontAwesomeIcon.cs`, an enum generated from the 6.7.1 package (identical names and values); the icon control is T16 |
| `Ookii.Dialogs.Wpf` (`VistaOpenFileDialog`, `VistaFolderBrowserDialog`) | `Compat/VistaOpenFileDialog.cs`, `Compat/MessageBox.cs`: Avalonia `StorageProvider` pickers, blocking via a nested dispatcher frame. Returns false when no visible window exists. `ShowNewFolderButton` is ignored (the portal picker always offers it) |
| `Microsoft.Win32.SaveFileDialog`, `System.Windows.MessageBox` | `Compat/MessageBox.cs`: `SaveFileDialog` uses the save picker (DefaultExt appended when the name has no extension, as Win32 AddExtension); `MessageBox.Show` opens a modal `MessageBoxDialog` (`Compat/Dialogs/`). Button labels are English; closing a YesNo box with Escape or the close button returns No (WPF disables closing it) |
| `Properties/Settings.Designer.cs` | Linked unchanged. The stock `LocalFileSettingsProvider` cannot save on Linux and `Upgrade()` throws, so `Linux/Settings/JsonSettingsProvider.cs` is attached to the partial `Settings` class. User-scoped values are stored as JSON in `$XDG_CONFIG_HOME/StatisticsAnalysisTool/Instances/<id>/UserSettings.json`; `Upgrade()` is a no-op (no earlier versions). Only `UpgradeRequired` is ever written; the real settings stay in `Settings.json` (`SettingsController`). |
| `System.Drawing.Icon` (exe icon in Settings) | `System.Drawing.Common` compiles; the call throws on Linux and is caught, so no icon is shown |
| `pack://application:,,,/...` URIs | `pack` scheme registered at startup (`Compat/PackUri.cs`); `BitmapImage` and `Application.GetResourceStream` map them to `avares://` |

## Compat shims (`Compat/`, namespaces as in WPF)

Dispatcher, DispatcherOperation, DispatcherTimer, Application (Resources, TryFindResource, MainWindow, Windows,
GetResourceStream), Window (derive ported views from `System.Windows.Window`), Color, Colors, Brush,
SolidColorBrush, Brushes, ImageSource, BitmapSource, BitmapImage, BitmapFrame, PngBitmapEncoder, GridLength,
IValueConverter (inherits the Avalonia contract), IMultiValueConverter (`AsAvalonia()` adapter), Binding.DoNothing,
BindingOperations, ListCollectionView (filter, sort, CustomSort, live shaping, currency), CollectionViewSource,
CommandManager, Clipboard, XmlLanguage, DataTemplate, DataTemplateSelector.

`Application.Resources` returns Avalonia colors and solid brushes as the WPF-shaped `Color` / `SolidColorBrush`,
so linked code casts them unchanged. The Avalonia theme must define the `Color.*` and `SolidColorBrush.*` keys (T08).

## Startup flow (`Linux/Startup/AppStartup.cs`)

`LinuxApp.OnFrameworkInitializationCompleted` starts `AppStartup.RunAsync()` (the async-void `OnStartup` of
`App.xaml.cs`, same steps in the same order, same fatal-error path). Differences:

- `ShutdownMode.OnExplicitShutdown` until the main window exists, then `OnMainWindowClose`, through
  `IClassicDesktopStyleApplicationLifetime`. `OnExit` is the lifetime `Exit` event and `SessionEnding` is
  `ShutdownRequested`; both run the same cleanup (notifications off, updater disposed, `PrepareForShutdown`,
  `CriticalData.Save`, backup), which runs once.
- The Npcap dialog is a message box (`CaptureStartupDialog`): the link is the capabilities(7) man page, and when the
  process lacks `CAP_NET_RAW` the exact setcap command is part of the text even if libpcap lists devices.
  The same guidance is also logged as a warning on every start. Shown once, like upstream (`IsNpcapInfoDialogShownOnStart`).
- `SatNotificationManager` is created without a dispatcher argument; `MainWindow` is the real view since T09.
- `AppStartup.ApplicationUptime` replaces `App.ApplicationUptime` for the T09 footer.
- Placeholders still standing for T10: `FirstStartGuideWindow` (completes at once, does not persist the flag),
  `GameDataPreparationWindow` (so a first run without a game folder ends in an early shutdown), `LanguageSelectionWindow`
  (needed when a completed guide left no culture), `DialogWindow` (the info dialog uses `MessageBox`), `ItemAlertWindow`,
  `EventValidationWindow`.
- The theme (T08, `Styles/`) provides the `Color.*` / `SolidColorBrush.*` keys that view models read (for example
  `Color.Accent.Blue.2`, `Color.Text.1`); the startup test no longer defines workarounds.

## Theme (`Styles/`, T08)

- `Styles/Colors.axaml` is `Styles/Colors.xaml` with only the namespace changed: the same 502 keys and values
  (`ThemeColorsTests` compares them with the upstream file and checks every literal `Color.*` / `SolidColorBrush.*`
  key used by the linked code). Edit `Colors.xaml` upstream first, then re-sync this file.
- `Styles/Theme.axaml` is the single entry point (`App.axaml` includes it after `FluentTheme`; the headless `TestApp`
  does the same). Each dictionary is merged there. Rendering is verified by headless tests (`Theme/SharedStylesTests.cs`).
- Ported so far (shared, non-feature): `Colors`, `MainStyles`, `HintBarStyles`, `InfoWindowStyles`, `SpellStyles`. Each feature
  dictionary is ported together with the views that use it: `Dungeon`, `Trade`, `Dashboard`, `DamageMeter`, `Gathering`,
  `ItemDetails`, `Tracking`, `Guild`, `Party`, `StorageHistory`, `Donation`, `Thanks`, `ItemSearch`, `PlayerInformation`,
  `Settings`, `EventValidation`. `MainWindowStyles` and `TabControlStyles` (navigation, header, FontAwesome icons) belong
  to T09 and `WindowButtonStyle` in `MainStyles` (Webdings glyphs) is ported with the window chrome.

### Style mapping (rules ported views must follow)

| WPF | Avalonia |
|---|---|
| `Style x:Key="K" TargetType="T"` (keyed) | `ControlTheme x:Key="K" TargetType="T"`; the view writes `Theme="{StaticResource K}"` instead of `Style="{StaticResource K}"` (also for `TextBlock`, `Border`, `Grid`, `Label`, `Window`). |
| `BasedOn="{StaticResource K2}"` | `BasedOn="{StaticResource K2}"` on the ControlTheme. A theme for a templated control (`Label`, `Window`, `TabControl`, `ComboBoxItem`, ...) that does not bring its own template must say `BasedOn="{StaticResource {x:Type T}}"` (the Fluent theme), otherwise the control has no template. |
| `Style TargetType="T"` without key and with a `Template` (Button, CheckBox, RadioButton, TabItem) | `ControlTheme x:Key="{x:Type T}"`: replaces the Fluent theme for every `T` without `Theme=`. |
| `Style TargetType="T"` without key, only property setters (Label, TextBox, ListBox, ComboBox, ScrollBar, ContextMenu, ProgressBar, TabControl) | also `ControlTheme x:Key="{x:Type T}" BasedOn="{StaticResource {x:Type T}}"` (a `<Style Selector="T">` would beat every keyed theme, because style selectors outrank theme setters; WPF lets an explicit Style replace the implicit one). |
| `{StaticResource SolidColorBrush.*}` inside styles | `{DynamicResource ...}` (the files are loaded separately). Views may keep `StaticResource` for keys that exist when the view is created. |
| `Trigger IsMouseOver/IsPressed/IsEnabled=False/IsChecked/IsSelected/IsFocused/IsKeyboardFocused` | nested `<Style Selector="^:pointerover|:pressed|:disabled|:checked|:selected|:focus">`; `IsChecked={x:Null}` is `:indeterminate`. |
| `Setter TargetName="N"` | `^:state /template/ Type#N`; setters without `TargetName` go on `^:state`. |
| `ControlTemplate.Triggers` / `Style.Triggers` with `DataTrigger` | a converter on the binding (`IsVisible="{Binding X, Converter=...}"`), or a class set from the view model (`Classes.name="{Binding Flag}"`) with `<Style Selector="^.name">`. |
| `FocusVisualStyle`, `OverridesDefaultStyle`, `SnapsToDevicePixels` | dropped (`UseLayoutRounding` is on by default; Fluent draws the focus adorner). |
| `Visibility="{Binding V}"` where `V` is the shim `Visibility` | `IsVisible="{Binding V, Converter={x:Static conv:VisibilityToBoolConverter.Instance}}"` (see converters below). A `bool` property binds to `IsVisible` directly. |
| `ListView` + `GridView`, `GridViewColumnHeader`, `ScrollViewer` `GridView.GridViewScrollViewerStyleKey` | not ported: no equivalent. Use `DataGrid` (or `ListBox`) in the view. |
| Glyph fonts (`Webdings`, `Segoe MDL2 Assets`) | not on Linux: draw a `Path` or use the FontAwesome icon (T16). |
| `{x:Type controls:X}` / `xmlns:controls="clr-namespace:StatisticsAnalysisTool.Common.Controls"` | unchanged (the Avalonia controls keep names and namespaces). |
| `x:CompileBindings` | style dictionaries set `x:CompileBindings="False"` (the project compiles bindings by default and style templates have no `x:DataType`); views declare `x:DataType` per view. |

### Icons (T16)

- **Font Awesome**: `fa:FontAwesomeImage` (`xmlns:fa="clr-namespace:StatisticsAnalysisTool.Common.Controls"`, same properties as in
  WPF: `Icon`, `Foreground`, `Spin`, `SpinDuration`, `Width`/`Height`) draws the glyph outline of the embedded Font Awesome Free 6.7.2
  fonts (`Assets/Fonts/fa-{solid-900,regular-400,brands-400}.ttf`), stretched uniformly into the control and filled with
  `Foreground` (inherited like a TextBlock). `Icon="SolidCircleInfo"` etc. use the existing `DSaladin.FontAwesome.WPF.FontAwesomeIcon`
  enum; `Linux/Icons/FontAwesomeGlyphs.g.cs` maps each of its 2059 members to style + codepoint (generated from
  `@fortawesome/fontawesome-free@6.7.2/metadata/icons.yml`; `Solid0` shares the value 0 with `None`, so `None` renders nothing). Tests
  check that every member and every `Icon="..."` used in the upstream XAML resolves and that every codepoint exists in its font.
  Without Width/Height an icon is 16x16. `Spin` is a 60 Hz timer rotation that runs only while the control is on screen.
  Enum-valued bindings (`Icon="{Binding X}"`) work unchanged. Fonts: SIL OFL 1.1, icons CC BY 4.0; the license is
  `Assets/Fonts/FontAwesome-LICENSE.txt` and must ship with the package (T19).
- The three `MainStyles` styles that use the icon are in `Styles/FontAwesomeStyles.axaml`: `Style.Button.Icon.White|Black`
  (`:disabled` replaces the "ancestor Button disabled" DataTrigger) and `App.TrackingStateButton` (two icon/text pairs switched by
  `:checked` instead of setters on template parts).

- **MahApps GameIcons** (`iconPacks:PackIconGameIcons`, `MahApps.Metro.IconPacks.GameIcons` 6.2.1 is WPF-only): same name, namespace and
  `Kind` values (`xmlns:iconPacks="clr-namespace:MahApps.Metro.IconPacks"`), implemented by `Linux/Controls/PackIconGameIcons.cs`. Only the
  12 kinds the upstream XAML uses exist (`Awareness`, `Broadsword`, `BulletImpacts`, `CrossedSwords`, `CrystalWand`, `GooeyImpact`,
  `Healing`, `Hourglass`, `Info`, `ShieldDisabled`, `SkullCrack`, `SnakeBite`); `GameIconsTests` fails if the upstream XAML starts using
  another one. Path data (512x512 view box, stretched uniformly, default size 26 like MahApps) is in `Linux/Icons/GameIconsData.g.cs`,
  extracted from the `Icons.json` resource embedded in the NuGet package (MIT). To add a kind, add it to `PackIconGameIconsKind` and the
  data file. The icons are game-icons.net artwork under CC BY 3.0: attribution is in `Assets/GameIcons-NOTICE.txt` (ship it, T19).
  `Style TargetType="{x:Type iconPacks:PackIconGameIcons}"` becomes a ControlTheme with `TargetType="iconPacks:PackIconGameIcons"`.
  WPF `DataTrigger`/`Trigger` setters of `Kind` (DamageMeterControl) become `Classes` plus `Style Selector` setters of `Kind`.

### Converters and controls (T08)

- Upstream `Common/Converters/*.cs` (`ZeroToDash`, `ValuePerHour`, `ValueAvg`, `ShortNumber`, `Rounding`, `SecondsToTimeFormat`,
  `IntToStringOrQuestionMark`, `EnumToIntOrQuestionMark`, `DurationSecondsToCompactString`) are linked unchanged: they implement
  the compat `IValueConverter`, which inherits the Avalonia contract, so `Converter={StaticResource ...}` works (tested through real
  bindings). Declare them in the view as in WPF (`<conv:ZeroToDashConverter x:Key="..."/>`).
- **Visibility**: Avalonia's `IsVisible` is a `bool`. For view-model properties of the shim type `Visibility` bind
  `IsVisible="{Binding X, Converter={x:Static conv:VisibilityToBoolConverter.Instance}}"` (`.Inverted` flips it;
  booleans pass through, so it also replaces `BooleanToVisibilityConverter`). `Hidden` and `Collapsed` both map to hidden.
  `Visibility="Collapsed"` literals become `IsVisible="False"`.
- **`CultureAwareBinding`**: a markup extension returning a `Binding` with `ConverterCulture = CurrentCulture`:
  `{common:CultureAwareBinding Path, StringFormat='{}{0:N0}', FallbackValue=0}`. `IsAsync` does not exist in Avalonia: drop it.
- **`TextBoxHelper`** (`OnlyNumeric` = `Double|Int|Uint|Natural`, `DefaultValue`, `EvenOddConstraint`): same XAML. Typed text is
  filtered on `TextInput`; paste is intercepted (`PastingFromClipboard`) and re-applied only if the resulting text is valid. The
  pending-decimal rule is unchanged (`1.5.` is accepted, as upstream). `DefaultValue` is applied from `TextChanged` (a posted job).
- **`DigitsTextBox`** keeps its name and namespace and reuses the `TextBox` theme (`StyleKeyOverride`), so there is no separate style.
  `ResponsiveUniformGrid` and `SpellDescriptionTextBlock` use `StyleKeyOverride` the same way.
- **`ProgressBarSmoother.SmoothValue`**: 250 ms linear interpolation by a 60 Hz `DispatcherTimer`, restarting from the current value.
- **`ScrollViewerExtensions.AlwaysScrollToEnd`**: deliberate difference. The WPF condition `Math.Abs(...) < 0` can never hold, so it
  scrolled to the end once; the port follows new content while the view was at the end and stops when the user scrolls away.
- **`VirtualizingWrapPanel`** (WpfToolkit, 10 views) has no Avalonia package: ported views use `ItemsRepeater` with a `WrapLayout`
  (or `UniformGridLayout`) inside a `ScrollViewer`, or a `ListBox` with `ItemsPanel = WrapPanel` for short lists. The
  `VirtualizingPanel.*` attached properties are dropped. Decide per view (T11/T12/T14).
- Tests for views can load snippets with `AvaloniaRuntimeXamlLoader` (see `Theme/SharedStylesTests.cs`, `Controls/ControlsRenderTests.cs`).

## Ported views

- `Views/ToolLoadingWindow.axaml` (T07): 600x36 borderless progress bar with the status text, draggable. Colors
  (`Background.1`, `Text.1`) are inlined until the theme lands (T08); it uses the Fluent progress bar.

## Exception handling

`Linux/Startup/GlobalExceptionHandlers.cs` ports the hooks of `App.xaml.cs`: `AppDomain.UnhandledException` and
`TaskScheduler.UnobservedTaskException` identically, `DispatcherUnhandledException` as
`Dispatcher.UIThread.UnhandledException` (log fatal, flush, not handled). Dropped as WPF-specific: the clipboard
`COMException` (-2147221040) workaround and the LiveCharts `CompositionTargetTicker` disposal NRE.

## Main window and chrome (T09)

- `Views/MainWindow.axaml(.cs)` ports `MainWindow.xaml(.cs)` 1:1 (header, status, tracking row, party row, stats drop-down, 16 tabs,
  footer, hint bars). `x:DataType` is `MainWindowViewModel`; `Translation.X` bindings read the static members through the instance
  path, which compiled bindings accept. Differences: `Visibility` bindings use `VisibilityToBoolConverter`; the design-time
  `d:ItemsControl.ItemsSource` array is dropped; `Hyperlink`s of the footer are clickable grids (`ExternalLink.Open`, http(s) only,
  `xdg-open`); `Label ContentStringFormat` is a `Binding StringFormat`; the unsupported-OS warning is never shown (upstream tests
  `Environment.OSVersion.Version.Major < 10`, which is the kernel version on Linux); `InitWindow` tests Left and Top for the
  centering (upstream tests Left twice); `MaxHeight = WorkingArea` on maximize is dropped (the window manager maximizes into the
  working area); the party circles get their template through `ItemTemplate` (Avalonia resources cannot hold an implicit
  `DataType` template).
- **Chrome**: theme `WindowChrome` (`ExtendClientAreaToDecorationsHint`, `NoChrome`) plus `MainWindowStyle`. The header grid starts a
  move with `Window.BeginMoveDrag` (a double click toggles maximize, as upstream); if the compositor rejects the request
  (`InvalidOperationException`/`NotSupportedException`) `WindowChromeController` falls back to `ManualWindowDrag`
  (window position follows the pointer delta). Resize uses eight `Border` handles in `ResizeHandles` calling `BeginResizeDrag`
  (edges 3 px, corners 8 px, bottom-right grip), hidden while maximized (`ResizeMode.NoResize`, as upstream). Minimize/maximize/
  close buttons keep the Webdings contents (`0`, `1`/`2`, `r`); `ChromeGlyphConverter` draws them as lines.
  Avalonia 11 runs through X11 (XWayland on Hyprland), where `_NET_WM_MOVERESIZE` works; native Wayland positioning
  (`Window.Position`) is a no-op, so the saved position is only honoured under X11/XWayland.
- `Linux/Common/WindowChromeController.cs` replaces `Common/WindowChromeController.cs` (same constructor over the shim `Window`,
  `ResizeMode` shim in `Compat/ResizeMode.cs`, `Window.ResizeMode` maps to `CanResize`).
- Styles: `MainWindowStyles.axaml` (also `WindowButtonStyle`), `TabControlStyles.axaml` and a PARTIAL `TrackingStyles.axaml`
  (party circles, current map, faction points, `ShortNumber`/`ValuePerHour` converter resources; the rest comes with the tracking
  views). Navigation tab icons use `fa:MaskedImage` (Rectangle + OpacityMask ImageBrush bound to `Tag` cannot be written in
  Avalonia) with `ImagePathToBitmapConverter` turning the upstream `pack://application:,,,/Assets/...` tags into bitmaps.
  The images of `Assets/` and `Resources/` of the WPF project are linked into the assembly (`avares://StatisticsAnalysisTool/...`).
- `UserControls/{Footer,ErrorBar,WarningBar,InformationBar}Control` are ported; the bar texts are styled properties.
- **Tabs still hosting `PortingPlaceholderControl`** ("Not yet ported: X"): DashboardControl, ItemSearchControl, LoggingControl,
  DungeonControl, DamageMeterControl, TradeMonitoringControl, GatheringControl, CraftingControl, PartyControl, GuildControl,
  StorageHistoryControl, MapHistoryControl, PlayerInformationControl, DonationControl, ThanksControl and SettingsControl
  (until the settings commit of T10). Each is replaced where it is ported (T11-T15).

## View placeholders

`Linux/Views/Placeholders.cs` declares `DialogWindow`, `ItemAlertWindow`, `GameDataPreparationWindow`,
`LanguageSelectionWindow` and `EventValidationWindow` so linked code compiles. Each is deleted when the real view
with the same name lands (T09/T10).

## Data migration

`Common/AppDataMigration.cs` is linked unchanged: it only copies directories and files and has no Windows API. Its
legacy sources (the installation directory and `<data>/Default`) are simply absent on a fresh Linux install, so it
is a no-op there; if they exist it migrates into the XDG targets above.

## Case sensitivity and path separators

- `AppDataPaths.GameFile(name)` (Linux copy) falls back to a case-insensitive match in `GameFiles` (the first in ordinal order when several differ only by case, with a logged warning). Upstream reads
  `IndexedItems.json` / `Items.json` (Settings defaults) while the extractor writes `indexedItems.json` / `items.json`.
- **Edit outside `src/StatisticsAnalysisTool/`:** `src/StatisticAnalysisTool.Extractor` hard-coded
  `.\Albion-Online_Data\StreamingAssets\GameData\...` and `cluster\world`. On Linux `\` is a file name character, so
  game data extraction found nothing. `ExtractorUtilities.GetBinFilePath` now joins separate segments, bin names are
  normalised with `NormalizeBinFileName`, and the unused `outFiles` block in `BinaryDumper` is removed. Behavior on
  Windows is identical; this is the only upstream-tree edit (a few lines, merge friendly).
- `Localization/localization.json` is copied to the output (the WPF project does this with a `Content` item).
- Linked code builds file names with `Path.Combine` and lower-case literals consistently; the only other mismatch found
  by a case-insensitive scan of all file name literals was the pair above.
- The game folder is chosen by the user (no registry or `C:\Program Files` default); `StreamingAssets/GameData` is
  looked up under `<folder>/game` with the exact casing used by the Steam and launcher installs.

## Network capture (libpcap)

- The `Libpcap` 0.1.5 package P/Invokes the native library name `pcap` (its resolver adds `wpcap` only on Windows).
  .NET probes `libpcap.so`, `pcap.so`, `libpcap`, `pcap`, so a machine with only the runtime package
  (`libpcap.so.1`, or `libpcap.so.0.8` on Debian) would fail. `Linux/Capture/LibpcapLibrary.cs` hooks
  `AssemblyLoadContext.ResolvingUnmanagedDll` (the package already registers a `DllImportResolver`, and a second one throws)
  to load `libpcap.so.1`, `libpcap.so`, `libpcap.so.0.8` in that order; `Program.Main` installs it.
- Listing devices works without privileges; opening one needs root or `CAP_NET_RAW` (`CAP_NET_ADMIN` optional).
- Permission errors surface through the existing upstream paths: a failed `pcap_activate` is a `PcapException`
  ("CAP_NET_RAW may be required"), no device opens, `Start()` fails and `TrackingController` shows
  `NO_LISTENING_ADAPTERS` (now the setcap guidance). `Start()` also logs the guidance up front.
- Packaging note (T19): file capabilities put the process in secure-execution mode, so `LD_LIBRARY_PATH` is ignored for
  that launcher; native libraries must be found through the system loader paths or the app directory.
- Open for T07/T10: `App.xaml.cs` (`ShowNpcapInfoDialogOnFirstStart`) hard-codes the link `https://npcap.com/`; the Avalonia
  version should link to `https://man7.org/linux/man-pages/man7/capabilities.7.html`. The `FirstStartGuideViewModel`
  warning only appears when no device is listed (listing works unprivileged), so the view should also show
  `CaptureGuidance.PermissionMessage` when `ApplicationCore.IsAppStartedAsAdministrator()` is false. The settings window
  still labels the providers `Sockets (...)` / `Npcap`.
