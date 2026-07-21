# AddictionTune

**AddictionTune** is a minimalist, zero-distraction desktop audio player designed for ADHD and neurodivergent listeners. Instead of endless libraries and playlists, it offers three curated audio atmospheres matched to mental states — press one button and the right sound just plays. No accounts, no ads, no doomscrolling through recommendations.

Built with **C# / WPF (.NET 8)**, **LibVLCSharp**, and **yt-dlp**.

## Why

For many neurodivergent people, the hardest part of listening to music is *choosing* it. Every decision point is a distraction trap. AddictionTune removes the choice: pick a mental state, get a shuffled stream of the right genre, and get back to what you were doing. The sticky mini-player keeps controls one glance away without stealing focus.

## Atmospheres

| Preset | Genres | Mental state |
| --- | --- | --- |
| 🔴 **ACTIVE** | Breakcore / Jungle | High-energy stimulation, dopamine boost |
| 🔵 **FOCUS** | Maidcore | Steady rhythm for deep work and studying |
| 🟢 **RELAX** | Ambient / Lo-Fi | Decompression and sensory calm |

## Features

- **One-click mood presets** — three curated atmospheres, shuffled on every launch.
- **Dual-UI layout** — a full-screen player plus a compact sticky mini-player that stays out of your way.
- **Resizable mini-player** — drag its edges to make it wider or narrower (within sane limits).
- **Gapless-feeling playback** — the next track's stream URL is prefetched in the background, so auto-advance and skipping are near-instant.
- **Stutter-resistant streaming** — 3-second network buffer and automatic HTTP reconnect.
- **Click-to-seek progress bar** — click anywhere on the timeline to jump straight to that timecode.
- **Synchronized controls** — playback and volume stay in sync between the full player and the mini-player.
- **3 languages** — English, Russian, and Spanish. Picked once on first launch, changeable anytime in Settings.
- **Light & dark themes** — with theme-aware icons.
- **Settings menu** — ⚙ button with Info (onboarding replay), Language, and About pages.
- **Self-maintaining** — yt-dlp auto-updates itself on launch, and the app notifies you when a newer VLC runtime is available.
- **Fully self-contained builds** — one `dotnet publish` produces a folder with everything bundled: .NET runtime, LibVLC, and yt-dlp. No installers, no dependencies.

## Tech stack

- **.NET 8 / WPF** — native Windows UI with smooth page-transition animations
- **LibVLCSharp + VideoLAN.LibVLC.Windows** — rock-solid audio engine, codecs included
- **yt-dlp** — audio stream resolution (bundled, self-updating)

## Building from source

Requirements: **Windows 10/11**, **.NET 8 SDK**.

git clone https://github.com/Ponosus/AddictionTune.git

cd AddictionTune

dotnet publish -c Release -r win-x64 --self-contained true -o publish

The `publish` folder will contain `AddictionTune.exe` with everything bundled — the .NET runtime, LibVLC binaries, and `yt-dlp.exe`. Copy the folder anywhere and run.

> **Note:** `yt-dlp.exe` ships with the repository and is copied to the output automatically. If you ever need to refresh it manually, grab the latest from [yt-dlp releases](https://github.com/yt-dlp/yt-dlp/releases/latest/download/yt-dlp.exe) and drop it next to the `.csproj`.

## Project structure

​
AddictionTune/

├── AddictionTune.csproj

├── App.xaml / App.xaml.cs        # theme management

├── MainWindow.xaml(.cs)          # all pages, player, mini-player

├── Models/                       # Track, Preset

├── Services/                     # AudioEngine, YtDlpService, UpdateChecker,

├── Themes/                       # Dark / Light dictionaries, control styles

├── Assets/                       # icons

└── yt-dlp.exe                    # bundled stream resolver

## Author

Made by **Sewerbox**

- GitHub: [github.com/Ponosus](https://github.com/Ponosus/)
- Telegram: [@VestronVulture](https://t.me/VestronVulture)

Enjoy! 🎧
