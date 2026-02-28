using System;
using CommunityToolkit.Mvvm.ComponentModel;
using PasMan.Models;

namespace PasMan.ViewModels;

public partial class PasswordEntryViewModel : ViewModelBase
{

    /// <summary>
    /// Creates new blank PasswordEntryViewModel
    /// </summary>
    public PasswordEntryViewModel()
    {
        _id = new Guid();
        _title = "Empty"; // Added there for workaround for now because of requirements of title fix later. 
        _createdAt = DateTimeOffset.Now;
        _updatedAt = DateTimeOffset.Now;
    }

    /// <summary>
    /// Creates a new PasswordEntryViewModel for the given <see cref="PasswordEntry"/>
    /// </summary>
    /// <param name="item">The item to load</param>

    public PasswordEntryViewModel(PasswordEntry item)
    {
        _id = item.Id;
        _title = item.Title;
        _login = item.Login;
        _password = item.Password;
        _url = item.Url;
        _note = item.Note;
        _createdAt = item.CreatedAt;
        _updatedAt = item.UpdatedAt;
    }

    /// <summary>
    /// Get PasswordEntry of this View Model
    /// </summary>
    /// <returns>PasswordEntry</returns>
    public PasswordEntry GetPasswordEntry()
    {
        return new PasswordEntry()
        {
            Id = Id ?? Guid.Empty,
            Title = Title ?? "NO NAME",
            Login = Login,
            Password = Password,
            Url = Url,
            Note = Note,
            CreatedAt = CreatedAt,
            UpdatedAt = UpdatedAt

        };
    }

    [ObservableProperty]
    private Guid? _id;
    [ObservableProperty]
    private string? _title;

    [ObservableProperty]
    private string? _login;

    [ObservableProperty]
    private string? _password;

    [ObservableProperty]
    private string? _url;

    [ObservableProperty]
    private string? _note;

    [ObservableProperty]
    private DateTimeOffset _createdAt;

    [ObservableProperty]
    private DateTimeOffset _updatedAt;
}
