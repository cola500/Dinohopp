using System.Text;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Singleton tracker for the "spell a word" pickup mechanic. Holds collected
/// state, refreshes the on-screen word display, shows a brief celebration panel
/// + sound when the full word is complete. SetWord() switches target between
/// levels (e.g. VIOLA → LINDENGARD) including auto-scaling the UI font.
///
/// Tracks BY POSITION INDEX so words with duplicate letters (LINDENGARD has two
/// N's and two D's) treat each pickup as its own slot.
/// </summary>
public class LetterCollectionManager : MonoBehaviour
{
    public static LetterCollectionManager Instance { get; private set; }

    [Header("Target")]
    [Tooltip("Current word the player spells. Letter pickups identify themselves by positionIndex.")]
    public string targetWord = "VIOLA";

    [Header("UI")]
    [Tooltip("Top-of-screen Text showing the word with collected letters highlighted.")]
    public Text wordText;
    [Tooltip("Panel briefly shown when the full word is collected (auto-hides).")]
    public GameObject allCollectedPanel;
    [Tooltip("Seconds the all-collected panel stays on-screen.")]
    public float allCollectedPanelDuration = 2.5f;

    [Header("Audio")]
    public AudioClip allCollectedClip;
    [Range(0f, 1f)] public float allCollectedVolume = 0.7f;

    [Header("Colors")]
    [Tooltip("Per-position fill colors, length matches targetWord. SceneBuilder reads these for the in-world pickups.")]
    public Color[] letterColors = DefaultLetterColors;

    /// <summary>5-color VIOLA palette — Level 1 default + base for cycling on longer words.</summary>
    public static readonly Color[] DefaultLetterColors = new[]
    {
        new Color(0.96f, 0.45f, 0.40f, 1f), // coral red
        new Color(1.00f, 0.83f, 0.30f, 1f), // golden yellow
        new Color(0.50f, 0.80f, 0.45f, 1f), // soft green
        new Color(0.42f, 0.68f, 0.95f, 1f), // sky blue
        new Color(0.72f, 0.55f, 0.92f, 1f), // lavender
    };

    [Header("Pop animation")]
    [Tooltip("Duration of the scale pop on the word row when a letter is collected.")]
    public float popDuration = 0.30f;
    [Tooltip("Peak scale multiplier at the top of the pop.")]
    public float popPeak = 1.20f;

    [Header("Font sizing")]
    [Tooltip("Word-text font size for short words (≤ 5 letters).")]
    public int fontSizeShort = 130;
    [Tooltip("Word-text font size for long words (> 5 letters).")]
    public int fontSizeLong = 90;

    bool[] collected;                       // indexed by position in targetWord
    AudioSource audioSource;
    DinoFeedback dinoFeedback;
    float allCollectedHideAt = float.PositiveInfinity;
    float popTimer;
    Vector3 wordTextBaseScale = Vector3.one;

    public bool AllCollected
    {
        get
        {
            if (collected == null || collected.Length == 0) return false;
            for (int i = 0; i < collected.Length; i++) if (!collected[i]) return false;
            return true;
        }
    }

    void Awake()
    {
        Instance = this;
        audioSource = GetComponent<AudioSource>();
        var player = GameObject.FindWithTag("Player");
        if (player != null) dinoFeedback = player.GetComponent<DinoFeedback>();
        collected = new bool[targetWord != null ? targetWord.Length : 0];
    }

    void Start()
    {
        if (allCollectedPanel != null) allCollectedPanel.SetActive(false);
        if (wordText != null) wordTextBaseScale = wordText.transform.localScale;
        ApplyFontSize();
        RefreshWordText();
    }

    void Update()
    {
        if (Time.time >= allCollectedHideAt)
        {
            allCollectedHideAt = float.PositiveInfinity;
            if (allCollectedPanel != null) allCollectedPanel.SetActive(false);
        }

        if (popTimer > 0f && wordText != null)
        {
            popTimer -= Time.deltaTime;
            float t = Mathf.Clamp01(popTimer / popDuration);
            float pulse = Mathf.Sin(t * Mathf.PI);
            float scale = 1f + pulse * (popPeak - 1f);
            wordText.transform.localScale = wordTextBaseScale * scale;
            if (popTimer <= 0f) wordText.transform.localScale = wordTextBaseScale;
        }
    }

    /// <summary>
    /// Switch to a new word. Resets collected state, replaces colors, auto-scales the
    /// UI font for longer words. Call when entering a new level.
    /// </summary>
    public void SetWord(string word, Color[] colors)
    {
        targetWord = string.IsNullOrEmpty(word) ? "VIOLA" : word;
        letterColors = (colors != null && colors.Length >= targetWord.Length)
            ? colors
            : DefaultLetterColors;
        collected = new bool[targetWord.Length];
        if (allCollectedPanel != null) allCollectedPanel.SetActive(false);
        allCollectedHideAt = float.PositiveInfinity;
        popTimer = 0f;
        if (wordText != null) wordText.transform.localScale = wordTextBaseScale;
        ApplyFontSize();
        RefreshWordText();
    }

    /// <summary>Called by LetterCollectible.OnTriggerEnter2D with its positionIndex.</summary>
    public void Collect(int positionIndex)
    {
        if (collected == null) return;
        if (positionIndex < 0 || positionIndex >= collected.Length) return;
        if (collected[positionIndex]) return;

        collected[positionIndex] = true;
        RefreshWordText();
        popTimer = popDuration;
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
        if (wordText == null || targetWord == null) return;
        wordText.supportRichText = true;

        var sb = new StringBuilder();
        string sep = (targetWord.Length <= 6) ? "  " : " ";
        for (int i = 0; i < targetWord.Length; i++)
        {
            char c = targetWord[i];
            Color baseColor = (letterColors != null && i < letterColors.Length) ? letterColors[i] : Color.white;
            bool isCollected = (collected != null && i < collected.Length && collected[i]);
            Color uiColor = isCollected ? baseColor : Dim(baseColor);
            string hex = ColorUtility.ToHtmlStringRGBA(uiColor);
            sb.Append("<color=#").Append(hex).Append('>').Append(c).Append("</color>");
            if (i < targetWord.Length - 1) sb.Append(sep);
        }
        wordText.text = sb.ToString();
    }

    void ApplyFontSize()
    {
        if (wordText == null) return;
        wordText.fontSize = (targetWord != null && targetWord.Length <= 5) ? fontSizeShort : fontSizeLong;
    }

    /// <summary>Darker, semi-transparent version used for not-yet-collected letters.</summary>
    static Color Dim(Color c) => new Color(c.r * 0.45f, c.g * 0.45f, c.b * 0.45f, 0.70f);

    /// <summary>
    /// Clear collected state + restore every LetterCollectible in the scene to its
    /// original visual state. Called by GameManager.ResetDino() on retry/replay.
    /// Idempotent — safe to call right after SetWord().
    /// </summary>
    public void ResetCollection()
    {
        if (collected != null)
        {
            for (int i = 0; i < collected.Length; i++) collected[i] = false;
        }
        RefreshWordText();
        if (allCollectedPanel != null) allCollectedPanel.SetActive(false);
        allCollectedHideAt = float.PositiveInfinity;
        popTimer = 0f;
        if (wordText != null) wordText.transform.localScale = wordTextBaseScale;

        // Letters from inactive level roots are also returned to "uncollected" so a
        // future activation of that level finds them fresh.
        var all = FindObjectsByType<LetterCollectible>(FindObjectsInactive.Include);
        for (int i = 0; i < all.Length; i++) all[i].ResetState();
    }
}
