using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class DinohoppSceneBuilder
{
    const string ScenePath = "Assets/Scenes/DinohoppPrototype.unity";
    const string SquareSpritePath = "Assets/Sprites/Square.png";
    const string CircleSpritePath = "Assets/Sprites/Circle.png";

    // Shared sky color — also used as the "hollow" inside Letter O so the ring reads.
    static readonly Color SkyColor = new Color(0.62f, 0.82f, 0.93f, 1f);

    [MenuItem("Tools/Dinohopp/Log Alignment")]
    public static void LogAlignment()
    {
        var dino = GameObject.FindWithTag("Player");
        if (dino != null)
        {
            var col = dino.GetComponent<BoxCollider2D>();
            var foot = dino.transform.Find("Visual/SurfaceCurve/Foot_Left");
            float footBottom = foot != null
                ? foot.position.y - foot.localScale.y * 0.5f
                : float.NaN;
            Debug.Log($"[Alignment] Dino  collider y: [{col.bounds.min.y:F3} .. {col.bounds.max.y:F3}]  |  feet bottom y: {footBottom:F3}");
        }

        var mushrooms = Object.FindObjectsByType<MushroomBounceFeedback>(FindObjectsInactive.Exclude);
        foreach (var m in mushrooms)
        {
            var col = m.GetComponent<BoxCollider2D>();
            var cap = m.transform.Find("Visual/Cap");
            float capTop = cap != null
                ? cap.position.y + cap.localScale.y * 0.5f
                : float.NaN;
            Debug.Log($"[Alignment] {m.gameObject.name}  collider top y: {col.bounds.max.y:F3}  |  cap top y: {capTop:F3}");
        }

        var goal = Object.FindAnyObjectByType<Goal>();
        if (goal != null)
        {
            var col = goal.GetComponent<BoxCollider2D>();
            var flag = goal.transform.Find("Visual/Flag");
            string flagInfo = flag != null
                ? $"x: [{flag.position.x - flag.localScale.x * 0.5f:F3} .. {flag.position.x + flag.localScale.x * 0.5f:F3}]"
                : "(no flag visual found)";
            Debug.Log($"[Alignment] Goal  trigger x: [{col.bounds.min.x:F3} .. {col.bounds.max.x:F3}]  |  flag visual {flagInfo}");
        }
    }

    [MenuItem("Tools/Dinohopp/Build Prototype Scene")]
    public static void Build()
    {
        EnsureFolders();
        var square = EnsureSquareSprite();
        var circle = EnsureCircleSprite();

        // Lazily generate placeholder SFX so the scene comes out audible on first run.
        if (!DinohoppAudioBuilder.AudioExists())
            DinohoppAudioBuilder.Generate();
        if (!DinohoppAudioBuilder.MushroomVoicesExist())
            DinohoppAudioBuilder.GenerateMushroomVoices();
        if (!DinohoppAudioBuilder.LetterAudioExists())
            DinohoppAudioBuilder.GenerateLetterAudio();

        var jumpClip          = AssetDatabase.LoadAssetAtPath<AudioClip>(DinohoppAudioBuilder.JumpPath);
        var landClip          = AssetDatabase.LoadAssetAtPath<AudioClip>(DinohoppAudioBuilder.LandPath);
        var bounceClip        = AssetDatabase.LoadAssetAtPath<AudioClip>(DinohoppAudioBuilder.BouncePath);
        var fallClip          = AssetDatabase.LoadAssetAtPath<AudioClip>(DinohoppAudioBuilder.FallPath);
        var letterCollectClip = AssetDatabase.LoadAssetAtPath<AudioClip>(DinohoppAudioBuilder.LetterCollectPath);
        var allLettersClip    = AssetDatabase.LoadAssetAtPath<AudioClip>(DinohoppAudioBuilder.AllLettersPath);

        // Per-mushroom voice clips. Fall back to the default bounce if a voice
        // file is missing so the scene still makes sound.
        var voices = new AudioClip[DinohoppAudioBuilder.MushroomVoicePaths.Length];
        for (int i = 0; i < voices.Length; i++)
        {
            voices[i] = AssetDatabase.LoadAssetAtPath<AudioClip>(DinohoppAudioBuilder.MushroomVoicePaths[i]);
            if (voices[i] == null) voices[i] = bounceClip;
        }

        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        var camera = BuildCamera();
        BuildSky(square, circle);

        // Level 1 — two ground segments separated by a gap, decorative mushrooms at
        // running height (dino passes underneath), one bridge mushroom in the gap.
        BuildGround("Ground_A", new Vector3(4f,  -3.5f, 0f), new Vector3(24f, 1.5f, 1f), square);
        BuildGround("Ground_B", new Vector3(29f, -3.5f, 0f), new Vector3(16f, 1.5f, 1f), square);
        // Visual-only polish (grass cap + dirt shadow + clumps/flowers/pebbles).
        // Lives on separate roots so colliders are untouched.
        BuildGroundDecor("Ground_A", new Vector3(4f,  -3.5f, 0f), new Vector3(24f, 1.5f, 1f), square, circle, seed: 401);
        BuildGroundDecor("Ground_B", new Vector3(29f, -3.5f, 0f), new Vector3(16f, 1.5f, 1f), square, circle, seed: 402);

        // Each mushroom gets its own voice clip. Pitch held at 1.0 across the board
        // so each voice plays at its natural frequency — pitch-shifting was hiding
        // the personality differences (everything got pushed up the same way).
        BuildMushroom("Mushroom_1", square, circle, new Vector3(4f,   -0.8f, 0f), voices[0], pitch: 1f, voiceName: "soft_boing");
        BuildMushroom("Mushroom_2", square, circle, new Vector3(7f,   -0.6f, 0f), voices[1], pitch: 1f, voiceName: "pip");
        BuildMushroom("Mushroom_3", square, circle, new Vector3(10f,  -0.8f, 0f), voices[2], pitch: 1f, voiceName: "plop_bubble");
        BuildMushroom("Mushroom_4", square, circle, new Vector3(17.5f,-1.5f, 0f), voices[3], pitch: 1f, voiceName: "big_boing");   // BRIDGE
        BuildMushroom("Mushroom_5", square, circle, new Vector3(26f,  -0.8f, 0f), voices[4], pitch: 1f, voiceName: "ding");
        BuildMushroom("Mushroom_6", square, circle, new Vector3(30f,  -0.6f, 0f), voices[5], pitch: 1f, voiceName: "happy_pop");

        var dino = BuildDino(square, circle, jumpClip, landClip);
        BuildGoal(square, circle, new Vector3(34f, -1.0f, 0f));

        // V-I-O-L-A collectibles — each letter gets its identity color from the
        // shared palette in LetterCollectionManager so the in-world pickup matches
        // the UI row exactly. Heights mostly catchable at running height; L sits
        // above the bridge so it's collected naturally during the bridge-jump.
        var palette = LetterCollectionManager.DefaultLetterColors;
        BuildLetter(square, circle, "V", new Vector3( 2f,   -1.5f, 0f), letterCollectClip, palette[0]);
        BuildLetter(square, circle, "I", new Vector3( 5f,   -1.2f, 0f), letterCollectClip, palette[1]);
        BuildLetter(square, circle, "O", new Vector3(10f,   -0.7f, 0f), letterCollectClip, palette[2]);
        BuildLetter(square, circle, "L", new Vector3(17.5f, -0.4f, 0f), letterCollectClip, palette[3]);
        BuildLetter(square, circle, "A", new Vector3(29f,   -1.5f, 0f), letterCollectClip, palette[4]);

        var ui = BuildUI();
        BuildGameManager(dino, camera, ui, fallClip);
        BuildLetterCollectionManager(ui, allLettersClip);

        EnsureSceneInBuildSettings();
        EditorSceneManager.SaveScene(scene, ScenePath);

        Debug.Log($"[Dinohopp] Prototype scene built at {ScenePath}");
    }

    // ---------- Builders ----------

    static GameObject BuildCamera()
    {
        var go = new GameObject("Main Camera");
        go.tag = "MainCamera";
        go.transform.position = new Vector3(0f, 1f, -10f);

        var cam = go.AddComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = 4.5f;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = SkyColor; // mjuk pastellblå "varm dag"
        cam.nearClipPlane = 0.3f;
        cam.farClipPlane = 100f;

        go.AddComponent<AudioListener>();

        var follow = go.AddComponent<CameraFollow2D>();
        follow.offset = new Vector3(2.5f, 1f, -10f);
        follow.smoothTime = 0.25f;
        follow.lockY = true;
        follow.lockedY = 1f;

        return go;
    }

    /// <summary>
    /// Soft pastel daytime sky: distant cloud layer + nearer tree-silhouette layer.
    /// Two SimpleParallax containers give cheap depth without changing gameplay.
    /// </summary>
    static void BuildSky(Sprite square, Sprite circle)
    {
        // ---- Distant cloud layer ----
        var clouds = new GameObject("Clouds");
        clouds.transform.position = Vector3.zero;
        var cloudsPx = clouds.AddComponent<SimpleParallax>();
        cloudsPx.parallaxFactor = 0.10f; // very slow drift

        var cloudColor = new Color(1f, 1f, 0.96f, 0.95f);
        var cRng = new System.Random(101);
        const int cloudCount = 7;
        for (int i = 0; i < cloudCount; i++)
        {
            float x = i * 9f - 8f + (float)cRng.NextDouble() * 4f;
            float y = 3.2f + (float)cRng.NextDouble() * 2.0f;       // upper portion of sky
            float scale = 0.7f + (float)cRng.NextDouble() * 0.6f;

            // Each cloud = three overlapping soft circles for a cartoony silhouette.
            var cloud = new GameObject($"Cloud_{i:00}");
            cloud.transform.SetParent(clouds.transform, worldPositionStays: false);
            cloud.transform.localPosition = new Vector3(x, y, 1f);
            cloud.transform.localScale = Vector3.one;

            SpawnChild(cloud.transform, "L", circle, cloudColor,
                new Vector3(-0.75f, -0.05f, 0f),
                new Vector3(1.15f * scale, 0.95f * scale, 1f), sortingOrder: -10);
            SpawnChild(cloud.transform, "M", circle, cloudColor,
                Vector3.zero,
                new Vector3(1.50f * scale, 1.25f * scale, 1f), sortingOrder: -10);
            SpawnChild(cloud.transform, "R", circle, cloudColor,
                new Vector3(0.75f, -0.10f, 0f),
                new Vector3(1.10f * scale, 0.90f * scale, 1f), sortingOrder: -10);
        }

        // ---- Mid-ground tree silhouette layer ----
        var trees = new GameObject("Trees");
        trees.transform.position = Vector3.zero;
        var treesPx = trees.AddComponent<SimpleParallax>();
        treesPx.parallaxFactor = 0.35f; // moves more than clouds → reads as closer

        var trunkColor = new Color(0.35f, 0.25f, 0.18f, 1f); // warm brown
        var crownColor = new Color(0.22f, 0.42f, 0.25f, 1f); // mossy green

        var tRng = new System.Random(42);
        const int treeCount = 9;
        for (int i = 0; i < treeCount; i++)
        {
            float x = i * 5f - 8f + (float)tRng.NextDouble() * 2.5f;
            float treeScale = 1.6f + (float)tRng.NextDouble() * 1.2f;
            float baseY = -2.0f; // trunk base sits inside the ground line

            // Trunk
            SpawnChild(trees.transform, $"Trunk_{i:00}", square, trunkColor,
                new Vector3(x, baseY + 0.35f * treeScale, 1f),
                new Vector3(0.45f * treeScale, 1.40f * treeScale, 1f),
                sortingOrder: -8);

            // Crown (single big oval — kept simple for clean silhouette)
            SpawnChild(trees.transform, $"Crown_{i:00}", circle, crownColor,
                new Vector3(x, baseY + 1.30f * treeScale, 1f),
                new Vector3(2.20f * treeScale, 2.10f * treeScale, 1f),
                sortingOrder: -7);
        }
    }

    static void BuildGround(string name, Vector3 position, Vector3 scale, Sprite sprite)
    {
        // Dirt-brown base. The grass cap + decor live on a separate decor root so
        // gameplay collider is untouched.
        var dirtMain = new Color(0.50f, 0.32f, 0.20f, 1f);
        var go = MakeSprite(name, sprite, dirtMain, position, scale, sortingOrder: 0);
        go.AddComponent<BoxCollider2D>();
    }

    /// <summary>
    /// Visual-only polish for a ground segment. Lives on its own root with scale 1
    /// (so child positions stay in world units) and has no colliders. Adds:
    ///   - bright grass cap straddling the ground top edge
    ///   - darker dirt-shadow band at the bottom for soft two-tone earth
    ///   - small grass tufts, flowers, and pebbles distributed deterministically
    /// </summary>
    static void BuildGroundDecor(string groundName, Vector3 groundPos, Vector3 groundScale,
                                 Sprite square, Sprite circle, int seed)
    {
        float halfW = groundScale.x * 0.5f;
        float halfH = groundScale.y * 0.5f;
        float xMin = groundPos.x - halfW;
        float xMax = groundPos.x + halfW;
        float topY = groundPos.y + halfH;
        float botY = groundPos.y - halfH;

        var root = new GameObject(groundName + "_Decor");
        root.transform.position = Vector3.zero;
        root.transform.localScale = Vector3.one;

        // ---- Big bands ----
        // Grass cap — bright green, slight overhang above the ground top.
        var grassColor = new Color(0.42f, 0.72f, 0.32f, 1f);
        SpawnChild(root.transform, "GrassCap", square, grassColor,
            new Vector3(groundPos.x, topY - 0.08f, 0f),
            new Vector3(groundScale.x, 0.40f, 1f),
            sortingOrder: 1);

        // Dirt shadow band at the bottom for soft depth.
        var dirtDark = new Color(0.36f, 0.22f, 0.13f, 1f);
        SpawnChild(root.transform, "DirtShadow", square, dirtDark,
            new Vector3(groundPos.x, botY + 0.15f, 0f),
            new Vector3(groundScale.x, 0.30f, 1f),
            sortingOrder: 1);

        // ---- Small details (deterministic via seeded RNG) ----
        var rng = new System.Random(seed);

        // Bright clumps along the grass line.
        var clumpColor = new Color(0.55f, 0.80f, 0.35f, 1f);
        int clumpCount = Mathf.Max(3, Mathf.RoundToInt(groundScale.x / 2.2f));
        for (int i = 0; i < clumpCount; i++)
        {
            float t = (i + 0.3f + (float)rng.NextDouble() * 0.4f) / clumpCount;
            float x = Mathf.Lerp(xMin + 0.4f, xMax - 0.4f, t);
            float y = topY + 0.05f + (float)rng.NextDouble() * 0.10f;
            float size = 0.20f + (float)rng.NextDouble() * 0.15f;
            SpawnChild(root.transform, $"Clump_{i:00}", circle, clumpColor,
                new Vector3(x, y, 0f),
                new Vector3(size * 1.5f, size * 0.85f, 1f),
                sortingOrder: 2);
        }

        // Sparse little flowers.
        var petalChoices = new[]
        {
            new Color(1.00f, 0.98f, 0.92f, 1f), // soft white
            new Color(1.00f, 0.78f, 0.85f, 1f), // pink
            new Color(0.92f, 0.88f, 1.00f, 1f), // lilac
        };
        var centerColor = new Color(1.00f, 0.85f, 0.30f, 1f);
        var stemColor   = new Color(0.30f, 0.55f, 0.25f, 1f);

        int flowerCount = Mathf.Max(2, Mathf.RoundToInt(groundScale.x / 6f));
        for (int i = 0; i < flowerCount; i++)
        {
            float t = (i + 0.5f + (float)rng.NextDouble() * 0.3f) / flowerCount;
            float x = Mathf.Lerp(xMin + 1.0f, xMax - 1.0f, t);
            float y = topY + 0.16f;
            var petal = petalChoices[rng.Next(petalChoices.Length)];
            BuildFlower(root.transform, square, circle, $"Flower_{i:00}",
                new Vector3(x, y, 0f), petal, centerColor, stemColor);
        }

        // Half-buried pebbles.
        var pebbleColor = new Color(0.60f, 0.55f, 0.50f, 1f);
        int pebbleCount = Mathf.Max(2, Mathf.RoundToInt(groundScale.x / 3.5f));
        for (int i = 0; i < pebbleCount; i++)
        {
            float x = Mathf.Lerp(xMin + 0.5f, xMax - 0.5f, (float)rng.NextDouble());
            float y = topY - 0.04f;
            float size = 0.16f + (float)rng.NextDouble() * 0.10f;
            SpawnChild(root.transform, $"Pebble_{i:00}", circle, pebbleColor,
                new Vector3(x, y, 0f),
                new Vector3(size, size * 0.65f, 1f),
                sortingOrder: 2);
        }
    }

    static void BuildFlower(Transform parent, Sprite square, Sprite circle, string name,
                            Vector3 pos, Color petalColor, Color centerColor, Color stemColor)
    {
        var flower = new GameObject(name);
        flower.transform.SetParent(parent, worldPositionStays: false);
        flower.transform.localPosition = pos;
        flower.transform.localScale = Vector3.one;

        // Tiny stem
        SpawnChild(flower.transform, "Stem", square, stemColor,
            new Vector3(0f, -0.10f, 0f),
            new Vector3(0.04f, 0.20f, 1f), sortingOrder: 2);

        // 4 petals around the center
        const float r = 0.09f;
        const float petalSize = 0.12f;
        SpawnChild(flower.transform, "PT", circle, petalColor,
            new Vector3(0f,  r, 0f),
            new Vector3(petalSize, petalSize, 1f), sortingOrder: 3);
        SpawnChild(flower.transform, "PB", circle, petalColor,
            new Vector3(0f, -r * 0.2f, 0f),
            new Vector3(petalSize, petalSize, 1f), sortingOrder: 3);
        SpawnChild(flower.transform, "PL", circle, petalColor,
            new Vector3(-r, r * 0.4f, 0f),
            new Vector3(petalSize, petalSize, 1f), sortingOrder: 3);
        SpawnChild(flower.transform, "PR", circle, petalColor,
            new Vector3( r, r * 0.4f, 0f),
            new Vector3(petalSize, petalSize, 1f), sortingOrder: 3);

        // Bright yellow center
        SpawnChild(flower.transform, "Center", circle, centerColor,
            new Vector3(0f, r * 0.4f, 0f),
            new Vector3(0.08f, 0.08f, 1f), sortingOrder: 4);
    }

    static void BuildMushroom(string name, Sprite square, Sprite circle,
                              Vector3 position, AudioClip bounceClip,
                              float pitch = 1f, string voiceName = "")
    {
        // Append voice name to hierarchy label so the Inspector reads "Mushroom_1 (soft_boing)".
        string fullName = string.IsNullOrEmpty(voiceName) ? name : $"{name} ({voiceName})";

        // Root holds physics + behaviour. Unit scale so children render at design size.
        var root = new GameObject(fullName);
        root.transform.position = position;
        root.transform.localScale = Vector3.one;

        // One-way platform collider: slightly wider than the visual cap (2.0 vs 1.8)
        // for forgiving landings, slightly thinner (0.5 vs 0.6) so the side-overlap
        // window is small. Offset 0.05 keeps the collider TOP at local y=+0.30, exactly
        // where it was before — landing heights and level layout unchanged.
        var col = root.AddComponent<BoxCollider2D>();
        col.size = new Vector2(2.0f, 0.5f);
        col.offset = new Vector2(0f, 0.05f);
        col.usedByEffector = true;

        // PlatformEffector2D makes the cap act as a one-way platform: dino lands from
        // above as before, but runs cleanly THROUGH the sides (fixes the stuck-on-side
        // bug where auto-run kept pressing into the wall and jumps couldn't recover).
        var effector = root.AddComponent<PlatformEffector2D>();
        effector.useOneWay = true;
        effector.useColliderMask = false;
        effector.surfaceArc = 170f;       // small inset from 180° = safer top-only detection
        effector.useSideFriction = false;
        effector.useSideBounce = false;

        // Per-mushroom AudioSource so each cap can pitch-shift its bounce independently.
        var src = root.AddComponent<AudioSource>();
        src.playOnAwake = false;
        src.spatialBlend = 0f; // pure 2D
        src.volume = 1f;

        var fb = root.AddComponent<MushroomBounceFeedback>();
        fb.dinoTag = "Player";
        fb.message = "Bra hopp!";
        fb.squishAmount = 1.15f;
        fb.squishDuration = 0.2f;
        fb.retriggerCooldown = 0.5f;
        fb.bounceClip = bounceClip;
        fb.bounceVolume = 0.75f; // bumped from 0.55 — voices are gentle, kids need to hear them clearly
        fb.pitch = pitch;

        var visual = new GameObject("Visual");
        visual.transform.SetParent(root.transform, worldPositionStays: false);

        var capColor  = new Color(0.92f, 0.27f, 0.32f); // bright red cap
        var stemColor = new Color(0.96f, 0.92f, 0.78f); // soft cream stem
        var spotColor = Color.white;

        // Stem (rectangle) — sits below the cap, partly behind it.
        SpawnChild(visual.transform, "Stem", square, stemColor,
                   localPos:   new Vector3(0f, -0.30f, 0f),
                   localScale: new Vector3(0.6f, 0.45f, 1f),
                   sortingOrder: 1);

        // Cap (flat oval) — top aligns with collider top y=+0.30.
        SpawnChild(visual.transform, "Cap", circle, capColor,
                   localPos:   new Vector3(0f, 0.05f, 0f),
                   localScale: new Vector3(1.8f, 0.50f, 1f),
                   sortingOrder: 2);

        // Three white spots scattered on the cap for that classic mushroom look.
        SpawnChild(visual.transform, "Spot_1", circle, spotColor,
                   localPos:   new Vector3(-0.45f, 0.10f, 0f),
                   localScale: new Vector3(0.24f, 0.18f, 1f),
                   sortingOrder: 3);
        SpawnChild(visual.transform, "Spot_2", circle, spotColor,
                   localPos:   new Vector3(0.20f, 0.18f, 0f),
                   localScale: new Vector3(0.20f, 0.15f, 1f),
                   sortingOrder: 3);
        SpawnChild(visual.transform, "Spot_3", circle, spotColor,
                   localPos:   new Vector3(0.55f, 0.05f, 0f),
                   localScale: new Vector3(0.18f, 0.13f, 1f),
                   sortingOrder: 3);

        // Ambient "breathing" — purely visual, lives on the Visual child so the
        // root's BoxCollider2D stays exactly where it is.
        var bob = visual.AddComponent<IdleBob>();
        bob.amplitude = 0.04f;
        bob.frequency = 0.5f;
        bob.phaseOffset = position.x * 0.3f; // unique per mushroom from world X
    }

    static GameObject BuildDino(Sprite square, Sprite circle, AudioClip jumpClip, AudioClip landClip)
    {
        // Root holds physics + behaviour. Unit scale means children render at design size.
        var root = new GameObject("Dino");
        root.transform.position = new Vector3(0f, 0f, 0f);
        root.transform.localScale = Vector3.one;
        root.tag = "Player";

        var rb = root.AddComponent<Rigidbody2D>();
        rb.gravityScale = 1.5f;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;

        // One generous collider on the root — covers the whole dino silhouette and a
        // bit more, so kids don't fall through mushroom edges. Same world size as before
        // (1.08 x 1.33) so jump arcs and landing heights are preserved exactly.
        var col = root.AddComponent<BoxCollider2D>();
        col.size = new Vector2(1.08f, 1.33f);
        col.offset = new Vector2(0f, -0.05f);

        var ctrl = root.AddComponent<DinoController>();
        ctrl.runSpeed = 1.8f;
        ctrl.jumpForce = 9f;
        ctrl.groundCheckRadius = 0.12f;
        ctrl.groundLayers = ~0; // Everything (filtered against own colliders in script).
        ctrl.coyoteTime = 0.18f;
        ctrl.jumpBufferTime = 0.18f;

        var audio = root.AddComponent<AudioSource>();
        audio.playOnAwake = false;
        audio.spatialBlend = 0f; // pure 2D
        audio.volume = 1f;

        var fb = root.AddComponent<DinoFeedback>();
        fb.jumpClip = jumpClip;
        fb.landClip = landClip;
        fb.volume = 0.6f;
        fb.squashDuration = 0.15f;
        fb.squashScale = new Vector2(1.12f, 0.88f);
        fb.stretchDuration = 0.12f;
        fb.stretchScale = new Vector2(0.90f, 1.12f);
        fb.joyDuration = 0.20f;
        fb.joyScale = new Vector2(1.18f, 1.18f);

        // Locomotion: idle breathing while paused, footstep bob while running.
        // Added after BuildDino creates the Visual child (added below) so the script
        // can find it via transform.Find in Awake.
        var loco = root.AddComponent<DinoLocomotion>();
        loco.breathFrequency = 0.45f;
        loco.breathAmount = 0.04f;
        loco.runBobFrequency = 4.0f;
        loco.runBobAmount = 0.04f;

        // ---- Visual hierarchy ----
        // Two nested transforms so DinoLocomotion (bob + breath on Visual) and
        // DinoSurfaceVisualOffset (mushroom-cap parabola on SurfaceCurve) compose
        // without fighting over the same property:
        //   root.scale    ← squash/stretch/joy (DinoFeedback)
        //   Visual        ← idle breathing + running footstep bob (DinoLocomotion)
        //   SurfaceCurve  ← parabolic lift on mushrooms (DinoSurfaceVisualOffset)
        //   body parts    ← children of SurfaceCurve
        var visual = new GameObject("Visual");
        visual.transform.SetParent(root.transform, worldPositionStays: false);
        // Drop the whole rig 0.25 down so the feet sprites' bottom edge matches
        // the collider bottom (-0.715). Without this offset the feet hover 0.25
        // units above every surface and dino looks like it's walking on air.
        visual.transform.localPosition = new Vector3(0f, -0.25f, 0f);

        var surfaceCurve = new GameObject("SurfaceCurve");
        surfaceCurve.transform.SetParent(visual.transform, worldPositionStays: false);

        var bodyColor = new Color(0.95f, 0.55f, 0.15f); // bright orange
        var darkBody  = new Color(0.75f, 0.40f, 0.10f); // shadowed orange for feet
        var eyeWhite  = Color.white;
        var eyePupil  = new Color(0.10f, 0.10f, 0.20f); // near-black, slight blue tint

        // Tail — slim rect, pointed up-back. Sorted behind body so it tucks in.
        SpawnChild(surfaceCurve.transform, "Tail", square, bodyColor,
                   localPos:   new Vector3(-0.35f, 0.00f, 0f),
                   localScale: new Vector3(0.32f, 0.12f, 1f),
                   sortingOrder: 1,
                   rotationZ: 20f);

        // Body — wide round blob.
        SpawnChild(surfaceCurve.transform, "Body", circle, bodyColor,
                   localPos:   new Vector3(0.00f, -0.05f, 0f),
                   localScale: new Vector3(0.78f, 0.70f, 1f),
                   sortingOrder: 2);

        // Two stubby feet, slightly darker for a hint of shading.
        SpawnChild(surfaceCurve.transform, "Foot_Left", square, darkBody,
                   localPos:   new Vector3(-0.07f, -0.40f, 0f),
                   localScale: new Vector3(0.20f, 0.13f, 1f),
                   sortingOrder: 3);
        SpawnChild(surfaceCurve.transform, "Foot_Right", square, darkBody,
                   localPos:   new Vector3(0.20f, -0.40f, 0f),
                   localScale: new Vector3(0.20f, 0.13f, 1f),
                   sortingOrder: 3);

        // Head — large round head front-and-up of the body.
        SpawnChild(surfaceCurve.transform, "Head", circle, bodyColor,
                   localPos:   new Vector3(0.28f, 0.22f, 0f),
                   localScale: new Vector3(0.48f, 0.48f, 1f),
                   sortingOrder: 4);

        // Big friendly eye — white sclera + dark pupil offset slightly forward.
        SpawnChild(surfaceCurve.transform, "Eye_White", circle, eyeWhite,
                   localPos:   new Vector3(0.36f, 0.28f, 0f),
                   localScale: new Vector3(0.18f, 0.18f, 1f),
                   sortingOrder: 5);
        SpawnChild(surfaceCurve.transform, "Eye_Pupil", circle, eyePupil,
                   localPos:   new Vector3(0.40f, 0.27f, 0f),
                   localScale: new Vector3(0.09f, 0.09f, 1f),
                   sortingOrder: 6);

        // Subtle blink — added after the eye children exist so DinoBlink can find them.
        var blink = root.AddComponent<DinoBlink>();
        blink.minInterval = 3f;
        blink.maxInterval = 6f;
        blink.blinkDuration = 0.10f;
        blink.closedScaleY = 0.10f;

        // Visual sink along the mushroom cap's oval edge — pure cosmetic.
        // Collider top equals the cap's PEAK so centre offset is 0; feet sink
        // toward the cap edges where the oval drops away.
        var surfaceOffset = root.AddComponent<DinoSurfaceVisualOffset>();
        surfaceOffset.surfaceCurve = surfaceCurve.transform;
        surfaceOffset.curveHeight = 0.20f;
        surfaceOffset.smoothSpeed = 10f;

        return root;
    }

    static void BuildGoal(Sprite square, Sprite circle, Vector3 position)
    {
        var root = new GameObject("Goal");
        root.transform.position = position;
        root.transform.localScale = Vector3.one;

        var col = root.AddComponent<BoxCollider2D>();
        // Trigger centred on the flag visual (offset.x = 0.45 = flag local x).
        // Width 1.0 covers flag + a tiny lead-in so success fires when dino's
        // right edge actually touches the pole, not 1.5 units earlier.
        col.size = new Vector2(1.0f, 5.0f);
        col.offset = new Vector2(0.45f, 0.5f);
        col.isTrigger = true;

        root.AddComponent<Goal>();

        var visual = new GameObject("Visual");
        visual.transform.SetParent(root.transform, worldPositionStays: false);

        var poleColor = new Color(0.55f, 0.40f, 0.25f); // tan/brown
        var flagColor = new Color(1.00f, 0.85f, 0.20f); // sunny yellow
        var knobColor = new Color(1.00f, 0.92f, 0.45f); // softer yellow

        // Tall pole stretching up from ground.
        SpawnChild(visual.transform, "Pole", square, poleColor,
                   localPos:   new Vector3(0f, 0.0f, 0f),
                   localScale: new Vector3(0.15f, 3.0f, 1f),
                   sortingOrder: 5);

        // Flag attached to side of pole.
        SpawnChild(visual.transform, "Flag", square, flagColor,
                   localPos:   new Vector3(0.45f, 1.0f, 0f),
                   localScale: new Vector3(0.70f, 0.45f, 1f),
                   sortingOrder: 6);

        // Round knob crowning the pole.
        SpawnChild(visual.transform, "Knob", circle, knobColor,
                   localPos:   new Vector3(0f, 1.5f, 0f),
                   localScale: new Vector3(0.22f, 0.22f, 1f),
                   sortingOrder: 7);
    }

    static void BuildGameManager(GameObject dino, GameObject camera, UIRefs ui, AudioClip fallClip)
    {
        var go = new GameObject("GameManager");

        // 2D audio source for state-machine SFX (fall "oops" today, more later).
        var src = go.AddComponent<AudioSource>();
        src.playOnAwake = false;
        src.spatialBlend = 0f;

        var gm = go.AddComponent<GameManager>();
        gm.dino = dino.GetComponent<DinoController>();
        gm.cameraFollow = camera.GetComponent<CameraFollow2D>();
        gm.startScreenPanel = ui.startScreenPanel;
        gm.retryPanel       = ui.retryPanel;
        gm.successPanel     = ui.successPanel;
        gm.fallThresholdY   = -7f;
        gm.fallClip         = fallClip;
        gm.fallVolume       = 0.65f;
    }

    static void BuildLetterCollectionManager(UIRefs ui, AudioClip allLettersClip)
    {
        var go = new GameObject("LetterCollectionManager");
        var src = go.AddComponent<AudioSource>();
        src.playOnAwake = false;
        src.spatialBlend = 0f;

        var lcm = go.AddComponent<LetterCollectionManager>();
        lcm.targetWord = "VIOLA";
        lcm.wordText = ui.wordText;
        lcm.allCollectedPanel = ui.allLettersPanel;
        lcm.allCollectedClip = allLettersClip;
        lcm.allCollectedVolume = 0.70f;
        lcm.allCollectedPanelDuration = 2.5f;
    }

    // ---------- Letter builders ----------

    static void BuildLetter(Sprite square, Sprite circle, string letter,
                            Vector3 position, AudioClip collectClip, Color color)
    {
        var root = new GameObject($"Letter_{letter}");
        root.transform.position = position;
        root.transform.localScale = Vector3.one;

        // Generous trigger so kids don't need to be precise.
        var col = root.AddComponent<BoxCollider2D>();
        col.size = new Vector2(1.1f, 1.3f);
        col.offset = Vector2.zero;
        col.isTrigger = true;

        var src = root.AddComponent<AudioSource>();
        src.playOnAwake = false;
        src.spatialBlend = 0f;

        var lc = root.AddComponent<LetterCollectible>();
        lc.letter = letter;
        lc.collectClip = collectClip;
        lc.volume = 0.75f;

        var visual = new GameObject("Visual");
        visual.transform.SetParent(root.transform, worldPositionStays: false);

        // Gentle bob so letters feel alive and "collect me".
        var bob = visual.AddComponent<IdleBob>();
        bob.amplitude = 0.10f;
        bob.frequency = 0.7f;
        bob.phaseOffset = position.x * 0.4f;

        // Soft dark backplate — gives every letter a halo/outline against the
        // bright pastel sky, regardless of the fill color underneath. Sorted
        // below the letter shape so it only peeks out around the edges.
        var backplate = new Color(0.10f, 0.15f, 0.25f, 0.55f);
        SpawnChild(visual.transform, "Backplate", circle, backplate,
            Vector3.zero, new Vector3(1.15f, 1.15f, 1f), sortingOrder: 28);

        switch (letter)
        {
            case "V": BuildLetterV(visual.transform, square, color); break;
            case "I": BuildLetterI(visual.transform, square, color); break;
            case "O": BuildLetterO(visual.transform, circle, color); break;
            case "L": BuildLetterL(visual.transform, square, color); break;
            case "A": BuildLetterA(visual.transform, square, color); break;
        }
    }

    static void BuildLetterV(Transform parent, Sprite square, Color color)
    {
        SpawnChild(parent, "Left",  square, color,
            new Vector3(-0.20f, 0f, 0f), new Vector3(0.15f, 0.95f, 1f),
            sortingOrder: 30, rotationZ: 18f);
        SpawnChild(parent, "Right", square, color,
            new Vector3( 0.20f, 0f, 0f), new Vector3(0.15f, 0.95f, 1f),
            sortingOrder: 30, rotationZ: -18f);
    }

    static void BuildLetterI(Transform parent, Sprite square, Color color)
    {
        SpawnChild(parent, "Bar",    square, color,
            Vector3.zero, new Vector3(0.22f, 0.95f, 1f), sortingOrder: 30);
        SpawnChild(parent, "Top",    square, color,
            new Vector3(0f,  0.45f, 0f), new Vector3(0.50f, 0.14f, 1f), sortingOrder: 30);
        SpawnChild(parent, "Bottom", square, color,
            new Vector3(0f, -0.45f, 0f), new Vector3(0.50f, 0.14f, 1f), sortingOrder: 30);
    }

    static void BuildLetterO(Transform parent, Sprite circle, Color color)
    {
        // Bright outer disc + hollow inner matching the sky background gives a ring "O".
        SpawnChild(parent, "Outer", circle, color,
            Vector3.zero, new Vector3(0.95f, 0.95f, 1f), sortingOrder: 30);
        SpawnChild(parent, "Inner", circle, SkyColor,
            Vector3.zero, new Vector3(0.50f, 0.50f, 1f), sortingOrder: 31);
    }

    static void BuildLetterL(Transform parent, Sprite square, Color color)
    {
        SpawnChild(parent, "Vert",  square, color,
            new Vector3(-0.20f, 0f, 0f), new Vector3(0.20f, 0.95f, 1f), sortingOrder: 30);
        SpawnChild(parent, "Base",  square, color,
            new Vector3( 0.05f, -0.38f, 0f), new Vector3(0.55f, 0.20f, 1f), sortingOrder: 30);
    }

    static void BuildLetterA(Transform parent, Sprite square, Color color)
    {
        SpawnChild(parent, "Left",  square, color,
            new Vector3(-0.20f, 0f, 0f), new Vector3(0.15f, 0.95f, 1f),
            sortingOrder: 30, rotationZ: 18f);
        SpawnChild(parent, "Right", square, color,
            new Vector3( 0.20f, 0f, 0f), new Vector3(0.15f, 0.95f, 1f),
            sortingOrder: 30, rotationZ: -18f);
        SpawnChild(parent, "Cross", square, color,
            new Vector3(0f, -0.10f, 0f), new Vector3(0.40f, 0.14f, 1f), sortingOrder: 30);
    }

    struct UIRefs
    {
        public GameObject startScreenPanel;
        public GameObject retryPanel;
        public GameObject successPanel;
        public Text       wordText;
        public GameObject allLettersPanel;
    }

    static UIRefs BuildUI()
    {
        var canvasGO = new GameObject("Canvas",
            typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));

        var canvas = canvasGO.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;

        var scaler = canvasGO.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        // Toast text for landings ("Bra hopp!") — anchored top-center.
        var textGO = new GameObject("FeedbackText",
            typeof(RectTransform), typeof(Text), typeof(FeedbackText));
        textGO.transform.SetParent(canvasGO.transform, worldPositionStays: false);

        var rt = (RectTransform)textGO.transform;
        rt.anchorMin = new Vector2(0.5f, 1f);
        rt.anchorMax = new Vector2(0.5f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.anchoredPosition = new Vector2(0f, -120f);
        rt.sizeDelta = new Vector2(1200f, 240f);

        var text = textGO.GetComponent<Text>();
        text.alignment = TextAnchor.MiddleCenter;
        text.fontSize = 110;
        text.fontStyle = FontStyle.Bold;
        text.color = new Color(1f, 0.95f, 0.55f, 1f);
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.horizontalOverflow = HorizontalWrapMode.Overflow;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        text.text = string.Empty;

        var fb = textGO.GetComponent<FeedbackText>();
        fb.visibleDuration = 0.7f;
        fb.fadeDuration = 0.4f;

        // State panels — full-screen centered big text, toggled by GameManager.
        // Neutral input wording so the same UI works on desktop, mouse, and mobile touch.
        var startPanel = BuildBigText(canvasGO.transform, "StartScreenPanel",
            "Tryck SPACE\neller på skärmen\nför att starta",
            fontSize: 110,
            color: new Color(1f, 0.95f, 0.55f, 1f));

        var retryPanel = BuildBigText(canvasGO.transform, "RetryPanel",
            "Oj då!\nFörsök igen\n\nTryck SPACE\neller på skärmen",
            fontSize: 120,
            color: new Color(1f, 0.85f, 0.50f, 1f));

        var successPanel = BuildBigText(canvasGO.transform, "SuccessPanel",
            "Bra jobbat!\n\nTryck SPACE\neller på skärmen",
            fontSize: 120,
            color: new Color(0.75f, 1f, 0.75f, 1f));

        // Initial visibility — GameManager will fix on Start, but set sane defaults
        // for the gap between scene load and first GameManager update.
        startPanel.SetActive(true);
        retryPanel.SetActive(false);
        successPanel.SetActive(false);

        // VIOLA progress bar — persistent, top of screen, rich-text-colored per letter.
        var wordGO = new GameObject("VIOLAProgress",
            typeof(RectTransform), typeof(Text));
        wordGO.transform.SetParent(canvasGO.transform, worldPositionStays: false);
        var wordRT = (RectTransform)wordGO.transform;
        wordRT.anchorMin = new Vector2(0.5f, 1f);
        wordRT.anchorMax = new Vector2(0.5f, 1f);
        wordRT.pivot     = new Vector2(0.5f, 1f);
        wordRT.anchoredPosition = new Vector2(0f, -40f);
        wordRT.sizeDelta = new Vector2(1400f, 200f);
        var wordTxt = wordGO.GetComponent<Text>();
        wordTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        wordTxt.alignment = TextAnchor.UpperCenter;
        wordTxt.fontSize = 130;
        wordTxt.fontStyle = FontStyle.Bold;
        wordTxt.color = Color.white;
        wordTxt.horizontalOverflow = HorizontalWrapMode.Overflow;
        wordTxt.verticalOverflow = VerticalWrapMode.Overflow;
        wordTxt.supportRichText = true;
        wordTxt.raycastTarget = false;
        wordTxt.text = "V  I  O  L  A"; // overridden by manager

        // "Du hittade VIOLA!" panel — flashes briefly when full word collected.
        var allLetters = BuildBigText(canvasGO.transform, "AllLettersPanel",
            "Du hittade VIOLA!",
            fontSize: 110,
            color: new Color(1f, 0.95f, 0.55f, 1f));
        allLetters.SetActive(false);

        return new UIRefs
        {
            startScreenPanel = startPanel,
            retryPanel       = retryPanel,
            successPanel     = successPanel,
            wordText         = wordTxt,
            allLettersPanel  = allLetters,
        };
    }

    static GameObject BuildBigText(Transform parent, string name, string content, int fontSize, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Text));
        go.transform.SetParent(parent, worldPositionStays: false);

        var rt = (RectTransform)go.transform;
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = new Vector2(1600f, 700f);

        var t = go.GetComponent<Text>();
        t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        t.text = content;
        t.alignment = TextAnchor.MiddleCenter;
        t.fontSize = fontSize;
        t.fontStyle = FontStyle.Bold;
        t.color = color;
        t.horizontalOverflow = HorizontalWrapMode.Overflow;
        t.verticalOverflow = VerticalWrapMode.Overflow;
        // Don't intercept clicks — clicks pass through to GameManager.
        t.raycastTarget = false;

        return go;
    }

    // ---------- Helpers ----------

    static GameObject MakeSprite(string name, Sprite sprite, Color color,
                                 Vector3 position, Vector3 scale, int sortingOrder)
    {
        var go = new GameObject(name);
        go.transform.position = position;
        go.transform.localScale = scale;

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.color = color;
        sr.sortingOrder = sortingOrder;

        return go;
    }

    static GameObject SpawnChild(Transform parent, string name, Sprite sprite, Color color,
                                 Vector3 localPos, Vector3 localScale,
                                 int sortingOrder, float rotationZ = 0f)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, worldPositionStays: false);
        go.transform.localPosition = localPos;
        go.transform.localScale = localScale;
        if (rotationZ != 0f)
            go.transform.localRotation = Quaternion.Euler(0f, 0f, rotationZ);

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.color = color;
        sr.sortingOrder = sortingOrder;

        return go;
    }

    static void EnsureFolders()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Sprites"))
            AssetDatabase.CreateFolder("Assets", "Sprites");
        if (!AssetDatabase.IsValidFolder("Assets/Scenes"))
            AssetDatabase.CreateFolder("Assets", "Scenes");
    }

    static Sprite EnsureSquareSprite()
    {
        var existing = AssetDatabase.LoadAssetAtPath<Sprite>(SquareSpritePath);
        if (existing != null) return existing;

        // Generate a 16x16 plain white PNG and import as Sprite.
        var tex = new Texture2D(16, 16, TextureFormat.RGBA32, mipChain: false);
        var pixels = new Color32[16 * 16];
        for (int i = 0; i < pixels.Length; i++) pixels[i] = new Color32(255, 255, 255, 255);
        tex.SetPixels32(pixels);
        tex.Apply();

        var bytes = tex.EncodeToPNG();
        Object.DestroyImmediate(tex);
        File.WriteAllBytes(SquareSpritePath, bytes);
        AssetDatabase.ImportAsset(SquareSpritePath, ImportAssetOptions.ForceUpdate);

        var importer = AssetImporter.GetAtPath(SquareSpritePath) as TextureImporter;
        if (importer != null)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spritePixelsPerUnit = 16f; // 16 px tall -> 1 world unit
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.filterMode = FilterMode.Point;
            importer.mipmapEnabled = false;
            importer.SaveAndReimport();
        }

        return AssetDatabase.LoadAssetAtPath<Sprite>(SquareSpritePath);
    }

    static Sprite EnsureCircleSprite()
    {
        var existing = AssetDatabase.LoadAssetAtPath<Sprite>(CircleSpritePath);
        if (existing != null) return existing;

        // 64x64 solid white circle on transparent. Bilinear filter gives a soft edge.
        const int size = 64;
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, mipChain: false);
        var pixels = new Color32[size * size];
        var transparent = new Color32(0, 0, 0, 0);
        var opaque      = new Color32(255, 255, 255, 255);

        float center = (size - 1) * 0.5f;
        float r = center;
        float r2 = r * r;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = x - center;
                float dy = y - center;
                pixels[y * size + x] = (dx * dx + dy * dy <= r2) ? opaque : transparent;
            }
        }
        tex.SetPixels32(pixels);
        tex.Apply();

        var bytes = tex.EncodeToPNG();
        Object.DestroyImmediate(tex);
        File.WriteAllBytes(CircleSpritePath, bytes);
        AssetDatabase.ImportAsset(CircleSpritePath, ImportAssetOptions.ForceUpdate);

        var importer = AssetImporter.GetAtPath(CircleSpritePath) as TextureImporter;
        if (importer != null)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spritePixelsPerUnit = 64f; // 64 px -> 1 world unit
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.filterMode = FilterMode.Bilinear; // soft edge
            importer.mipmapEnabled = false;
            importer.SaveAndReimport();
        }

        return AssetDatabase.LoadAssetAtPath<Sprite>(CircleSpritePath);
    }

    static void EnsureSceneInBuildSettings()
    {
        var scenes = EditorBuildSettings.scenes;
        foreach (var s in scenes)
        {
            if (s.path == ScenePath) return;
        }
        var list = new System.Collections.Generic.List<EditorBuildSettingsScene>(scenes)
        {
            new EditorBuildSettingsScene(ScenePath, true)
        };
        EditorBuildSettings.scenes = list.ToArray();
    }
}
