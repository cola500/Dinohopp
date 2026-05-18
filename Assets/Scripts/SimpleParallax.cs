using UnityEngine;

/// <summary>
/// Moves this transform a fraction of the target transform's displacement.
/// Useful for background layers that should feel distant — stars stay in view
/// but drift slower than the world, giving a sense of depth.
/// </summary>
public class SimpleParallax : MonoBehaviour
{
    [Tooltip("What to parallax against. Auto-finds Camera.main at Start if null.")]
    public Transform target;

    [Range(0f, 1f)]
    [Tooltip("0 = locked to start (looks static), 1 = matches the camera (no relative motion).")]
    public float parallaxFactor = 0.2f;

    Vector3 startTargetPos;
    Vector3 startSelfPos;
    bool ready;

    void Start()
    {
        if (target == null && Camera.main != null) target = Camera.main.transform;
        if (target == null) return;
        startTargetPos = target.position;
        startSelfPos   = transform.position;
        ready = true;
    }

    void LateUpdate()
    {
        if (!ready) return;
        Vector3 delta = target.position - startTargetPos;
        transform.position = startSelfPos + new Vector3(delta.x * parallaxFactor,
                                                       delta.y * parallaxFactor,
                                                       0f);
    }
}
