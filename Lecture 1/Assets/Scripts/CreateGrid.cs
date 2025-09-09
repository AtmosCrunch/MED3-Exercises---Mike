using UnityEngine;

public class CreateGrid : MonoBehaviour
{
    public Transform prefab;
    public int gridResolution;
    public Transform[] grid;
    public Vector3[] startPos;
    public Material[] materials;

    public Color colorA = Color.red;
    public Color colorB = Color.blue;

    void Awake()
    {
        int totalCubes = gridResolution * gridResolution * gridResolution;
        grid = new Transform[totalCubes];
        startPos = new Vector3[totalCubes];
        materials = new Material[totalCubes];

        for (int i = 0, z = 0; z < gridResolution; z++)
        {
            for (int y = 0; y < gridResolution; y++)
            {
                for (int x = 0; x < gridResolution; x++, i++)
                {
                    grid[i] = CreateGridPoint(x, y, z, i);
                    startPos[i] = GetCoordinates(x, y, z);
                }
            }
        }
    }

    Transform CreateGridPoint(int x, int y, int z, int index)
    {
        Transform point = Instantiate(prefab);
        point.localPosition = GetCoordinates(x, y, z);

        Material mat = new Material(point.GetComponent<MeshRenderer>().material);
        point.GetComponent<MeshRenderer>().material = mat;

        // Randomly assign one of the two colours
        Color chosenColor = Random.value > 0.5f ? colorA : colorB;
        mat.color = chosenColor;

        materials[index] = mat;

        return point;
    }

    Vector3 GetCoordinates(int x, int y, int z)
    {
        return new Vector3(
            x - (gridResolution - 1) * 0.5f,
            y - (gridResolution - 1) * 0.5f,
            z - (gridResolution - 1) * 0.5f
        );
    }
}
