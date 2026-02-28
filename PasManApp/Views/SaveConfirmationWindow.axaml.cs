using Avalonia.Controls;
using Avalonia.Interactivity;

namespace PasMan.Views;

public partial class SaveConfirmationWindow : Window
{
    public SaveConfirmationWindow()
    {
        InitializeComponent();
    }

    public SaveConfirmationWindow(string? entryTitle)
    {
        InitializeComponent();
        if (!string.IsNullOrWhiteSpace(entryTitle))
        {
            MessageTextBlock.Text = $"Save changes to entry '{entryTitle}'?";
        }
    }

    private void OnSaveClick(object? sender, RoutedEventArgs e)
    {
        Close(true);
    }

    private void OnCancelClick(object? sender, RoutedEventArgs e)
    {
        Close(false);
    }
}
