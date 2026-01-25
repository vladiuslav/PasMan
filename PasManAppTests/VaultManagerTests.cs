using System;
using System.IO;
using System.Linq;
using System.Reflection;
using PasMan.Models;
using PasManApp.Services;
using Xunit;

namespace PasManAppTests;

public class VaultManagerTests
{
    private static VaultManager CreateManagerWithTempPath(string tempDir)
    {
        Directory.CreateDirectory(tempDir);

        // Only cleanup leftovers that could exist after a crash during save.
        var basePath = Path.Combine(tempDir, "vault.bin");
        TryDelete(basePath + ".tmp");
        // Optional: keep .bak for investigation; safe to delete in tests
        TryDelete(basePath + ".bak");

        var manager = new VaultManager();

        var field = typeof(VaultManager).GetField("vaultFilePath",
            BindingFlags.Instance | BindingFlags.NonPublic);

        Assert.NotNull(field);

        field!.SetValue(manager, basePath);
        Directory.CreateDirectory(Path.GetDirectoryName(basePath)!);

        return manager;
    }

    private static void TryDelete(string path)
    {
        try
        {
            if (File.Exists(path))
                File.Delete(path);
        }
        catch { /* ignore */ }
    }

    private static string GetVaultPath(VaultManager manager)
    {
        var field = typeof(VaultManager).GetField("vaultFilePath",
            BindingFlags.Instance | BindingFlags.NonPublic);

        Assert.NotNull(field);

        return (string)field!.GetValue(manager)!;
    }

    [Fact]
    public void OpenVault_WhenFileDoesNotExist_UnlocksEmptyVault()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "PasManTests", Guid.NewGuid().ToString("N"));
        var master = "test-master";

        var vm = CreateManagerWithTempPath(tempDir);

        vm.OpenVault(master);

        Assert.False(vm.IsVaultBlocked);
        Assert.Empty(vm.SearchPasswordEntries(""));
    }

    [Fact]
    public void RoundTrip_SaveThenOpen_RestoresEntries()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "PasManTests", Guid.NewGuid().ToString("N"));
        var master = "test-master";
        var created = DateTimeOffset.UtcNow;

        var vm1 = CreateManagerWithTempPath(tempDir);
        vm1.OpenVault(master);

        var entry = new PasswordEntry
        {
            Id = Guid.NewGuid(),
            Title = "GitHub",
            Login = "user",
            Password = "pass123",
            Url = "https://github.com",
            Note = "note",
            CreatedAt = created,
            UpdatedAt = created
        };

        vm1.AddPasswordEntry(entry);
        vm1.LockVault(master);

        Assert.True(vm1.IsVaultBlocked);

        var vm2 = CreateManagerWithTempPath(tempDir);
        vm2.OpenVault(master);

        var loaded = vm2.SearchPasswordEntries("").ToList();
        Assert.Single(loaded);

        Assert.Equal(entry.Id, loaded[0].Id);
        Assert.Equal("GitHub", loaded[0].Title);
        Assert.Equal("user", loaded[0].Login);
        Assert.Equal("pass123", loaded[0].Password);
        Assert.Equal("https://github.com", loaded[0].Url);
        Assert.Equal("note", loaded[0].Note);
        Assert.Equal(created, loaded[0].CreatedAt);
        Assert.Equal(created, loaded[0].UpdatedAt);
    }

    [Fact]
    public void OpenVault_WithWrongPassword_Throws()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "PasManTests", Guid.NewGuid().ToString("N"));
        var correct = "correct-password";
        var wrong = "wrong-password";

        var vm1 = CreateManagerWithTempPath(tempDir);
        vm1.OpenVault(correct);

        vm1.AddPasswordEntry(new PasswordEntry
        {
            Id = Guid.NewGuid(),
            Title = "X",
            Login = "Y",
            Password = "Z",
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        });

        vm1.LockVault(correct);

        var vm2 = CreateManagerWithTempPath(tempDir);

        var ex = Assert.Throws<InvalidOperationException>(() => vm2.OpenVault(wrong));
        Assert.Contains("Invalid master password", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void OpenVault_WhenFileIsTampered_Throws()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "PasManTests", Guid.NewGuid().ToString("N"));
        var master = "test-master";

        var vm1 = CreateManagerWithTempPath(tempDir);
        vm1.OpenVault(master);

        vm1.AddPasswordEntry(new PasswordEntry
        {
            Id = Guid.NewGuid(),
            Title = "A",
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        });

        vm1.LockVault(master);

        var path = GetVaultPath(vm1);
        var bytes = File.ReadAllBytes(path);

        // salt(16) + nonce(12) + tag(16) = 44 bytes
        var index = bytes.Length > 60 ? 60 : bytes.Length - 1;
        bytes[index] ^= 0x01;

        File.WriteAllBytes(path, bytes);

        var vm2 = CreateManagerWithTempPath(tempDir);
        Assert.Throws<InvalidOperationException>(() => vm2.OpenVault(master));
    }

    [Fact]
    public void Crud_WhenLocked_Throws()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "PasManTests", Guid.NewGuid().ToString("N"));
        var vm = CreateManagerWithTempPath(tempDir);

        Assert.True(vm.IsVaultBlocked);

        Assert.Throws<InvalidOperationException>(() =>
            vm.AddPasswordEntry(new PasswordEntry
            {
                Id = Guid.NewGuid(),
                Title = "T",
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            }));

        Assert.Throws<InvalidOperationException>(() =>
            vm.EditPasswordEntry(new PasswordEntry
            {
                Id = Guid.NewGuid(),
                Title = "T",
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            }));

        Assert.Throws<InvalidOperationException>(() => vm.RemovePasswordEntry(Guid.NewGuid()));
        Assert.Throws<InvalidOperationException>(() => vm.SearchPasswordEntries("x").ToList());
    }

    [Fact]
    public void Search_FindsByTitleLoginUrlNote()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "PasManTests", Guid.NewGuid().ToString("N"));
        var master = "test-master";
        var now = DateTimeOffset.UtcNow;

        var vm = CreateManagerWithTempPath(tempDir);
        vm.OpenVault(master);

        vm.AddPasswordEntry(new PasswordEntry
        {
            Id = Guid.NewGuid(),
            Title = "Email",
            Login = "alice@example.com",
            Password = "secret",
            Url = "https://mail.example.com",
            Note = "primary inbox",
            CreatedAt = now,
            UpdatedAt = now
        });

        Assert.Single(vm.SearchPasswordEntries("email"));
        Assert.Single(vm.SearchPasswordEntries("alice@"));
        Assert.Single(vm.SearchPasswordEntries("mail.example"));
        Assert.Single(vm.SearchPasswordEntries("inbox"));
        Assert.Empty(vm.SearchPasswordEntries("does-not-exist"));
    }
}
