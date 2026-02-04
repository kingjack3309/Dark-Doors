using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class DungeonGenerator : MonoBehaviour
{
    [Header("Prefabs")]
    public GameObject startRoomPrefab;
    public List<GameObject> hallwayPrefabs;

    [Header("Generation Settings")]
    public Vector3 startPosition = Vector3.zero;
    public int maxIterations = 50;
    public bool clearOnGenerate = true;
    public bool randomizeAnchorOrder = true;
    public int maxPrefabAttempts = 10;
    [Range(0f, 0.5f)] public float overlapTolerance = 0.05f; // Allow slight touching but not penetrating

    [Header("Random Seed (0 = Random)")]
    public int randomSeed = 0;

    [Header("Debug")]
    public bool generateOnStart = true;
    public bool drawBounds = false;           // Visualize collision bounds in Scene view
    public bool logPlacementAttempts = false; // Log every failed attempt
    public KeyCode regenerateKey = KeyCode.Space;
    public bool logDeadEnds = true;

    private List<Anchor> openAnchors = new List<Anchor>();
    private List<GameObject> placedPieces = new List<GameObject>();
    private int deadEndCount = 0;

    void Start()
    {
        if (generateOnStart)
            Generate();
    }

    void Update()
    {
        if (Input.GetKeyDown(regenerateKey))
        {
            Generate();
        }
    }

    public void Generate()
    {
        if (clearOnGenerate)
            ClearDungeon();

        if (randomSeed != 0)
            Random.InitState(randomSeed);
        else
            Random.InitState(System.Environment.TickCount);

        if (startRoomPrefab == null)
        {
            Debug.LogError("Start Room Prefab is not assigned!");
            return;
        }

        deadEndCount = 0;
        PlaceStartRoom();

        int iterations = 0;
        while (openAnchors.Count > 0 && iterations < maxIterations)
        {
            ProcessNextAnchor();
            iterations++;
        }

        Debug.Log($"Generation complete: {placedPieces.Count} pieces, {deadEndCount} dead ends.");
    }

    void PlaceStartRoom()
    {
        GameObject instance = Instantiate(startRoomPrefab, startPosition, Quaternion.identity, transform);
        instance.name = "START_" + startRoomPrefab.name;
        placedPieces.Add(instance);

        // Ensure physics is updated before we start checking against this
        UpdatePhysicsImmediately(instance);
        RegisterAnchors(instance);
    }

    void ProcessNextAnchor()
    {
        if (openAnchors.Count == 0) return;

        int index = randomizeAnchorOrder ? Random.Range(0, openAnchors.Count) : 0;
        Anchor targetAnchor = openAnchors[index];
        openAnchors.RemoveAt(index);

        if (targetAnchor == null || targetAnchor.isOccupied) return;

        GameObject placedPiece = TryPlaceAnyPrefab(targetAnchor);

        if (placedPiece != null)
        {
            targetAnchor.isOccupied = true;
            placedPiece.transform.SetParent(transform);
            placedPieces.Add(placedPiece);
            UpdatePhysicsImmediately(placedPiece); // Force collider update before next check
            RegisterAnchors(placedPiece);
        }
        else
        {
            targetAnchor.isOccupied = true;
            deadEndCount++;
            if (logDeadEnds)
                Debug.Log($"Dead end at {targetAnchor.transform.position}");
        }
    }

    GameObject TryPlaceAnyPrefab(Anchor targetAnchor)
    {
        List<GameObject> candidates = new List<GameObject>(hallwayPrefabs);
        ShuffleList(candidates);

        int attempts = Mathf.Min(candidates.Count, maxPrefabAttempts);

        for (int i = 0; i < attempts; i++)
        {
            GameObject prefab = candidates[i];
            GameObject result = TryAttachSinglePrefab(prefab, targetAnchor);

            if (result != null)
                return result;
        }

        return null;
    }

    GameObject TryAttachSinglePrefab(GameObject prefab, Anchor targetAnchor)
    {
        GameObject temp = Instantiate(prefab);
        temp.name = "TEMP_" + prefab.name;

        // Deactivate initially to prevent physics issues while positioning
        temp.SetActive(false);

        Anchor[] sourceAnchors = temp.GetComponentsInChildren<Anchor>();

        foreach (var sourceAnchor in sourceAnchors)
        {
            if (!AreOpposite(targetAnchor.direction, sourceAnchor.direction))
                continue;

            // Calculate position to align anchors
            Vector3 offset = targetAnchor.transform.position - sourceAnchor.transform.position;
            temp.transform.position = offset;

            // Activate to ensure colliders are calculated
            temp.SetActive(true);

            // Force physics update
            UpdatePhysicsImmediately(temp);

            if (!WouldOverlap(temp))
            {
                sourceAnchor.isOccupied = true;
                temp.name = prefab.name;
                return temp;
            }

            // Deactivate again to try next anchor position
            temp.SetActive(false);
        }

        DestroyImmediate(temp);
        return null;
    }

    bool WouldOverlap(GameObject candidate)
    {
        // METHOD 1: Physics2D Check (Most accurate for Tilemaps)
        Collider2D[] candidateColliders = candidate.GetComponentsInChildren<Collider2D>();

        if (candidateColliders.Length > 0)
        {
            ContactFilter2D filter = new ContactFilter2D();
            filter.useTriggers = false;
            List<Collider2D> results = new List<Collider2D>();

            foreach (var col in candidateColliders)
            {
                // Skip disabled colliders or triggers
                if (!col.enabled || col.isTrigger) continue;

                // Shrink collider slightly to allow touching walls
                Vector2 size = col.bounds.size;
                size -= Vector2.one * overlapTolerance * 2f;

                if (size.x <= 0 || size.y <= 0) continue; // Too small

                // Check overlap at this position
                int count = Physics2D.OverlapBox(col.bounds.center, size, 0f, filter, results);

                for (int i = 0; i < count; i++)
                {
                    Collider2D hit = results[i];

                    // Ignore if it's part of the candidate itself
                    if (hit.transform.IsChildOf(candidate.transform)) continue;

                    // Check if it belongs to a placed piece
                    foreach (var piece in placedPieces)
                    {
                        if (piece != null && hit.transform.IsChildOf(piece.transform))
                        {
                            if (logPlacementAttempts)
                                Debug.Log($"Overlap detected: {candidate.name} would hit {piece.name} at {hit.bounds.center}");
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        // METHOD 2: Bounds Check (Fallback)
        Bounds candidateBounds = GetObjectBounds(candidate);
        candidateBounds.Expand(-overlapTolerance); // Shrink slightly

        foreach (var piece in placedPieces)
        {
            if (piece == null) continue;

            Bounds existingBounds = GetObjectBounds(piece);
            existingBounds.Expand(-overlapTolerance);

            if (existingBounds.Intersects(candidateBounds))
                return true;
        }

        return false;
    }

    Bounds GetObjectBounds(GameObject obj)
    {
        // Try to calculate from Tilemaps (more accurate than TilemapAnchor)
        Tilemap[] tilemaps = obj.GetComponentsInChildren<Tilemap>();
        if (tilemaps.Length > 0)
        {
            Bounds totalBounds = new Bounds(obj.transform.position, Vector3.zero);
            bool hasBounds = false;

            foreach (var tm in tilemaps)
            {
                tm.CompressBounds();
                Bounds localBounds = tm.localBounds;

                // Convert to world space properly
                Vector3 worldCenter = tm.transform.TransformPoint(localBounds.center);
                Vector3 worldSize = Vector3.Scale(localBounds.size, tm.transform.lossyScale);

                Bounds worldBounds = new Bounds(worldCenter, worldSize);

                if (!hasBounds)
                {
                    totalBounds = worldBounds;
                    hasBounds = true;
                }
                else
                {
                    totalBounds.Encapsulate(worldBounds);
                }
            }

            if (hasBounds) return totalBounds;
        }

        // Fallback to Collider2D
        Collider2D col = obj.GetComponentInChildren<Collider2D>();
        if (col != null)
            return col.bounds;

        return new Bounds(obj.transform.position, Vector3.one);
    }

    void UpdatePhysicsImmediately(GameObject obj)
    {
        // Force Unity to update collider positions immediately (usually waits until end of frame)
        Collider2D[] colliders = obj.GetComponentsInChildren<Collider2D>();
        foreach (var col in colliders)
        {
            if (col != null)
            {
                // Accessing bounds forces an update
                var _ = col.bounds;
            }
        }
    }

    bool AreOpposite(Direction a, Direction b)
    {
        return (a == Direction.Top && b == Direction.Bottom) ||
               (a == Direction.Bottom && b == Direction.Top) ||
               (a == Direction.Left && b == Direction.Right) ||
               (a == Direction.Right && b == Direction.Left);
    }

    void RegisterAnchors(GameObject piece)
    {
        foreach (var anchor in piece.GetComponentsInChildren<Anchor>())
        {
            if (!anchor.isOccupied)
                openAnchors.Add(anchor);
        }
    }

    void ShuffleList<T>(List<T> list)
    {
        int n = list.Count;
        for (int i = n - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }

    public void ClearDungeon()
    {
        foreach (var piece in placedPieces)
        {
            if (piece != null)
                Destroy(piece);
        }
        placedPieces.Clear();
        openAnchors.Clear();
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(startPosition, 0.5f);

        // Visualize open anchors
        Gizmos.color = Color.yellow;
        foreach (var anchor in openAnchors)
        {
            if (anchor != null)
            {
                Vector3 dir = Vector3.zero;
                switch (anchor.direction)
                {
                    case Direction.Top: dir = Vector3.up; break;
                    case Direction.Bottom: dir = Vector3.down; break;
                    case Direction.Left: dir = Vector3.left; break;
                    case Direction.Right: dir = Vector3.right; break;
                }
                Gizmos.DrawRay(anchor.transform.position, dir * 0.3f);
                Gizmos.DrawWireSphere(anchor.transform.position, 0.15f);
            }
        }

        // Visualize bounds of placed pieces (for debugging overlap issues)
        if (drawBounds && Application.isPlaying)
        {
            Gizmos.color = new Color(1, 0, 0, 0.3f);
            foreach (var piece in placedPieces)
            {
                if (piece != null)
                {
                    Bounds b = GetObjectBounds(piece);
                    Gizmos.DrawWireCube(b.center, b.size);
                }
            }
        }
    }
}