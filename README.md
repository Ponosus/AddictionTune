# AddictionTune

AddictionTune is a minimalistic desktop audio player built with Python, CustomTkinter, and VLC. It features curated audio atmospheres designed for different mental states (Active, Focus, Relax) and utilizes an embedded mini-player with complete media and volume controls for a seamless background listening experience.

## Features

* **Mood-Based Presets:** Quick access to tailored audio streams (Breakcore/Jungle, Maidcore, Ambient/Lo-Fi).
* **Dual-UI Layout:** Full-screen detailed player and a compact sticky mini-player.
* **Synchronized Controls:** Real-time volume and playback synchronization between both player views.
* **Asynchronous Streaming:** Powered by `yt_dlp` and `python-vlc` for fast, non-blocking audio loading and playback.
* **Onboarding Screen:** A built-in user guide for first-time launches (can be re-accessed at any time).
* **Dynamic Theme Toggle:** Support for both Light and Dark modes.

---

## Compilation Guide

To compile this Python script into a standalone executable (`.exe`) for Windows, follow the instructions below. 

Because the application relies on the external VLC media framework (`libvlc.dll`), you must bundle the required binary assets alongside the compiled folder for the application to start properly.

Installing

1. Install PyInstaller via your terminal:
   ```bash
   pip install pyinstaller

Download or locate a standard 64-bit installation of VLC Media Player on your machine (usually found at C:\Program Files\VideoLAN\VLC).

2. Asset Preparation
Before compiling, copy the following files and folders from your VLC installation directory directly into your project's root folder (where your main.py is located):

libvlc.dll
libvlccore.dll
plugins (this directory contains necessary audio codecs)

Your project structure should look like this before building:

YourProject/
├── main.py
├── libvlc.dll
├── libvlccore.dll
└── plugins/
Build Command
Open your terminal or command prompt in the project root directory and execute the following command:

command for compiling:
```bash
pyinstaller --noconfirm --onedir --windowed --add-data "libvlc.dll;." --add-data "libvlccore.dll;." --add-data "plugins;plugins" "Addiction.py"
