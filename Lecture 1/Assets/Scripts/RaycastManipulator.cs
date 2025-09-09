using System.Collections.Generic;
using UnityEngine;

public class RaycastManipulator : MonoBehaviour
{
    public int numRaycasts = 10;
    public int spectrumSize = 64;
    public float sensitivity = 10f;
    public float resetThreshold = 0.5f; // Trigger reset when this value is exceeded

    CreateGrid createGrid;
    List<GameObject> hitCubes = new List<GameObject>();
    float[] spectrum;

    void Start()
    {
        createGrid = GetComponent<CreateGrid>();
        spectrum = new float[spectrumSize];
    }

    void Update()
    {
        AudioListener.GetSpectrumData(spectrum, 0, FFTWindow.BlackmanHarris);

        // Check if any frequency band exceeds the threshold
        bool shouldReset = false;
        for (int i = 0; i < spectrumSize; i++)
        {
            if (spectrum[i] * sensitivity > resetThreshold)
            {
                shouldReset = true;
                break;
            }
        }

        if (shouldReset && hitCubes.Count > 0)
        {
            foreach (GameObject cube in hitCubes)
            {
                cube.SetActive(true);
            }
            hitCubes.Clear();
        }

        // Raycasting
        Vector3 cubeCenter = transform.position;
        for (int i = 0; i < numRaycasts; i++)
        {
            Vector3 direction = Random.onUnitSphere;
            RaycastHit hit;
            if (Physics.Raycast(cubeCenter, direction, out hit, 100))
            {
                GameObject hitObj = hit.transform.gameObject;
                if (!hitCubes.Contains(hitObj))
                {
                    hitObj.SetActive(false);
                    hitCubes.Add(hitObj);
                }
            }
        }
    }
}