using UnityEngine;

/// <summary>
/// Per-level metadata read by GameManager when activating a level. Attached to each
/// Level_X root GameObject; the root + all children make up that level's content.
/// </summary>
public class LevelInfo : MonoBehaviour
{
    [Tooltip("Friendly name for debug logs.")]
    public string displayName = "Level";

    [Tooltip("Word the player collects this level. Letter pickups live under this level root.")]
    public string wordToCollect = "VIOLA";

    [Tooltip("Per-letter fill colors, one per position in the word. Indexed 0..word.Length-1.")]
    public Color[] letterColors;

    [Tooltip("Camera background color while this level is active.")]
    public Color skyColor = new Color(0.62f, 0.82f, 0.93f, 1f);

    [Tooltip("Dino's starting world position for this level.")]
    public Vector3 dinoStartPosition = Vector3.zero;
}
