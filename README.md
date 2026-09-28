# 🎵 Spotify Knob Plugin per StreamDock & MiraBox Craft

Plugin nativo per **StreamDock** e **MiraBox Craft** (dispositivi 293S, N4ProE, Ajazz, Fifine, ecc. con display LCD e manopola rotativa Knob) per il controllo completo di **Spotify** su Windows 10 e Windows 11.

---

## ✨ Funzionalità Principali

- 🔘 **Click Singolo del Knob** (o tasto):
  - **Play / Pausa** immediato con risposta istantanea.
  - Se Spotify è chiuso, lo avvia automaticamente.
- 🔀 **Doppio Click Rapido del Knob** (o **Tap sullo Schermo LCD**):
  - Attiva la **Modalità Cambio Brani (`⏮ ⏭`)** e avvia subito la musica per l'ascolto.
  - **Ruota in senso orario (destra)**: Brano Successivo (`⏭`).
  - **Ruota in senso antiorario (sinistra)**: Brano Precedente (`⏮`).
  - Dopo 4 secondi di inattività (o con un click del knob), torna automaticamente alla modalità Volume.
- 🔊 **Rotazione del Knob (Modalità Volume)**:
  - Regola **esclusivamente il volume dell'app Spotify** nel mixer di Windows (WASAPI), senza toccare il volume master né quello di giochi/Discord/browser.
  - Mostra il popup temporaneo della percentuale e della barra verde sul display LCD.
- 🎨 **Grafica Display LCD Elegante & Leggera**:
  - Sfondo 100% trasparente con logo Spotify extra-large in filigrana.
  - **Titolo del brano** in bianco grande (con scorrimento marquee se lungo).
  - **Nome dell'artista** in verde Spotify in grassetto.
  - **Barra di stato superiore**:
    - `▶` verde: in riproduzione.
    - `⏸` compatto: in pausa.
    - `⏮ ⏭` badge verde: modalità cambio brani attiva.
  - Grafica generata in puro vettoriale SVG (nessun ritardo e zero consumo di CPU).
- ⚙️ **Property Inspector Integrato**:
  - Scelta del passo volume (1%, 2%, 5%, 10%, 15%, 20%).
  - Scelta della modalità audio (Volume App WASAPI, Volume Master o tasti rapidi).
  - Pulsanti di test rapido nell'app StreamDock.

---

## 🚀 Installazione Rapida

1. Scarica la cartella `com.luca.spotifyknob.sdPlugin`.
2. Chiudi l'applicazione **StreamDock** (o MiraBox Craft).
3. Copia la cartella `com.luca.spotifyknob.sdPlugin` in:
   ```text
   %APPDATA%\HotSpot\StreamDock\plugins\
   ```
   *(Puoi arrivarci premendo `Win + R`, incollando il percorso sopra e premendo Invio)*.
4. Riavvia **StreamDock**.
5. Trascina l'azione **Spotify Knob** sul tuo Knob o su un tasto!

---

## 🛠️ Come Ricompilare il Progetto

Il plugin è scritto in **C# nativo** e non richiede installazioni esterne (né Node.js, né Python, né Visual Studio pesante): sfrutta il compilatore C# già presente in ogni computer Windows (`csc.exe`).

Per ricompilare:
1. Apri la cartella `com.luca.spotifyknob.sdPlugin`.
2. Fai doppio clic su `compila.bat`.
3. Il nuovo file `SpotifyKnob.exe` verrà generato automaticamente in `bin/`.

---

## 📁 Struttura del Repository

```text
com.luca.spotifyknob.sdPlugin/
├── manifest.json                  # Manifest del plugin per StreamDock
├── compila.bat                    # Script di compilazione nativa 1-click
├── bin/
│   └── SpotifyKnob.exe            # Eseguibile precompilato pronto all'uso
├── src_csharp/                    # Codice sorgente C#
│   ├── Program.cs                 # Connessione WebSocket, loop e gestione eventi
│   ├── Spotify.cs                 # Rilevamento Spotify, Win32 e controlli multimediali
│   ├── Audio.cs                   # Controllo volume mixer per-app WASAPI CoreAudio
│   ├── SvgRenderer.cs             # Generatore interfaccia vettoriale SVG per LCD
│   └── SimpleJson.cs              # Parser JSON nativo super leggero
├── property inspector/            # Interfaccia impostazioni StreamDock (HTML/JS/CSS)
├── images/                        # Icone del plugin
├── it.json / en.json              # File di localizzazione
└── README.md
```

---

## 📄 Licenza

Distribuito con licenza MIT. Libero per l'uso, la modifica e la redistribuzione.
