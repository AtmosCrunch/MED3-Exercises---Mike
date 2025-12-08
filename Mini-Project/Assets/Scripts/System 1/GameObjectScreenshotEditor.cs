#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(GameObjectScreenshot))]
public class GameObjectScreenshotEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        
        GameObjectScreenshot script = (GameObjectScreenshot)target;
        
        GUILayout.Space(10);
        
        if (GUILayout.Button("Capture Screenshot", GUILayout.Height(40)))
        {
            script.Capture();
        }
    }
}
#endif