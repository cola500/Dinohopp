---
title: Dinohopp Audio Assets
description: Källor, licens och attribution för ljudfilerna i Dinohopp.
category: assets
status: active
last_updated: 2026-05-17
sections:
  - Innehåll
  - Källa och licens
  - Att byta ut placeholder-ljud
---

# Dinohopp Audio

## Innehåll

### Dino + standard bounce
| Fil | Användning | Längd |
|-----|------------|-------|
| `jump.wav` | Spelas när dinon hoppar (`DinoFeedback.jumpClip`) | ~0.18 s |
| `land.wav` | Spelas när dinon landar (`DinoFeedback.landClip`) | ~0.22 s |
| `mushroom-bounce.wav` | Default bounce + fallback om voice-fil saknas | ~0.15 s |

### Per-mushroom voices (Generated/)
Varje svamp i Level 1 har en egen voice-clip med distinkt timbre. Pentatonisk pitch-multiplier appliceras ovanpå (1.0–2.0×) så hopp-sekvensen även blir en uppåtgående skala.

| Fil | Svamp | Beskrivning | Längd |
|-----|-------|-------------|-------|
| `Generated/mushroom_1_soft_boing.wav` | M1 | Mjukt boing, ren sinus, lång attack | ~0.22 s |
| `Generated/mushroom_2_pip.wav` | M2 | Kort pip, fågelchirp | ~0.08 s |
| `Generated/mushroom_3_plop_bubble.wav` | M3 | Bubble-plopp, pitch-sweep nedåt | ~0.18 s |
| `Generated/mushroom_4_big_boing.wav` | M4 (bro) | Fet boing, fundamental + oktav-harmonisk + vibrato | ~0.35 s |
| `Generated/mushroom_5_ding.wav` | M5 | Bell-aktig ding, lång ringande decay | ~0.50 s |
| `Generated/mushroom_6_happy_pop.wav` | M6 | Glad pop, snabb uppåtgående sweep | ~0.10 s |

## Källa och licens

**Alla filer:** programmatiskt syntetiserade av `Assets/Editor/DinohoppAudioBuilder.cs` (sinus + ASR-envelope + valfri oktav-harmonisk + valfritt vibrato, 16-bit mono PCM, 44.1 kHz). Inga externa samples ingår.

- **Författare:** Genererade i Dinohopp-projektet
- **Licens:** [CC0 1.0 Universal (Public Domain Dedication)](https://creativecommons.org/publicdomain/zero/1.0/)
- **Attribution:** Krävs ej
- **Datum:** 2026-05-17

För att regenerera filerna identiskt:
- **Tools → Dinohopp → Generate Placeholder Audio** — `jump.wav`, `land.wav`, `mushroom-bounce.wav`.
- **Tools → Dinohopp → Generate Mushroom Voices** — alla sex voice-clips i `Generated/`.

Båda menyerna är deterministiska — samma WAV-data varje gång.

## Att byta ut placeholder-ljud

Filerna är medvetet enkla placeholders. När du vill ha "riktiga" ljud:

1. Hämta en CC0-WAV (rekommenderat: [Kenney](https://kenney.nl/assets/category:Audio?sort=update) eller [OpenGameArt CC0-filter](https://opengameart.org/content-search/?searchword=&field_art_licenses_tid%5B%5D=4)).
2. **Ersätt filen på plats** — behåll filnamnen `jump.wav`, `land.wav`, `mushroom-bounce.wav`. Då plockar `DinohoppSceneBuilder` upp dem automatiskt vid nästa scenbygge utan koddrift.
3. Uppdatera tabellen ovan med ny källa, författare, licens, attribution och datum.

**Undvik:**
- Mario/Nintendo-liknande ljud (varumärkesintrång)
- Ljud utan tydlig CC0/CC-BY/public-domain-licens
- Skrikiga eller höga effekter — målgrupp är 3,5 år
