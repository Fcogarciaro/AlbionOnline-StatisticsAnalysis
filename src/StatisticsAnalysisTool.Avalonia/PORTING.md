# Porting notes: WPF to Avalonia (Linux)

`StatisticsAnalysisTool.Avalonia` compiles the non-view `.cs` files of `src/StatisticsAnalysisTool` in place
(`LinkWpfSources=true`, the default). Upstream WPF files are never edited, so upstream merges stay cheap.
Anything the Linux build cannot use is excluded in the csproj and replaced by a file with the same public
surface under `Linux/`, or covered by a WPF-shaped shim under `Compat/` (namespaces match WPF).

## Excluded files and their Linux replacements

| WPF file (excluded) | Reason | Replacement | Behavior difference |
|---|---|---|---|
| `Updater/AutoUpdateController.cs`, `Updater/UpdateDownloadSession.cs` | NetSparkleUpdater.UI.WPF, Authenticode, WPF update window | `Linux/Updater/AutoUpdateController.cs` (+ pure logic in `Linux/Updater/LinuxUpdate.cs`) | Notify-only updater. Checks the GitHub releases of the fork (`Fcogarciaro/AlbionOnline-StatisticsAnalysis`) at start and every 6 h, takes the newest non-draft `linux-v<version>` release that ships `StatisticsAnalysisTool-linux-x64.tar.gz` (pre-releases only with `IsSuggestPreReleaseUpdatesActive`), compares it with the assembly `<Version>` (missing components count as 0), raises `UpdateAvailabilityChanged` (footer badge) and opens `UpdateWindow` with the release notes. Its action opens the release page. **It never downloads or swaps files**: the binary carries file capabilities (setcap) that a replaced file loses, the PKGBUILD install is root-owned, and a `.sha256` from the same origin does not authenticate the archive. Update with the package manager or `install.sh`. A failed check is silent in the background and a message box on the manual check. |
| `Updater/AuthenticodeSignatureVerifier.cs` | Windows Authenticode | none (only used by the excluded controller) | n/a |
| generated `AutoUpdateSecurityBuildSettings.g.cs` | MSBuild-generated in the WPF project | `Linux/Updater/AutoUpdateSecurityBuildSettings.cs` | All flags off. |
| `Common/AppDataPaths.cs` | `LocalApplicationData` is the only root; everything (incl. `Settings.json`) sits in it | `Linux/Common/AppDataPaths.cs` (copy, keep in sync) | Same members and `Instances/<id>/...` sub-structure. `Settings.json` goes to `$XDG_CONFIG_HOME/StatisticsAnalysisTool` (default `~/.config`); `Backups`, `UserData-*`, `logs`, `GameFiles` to `$XDG_DATA_HOME/StatisticsAnalysisTool` (default `~/.local/share`, where .NET already pointed `LocalApplicationData`); `temp`, `ImageResources`, `SpellImageResources` to `$XDG_CACHE_HOME/StatisticsAnalysisTool` (default `~/.cache`, safe to delete). Relative XDG values are ignored per spec. Copy the folders between machines by recreating the same `Instances/<id>` layout. Upstream has no portable mode, so none is offered. Extra member `UserSettingsFile`. |
| `Network/PacketProviders/SocketsPacketProvider.cs` | Raw sockets with Windows `SIO_RCVALL` | `Linux/Network/SocketsPacketProvider.cs` (subclass of the libpcap provider) | The `Sockets` choice (enum value 1, may come from a Windows settings file) runs libpcap. The default `PacketProvider` is already `Npcap` = libpcap. The settings UI relabels them through `PacketProviderLabelConverter`: `libpcap` and `Sockets (mapped to libpcap)`. |
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
| `Shortcut/**` | Windows shell (`.lnk`) | `Linux/Platform/DesktopShortcut.cs` | "Create desktop shortcut" writes a Desktop Entry to `$XDG_DATA_HOME/applications` (menu) and, when `~/Desktop` (or `$XDG_DESKTOP_DIR`) exists, an executable copy there. |
| `Common/Controls/**` (including `FontAwesomeImage`), `TextBoxHelper.cs`, `ScrollViewerExtensions.cs`, `ProgressBarSmoother.cs`, `CultureAwareBinding.cs` | WPF controls and attached properties | `Linux/Controls/{DigitsTextBox,ResponsiveUniformGrid,SpellDescriptionTextBlock,FontAwesomeImage}.cs`, `Linux/Common/{TextBoxHelper,NumericInputRules,ScrollViewerExtensions,ProgressBarSmoother,CultureAwareBinding}.cs` (same names and namespaces) | See "Converters and controls" and "Icons" below. |
| `Common/Validation/**`, `Converters/ValidationErrorConverter.cs` | WPF `ValidationRule`/`Validation.Errors` | `Linux/Common/Validation/PathValidationRule.cs` (static `GetError`) | The backup path text box shows the message as tooltip and a red border (class `invalid`), set by `SettingsControl` on every text change. The converter is not needed. |
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

### File pickers under setcap (`Linux/Platform/ExternalFilePicker.cs`, `PickerRouter.cs`)

A binary with file capabilities (`setcap cap_net_raw,cap_net_admin=eip`) is non-dumpable, so xdg-desktop-portal cannot open
`/proc/<pid>/root` and rejects every portal picker call (`AccessDenied`). All pickers (`SaveFileDialog`,
`VistaFolderBrowserDialog`, `VistaOpenFileDialog`) go through `PickerHost.Pick`, which routes through `PickerRouter`:

1. Non-dumpable process (`prctl(PR_GET_DUMPABLE) != 1`; fallback: non-zero `CapEff`): try the external picker first.
2. Otherwise, or when no tool exists, call the Avalonia `IStorageProvider`. Any exception from it (notably `DBusException`)
   is logged and falls back to the external picker.
3. The external picker runs `zenity` (preferred) or `kdialog` as a child process via `ArgumentList` (no shell); file
   capabilities are not inherited across exec. Exit code 1 or empty output means cancelled; multi-select output is
   newline separated. Probing and process running are injectable (`Func<string,bool>`, `IProcessRunner`).
4. With no tool the call never throws: it returns "cancelled", logs, and shows a message asking the user to install zenity.

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
- The capture info dialog is the real `DialogWindow` (as upstream) with the capabilities(7) link as its clickable link
  (`CaptureStartupDialog.BuildContent`). The first start guide is the real window (`AppStartup` takes an optional `showFirstStartGuide` for tests).
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
- All tabs host their real control since T15 (the temporary placeholder control is gone); `ItemSearchControl` hosts `ItemDetailsControl` in its details slot.

## Secondary windows (T10)

All derive from the `System.Windows.Window` shim, use theme `WindowChrome`, and drag through `HotbarDragHandler` (`BeginMoveDrag`,
pointer-delta fallback; double click toggles maximize where upstream did). Resizable ones (`EventValidation`, `DamageMeter`,
`Dashboard`) use `WindowChromeController` and `fa:WindowResizeHandles` (hidden when maximized).
- `DialogWindow`, `InfoWindow`, `ItemAlertWindow`, `LanguageSelectionWindow`, `LootComparatorSaveNameWindow`: 1:1. `Hyperlink`s are
  clickable text (`ExternalLink`, http(s) only). InfoWindow step 2 links the capabilities(7) man page, and
  `DESCRIPTION_TRACK_MY_GAME_DETAILS_1/2` are overridden by `CaptureGuidance` (English only). `ItemAlertWindow`'s city gradient is built in
  code (gradient stops cannot bind). `DashboardWindow` title: upstream binds the non-existent `DashboardBindings.TranslationTitle`
  (always the fallback); the port binds `Translation.Dashboard`.
- `GameDataPreparationWindow`: same layout, plus a Linux block "Found on this computer" fed by `GameFolderLocator` (Steam homes and
  libraries from `libraryfolders.vdf`, Flatpak Steam, `WINEPREFIX`, `~/.wine`, `~/Games/*`, Bottles); the first valid hit is
  preselected (Confirm stays manual), and the picker opens at the best-guess folder. Validation is `Extractor.IsValidMainGameFolder`.
- `EventValidationWindow` uses `Styles/EventValidationStyles.axaml` and a `ListBox` for the WpfToolkit `VirtualizingItemsControl`.
  `DamageMeterWindow` shows the damage meter lists since T14 and `DashboardWindow` the dashboard since T13.
- `FirstStartGuideWindow`: same six steps and view model (it persists `HasCompletedFirstStartGuide` and saves the settings, as upstream). Linux:
  the tracking step lists `libpcap` and `Sockets (mapped to libpcap)` (texts overridden in `CaptureGuidance`: `FIRST_START_GUIDE_*`,
  English only), the Npcap download link opens the capabilities(7) man page, and a `CapturePermissionPanel` shows
  `CaptureGuidance.PermissionMessage` (setcap) whenever the process lacks `CAP_NET_RAW`, unless the view model's own warning
  already carries it. The game folder step prefills the first detected install (`GameFolderLocator`) and the picker opens near it.
  Language dots and step dots are classes (`ToStringEqualsConverter` because XAML cannot pass an int `ConverterParameter`).
- `UpdateWindow`: 1:1 with the upstream constructor. The Linux updater (notify-only, see the table) opens it from the footer badge and the
  manual check in Settings; the action button reads "Open release page" (English literal, there is no installer to run).
- `CompatBrushConverter`: shim `System.Windows.Media.Brush` properties of view models (e.g. `UpdateNoteSectionViewModel.TitleBrush`) need it in bindings.
- `ListCollectionView` (shim) is now a read-only `IList`: Avalonia refuses INotifyCollectionChanged sources that are not.
- Avalonia binding errors are logged, not thrown: view tests collect them with `BindingErrorLog`.

## Settings (T10)

- `UserControls/SettingsControl.axaml(.cs)` is the 621-line view converted mechanically (keyed styles to `Theme=`, `Visibility` to
  `IsVisible` + converter, `ToolTip.Tip`, `DisplayMemberPath` to `DisplayMemberBinding`, `ViewportWidth` to `Viewport.Width`), same cards,
  fields, bindings and handlers; `Styles/SettingsStyles.axaml` ports `SettingsStyles.xaml` (including the custom `Settings.OptionCheckBox`).
  `Dashboard.Section` and `Dashboard.SectionTitle` (parents of two settings themes) are defined there until the dashboard styles land (T13).
- Differences: folders open through `xdg-open` (`ExternalLink.OpenFolder`); the desktop shortcut is a Desktop Entry; the packet provider
  names are relabeled (above); path validation is the `invalid` class; the settings reload when the tab content is attached (a TabControl
  only keeps the selected content in the tree; upstream used `IsVisibleChanged`); the tracking toggle re-reads the real state with
  `SetCurrentValue` instead of `UpdateTarget()`.
- Windows wording is replaced through `Linux/Localization/LinuxWording.cs`, hooked into `LocalizationController.Translation` after
  `CaptureGuidance` (English only, like the capture texts): "Start at login", "system tray" instead of "notification area",
  "Create desktop entry", the autostart failure text, and the socket/packet-handler errors that named Administrator or Npcap.
  Other languages show the same English Linux text for these keys.
- The desktop entry written by "Create desktop entry" is `statisticsanalysistool.desktop`, the file name of the packaged entry, with
  `Icon=statisticsanalysistool` (hicolor theme icon) unless an icon file sits next to the executable.
- Not verified live in this session: the real desktop (see the T20 note in the feature document).

## Simple tabs (T11)

Each view keeps the upstream structure, bindings, handlers, texts and icons; its feature dictionary is ported with it (`Styles/*.axaml`,
merged in `Theme.axaml`, keys unchanged). Tests render the real control with the real `MainWindowViewModel` and assert zero binding
errors (`Views/ViewHarness.cs`, `BindingErrorLog`).

### Helpers added for these views

- **`common:MouseCommands.LeftClick` / `LeftDoubleClick` / `CommandParameter`** (`Linux/Common/MouseCommands.cs`): replace
  `<X.InputBindings><MouseBinding MouseAction="LeftClick" Command="..."/>`. Fires on the left press (double click on the second press).
- **`conv:GridLengthConverter`**: two-way, for `RowDefinition.Height="{Binding VaultBindings.GridSplitterPosition, Mode=TwoWay, Converter=...}"`
  where the view model holds the shim `System.Windows.GridLength` (a string `FallbackValue` cannot be converted, so none is used).
- **`conv:ValueInListConverter`**: `ConverterParameter='A|B|C'`, true when the bound value equals one of the names; turns a group of
  DataTriggers with the same setters (city names) into one class. Apostrophes in names are written `\'`.
- **`conv:DonationTypeIconConverter`**: the 35 `DonationType` DataTriggers of `Donation.Icon.Style` as one lookup table (a test compares
  it with the upstream XAML).
- **`fa:UrlImage`** (`Linux/Controls/UrlImage.cs`): `Image` with a `Url` property that downloads an http(s) url asynchronously (WPF
  converted the string itself). Replaces `Image Source="{Binding SomeUrl}"` and `Ellipse` + `ImageBrush ImageSource="{Binding SomeUrl}"`
  (an `ImageBrush` cannot download: use a `Border CornerRadius ClipToBounds` around the `UrlImage`).
- **`DataGrid`** (`Avalonia.Controls.DataGrid` 11.3.12; there is no 11.3.22): the replacement of `ListView` + `GridView`.
  `Styles/DataGridStyles.axaml` gives it the look of the upstream `ListView`/`ListViewItem`/`GridViewColumnHeader` styles (transparent,
  blue hover/selection, 14 px blue headers). Column headers are `DataGridTemplateColumn.Header` with a `TextBlock` (a bound `Header`
  string does not see the view model). The WPF header gripper had Width 0, so `CanUserResizeColumns` is off.

### Conversion rules found on the way

- `Width="auto"`/`Height="auto"` is `NaN` (or just removed); `ToolTip=` is `ToolTip.Tip=`; `Panel.ZIndex` is `ZIndex`;
  `MouseEnter`/`MouseLeave` are `PointerEntered`/`PointerExited`; a `Grid` with no `Background` is not hit-testable, so hover areas
  get `Background="Transparent"`; `Visibility.Hidden` (keeps the space) is `Opacity` 0 through a class, `Collapsed` is `IsVisible`.
- Null mid-path values log a binding warning in Avalonia (WPF is silent): write `Item?.Icon`, `GameInfoPlayers?.LifetimeStatistics?.Total`.
- `x:Name` on `ColumnDefinition`/`DataGridColumn` does not compile (not a `StyledElement`): take them by index in the code-behind.
- `ComboBox DisplayMemberPath` is `DisplayMemberBinding="{ReflectionBinding X}"`, `SelectedValuePath` is
  `SelectedValueBinding="{ReflectionBinding X}"`; a ComboBox template contains a `TextBox`/`Button`, so tests filter on `TemplatedParent == null`.
- `ItemsControl.ItemContainerStyle` of a `ContentPresenter` is `ItemContainerTheme` (a `ControlTheme` cannot contain a descendant selector).
- `VirtualizingItemsControl` (WpfToolkit) is a `ScrollViewer` around an `ItemsControl` with a `VirtualizingStackPanel` (or a `WrapPanel`
  for the container icons); no recycling options.
- Style rules that must be selectors (`Image.cluster-row...`, per-class gradients) live after `Styles.Resources` in the same dictionary.

### Per view

- **GuildControl** + `GuildStyles`: 1:1. `LootLogCheck.ContainerItem.Template` is not ported (only a commented-out tab used it). The first
  (hidden) tab keeps `IsVisible="False"`. Upstream binds the delete button to `TradeMonitoringBindings.IsDeleteTradesButtonEnabled`
  (kept). The info icon gets a transparent background so the pointer events fire.
- **PartyControl** + `PartyStyle`: 1:1; the header click uses `MouseCommands`; the item power block of the local player is invisible
  but keeps its space (Opacity 0), as `Visibility.Hidden`. The splitter position goes through `GridLengthConverter`.
- **StorageHistoryControl** + `StorageHistoryStyle`: 1:1. The WPF value text also set `Visibility` in its triggers, but its local
  binding won, so only the binding applies here too.
- **MapHistoryControl**: 1:1 (selection tracking, copy and delete dialogs). It needed `ClusterInfoTemplate` and `MapMarkerIconTemplate`,
  added to the partial `TrackingStyles.axaml` with the per-mode gradients and butterfly/marker images as classes.
- **ItemSearchControl** (+ `ItemSearchImageCounterControl`) + `ItemSearchStyles`: the list is a `DataGrid`; saved column widths are
  applied and written back (`DataGridColumn.WidthProperty` observable); the click on an already selected row refreshes the details
  (tunnelling `PointerPressed`). `ItemDetailsControl` fills the details slot since T15 (same data context and visibility).
  The counter control keeps binding to the inherited data context, as upstream. The toolbar separator is a 1 px vertical line.
- **DonationControl** + `DonationStyles`: the logos are clickable grids (`xdg-open`, http(s) only). The Patreon, PayPal and GitHub logos are
  72x72 (the WPF inline size is not defined); the progress bar template keeps the 2.8 s shimmer as a style animation.
- **ThanksControl** + `ThanksStyle`: 1:1.
- **PlayerInformationControl** + `PlayerInformationStyles`: 1:1; the avatar is a circular clip around `fa:UrlImage`; the
  `PlayerMode.Label.*`/`PlayerMode.Title*` styles are not ported (unused upstream). The header tabs are a `TabControl` whose template is
  only the item strip (needs a live check on the real desktop).
- Not verified on a real desktop in this session: the look of the `DataGrid` (template part names `BackgroundRectangle`,
  `VerticalSeparator` come from the Fluent DataGrid theme) and of the shimmer.

## Logging and dashboards (T12, T13)

### LoggingControl (T12)

- `UserControls/LoggingControl.axaml(.cs)` is 1:1 (filter card, notifications and stats tabs, loot comparator with its three cards,
  guild drop-down, info panel). Its templates come with the partial `TrackingStyles.axaml` (`NotificationTemplate`,
  `LootedItemTemplate`, `LootingPlayersTemplate`, `LoggingFilterObject`, `TopLooterObjectTemplate` and their themes). `x:DataType` is
  `MainWindowViewModel`; the three stats templates inside the control use `x:CompileBindings="False"` because they read the view model
  through `RelativeSource AncestorType=UserControl`.
- **Virtualization**: the notification list is a `ListBox` with a `VirtualizingStackPanel` (a test adds 5000 events and checks that fewer than
  200 rows are realized); the looting players list is a `ScrollViewer` around an `ItemsControl` with a `VirtualizingStackPanel`
  (WpfToolkit `VirtualizingItemsControl`). `IsAsync` bindings are plain bindings (the lists are filled on the UI thread).
- **Implicit fragment templates**: the WPF `DataTemplate DataType=...` entries for the notification fragments cannot live in a resource
  dictionary, so they are the `ContentControl.DataTemplates` of `NotificationTemplate` (the only place a fragment is shown).
- DataTriggers: the activation toggle icon is `Logging.ToggleIcon` + `Classes.on`; the `+`/`-` operator text is `OperatorTextBlockStyle` +
  `Classes.minus`; fame text color is `Tracker.Fragment.FameText` + `pve`/`pvp`; faction coin/flag/city colors are classes; the looted
  item overlay is `LootedItem.StatusOverlay` (fades on `:pointerover`); the chest log placeholder is `StringConverters.IsNullOrEmpty`.
  `Visibility.Hidden` bonus factor and affiliation texts are collapsed instead (the WPF styles also set `Width=0`).
- The guild drop-down is a `Popup` with `IsLightDismissEnabled` and `OverlayInputPassThroughElement` (so the toggle closes it). Copy to
  clipboard goes through the `System.Windows.Clipboard` shim (main window clipboard). The export icons and the info icon use
  `PointerReleased`. `TextBox` scroll bars are the `ScrollViewer.*ScrollBarVisibility` attached properties.
- `EventValidationWindow` was already complete (T10).

### Dashboard styles and GatheringControl (T13)

- `Styles/DashboardStyles.axaml` ports `DashboardStyles.xaml` (it now owns `Dashboard.Section` and `Dashboard.SectionTitle`; `Theme.axaml` includes it
  before `SettingsStyles`). Patterns used: `DataTrigger` on `IsIncrease`/`IsDecrease` is `Classes.increase|decrease` on every
  `Dashboard.SummaryDeltaText` use; tier and mob type tags are classes (`t1`..`t8`, one per `MobTypeCategory`); `Visibility` triggers on enums or
  booleans are `IsVisible` with `ObjectConverters.Equal|NotEqual` or `BoolConverters.Not`; the two custom `Expander` templates
  (`Dashboard.CombatEventExpander`, `Dashboard.MobExpander`) are an `Expander` theme whose header is a `ToggleButton` theme (`...Toggle`) with the
  chevron switched by `:checked`; the ranking bar keeps `PART_Track`/`PART_Indicator` of the Avalonia `ProgressBar`.
  `Dashboard.SessionDeleteButton` is hidden by default and shown by `ComboBoxItem Button.session-delete.can-delete` (the MultiDataTrigger); the dashboard sets
  `Classes.can-delete`, the gathering view overrides it with a local `IsVisible` binding as upstream.
- Items whose view-model type is the shim `Brush` (`Brush` of chart filters, ranking items, type summaries) bind through `CompatBrushConverter`.
- `Styles/GatheringStyles.axaml` ports `GatheringStyles.xaml`: the resource tab lists (`WpfToolkit VirtualizingItemsControl`) are a `ScrollViewer`
  around an `ItemsControl` with a `VirtualizingStackPanel`. `TrackingStyles.axaml` also gained the `Icon.*` image themes (chests, dungeon, fame, silver, re-spec).
- `UserControls/GatheringControl.axaml(.cs)`: 1:1; charts are `lvc:PieChart`/`lvc:CartesianChart` (`LiveChartsCore.SkiaSharpView.Avalonia`, same
  `Series`, `XAxes`, `LegendPosition`, `LegendTextPaint`, ... as the WPF `wpf:` charts). `ItemsSource` properties of nullable members use `?.`
  (`BestResource?.Item?.Icon`). WPF `IsVisibleChanged` becomes `Loaded`, `Unloaded` and a change of `IsVisible` (a TabControl detaches unselected
  content), reporting `IsLoaded && IsEffectivelyVisible` to `GatheringBindings.SetViewVisibility`. Tests draw a pie and a line chart through Skia
  headless and check their pixels. Note that `GatheringBindings` replaces `GatheringStats` series collections itself on its refresh, so tests set the
  series before showing the control.

### DashboardControl, DashboardLootControl, DashboardWindow (T13)

- 1:1 ports (`UserControls/DashboardControl.axaml(.cs)`, `DashboardLootControl`, `Views/DashboardWindow` hosting the control with `ShowOpenWindowButton=False`).
  Section headers use `PointerPressed` (left button only) instead of `MouseLeftButtonDown`; `ShowOpenWindowButton` and `ShowDetails` are styled properties.
  The mob list height still follows the control (`UpdateMobListHeight`), re-run when the list is attached or shown (WPF `IsVisibleChanged`).
  The inline `Border.Style` triggers that top-align a collapsed section are the class `valign-top` (`ObjectConverters.Equal` against `Visibility.Collapsed`).
- `ImagePathToBitmapConverter` also accepts `Uri` values (faction seal, coin and standing icons) and `/Assets/...` strings (`IconSource`, `ChestIconSource`, `ContentIconSource`).
- The filter `SelectionChanged` handlers skip the refresh while no `TrackingController` is registered (`ServiceLocator.Resolve` throws; upstream always has one).
  Tests that need it use `ViewHarness.UseTrackingController()`.
- Fidelity gaps: the expander chevrons and the status overlay of looted slots were not compared on a real desktop; the dashboard history chart is only checked for its
  bindings (the dashboard controller refreshes its series itself), the line/pie drawing is covered by the gathering tests.

### CraftingControl (T13)

- `UserControls/CraftingControl.axaml(.cs)` + `Styles/CraftingStyles.axaml` (the former `UserControl.Resources`). Same editor, resource list, journal block, result tiles, saved list
  and Black Market tab. The three `ListView` + `GridView` become `DataGrid`s (resource list, saved craftings, Black Market items; headers keep their tooltips); saved rows use `RowHeight=50`
  instead of the `ListViewItem` style. DataTriggers are classes: profit/ROI/profit per item (`positive`/`negative`, set by every use), the sell price status colors (`normal`, `old1`..`old3`,
  `none`), tier and enchantment level colors (`t1`..`t8`, `l0`..`l4`) and the saved focus icon (`Crafting.FocusIcon` + `on`).
- Price and search popups are `Popup`s with `IsLightDismissEnabled` and `OverlayInputPassThroughElement` (the text box); their content inherits the data context (no
  `PlacementTarget.DataContext` hop). WPF `Preview*MouseLeftButton*` handlers are tunnelling pointer handlers on the control (`handledEventsToo`), dispatched by the text box
  (`Classes` `price-input`, `resource-price`). `LostKeyboardFocus` handlers read the focused element after the focus moved (Avalonia gives no `NewFocus`); the popup lists are separate top
  levels and never take focus from the text box.
- `Journal` and `Calculation` are null until a crafting exists: bindings use `?.` and the journal popup flags use `TargetNullValue=False`.
- `LossExplorerControl` is ported in T14 (see below).
- Not verified on a real desktop: popup placement and light dismiss with the price text boxes, and the `DataGrid` row look.

## DamageMeter, Dungeon, LossExplorer, TradeMonitoring, ItemDetails (T14, T15)

### Conversion tool and `ValueMapConverter`

The big XAML files of T14/T15 were converted by a throw-away script (XML tree to XML tree) that applies the rules above mechanically
(namespaces, `Style` to `ControlTheme`, `Style=` to `Theme=`, `Visibility` to `IsVisible`, `ToolTip` to `ToolTip.Tip`, `MouseBinding` to
`MouseCommands`, `IsAsync`, `x:Type`, brush resources to `DynamicResource`, `Label ContentStringFormat` into the binding, `ImageBrush.ImageSource`),
followed by manual review. The script is not part of the repository; the output is the source of truth.

`conv:ValueMapConverter` (`Linux/Common/Converters/ValueMapConverter.cs`) replaces groups of DataTriggers (and `Trigger Property="Tag"`) that set one
property from one binding value: `ConverterParameter='sword=@SolidColorBrush.Weapon.Sword|*=@SolidColorBrush.Weapon.Unknown'`. The value is compared by
its invariant text (enum members by name), `*` is the default (the original setter), `@Key` is an application resource, other results are parsed as
the target type. It is used inside `ControlTheme` setters (`<Setter Property="Foreground" Value="{Binding X, Converter=...}"/>`, the binding runs against the
control's data context), in local attributes (inline `X.Style` with triggers) and for `DataTemplate.Triggers` with `TargetName` (the binding goes on the named
element). Visibility triggers become `IsVisible` with `Name=True|*=False`.

### DamageMeterControl, DamageMeterStyles, DamageMeterWindow (T14)

- 1:1 ports. `DamageMeter.TrackingStatusText` (Text switched by a trigger) is two stacked text blocks with `IsVisible`; `DamageMeter.Spell.Icon.Selector`
  is two stacked images (the auto attack icon for spell index 0). The activation toggle color binds through `CompatBrushConverter`.
- `DamageMeterWindow` shows the player list (`ScrollViewer` + `ItemsControl`) or the mob list (`ListBox`, theme `DamageMeter.VirtualizedList`), chosen by
  the sort type. Upstream has no `Topmost` and no transparency settings (`AllowsTransparency` only): none were added. Note for any future always-on-top
  overlay: `Window.Topmost` is honoured by X11/XWayland window managers but native Wayland compositors may ignore it (no protocol for it outside layer-shell).
- Null paths use `?.` (`CauserMainHand?.Icon`, `DamageMeterSnapshotSelection?.DamageMeter`). Compiled bindings are off in the control (`x:CompileBindings="False"`);
  the window keeps `x:DataType` (without it the Avalonia compiler demands a parameterless constructor).
- The Spell tooltip is the content of `ToolTip.Tip`; the `Spell.ToolTip` theme chrome (Border, padding, max width) is not applied to the tip itself,
  so the default Avalonia tooltip frame surrounds the spell text.

### DungeonControl, DungeonStyles, LossExplorerControl (T14)

- `DungeonStyles.axaml` (3.5k lines) and `DungeonControl.axaml` are the converted upstream files. Triggers: value maps (`ValueMapConverter`) for every
  group of DataTriggers; `ImageBrush` setters of the faction/mode backgrounds are hoisted into named brush resources (`DungeonFragment.Faction.Background.*`)
  that the map returns (`@Key`); trigger tooltips whose value was a `Translation*` binding use the `$KEY` form (the localized text of the key, read at
  convert time); `Dungeon.DeltaText` uses `Classes.positive|negative` (set by every use, bound to `IsPositiveChange|IsNegativeChange`); the loot badge border is
  `Dungeon.RarityBadge` + `Classes.other`; the cluster icon is two stacked images (the Abyssal Depths mode overrides the cluster type); the fastest run time is two
  stacked text blocks (the dash replaces the value without a run).
- `Translation*` properties that upstream declares `static` on `DungeonBaseFragment` are bound through `{x:Static ...}` (an instance path to a static member does
  not resolve in Avalonia).
- Every dotted binding path is null-safe (`A?.B?.C`): WPF is silent about null mid-path values, Avalonia logs them.
- `VirtualizingItemsControl` + `DungeonDataTemplateSelector` is a `ScrollViewer` around an `ItemsControl` with a `VirtualizingStackPanel`; the selector maps all nine
  fragment types to `Dungeon.ModernRunEntry.Template`, so the template is used directly (the compat `DataTemplateSelector` holds shim templates that XAML cannot fill).
- `ContentPresenter` outside control templates is a `ContentControl` (a presenter in a data template does not take its own data context for the `Content` binding).
- `LossExplorerControl`: the cards use `ItemsRepeater` + `WrapLayout` from the new package `Avalonia.Controls.ItemsRepeater` 11.1.5 (virtualized wrap panel,
  replaces `VirtualizingWrapPanel`; use it for the other `VirtualizingWrapPanel` views too). The load on `IsVisibleChanged` runs on `Loaded` and when `IsVisible` turns true.
- Fidelity gaps: not compared pixel by pixel on a real desktop (card look, image-brush banners, tooltips); the stats templates are exercised through their view model
  in tests, the run entries with two fragment types.

### TradeMonitoringControl, TradeMonitoringStyle (T15)

- Converted like the dungeon files (value maps, null-safe paths, `IsAsync` dropped, `ContentPresenter` outside templates not used). Differences:
  - **Heatmap**: `fa:SKElement` (`Linux/Controls/SKElement.cs`) replaces `skia:SKElement` of SkiaSharp.Views.WPF. It raises `PaintSurface` with a canvas sized like the control
    (`SKPaintSurfaceEventArgs.Canvas` / `.Info`, so `e.Surface.Canvas` becomes `e.Canvas`; the drawing code of `ProfitByTimeOfDayHeatmap_OnPaintSurface`, the layout and the
    cell color blend are the upstream code unchanged). It draws through an `ICustomDrawOperation` and `ISkiaSharpApiLeaseFeature`, clipped to the element and inside a transparent
    layer (a WPF element owns a transparent bitmap, whereas `Canvas.Clear(Transparent)` on the shared Avalonia canvas would erase the parent; tests cover both). Hover tooltips use
    `ToolTip.SetTip` from `PointerMoved`/`PointerExited`. Tests read the pixels of the profit, loss and zero cells of a rendered window.
  - `DatePicker` is `CalendarDatePicker` (text box plus calendar popup, the closest control; the `DatePickerTextBox` template is dropped).
  - `TradeDataTemplateSelector` holds shim `System.Windows.DataTemplate`s, which XAML cannot fill, so the control builds it in its constructor from the application resources
    and assigns it to `TradeList.ItemTemplate`; the list is a `ScrollViewer` around a virtualizing `ItemsControl`.
  - The location statistics table is shown through `CollectionViewSource.GetDefaultView(LocationStatistics)` (assigned in code on every change) so the header sort still works.
  - `PreviewMouseWheel` on the charts and the heatmap is a tunnelling `PointerWheelChanged` handler on the diagrams `ScrollViewer` (only for events from a chart or the heatmap, which scrolls
    the page instead of zooming). The handler on the ranking lists was dead code upstream (`IsHitTestVisible=False`) and stays unattached.
  - Charts are `lvc:CartesianChart` (`LiveChartsCore.SkiaSharpView.Avalonia`).
  - The tracking status text and the "most expensive" value texts (a trigger swapping the bound text) are stacked text blocks switched by `IsVisible`.
- Fidelity gaps: not compared on a real desktop (card look, calendar popup, wheel behaviour over the charts).

### ItemDetailsControl, ItemDetailsStyles (T15)

- Converted like the other T14/T15 files. Differences:
  - The three `ListView` + `GridView` lists (main prices, quality prices, real money prices) are `DataGrid`s with `DataGridTemplateColumn`s that use the unchanged
    `GridViewColumn*Template` data templates (width `Auto`, headers are text blocks). The main list keeps its click-to-sort headers: the columns carry the sort property in `Tag`
    and the grid's `Sorting` event (handled) sorts the default collection view of `MainTabBindings.ItemPrices`, which the control assigns as the grid's items (and re-assigns when
    the view model replaces the collection), toggling the direction on every click as upstream. `Language="{Binding ItemListViewLanguage}"` is dropped (no per-element language).
  - The double click that copies a price (`MouseBinding` in the cell labels) is `MouseCommands.LeftDoubleClick`; its parameter `Content.X` of the templated parent is the row property `X`.
  - The refresh icon: `PointerPressed` (left button) plus `Classes="refresh"` with a `:disabled` style (opacity 0.35, arrow cursor) instead of the `IsEnabled` trigger.
  - `LinearGradientBrush` points are percentages in Avalonia (`0,0`/`1,0` would be pixels): `StartPoint`/`EndPoint` of every gradient ported in T14/T15 were rewritten to `0%,0%`/`100%,0%`, `MappingMode` dropped.
  - The history chart is `lvc:CartesianChart`.
- `TrackingStyles.axaml` now also has `Tracking.Information.*`, `ButtonLabel` and `BarButton` (unreferenced upstream).
- The placeholder control and its infrastructure were removed: every upstream view has its Avalonia view.
- Fidelity gaps: not compared on a real desktop (grid row look, tooltips of the spell blocks, gradient banners); item details are checked with a real view model for a fake item (the
  price requests of `InitAsync` run in the background and are not awaited).

## View placeholders

There are none: every upstream window and tab has its Avalonia view.

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

## Packaging (T19)

- `packaging/linux/build-tarball.sh [VERSION]` publishes self-contained `linux-x64` (Release, no pdb) and writes
  `artifacts/StatisticsAnalysisTool-linux-x64.tar.gz` plus `.sha256` (`artifacts/` is gitignored). VERSION is compiled in as the
  assembly version; the update check compares it with the newest `linux-v*` release. The tarball holds `StatisticsAnalysisTool/` with the
  app, `install.sh`, `share/` (desktop entry, hicolor icons 256/128/64), `LICENSE`, `licenses/` and `README-linux.md`.
- Licence files are `Content` items of the project, so they land in build and publish output: `LICENSE` (GPL-3.0),
  `licenses/FontAwesome-LICENSE.txt`, `licenses/GameIcons-NOTICE.txt`.
- `install.sh` installs to `~/.local/opt/StatisticsAnalysisTool` (user scope, no root), links `~/.local/bin/statisticsanalysistool`, pins the
  desktop entry `Exec`/`Path` to the install, and prints (or with `--setcap` runs) `sudo setcap cap_net_raw,cap_net_admin=eip <binary>`.
  setcap must be reapplied after each update, because replacing the file drops the capability.
- `packaging/arch/PKGBUILD` builds from the `linux-v$pkgver` source tag with `dotnet-sdk`, installs to `/opt/statisticsanalysistool`
  (`/usr/bin/statisticsanalysistool` symlink), depends on `libpcap`, and `statisticsanalysistool.install` runs setcap in
  `post_install`/`post_upgrade`. `sha256sums` is `SKIP` until the first tag exists (`updpkgsums`). Not run through `namcap` (not installed).
- The icons are scaled from the upstream 80 px `sat-icon.png`; replace them with a vector or larger source when one exists.
- CI: `.github/workflows/linux.yml` (build + headless tests on pushes and PRs to `feat/avalonia-linux-port` and `main`; on tag
  `linux-v*` it builds the tarball and uploads both assets). Upstream workflows are unchanged: `pr-build-and-unit-tests.yml` still
  runs on pushes to `main` (it builds the Windows solution on ubuntu, so expect it to fail or be useless there) and the upstream release
  workflow only fires on `v*` tags.
