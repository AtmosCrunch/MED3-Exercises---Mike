using UnityEngine;

public class AudioSpectrumAnalyzer : MonoBehaviour
{
    public int spectrumSize = 64;
    public float sensitivity = 10f;
    public float minScale = 0.1f;
    public float maxScale = 3f;

    public CreateGrid createGrid; // Assign this in the Inspector or via script

    private float[] spectrum;

    void Start()
    {
        spectrum = new float[spectrumSize];
    }

    void Update()
    {
        if (createGrid == null || createGrid.grid == null) return;

        AudioListener.GetSpectrumData(spectrum, 0, FFTWindow.BlackmanHarris);

        int cubeCount = createGrid.grid.Length;
        for (int i = 0; i < cubeCount; i++)
        {
            int band = Mathf.FloorToInt((float)i / cubeCount * spectrumSize);
            float intensity = spectrum[band] * sensitivity;
            intensity = Mathf.Clamp(intensity, minScale, maxScale);

            Transform cube = createGrid.grid[i];
            if (cube != null)
            {
                Vector3 scale = cube.localScale;
                scale.y = intensity;
                cube.localScale = scale;
            }
        }
    }
}