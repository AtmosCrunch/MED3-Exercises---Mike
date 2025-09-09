using UnityEngine;

public class Transformations : MonoBehaviour
{
    CreateGrid createGrid;
    Vector3[] startPos;
    Vector3 startScale = Vector3.one;

    [Header("Movement Controls")]
    public float moveFrequency = 2f;
    public float moveAmplitude = 5f;
    public float moveOffset = 0f;

    [Header("Scale Controls")]
    public float scaleFrequency = 2f;
    public float scaleAmplitude = 5f;
    public float scaleOffset = 0f;

    [Header("Rotation Controls")]
    public float rotationSpeed = 1f;

    [Header("Audio Spectrum Controls")]
    public int spectrumSize = 64;
    public float sensitivity = 10f;
    public float minScale = 0.1f;
    public float maxScale = 3f;

    float[] spectrum;

    void Start()
    {
        createGrid = GetComponent<CreateGrid>();
        startPos = createGrid.startPos;
        startScale = createGrid.grid[0].localScale;
        spectrum = new float[spectrumSize];
    }

    void Update()
    {
        AudioListener.GetSpectrumData(spectrum, 0, FFTWindow.BlackmanHarris);

        for (int i = 0, z = 0; z < createGrid.gridResolution; z++)
        {
            for (int y = 0; y < createGrid.gridResolution; y++)
            {
                for (int x = 0; x < createGrid.gridResolution; x++, i++)
                {
                    Transform cube = createGrid.grid[i];

                    // Movement
                    cube.localPosition = startPos[i] + Vector3.up * Mathf.Sin(Time.time * moveFrequency + moveOffset) * moveAmplitude;

                    // Audio-driven scale
                    int band = i % spectrumSize;
                    float intensity = spectrum[band] * sensitivity;
                    intensity = Mathf.Clamp(intensity, minScale, maxScale);

                    Vector3 newScale = startScale;
                    newScale.y = intensity;
                    cube.localScale = newScale;

                    // Rotation
                    cube.Rotate(Vector3.forward * Time.deltaTime * rotationSpeed);

                    // Audio-driven colour change for one of the colours
                    Color dynamicColor = Color.Lerp(Color.blue, Color.magenta, intensity / maxScale);
                    createGrid.materials[i].color = createGrid.materials[i].color == createGrid.colorB ? dynamicColor : createGrid.materials[i].color;
                }
            }
        }
    }
}
