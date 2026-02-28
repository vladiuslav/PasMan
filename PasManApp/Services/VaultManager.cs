using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text.Json;
using PasMan.Models;

namespace PasManApp.Services;

/// <summary>
/// Manages an encrypted vault of <see cref="PasswordEntry"/> items.
///
/// The vault is stored as an encrypted binary file on disk. This class
/// provides methods to open and lock the vault, perform CRUD operations
/// on the in-memory list of entries, and persist the vault atomically.
/// </summary>
public class VaultManager
{
    private const int SaltSize = 16;
    private const int KeySize = 32;
    private const int NonceSize = 12;
    private const int TagSize = 16;
    private const int Pbkdf2Iterations = 200_000;

    private readonly string vaultFilePath =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                     "PasMan", "vault.bin");

    private List<PasswordEntry> passwordEntries = new();
    private bool isBlocked = true;

    /// <summary>
    /// Gets a value indicating whether the vault is currently locked.
    /// </summary>
    public bool IsVaultBlocked => isBlocked;

    /// <summary>
    /// Initializes a new instance of the <see cref="VaultManager"/> class
    /// and ensures the directory for the vault file exists.
    /// </summary>
    public VaultManager()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(vaultFilePath)!);
    }

    #region CRUD

    /// <summary>
    /// Adds a new <see cref="PasswordEntry"/> to the vault.
    /// </summary>
    /// <param name="entry">The entry to add. If <see cref="PasswordEntry.Id"/> is empty, a new id will be assigned.</param>
    /// <exception cref="InvalidOperationException">Thrown if the vault is locked.</exception>
    public void AddPasswordEntry(PasswordEntry entry)
    {
        EnsureUnlocked();

        if (entry.Id == Guid.Empty)
            entry.Id = Guid.NewGuid();

        var now = DateTimeOffset.UtcNow;
        if (entry.UpdatedAt == default)
            entry.UpdatedAt = now;

        passwordEntries.Add(entry);
    }

    /// <summary>
    /// Updates an existing <see cref="PasswordEntry"/> in the vault.
    /// </summary>
    /// <param name="entry">The entry to update. The entry is matched by its <see cref="PasswordEntry.Id"/>.</param>
    /// <exception cref="InvalidOperationException">Thrown if the vault is locked.</exception>
    public void EditPasswordEntry(PasswordEntry entry)
    {
        EnsureUnlocked();

        var index = passwordEntries.FindIndex(e => e.Id == entry.Id);
        if (index < 0)
            return;

        entry.UpdatedAt = DateTimeOffset.UtcNow;
        passwordEntries[index] = entry;
    }

    /// <summary>
    /// Removes a password entry from the vault by id.
    /// </summary>
    /// <param name="id">The id of the entry to remove.</param>
    /// <exception cref="InvalidOperationException">Thrown if the vault is locked.</exception>
    public void RemovePasswordEntry(Guid id)
    {
        EnsureUnlocked();
        passwordEntries.RemoveAll(e => e.Id == id);
    }

    /// <summary>
    /// Searches the vault for entries containing the specified query in title, login, url, or note.
    /// </summary>
    /// <param name="query">The search query. If <c>null</c>, treated as an empty string.</param>
    /// <returns>An enumerable of matching <see cref="PasswordEntry"/> objects.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the vault is locked.</exception>
    public IEnumerable<PasswordEntry> SearchPasswordEntries(string query)
    {
        EnsureUnlocked();

        query ??= string.Empty;

        return passwordEntries.FindAll(e =>
            (e.Title?.Contains(query, StringComparison.OrdinalIgnoreCase) ?? false) ||
            (e.Login?.Contains(query, StringComparison.OrdinalIgnoreCase) ?? false) ||
            (e.Url?.Contains(query, StringComparison.OrdinalIgnoreCase) ?? false) ||
            (e.Note?.Contains(query, StringComparison.OrdinalIgnoreCase) ?? false));
    }

    #endregion

    #region Vault Control

    public void LockVault(string masterPassword)
    {
        EnsureUnlocked();
        SaveCurrentState(masterPassword);
        passwordEntries.Clear();
        isBlocked = true;
    }

    /// <summary>
    /// Saves the current in-memory vault using <paramref name="masterPassword"/> and then locks the vault.
    /// </summary>
    /// <param name="masterPassword">The master password used to encrypt the vault.</param>
    /// <exception cref="InvalidOperationException">Thrown if the vault is already locked.</exception>

    public void OpenVault(string masterPassword)
    {
        if (!File.Exists(vaultFilePath))
        {
            passwordEntries = new List<PasswordEntry>();
            isBlocked = false;
            return;
        }

        UpdateStateFromFile(masterPassword);
        isBlocked = false;
    }

    /// <summary>
    /// Persists the current unlocked vault to disk using the provided master password.
    /// </summary>
    /// <param name="masterPassword">The master password used to encrypt the vault.</param>
    public void SaveVault(string masterPassword)
    {
        EnsureUnlocked();
        SaveCurrentState(masterPassword);
    }

    /// <summary>
    /// Opens and decrypts the vault using the provided master password.
    /// If the vault file does not exist, creates an empty vault in memory.
    /// </summary>
    /// <param name="masterPassword">The master password used to decrypt the vault.</param>
    /// <exception cref="InvalidOperationException">Thrown if the master password is invalid or the vault file is corrupted.</exception>

    #endregion

    #region Persistence

    // CHANGED: atomic save (temp -> flush -> replace)
    private void SaveCurrentState(string masterPassword)
    {
        EnsureUnlocked();

        byte[] plaintext = JsonSerializer.SerializeToUtf8Bytes(passwordEntries);

        byte[] salt = RandomNumberGenerator.GetBytes(SaltSize);
        byte[] key = DeriveKey(masterPassword, salt);
        byte[] nonce = RandomNumberGenerator.GetBytes(NonceSize);

        byte[] ciphertext = new byte[plaintext.Length];
        byte[] tag = new byte[TagSize];

        using (var aes = new AesGcm(key))
        {
            aes.Encrypt(nonce, plaintext, ciphertext, tag);
        }

        string dir = Path.GetDirectoryName(vaultFilePath)!;
        Directory.CreateDirectory(dir);

        string tempPath = vaultFilePath + ".tmp";
        string backupPath = vaultFilePath + ".bak";

        try
        {
            // Write new data to temp file first
            using (var fs = new FileStream(
                       tempPath,
                       FileMode.Create,
                       FileAccess.Write,
                       FileShare.None,
                       bufferSize: 4096,
                       options: FileOptions.WriteThrough))
            using (var bw = new BinaryWriter(fs))
            {
                bw.Write(salt);
                bw.Write(nonce);
                bw.Write(tag);
                bw.Write(ciphertext);

                bw.Flush();
                fs.Flush(flushToDisk: true);
            }

            // Atomically replace the old vault file
            if (File.Exists(vaultFilePath))
            {
                // Creates/overwrites backupPath
                File.Replace(tempPath, vaultFilePath, backupPath, ignoreMetadataErrors: true);
            }
            else
            {
                // First save
                File.Move(tempPath, vaultFilePath);
            }
        }
        finally
        {
            // Best-effort cleanup if something failed before replace/move
            if (File.Exists(tempPath))
            {
                try { File.Delete(tempPath); } catch { /* ignore */ }
            }

            CryptographicOperations.ZeroMemory(plaintext);
            CryptographicOperations.ZeroMemory(key);
        }
    }

    /// <summary>
    /// Serializes and encrypts the current in-memory vault and atomically writes it to disk.
    /// </summary>
    /// <param name="masterPassword">The master password used to derive the encryption key.</param>
    /// <exception cref="InvalidOperationException">Thrown if the vault is locked.</exception>

    private void UpdateStateFromFile(string masterPassword)
    {
        byte[] file = File.ReadAllBytes(vaultFilePath);

        byte[] salt = file[..SaltSize];
        byte[] nonce = file[SaltSize..(SaltSize + NonceSize)];
        byte[] tag = file[(SaltSize + NonceSize)..(SaltSize + NonceSize + TagSize)];
        byte[] ciphertext = file[(SaltSize + NonceSize + TagSize)..];

        byte[] key = DeriveKey(masterPassword, salt);
        byte[] plaintext = new byte[ciphertext.Length];

        try
        {
            using var aes = new AesGcm(key);
            aes.Decrypt(nonce, ciphertext, tag, plaintext);
        }
        catch (CryptographicException)
        {
            throw new InvalidOperationException("Invalid master password or corrupted vault.");
        }

        passwordEntries =
            JsonSerializer.Deserialize<List<PasswordEntry>>(plaintext)
            ?? new List<PasswordEntry>();

        CryptographicOperations.ZeroMemory(plaintext);
        CryptographicOperations.ZeroMemory(key);
    }

    /// <summary>
    /// Reads the vault file, decrypts it using the provided master password, and updates the in-memory state.
    /// </summary>
    /// <param name="masterPassword">The master password used to derive the decryption key.</param>
    /// <exception cref="InvalidOperationException">Thrown when the master password is incorrect or the vault file is corrupted.</exception>

    #endregion

    #region Helpers

    private static byte[] DeriveKey(string password, byte[] salt)
    {
        using var kdf = new Rfc2898DeriveBytes(
            password,
            salt,
            Pbkdf2Iterations,
            HashAlgorithmName.SHA256);

        return kdf.GetBytes(KeySize);
    }

    /// <summary>
    /// Derives a symmetric encryption key from the given password and salt using PBKDF2 with SHA-256.
    /// </summary>
    /// <param name="password">The password to derive the key from.</param>
    /// <param name="salt">The salt to use for key derivation.</param>
    /// <returns>A byte array containing the derived key.</returns>

    private void EnsureUnlocked()
    {
        if (isBlocked)
            throw new InvalidOperationException("Vault is locked.");
    }

    /// <summary>
    /// Throws <see cref="InvalidOperationException"/> if the vault is currently locked.
    /// </summary>

    #endregion
}
