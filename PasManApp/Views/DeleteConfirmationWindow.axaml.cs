using Avalonia.Controls;
using Avalonia.Interactivity;

namespace PasMan.Views;

public partial class DeleteConfirmationWindow : Window
{
    public DeleteConfirmationWindow()
    {
        InitializeComponent();
    }

    public DeleteConfirmationWindow(string? entryTitle)
    {
        InitializeComponent();
        if (!string.IsNullOrWhiteSpace(entryTitle))
        {
            MessageTextBlock.Text = $"Delete entry '{entryTitle}'?";
        }
    }

    private void OnDeleteClick(object? sender, RoutedEventArgs e)
    {
        Close(true);
    }

    private void OnCancelClick(object? sender, RoutedEventArgs e)
    {
        Close(false);
    }
}
