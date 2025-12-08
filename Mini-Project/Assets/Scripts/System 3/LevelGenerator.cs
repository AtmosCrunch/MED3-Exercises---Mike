using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class LevelGenerator : MonoBehaviour
{
    [System.Serializable]
    public class RoomPrefab
    {
        public GameObject prefab;
        public bool isStartRoom;
        public bool isEndRoom;
    }

    [Header("Room Prefabs")]
    public List<RoomPrefab> roomPrefabs = new List<RoomPrefab>();
    
    [Header("Generation Settings")]
    public int minRooms = 5;
    public int maxRooms = 10;
    public int maxAttempts = 100;
    
    [Header("Debug")]
    public bool showDebugGizmos = true;
    public bool generateOnStart = true;

    private List<RoomInfo> placedRooms = new List<RoomInfo>();
    private List<Door> availableDoors = new List<Door>();
    private int roomsPlaced = 0;
    private int targetRoomCount;
    
    // Public properties for checking level state
    public bool HasStartRoom { get; private set; }
    public bool HasEndRoom { get; private set; }
    public bool IsLevelValid => HasStartRoom && HasEndRoom && roomsPlaced >= 2;

    void Start()
    {
        if (generateOnStart)
        {
            GenerateLevel();
        }
    }

    public void GenerateLevel()
    {
        ClearLevel();
        targetRoomCount = Random.Range(minRooms, maxRooms + 1);
        
        Debug.Log($"=== Starting Level Generation ===");
        Debug.Log($"Target room count: {targetRoomCount}");
        
        // Place start room
        GameObject startPrefab = roomPrefabs.First(r => r.isStartRoom).prefab;
        GameObject startRoomObj = Instantiate(startPrefab, Vector3.zero, Quaternion.identity, transform);
        RoomInfo startRoom = startRoomObj.GetComponent<RoomInfo>();
        
        if (startRoom == null)
        {
            Debug.LogError("Start room prefab missing RoomInfo component!");
            return;
        }

        placedRooms.Add(startRoom);
        roomsPlaced++;
        
        // Add start room's doors to available doors
        Door[] startDoors = startRoom.GetComponentsInChildren<Door>();
        Debug.Log($"Start room has {startDoors.Length} doors");
        
        foreach (Door door in startDoors)
        {
            if (!door.isConnected)
            {
                availableDoors.Add(door);
                Debug.Log($"Added door at position {door.transform.position} to available doors");
            }
        }

        Debug.Log($"Available doors after start room: {availableDoors.Count}");

        // Generate rooms until we reach target count
        int attempts = 0;
        while (roomsPlaced < targetRoomCount - 1 && attempts < maxAttempts)
        {
            attempts++;
            
            // Always keep at least 1 door available for the end room
            if (availableDoors.Count <= 1)
            {
                Debug.Log("Only 1 door left, reserving it for end room");
                break;
            }

            Debug.Log($"--- Attempt {attempts}: Trying to place room {roomsPlaced + 1}/{targetRoomCount - 1} ---");
            
            // Pick a random available door
            Door doorToConnect = availableDoors[Random.Range(0, availableDoors.Count)];
            Debug.Log($"Connecting to door at position {doorToConnect.transform.position}");
            
            // Try to place a room
            if (TryPlaceRoom(doorToConnect, false))
            {
                attempts = 0; // Reset attempts on success
                Debug.Log($"Room placement successful! Available doors: {availableDoors.Count}");
            }
            else
            {
                Debug.LogWarning($"Room placement failed. Available doors: {availableDoors.Count}");
            }
        }

        if (attempts >= maxAttempts)
        {
            Debug.LogWarning($"Reached max attempts ({maxAttempts}) without placing all rooms");
        }

        // Place end room - GUARANTEED
        if (availableDoors.Count > 0)
        {
            Debug.Log($"Placing end room. Available doors: {availableDoors.Count}");
            Door finalDoor = availableDoors[Random.Range(0, availableDoors.Count)];
            
            // Try multiple times to place end room if necessary
            int endRoomAttempts = 0;
            while (endRoomAttempts < maxAttempts && availableDoors.Count > 0)
            {
                if (TryPlaceRoom(finalDoor, true))
                {
                    Debug.Log("End room placed successfully!");
                    break;
                }
                else
                {
                    // If this door didn't work, remove it and try another
                    availableDoors.Remove(finalDoor);
                    if (availableDoors.Count > 0)
                    {
                        finalDoor = availableDoors[Random.Range(0, availableDoors.Count)];
                        endRoomAttempts++;
                        Debug.LogWarning($"End room placement failed, trying different door (attempt {endRoomAttempts})");
                    }
                }
            }
            
            if (endRoomAttempts >= maxAttempts || availableDoors.Count == 0)
            {
                Debug.LogError("CRITICAL: Failed to place end room after all attempts!");
            }
        }
        else
        {
            Debug.LogError("CRITICAL: No available doors for end room!");
        }

        Debug.Log($"=== Level generation complete with {roomsPlaced} rooms (including start and end) ===");
        
        // Validate that we have start and end rooms
        ValidateLevel();
    }
    
    void ValidateLevel()
    {
        HasStartRoom = false;
        HasEndRoom = false;
        
        foreach (RoomInfo room in placedRooms)
        {
            if (room == null || room.gameObject == null) continue;
            
            // Get the room's name without the "(Clone)" suffix
            string roomName = room.gameObject.name.Replace("(Clone)", "").Trim();
            
            // Check if this room's prefab is marked as start or end
            foreach (RoomPrefab rp in roomPrefabs)
            {
                if (rp == null || rp.prefab == null) continue;
                
                if (rp.prefab.name == roomName)
                {
                    if (rp.isStartRoom) HasStartRoom = true;
                    if (rp.isEndRoom) HasEndRoom = true;
                }
            }
        }
        
        if (!HasStartRoom)
        {
            Debug.LogError("VALIDATION FAILED: No start room in level!");
        }
        else
        {
            Debug.Log("✓ Start room present");
        }
        
        if (!HasEndRoom)
        {
            Debug.LogError("VALIDATION FAILED: No end room in level!");
        }
        else
        {
            Debug.Log("✓ End room present");
        }
        
        if (IsLevelValid)
        {
            Debug.Log("✓✓ Level validation passed: Start and End rooms confirmed");
        }
    }

    bool TryPlaceRoom(Door existingDoor, bool placeEndRoom)
    {
        // Get appropriate room prefab
        List<RoomPrefab> validPrefabs = roomPrefabs.Where(r => 
            placeEndRoom ? r.isEndRoom : !r.isStartRoom && !r.isEndRoom
        ).ToList();

        if (validPrefabs.Count == 0)
        {
            Debug.LogError(placeEndRoom ? "No end room prefabs found!" : "No regular room prefabs found!");
            return false;
        }

        GameObject selectedPrefab = validPrefabs[Random.Range(0, validPrefabs.Count)].prefab;
        
        if (selectedPrefab == null)
        {
            Debug.LogError("Selected prefab is null!");
            return false;
        }
        
        // Get doors from the prefab
        Door[] prefabDoors = selectedPrefab.GetComponentsInChildren<Door>(true);
        
        if (prefabDoors == null || prefabDoors.Length == 0)
        {
            Debug.LogError($"Room prefab '{selectedPrefab.name}' has no Door components!");
            return false;
        }

        Debug.Log($"Trying to place {(placeEndRoom ? "end" : "regular")} room '{selectedPrefab.name}' with {prefabDoors.Length} doors");

        // Try each door in the new room
        List<Door> doorList = prefabDoors.ToList();
        
        // Shuffle doors for randomness
        for (int i = 0; i < doorList.Count; i++)
        {
            int randomIndex = Random.Range(i, doorList.Count);
            Door temp = doorList[i];
            doorList[i] = doorList[randomIndex];
            doorList[randomIndex] = temp;
        }

        foreach (Door prefabDoor in doorList)
        {
            // Calculate position and rotation for the new room
            Vector3 position;
            Quaternion rotation;
            CalculateRoomTransform(existingDoor, prefabDoor, out position, out rotation);

            // Check if room overlaps with existing rooms
            if (!CheckOverlap(position, rotation, selectedPrefab))
            {
                // Place the room
                GameObject newRoomObj = Instantiate(selectedPrefab, position, rotation, transform);
                RoomInfo newRoom = newRoomObj.GetComponent<RoomInfo>();
                
                if (newRoom == null)
                {
                    Debug.LogError("Instantiated room missing RoomInfo component!");
                    Destroy(newRoomObj);
                    return false;
                }
                
                // Find the door that's now at the connection point
                Door connectedDoor = null;
                float closestDistance = float.MaxValue;
                
                Door[] instantiatedDoors = newRoom.GetComponentsInChildren<Door>();
                foreach (Door door in instantiatedDoors)
                {
                    float distance = Vector3.Distance(door.GetConnectionPoint(), existingDoor.GetConnectionPoint());
                    if (distance < closestDistance)
                    {
                        closestDistance = distance;
                        connectedDoor = door;
                    }
                }
                
                if (connectedDoor != null && closestDistance < 2f) // Increased from 1f to 2f to be more lenient
                {
                    // Connect the doors
                    existingDoor.Connect(connectedDoor);
                    
                    placedRooms.Add(newRoom);
                    roomsPlaced++;
                    
                    // Remove the connected door from available doors
                    availableDoors.Remove(existingDoor);
                    
                    // Add new room's unconnected doors to available doors (except for end room)
                    if (!placeEndRoom)
                    {
                        Door[] roomDoors = newRoom.GetComponentsInChildren<Door>();
                        foreach (Door door in roomDoors)
                        {
                            if (!door.isConnected)
                                availableDoors.Add(door);
                        }
                    }
                    
                    Debug.Log($"Successfully placed room at {position}. Total rooms: {roomsPlaced}. Door distance: {closestDistance:F2}");
                    return true;
                }
                else
                {
                    Debug.LogWarning($"Could not find matching door. Closest distance: {closestDistance:F2} (threshold is 2.0). Destroying and trying next door orientation.");
                    Destroy(newRoomObj);
                }
            }
            else
            {
                Debug.Log($"Room would overlap at position {position}, trying next door orientation...");
            }
        }

        Debug.LogWarning($"Failed to place room '{selectedPrefab.name}' - all door orientations resulted in overlaps or connection issues");
        return false;
    }

    void CalculateRoomTransform(Door existingDoor, Door newRoomDoor, out Vector3 position, out Quaternion rotation)
    {
        // Since doors point INWARD, rotate so they face opposite directions
        Vector3 existingDoorInward = existingDoor.transform.forward;
        Vector3 newRoomDoorInward = newRoomDoor.transform.forward;
        
        // Rotate new room so its door faces opposite to existing door
        float angle = Vector3.SignedAngle(newRoomDoorInward, -existingDoorInward, Vector3.up);
        rotation = Quaternion.Euler(0, angle, 0);
        
        // Use connection points instead of transform positions for better alignment
        Vector3 existingConnectionPoint = existingDoor.GetConnectionPoint();
        
        // Calculate where the new door's connection point will be after rotation
        Vector3 newDoorLocalConnectionPoint = newRoomDoor.transform.localPosition + 
                                               newRoomDoor.transform.localRotation * newRoomDoor.connectionPointOffset;
        Vector3 rotatedConnectionPoint = rotation * newDoorLocalConnectionPoint;
        
        // Position the room so connection points align
        position = existingConnectionPoint - rotatedConnectionPoint;
        
        Debug.Log($"Existing connection point: {existingConnectionPoint}, New room position: {position}");
    }

    bool CheckOverlap(Vector3 position, Quaternion rotation, GameObject prefab)
    {
        RoomInfo checkRoom = prefab.GetComponent<RoomInfo>();
        if (checkRoom == null) return true;

        // Get all floor tiles (or any child with "Floor" in the name) from the prefab
        Transform[] prefabFloors = prefab.GetComponentsInChildren<Transform>(true);
        
        // For each placed room, check if any floor tiles would overlap
        foreach (RoomInfo placedRoom in placedRooms)
        {
            Transform[] placedFloors = placedRoom.GetComponentsInChildren<Transform>();
            
            // Check every floor tile in the new room against every floor in placed rooms
            foreach (Transform newFloor in prefabFloors)
            {
                // Skip if not a floor tile (check name or tag)
                if (!newFloor.name.Contains("Floor")) continue;
                
                // Calculate where this floor tile will be in world space
                Vector3 newFloorLocalPos = prefab.transform.InverseTransformPoint(newFloor.position);
                Vector3 newFloorWorldPos = position + rotation * newFloorLocalPos;
                
                foreach (Transform placedFloor in placedFloors)
                {
                    // Skip if not a floor tile
                    if (!placedFloor.name.Contains("Floor")) continue;
                    
                    // Check distance between floor tiles
                    // Floor tiles are 6x6, so if centers are less than 6 units apart on X or Z, they overlap
                    Vector3 placedFloorPos = placedFloor.position;
                    
                    float xDist = Mathf.Abs(newFloorWorldPos.x - placedFloorPos.x);
                    float zDist = Mathf.Abs(newFloorWorldPos.z - placedFloorPos.z);
                    
                    // Tiles overlap if they're too close on both X and Z axes
                    // Use 5.5 instead of 6 to allow for small floating point errors
                    if (xDist < 5.5f && zDist < 5.5f)
                    {
                        Debug.Log($"FLOOR OVERLAP: New floor at {newFloorWorldPos} overlaps with placed floor at {placedFloorPos} (xDist: {xDist:F2}, zDist: {zDist:F2})");
                        return true; // Overlap detected
                    }
                }
            }
        }

        Debug.Log($"No overlap detected for room at position {position}");
        return false; // No overlap
    }

    void ClearLevel()
    {
        foreach (Transform child in transform)
        {
            Destroy(child.gameObject);
        }
        
        placedRooms.Clear();
        availableDoors.Clear();
        roomsPlaced = 0;
        HasStartRoom = false;
        HasEndRoom = false;
    }

    void OnDrawGizmos()
    {
        if (!showDebugGizmos || !Application.isPlaying) return;

        // Draw available doors in green
        Gizmos.color = Color.green;
        foreach (Door door in availableDoors)
        {
            if (door != null)
            {
                Gizmos.DrawWireSphere(door.transform.position, 0.3f);
                Gizmos.DrawRay(door.transform.position, door.transform.forward * 1f);
            }
        }

        // Draw connected doors in red
        Gizmos.color = Color.red;
        foreach (RoomInfo room in placedRooms)
        {
            Door[] roomDoors = room.GetComponentsInChildren<Door>();
            foreach (Door door in roomDoors)
            {
                if (door.isConnected)
                {
                    Gizmos.DrawWireSphere(door.transform.position, 0.3f);
                }
            }
        }
    }
}