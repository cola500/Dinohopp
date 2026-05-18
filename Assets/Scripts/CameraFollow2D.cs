using UnityEngine;

public class CameraFollow2D : MonoBehaviour
{
    [Tooltip("What the camera follows (the dino).")]
    public Transform target;

    [Tooltip("World-space offset from the target.")]
    public Vector3 offset = new Vector3(2.5f, 1f, -10f);

    [Tooltip("Smaller = snappier. Larger = lazier.")]
    public float smoothTime = 0.25f;

    [Tooltip("Lock vertical position so jumps don't shake the camera.")]
    public bool lockY = true;

    [Tooltip("World Y the camera stays at when lockY is enabled.")]
    public float lockedY = 1f;

    Vector3 velocity;

    void Start()
    {
        if (target == null)
        {
            var found = GameObject.FindWithTag("Player");
            if (found != null) target = found.transform;
        }
    }

    void LateUpdate()
    {
        if (target == null) return;

        Vector3 desired = target.position + offset;
        if (lockY) desired.y = lockedY;
        desired.z = offset.z;

        transform.position = Vector3.SmoothDamp(transform.position, desired, ref velocity, smoothTime);
    }

    /// <summary>Snap immediately to the target — used after a level reset so the
    /// camera doesn't slowly pan back across the level.</summary>
    public void SnapToTarget()
    {
        if (target == null) return;
        Vector3 desired = target.position + offset;
        if (lockY) desired.y = lockedY;
        desired.z = offset.z;
        transform.position = desired;
        velocity = Vector3.zero;
    }
}
