using UnityEngine;

/// <summary>
/// A pickup letter for the VIOLA-collection mechanic. When the dino's collider
/// enters this trigger, plays a ping, notifies LetterCollectionManager, then
/// animates upward and destroys itself.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class LetterCollectible : MonoBehaviour
{
    [Tooltip("Which letter this is, e.g. 'V'. Sent to LetterCollectionManager on pickup.")]
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
    Collider2D col2D;

    // Snapshot of the letter's initial visual state, captured once at Awake.
    // Used by ResetState() so the same letter can be re-collected next round.
    Vector3 originalPos;
    Vector3 originalScale;
    Color[] originalColors;

    void Awake()
    {
        col2D = GetComponent<Collider2D>();
        col2D.isTrigger = true;
        audioSource = GetComponent<AudioSource>();
        sprites = GetComponentsInChildren<SpriteRenderer>();

        originalPos = transform.position;
        originalScale = transform.localScale;
        originalColors = new Color[sprites.Length];
        for (int i = 0; i < sprites.Length; i++) originalColors[i] = sprites[i].color;
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
            LetterCollectionManager.Instance.Collect(letter);
    }

    /// <summary>Restore the letter to its original state so it can be collected again.
    /// Called by LetterCollectionManager on a level reset.</summary>
    public void ResetState()
    {
        collected = false;
        animTimer = 0f;
        transform.position = originalPos;
        transform.localScale = originalScale;
        for (int i = 0; i < sprites.Length; i++) sprites[i].color = originalColors[i];
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

        // Deactivate (don't destroy) so LetterCollectionManager can ResetState() us
        // for a clean next round.
        if (u >= 1f) gameObject.SetActive(false);
    }
}
