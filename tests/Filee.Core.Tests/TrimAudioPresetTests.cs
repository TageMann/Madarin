// Mandarin's Trim Audio: put in the library and first on the audio wheel once, without growing a full face.

using Filee.Core.Presets;
using Filee.Core.Settings;

namespace Filee.Core.Tests;

public class TrimAudioPresetTests
{
    [Fact]
    public void Trim_audio_lands_first_on_the_audio_wheel_after_cut_a_clip()
    {
        using var dir = new TempDir();
        var store = new UserDataStore(dir.Path);
        store.Load();
        Assert.True(store.AddTrimAudio());

        var trim = store.FindPreset(BuiltInData.TrimAudioId);
        Assert.NotNull(trim);
        Assert.True(trim!.IsClipTool);
        Assert.Equal("{name} Trimmed", trim.Output.FileNamePattern);
        var audio = store.Profiles.Single(p => p.Id == "audio");
        Assert.Equal(BuiltInData.TrimAudioId, audio.PresetIds[0]);
        Assert.True(audio.PresetIds.Count <= Filee.Core.Profiles.ToolbarProfile.MaxSlices);
        var clip = store.Presets.FindIndex(p => p.Id == "tool-clip");
        if (clip >= 0)
            Assert.Equal(clip + 1, store.Presets.FindIndex(p => p.Id == BuiltInData.TrimAudioId));
    }

    [Fact]
    public void Adding_it_again_changes_nothing()
    {
        using var dir = new TempDir();
        var store = new UserDataStore(dir.Path);
        store.Load();
        store.AddTrimAudio();
        var count = store.Presets.Count;
        Assert.False(store.AddTrimAudio());
        Assert.Equal(count, store.Presets.Count);
        Assert.Single(store.Profiles.Single(p => p.Id == "audio").PresetIds, id => id == BuiltInData.TrimAudioId);
    }

    [Fact]
    public void A_full_audio_face_gives_up_its_last_slice()
    {
        using var dir = new TempDir();
        var store = new UserDataStore(dir.Path);
        store.Load();
        var audio = store.Profiles.Single(p => p.Id == "audio");
        var spare = store.Presets.Select(p => p.Id).Where(id => !audio.PresetIds.Contains(id)).ToList();
        var next = 0;
        while (audio.PresetIds.Count < Filee.Core.Profiles.ToolbarProfile.MaxSlices)
            audio.PresetIds.Add(spare[next++]);
        var last = audio.PresetIds[^1];

        store.AddTrimAudio();

        Assert.Equal(Filee.Core.Profiles.ToolbarProfile.MaxSlices, audio.PresetIds.Count);
        Assert.DoesNotContain(last, audio.PresetIds);
    }

    [Fact]
    public void Merge_pdf_lands_once_on_the_picture_wheel()
    {
        using var dir = new TempDir();
        var store = new UserDataStore(dir.Path);
        store.Load();
        var images = store.Profiles.Single(p => p.Id == "images");
        images.PresetIds.Remove("merge-pdf");
        images.BackPresetIds?.Remove("merge-pdf");

        Assert.True(store.AddMergePdf());
        Assert.Equal("merge-pdf", images.PresetIds[^1]);
        Assert.False(store.AddMergePdf());
        Assert.Single(images.PresetIds, id => id == "merge-pdf");
    }
}
