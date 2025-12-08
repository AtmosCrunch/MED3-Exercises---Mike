using System.Collections.Generic;
using UnityEngine;

public class RoomManager : MonoBehaviour
{
    [Header("Room Prefabs")]
    public GameObject startRoomPrefab;
    public GameObject[] roomPrefabs;
    public GameObject endRoomPrefab;

    [Header("Level Settings")]
    public int minRoomCount = 3;
    public int maxRoomCount = 8;

    [Header("Props")]
    public GameObject[] propPrefabs;
    public int minPropsPerRoom = 2;
    public int maxPropsPerRoom = 5;
    [Tooltip("Minimum distance between props")]
    public float minPropSpacing = 2f;
    [Tooltip("Distance from walls/edges to avoid placing props")]
    public float wallBuffer = 1.5f;
    [Tooltip("Maximum attempts to find valid prop position")]
    public int maxPropPlacementAttempts = 30;
    [Tooltip("Use physics raycast to check for collisions (requires colliders on props/walls)")]
    public bool usePhysicsCheck = true;
    [Tooltip("Radius for physics overlap check")]
    public float propCheckRadius = 0.5f;
    
    [Header("Prop Physics")]
    [Tooltip("Add rigidbody to props so player can push them")]
    public bool makePropsPhysical = true;
    [Tooltip("Mass of props with rigidbody")]
    public float propMass = 10f;
    [Tooltip("Drag for prop rigidbodies")]
    public float propDrag = 2f;

    [Header("Special Props - Pallets")]
    [Tooltip("Props that are pallets (stackable platforms)")]
    public GameObject[] palletPrefabs;
    [Tooltip("Props that can spawn on top of pallets")]
    public GameObject[] stackablePrefabs;
    [Tooltip("Min/max items per pallet")]
    public int minItemsPerPallet = 1;
    public int maxItemsPerPallet = 3;
    
    [Header("Special Props - Corner/Door Props")]
    [Tooltip("Props that spawn in corners (garbage cans, etc)")]
    public GameObject[] cornerPrefabs;
    [Tooltip("Props that spawn near doors (fire extinguishers, etc)")]
    public GameObject[] doorPrefabs;

    [Header("Player Settings")]
    public GameObject playerPrefab;

    [Header("End Room Object")]
    public GameObject endRoomObjectPrefab;

    [Header("Collision Detection")]
    [Tooltip("Horizontal distance threshold for collision (XZ plane)")]
    public float horizontalCollisionThreshold = 1.5f;
    [Tooltip("Vertical distance threshold for collision (Y axis) - rooms further apart vertically won't collide")]
    public float verticalCollisionThreshold = 3f;

    private List<Room> placedRooms = new List<Room>();
    private List<Transform> usedDoorPoints = new List<Transform>();
    private GameObject playerInstance;
    private GameObject endRoomObjectInstance;

    void Start()
    {
        GenerateLevel();
    }

    public void GenerateLevel()
    {
        ClearLevel();

        // 1. Place start room at origin
        Room startRoom = Instantiate(startRoomPrefab).GetComponent<Room>();
        startRoom.isStartRoom = true;
        startRoom.transform.position = Vector3.zero;
        startRoom.transform.rotation = Quaternion.identity;
        placedRooms.Add(startRoom);

        Transform nextDoor = startRoom.doorPoints[0];
        usedDoorPoints.Add(nextDoor);

        // 2. Calculate intermediate rooms (total - start - end)
        int totalRooms = Random.Range(minRoomCount, maxRoomCount + 1);
        int intermediateRooms = Mathf.Max(1, totalRooms - 2);
        Debug.Log($"Generating level with {totalRooms} total rooms ({intermediateRooms} intermediate)");

        // 3. Place intermediate rooms
        for (int i = 0; i < intermediateRooms; i++)
        {
            Room newRoom = TryPlaceRoom(roomPrefabs, nextDoor);
            
            if (newRoom == null)
            {
                Debug.LogWarning($"Could not place room {i} without collision - ending generation early");
                break;
            }

            placedRooms.Add(newRoom);
            PopulateRoom(newRoom);
            
            Transform usedEntryDoor = FindClosestDoor(newRoom, nextDoor);
            if (usedEntryDoor != null)
            {
                usedDoorPoints.Add(usedEntryDoor);
            }
            usedDoorPoints.Add(nextDoor);

            nextDoor = GetUnusedDoor(newRoom);
            if (nextDoor == null)
            {
                Debug.LogWarning($"Room {i} has no unused doors - ending generation early");
                break;
            }
        }

        // 4. Place end room
        if (nextDoor != null)
        {
            Room endRoom = TryPlaceRoom(new GameObject[] { endRoomPrefab }, nextDoor);
            
            if (endRoom != null)
            {
                endRoom.isEndRoom = true;
                placedRooms.Add(endRoom);
                PopulateRoom(endRoom);
                
                Transform usedEndDoor = FindClosestDoor(endRoom, nextDoor);
                if (usedEndDoor != null)
                {
                    usedDoorPoints.Add(usedEndDoor);
                }
                usedDoorPoints.Add(nextDoor);

                if (endRoomObjectPrefab != null)
                {
                    Vector3 endRoomCenter = endRoom.spawnPoint != null ? endRoom.spawnPoint.position : endRoom.transform.position;
                    endRoomObjectInstance = Instantiate(endRoomObjectPrefab, endRoomCenter, Quaternion.identity);

                    EndRoomInteraction interaction = endRoomObjectInstance.GetComponent<EndRoomInteraction>();
                    if (interaction == null)
                        interaction = endRoomObjectInstance.AddComponent<EndRoomInteraction>();

                    interaction.roomManager = this;
                }
            }
        }

        SpawnPlayerInStartRoom();

        Debug.Log($"Level generated: {placedRooms.Count} rooms total");
    }

    Room TryPlaceRoom(GameObject[] roomOptions, Transform targetDoor)
    {
        if (targetDoor == null || roomOptions == null || roomOptions.Length == 0)
            return null;

        GameObject[] shuffled = ShuffleArray(roomOptions);

        foreach (GameObject roomPrefab in shuffled)
        {
            Room testRoom = Instantiate(roomPrefab).GetComponent<Room>();
            
            if (testRoom == null || testRoom.doorPoints == null || testRoom.doorPoints.Length == 0)
            {
                if (testRoom != null) Destroy(testRoom.gameObject);
                continue;
            }

            Transform entryDoor = testRoom.doorPoints[0];
            ConnectRoomToAdjust(testRoom, entryDoor, targetDoor);

            if (RoomCollidesWithExisting(testRoom))
            {
                Destroy(testRoom.gameObject);
                continue;
            }

            return testRoom;
        }

        return null;
    }

    bool RoomCollidesWithExisting(Room newRoom)
    {
        Transform[] newFloorTiles = GetFloorTiles(newRoom.gameObject);
        
        if (newFloorTiles.Length == 0)
        {
            return FallbackBoundsCheck(newRoom);
        }

        foreach (Room existingRoom in placedRooms)
        {
            Transform[] existingFloorTiles = GetFloorTiles(existingRoom.gameObject);
            
            if (existingFloorTiles.Length == 0) continue;

            foreach (Transform newTile in newFloorTiles)
            {
                Vector3 newTilePos = newTile.position;
                
                foreach (Transform existingTile in existingFloorTiles)
                {
                    Vector3 existingTilePos = existingTile.position;
                    
                    float verticalDistance = Mathf.Abs(newTilePos.y - existingTilePos.y);
                    if (verticalDistance > verticalCollisionThreshold)
                    {
                        continue;
                    }
                    
                    float horizontalDistance = Vector2.Distance(
                        new Vector2(newTilePos.x, newTilePos.z),
                        new Vector2(existingTilePos.x, existingTilePos.z)
                    );

                    if (horizontalDistance < horizontalCollisionThreshold)
                    {
                        return true;
                    }
                }
            }
        }

        return false;
    }

    Transform[] GetFloorTiles(GameObject room)
    {
        List<Transform> floorTiles = new List<Transform>();
        
        Transform[] allChildren = room.GetComponentsInChildren<Transform>();
        foreach (Transform child in allChildren)
        {
            if (child.name.Contains("Floor") || child.name.Contains("floor"))
            {
                floorTiles.Add(child);
            }
        }
        
        return floorTiles.ToArray();
    }

    bool FallbackBoundsCheck(Room newRoom)
    {
        Bounds newBounds = GetRoomBounds(newRoom.gameObject);
        
        foreach (Room existingRoom in placedRooms)
        {
            Bounds existingBounds = GetRoomBounds(existingRoom.gameObject);
            
            if (newBounds.Intersects(existingBounds))
            {
                return true;
            }
        }
        
        return false;
    }

    Bounds GetRoomBounds(GameObject room)
    {
        Renderer[] renderers = room.GetComponentsInChildren<Renderer>();
        
        if (renderers.Length == 0)
        {
            return new Bounds(room.transform.position, Vector3.one * 2f);
        }
        
        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
        {
            bounds.Encapsulate(renderers[i].bounds);
        }
        
        bounds.Expand(1f);
        
        return bounds;
    }

    GameObject[] ShuffleArray(GameObject[] array)
    {
        GameObject[] shuffled = new GameObject[array.Length];
        array.CopyTo(shuffled, 0);

        for (int i = shuffled.Length - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            GameObject temp = shuffled[i];
            shuffled[i] = shuffled[j];
            shuffled[j] = temp;
        }

        return shuffled;
    }

    Transform FindClosestDoor(Room room, Transform target)
    {
        if (room == null || room.doorPoints == null || target == null)
            return null;

        Transform closest = null;
        float minDist = float.MaxValue;

        foreach (Transform door in room.doorPoints)
        {
            if (door == null) continue;

            float dist = Vector3.Distance(door.position, target.position);
            if (dist < minDist)
            {
                minDist = dist;
                closest = door;
            }
        }

        return closest;
    }

    void ConnectRoomToAdjust(Room room, Transform roomDoor, Transform targetDoor)
    {
        Vector3 targetForward = targetDoor.forward;
        Vector3 desiredForward = -targetForward;
        Vector3 roomDoorForward = roomDoor.forward;
        
        Quaternion rotationAdjustment = Quaternion.FromToRotation(roomDoorForward, desiredForward);
        room.transform.rotation = rotationAdjustment * room.transform.rotation;
        
        Vector3 doorOffset = roomDoor.position - room.transform.position;
        room.transform.position = targetDoor.position - doorOffset;
    }

    Transform GetUnusedDoor(Room room)
    {
        if (room == null || room.doorPoints == null) return null;

        foreach (Transform door in room.doorPoints)
        {
            if (door != null && !usedDoorPoints.Contains(door))
                return door;
        }
        return null;
    }

    /// <summary>
    /// Enhanced prop placement with special prop types and floor snapping
    /// </summary>
    void PopulateRoom(Room room)
    {
        if (propPrefabs == null || propPrefabs.Length == 0) return;

        Bounds roomBounds = GetRoomBounds(room.gameObject);
        Vector3 roomCenter = room.transform.position;
        
        float safeWidth = Mathf.Max(2f, roomBounds.size.x - (wallBuffer * 2));
        float safeDepth = Mathf.Max(2f, roomBounds.size.z - (wallBuffer * 2));
        
        int propCount = Random.Range(minPropsPerRoom, maxPropsPerRoom + 1);
        List<Vector3> placedPropPositions = new List<Vector3>();
        List<GameObject> placedPallets = new List<GameObject>();
        
        Debug.Log($"Attempting to place {propCount} props in {room.name}");
        
        // Place regular props and pallets
        for (int i = 0; i < propCount; i++)
        {
            GameObject propPrefab = propPrefabs[Random.Range(0, propPrefabs.Length)];
            bool isPallet = IsPallet(propPrefab);
            
            Vector3 propPosition = Vector3.zero;
            bool validPositionFound = false;
            
            for (int attempt = 0; attempt < maxPropPlacementAttempts; attempt++)
            {
                float randomX = Random.Range(-safeWidth / 2, safeWidth / 2);
                float randomZ = Random.Range(-safeDepth / 2, safeDepth / 2);
                propPosition = roomCenter + new Vector3(randomX, 100f, randomZ); // Start high for raycast
                
                if (IsValidPropPosition(propPosition, placedPropPositions, room))
                {
                    validPositionFound = true;
                    break;
                }
            }
            
            if (validPositionFound)
            {
                // Snap to floor
                Vector3 finalPosition = SnapToFloor(propPosition, room);
                GameObject prop = Instantiate(propPrefab, finalPosition, Quaternion.Euler(0, Random.Range(0f, 360f), 0), room.transform);
                
                // Add physics if enabled
                if (makePropsPhysical)
                {
                    AddPhysicsToProp(prop);
                }
                
                placedPropPositions.Add(finalPosition);
                
                // Track pallets for later stacking
                if (isPallet)
                {
                    placedPallets.Add(prop);
                }
            }
        }
        
        // Stack items on pallets
        if (stackablePrefabs != null && stackablePrefabs.Length > 0 && placedPallets.Count > 0)
        {
            foreach (GameObject pallet in placedPallets)
            {
                PlaceItemsOnPallet(pallet, room);
            }
        }
        
        // Place corner props (garbage cans)
        if (cornerPrefabs != null && cornerPrefabs.Length > 0)
        {
            PlaceCornerProps(room, placedPropPositions);
        }
        
        // Place door props (fire extinguishers)
        if (doorPrefabs != null && doorPrefabs.Length > 0)
        {
            PlaceDoorProps(room, placedPropPositions);
        }
    }

    /// <summary>
    /// Snaps prop position to floor using raycast
    /// </summary>
    Vector3 SnapToFloor(Vector3 position, Room room)
    {
        RaycastHit hit;
        // Raycast downward to find floor
        if (Physics.Raycast(position, Vector3.down, out hit, 200f))
        {
            // Check if we hit a floor
            if (hit.collider.name.Contains("Floor") || hit.collider.name.Contains("floor"))
            {
                return hit.point; // Place exactly on floor
            }
        }
        
        // Fallback: use room Y position
        return new Vector3(position.x, room.transform.position.y, position.z);
    }

    /// <summary>
    /// Adds rigidbody and makes prop pushable
    /// </summary>
    void AddPhysicsToProp(GameObject prop)
    {
        if (prop.GetComponent<Rigidbody>() != null) return; // Already has one
        
        Rigidbody rb = prop.AddComponent<Rigidbody>();
        rb.mass = propMass;
        rb.linearDamping = propDrag;
        rb.angularDamping = 1f;
        
        // Ensure prop has a collider
        if (prop.GetComponent<Collider>() == null)
        {
            BoxCollider col = prop.AddComponent<BoxCollider>();
        }
    }

    /// <summary>
    /// Places stackable items on top of a pallet
    /// </summary>
    void PlaceItemsOnPallet(GameObject pallet, Room room)
    {
        if (stackablePrefabs == null || stackablePrefabs.Length == 0) return;
        
        // Get pallet top position
        Bounds palletBounds = GetObjectBounds(pallet);
        Vector3 palletTop = new Vector3(palletBounds.center.x, palletBounds.max.y + 0.1f, palletBounds.center.z);
        
        int itemCount = Random.Range(minItemsPerPallet, maxItemsPerPallet + 1);
        
        for (int i = 0; i < itemCount; i++)
        {
            GameObject itemPrefab = stackablePrefabs[Random.Range(0, stackablePrefabs.Length)];
            
            // Slight random offset on pallet surface
            Vector3 offset = new Vector3(
                Random.Range(-0.3f, 0.3f), 
                i * 0.5f, // Stack vertically
                Random.Range(-0.3f, 0.3f)
            );
            
            GameObject item = Instantiate(itemPrefab, palletTop + offset, Quaternion.Euler(0, Random.Range(0f, 360f), 0), pallet.transform);
            
            if (makePropsPhysical)
            {
                AddPhysicsToProp(item);
            }
        }
        
        Debug.Log($"  Placed {itemCount} items on pallet");
    }

    /// <summary>
    /// Places props in corners of the room
    /// </summary>
    void PlaceCornerProps(Room room, List<Vector3> placedPropPositions)
    {
        Bounds roomBounds = GetRoomBounds(room.gameObject);
        Vector3 roomCenter = room.transform.position;
        
        // Calculate approximate corners
        Vector3[] corners = new Vector3[]
        {
            roomCenter + new Vector3(-roomBounds.extents.x + wallBuffer, 100f, -roomBounds.extents.z + wallBuffer),
            roomCenter + new Vector3(roomBounds.extents.x - wallBuffer, 100f, -roomBounds.extents.z + wallBuffer),
            roomCenter + new Vector3(-roomBounds.extents.x + wallBuffer, 100f, roomBounds.extents.z - wallBuffer),
            roomCenter + new Vector3(roomBounds.extents.x - wallBuffer, 100f, roomBounds.extents.z - wallBuffer)
        };
        
        // Place 1-2 corner props
        int cornerPropsToPlace = Random.Range(1, 3);
        List<int> usedCorners = new List<int>();
        
        for (int i = 0; i < cornerPropsToPlace; i++)
        {
            // Pick random unused corner
            int cornerIndex = Random.Range(0, corners.Length);
            int attempts = 0;
            while (usedCorners.Contains(cornerIndex) && attempts < 10)
            {
                cornerIndex = Random.Range(0, corners.Length);
                attempts++;
            }
            
            if (usedCorners.Contains(cornerIndex)) continue;
            usedCorners.Add(cornerIndex);
            
            Vector3 cornerPos = corners[cornerIndex];
            
            // Check if valid
            if (IsValidPropPosition(cornerPos, placedPropPositions, room))
            {
                Vector3 finalPos = SnapToFloor(cornerPos, room);
                GameObject cornerProp = cornerPrefabs[Random.Range(0, cornerPrefabs.Length)];
                GameObject prop = Instantiate(cornerProp, finalPos, Quaternion.identity, room.transform);
                
                if (makePropsPhysical)
                {
                    AddPhysicsToProp(prop);
                }
                
                placedPropPositions.Add(finalPos);
                Debug.Log($"  Placed corner prop at corner {cornerIndex}");
            }
        }
    }

    /// <summary>
    /// Places props near doors (fire extinguishers, etc)
    /// </summary>
    void PlaceDoorProps(Room room, List<Vector3> placedPropPositions)
    {
        if (room.doorPoints == null || room.doorPoints.Length == 0) return;
        
        foreach (Transform door in room.doorPoints)
        {
            if (door == null) continue;
            
            // 50% chance to place prop near each door
            if (Random.value < 0.5f) continue;
            
            // Position to the side of the door
            Vector3 doorRight = door.right;
            float side = Random.value < 0.5f ? -1f : 1f; // Left or right of door
            
            Vector3 propPos = door.position + (doorRight * side * 1.5f) + Vector3.up * 100f;
            
            // Check if valid
            if (IsValidPropPosition(propPos, placedPropPositions, room))
            {
                Vector3 finalPos = SnapToFloor(propPos, room);
                GameObject doorProp = doorPrefabs[Random.Range(0, doorPrefabs.Length)];
                GameObject prop = Instantiate(doorProp, finalPos, Quaternion.identity, room.transform);
                
                if (makePropsPhysical)
                {
                    AddPhysicsToProp(prop);
                }
                
                placedPropPositions.Add(finalPos);
                Debug.Log($"  Placed door prop near door");
            }
        }
    }

    /// <summary>
    /// Checks if prefab is a pallet
    /// </summary>
    bool IsPallet(GameObject prefab)
    {
        if (palletPrefabs == null || palletPrefabs.Length == 0) return false;
        
        foreach (GameObject palletPrefab in palletPrefabs)
        {
            if (prefab == palletPrefab) return true;
        }
        
        return false;
    }

    /// <summary>
    /// Gets bounds of a single object
    /// </summary>
    Bounds GetObjectBounds(GameObject obj)
    {
        Renderer[] renderers = obj.GetComponentsInChildren<Renderer>();
        
        if (renderers.Length == 0)
        {
            return new Bounds(obj.transform.position, Vector3.one);
        }
        
        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
        {
            bounds.Encapsulate(renderers[i].bounds);
        }
        
        return bounds;
    }

    bool IsValidPropPosition(Vector3 position, List<Vector3> existingProps, Room room)
    {
        // Check distance from other props
        foreach (Vector3 existingProp in existingProps)
        {
            float distance = Vector3.Distance(position, existingProp);
            if (distance < minPropSpacing)
            {
                return false;
            }
        }
        
        // Check distance from doors
        if (room.doorPoints != null)
        {
            foreach (Transform door in room.doorPoints)
            {
                if (door == null) continue;
                
                float doorDistance = Vector3.Distance(position, door.position);
                if (doorDistance < wallBuffer * 2)
                {
                    return false;
                }
            }
        }
        
        // Optional physics check
        if (usePhysicsCheck)
        {
            Collider[] colliders = Physics.OverlapSphere(position, propCheckRadius);
            foreach (Collider col in colliders)
            {
                if (col.transform.IsChildOf(room.transform) && 
                    (col.name.Contains("Wall") || col.name.Contains("wall")))
                {
                    return false;
                }
            }
        }
        
        return true;
    }

    void SpawnPlayerInStartRoom()
    {
        Room startRoom = placedRooms.Find(r => r.isStartRoom);
        if (startRoom == null) return;

        Vector3 spawnPos = startRoom.spawnPoint != null
            ? startRoom.spawnPoint.position
            : startRoom.transform.position + Vector3.up * 1f;

        playerInstance = Instantiate(playerPrefab, spawnPos, Quaternion.identity);
        playerInstance.name = "Player";
        playerInstance.tag = "Player";
    }

    void ClearLevel()
    {
        foreach (Room room in placedRooms)
        {
            if (room != null) Destroy(room.gameObject);
        }
        placedRooms.Clear();
        usedDoorPoints.Clear();

        if (playerInstance != null) Destroy(playerInstance);
        if (endRoomObjectInstance != null) Destroy(endRoomObjectInstance);
    }

    void OnDrawGizmos()
    {
        if (!Application.isPlaying) return;

        foreach (Room room in placedRooms)
        {
            if (room == null || room.doorPoints == null) continue;

            foreach (Transform door in room.doorPoints)
            {
                if (door == null) continue;

                Gizmos.color = usedDoorPoints.Contains(door) ? Color.green : Color.red;
                Gizmos.DrawSphere(door.position, 0.3f);
                
                Gizmos.color = Color.blue;
                Gizmos.DrawLine(door.position, door.position + door.forward * 1f);
            }

            Bounds roomBounds = GetRoomBounds(room.gameObject);
            Gizmos.color = new Color(0, 1, 1, 0.3f);
            Gizmos.DrawWireCube(roomBounds.center, roomBounds.size);
            
            float safeWidth = Mathf.Max(2f, roomBounds.size.x - (wallBuffer * 2));
            float safeDepth = Mathf.Max(2f, roomBounds.size.z - (wallBuffer * 2));
            Gizmos.color = new Color(1, 1, 0, 0.2f);
            Gizmos.DrawWireCube(room.transform.position, new Vector3(safeWidth, 1f, safeDepth));
        }
    }
}