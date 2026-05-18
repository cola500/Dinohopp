using UnityEngine;

/// <summary>
/// Cosmetic-only: while the dino stands on a mushroom, the body composite is
/// lifted along a parabola peaking in the middle of the cap, so the feet appear
/// to follow the mushroom's oval shape. Operates on a separate child transform
/// under Visual so DinoLocomotion (bob + breath) and this script don't fight.
///
/// Collider, Rigidbody2D, jump physics — all untouched. Pure visual polish.
/// </summary>
[RequireComponent(typeof(DinoController))]
public class DinoSurfaceVisualOffset : MonoBehaviour
{
    [Tooltip("Child holding the dino body composite. Auto-resolved to 'Visual/SurfaceCurve' if null.")]
    public Transform surfaceCurve;

    [Tooltip("Peak Y DROP (world units) at the mushroom cap's edge. At the cap centre " +
             "the offset is 0 (dino rests on the collider's flat top = cap's peak). " +
             "Higher = more visible sink toward the cap edges. Subtle (0.15–0.25).")]
    public float curveHeight = 0.20f;

    [Tooltip("Smoothing speed for the offset. Higher = snappier response when stepping on or off a mushroom.")]
    public float smoothSpeed = 10f;

    DinoController controller;
    Vector3 baseLocalPos;
    float currentOffset;
    float smoothVelocity;

    void Awake()
    {
        controller = GetComponent<DinoController>();
        if (surfaceCurve == null)
        {
            var visual = transform.Find("Visual");
            if (visual != null) surfaceCurve = visual.Find("SurfaceCurve");
        }
        if (surfaceCurve != null) baseLocalPos = surfaceCurve.localPosition;
    }

    void LateUpdate()
    {
        if (surfaceCurve == null) return;

        float target = ComputeCurveOffset();

        // Framerate-independent smoothing via SmoothDamp.
        float smoothTime = (smoothSpeed > 0.01f) ? (1f / smoothSpeed) : 0.01f;
        currentOffset = Mathf.SmoothDamp(currentOffset, target, ref smoothVelocity, smoothTime);

        surfaceCurve.localPosition = baseLocalPos + new Vector3(0f, currentOffset, 0f);
    }

    /// <summary>
    /// Visual offset that traces the mushroom cap's oval surface from above.
    /// The collider top sits AT THE CAP'S PEAK (cap centre), so the natural rest
    /// position when centred is already correct → offset 0 at centre. As dino moves
    /// toward the cap edges the visible cap drops away, so the feet must sink to
    /// follow → negative offset proportional to n² (parabolic approximation).
    /// </summary>
    float ComputeCurveOffset()
    {
        if (controller == null || !controller.IsGrounded) return 0f;
        var ground = controller.CurrentGroundCollider;
        if (ground == null) return 0f;

        // Only curve over mushrooms — flat ground stays flat.
        if (ground.GetComponent<MushroomBounceFeedback>() == null) return 0f;

        var bounds = ground.bounds;
        float halfWidth = bounds.extents.x;
        if (halfWidth <= 0.001f) return 0f;

        float dx = transform.position.x - bounds.center.x;
        float n = Mathf.Clamp(Mathf.Abs(dx) / halfWidth, 0f, 1f);
        float drop = n * n;              // 0 at centre, 1 at edges
        return -drop * curveHeight;      // sink toward edges, neutral at centre
    }
}
