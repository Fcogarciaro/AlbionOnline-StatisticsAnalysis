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
| `Common/SoundController.cs` | `System.Windows.Media.MediaPlayer` | `Linux/Common/SoundController.cs` | Sound discovery identical (`Sounds/*.wav` are copied to the output from the WPF project). Playback runs the first available of `pw-play`, `paplay`, `aplay` as a detached process. Volume is applied by `pw-play` and `paplay`; `aplay` cannot scale volume. Volume 0 plays nothing (as in WPF). With no player installed a single warning is logged. |
| `Common/SystemInfo.cs` | WMI (System.Management), `WindowsPrincipal` | `Linux/Common/SystemInfo.cs` | CPU from `/proc/cpuinfo`, admin means root, GPU unknown. TODO(T03). |
| `Common/Utilities.cs` | user32 `FlashWindowEx`, `WindowInteropHelper`, `SystemParameters` | `Linux/Common/Utilities.cs` (copy, keep in sync) | `FlashWindow` asks the X11 window manager for `_NET_WM_STATE_DEMANDS_ATTENTION` through `wmctrl` (skipped when the window is active); a no-op on Wayland or without `wmctrl` (Avalonia has no urgency API). `count` is ignored; `StopFlashingWindow` clears the state. Centering uses Avalonia screens. |
| `Notification/SatNotificationManager.cs` | Notification.Wpf toasts | `Linux/Notification/SatNotificationManager.cs` | Same API and the same setting gates (`IsNotificationTrackingStatusActive` for success/error/tracking, `IsNotificationFilterTradeActive` for trades). Toasts are Avalonia `WindowNotificationManager` cards (bottom right, 5 s, the Notification.Wpf default) while the main window is visible. When the window is hidden or unfocused and `notify-send` exists, a desktop notification is sent too (error = critical urgency). Differences: no per-row text trimming (`RowsCount`), colors come from the Avalonia theme instead of the `SolidColorBrush.Notification.Background.*` keys. |
| `Localization/Culture.cs` | `FrameworkElement.LanguageProperty` (xml:lang) | `Linux/Localization/Culture.cs` | Sets thread cultures only; Avalonia has no per-element language. |
| `Common/WindowsStartupService.cs` | HKCU Run registry key | `Linux/Common/WindowsStartupService.cs` | Writes/removes `$XDG_CONFIG_HOME/autostart/StatisticsAnalysisTool.desktop` (default `~/.config/autostart`, relative XDG values ignored per spec). One fixed file name, whereas Windows used one registry value per install path (`InstanceId`); two installs share the entry. `Exec` is the process path, or `dotnet <dll>` when launched through the dotnet host. |
| `SystemTrayService.cs`, `SystemTrayMenuRenderer.cs` | WinForms `NotifyIcon` | `Linux/Common/SystemTrayService.cs` (renderer dropped) | Same menu (Open, Activate/Deactivate tracking, Exit) and minimize-to-tray logic on an Avalonia `TrayIcon` (needs a StatusNotifierItem host; GNOME needs an extension). Takes `Avalonia.Controls.Window`. Single click restores (no double-click event). The first-hide hint is a `notify-send` notification (no balloon-tip click). Native menus are not themed. The service needs `MainWindowViewModel`, so it is constructed by the real `MainWindow` (T09), not by `LinuxApp`. Icon: `sat-icon.ico` as Avalonia resource |
| `Shortcut/**`, `WindowChromeController.cs` | Windows shell | none yet | Chrome belongs to T09. |
| `Common/Controls/**`, `TextBoxHelper.cs`, `ScrollViewerExtensions.cs`, `ProgressBarSmoother.cs` | WPF controls and attached properties | none yet | Re-created as Avalonia controls/behaviors in T08. |
| `Common/CultureAwareBinding.cs`, `Common/Validation/**`, `Converters/ValidationErrorConverter.cs` | WPF binding and validation classes | none yet | T08/T10. |
| `Views/**`, `UserControls/**`, `*.xaml.cs`, `App.xaml.cs`, `Properties/AssemblyInfo.cs`, `Properties/Resources.Designer.cs` | Views, application shell, resx image resources | ported views (T07-T15) | See view placeholders below. |
| `Models/StatPricesTotal.cs`, `Network/Handler/JoinHandler.cs`, `Network/SilverCountUpTimer.cs` | Excluded since T01 | n/a | n/a |

## Package and API replacements

| WPF | Linux |
|---|---|
| `LiveChartsCore.SkiaSharpView.WPF` 2.0.5 | `LiveChartsCore.SkiaSharpView.Avalonia` 2.0.5 (same release) |
| `SkiaSharp` 4.151.1 | `SkiaSharp` 3.119.0 + `SkiaSharp.NativeAssets.Linux`: LiveCharts 2.0.5 requires 3.119.0 and Avalonia 11.3 supports 3.x |
| `DSaladin.FontAwesome.WPF` `FontAwesomeIcon` | `Linux/Icons/FontAwesomeIcon.cs`, an enum generated from the 6.7.1 package (identical names and values); the icon control is T16 |
| `Ookii.Dialogs.Wpf` (`VistaOpenFileDialog`, `VistaFolderBrowserDialog`) | `Compat/VistaOpenFileDialog.cs`, `Compat/MessageBox.cs`: Avalonia `StorageProvider` pickers, blocking via a nested dispatcher frame. Returns false when no visible window exists. `ShowNewFolderButton` is ignored (the portal picker always offers it) |
| `Microsoft.Win32.SaveFileDialog`, `System.Windows.MessageBox` | `Compat/MessageBox.cs`: `SaveFileDialog` uses the save picker (DefaultExt appended when the name has no extension, as Win32 AddExtension); `MessageBox.Show` opens a modal `MessageBoxDialog` (`Compat/Dialogs/`). Button labels are English; closing a YesNo box with Escape or the close button returns No (WPF disables closing it) |
| `Properties/Settings.Designer.cs` | Linked unchanged; `System.Configuration.ConfigurationManager` stores user settings under the user's local data directory. TODO(T04): XDG paths |
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

## View placeholders

`Linux/Views/Placeholders.cs` declares `DialogWindow`, `ItemAlertWindow`, `GameDataPreparationWindow`,
`LanguageSelectionWindow` and `EventValidationWindow` so linked code compiles. Each is deleted when the real view
with the same name lands (T09/T10).
