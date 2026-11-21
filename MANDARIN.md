# Mandarin, by Tage

Mandarin is a version of Filee 2.5.0 with a liquid-glass design, sounds, and a few new tools. All of Filee's
converters and tools are still there. It is built with source code from Filee (filee.sh,
github.com/KnifeLemon/Filee), used under the MIT license.

## New in Mandarin 2

- **Drop with ⌘ still held.** Start dragging, hold ⌘, move onto a slice and let go. You no longer have to release ⌘
  first. Letting go of ⌘ before the drop still works. Finder isn't told to move anything: the file stays where it is.
- **Clear glass wheel.** The wheel is now neutral glass (white in light mode, graphite in dark mode). Only the slice
  under the file takes your accent colour.
- **New progress ring.** A small glass ring with the accent arc and the real percentage. Until a percentage is known,
  a short arc goes round, and no number is shown. When the file is ready, a green glass check appears and the confirm
  sound plays.
- **Menu bar app.** Mandarin has no Dock icon. Its menu bar icon has **Open Settings…**, **Enable/Disable
  Mandarin**, **Launch at Login**, **About Mandarin** and **Quit Mandarin**. To show the Dock icon again, go to
  **Settings > General**.
- **Panels stay out of the way.** If the settings window is open behind another app, it goes to the menu bar when a
  panel opens from the wheel (Make GIF's frame order, a tool), so only that panel comes forward.
- **Trim Audio** (first on the audio wheel). It shows the waveform with a handle at each end, the start, the end and
  how much is selected. Use **Trim silent ends automatically** and **Play Selected Range**, then **Apply** to save
  "{name} Trimmed" as a new file. The original isn't changed. Playback needs FFmpeg's player (part of the FFmpeg
  download).
- **Accent colours** (Settings > Theme): Mandarin Orange (the default), macOS Blue, Soft Purple, Rose Pink, Mint Green
  and Graphite.
- **Add BG:** the controls are on the left and the picture is large on the right. It no longer appears for GIFs.
- **Error sound:** tools play the Bleep sound when something can't be done.
- New icon and new installer artwork. The icon fills Apple's rounded-square shape, so macOS doesn't put it in a grey
  frame.
- **Merge PDF** on the picture wheel: mark several pictures in Finder, drag them onto it, choose their order, and they
  become one PDF.
- **Add BG** shows the glass progress ring over the panel while it saves, instead of the box in the corner.

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

**Look.** Neutral glass panels, glass buttons, white slider thumbs on an accent track, and the new icon. Light and
dark mode both have their own version (Settings > Theme). On macOS the settings window lets the desktop show through,
blurred. With **Reduce transparency** on, the wheel is drawn solid.

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
4. Allow Mandarin under **Accessibility** and **Input Monitoring** (System Settings > Privacy & Security). Then quit
   Mandarin from its menu bar icon and open it again: macOS only applies these permissions at launch. If
   **Settings > Shortcuts** still says "Global input detection is stopped", remove Mandarin from both lists with
   **−**, add it again with **+**, switch it on, and reopen Mandarin.
5. Select a picture in Finder, hold ⌘ and drag it a little: the wheel opens.

Mandarin doesn't install updates from Filee: the automatic update check is turned off, because an official Filee
update would replace Mandarin. Leave **About > Check for updates automatically** off.

## Not included yet

- A see-through (transparent) background in Add BG: its pictures are always saved with a solid background.
- Light adjustments are only in Edit Photo, for pictures; they aren't remembered between uses.
- The wheel and panels don't blur what's behind them (only the settings window does). The wheel's glass is drawn by
  Mandarin itself, because its drawing framework can't put a blur behind a round shape.
- Trackpad taps need a Force Touch trackpad and a finger on it. macOS may not deliver them while another app is in
  front.
