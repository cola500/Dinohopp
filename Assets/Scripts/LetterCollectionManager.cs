using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Singleton tracker for the "spell VIOLA" pickup mechanic. Holds collected
/// state, refreshes the on-screen word display, and shows a brief celebration
/// panel + sound when the full word is complete.
/// </summary>
public class LetterCollectionManager : MonoBehaviour
{
    public static LetterCollectionManager Instance { get; private set; }

    [Header("Target")]
    [Tooltip("The word the player spells. Each unique character maps to one collectible.")]
    public string targetWord = "VIOLA";

    [Header("UI")]
    [Tooltip("Top-of-screen Text showing 'V I O L A' with collected letters highlighted.")]
    public Text wordText;
    [Tooltip("Panel briefly shown when the full word is collected (auto-hides).")]
    public GameObject allCollectedPanel;
    [Tooltip("Seconds the all-collected panel stays on-screen.")]
    public float allCollectedPanelDuration = 2.5f;

    [Header("Audio")]
    public AudioClip allCollectedClip;
    [Range(0f, 1f)] public float allCollectedVolume = 0.7f;

    [Header("Colors")]
    [Tooltip("Per-letter fill colors. Order matches targetWord. SceneBuilder reads these for the in-world pickups too — keep in sync.")]
    public Color[] letterColors = DefaultLetterColors;

    /// <summary>
    /// Source-of-truth palette for V-I-O-L-A. SceneBuilder reads this so the
    /// in-world pickup colors always match the UI row.
    /// </summary>
    public static readonly Color[] DefaultLetterColors = new[]
    {
        new Color(0.96f, 0.45f, 0.40f, 1f), // V — coral red
        new Color(1.00f, 0.83f, 0.30f, 1f), // I — golden yellow
        new Color(0.50f, 0.80f, 0.45f, 1f), // O — soft green
        new Color(0.42f, 0.68f, 0.95f, 1f), // L — sky blue
        new Color(0.72f, 0.55f, 0.92f, 1f), // A — lavender
    };

    [Header("Pop animation")]
    [Tooltip("Duration of the scale pop on the VIOLA row when a letter is collected.")]
    public float popDuration = 0.30f;
    [Tooltip("Peak scale multiplier at the top of the pop.")]
    public float popPeak = 1.20f;

    readonly HashSet<char> collected = new HashSet<char>();
    AudioSource audioSource;
    DinoFeedback dinoFeedback;
    float allCollectedHideAt = float.PositiveInfinity;
    float popTimer;
    Vector3 wordTextBaseScale = Vector3.one;

    public bool AllCollected => collected.Count >= UniqueLetterCount();

    int UniqueLetterCount()
    {
        var seen = new HashSet<char>();
        for (int i = 0; i < targetWord.Length; i++) seen.Add(targetWord[i]);
        return seen.Count;
    }

    void Awake()
    {
        Instance = this;
        audioSource = GetComponent<AudioSource>();
        var player = GameObject.FindWithTag("Player");
        if (player != null) dinoFeedback = player.GetComponent<DinoFeedback>();
    }

    void Start()
    {
        if (allCollectedPanel != null) allCollectedPanel.SetActive(false);
        if (wordText != null) wordTextBaseScale = wordText.transform.localScale;
        RefreshWordText();
    }

    void Update()
    {
        if (Time.time >= allCollectedHideAt)
        {
            allCollectedHideAt = float.PositiveInfinity;
            if (allCollectedPanel != null) allCollectedPanel.SetActive(false);
        }

        // Scale-pop on the VIOLA row when a letter has just been collected.
        if (popTimer > 0f && wordText != null)
        {
            popTimer -= Time.deltaTime;
            float t = Mathf.Clamp01(popTimer / popDuration);
            float pulse = Mathf.Sin(t * Mathf.PI); // 0 → 1 → 0 as t goes 1 → 0.5 → 0
            float scale = 1f + pulse * (popPeak - 1f);
            wordText.transform.localScale = wordTextBaseScale * scale;
            if (popTimer <= 0f) wordText.transform.localScale = wordTextBaseScale;
        }
    }

    public void Collect(string letter)
    {
        if (string.IsNullOrEmpty(letter)) return;
        char c = letter[0];
        if (collected.Contains(c)) return;
        if (targetWord.IndexOf(c) < 0) return;

        collected.Add(c);
        RefreshWordText();
        popTimer = popDuration; // trigger UI scale pop
        if (dinoFeedback != null) dinoFeedback.TriggerJoyBounce();

        if (AllCollected)
        {
            if (allCollectedPanel != null)
            {
                allCollectedPanel.SetActive(true);
                allCollectedHideAt = Time.time + allCollectedPanelDuration;
            }
            if (audioSource != null && allCollectedClip != null)
                audioSource.PlayOneShot(allCollectedClip, allCollectedVolume);
        }
    }

    void RefreshWordText()
    {
        if (wordText == null) return;
        wordText.supportRichText = true;

        var sb = new StringBuilder();
        for (int i = 0; i < targetWord.Length; i++)
        {
            char c = targetWord[i];
            Color baseColor = (i < letterColors.Length) ? letterColors[i] : Color.white;
            Color uiColor = collected.Contains(c) ? baseColor : Dim(baseColor);
            string hex = ColorUtility.ToHtmlStringRGBA(uiColor);
            sb.Append("<color=#").Append(hex).Append('>').Append(c).Append("</color>");
            if (i < targetWord.Length - 1) sb.Append("  ");
        }
        wordText.text = sb.ToString();
    }

    /// <summary>Darker, semi-transparent version used for not-yet-collected letters.</summary>
    static Color Dim(Color c) => new Color(c.r * 0.45f, c.g * 0.45f, c.b * 0.45f, 0.70f);

    /// <summary>
    /// Clear all collected letters, restore every LetterCollectible to its original
    /// state, and reset the UI. Called by GameManager.ResetDino() so every restart
    /// (after fall OR after success) starts with a fresh V-I-O-L-A row.
    /// </summary>
    public void ResetCollection()
    {
        collected.Clear();
        RefreshWordText();
        if (allCollectedPanel != null) allCollectedPanel.SetActive(false);
        allCollectedHideAt = float.PositiveInfinity;
        popTimer = 0f;
        if (wordText != null) wordText.transform.localScale = wordTextBaseScale;

        var all = FindObjectsByType<LetterCollectible>(FindObjectsInactive.Include);
        for (int i = 0; i < all.Length; i++) all[i].ResetState();
    }
}
