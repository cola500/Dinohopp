---
title: Dinohopp — WebGL build & deploy
description: Bygg Dinohopp till webbläsare och publicera så mobil kan spela.
category: build
status: active
last_updated: 2026-05-18
sections:
  - Snabbstart
  - Bygga lokalt
  - Testa lokalt (Mac)
  - Publicera till GitHub Pages
  - Publicera till itch.io
  - Mobilbrowser-risker
---

# Dinohopp — WebGL build & deploy

## Snabbstart

1. **I Unity Editor:** kör menyn `Tools → Dinohopp → Configure WebGL Build` **EN GÅNG**. Den växlar build-target till WebGL och sätter sane defaults.
2. **I Unity Editor:** kör `Tools → Dinohopp → Build WebGL`. Tar 1–5 min första gången, snabbare efteråt.
3. **I terminal:** `cd Builds/WebGL && python3 -m http.server 8000`
4. **På datorn:** öppna `http://localhost:8000` — spela.
5. **På mobilen:** kolla datorns IP (`ipconfig getifaddr en0` på Mac), öppna `http://<IP>:8000` i mobilbrowsern på samma WiFi.

## Bygga lokalt

### Förkrav
- Unity 6000.4.7f1 (eller senare) med **WebGL Build Support**-modul installerad.
- WebGL-modulen verifierad: `find /Applications/Unity/Hub/Editor -name "WebGLSupport"` → bör returnera en path.

### Konfiguration (engångs)
Meny: `Tools → Dinohopp → Configure WebGL Build`

Vad scriptet sätter:
- `runInBackground = true` (audio fortsätter när fönstret tappar fokus)
- `defaultScreenWidth/Height = 1280 × 720` (landscape baseline)
- `WebGL.compressionFormat = Disabled` — bigger build (~25–35 MB) men funkar på vilken statisk host som helst utan special-headers
- `WebGL.dataCaching = true` (snabbare reload)
- `WebGL.exceptionSupport = None` (minsta möjliga build)
- `WebGL.linkerTarget = Wasm`, `template = APPLICATION:Default`, `threadsSupport = false`
- Build Settings scenes = bara `DinohoppPrototype.unity`
- Active build target = WebGL (växlar plattform om du står på en annan)

### Bygga
Meny: `Tools → Dinohopp → Build WebGL`

Output: `Builds/WebGL/` (i projektrooten, syskon till `Assets/`).

Struktur efter byggning:
```
Builds/WebGL/
├── index.html
├── Build/
│   ├── WebGL.data
│   ├── WebGL.framework.js
│   ├── WebGL.loader.js
│   └── WebGL.wasm
├── StreamingAssets/  (om relevant)
└── TemplateData/     (CSS, ikoner)
```

Vid lyckad build loggas storlek + tid i consolen.

## Testa lokalt (Mac)

```bash
cd Builds/WebGL
python3 -m http.server 8000
```

Öppna `http://localhost:8000` i Chrome eller Safari.

**Testa på mobil i samma WiFi:**
```bash
ipconfig getifaddr en0    # din Macs lokala IP, t.ex. 192.168.1.42
```
Öppna `http://192.168.1.42:8000` i mobilbrowsern.

Om mobilen inte når servern: kontrollera firewall (Mac → Settings → Network → Firewall).

## Publicera till GitHub Pages

Snabbaste vägen:
1. Initiera git i projektroten (om inte redan gjort): `git init && git branch -M main`
2. Skapa repo på GitHub.
3. Kopiera **innehållet** i `Builds/WebGL/` till en ny branch (`gh-pages`) eller till mappen `/docs` på `main`.
4. På GitHub: Settings → Pages → välj branch `gh-pages` (eller `/docs` på `main`) → save.
5. URL blir typ `https://<user>.github.io/<repo>/`.

**Viktigt:** GitHub Pages serverar statiskt utan special-headers. Med `compressionFormat = Disabled` (vår default) funkar det direkt. Om du senare byter till Gzip behöver du antingen `.htaccess`-style headers (inte tillgängligt på Pages) eller stanna kvar på Disabled.

## Publicera till itch.io

itch.io är **det** lättaste sättet att dela en Unity WebGL-build.

1. Zippa `Builds/WebGL/` (inte mappen, **dess innehåll** — `index.html` ska ligga i zippens root).
2. På itch.io: skapa nytt projekt → Kind of project: **HTML**.
3. Ladda upp zippen.
4. Markera "This file will be played in the browser".
5. Embed options: bredd 1280, höjd 720, "Fullscreen button" på, "Mobile friendly" på.
6. Save → spela på `https://<user>.itch.io/<projekt>`.

itch.io hanterar Gzip automatiskt om du senare väljer komprimering.

## Mobilbrowser-risker

| Risk | Vad som händer | Workaround |
|---|---|---|
| **Audio kräver user gesture** | iOS Safari spelar inget ljud tills en tap har skett | Dinohopp:s första tap STARTAR spelet → ljud aktiveras naturligt. Ingen åtgärd behövs. |
| **Performance på äldre mobiler** | Wasm kan stamma på iPhone < 10 / Android < 8 | Vår scen är trivial. Förmodligen inget problem. |
| **WebGL stänger av sig** | Mobil sätter tab i bakgrund → context lost | Spelaren returnerar och får svart skärm. Ladda om sidan. |
| **Touchscreen.current saknas på desktop** | `wasPressedThisFrame` returnerar false | OK — Mouse + Keyboard tar över på desktop. Vår input-pipeline täcker båda. |
| **Pinch-zoom triggar skala** | Mobilen zoomar in på spelet | Unity:s default WebGL-template har en `<meta viewport>` som blockar det. Om problem: anpassa templaten. |
| **Liggande orientering** | Vissa mobiler startar i portrait | Spel är designat för landscape. Spelaren får vrida. Kan vid behov lägga till orientation-hint i templaten. |
| **Build-storlek 25–35 MB** | Långsam första laddning på 4G | OK för delning + WiFi-test. Senare: aktivera Gzip-komprimering (kräver host som sätter `Content-Encoding`). |
| **Cache** | Gammal build cachas | Hård reload (Cmd+Shift+R) eller bump version-string. |

## Felsökning

- **"Failed to switch build target"** under Configure → Unity har inte WebGL-modulen. Installera via Unity Hub → projektets Unity-version → Add Modules.
- **Build hänger** → kontrollera Console för errors. Typiskt missing assemblies eller stora textures (vi har inga).
- **Vit skärm i browsern** → öppna browser-consolen (F12). Vanligaste fel: serverade builden via `file://` (gör inte det) eller MIME-type-fel (använd `python3 -m http.server`).
- **Touch funkar inte** → bekräfta att Unity Input System-paketet är aktivt (`Edit → Project Settings → Player → Active Input Handling = Input System Package`). Vi använder det redan.
