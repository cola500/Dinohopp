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

    void Awake()
    {
        var col = GetComponent<Collider2D>();
        col.isTrigger = true;
        audioSource = GetComponent<AudioSource>();
        sprites = GetComponentsInChildren<SpriteRenderer>();
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
        var col = GetComponent<Collider2D>();
        if (col != null) col.enabled = false;

        if (LetterCollectionManager.Instance != null)
            LetterCollectionManager.Instance.Collect(letter);
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

        if (u >= 1f) Destroy(gameObject);
    }
}
