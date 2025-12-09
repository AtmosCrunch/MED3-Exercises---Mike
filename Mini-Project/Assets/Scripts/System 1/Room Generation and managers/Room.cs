using UnityEngine;

public class Room : MonoBehaviour
{
    public Transform[] doorPoints; // Positions where other rooms can connect
    public bool isStartRoom;
    public bool isEndRoom;

    [Header("Spawn Settings")]
    public Transform spawnPoint; // Player spawn location inside this room
    
    [Header("Enemy Spawns")]
    [Tooltip("Positions where enemies can spawn in this room")]
    public Transform[] enemySpawnPoints;
}