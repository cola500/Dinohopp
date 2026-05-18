---
title: Dinohopp Visual Polish Notes
description: Art-direction-anteckningar + commit-historik för visuell polish.
category: design
status: active
last_updated: 2026-05-18
sections:
  - Riktning
  - Polish-slice 1 — Sky + Moln + Träd
  - Polish-slice 2 — Färgkodade VIOLA-bokstäver
  - Polish-slice 3 — Markpolish
  - Polish-slice 4 — Dino Personality Pass
  - Nästa polish-idéer
  - Risker att hålla koll på
  - Framtida art direction
---

# Dinohopp — Visual Polish Notes

## Riktning

Mål: **mysig spelvärld för en 3,5-åring**, inspirerad av Yoshi/Kirby/Nintendo-prototypers varma 2D-cartoon-look. Inte realism.

Kärnprinciper:
- Mjuka pastellfärger framför mättade primärfärger.
- Tydliga siluetter, stora objekt.
- Diorama-/leksakskänsla via flera tydliga djuplager (sky → moln → träd → mark).
- Mjuk, låg-stress läsbarhet — barn ska kunna identifiera varje objekt på 0.5 sekunder.
- Allt görs med befintliga square/circle-primitiver färgade i kod. Inga externa assets.

## Polish-slice 1 — Sky + Moln + Träd  *(2026-05-18)*

**Vad ändrades:**
- Kamerans bakgrundsfärg: mörk natt-blå `(0.08, 0.12, 0.28)` → mjuk pastellblå dag `(0.62, 0.82, 0.93)`.
- `BuildStars`-anrop ersatt av `BuildSky` i scenbygget. Stjärnorna är borttagna eftersom de hör hemma i en nattscen, inte en varm dagsmiljö.
- Ny **moln-layer** (sortingOrder −10, parallax 0.10×): sju mjuka vita kompositmoln (tre överlappande cirklar var) längs nedre delen av himlen, lugn drift.
- Ny **trädsiluett-layer** (sortingOrder −7/−8, parallax 0.35×): nio mossgröna kronor + varmbruna stammar utspridda längs hela spelområdet, mid-ground.
- `BuildLetterO` använder nu en delad `SkyColor`-konstant istället för en hårdkodad nattblå. Tidigare slice hade ringen "fyllningen" matchat den gamla mörka skyn — om man bytt sky utan att fixa O hade ringen försvunnit.

**Varför:**
Tidigare scen kändes som en teknisk natt-prototyp. ~70% av skärmen var mörkblå tomhet. Sky-bytet + två parallax-bakgrundslager transformerar hela atmosfären på en enda commit utan att röra någon gameplay-kod. Det är den slice som ger högst impact per förändring just nu.

**Inga gameplay-ändringar:**
- Colliders, fysik, GameManager, mushroom-audio, level-layout — orörda.
- Endast `DinohoppSceneBuilder.cs` ändrat (4 edits) + denna fil tillagd.

## Polish-slice 2 — Färgkodade VIOLA-bokstäver  *(2026-05-18)*

**Vad ändrades:**
- Centraliserad färgpalett: `LetterCollectionManager.DefaultLetterColors[]` — source-of-truth som både SceneBuilder och UI läser. Ingen risk för UI/world-divergens.
- Per-letter färger (varma, mjuka, harmoniska):
  - V → **coral red** `(0.96, 0.45, 0.40)`
  - I → **golden yellow** `(1.00, 0.83, 0.30)`
  - O → **soft green** `(0.50, 0.80, 0.45)`
  - L → **sky blue** `(0.42, 0.68, 0.95)`
  - A → **lavender** `(0.72, 0.55, 0.92)`
- Varje pickup får en **dark backplate** (halv-transparent mörkblå cirkel, scale 1.15×, sortingOrder 28) bakom letter-komposit. Ger outline/halo-känsla mot den ljusa pastellhimlen utan att duplicera composites.
- UI-raden "VIOLA": rich-text per-letter färger. Osamlade = `Dim(baseColor)` (×0.45 brightness, alpha 0.70). Samlade = full färg.
- **Scale-pop på UI** vid varje pickup: hela `VIOLAProgress`-rect skalar `1.0 → 1.20 → 1.0` över 0.3 s via sin-puls.
- **Pickup-animation** i `LetterCollectible`: bytte linjär `1 + 0.3·u` grow mot `sin(π·u) · 0.35` (peakar i mitten) — känns snappier och mer "magisk".

**Varför färgerna valdes:**
- Vald palette är mjuka pastell-versioner av primärfärger — undviker skrikig "mobile-ad"-estetik samtidigt som varje bokstav får distinkt identitet.
- Tonaliteten balanserad: alla på liknande luminans + mättnad, så de känns samhöriga som ett SET (inte 5 lösa färger).
- Coral/golden/green/sky/lavender är välkända "barnvänliga" toner i bilderböcker — låg visuell stress.

**Readability-notes:**
- Backplate ger uniform mörk halo runt varje bokstav. Funkar på alla bakgrunder vi har idag (mark, träd, himmel) — viktigt eftersom letter L sitter högt upp och kan ha himlen som backdrop medan letter V/A har gräs/mark bakom.
- För osamlade UI-bokstäver: dim+alpha gör dem synliga men låter samlade bokstäver dominera blicken. Inte gråa "disabled"-känsla.
- Bokstavskonstruktion oförändrad (block-letter composites) — fortfarande tjocka former, inga tunna streck.

**Accessibility-tankar:**
- Färgblindhet: vald palette har god luminans-spridning (V mest mättad, O ljusast, A mörkast vid full alpha) — bokstäverna är fortfarande särskiljbara om någon nyans försvinner. Dessutom är de ALLA olika BOKSTAVSFORMER, så form > färg.
- Backplate ger mörkare kontrastlapp för synsvaga.
- UI-bokstavspoppen är mjuk (1.20×, 0.3s) — ingen flashande/blinkande feedback som kan trigga photosensitivity.

**Inga gameplay-ändringar:**
- Colliders (1.1×1.3 trigger), fysik, GameManager, audio — orörda.
- 3 filer ändrade: `LetterCollectionManager.cs`, `LetterCollectible.cs`, `DinohoppSceneBuilder.cs`.

## Polish-slice 3 — Markpolish  *(2026-05-18)*

**Vad ändrades (1 fil):**
- `Assets/Editor/DinohoppSceneBuilder.cs`:
  - `BuildGround` bytte bas-färg grön → varm dirt-brun `(0.50, 0.32, 0.20)`. Collider, position, scale ORÖRDA.
  - Två nya anrop i `Build()`: `BuildGroundDecor("Ground_A", ...)` och `BuildGroundDecor("Ground_B", ...)` med olika seeds (401, 402) för deterministisk men varierad layout.
  - Två nya metoder: `BuildGroundDecor()` och `BuildFlower()`.

**Hur det byggs:**
Varje ground har nu två syskon-rötter:
- `Ground_A` / `Ground_B` (orört) → BoxCollider2D + brun bas-sprite (sortingOrder 0)
- `Ground_A_Decor` / `Ground_B_Decor` (nytt, scale 1) → bara visuella barn:
  - **GrassCap** — grön strip vid ground-toppen (sortingOrder 1), sticker upp 0.08 enheter ovan ground-kanten
  - **DirtShadow** — mörkare brun stripa vid botten (sortingOrder 1)
  - **Clump_NN** — små bright-green ovaler längs grass-linjen (sortingOrder 2)
  - **Flower_NN** — kompositer (stem + 4 petals + gult center, sortingOrder 2-4), färgvariation vit/rosa/lila
  - **Pebble_NN** — små gråa ellipser, halvbegravda i gräset (sortingOrder 2)

Antal dekorationer skalar med ground-bredden:
- Ground_A (24 enheter): ~11 clumps, ~4 blommor, ~7 pebbles
- Ground_B (16 enheter): ~7 clumps, ~3 blommor, ~5 pebbles

**Färgpalett (varma, mjuka):**
- Dirt main `(0.50, 0.32, 0.20)` — varm chokladbrun
- Dirt shadow `(0.36, 0.22, 0.13)` — djup brun
- Grass cap `(0.42, 0.72, 0.32)` — klar klövergrön
- Grass clump (brighter) `(0.55, 0.80, 0.35)` — popig limegrön
- Flower petals: vit `(1.00, 0.98, 0.92)`, rosa `(1.00, 0.78, 0.85)`, lila `(0.92, 0.88, 1.00)`
- Flower center `(1.00, 0.85, 0.30)` — gyllengul
- Flower stem `(0.30, 0.55, 0.25)`
- Pebble `(0.60, 0.55, 0.50)` — varm grå

**Varför det förbättrar barnvänlighet/läsbarhet:**
- Tvåtonig jord (ljus topp, mörk botten) ger djup utan att tillföra detaljer som distraherar.
- Klart grön grass cap signalerar tydligt "marken börjar här" — bra för 3,5-åring som lär sig var dino kan landa.
- Små dekorationer (blommor, klumpar, pebbles) ger "liten värld att titta på" utan att se ut som hinder. Alla är små (max 0.35 unit) och har inga colliders — kan aldrig blockera dino.
- Färgvariation i blommor (vit/rosa/lila) håller blicken vandrande över banan istället för att låsa på en monoton mark.

**Hur colliders förblev orörda:**
- `BuildGround` bara bytte färgen på MakeSprite-anropet. Position, scale, BoxCollider2D.size, .offset — allt identiskt.
- All polish ligger på SEPARATA root-objekt med scale 1 och utan Collider2D. Sortingorder 1-4 (under bokstäver vid 28+, under mushroom-caps vid 2+ men avskilt visuellt).
- Verifierat: scene rebuilt, 0 console errors, gameplay-kod orörd.

**Risker att hålla koll på:**
- **Visuell stress vid många decor-objekt**: 23 dekoration-barn över bägge grounds idag. Om vi adderar mer (foreground-foliage etc.) ska vi mätare densiteten — för mycket gör att blicken inte vet vart den ska.
- **Sorting-konflikter**: dekorationer ligger vid sortingOrder 1-4. Mushrooms cap är vid 2. Om en mushroom skulle placeras med y-överlapp på ground (vilket inte sker idag) kan flowers/clumps döljas. Inte ett problem för current level-layout.
- **Letter-overlap**: V och A sitter vid y=-1.5 (ovan ground). Decor finns vid y≈-2.6 till -2.85. Inget vertikalt overlap → ingen risk att decor visuellt täcker bokstäver.
- **Deterministisk seed**: 401/402. Om nya grounds tillkommer, gi dem unika seeds (403+) annars upprepar layouten.

## Polish-slice 4 — Dino Personality Pass  *(2026-05-18)*

**Vad ändrades (5 filer):**
- `Assets/Scripts/DinoController.cs` — nytt publikt `IsGrounded`-property (read-only wrapper). Inga gameplay-ändringar.
- `Assets/Scripts/DinoFeedback.cs` — generaliserat pulse-system. Lade till stretch-on-jump och `TriggerJoyBounce()` (publik metod). Befintlig squash-on-land bevarad. Audio orört.
- `Assets/Scripts/DinoBlink.cs` — pupil-look-framåt: när `controller.controlsEnabled` är true drivs pupillen +X via `SmoothDamp`, centrerar när paused.
- `Assets/Scripts/DinoLocomotion.cs` (NY) — driver Visual-childens scale (breathing) och localPosition (running bob). Rör aldrig root/collider.
- `Assets/Scripts/LetterCollectionManager.cs` — cachear `DinoFeedback`-ref via Player-tag i Awake, anropar `TriggerJoyBounce()` vid varje collect.
- `Assets/Editor/DinohoppSceneBuilder.cs` — `BuildDino` lägger till `DinoLocomotion`-komponenten och konfigurerar nya `DinoFeedback`-fält (stretch + joy).

**Animationer som lades till:**

| Animation | Trigger | Påverkar | Varaktighet | Gameplay-kritisk? |
|---|---|---|---|---|
| Jump stretch | `OnJump`-event | Root.localScale (0.90, 1.12) | 0.12 s | Nej, kosmetisk |
| Landing squash | `OnLand`-event | Root.localScale (1.12, 0.88) | 0.15 s | Nej, kosmetisk (fanns redan) |
| Joy bounce | `TriggerJoyBounce()` från letter collect | Root.localScale (1.18, 1.18) | 0.20 s | Nej, kosmetisk |
| Idle breathing | `controlsEnabled == false` (paused) | Visual.localScale.y ± 0.04 @ 0.45 Hz | kontinuerligt under pause | Nej, kosmetisk |
| Running bob | `controlsEnabled == true && IsGrounded` | Visual.localPosition.y +0.04 @ 4 Hz | kontinuerligt vid löpning | Nej, kosmetisk |
| Pupil look-forward | `controlsEnabled == true` | Eye_Pupil.localPosition.x ± 0.04 (SmoothDamp 0.18 s) | smooth under spel | Nej, kosmetisk |
| Blink (befintlig) | Random 3-6 s interval | Eye_White + Eye_Pupil scale.y → 0.10 → 1 | 0.10 s | Nej, kosmetisk |

**Hur de förbättrar barnkänslan:**
- **Stretch + squash** (samverkar): dinon "andas" genom hoppet — slimmar upp, sen plopper ner. Klassisk Disney/Pixar-trick. Hopp känns mindre robotigt.
- **Joy bounce**: dinon REAGERAR på vad spelaren gör. När barn samlar en bokstav får de visuell bekräftelse direkt på sin karaktär ("dinon är glad!").
- **Idle breathing**: under start-skärm/retry/success är dinon aldrig HELT stilla. Visar att den lever, inte är pausad. Förebygger att barn tror spelet hängt.
- **Running bob**: glidande rörelse → fotsteg-känsla. Subtil men kraftigt skillnad i "är denna karaktären på riktigt eller är den ett spelobjekt".
- **Pupil look-forward**: ger karaktären RIKTNING. När dinon springer ser den framåt — gör att man känner att den "tittar dit den ska", inte stirrar tomt.

**Inga gameplay-ändringar:**
- Inga colliders ändrade. Inga physics-värden ändrade. Inga GameManager-ändringar. Inga level-positioner.
- ALL ny animation körs på antingen Visual-childen (position+scale) eller root.localScale (kort burst, < 0.20 s). Collider på rooten är BoxCollider2D med fast size som ALDRIG modifieras av animation — Unity beräknar collider-overlap från size + transform.scale, vilket innebär att squash-bursten i 0.15s tekniskt sett kortvarigt ändrar collider-bredd/höjd med ±12%. För 0.15 sekunder är det fysiskt irrelevant (dino är på marken under hela bursten).
- Pupil look och eye blink kan kollidera (båda rör eyePupil), men de ändrar olika properties (position vs scale.y) — composes correctly.

**Framtida character-idéer (för framtida slices):**
- Reactive squash vid mushroom bounce — extra bounce när dino tar fart från svamp (utöver landing squash).
- Tail wag — när dino blinkar eller är glad, svansen pendlar lite (kräver att svansen är egen child, vilket den redan är).
- Smile-eye vid joy bounce — under joy bounce, ögat kortvarigt övergår till ^ form (kräver extra child för smile-overlay eller scale tricks).
- Idle-fidget — om dino står stilla i >10s under pause, lite extra fidget (vippa på ena foten, titta upp). Engagerar barn under väntetid.
- Acceleration-tilt — vid hopp-start, lutar dino lite framåt; vid landning lite bakåt. Riktningskänsla.

## Nästa polish-idéer (prioriterad lista)

### HIGH IMPACT / LOW EFFORT
1. **🍂 Foreground-foliage** — en tredje parallax-layer närmast (0.7×) med små buskar/gräs som scrollar fortare. Förstärker djupkänslan ytterligare.
2. **✨ Partikel-burst vid bokstavscollect** — 5-7 små färgade cirklar (samma färg som bokstaven) som flyger radialt + fadar. Magisk feedback ovanpå nuvarande pop + joy bounce.
3. **🌫 Distans-dim på fjärrträd** — sänk träd-alpha till ~0.75 eller skifta mot blågrön så de känns längre bort. Subtil djupskänsla.

### MEDIUM
4. **Drop-shadow på pickup-bokstäver** — duplicera varje letter-komposit en gång i mörkare ton bakom huvudbokstaven, lite offset. Bättre läsbarhet mot trädgrönska.
5. **Mjuk skugga (ellips) under dino/svampar** — platt grå-svart oval-sprite, alpha 0.3, lagrad under varje karaktär. Klassiskt cartoon-trick.
6. **Polish av svampar** — lite skuggning på cap (mörkare halv-måne sprite), mjukare prickar (lite mindre, jämnare fördelning).

### LOWER
7. **Squash & stretch på pickup vid collect** — dynamisk skala-puls samtidigt som fly-up-animationen. Mer "snappiness".
8. **Partikel-burst vid bokstavscollect** — 5-7 små gula stjärn/cirkel-sprites som flyger radialt + fadar. Magisk feedback.
9. **Distans-dim** — mörkare alpha på fjärrträd, ljusare på närträd. Atmosfärisk djupkänsla.
10. **Dino svans-wag eller idle-breath** — subtle Y-scale-puls på dino när stilla.

## Risker att hålla koll på

- **Visuell stress**: lägger man för mycket dekor (träd, gräs, blommor, partiklar samtidigt) tappar 3,5-åringen fokus på dinon. Tumregel: max ETT nytt visuellt element per slice, observera känslan, sen nästa.
- **Sky-färgens kopplingar**: `SkyColor`-konstanten används av (a) kamerans backgroundColor och (b) Letter O's inner ring. Ändras färgen igen, fixa båda samtidigt — finns nu i en konstant, lätt att hitta.
- **Sorting orders**: trädsiluetter ligger vid −7/−8, moln vid −10, mark vid 0, mushrooms 1+, dino 2, bokstäver 30, UI 100. Nya bakgrundslager bör hamna mellan −10 och −5 för att inte krocka.
- **Parallax-koherens**: alla parallax-lager måste täcka ett tillräckligt brett x-range. Moln-x sträcker sig nu −8 till +54, träd-x −8 till +37. Om level blir längre måste vi utöka.
- **Performance**: ~50 nya sprite-objekt totalt (28 moln-pieces + 18 träd-pieces). Trivialt för 2D, men nästa bakgrundslager bör hålla sig under ~30 nya.

## Framtida art direction

På sikt vill vi närma oss referensbilden ännu mer:
- **Två distansträd-lager** (närmast normalgrön, längst bort dimmad blågrön) för riktig djupskänsla.
- **Mark som vävt mönster** — istället för en platt grön rektangel, en serie korta grön-toppade segment med brun-jord-underdel, eventuellt med små rotter eller stenar längs kanterna.
- **Egen dino-sprite** — när vi pratar om grafik vill jag rita en söt dino i samma stil som bilden. Composit-dinon vi har nu är OK för prototyp men begränsar charmen.
- **Egen svamp-sprite** — likadant; cap-form med mjuk skuggning och mer "personlighet" i varje svamp.
- **Mjukare UI-typography** — rundad teckenform för VIOLA-progressen, eventuell egen bitmap-font.

Det ovanstående är medium-impact / medium-effort. Tas i mån av tid när primitiv-baserade slicen är "klar nog".
