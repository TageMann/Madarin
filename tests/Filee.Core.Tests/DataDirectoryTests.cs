// FILEE_DATA_DIR moves the data folder, so a development or test copy never reads or writes the user's own Filee.

using Filee.Core.Settings;

namespace Filee.Core.Tests;

public class DataDirectoryTests
{
    [Fact]
    public void The_variable_moves_the_default_data_folder()
    {
        using var dir = new TempDir();
        var before = Environment.GetEnvironmentVariable(UserDataStore.DirectoryVariable);
        try
        {
            Environment.SetEnvironmentVariable(UserDataStore.DirectoryVariable, dir.Path + Path.DirectorySeparatorChar);
            Assert.Equal(Path.GetFullPath(dir.Path), UserDataStore.OverrideDirectory);
            Assert.Equal(Path.GetFullPath(dir.Path), UserDataStore.DefaultDirectory);

            Environment.SetEnvironmentVariable(UserDataStore.DirectoryVariable, null);
            Assert.Null(UserDataStore.OverrideDirectory);
            Assert.Equal(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Mandarin"),
                UserDataStore.DefaultDirectory);
        }
        finally
        {
            Environment.SetEnvironmentVariable(UserDataStore.DirectoryVariable, before);
        }
    }

    [Fact]
    public void Mandarin_copies_fileees_settings_once_and_leaves_filee_alone()
    {
        using var dir = new TempDir();
        var filee = Path.Combine(dir.Path, "Filee");
        var mandarin = Path.Combine(dir.Path, "Mandarin");
        Directory.CreateDirectory(Path.Combine(filee, "logs"));
        Directory.CreateDirectory(Path.Combine(filee, "sounds"));
        File.WriteAllText(Path.Combine(filee, "settings.json"), "{}");
        File.WriteAllText(Path.Combine(filee, "logs", "filee.log"), "x");
        File.WriteAllText(Path.Combine(filee, "sounds", "done.wav"), "x");

        Assert.True(UserDataStore.AdoptLegacyData(filee, mandarin));

        Assert.True(File.Exists(Path.Combine(mandarin, "settings.json")));
        Assert.True(File.Exists(Path.Combine(mandarin, "sounds", "done.wav")));
        Assert.False(Directory.Exists(Path.Combine(mandarin, "logs")));
        Assert.True(File.Exists(Path.Combine(filee, "settings.json")));
        File.WriteAllText(Path.Combine(mandarin, "settings.json"), "{\"mine\":1}");
        Assert.False(UserDataStore.AdoptLegacyData(filee, mandarin));
        Assert.Contains("mine", File.ReadAllText(Path.Combine(mandarin, "settings.json")));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void An_empty_variable_is_no_override(string? value) => Assert.Null(UserDataStore.DirectoryFrom(value));

    [Fact]
    public void A_relative_folder_is_made_absolute() =>
        Assert.Equal(Path.Combine(Environment.CurrentDirectory, "filee-test"), UserDataStore.DirectoryFrom("filee-test"));
}
