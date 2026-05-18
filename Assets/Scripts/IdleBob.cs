using UnityEngine;

/// <summary>
/// Slow vertical bob on the GameObject's localPosition. Designed for visual children
/// so the parent collider stays put — does not touch physics. Each instance can run at
/// a different phase so multiple bobs (e.g., mushrooms) don't move in lock-step.
/// </summary>
public class IdleBob : MonoBehaviour
{
    [Tooltip("Vertical bob amplitude in local units. Keep tiny for a 'breathing' feel.")]
    public float amplitude = 0.04f;
    [Tooltip("Cycles per second (Hz). Slow values feel ambient.")]
    public float frequency = 0.5f;
    [Tooltip("Per-instance phase offset (seconds) so simultaneous bobs aren't synced.")]
    public float phaseOffset;

    Vector3 baseLocalPos;

    void Awake()
    {
        baseLocalPos = transform.localPosition;
    }

    void Update()
    {
        float y = Mathf.Sin((Time.time + phaseOffset) * Mathf.PI * 2f * frequency) * amplitude;
        transform.localPosition = baseLocalPos + new Vector3(0f, y, 0f);
    }
}
