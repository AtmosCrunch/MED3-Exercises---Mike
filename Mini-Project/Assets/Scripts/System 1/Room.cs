using UnityEngine;

public class Room : MonoBehaviour
{
    public Transform[] doorPoints; // Positions where other rooms can connect
    public bool isStartRoom;
    public bool isEndRoom;

    [Header("Spawn Settings")]
    public Transform spawnPoint; // Player spawn location inside this room
}