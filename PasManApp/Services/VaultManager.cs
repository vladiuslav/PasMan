using System;
using System.Collections.Generic;
using PasMan.Models;

namespace PasManApp.Services;

public class VaultManager
{
    private readonly IVaultSerializer _vaultSerializer;
    private readonly IVaultEncrypter _vaultEncrypter;
    private readonly IVaultStorer _vaultStorer;

    private List<PasswordEntry> passwordEntries = new List<PasswordEntry>();

    private bool isBlocked = true;
    public bool IsVaultBlocked {get {return isBlocked;}}
    public VaultManager(IVaultSerializer vaultSerializer, IVaultEncrypter vaultEncrypter, IVaultStorer vaultStorer)
    {
        _vaultSerializer = vaultSerializer;
        _vaultEncrypter = vaultEncrypter;
        _vaultStorer = vaultStorer;
    }

    public void AddPasswordEntry()
    {
        
    }
    public void EditPasswordEntry()
    {
        
    }
    public void RemovePasswordEntry()
    {
        
    }
    public void SearchPasswordEntries()
    {
        
    }

    public void LockVault()
    {
        // Remove passwoird Entries from memmory and blocks Vault 
    }

    public void OpenVault(string masterPassword)
    {
        // Tryies to open Entries from memmory and opens vault for usage at the end.
    }


}
