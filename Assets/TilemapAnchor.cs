using UnityEngine;
using UnityEngine.Tilemaps;

public class TilemapAnchor : MonoBehaviour
{
    [HideInInspector] public Bounds tileBounds;

    void Awake()
    {
        CalculateBounds();
    }

    void CalculateBounds()
    {
        // Get all tilemaps in this prefab (handles multi-tilemap rooms too)
        Tilemap[] tilemaps = GetComponentsInChildren<Tilemap>();
        if (tilemaps.Length == 0) return;

        // Encapsulate all tilemaps to get total bounds
        tileBounds = tilemaps[0].localBounds;
        for (int i = 1; i < tilemaps.Length; i++)
        {
            tilemaps[i].CompressBounds();
            tileBounds.Encapsulate(tilemaps[i].localBounds);
        }
    }

    // Get world position of the tile content center
    public Vector3 GetVisualCenter()
    {
        return transform.position + tileBounds.center;
    }

    void OnDrawGizmosSelected()
    {
        // Visualize the actual tile bounds in scene view
        Gizmos.color = new Color(0, 1, 1, 0.3f);
        Gizmos.DrawWireCube(GetVisualCenter(), tileBounds.size);

        Gizmos.color = Color.red;
        Gizmos.DrawSphere(transform.position, 0.1f); // Show pivot
    }
}