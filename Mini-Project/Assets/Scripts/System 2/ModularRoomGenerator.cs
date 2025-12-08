using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class ModularRoomGenerator : MonoBehaviour
{
    [Header("Room Settings")]
    public Vector2Int floorSize = new Vector2Int(6, 6);
    public int maxDoors = 4;

    [Header("Prefabs")]
    public GameObject floorPrefab;
    public GameObject wallPrefab;
    public GameObject doorPrefab;
    public GameObject cornerPrefab;

    [Header("Offsets")]
    public float tileSize = 1f;

    private List<Transform> perimeterSnapTargets = new List<Transform>();

    void Start()
    {
        GenerateRoom();
    }

    void GenerateRoom()
    {
        // 1. Generate floor grid
        for (int x = 0; x < floorSize.x; x++)
        {
            for (int y = 0; y < floorSize.y; y++)
            {
                Vector3 pos = new Vector3(x * tileSize, 0, y * tileSize);
                GameObject floorTile = Instantiate(floorPrefab, pos, Quaternion.identity, transform);

                // Collect perimeter snap points only for edge tiles
                if (x == 0 || y == 0 || x == floorSize.x - 1 || y == floorSize.y - 1)
                {
                    SnapModule snapModule = floorTile.GetComponent<SnapModule>();
                    if (snapModule != null)
                    {
                        foreach (Transform snap in snapModule.snapPoints)
                        {
                            // Only add snap points that face outward (based on tile position)
                            if (IsSnapOnPerimeter(snap.name, x, y))
                                perimeterSnapTargets.Add(snap);
                        }
                    }
                }
            }
        }

        // 2. Place walls and doors using perimeter snap points
        int doorCount = 0;
        foreach (Transform targetSnap in perimeterSnapTargets)
        {
            GameObject prefabToPlace;

            if (doorCount < maxDoors && Random.value < 0.2f) // 20% chance for door
            {
                prefabToPlace = doorPrefab;
                doorCount++;
            }
            else
            {
                prefabToPlace = wallPrefab;
            }

            PlaceModuleAtSnap(prefabToPlace, targetSnap);
        }

        // 3. Place corners at calculated positions
        PlaceModuleAtSnap(cornerPrefab, null, new Vector3(-tileSize / 2, 0, -tileSize / 2));
        PlaceModuleAtSnap(cornerPrefab, null, new Vector3((floorSize.x - 1) * tileSize + tileSize / 2, 0, -tileSize / 2));
        PlaceModuleAtSnap(cornerPrefab, null, new Vector3(-tileSize / 2, 0, (floorSize.y - 1) * tileSize + tileSize / 2));
        PlaceModuleAtSnap(cornerPrefab, null, new Vector3((floorSize.x - 1) * tileSize + tileSize / 2, 0, (floorSize.y - 1) * tileSize + tileSize / 2));
    }

    bool IsSnapOnPerimeter(string snapName, int x, int y)
    {
        // Only include snap points that correspond to the tile's edge
        if (snapName.Contains("North") && y == floorSize.y - 1) return true;
        if (snapName.Contains("South") && y == 0) return true;
        if (snapName.Contains("East") && x == floorSize.x - 1) return true;
        if (snapName.Contains("West") && x == 0) return true;
        return false;
    }

    void PlaceModuleAtSnap(GameObject prefab, Transform targetSnap, Vector3? overridePos = null)
    {
        GameObject module = Instantiate(prefab, Vector3.zero, Quaternion.identity, transform);
        SnapModule snapModule = module.GetComponent<SnapModule>();

        if (snapModule == null || snapModule.snapPoints.Length == 0)
        {
            Debug.LogWarning($"Prefab {prefab.name} has no snap points!");
            return;
        }

        Transform moduleSnap = snapModule.snapPoints[0]; // Use first snap point for alignment

        if (targetSnap != null)
        {
            // Align position
            Vector3 offset = module.transform.position - moduleSnap.position;
            module.transform.position = targetSnap.position + offset;

            // Align rotation using offset logic
            Quaternion rotationOffset = targetSnap.rotation * Quaternion.Inverse(moduleSnap.rotation);
            module.transform.rotation = rotationOffset;
        }
        else if (overridePos.HasValue)
        {
            module.transform.position = overridePos.Value;
        }

        // Optional: Apply random rotation in 90° increments AFTER snapping
        int rotationStep = Random.Range(0, 4);
        module.transform.Rotate(Vector3.up * rotationStep * 90f, Space.World);
    }
}
