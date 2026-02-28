using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PasMan.Models;
using PasManApp.Services;

namespace PasMan.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private readonly VaultManager _vaultManager;
    private readonly List<PasswordEntryViewModel> _allPasswordEntries = [];
    private string? _masterPassword;

    public ObservableCollection<PasswordEntryViewModel> PasswordEntries { get; } = [];

    [ObservableProperty]
    private PasswordEntryViewModel? _selectedItem;

    [ObservableProperty]
    private string? _newTitleName;

    [ObservableProperty]
    private string? _searchText;

    public MainWindowViewModel()
        : this(new VaultManager())
    {
    }

    public MainWindowViewModel(VaultManager vaultManager)
    {
        _vaultManager = vaultManager;
    }

    public void InitializeVault(string masterPassword)
    {
        _vaultManager.OpenVault(masterPassword);
        _masterPassword = masterPassword;

        _allPasswordEntries.Clear();
        foreach (PasswordEntry entry in _vaultManager.SearchPasswordEntries(string.Empty))
        {
            _allPasswordEntries.Add(new PasswordEntryViewModel(entry));
        }

        FilterPasswordEntries();
    }

    [RelayCommand]
    private void AddItem()
    {
        if (!CanUseVault())
        {
            return;
        }

        var newEntry = new PasswordEntryViewModel
        {
            Title = string.IsNullOrWhiteSpace(NewTitleName) ? "Empty" : NewTitleName.Trim()
        };

        NewTitleName = string.Empty;

        var model = newEntry.GetPasswordEntry();
        _vaultManager.AddPasswordEntry(model);
        _vaultManager.SaveVault(_masterPassword!);

        newEntry.Id = model.Id;
        newEntry.UpdatedAt = model.UpdatedAt;
        _allPasswordEntries.Add(newEntry);
        FilterPasswordEntries();
        SelectedItem = newEntry;
    }

    [RelayCommand]
    private void ChangeItem()
    {
        if (!CanUseVault() || SelectedItem is null || SelectedItem.Id is null)
        {
            return;
        }

        var updatedModel = SelectedItem.GetPasswordEntry();
        _vaultManager.EditPasswordEntry(updatedModel);
        _vaultManager.SaveVault(_masterPassword!);
        SelectedItem.UpdatedAt = updatedModel.UpdatedAt;
    }

    [RelayCommand]
    private void RemoveItem()
    {
        if (!CanUseVault() || SelectedItem is null || SelectedItem.Id is null)
        {
            return;
        }

        _vaultManager.RemovePasswordEntry(SelectedItem.Id.Value);
        _vaultManager.SaveVault(_masterPassword!);
        _allPasswordEntries.Remove(SelectedItem);
        FilterPasswordEntries();
    }

    partial void OnSearchTextChanged(string? value)
    {
        FilterPasswordEntries();
    }

    private void FilterPasswordEntries()
    {
        var query = SearchText?.Trim();
        var hasQuery = !string.IsNullOrWhiteSpace(query);

        PasswordEntries.Clear();

        foreach (var entry in _allPasswordEntries)
        {
            if (!hasQuery || MatchesSearch(entry, query!))
            {
                PasswordEntries.Add(entry);
            }
        }

        if (SelectedItem is not null && !PasswordEntries.Contains(SelectedItem))
        {
            SelectedItem = null;
        }
    }

    private static bool MatchesSearch(PasswordEntryViewModel entry, string query)
    {
        return Contains(entry.Title, query)
            || Contains(entry.Login, query)
            || Contains(entry.Url, query)
            || Contains(entry.Note, query);
    }

    private static bool Contains(string? source, string query)
    {
        return !string.IsNullOrWhiteSpace(source)
            && source.Contains(query, StringComparison.OrdinalIgnoreCase);
    }

    private bool CanUseVault()
    {
        return !string.IsNullOrWhiteSpace(_masterPassword) && !_vaultManager.IsVaultBlocked;
    }
}
