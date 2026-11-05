// Placeholders for the WPF windows referenced by linked ViewModels/controllers. Each is replaced by the real
// Avalonia view of the same name and namespace in T09/T10 (delete the placeholder when the view lands).
using StatisticsAnalysisTool.Models;
using System.Windows;

namespace StatisticsAnalysisTool.Views;

public class DialogWindow : Window
{
    public DialogWindow(string title, string message)
    {
        Title = title;
        Message = message;
    }

    public string Message { get; }
}

public class ItemAlertWindow : Window
{
    public ItemAlertWindow(AlertInfos alertInfos)
    {
        AlertInfos = alertInfos;
    }

    public AlertInfos AlertInfos { get; }
}

public class GameDataPreparationWindow : Window;

public class LanguageSelectionWindow : Window;

public class EventValidationWindow : Window;
