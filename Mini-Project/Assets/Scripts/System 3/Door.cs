using UnityEngine;

public class Door : MonoBehaviour
{
    [HideInInspector]
    public RoomInfo parentRoom;
    
    [HideInInspector]
    public bool isConnected = false;
    
    [HideInInspector]
    public Door connectedDoor;
    
    [Tooltip("Offset from door transform to the center of the door opening (in local space). Set this if your door GameObject isn't centered on the opening.")]
    public Vector3 connectionPointOffset = Vector3.zero;

    public void Connect(Door otherDoor)
    {
        isConnected = true;
        connectedDoor = otherDoor;
        
        otherDoor.isConnected = true;
        otherDoor.connectedDoor = this;
    }
    
    /// <summary>
    /// Gets the world position where doors should connect
    /// </summary>
    public Vector3 GetConnectionPoint()
    {
        return transform.position + transform.TransformDirection(connectionPointOffset);
    }

    void OnDrawGizmos()
    {
        // Draw arrow showing door direction
        Gizmos.color = isConnected ? Color.red : Color.green;
        Gizmos.DrawWireSphere(transform.position, 0.3f);
        Gizmos.DrawRay(transform.position, transform.forward * 1.5f);
        
        // Draw connection point if there's an offset
        if (connectionPointOffset != Vector3.zero)
        {
            Vector3 connectionPoint = GetConnectionPoint();
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(connectionPoint, 0.4f);
            Gizmos.DrawLine(transform.position, connectionPoint);
        }
        
        // Draw line to connected door
        if (isConnected && connectedDoor != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(GetConnectionPoint(), connectedDoor.GetConnectionPoint());
        }
    }

    void OnDrawGizmosSelected()
    {
        // Highlight selected door
        Gizmos.color = Color.white;
        Gizmos.DrawWireSphere(transform.position, 0.5f);
        Gizmos.DrawRay(transform.position, transform.forward * 2f);
    }
}