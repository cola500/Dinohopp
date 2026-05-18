---
title: Dinohopp — WebGL build & deploy
description: Bygg Dinohopp till webbläsare och publicera så mobil kan spela.
category: build
status: active
last_updated: 2026-05-18
sections:
  - Live URLs
  - Mobile WebGL template
  - Snabbstart
  - Bygga lokalt
  - Testa lokalt (Mac)
  - Testa på riktig mobil
  - Ny deploy efter ändringar
  - Publicera till itch.io
  - Mobilbrowser-risker
---

# Dinohopp — WebGL build & deploy

## Live URLs

- **Repo:** https://github.com/cola500/Dinohopp
- **Spel (GitHub Pages):** https://cola500.github.io/Dinohopp/
- **Branches:**
  - `main` — Unity source (Assets/, Packages/, ProjectSettings/, docs)
  - `gh-pages` — bara WebGL-byggets innehåll + `.nojekyll`

Första deploy gjordes 2026-05-18. Pages tar 1–2 min att gå live efter push.

## Mobile WebGL template

Egen WebGL-template: **`Assets/WebGLTemplates/DinohoppMobile/index.html`**.

Vad templaten gör:
- **16:9 letterbox-fit** — canvas storleksanpassas dynamiskt via JS (`fitCanvas()`) så det fyller viewporten utan att förvränga aspect ratio. Svarta band uppe/nere eller på sidorna vid behov.
- **Lyssnar på `resize` + `orientationchange`** — re-fittar när telefonen vrids eller browsern ändrar storlek.
- **Portrait-overlay** — visar "Vänd telefonen liggande" om enheten är touch + portrait. Försvinner automatiskt vid rotation till landscape.
- **Fullskärm-knapp** (top-right) — anropar `requestFullscreen` med webkit-fallback. På iOS Safari ignoreras anropet ibland, men canvas fyller redan viewporten via `100dvh` + `fitCanvas()` så spelet är fullt spelbart utan fullskärm.
- **Dynamic viewport units** (`100dvh`) — undviker att mobile browser-chrome (URL-bar) stjäl pixlar.
- **Safe-area** — fullskärm-knappen respekterar notch/dynamic-island via `env(safe-area-inset-*)`.
- **`touch-action: none`** + `viewport-fit=cover` — blockerar pinch-zoom och pull-to-refresh.

Templaten är aktiv via `PlayerSettings.WebGL.template = "PROJECT:DinohoppMobile"` som sätts av `Tools → Dinohopp → Configure WebGL Build`.

### Maximera vy + iOS "Add to Home Screen"

Riktig fullscreen via `Element.requestFullscreen()` är **blockerad på iOS Safari/Chrome** (Apple-policy för canvas/div). Vår mobile UX hanterar detta:

1. **"⛶ Maximera"-knappen** (top-right):
   - Försöker `requestFullscreen()` först (funkar på Android Chrome, desktop).
   - Faller alltid tillbaka till `body.maximized`-CSS-läge: scrollar viewporten till top, krymper knappen till 75% opacity 0.25 så den inte stör, och kör om `fitCanvas()`. Canvas fyller redan viewporten via 16:9-fit, så maximize-läget är mest en visuell bekräftelse + nedtoning av UI.

2. **iOS-tipsbanner** (visas en gång på iOS):
   - Text: *"📱 På iPhone: Dela ⃫ → 'Lägg till på hemskärmen' för störst spelvy."*
   - Dismiss-knapp (✕) → state sparas i `localStorage` så det inte kommer tillbaka.
   - Visas INTE när enheten redan är i standalone-mode (= redan tillagd på hemskärmen).
   - Visas INTE när portrait-overlay täcker skärmen (för att inte stapla UI).

3. **PWA-manifest** (`manifest.json`):
   - `display: fullscreen`, `orientation: landscape`, theme/background `#0e1929`.
   - Tillsammans med `apple-mobile-web-app-capable=yes` + `apple-mobile-web-app-status-bar-style=black-translucent` ger iOS-användaren en **riktig fullscreen-upplevelse** när spelet är tillagt på hemskärmen och startas därifrån.
   - Detta är "best mobile experience" på iPhone.
   - Note: vi har inga PWA-ikoner än → iOS använder screenshot för hemskärmsikonen. Lägg till `apple-touch-icon` i framtida slice för polerad ikon.

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

## Testa på riktig mobil

Checklista efter `Build WebGL` + `python3 -m http.server 8000`:

1. **Öppna URL:n i mobil-browsern** (Safari på iOS, Chrome på Android).
2. **Håll mobilen i portrait** → ska visa stor "Vänd telefonen liggande"-overlay med ↩️-ikon.
3. **Vrid till landscape** → overlay försvinner, dinon syns mitt på skärmen.
4. **Canvas ska fylla skärmen i 16:9** — svarta band ovanpå/under eller på sidorna vid behov. Inga pinch-zoom-glapp. Inga URL-bar-glapp.
5. **Tryck "⛶ Fullskärm"** (top-right) — på Android Chrome ska browsern gå i fullskärm. På iOS Safari ignoreras anropet ofta, men spelet ska ändå fylla viewporten.
6. **Tryck på skärmen** → starta spelet. Tryck igen för att hoppa. Bokstavscollect, fall, mål — allt ska fungera.
7. **Ljud:** första tap aktiverar AudioContext (iOS Safari-kravet). Hopp/landning/svampbounce ska höras.
8. **Vrid till portrait mitt i spelet** → overlay återkommer. Vrid tillbaka → spelet fortsätter.

## Ny deploy efter ändringar

Repo är redan satt upp. För varje ny deploy:

### 1. Bygg om i Unity
Meny `Tools → Dinohopp → Build WebGL` — output i `Builds/WebGL/`.

### 2. Pusha source-ändringar till `main` (valfritt men rekommenderat)
```bash
cd /Users/johanlindengard/Development/Dinohopp/Dinohopp
git add Assets/ Packages/ ProjectSettings/ *.md
git commit -m "<beskrivning av ändringen>"
git push origin main
```

### 3. Pusha ny WebGL-build till `gh-pages`
Använder en temporär git worktree så `main` aldrig får build-artefakter:

```bash
cd /Users/johanlindengard/Development/Dinohopp/Dinohopp

# 1) Skapa eller återanvänd worktree för gh-pages
WORKTREE=/tmp/dinohopp-gh-pages
rm -rf "$WORKTREE"
git worktree add "$WORKTREE" gh-pages

# 2) Töm gamla filer + kopiera in ny build (utan .DS_Store)
rm -rf "$WORKTREE"/Build "$WORKTREE"/TemplateData "$WORKTREE"/index.html
rsync -a --exclude '.DS_Store' Builds/WebGL/ "$WORKTREE"/
touch "$WORKTREE/.nojekyll"

# 3) Commit + push
cd "$WORKTREE"
git add .
git commit -m "Deploy WebGL build $(date +%Y-%m-%d)"
git push origin gh-pages
cd -

# 4) Städa upp
git worktree remove "$WORKTREE" --force
```

### 4. Vänta + verifiera
Pages tar 1–2 min att republikera. Öppna https://cola500.github.io/Dinohopp/ och hård-reload (`Cmd+Shift+R`) så cachen inte serverar den gamla builden.

**Viktigt:** GitHub Pages serverar statiskt utan special-headers. Med `compressionFormat = Disabled` (vår default i `DinohoppBuildSettings.cs`) funkar det direkt. Om du senare byter till Gzip behöver du en host som sätter `Content-Encoding: gzip` — Pages kan, men inte garanterat. Stanna på Disabled tills du behöver minska filstorleken.

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
| **Liggande orientering** | Vissa mobiler startar i portrait | DinohoppMobile-templaten visar "Vänd telefonen"-overlay i portrait, försvinner i landscape. |
| **iOS Safari fullskärm** | `requestFullscreen()` ignoreras alltid på iOS Safari/Chrome (Apple-policy för canvas/div) | **Förväntat beteende** — "⛶ Maximera"-knappen aktiverar `body.maximized`-fallback istället: scrollar till top, krymper knappen, canvas fyller redan viewporten via `fitCanvas()`. För riktig fullskärm på iOS: tipsbanner uppe på skärmen rekommenderar **"Lägg till på hemskärmen"** via manifest+apple-meta-taggar. |
| **iOS Safari URL-bar** | URL-bar tar pixlar tills första scroll | `100dvh` lägger canvas över hela visible-viewporten. När user trycker överst skickas URL-baren bort automatiskt vid första `requestFullscreen`-anrop på Android. |
| **iOS Chrome** | Använder WebKit pga Apple-policy, samma fullscreen-begränsning som Safari | Samma fallback gäller. |
| **Build-storlek 25–35 MB** | Långsam första laddning på 4G | OK för delning + WiFi-test. Senare: aktivera Gzip-komprimering (kräver host som sätter `Content-Encoding`). |
| **Cache** | Gammal build cachas | Hård reload (Cmd+Shift+R) eller bump version-string. |

## Felsökning

- **"Failed to switch build target"** under Configure → Unity har inte WebGL-modulen. Installera via Unity Hub → projektets Unity-version → Add Modules.
- **Build hänger** → kontrollera Console för errors. Typiskt missing assemblies eller stora textures (vi har inga).
- **Vit skärm i browsern** → öppna browser-consolen (F12). Vanligaste fel: serverade builden via `file://` (gör inte det) eller MIME-type-fel (använd `python3 -m http.server`).
- **Touch funkar inte** → bekräfta att Unity Input System-paketet är aktivt (`Edit → Project Settings → Player → Active Input Handling = Input System Package`). Vi använder det redan.
