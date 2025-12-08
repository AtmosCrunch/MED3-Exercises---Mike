using System.Collections.Generic;
using UnityEngine;

public class RoomInfo : MonoBehaviour
{
    public List<Door> doors = new List<Door>();

    void Awake()
    {
        // Auto-find doors if not set
        if (doors.Count == 0)
        {
            doors.AddRange(GetComponentsInChildren<Door>());
        }

        // Set parent room reference for each door
        foreach (Door door in doors)
        {
            door.parentRoom = this;
        }
    }

    void OnDrawGizmosSelected()
    {
        // Visualize room bounds
        Gizmos.color = Color.cyan;
        MeshRenderer[] renderers = GetComponentsInChildren<MeshRenderer>();
        
        foreach (MeshRenderer renderer in renderers)
        {
            Gizmos.DrawWireCube(renderer.bounds.center, renderer.bounds.size);
        }

        // Draw door positions
        Door[] doorComponents = GetComponentsInChildren<Door>();
        foreach (Door door in doorComponents)
        {
            Gizmos.color = door.isConnected ? Color.red : Color.green;
            Gizmos.DrawWireSphere(door.transform.position, 0.5f);
            Gizmos.DrawRay(door.transform.position, door.transform.forward * 2f);
        }
    }
}