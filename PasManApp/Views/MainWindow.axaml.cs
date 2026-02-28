using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using System;
using PasMan.ViewModels;

namespace PasMan.Views;

public partial class MainWindow : Window
{
    private bool _vaultInitialized;

    public MainWindow()
    {
        InitializeComponent();
        Opened += OnOpened;
    }

    private async void OnOpened(object? sender, EventArgs e)
    {
        if (_vaultInitialized)
        {
            return;
        }

        _vaultInitialized = true;

        if (DataContext is not MainWindowViewModel viewModel)
        {
            Close();
            return;
        }

        var masterPasswordDialog = new MasterPasswordWindow();
        var masterPassword = await masterPasswordDialog.ShowDialog<string?>(this);

        if (string.IsNullOrWhiteSpace(masterPassword))
        {
            Close();
            return;
        }

        try
        {
            viewModel.InitializeVault(masterPassword);
        }
        catch (InvalidOperationException)
        {
            Close();
        }
    }

    private async void OnDeleteClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (DataContext is not MainWindowViewModel viewModel || viewModel.SelectedItem is null)
        {
            return;
        }

        var confirmDialog = new DeleteConfirmationWindow(viewModel.SelectedItem.Title);
        var shouldDelete = await confirmDialog.ShowDialog<bool>(this);
        if (!shouldDelete)
        {
            return;
        }

        viewModel.RemoveItemCommand.Execute(null);
    }

    private async void OnSaveClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not MainWindowViewModel viewModel || viewModel.SelectedItem is null)
        {
            return;
        }

        if (!viewModel.ChangeItemCommand.CanExecute(null))
        {
            return;
        }

        var confirmDialog = new SaveConfirmationWindow(viewModel.SelectedItem.Title);
        var shouldSave = await confirmDialog.ShowDialog<bool>(this);
        if (!shouldSave)
        {
            return;
        }

        viewModel.ChangeItemCommand.Execute(null);
    }

    private async void OnCopyClick(object? sender, RoutedEventArgs e)
    {
        if (sender is not Button button)
        {
            return;
        }

        var text = button.Tag?.ToString();
        if (string.IsNullOrEmpty(text))
        {
            return;
        }

        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel?.Clipboard is null)
        {
            return;
        }

        await topLevel.Clipboard.SetTextAsync(text);
    }
}
