# 🎵 Spotify Knob Plugin for StreamDock & MiraBox Craft

<p align="center">
  <b>Native Windows plugin for StreamDock & MiraBox Craft devices with rotary knobs and LCD displays to control Spotify.</b>
</p>

<p align="center">
  <a href="#-english">English</a> •
  <a href="#-italiano">Italiano</a>
</p>

---

## 🇬🇧 English

### ✨ Features

- 🔘 **Knob Single Click** (or keypad key):
  - **Instant Play / Pause** toggle.
  - **Auto-Launch**: Automatically opens Spotify if it is closed.
- 🔀 **Knob Double Click** (or **LCD Touch Tap**):
  - Activates **Track Selection Mode (`⏮ ⏭`)** and resumes playback for instant listening.
  - **Rotate Clockwise (Right)**: Next Track (`⏭`).
  - **Rotate Counter-Clockwise (Left)**: Previous Track (`⏮`).
  - Automatically returns to Volume Mode after 4 seconds of inactivity (or on click).
- 🔊 **Knob Rotation (Volume Mode)**:
  - Adjusts **only the Spotify application volume** in the Windows Audio Mixer (WASAPI CoreAudio session) — leaving master volume and other apps (games, Discord, browser) untouched.
  - Displays transient volume percentage and green progress bar on the LCD.
- 🎨 **Clean Vector Display UI (LCD)**:
  - 100% transparent background with extra-large Spotify logo watermark.
  - **Song Title** in bold white text (with smooth marquee scrolling for long titles).
  - **Artist Name** in bold Spotify green.
  - **Dynamic Top Status Bar**:
    - `▶` Green: Currently playing.
    - `⏸` Compact: Currently paused.
    - `⏮ ⏭` Green Badge: Track Selection Mode active.
  - Pure SVG vector rendering: instantaneous response, ultra-low memory, 0% idle CPU.
- ⚙️ **Integrated Property Inspector**:
  - Configurable volume step (1%, 2%, 5%, 10%, 15%, 20%).
  - Audio mode selector (WASAPI App Volume, System Master, or Keyboard shortcuts).
  - Quick action test buttons directly inside StreamDock / MiraBox Craft.

---

### 🚀 Quick Installation

1. Download or clone `com.luca.spotifyknob.sdPlugin`.
2. Close **StreamDock** (or MiraBox Craft).
3. Copy the `com.luca.spotifyknob.sdPlugin` folder into your StreamDock plugins directory:
   ```text
   %APPDATA%\HotSpot\StreamDock\plugins\
   ```
   *(Press `Win + R`, paste the path above, and press Enter)*.
4. Restart **StreamDock**.
5. Drag and drop the **Spotify Knob** action onto your Knob or keypad button!

---

### 🛠️ Building from Source

The plugin is written in **native C#** and compiles out-of-the-box on any Windows 10/11 system using the built-in Microsoft .NET Framework C# compiler (`csc.exe`):
1. Open the `com.luca.spotifyknob.sdPlugin` directory.
2. Double-click on `compila.bat`.
3. The executable `bin/SpotifyKnob.exe` will be generated instantly.

---

## 🇮🇹 Italiano

### ✨ Funzionalità

- 🔘 **Click Singolo del Knob** (o tasto):
  - **Play / Pausa** immediato con risposta istantanea.
  - Se Spotify è chiuso, lo avvia automaticamente.
- 🔀 **Doppio Click Rapido del Knob** (oppure **Tap sullo Schermo LCD**):
  - Attiva la **Modalità Cambio Brani (`⏮ ⏭`)** e avvia subito la musica per l'ascolto durante lo sfoglio.
  - **Ruota in senso orario (destra)**: Brano Successivo (`⏭`).
  - **Ruota in senso antiorario (sinistra)**: Brano Precedente (`⏮`).
  - Dopo 4 secondi di inattività (o premendo il knob), torna automaticamente al controllo Volume.
- 🔊 **Rotazione del Knob (Modalità Volume)**:
  - Regola **esclusivamente il volume dell'app Spotify** nel mixer di Windows (WASAPI CoreAudio), senza toccare il volume master né quello di giochi/Discord/browser.
  - Mostra il popup temporaneo della percentuale e della barra verde sul display LCD.
- 🎨 **Grafica Display LCD Elegante & Vettoriale**:
  - Sfondo 100% trasparente con logo Spotify extra-large in filigrana.
  - **Titolo del brano** in bianco grande (con scorrimento marquee se lungo).
  - **Nome dell'artista** in verde Spotify in grassetto.
  - **Barra di stato superiore**:
    - `▶` verde: in riproduzione.
    - `⏸` compatto: in pausa.
    - `⏮ ⏭` badge verde: modalità cambio brani attiva.
  - Rendering in puro vettoriale SVG (zero consumo di CPU).
- ⚙️ **Property Inspector Integrato**:
  - Scelta del passo volume (1%, 2%, 5%, 10%, 15%, 20%).
  - Scelta della modalità audio (Volume App WASAPI, Volume Master o tasti rapidi).
  - Pulsanti di test rapido nell'app StreamDock.

---

### 🚀 Installazione Rapida

1. Scarica la cartella `com.luca.spotifyknob.sdPlugin`.
2. Chiudi l'applicazione **StreamDock** (o MiraBox Craft).
3. Copia la cartella `com.luca.spotifyknob.sdPlugin` in:
   ```text
   %APPDATA%\HotSpot\StreamDock\plugins\
   ```
   *(Premi `Win + R`, incolla il percorso sopra e premi Invio)*.
4. Riavvia **StreamDock**.
5. Trascina l'azione **Spotify Knob** sul tuo Knob o su un tasto!

---

### 🛠️ Come Ricompilare il Progetto

1. Apri la cartella `com.luca.spotifyknob.sdPlugin`.
2. Fai doppio clic su `compila.bat`.
3. Il nuovo file `SpotifyKnob.exe` verrà generato automaticamente in `bin/`.

---

## 📁 Project Structure / Struttura del Progetto

```text
com.luca.spotifyknob.sdPlugin/
├── manifest.json                  # StreamDock plugin manifest
├── compila.bat                    # 1-Click native Windows batch compiler
├── bin/
│   └── SpotifyKnob.exe            # Pre-compiled executable ready to use
├── src_csharp/                    # Native C# source code
│   ├── Program.cs                 # WebSocket communication, state & event loop
│   ├── Spotify.cs                 # Spotify detection, Win32 window & media controls
│   ├── Audio.cs                   # WASAPI CoreAudio per-app volume controller
│   ├── SvgRenderer.cs             # Vector SVG LCD interface generator
│   └── SimpleJson.cs              # Lightweight native JSON parser
├── property inspector/            # StreamDock settings UI (HTML/JS/CSS)
├── images/                        # Plugin icons
├── it.json / en.json              # Localization files
└── README.md
```

---

## 📄 License

Distributed under the **MIT License**. Free for personal and commercial use, modification, and redistribution.
