using System.IO;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Synthesizes short, soft placeholder SFX (jump/land/mushroom-bounce) directly to
/// Assets/Audio/ as 16-bit mono WAVs. Pure code = CC0, no external dependencies.
/// Designed to be gentle for young kids — low volume, no harsh transients.
/// </summary>
public static class DinohoppAudioBuilder
{
    const int SampleRate = 44100;
    const string AudioFolder = "Assets/Audio";

    public const string JumpPath   = "Assets/Audio/jump.wav";
    public const string LandPath   = "Assets/Audio/land.wav";
    public const string BouncePath = "Assets/Audio/mushroom-bounce.wav";

    const string GeneratedFolder = "Assets/Audio/Generated";

    /// <summary>
    /// Per-mushroom "voice" clips. Each has a distinct timbre/envelope so the six
    /// mushrooms feel like different little creatures, not just pitch-shifts of
    /// the same sound. Anchored around ~300 Hz so the existing pentatonic pitch
    /// multipliers still create an ascending scale when hopped in order.
    /// </summary>
    public static readonly string[] MushroomVoicePaths = new[]
    {
        "Assets/Audio/Generated/mushroom_1_soft_boing.wav",
        "Assets/Audio/Generated/mushroom_2_pip.wav",
        "Assets/Audio/Generated/mushroom_3_plop_bubble.wav",
        "Assets/Audio/Generated/mushroom_4_big_boing.wav",
        "Assets/Audio/Generated/mushroom_5_ding.wav",
        "Assets/Audio/Generated/mushroom_6_happy_pop.wav",
    };

    public const string LetterCollectPath = "Assets/Audio/Generated/letter_collect.wav";
    public const string AllLettersPath    = "Assets/Audio/Generated/all_letters_collected.wav";

    [MenuItem("Tools/Dinohopp/Generate Placeholder Audio")]
    public static void Generate()
    {
        EnsureFolder();

        WriteWav(JumpPath,   JumpSamples());
        WriteWav(LandPath,   LandSamples());
        WriteWav(BouncePath, BounceSamples());

        // Import the new files synchronously so they're loadable immediately.
        const ImportAssetOptions opts = ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport;
        AssetDatabase.ImportAsset(JumpPath,   opts);
        AssetDatabase.ImportAsset(LandPath,   opts);
        AssetDatabase.ImportAsset(BouncePath, opts);

        Debug.Log("[Dinohopp] Placeholder audio generated in " + AudioFolder);
    }

    /// <summary>Returns true if all three placeholder clips exist on disk.</summary>
    public static bool AudioExists()
    {
        return File.Exists(JumpPath) && File.Exists(LandPath) && File.Exists(BouncePath);
    }

    [MenuItem("Tools/Dinohopp/Generate Mushroom Voices")]
    public static void GenerateMushroomVoices()
    {
        EnsureFolder();
        EnsureGeneratedFolder();

        // Each voice uses a DIFFERENT synthesis method so they're audibly distinct
        // even without pitch-shifting: different waveforms, envelopes, harmonics,
        // pitch behavior, and very different durations.

        // M1 — cartoony BOING: dramatic downward pitch bend, sine, ~0.45s.
        WriteWav(MushroomVoicePaths[0], SoftBoingSamples());

        // M2 — two quick high triangle bleeps "pip-pip", ~0.13s.
        WriteWav(MushroomVoicePaths[1], DoublePipSamples());

        // M3 — punchy plop with sharp attack + exponential downward sweep, ~0.22s.
        WriteWav(MushroomVoicePaths[2], PlopBubbleSamples());

        // M4 — HEAVY low boing, triangle-rich, strong vibrato, ~0.6s.
        WriteWav(MushroomVoicePaths[3], BigBoingSamples());

        // M5 — bell ding, inharmonic additive partials, longest at ~1.0s.
        WriteWav(MushroomVoicePaths[4], DingSamples());

        // M6 — bright cartoony pop, square wave, upward sweep, ~0.12s.
        WriteWav(MushroomVoicePaths[5], HappyPopSamples());

        const ImportAssetOptions opts =
            ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport;
        foreach (var p in MushroomVoicePaths)
            AssetDatabase.ImportAsset(p, opts);

        Debug.Log("[Dinohopp] Mushroom voices generated in " + GeneratedFolder);
    }

    public static bool MushroomVoicesExist()
    {
        foreach (var p in MushroomVoicePaths)
            if (!File.Exists(p)) return false;
        return true;
    }

    [MenuItem("Tools/Dinohopp/Generate Letter Audio")]
    public static void GenerateLetterAudio()
    {
        EnsureFolder();
        EnsureGeneratedFolder();

        WriteWav(LetterCollectPath, LetterCollectSamples());
        WriteWav(AllLettersPath,    AllLettersSamples());

        const ImportAssetOptions opts =
            ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport;
        AssetDatabase.ImportAsset(LetterCollectPath, opts);
        AssetDatabase.ImportAsset(AllLettersPath,    opts);

        Debug.Log("[Dinohopp] Letter audio generated in " + GeneratedFolder);
    }

    public static bool LetterAudioExists()
    {
        return File.Exists(LetterCollectPath) && File.Exists(AllLettersPath);
    }

    // ---------- Preview menu ----------

    [MenuItem("Tools/Dinohopp/Preview Mushroom Voices")]
    public static void PreviewMushroomVoices()
    {
        if (!MushroomVoicesExist())
        {
            Debug.LogWarning("[Dinohopp] Voices missing — run 'Generate Mushroom Voices' first.");
            return;
        }

        var clips = new AudioClip[MushroomVoicePaths.Length];
        for (int i = 0; i < MushroomVoicePaths.Length; i++)
            clips[i] = AssetDatabase.LoadAssetAtPath<AudioClip>(MushroomVoicePaths[i]);

        StopAllPreviewClips();
        previewQueue = clips;
        previewIndex = 0;
        previewNextTime = EditorApplication.timeSinceStartup;
        EditorApplication.update -= PreviewTick;
        EditorApplication.update += PreviewTick;
    }

    static AudioClip[] previewQueue;
    static int previewIndex;
    static double previewNextTime;

    static void PreviewTick()
    {
        if (previewQueue == null) { EditorApplication.update -= PreviewTick; return; }
        if (EditorApplication.timeSinceStartup < previewNextTime) return;

        if (previewIndex >= previewQueue.Length)
        {
            Debug.Log("[Dinohopp] Preview done.");
            EditorApplication.update -= PreviewTick;
            previewQueue = null;
            return;
        }

        var clip = previewQueue[previewIndex];
        var name = Path.GetFileNameWithoutExtension(MushroomVoicePaths[previewIndex]);
        Debug.Log($"[Dinohopp] ▶ {previewIndex + 1}/{previewQueue.Length}  {name}  ({clip.length:F2}s)");
        PlayPreviewClip(clip);
        // Wait for clip to finish + 0.5 s gap before next.
        previewNextTime = EditorApplication.timeSinceStartup + Mathf.Max(clip.length, 0.2f) + 0.5f;
        previewIndex++;
    }

    static void PlayPreviewClip(AudioClip clip)
    {
        var t = typeof(AudioImporter).Assembly.GetType("UnityEditor.AudioUtil");
        if (t == null) return;
        // Unity 2020+: PlayPreviewClip. Older: PlayClip. Try both for safety.
        var m = t.GetMethod("PlayPreviewClip",
                    BindingFlags.Static | BindingFlags.Public, null,
                    new[] { typeof(AudioClip), typeof(int), typeof(bool) }, null)
             ?? t.GetMethod("PlayClip",
                    BindingFlags.Static | BindingFlags.Public, null,
                    new[] { typeof(AudioClip), typeof(int), typeof(bool) }, null);
        m?.Invoke(null, new object[] { clip, 0, false });
    }

    static void StopAllPreviewClips()
    {
        var t = typeof(AudioImporter).Assembly.GetType("UnityEditor.AudioUtil");
        if (t == null) return;
        var m = t.GetMethod("StopAllPreviewClips", BindingFlags.Static | BindingFlags.Public)
             ?? t.GetMethod("StopAllClips",        BindingFlags.Static | BindingFlags.Public);
        m?.Invoke(null, null);
    }

    // ---------- Synthesis ----------

    // Soft "bloop" — rising sine 220 -> 440 Hz over 180 ms.
    static float[] JumpSamples()
    {
        const float duration = 0.18f;
        int n = (int)(SampleRate * duration);
        var buf = new float[n];
        for (int i = 0; i < n; i++)
        {
            float t = (float)i / SampleRate;
            float u = (float)i / n;
            float freq = Mathf.Lerp(220f, 440f, u);
            float env  = Envelope(u, attack: 0.05f, sustainEnd: 0.55f);
            buf[i] = Mathf.Sin(2f * Mathf.PI * freq * t) * env * 0.4f;
        }
        return buf;
    }

    // Soft thud — descending 140 -> 70 Hz with a touch of second harmonic.
    static float[] LandSamples()
    {
        const float duration = 0.22f;
        int n = (int)(SampleRate * duration);
        var buf = new float[n];
        for (int i = 0; i < n; i++)
        {
            float t = (float)i / SampleRate;
            float u = (float)i / n;
            float freq = Mathf.Lerp(140f, 70f, u);
            float env  = Envelope(u, attack: 0.03f, sustainEnd: 0.4f);
            float s = Mathf.Sin(2f * Mathf.PI * freq * t)
                    + 0.3f * Mathf.Sin(2f * Mathf.PI * freq * 2f * t);
            buf[i] = s * env * 0.35f;
        }
        return buf;
    }

    // Light "boing" — quick pitch bend up and back.
    static float[] BounceSamples()
    {
        const float duration = 0.15f;
        int n = (int)(SampleRate * duration);
        var buf = new float[n];
        for (int i = 0; i < n; i++)
        {
            float t = (float)i / SampleRate;
            float u = (float)i / n;
            float bend = Mathf.Sin(Mathf.PI * u);      // 0 -> 1 -> 0
            float freq = 350f + bend * 150f;
            float env  = Envelope(u, attack: 0.04f, sustainEnd: 0.5f);
            buf[i] = Mathf.Sin(2f * Mathf.PI * freq * t) * env * 0.4f;
        }
        return buf;
    }

    // Simple ASR envelope. attack/sustainEnd are fractions of the clip (0..1).
    static float Envelope(float u, float attack, float sustainEnd)
    {
        if (u < attack)     return u / attack;
        if (u < sustainEnd) return 1f;
        float decayPos = (u - sustainEnd) / (1f - sustainEnd);
        return Mathf.Pow(1f - decayPos, 2f);
    }

    // ---------- Mushroom voice generators ----------

    /// <summary>M1: cartoony boing — sine with big downward pitch bend.</summary>
    static float[] SoftBoingSamples()
    {
        return SineEnvClip(
            duration: 0.45f,
            startFreq: 400f, endFreq: 150f,    // dramatic sweep DOWN = clear "boing"
            attack: 0.06f, sustainEnd: 0.30f,
            amplitude: 0.70f);
    }

    /// <summary>M2: two staccato triangle bleeps "pip-pip" at different pitches.</summary>
    static float[] DoublePipSamples()
    {
        const float pipDur = 0.05f;
        const float gapDur = 0.03f;
        const float amp = 0.78f;
        float totalDur = pipDur * 2f + gapDur;
        int n = (int)(SampleRate * totalDur);
        int pip1End = (int)(SampleRate * pipDur);
        int gapEnd  = (int)(SampleRate * (pipDur + gapDur));
        var buf = new float[n];

        for (int i = 0; i < n; i++)
        {
            float t = (float)i / SampleRate;
            if (i < pip1End)
            {
                float u = (float)i / pip1End;
                buf[i] = TriangleSample(700f, t) * Envelope(u, 0.05f, 0.40f) * amp;
            }
            else if (i >= gapEnd)
            {
                int localI = i - gapEnd;
                int localN = n - gapEnd;
                float u = (float)localI / localN;
                buf[i] = TriangleSample(900f, t) * Envelope(u, 0.05f, 0.40f) * amp;
            }
            // else: silent gap
        }
        return buf;
    }

    /// <summary>M3: percussive plop — sharp attack + exponential downward sweep.</summary>
    static float[] PlopBubbleSamples()
    {
        const float duration = 0.22f;
        int n = (int)(SampleRate * duration);
        var buf = new float[n];
        double phase = 0.0;
        double dt = 1.0 / SampleRate;
        const double tau = 2.0 * System.Math.PI;

        for (int i = 0; i < n; i++)
        {
            float u = (float)i / n;
            // Exponential drop 600 -> 130 Hz (u² curves it more)
            float freq = Mathf.Lerp(600f, 130f, u * u);
            phase += tau * freq * dt;
            float s = (float)System.Math.Sin(phase);

            // Sharp 3 ms attack, then exponential decay for the "pop"
            float attack = u < 0.013f ? u / 0.013f : 1f;
            float decay  = u < 0.013f ? 1f : Mathf.Exp(-5f * (u - 0.013f));
            buf[i] = s * attack * decay * 0.80f;
        }
        return buf;
    }

    /// <summary>M4: HEAVY low boing — fundamental + harmonic + strong wobble, long.</summary>
    static float[] BigBoingSamples()
    {
        return SineEnvClip(
            duration: 0.60f,
            startFreq: 130f, endFreq: 100f,     // very LOW and dips further down
            attack: 0.05f, sustainEnd: 0.45f,
            amplitude: 0.85f,
            harmonic2Amp: 0.55f,                // strong octave harmonic = body
            vibratoHz: 6f, vibratoAmount: 0.15f); // pronounced wobble
    }

    /// <summary>M5: bell ding — additive inharmonic partials, very long ring.</summary>
    static float[] DingSamples()
    {
        const float duration = 1.0f;
        int n = (int)(SampleRate * duration);
        var buf = new float[n];
        double dt = 1.0 / SampleRate;

        // Bell-like inharmonic partials (ratios roughly 1 : 2.01 : 3 : 5)
        float[] freqs  = { 440f, 886f, 1320f, 2200f };
        float[] amps   = { 0.55f, 0.30f, 0.18f, 0.10f };
        float[] decays = { 2.5f,  3.5f,  5.0f,  7.0f }; // higher partials fade quicker

        for (int i = 0; i < n; i++)
        {
            float t = (float)(i * dt);
            float s = 0f;
            for (int p = 0; p < freqs.Length; p++)
            {
                float d = Mathf.Exp(-decays[p] * t);
                s += Mathf.Sin(2f * Mathf.PI * freqs[p] * t) * amps[p] * d;
            }
            // 3 ms attack for crisp strike
            float attack = Mathf.Clamp01(t / 0.003f);
            buf[i] = s * attack * 0.75f;
        }
        return buf;
    }

    /// <summary>M6: bright cartoon pop — square wave with upward pitch sweep.</summary>
    static float[] HappyPopSamples()
    {
        const float duration = 0.12f;
        int n = (int)(SampleRate * duration);
        var buf = new float[n];
        double phase = 0.0;
        double dt = 1.0 / SampleRate;

        for (int i = 0; i < n; i++)
        {
            float u = (float)i / n;
            float freq = Mathf.Lerp(250f, 750f, u);
            phase += freq * dt;
            // Square wave: +1 first half cycle, -1 second half
            float sq = (phase - System.Math.Floor(phase) < 0.5) ? 1f : -1f;
            // Soften slightly — pure square is harsh for small kids
            sq *= 0.65f;
            float env = Envelope(u, 0.02f, 0.40f);
            buf[i] = sq * env * 0.70f;
        }
        return buf;
    }

    static float TriangleSample(float freq, float t)
    {
        float phase = freq * t;
        return 4f * Mathf.Abs(phase - Mathf.Floor(phase + 0.5f)) - 1f;
    }

    // ---------- Letter audio ----------

    /// <summary>Quick bright ping when a single letter is collected.</summary>
    static float[] LetterCollectSamples()
    {
        return SineEnvClip(
            duration: 0.18f,
            startFreq: 880f, endFreq: 1100f,    // chime rises slightly
            attack: 0.01f, sustainEnd: 0.20f,
            amplitude: 0.65f);
    }

    /// <summary>Happy four-note ascending arpeggio (C5-E5-G5-C6) when VIOLA completes.</summary>
    static float[] AllLettersSamples()
    {
        float[] notes = { 523.25f, 659.25f, 783.99f, 1046.50f }; // C E G C major chord
        const float noteDur = 0.13f;
        const float noteGap = 0.04f;

        int noteSamples = (int)(SampleRate * noteDur);
        int gapSamples  = (int)(SampleRate * noteGap);
        int total = notes.Length * noteSamples + (notes.Length - 1) * gapSamples;
        var buf = new float[total];

        for (int n = 0; n < notes.Length; n++)
        {
            int start = n * (noteSamples + gapSamples);
            for (int i = 0; i < noteSamples && start + i < total; i++)
            {
                float u = (float)i / noteSamples;
                float t = (float)(start + i) / SampleRate;
                float tri = TriangleSample(notes[n], t);
                float env = Envelope(u, 0.04f, 0.40f);
                buf[start + i] = tri * env * 0.55f;
            }
        }
        return buf;
    }

    // ---------- Generic synth helper ----------

    /// <summary>
    /// Generic sine generator with linear frequency sweep, ASR envelope, optional
    /// octave-up harmonic and optional vibrato. Used by some of the simpler voices.
    /// </summary>
    static float[] SineEnvClip(float duration, float startFreq, float endFreq,
                                float attack, float sustainEnd, float amplitude,
                                float harmonic2Amp = 0f,
                                float vibratoHz = 0f, float vibratoAmount = 0f)
    {
        int n = (int)(SampleRate * duration);
        var buf = new float[n];
        double phase = 0.0;
        double phase2 = 0.0;
        const double tau = 2.0 * System.Math.PI;
        double dt = 1.0 / SampleRate;

        for (int i = 0; i < n; i++)
        {
            float u = (float)i / n;
            float freq = Mathf.Lerp(startFreq, endFreq, u);
            if (vibratoAmount > 0f && vibratoHz > 0f)
            {
                float vib = Mathf.Sin((float)(tau * vibratoHz * i * dt));
                freq *= 1f + vib * vibratoAmount;
            }

            phase += tau * freq * dt;
            float s = (float)System.Math.Sin(phase);
            if (harmonic2Amp > 0f)
            {
                phase2 += tau * freq * 2.0 * dt;
                s = (s + (float)System.Math.Sin(phase2) * harmonic2Amp) / (1f + harmonic2Amp);
            }
            float env = Envelope(u, attack, sustainEnd);
            buf[i] = s * env * amplitude;
        }
        return buf;
    }

    // ---------- WAV writer (RIFF / 16-bit mono PCM) ----------

    static void WriteWav(string path, float[] samples)
    {
        int byteCount = samples.Length * 2;
        using (var fs = new FileStream(path, FileMode.Create))
        using (var bw = new BinaryWriter(fs))
        {
            bw.Write(Encoding.ASCII.GetBytes("RIFF"));
            bw.Write(36 + byteCount);
            bw.Write(Encoding.ASCII.GetBytes("WAVE"));
            bw.Write(Encoding.ASCII.GetBytes("fmt "));
            bw.Write(16);                         // fmt chunk size
            bw.Write((short)1);                   // PCM
            bw.Write((short)1);                   // mono
            bw.Write(SampleRate);
            bw.Write(SampleRate * 2);             // byte rate
            bw.Write((short)2);                   // block align
            bw.Write((short)16);                  // bits per sample
            bw.Write(Encoding.ASCII.GetBytes("data"));
            bw.Write(byteCount);
            for (int i = 0; i < samples.Length; i++)
            {
                float s = Mathf.Clamp(samples[i], -1f, 1f);
                bw.Write((short)(s * 32767f));
            }
        }
    }

    static void EnsureFolder()
    {
        if (!AssetDatabase.IsValidFolder(AudioFolder))
            AssetDatabase.CreateFolder("Assets", "Audio");
    }

    static void EnsureGeneratedFolder()
    {
        if (!AssetDatabase.IsValidFolder(GeneratedFolder))
            AssetDatabase.CreateFolder(AudioFolder, "Generated");
    }
}
