using Avalonia.Controls;
using Avalonia.Interactivity;
using PasMan.Models;
using PasManApp.Services;
using System;
using System.Diagnostics;
using System.Linq;

namespace PasMan.Views;

public partial class MainWindow : Window
{
    private VaultManager vaultManager;

    private string masterPassword = "gDhuG1df71sfd";
    public MainWindow()
    {
        InitializeComponent();

        vaultManager = new VaultManager();
        vaultManager.OpenVault(masterPassword);
        
        var allEntries = vaultManager.SearchPasswordEntries("");

        foreach (var passwordEntry in allEntries)
        {
            TextBlock textBlock = new TextBlock();
            textBlock.Text = passwordEntry.Title;
            PasswordListBox.Items.Add(textBlock);
        }

    }
}