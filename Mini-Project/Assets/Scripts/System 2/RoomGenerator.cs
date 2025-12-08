using UnityEngine;
using System.Collections.Generic;

public class RoomGenerator : MonoBehaviour
{
    [System.Serializable]
    public class RoomPrefabs
    {
        public GameObject floorPrefab;
        public GameObject wallPrefab;
        public GameObject cornerPrefab;
        public GameObject doorPrefab;
    }

    public enum RoomShape
    {
        Rectangle,
        LShape,
        TShape,
        CrossShape,
        Random
    }

    [Header("Prefabs")]
    public RoomPrefabs prefabs;

    [Header("Room Configuration")]
    public RoomShape roomShape = RoomShape.Random;
    public int minRoomSize = 3;
    public int maxRoomSize = 7;
    [Range(1, 10)]
    public int doorCount = 1;

    [Header("Module Settings")]
    public float moduleSize = 6f;
    public float wallHeight = 0f;

    private List<GameObject> spawnedObjects = new List<GameObject>();
    private HashSet<Vector2Int> floorTiles = new HashSet<Vector2Int>();

    public GameObject playerPrefab; // Assign a Capsule prefab in Inspector
    
    void Start()
    {
        GenerateRoom();
    }

    [ContextMenu("Generate Room")]
    public void GenerateRoom()
    {
        ClearRoom();
        floorTiles.Clear();
        GenerateFloorLayout();
        GenerateFloor();
        GenerateWalls();
        SpawnPlayerAtCenter(); // Add this
    }

    [ContextMenu("Clear Room")]
    public void ClearRoom()
    {
        foreach (GameObject obj in spawnedObjects)
        {
            if (obj != null) DestroyImmediate(obj);
        }
        spawnedObjects.Clear();
    }

    void GenerateFloorLayout()
    {
        RoomShape shape = roomShape;
        if (shape == RoomShape.Random)
            shape = (RoomShape)Random.Range(0, 4);

        switch (shape)
        {
            case RoomShape.Rectangle: GenerateRectangleLayout(); break;
            case RoomShape.LShape: GenerateLShapeLayout(); break;
            case RoomShape.TShape: GenerateTShapeLayout(); break;
            case RoomShape.CrossShape: GenerateCrossShapeLayout(); break;
        }
    }

    void GenerateRectangleLayout()
    {
        int width = Random.Range(minRoomSize, maxRoomSize + 1);
        int depth = Random.Range(minRoomSize, maxRoomSize + 1);
        for (int x = 0; x < width; x++)
            for (int z = 0; z < depth; z++)
                floorTiles.Add(new Vector2Int(x, z));
    }

    void GenerateLShapeLayout()
    {
        int width1 = Random.Range(minRoomSize, maxRoomSize + 1);
        int depth1 = Random.Range(minRoomSize, maxRoomSize + 1);
        int width2 = Random.Range(minRoomSize - 1, width1);
        int depth2 = Random.Range(minRoomSize - 1, depth1);

        for (int x = 0; x < width1; x++)
            for (int z = 0; z < depth1; z++)
                floorTiles.Add(new Vector2Int(x, z));

        for (int x = width1; x < width1 + width2; x++)
            for (int z = 0; z < depth2; z++)
                floorTiles.Add(new Vector2Int(x, z));
    }

    void GenerateTShapeLayout()
    {
        int stemWidth = Random.Range(minRoomSize - 1, minRoomSize + 1);
        int stemDepth = Random.Range(minRoomSize, maxRoomSize + 1);
        int topWidth = Random.Range(minRoomSize + 2, maxRoomSize + 1);
        int topDepth = Random.Range(minRoomSize - 1, minRoomSize + 1);

        int stemStartX = (topWidth - stemWidth) / 2;
        for (int x = stemStartX; x < stemStartX + stemWidth; x++)
            for (int z = 0; z < stemDepth; z++)
                floorTiles.Add(new Vector2Int(x, z));

        for (int x = 0; x < topWidth; x++)
            for (int z = stemDepth; z < stemDepth + topDepth; z++)
                floorTiles.Add(new Vector2Int(x, z));
    }

    void GenerateCrossShapeLayout()
    {
        int size = Random.Range(minRoomSize, maxRoomSize + 1);
        int armWidth = Mathf.Max(2, Random.Range(minRoomSize - 2, minRoomSize));
        int center = size / 2;

        for (int x = 0; x < size; x++)
            for (int z = center - armWidth / 2; z < center + armWidth / 2 + 1; z++)
                floorTiles.Add(new Vector2Int(x, z));

        for (int x = center - armWidth / 2; x < center + armWidth / 2 + 1; x++)
            for (int z = 0; z < size; z++)
                floorTiles.Add(new Vector2Int(x, z));
    }

    void GenerateFloor()
    {
        foreach (Vector2Int tile in floorTiles)
        {
            Vector3 position = new Vector3(tile.x * moduleSize, 0, tile.y * moduleSize);
            GameObject floor = Instantiate(prefabs.floorPrefab, position, Quaternion.identity, transform);
            floor.name = $"Floor_{tile.x}_{tile.y}";
            spawnedObjects.Add(floor);
        }
    }

    void GenerateWalls()
    {
        List<Vector3> doorPositions = GetDoorPositions();
        Dictionary<Vector2Int, int> cornerPositions = FindAllCorners();
        HashSet<Vector2Int> processedCorners = new HashSet<Vector2Int>();

        foreach (var kvp in cornerPositions)
        {
            Vector3 pos = new Vector3(kvp.Key.x * moduleSize, wallHeight, kvp.Key.y * moduleSize);
            PlaceCorner(pos, kvp.Value);
            processedCorners.Add(kvp.Key);
        }

        foreach (Vector2Int tile in floorTiles)
        {
            bool hasNorth = floorTiles.Contains(new Vector2Int(tile.x, tile.y + 1));
            bool hasSouth = floorTiles.Contains(new Vector2Int(tile.x, tile.y - 1));
            bool hasEast = floorTiles.Contains(new Vector2Int(tile.x + 1, tile.y));
            bool hasWest = floorTiles.Contains(new Vector2Int(tile.x - 1, tile.y));

            TryPlaceWallOrDoor(tile, hasNorth, 90, new Vector2Int(tile.x, tile.y + 1), doorPositions, cornerPositions, processedCorners);
            TryPlaceWallOrDoor(tile, hasSouth, -90, new Vector2Int(tile.x, tile.y), doorPositions, cornerPositions, processedCorners);
            TryPlaceWallOrDoor(tile, hasEast, 180, new Vector2Int(tile.x + 1, tile.y), doorPositions, cornerPositions, processedCorners);
            TryPlaceWallOrDoor(tile, hasWest, 0, new Vector2Int(tile.x, tile.y), doorPositions, cornerPositions, processedCorners);
        }
    }

    void TryPlaceWallOrDoor(Vector2Int tile, bool hasNeighbor, int rotation, Vector2Int edgePos,
        List<Vector3> doorPositions, Dictionary<Vector2Int, int> cornerPositions, HashSet<Vector2Int> processedCorners)
    {
        if (!hasNeighbor && !processedCorners.Contains(edgePos))
        {
            Vector3 pos = new Vector3(edgePos.x * moduleSize, wallHeight, edgePos.y * moduleSize);
            if (!IsPositionOccupiedByDoor(pos, doorPositions) && !IsNearCorner(pos, cornerPositions))
                PlaceWall(pos, Quaternion.Euler(0, rotation, 0), $"Wall_{tile.x}_{tile.y}");
            else if (!IsNearCorner(pos, cornerPositions))
                PlaceDoor(pos, Quaternion.Euler(0, rotation, 0));
        }
    }

    Dictionary<Vector2Int, int> FindAllCorners()
    {
        Dictionary<Vector2Int, int> corners = new Dictionary<Vector2Int, int>();
        HashSet<Vector2Int> checkedPositions = new HashSet<Vector2Int>();

        foreach (Vector2Int tile in floorTiles)
        {
            CheckCorner(tile.x, tile.y, corners, checkedPositions);
            CheckCorner(tile.x + 1, tile.y, corners, checkedPositions);
            CheckCorner(tile.x, tile.y + 1, corners, checkedPositions);
            CheckCorner(tile.x + 1, tile.y + 1, corners, checkedPositions);
        }
        return corners;
    }

    void CheckCorner(int x, int y, Dictionary<Vector2Int, int> corners, HashSet<Vector2Int> checkedPositions)
    {
        Vector2Int pos = new Vector2Int(x, y);
        if (checkedPositions.Contains(pos)) return;
        checkedPositions.Add(pos);

        bool hasSW = floorTiles.Contains(new Vector2Int(x - 1, y - 1));
        bool hasSE = floorTiles.Contains(new Vector2Int(x, y - 1));
        bool hasNW = floorTiles.Contains(new Vector2Int(x - 1, y));
        bool hasNE = floorTiles.Contains(new Vector2Int(x, y));

        int tileCount = (hasSW ? 1 : 0) + (hasSE ? 1 : 0) + (hasNW ? 1 : 0) + (hasNE ? 1 : 0);

        int rotation = 0;
        if (tileCount == 1)
        {
            if (hasSW) rotation = 180;
            else if (hasSE) rotation = 90;
            else if (hasNE) rotation = 0;
            else if (hasNW) rotation = 270;
        }
        else if (tileCount == 3)
        {
            if (!hasSW) rotation = 180; // FIXED: flipped this case
            else if (!hasSE) rotation = 90;
            else if (!hasNE) rotation = 180;
            else if (!hasNW) rotation = 270;
        }

        if (tileCount == 1 || tileCount == 3)
            corners[pos] = rotation;
    }

    void PlaceCorner(Vector3 pos, int rotation)
    {
        GameObject corner = Instantiate(prefabs.cornerPrefab, pos, Quaternion.Euler(0, rotation, 0), transform);
        corner.name = $"Corner_{pos.x}_{pos.z}";
        spawnedObjects.Add(corner);
    }

    void PlaceWall(Vector3 pos, Quaternion rot, string objName)
    {
        GameObject wall = Instantiate(prefabs.wallPrefab, pos, rot, transform);
        wall.name = objName;
        spawnedObjects.Add(wall);
    }

    void PlaceDoor(Vector3 pos, Quaternion rot)
    {
        GameObject door = Instantiate(prefabs.doorPrefab, pos, rot, transform);
        door.name = $"Door_{pos.x}_{pos.z}";
        spawnedObjects.Add(door);
    }

    List<Vector3> GetDoorPositions()
    {
        List<Vector3> positions = new List<Vector3>();
        List<Vector3> validWallPositions = new List<Vector3>();
        Dictionary<Vector2Int, int> cornerPositions = FindAllCorners();

        foreach (Vector2Int tile in floorTiles)
        {
            bool hasNorth = floorTiles.Contains(new Vector2Int(tile.x, tile.y + 1));
            bool hasSouth = floorTiles.Contains(new Vector2Int(tile.x, tile.y - 1));
            bool hasEast = floorTiles.Contains(new Vector2Int(tile.x + 1, tile.y));
            bool hasWest = floorTiles.Contains(new Vector2Int(tile.x - 1, tile.y));

            if (!hasNorth && !cornerPositions.ContainsKey(new Vector2Int(tile.x, tile.y + 1)))
                validWallPositions.Add(new Vector3(tile.x * moduleSize, wallHeight, (tile.y + 1) * moduleSize));
            if (!hasSouth && !cornerPositions.ContainsKey(new Vector2Int(tile.x, tile.y)))
                validWallPositions.Add(new Vector3(tile.x * moduleSize, wallHeight, tile.y * moduleSize));
            if (!hasEast && !cornerPositions.ContainsKey(new Vector2Int(tile.x + 1, tile.y)))
                validWallPositions.Add(new Vector3((tile.x + 1) * moduleSize, wallHeight, tile.y * moduleSize));
            if (!hasWest && !cornerPositions.ContainsKey(new Vector2Int(tile.x, tile.y)))
                validWallPositions.Add(new Vector3(tile.x * moduleSize, wallHeight, tile.y * moduleSize));
        }

        validWallPositions = RemoveDuplicatePositions(validWallPositions);
        int actualDoorCount = Mathf.Min(Mathf.Max(1, doorCount), validWallPositions.Count);

        for (int i = 0; i < actualDoorCount && validWallPositions.Count > 0; i++)
        {
            int randomIndex = Random.Range(0, validWallPositions.Count);
            positions.Add(validWallPositions[randomIndex]);
            validWallPositions.RemoveAt(randomIndex);
        }
        return positions;
    }

    List<Vector3> RemoveDuplicatePositions(List<Vector3> positions)
    {
        List<Vector3> unique = new List<Vector3>();
        foreach (Vector3 pos in positions)
        {
            bool isDuplicate = false;
            foreach (Vector3 existing in unique)
            {
                if (Vector3.Distance(pos, existing) < 0.1f)
                {
                    isDuplicate = true;
                    break;
                }
            }
            if (!isDuplicate) unique.Add(pos);
        }
        return unique;
    }

    bool IsPositionOccupiedByDoor(Vector3 pos, List<Vector3> doorPositions)
    {
        foreach (Vector3 doorPos in doorPositions)
        {
            if (Vector3.Distance(pos, doorPos) < 0.1f) return true;
        }
        return false;
    }

    bool IsNearCorner(Vector3 pos, Dictionary<Vector2Int, int> cornerPositions)
    {
        foreach (var corner in cornerPositions.Keys)
        {
            Vector3 cornerPos = new Vector3(corner.x * moduleSize, wallHeight, corner.y * moduleSize);
            if (Vector3.Distance(pos, cornerPos) < moduleSize * 0.5f) return true;
        }
        return false;
    }
    
    void SpawnPlayerAtCenter()
    {
        if (floorTiles.Count == 0) return;

        // Calculate center of all floor tiles
        float sumX = 0f, sumZ = 0f;
        foreach (var tile in floorTiles)
        {
            sumX += tile.x * moduleSize;
            sumZ += tile.y * moduleSize;
        }
        Vector3 centerPos = new Vector3(sumX / floorTiles.Count, 1f, sumZ / floorTiles.Count);

        // Instantiate player
        GameObject player = Instantiate(playerPrefab, centerPos, Quaternion.identity);
        player.name = "Player";
    }

}