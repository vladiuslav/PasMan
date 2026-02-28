using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace PasMan.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private readonly List<PasswordEntryViewModel> _allPasswordEntries = [];

    public ObservableCollection<PasswordEntryViewModel> PasswordEntries { get; } = [];

    [ObservableProperty]
    private PasswordEntryViewModel? _selectedItem;

    [ObservableProperty]
    private string? _newTitleName;

    [ObservableProperty]
    private string? _searchText;

    [RelayCommand]
    private void AddItem()
    {
        var newEntry = new PasswordEntryViewModel
        {
            Id = Guid.NewGuid(),
            Title = string.IsNullOrWhiteSpace(NewTitleName) ? "Empty" : NewTitleName.Trim()
        };

        NewTitleName = string.Empty;

        _allPasswordEntries.Add(newEntry);
        FilterPasswordEntries();
        SelectedItem = newEntry;
    }

    [RelayCommand]
    private void RemoveItem()
    {
        if (SelectedItem is null)
        {
            return;
        }

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
}
