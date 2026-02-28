using Avalonia.Controls;
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
}
