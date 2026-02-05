using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class DungeonGenerator : MonoBehaviour
{
    [Header("Prefabs")]
    public GameObject startRoomPrefab;
    public List<GameObject> hallwayPrefabs;
    public List<GameObject> roomPrefabs; // NEW: Special rooms that can't connect to each other
    public GameObject endRoomPrefab; // NEW: Must have an anchor on bottom (Direction.Bottom)

    [Header("End Caps (Dead End Fillers)")]
    public GameObject endCapTop; // For Direction.Top openings
    public GameObject endCapBottom; // For Direction.Bottom openings
    public GameObject endCapLeft; // For Direction.Left openings
    public GameObject endCapRight; // For Direction.Right openings

    [Header("Generation Settings")]
    public Vector3 startPosition = Vector3.zero;
    public int maxIterations = 50;
    public int roomSpawnChance = 20; // NEW: Percent chance to try a room instead of hallway
    public bool clearOnGenerate = true;
    public bool randomizeAnchorOrder = true;
    public int maxPrefabAttempts = 10;
    [Range(0f, 0.5f)]
    public float overlapTolerance = 0.05f;
    [Range(0.01f, 1f)] // NEW: Radius for checking overlapping anchors
    public float anchorOverlapCheckRadius = 0.1f;

    [Header("Tags")]
    public string roomAnchorTag = "RoomAnchor"; // NEW: Tag on room entrance anchors
    public string hallwayAnchorTag = "HallwayAnchor"; // NEW: Tag on hallway anchors

    [Header("Random Seed (0 = Random)")]
    public int randomSeed = 0;

    [Header("Debug")]
    public bool generateOnStart = true;
    public bool drawBounds = false;
    public bool logPlacementAttempts = false;
    public KeyCode regenerateKey = KeyCode.Space;
    public bool logDeadEnds = true;

    private List<Anchor> openAnchors = new List<Anchor>();
    private List<GameObject> placedPieces = new List<GameObject>();
    private int deadEndCount = 0;

    void Start()
    {
        if (generateOnStart) Generate();
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
        if (clearOnGenerate) ClearDungeon();

        if (randomSeed != 0) Random.InitState(randomSeed);
        else Random.InitState(System.Environment.TickCount);

        if (startRoomPrefab == null)
        {
            Debug.LogError("Start Room Prefab is not assigned!");
            return;
        }

        deadEndCount = 0;
        PlaceStartRoom();

        // PHASE 1: Generate main dungeon
        int iterations = 0;
        while (openAnchors.Count > 0 && iterations < maxIterations)
        {
            ProcessNextAnchor();
            iterations++;
        }

        // PHASE 2: Place end room on a remaining anchor
        PlaceEndRoom();

        // PHASE 3: Cap all remaining dead ends (with anchor overlap check)
        CapAllDeadEnds();

        Debug.Log($"Generation complete: {placedPieces.Count} pieces, {deadEndCount} dead ends capped.");
    }

    void PlaceStartRoom()
    {
        GameObject instance = Instantiate(startRoomPrefab, startPosition, Quaternion.identity, transform);
        instance.name = "START_" + startRoomPrefab.name;
        placedPieces.Add(instance);
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

        // NEW: Check what type of piece we're attaching TO
        string targetTag = targetAnchor.gameObject.tag;
        bool connectingToRoom = (targetTag == roomAnchorTag);
        bool connectingToHallway = (targetTag == hallwayAnchorTag || string.IsNullOrEmpty(targetTag));

        GameObject placedPiece = null;

        if (connectingToRoom)
        {
            // NEW: Rooms can only connect to hallways, not other rooms
            placedPiece = TryPlaceAnyPrefab(targetAnchor, hallwayPrefabs);
        }
        else if (connectingToHallway)
        {
            // NEW: Can be hallway or room (based on chance)
            bool tryRoom = (roomPrefabs.Count > 0 && Random.Range(0, 100) < roomSpawnChance);
            if (tryRoom)
            {
                // MODIFIED: Try room first, if it overlaps, destroy it and try hallway
                placedPiece = TryPlaceAnyPrefab(targetAnchor, roomPrefabs);
                if (placedPiece == null)
                {
                    if (logPlacementAttempts) Debug.Log($"Room placement failed at {targetAnchor.transform.position}, trying hallway");
                    placedPiece = TryPlaceAnyPrefab(targetAnchor, hallwayPrefabs);
                }
            }
            else
            {
                placedPiece = TryPlaceAnyPrefab(targetAnchor, hallwayPrefabs);
            }
        }

        if (placedPiece != null)
        {
            targetAnchor.isOccupied = true;
            placedPiece.transform.SetParent(transform);
            placedPieces.Add(placedPiece);
            UpdatePhysicsImmediately(placedPiece);
            RegisterAnchors(placedPiece);
        }
        else
        {
            // NEW: Don't mark as occupied - leave for end room or capping
            if (!targetAnchor.isOccupied) openAnchors.Add(targetAnchor);
        }
    }

    // NEW: Overload that accepts specific prefab list
    GameObject TryPlaceAnyPrefab(Anchor targetAnchor, List<GameObject> prefabList)
    {
        if (prefabList == null || prefabList.Count == 0) return null;

        List<GameObject> candidates = new List<GameObject>(prefabList);
        ShuffleList(candidates);

        int attempts = Mathf.Min(candidates.Count, maxPrefabAttempts);
        for (int i = 0; i < attempts; i++)
        {
            GameObject prefab = candidates[i];
            GameObject result = TryAttachSinglePrefab(prefab, targetAnchor);
            if (result != null) return result;
        }
        return null;
    }

    GameObject TryPlaceAnyPrefab(Anchor targetAnchor)
    {
        return TryPlaceAnyPrefab(targetAnchor, hallwayPrefabs);
    }

    GameObject TryAttachSinglePrefab(GameObject prefab, Anchor targetAnchor)
    {
        GameObject temp = Instantiate(prefab);
        temp.name = "TEMP_" + prefab.name;
        temp.SetActive(false);

        Anchor[] sourceAnchors = temp.GetComponentsInChildren<Anchor>();

        foreach (var sourceAnchor in sourceAnchors)
        {
            if (!AreOpposite(targetAnchor.direction, sourceAnchor.direction))
                continue;

            Vector3 offset = targetAnchor.transform.position - sourceAnchor.transform.position;
            temp.transform.position = offset;
            temp.SetActive(true);
            UpdatePhysicsImmediately(temp);

            // MODIFIED: More robust overlap check with immediate cleanup
            if (!WouldOverlap(temp))
            {
                sourceAnchor.isOccupied = true;
                temp.name = prefab.name;
                return temp;
            }

            // MODIFIED: If overlap detected, immediately destroy and return null
            // This ensures no lingering temp objects
            temp.SetActive(false);
            DestroyImmediate(temp);
            return null;
        }

        DestroyImmediate(temp);
        return null;
    }

    // NEW: Place end room on a remaining anchor
    void PlaceEndRoom()
    {
        if (endRoomPrefab == null || openAnchors.Count == 0)
        {
            if (endRoomPrefab == null) Debug.LogWarning("No end room prefab assigned");
            return;
        }

        List<Anchor> candidates = new List<Anchor>(openAnchors);
        ShuffleList(candidates);

        foreach (var anchor in candidates)
        {
            if (anchor.isOccupied) continue;

            GameObject result = TryAttachEndRoom(anchor);
            if (result != null)
            {
                anchor.isOccupied = true;
                result.transform.SetParent(transform);
                result.name = "END_" + endRoomPrefab.name;
                placedPieces.Add(result);
                UpdatePhysicsImmediately(result);
                Debug.Log($"Placed end room at {anchor.transform.position}");
                openAnchors.Remove(anchor);
                return;
            }
        }

        Debug.LogWarning("Could not place end room - no valid anchor found");
    }

    // NEW: Special attachment for end room (requires bottom anchor)
    GameObject TryAttachEndRoom(Anchor targetAnchor)
    {
        GameObject temp = Instantiate(endRoomPrefab);
        temp.name = "TEMP_ENDROOM";
        temp.SetActive(false);

        Anchor[] sourceAnchors = temp.GetComponentsInChildren<Anchor>();

        foreach (var sourceAnchor in sourceAnchors)
        {
            if (sourceAnchor.direction != Direction.Bottom) continue;
            if (targetAnchor.direction != Direction.Top) continue;

            Vector3 offset = targetAnchor.transform.position - sourceAnchor.transform.position;
            temp.transform.position = offset;
            temp.SetActive(true);
            UpdatePhysicsImmediately(temp);

            if (!WouldOverlap(temp))
            {
                sourceAnchor.isOccupied = true;
                return temp;
            }

            temp.SetActive(false);
            DestroyImmediate(temp);
            return null;
        }

        DestroyImmediate(temp);
        return null;
    }

    // MODIFIED: Check for anchor overlaps before capping dead ends
    void CapAllDeadEnds()
    {
        int cappedCount = 0;
        List<Anchor> anchorsToRemove = new List<Anchor>();

        foreach (var anchor in openAnchors.ToArray()) // Use ToArray to safely modify list during iteration
        {
            if (anchor == null || anchor.isOccupied) continue;

            // NEW: Check if this anchor overlaps with any other anchor (RoomAnchor or HallwayAnchor)
            // This indicates a connection that wasn't properly marked
            if (CheckAnchorOverlapWithOtherAnchors(anchor))
            {
                // This anchor is actually connected, mark it as occupied
                anchor.isOccupied = true;
                if (logDeadEnds) Debug.Log($"Anchor at {anchor.transform.position} overlaps with another anchor - marking as connected");
                continue;
            }

            // No overlap found, this is a true dead end - cap it
            GameObject capPrefab = GetEndCapForDirection(anchor.direction);
            if (capPrefab == null) continue;

            GameObject cap = Instantiate(capPrefab, anchor.transform.position, Quaternion.identity, anchor.transform.parent);
            cap.name = "CAP_" + capPrefab.name;
            anchor.isOccupied = true;
            cappedCount++;
            deadEndCount++;
        }

        openAnchors.RemoveAll(a => a == null || a.isOccupied);

        if (cappedCount > 0) Debug.Log($"Capped {cappedCount} dead ends");
    }

    // NEW: Check if an anchor overlaps with any other placed anchor
    bool CheckAnchorOverlapWithOtherAnchors(Anchor anchor)
    {
        Vector3 anchorPos = anchor.transform.position;

        // Check against all placed pieces' anchors
        foreach (var piece in placedPieces)
        {
            if (piece == null) continue;

            Anchor[] pieceAnchors = piece.GetComponentsInChildren<Anchor>();
            foreach (var otherAnchor in pieceAnchors)
            {
                // Skip if it's the same anchor or same parent piece
                if (otherAnchor == anchor) continue;
                if (otherAnchor.transform.IsChildOf(anchor.transform.parent)) continue;

                // Check if tags indicate it's a valid anchor type
                string otherTag = otherAnchor.gameObject.tag;
                bool isValidAnchorType = (otherTag == roomAnchorTag || otherTag == hallwayAnchorTag);

                if (!isValidAnchorType) continue;

                // Check distance overlap
                float distance = Vector3.Distance(anchorPos, otherAnchor.transform.position);
                if (distance <= anchorOverlapCheckRadius)
                {
                    if (logDeadEnds) Debug.Log($"Anchor overlap detected: {anchor.name} at {anchorPos} overlaps with {otherAnchor.name} at {otherAnchor.transform.position} (distance: {distance})");

                    // Mark both as occupied since they're connected
                    otherAnchor.isOccupied = true;
                    return true;
                }
            }
        }

        return false;
    }

    // NEW: Get correct end cap based on direction
    GameObject GetEndCapForDirection(Direction dir)
    {
        switch (dir)
        {
            case Direction.Top: return endCapTop;
            case Direction.Bottom: return endCapBottom;
            case Direction.Left: return endCapLeft;
            case Direction.Right: return endCapRight;
            default: return null;
        }
    }

    // MODIFIED: Improved overlap detection for better accuracy with large rooms
    bool WouldOverlap(GameObject candidate)
    {
        // Ensure physics is up to date
        UpdatePhysicsImmediately(candidate);

        Collider2D[] candidateColliders = candidate.GetComponentsInChildren<Collider2D>();

        if (candidateColliders.Length > 0)
        {
            ContactFilter2D filter = new ContactFilter2D();
            filter.useTriggers = false;
            filter.useLayerMask = false;

            List<Collider2D> results = new List<Collider2D>();

            foreach (var col in candidateColliders)
            {
                if (!col.enabled || col.isTrigger) continue;

                // MODIFIED: Use slightly smaller bounds for more accurate detection
                Vector2 size = col.bounds.size;
                Vector2 shrinkAmount = Vector2.one * overlapTolerance * 2f;
                size -= shrinkAmount;

                if (size.x <= 0 || size.y <= 0) continue;

                // Use OverlapBox for precise collision check
                int count = Physics2D.OverlapBox(col.bounds.center, size, 0f, filter, results);

                for (int i = 0; i < count; i++)
                {
                    Collider2D hit = results[i];

                    // Skip if it's part of the candidate itself
                    if (hit.transform.IsChildOf(candidate.transform)) continue;

                    // Check if it hits any already placed piece
                    foreach (var piece in placedPieces)
                    {
                        if (piece != null && hit.transform.IsChildOf(piece.transform))
                        {
                            if (logPlacementAttempts)
                                Debug.Log($"Overlap detected: {candidate.name} would hit {piece.name} at {hit.transform.position}");
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        // Fallback to bounds-based check if no colliders
        Bounds candidateBounds = GetObjectBounds(candidate);

        // MODIFIED: Expand bounds slightly inward for tolerance, not outward
        if (overlapTolerance > 0)
        {
            Vector3 shrink = Vector3.one * overlapTolerance * 2f;
            candidateBounds.size = Vector3.Max(candidateBounds.size - shrink, Vector3.zero);
        }

        foreach (var piece in placedPieces)
        {
            if (piece == null) continue;

            Bounds existingBounds = GetObjectBounds(piece);

            // Same shrink for existing
            if (overlapTolerance > 0)
            {
                Vector3 shrink = Vector3.one * overlapTolerance * 2f;
                existingBounds.size = Vector3.Max(existingBounds.size - shrink, Vector3.zero);
            }

            // MODIFIED: Use Intersects with a small epsilon for floating point errors
            if (existingBounds.Intersects(candidateBounds))
            {
                // Additional check: ensure it's not just touching at edges
                float epsilon = 0.001f;
                Bounds expandedCandidate = candidateBounds;
                expandedCandidate.Expand(epsilon);

                if (existingBounds.Intersects(expandedCandidate))
                {
                    if (logPlacementAttempts)
                        Debug.Log($"Bounds overlap: {candidate.name} intersects with {piece.name}");
                    return true;
                }
            }
        }

        return false;
    }

    Bounds GetObjectBounds(GameObject obj)
    {
        Tilemap[] tilemaps = obj.GetComponentsInChildren<Tilemap>();
        if (tilemaps.Length > 0)
        {
            Bounds totalBounds = new Bounds(obj.transform.position, Vector3.zero);
            bool hasBounds = false;

            foreach (var tm in tilemaps)
            {
                tm.CompressBounds();
                Bounds localBounds = tm.localBounds;
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

        Collider2D col = obj.GetComponentInChildren<Collider2D>();
        if (col != null) return col.bounds;

        return new Bounds(obj.transform.position, Vector3.one);
    }

    void UpdatePhysicsImmediately(GameObject obj)
    {
        Collider2D[] colliders = obj.GetComponentsInChildren<Collider2D>();
        foreach (var col in colliders)
        {
            if (col != null)
            {
                // Force bounds update
                var _ = col.bounds;
            }
        }

        // MODIFIED: Also update any rigidbodies to ensure physics sync
        Rigidbody2D[] rbs = obj.GetComponentsInChildren<Rigidbody2D>();
        foreach (var rb in rbs)
        {
            if (rb != null && rb.bodyType == RigidbodyType2D.Dynamic)
            {
                rb.position = rb.position; // Force sync
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
            if (!anchor.isOccupied) openAnchors.Add(anchor);
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
            if (piece != null) Destroy(piece);
        }
        placedPieces.Clear();
        openAnchors.Clear();
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(startPosition, 0.5f);

        // NEW: Color code anchors by tag
        foreach (var anchor in openAnchors)
        {
            if (anchor == null) continue;

            if (anchor.gameObject.tag == roomAnchorTag)
                Gizmos.color = Color.magenta;
            else if (anchor.gameObject.tag == hallwayAnchorTag)
                Gizmos.color = Color.cyan;
            else
                Gizmos.color = Color.yellow;

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

            // NEW: Draw overlap check radius for dead end anchors
            if (!anchor.isOccupied)
            {
                Gizmos.color = new Color(Gizmos.color.r, Gizmos.color.g, Gizmos.color.b, 0.3f);
                Gizmos.DrawWireSphere(anchor.transform.position, anchorOverlapCheckRadius);
            }
        }

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