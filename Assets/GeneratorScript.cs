using System.Collections.Generic;
using UnityEngine;

public class GeneratorScript : MonoBehaviour
{
    [Header("Prefabs")]
    public GameObject startRoom;
    public GameObject[] hallwayPrefabs;

    [Header("Settings")]
    public int maxIterations = 5000;

    [Header("Collision & Anchors")]
    public LayerMask dungeonLayer;
    public float anchorProbeRadius = 0.05f;
    public float nudgeAmount = 0.01f;

    private Queue<GenerationAnchor> openAnchors = new Queue<GenerationAnchor>();
    private HashSet<Vector3> closedAnchors = new HashSet<Vector3>();
    private System.Random rng = new System.Random();

    private enum AnchorState { Untried, Success, Failed }
    private Dictionary<Vector3, AnchorState> anchorStatus = new Dictionary<Vector3, AnchorState>();

    void Start()
    {
        GenerateDungeon();
    }

    void GenerateDungeon()
    {
        openAnchors.Clear();
        closedAnchors.Clear();
        anchorStatus.Clear();

        // Spawn start room
        GameObject start = Instantiate(startRoom, Vector3.zero, Quaternion.identity);
        EnqueueAnchors(start);

        int iterations = 0;

        while (openAnchors.Count > 0 && iterations < maxIterations)
        {
            iterations++;
            GenerationAnchor current = openAnchors.Dequeue();

            if (current.anchorRef.isOccupied) continue;
            if (closedAnchors.Contains(current.position)) continue;

            bool placed = false;

            // Shuffle remaining prefabs
            Shuffle(current.remainingPrefabs);

            for (int i = current.remainingPrefabs.Count - 1; i >= 0; i--)
            {
                GameObject prefab = current.remainingPrefabs[i];

                if (!PrefabHasAnchor(prefab, Opposite(current.direction))) continue;

                GameObject instance = Instantiate(prefab);

                Physics2D.SyncTransforms();

                Anchor backAnchor = FindAnchor(instance, Opposite(current.direction));
                if (backAnchor == null)
                {
                    Destroy(instance);
                    continue;
                }

                instance.transform.position = current.position - backAnchor.transform.localPosition + DirectionOffset(current.direction);

                if (PrefabOverlaps(instance) || AnchorBlocked(current.position))
                {
                    Destroy(instance);
                    current.remainingPrefabs.RemoveAt(i);
                    continue;
                }

                // Successful placement
                placed = true;
                backAnchor.isOccupied = true;
                current.anchorRef.isOccupied = true;
                anchorStatus[current.position] = AnchorState.Success;
                anchorStatus[backAnchor.transform.position] = AnchorState.Success;

                // **New Step:** Connect overlapping anchors
                List<GenerationAnchor> updatedQueue = new List<GenerationAnchor>(openAnchors);
                for (int j = 0; j < updatedQueue.Count; j++)
                {
                    if (!updatedQueue[j].anchorRef.isOccupied &&
                        Vector3.Distance(updatedQueue[j].position, backAnchor.transform.position) <= anchorProbeRadius)
                    {
                        updatedQueue[j].anchorRef.isOccupied = true;
                        anchorStatus[updatedQueue[j].position] = AnchorState.Success;
                    }
                }
                openAnchors = new Queue<GenerationAnchor>(updatedQueue);

                // Add new anchors
                foreach (Anchor a in instance.GetComponentsInChildren<Anchor>())
                {
                    if (a == backAnchor) continue;
                    if (!a.isOccupied && !closedAnchors.Contains(a.transform.position))
                    {
                        openAnchors.Enqueue(new GenerationAnchor
                        {
                            position = a.transform.position,
                            direction = a.direction,
                            anchorRef = a,
                            remainingPrefabs = new List<GameObject>(hallwayPrefabs)
                        });

                        if (!anchorStatus.ContainsKey(a.transform.position))
                            anchorStatus[a.transform.position] = AnchorState.Untried;
                    }
                }

                break; // stop trying other prefabs
            }

            if (!placed)
            {
                if (current.remainingPrefabs.Count == 0)
                {
                    closedAnchors.Add(current.position);
                    anchorStatus[current.position] = AnchorState.Failed;
                }
                else
                {
                    // Retry next prefab later
                    openAnchors.Enqueue(current);
                }
            }
        }

        Debug.Log($"Dungeon finished: closed anchors = {closedAnchors.Count}, iterations = {iterations}");
    }

    struct GenerationAnchor
    {
        public Vector3 position;
        public Direction direction;
        public Anchor anchorRef;
        public List<GameObject> remainingPrefabs;
    }

    Direction Opposite(Direction d)
    {
        switch (d)
        {
            case Direction.Top: return Direction.Bottom;
            case Direction.Bottom: return Direction.Top;
            case Direction.Left: return Direction.Right;
            case Direction.Right: return Direction.Left;
        }
        return d;
    }

    Vector3 DirectionOffset(Direction d)
    {
        switch (d)
        {
            case Direction.Top: return Vector3.up * nudgeAmount;
            case Direction.Bottom: return Vector3.down * nudgeAmount;
            case Direction.Left: return Vector3.left * nudgeAmount;
            case Direction.Right: return Vector3.right * nudgeAmount;
        }
        return Vector3.zero;
    }

    void EnqueueAnchors(GameObject piece)
    {
        foreach (Anchor a in piece.GetComponentsInChildren<Anchor>())
        {
            openAnchors.Enqueue(new GenerationAnchor
            {
                position = a.transform.position,
                direction = a.direction,
                anchorRef = a,
                remainingPrefabs = new List<GameObject>(hallwayPrefabs)
            });

            if (!anchorStatus.ContainsKey(a.transform.position))
                anchorStatus[a.transform.position] = AnchorState.Untried;
        }
    }

    bool PrefabHasAnchor(GameObject prefab, Direction dir)
    {
        foreach (Anchor a in prefab.GetComponentsInChildren<Anchor>())
            if (a.direction == dir) return true;
        return false;
    }

    Anchor FindAnchor(GameObject piece, Direction dir)
    {
        foreach (Anchor a in piece.GetComponentsInChildren<Anchor>())
            if (a.direction == dir) return a;
        return null;
    }

    bool AnchorBlocked(Vector3 pos)
    {
        return Physics2D.OverlapCircle(pos, anchorProbeRadius, dungeonLayer);
    }

    bool PrefabOverlaps(GameObject obj)
    {
        foreach (Collider2D c in obj.GetComponentsInChildren<Collider2D>())
        {
            Bounds b = c.bounds;
            Collider2D[] hits = Physics2D.OverlapBoxAll(b.center, b.size, 0f, dungeonLayer);
            foreach (Collider2D hit in hits)
            {
                if (!hit.transform.IsChildOf(obj.transform))
                    return true;
            }
        }
        return false;
    }

    void SetLayerRecursively(GameObject go, int layer)
    {
        go.layer = layer;
        foreach (Transform child in go.transform)
            SetLayerRecursively(child.gameObject, layer);
    }

    void Shuffle<T>(List<T> list)
    {
        int n = list.Count;
        while (n > 1)
        {
            n--;
            int k = rng.Next(n + 1);
            T temp = list[k];
            list[k] = list[n];
            list[n] = temp;
        }
    }

#if UNITY_EDITOR
    void OnDrawGizmos()
    {
        if (anchorStatus != null)
        {
            foreach (var kvp in anchorStatus)
            {
                switch (kvp.Value)
                {
                    case AnchorState.Untried: Gizmos.color = Color.yellow; break;
                    case AnchorState.Success: Gizmos.color = Color.green; break;
                    case AnchorState.Failed: Gizmos.color = Color.red; break;
                }
                Gizmos.DrawSphere(kvp.Key, 0.08f);
            }
        }
    }
#endif
}