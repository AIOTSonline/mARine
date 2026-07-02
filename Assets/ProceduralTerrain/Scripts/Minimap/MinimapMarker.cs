using UnityEngine;

// Add to anything you want shown on the minimap. Self-registers on enable and
// removes itself on disable/destroy, so streamed-in/out props clean up on their own.
public class MinimapMarker : MonoBehaviour
{
    [Tooltip("Blip colour on the minimap.")]
    public Color color = new Color(0.5f, 1f, 0.65f, 1f);

    void OnEnable()  => MinimapRegistry.Add(this);
    void OnDisable() => MinimapRegistry.Remove(this);
}
