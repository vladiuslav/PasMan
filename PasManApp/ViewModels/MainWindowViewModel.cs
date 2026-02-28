using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
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
    private PasswordEntryViewModel? _trackedItem;

    public ObservableCollection<PasswordEntryViewModel> PasswordEntries { get; } = [];

    [NotifyCanExecuteChangedFor(nameof(ChangeItemCommand))]
    [ObservableProperty]
    private PasswordEntryViewModel? _selectedItem;

    [ObservableProperty]
    private string? _newTitleName;

    [ObservableProperty]
    private string? _searchText;

    [NotifyCanExecuteChangedFor(nameof(ChangeItemCommand))]
    [ObservableProperty]
    private bool _isSelectedItemDirty;

    [NotifyCanExecuteChangedFor(nameof(ChangeItemCommand))]
    [ObservableProperty]
    private string? _urlValidationMessage;

    [ObservableProperty]
    private bool _isPasswordVisible;

    public bool HasUrlValidationError => !string.IsNullOrWhiteSpace(UrlValidationMessage);

    public char PasswordMaskChar => IsPasswordVisible ? '\0' : '*';

    public string PasswordVisibilityButtonText => IsPasswordVisible ? "Hide" : "Show";

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

        SelectedItem = null;
        IsSelectedItemDirty = false;
        UrlValidationMessage = null;
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
        newEntry.CreatedAt = model.CreatedAt;
        _allPasswordEntries.Add(newEntry);
        FilterPasswordEntries();
        SelectedItem = newEntry;
    }

    [RelayCommand(CanExecute = nameof(CanSaveSelectedItem))]
    private void ChangeItem()
    {
        if (!CanSaveSelectedItem())
        {
            return;
        }

        var selectedItem = SelectedItem!;
        var updatedModel = selectedItem.GetPasswordEntry();
        _vaultManager.EditPasswordEntry(updatedModel);
        _vaultManager.SaveVault(_masterPassword!);
        selectedItem.UpdatedAt = updatedModel.UpdatedAt;
        IsSelectedItemDirty = false;
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

    partial void OnSelectedItemChanged(PasswordEntryViewModel? value)
    {
        if (_trackedItem is not null)
        {
            _trackedItem.PropertyChanged -= OnSelectedItemPropertyChanged;
        }

        _trackedItem = value;
        if (_trackedItem is not null)
        {
            _trackedItem.PropertyChanged += OnSelectedItemPropertyChanged;
            ValidateSelectedItemUrl();
        }
        else
        {
            UrlValidationMessage = null;
        }

        IsSelectedItemDirty = false;
    }

    partial void OnIsPasswordVisibleChanged(bool value)
    {
        OnPropertyChanged(nameof(PasswordMaskChar));
        OnPropertyChanged(nameof(PasswordVisibilityButtonText));
    }

    partial void OnUrlValidationMessageChanged(string? value)
    {
        OnPropertyChanged(nameof(HasUrlValidationError));
    }

    [RelayCommand]
    private void TogglePasswordVisibility()
    {
        IsPasswordVisible = !IsPasswordVisible;
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

    private bool CanSaveSelectedItem()
    {
        return CanUseVault()
               && SelectedItem is not null
               && SelectedItem.Id is not null
               && IsSelectedItemDirty
               && string.IsNullOrWhiteSpace(UrlValidationMessage);
    }

    private void OnSelectedItemPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (SelectedItem is null)
        {
            return;
        }

        if (e.PropertyName == nameof(PasswordEntryViewModel.Url))
        {
            ValidateSelectedItemUrl();
        }

        if (e.PropertyName == nameof(PasswordEntryViewModel.Title)
            || e.PropertyName == nameof(PasswordEntryViewModel.Login)
            || e.PropertyName == nameof(PasswordEntryViewModel.Password)
            || e.PropertyName == nameof(PasswordEntryViewModel.Url)
            || e.PropertyName == nameof(PasswordEntryViewModel.Note))
        {
            IsSelectedItemDirty = true;
        }
    }

    private void ValidateSelectedItemUrl()
    {
        if (SelectedItem is null)
        {
            UrlValidationMessage = null;
            return;
        }

        var url = SelectedItem.Url?.Trim();
        if (string.IsNullOrWhiteSpace(url))
        {
            UrlValidationMessage = null;
            return;
        }

        var isValid = Uri.TryCreate(url, UriKind.Absolute, out var parsed)
                      && (parsed.Scheme == Uri.UriSchemeHttp || parsed.Scheme == Uri.UriSchemeHttps);

        UrlValidationMessage = isValid ? null : "URL must start with http:// or https://";
    }

}
