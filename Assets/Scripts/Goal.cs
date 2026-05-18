using UnityEngine;

/// <summary>
/// Trigger volume placed at the end of the level. When the dino enters, notifies
/// the GameManager so it can transition to Success.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class Goal : MonoBehaviour
{
    [Tooltip("Tag the goal looks for to detect the dino.")]
    public string playerTag = "Player";

    void Awake()
    {
        GetComponent<Collider2D>().isTrigger = true;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag(playerTag)) return;
        if (GameManager.Instance != null)
            GameManager.Instance.OnGoalReached();
    }
}
