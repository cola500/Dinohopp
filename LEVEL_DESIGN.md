---
title: Dinohopp Level Design
description: Level-progression, layout, retry-regler och svårighet.
category: design
status: active
last_updated: 2026-05-18
sections:
  - Progression
  - Level 1 — VIOLA
  - Level 2 — LINDENGARD
  - Retry-regler
  - Mobile UI-anpassning
  - Tekniska anteckningar
  - Framtida levels
---

# Dinohopp — Level Design

## Progression

Linjär 2-nivå-loop:
- **Level 1 (VIOLA)** → Success → "Bra jobbat! Nästa bana!" → SPACE → **Level 2**
- **Level 2 (LINDENGARD)** → Success → "Du klarade Dinohopp!" → SPACE → tillbaka till **Level 1**

Retry på fall stannar alltid på samma level. Bokstäver återställs vid varje restart (level-byte eller retry).

## Level 1 — VIOLA

**Syfte:** introducera mekaniken. 3,5-åringen lär sig dinos auto-run, hopp, bokstavsplockning, mål.

**Ord:** `V I O L A` (5 unika bokstäver, 5 pickups)

**Färgpalett:** 5-färgs VIOLA-set:
- V → korall-röd
- I → gyllen-gul
- O → mjuk-grön
- L → himmelsblå
- A → lavendel

**Sky:** mjuk pastellblå `(0.62, 0.82, 0.93)` — dag

**Layout:**
```
Ground_A [-8..16, 24 enheter]   Mushroom 1–3 (decor)    Letters V, I, O
   ↓ GAP (5 enheter)
Bridge mushroom M4 (y=-1.5)     Letter L på bron
   ↓
Ground_B [21..37, 16 enheter]   Mushroom 5–6 (decor)    Letter A
   ↓
Goal vid x=34 (flagga)
```

**Svårighet:** mycket lätt. Ett gap, en bro-svamp. Letter L sitter på bron så den fångas naturligt.

## Level 2 — LINDENGARD

**Syfte:** behåll samma spelkänsla men variera miljön och utöka banan.

**Ord:** `L I N D E N G A R D` (10 pickups; N och D förekommer två gånger som distinkta slots)

**Färgpalett:** VIOLA-paletten cyklad 2×:
- L₀ korall, I₁ gul, N₂ grön, D₃ blå, E₄ lavendel
- N₅ korall, G₆ gul, A₇ grön, R₈ blå, D₉ lavendel

Tvillingbokstäver får olika färger vilket gör de visuellt distinkta som plats-pickups.

**Sky:** dusty lavender `(0.78, 0.68, 0.85)` — skymning. Träd + moln-parallax delas med Level 1 (samma sprites, ny himmelsfärg ger ny stämning utan ny asset-pipeline).

**Layout:**
```
Ground_A2 [-5..10, 15 enheter]   M2_1–M2_3 decor    Letters L(2), I(5), N(8)
   ↓ GAP 1 (4 enheter)
Bridge M2_4 vid x=12              Letter D på bron
   ↓
Ground_B2 [14..22, 8 enheter]    M2_5 decor          Letters E(15), N(18), G(21)
   ↓ GAP 2 (5 enheter, lite längre)
Bridge M2_6 vid x=24.5            Letter A på bron
   ↓
Ground_C2 [27..40, 13 enheter]   M2_7–M2_8 decor    Letters R(28), D(35)
   ↓
Goal vid x=39 (flagga)
```

**Svårighet:** måttlig. Två gaps istället för ett, banan ~25% längre, fler bokstäver. Ingen pixel-perfect-precision; varje gap har en generös bro-svamp.

## Retry-regler

| Trigger | Vad händer |
|---|---|
| Dino faller (y < -7) | Fall "oops"-ljud spelas en gång. `GameManager.ResetDino()` → dinon teleporteras till nuvarande levels `dinoStartPosition`. `LetterCollectionManager.ResetCollection()` rensar pickup-state + re-aktiverar alla LetterCollectibles i scenen. State = Retry. |
| Tryck SPACE/tap i Retry | Samma level startar om. Bokstäver fräscha. |
| Tryck SPACE/tap i Success (ej sista level) | `GameManager.ActivateLevel(currentIndex + 1)` togglar level-roots, applicerar nya `LevelInfo` (sky, dinoStart, word, colors). ResetDino + state = Playing. |
| Tryck SPACE/tap i Success (sista level) | `ActivateLevel(0)` — loopar tillbaka till Level 1. ResetDino + state = Playing. |

## Mobile UI-anpassning

`VIOLAProgress`-raden använder `LetterCollectionManager.fontSizeShort = 130` för ord ≤ 5 bokstäver, `fontSizeLong = 90` för längre. Sätts automatiskt av `SetWord()`:

- Level 1 VIOLA (5 chars × 130 pt + double-space) ≈ 1100 enheter bred — passar mobil-canvas 1400 wide.
- Level 2 LINDENGARD (10 chars × 90 pt + single-space) ≈ 1130 enheter bred — passar också, med tighter spacing automatiskt.

På iPhone landscape (≈ 812×375) blir effective scale ~0.385 → fonten är ~35 px på Level 2, läsbar för en 3,5-åring som spelar med förälder.

## Tekniska anteckningar

**Level-arkitektur:**
- Varje level är en separat root-`GameObject` (`Level_1`, `Level_2`) med `LevelInfo`-komponent.
- `LevelInfo`: `displayName`, `wordToCollect`, `letterColors`, `skyColor`, `dinoStartPosition`.
- `GameManager.levels[]` är array av `LevelInfo`. `ActivateLevel(int)` togglar `SetActive` på rooten och applicerar config.

**Per-position-tracking:**
- `LetterCollectible` har `positionIndex` (0..N-1). Manager använder `bool[]` istället för `HashSet<char>`. Detta gör att LINDENGARDs N₂ och N₅ är separata pickups som båda måste samlas.

**Delade element (lever utanför level roots):**
- Main Camera, Sky (clouds + trees parallax), Canvas + UI, GameManager, LetterCollectionManager, Dino — finns i scenen oavsett vilken level som är aktiv.
- Bara level-specifikt content (ground, mushrooms, letters, goal) ligger inom respektive `Level_X` root.

**Sky-färg:**
- Kameran's `backgroundColor` ändras runtime av `GameManager.ActivateLevel()`. Trees + clouds är desamma över levels — bara backgrund-färgen ger den nya stämningen.
- `BuildLetterO` använder Level 1's sky-färg som inner-cutout. Letter O finns bara i Level 1, så det blir aldrig en konflikt med Level 2:s sky.

## Framtida levels

Idéer för Level 3+:
- **Level 3 — JÄRNFOTEN** (efternamnet): 9 letters, ny färgpalett, lite trickier
- **Tematisk variation:** snöbana, undervattensbana, regnbågsbana — varje med ny sky-färg + ev. tree-tint.
- **Mekanik-variant:** rörliga svampar, en svamp som studsar dino EXTRA högt (en "super-svamp"), kort tid-baserad bonus.
- **Speedrun-läge:** efter alla levels är klarade, "tid"-stämpel sparas i localStorage.

Allt går att lägga till genom att kalla `BuildLevel3()` i `Build()` och lägga till nytt `LevelInfo` i `gm.levels`-arrayen — inga arkitektur-ändringar.
