# Mandarin

Mandarin is a version of Filee 2.5.0 with a warm, orange glass design, your own sounds, and a few new tools. All of
Filee's converters and tools are still there.

## What's new

**The wheel.** Hold **⌘ (Command)** and drag files in Finder to open it. The wheel is now a glass disc with rounded
glass slices. The slice under the pointer turns orange, with a soft click and a light tap on the trackpad. The centre
shows only the files' size in a small glass pill ("2.2 MB"), and the hole is clear, so the file you're dragging stays
visible.

- **Picture wheel, front:** Compress, Metadata, Edit, Annotate, **Add BG**, Crop, Redact.
- **Picture wheel, back (the converter):** press **Tab** to turn the wheel over to JPG, PNG, WEBP, HEIC, TIFF, AVIF,
  BMP and PDF. Press Tab again to turn it back. A soft pop plays as it turns.
- Slices that can't handle the files you dropped are left out, so a PNG gets no "PNG" slice.
- Other file types (PDFs, video, audio and so on) keep their own wheels, in the new glass look.

⌘C, ⌘V, ⌘Tab and every other ⌘ shortcut still work as usual: the wheel opens only when you hold ⌘ **and drag**. To
use another key, go to **Settings > Shortcuts**.

**Add BG.** Puts a picture on a background for social media:

- **Backgrounds:** six pictures cut from your blue gradient (built into the app), ten gradients (tangerine, apricot,
  sky, dusk, ocean and more), twelve colours, or **+** for a picture of your own.
- **Format:** Auto, Original, 1:1, 4:5, 9:16, 16:9, or Custom (type a ratio such as 1080 × 1350).
- **Size and place:** make the picture smaller on its canvas and move it left, right, up or down. Margin, corners and
  shadow are adjustable too. Each slider has a small ↺ to reset it.
- The picture is never cropped, and the original is never changed: Save writes a new PNG next to it.
- To crop instead, use **Crop**; it has 4:5, 9:16 and a custom ratio.

**Edit Photo.** A large preview in a dark well, and a new **Light** group: Exposure, Brightness, Contrast, Highlights
and Shadows, each with its value and a reset. The preview updates as you move them. Size, format, rotation, flip,
quality and black & white are under it, as before. Light starts at 0 each time.

**Sounds.** A click as you move between slices, a pop as the wheel opens, turns over or takes files, a slide as a
panel opens, a confirm sound when a conversion is done, and a bleep when it fails or the files can't go there. Turn
them off, or change the volume, in **Settings > General**. To use your own sound for any of them, click **Open** next
to the data folder there, make a folder named `sounds`, and add files named `open`, `hover`, `drop`, `flip`, `panel`,
`select`, `done` or `failed` (`.wav`, `.aiff`, `.m4a` or `.mp3`).

**Look.** Orange accent, warm orange-to-peach glass panels, glass buttons, white slider thumbs on a red-orange track,
and the new icon. Light and dark mode both have their own version (Settings > Theme).

## Moving from Filee to Mandarin

- **Can they both be installed?** Yes. Mandarin has its own name, bundle ID and settings folder, so it doesn't replace
  or change Filee.
- **Do they compete?** Their wheel gestures differ (Filee: Option + drag, Mandarin: ⌘ + drag), but they share other
  shortcuts (Ctrl + Option + Space, "Convert with…" in Finder). **Don't run both at once.**
- **Your settings:** the first time Mandarin starts, it copies Filee's settings, presets, wheels, history and theme.
  Filee's own folder is only read, never changed. Engines you downloaded in Filee (FFmpeg, LibreOffice…) are shared,
  not downloaded again.
- **Permissions:** macOS treats Mandarin as a new app, so it asks again for Accessibility, Input Monitoring and
  Finder automation.
- **Uninstalling Filee** is not needed. If you later want to, drag Filee from Applications to the Trash. Its settings
  stay in `~/Library/Application Support/Filee` until you delete them.

### Install

1. **Quit Filee:** click its icon in the menu bar and choose **Quit**. In Filee's **Settings > General**, turn off
   **Start Filee when I sign in**, so it doesn't come back at login.
2. Open the `Mandarin-…-osx-arm64.dmg` file and drag **Mandarin** onto **Applications**.
3. Open Mandarin from Applications. This build isn't notarized by Apple, so the first time macOS says it can't check
   it: go to **System Settings > Privacy & Security**, scroll down, and click **Open Anyway** next to Mandarin.
4. Allow Mandarin under **Accessibility** and **Input Monitoring** when asked. If gestures don't work, open System
   Settings > Privacy & Security > Accessibility, remove Mandarin with **−**, add it again with **+**, and switch it on.
5. Select a picture in Finder, hold ⌘ and drag it a little: the wheel opens.

Mandarin doesn't install updates from Filee: the automatic update check is turned off, because an official Filee
update would replace Mandarin. Leave **About > Check for updates automatically** off.

## Not included yet

- A see-through (transparent) background in Add BG: its pictures are always saved with a solid background.
- Light adjustments are only in Edit Photo, for pictures; they aren't remembered between uses.
- Trackpad taps need a Force Touch trackpad and a finger on it. macOS may not deliver them while another app is in
  front.
