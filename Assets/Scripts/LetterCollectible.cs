using UnityEngine;

/// <summary>
/// A pickup letter for the VIOLA-collection mechanic. When the dino's collider
/// enters this trigger, plays a ping, notifies LetterCollectionManager, then
/// animates upward and destroys itself.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class LetterCollectible : MonoBehaviour
{
    [Tooltip("Position (0-based) inside the word. Lets us spell LINDENGARD with duplicate letters: each pickup is a distinct slot.")]
    public int positionIndex = 0;
    [Tooltip("Display letter, e.g. 'V' / 'L'. Cosmetic — manager identifies by positionIndex.")]
    public string letter = "V";
    [Tooltip("Tag that triggers collection.")]
    public string playerTag = "Player";

    [Header("Audio")]
    public AudioClip collectClip;
    [Range(0f, 1f)] public float volume = 0.75f;

    [Header("Pickup Animation")]
    [Tooltip("How far the letter flies up after pickup.")]
    public float flyUpDistance = 1.5f;
    [Tooltip("Duration of fly-up + fade.")]
    public float flyDuration = 0.45f;

    bool collected;
    float animTimer;
    Vector3 collectStartPos;
    Vector3 collectStartScale;
    AudioSource audioSource;
    SpriteRenderer[] sprites;
    TextMesh[] textMeshes;
    Collider2D col2D;

    // Snapshot of the letter's initial visual state, captured once at Awake.
    // Used by ResetState() so the same letter can be re-collected next round.
    Vector3 originalPos;
    Vector3 originalScale;
    Color[] originalColors;
    Color[] originalTextColors;

    void Awake()
    {
        CaptureOriginals();
    }

    /// <summary>
    /// Cache the letter's pristine visual state. Called from Awake when the letter
    /// is active at scene load, OR lazily from ResetState when the letter belongs
    /// to a level root that started inactive (Unity skips Awake on inactive children).
    /// </summary>
    void CaptureOriginals()
    {
        if (sprites != null) return; // already captured
        col2D = GetComponent<Collider2D>();
        if (col2D != null) col2D.isTrigger = true;
        audioSource = GetComponent<AudioSource>();
        // includeInactive: true so we find the body sprites + text meshes even when
        // our own level root hasn't been activated yet.
        sprites     = GetComponentsInChildren<SpriteRenderer>(true);
        textMeshes  = GetComponentsInChildren<TextMesh>(true);

        originalPos = transform.position;
        originalScale = transform.localScale;
        originalColors = new Color[sprites.Length];
        for (int i = 0; i < sprites.Length; i++) originalColors[i] = sprites[i].color;
        originalTextColors = new Color[textMeshes.Length];
        for (int i = 0; i < textMeshes.Length; i++) originalTextColors[i] = textMeshes[i].color;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (collected) return;
        if (!other.CompareTag(playerTag)) return;
        Collect();
    }

    void Collect()
    {
        collected = true;
        animTimer = 0f;
        collectStartPos = transform.position;
        collectStartScale = transform.localScale;

        if (collectClip != null)
        {
            if (audioSource != null)
                audioSource.PlayOneShot(collectClip, volume);
            else
                AudioSource.PlayClipAtPoint(collectClip, transform.position, volume);
        }

        // Prevent re-trigger while flying up.
        if (col2D != null) col2D.enabled = false;

        if (LetterCollectionManager.Instance != null)
            LetterCollectionManager.Instance.Collect(positionIndex);
    }

    /// <summary>Restore the letter to its original state so it can be collected again.
    /// Called by LetterCollectionManager on a level reset.</summary>
    public void ResetState()
    {
        // Safe to call before Awake (inactive level roots get reset too).
        CaptureOriginals();

        collected = false;
        animTimer = 0f;
        transform.position = originalPos;
        transform.localScale = originalScale;
        for (int i = 0; i < sprites.Length; i++) sprites[i].color = originalColors[i];
        for (int i = 0; i < textMeshes.Length; i++) textMeshes[i].color = originalTextColors[i];
        if (col2D != null) col2D.enabled = true;
        gameObject.SetActive(true);
    }

    void Update()
    {
        if (!collected) return;
        animTimer += Time.deltaTime;
        float u = Mathf.Clamp01(animTimer / flyDuration);

        transform.position = collectStartPos + Vector3.up * (flyUpDistance * u);

        // Sin-pop: peaks at u=0.5 then settles back — feels snappier than a linear grow.
        float pulse = Mathf.Sin(u * Mathf.PI);
        transform.localScale = collectStartScale * (1f + pulse * 0.35f);

        float alpha = 1f - u;
        for (int i = 0; i < sprites.Length; i++)
        {
            var c = sprites[i].color;
            c.a = alpha;
            sprites[i].color = c;
        }
        for (int i = 0; i < textMeshes.Length; i++)
        {
            var c = textMeshes[i].color;
            c.a = alpha;
            textMeshes[i].color = c;
        }

        // Deactivate (don't destroy) so LetterCollectionManager can ResetState() us
        // for a clean next round.
        if (u >= 1f) gameObject.SetActive(false);
    }
}
