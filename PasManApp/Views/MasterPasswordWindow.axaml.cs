using Avalonia.Controls;
using Avalonia.Interactivity;

namespace PasMan.Views;

public partial class MasterPasswordWindow : Window
{
    public MasterPasswordWindow()
    {
        InitializeComponent();
        Opened += (_, _) => PasswordTextBox.Focus();
    }

    private void OnOpenClick(object? sender, RoutedEventArgs e)
    {
        var password = PasswordTextBox.Text;
        if (string.IsNullOrWhiteSpace(password))
        {
            ErrorTextBlock.Text = "Master password is required.";
            ErrorTextBlock.IsVisible = true;
            return;
        }

        Close(password);
    }

    private void OnCancelClick(object? sender, RoutedEventArgs e)
    {
        Close(null);
    }
}
