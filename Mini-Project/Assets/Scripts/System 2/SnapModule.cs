using System.Linq;
using UnityEngine;

public class SnapModule : MonoBehaviour
{
    public Transform[] snapPoints;

    void Awake()
    {
        // Auto-collect snap points by name or tag
        snapPoints = GetComponentsInChildren<Transform>()
            .Where(t => t.name.StartsWith("Snap_"))
            .ToArray();
    }
}