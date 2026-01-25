using System;

namespace PasMan.Models;

public class PasswordEntry
{
    public Guid Id {get;set;}
    public required string Title { get;set;}
    public string? Login { get;set;}
    public string? Password { get;set;}
    public string? Url { get;set;}
    public string? Note { get;set;}
    public DateTimeOffset CreatedAt { get;set;}
    public DateTimeOffset UpdatedAt { get;set;}
}
