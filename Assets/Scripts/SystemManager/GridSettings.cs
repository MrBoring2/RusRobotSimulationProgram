using System.Collections.Generic;
using UnityEngine;
public class GridSettings : MonoBehaviour
{
    public float cellSize = 1f;
    public int drawDistance = 100;

    public Color thinLineColor = new Color(0.6f, 0.6f, 0.6f, 0.8f);
    public Color thickLineColor = new Color(0.831f, 0.831f, 0.831f, 1f);

    private Material[] materials;
    private MeshFilter meshFilter;
    private Camera mainCamera;
    private Vector3 lastCameraPos;

    void Start()
    {
        mainCamera = Camera.main;
        InitializeGrid();
    }

    void Update()
    {
        if (mainCamera == null) return;

        // ѕровер€ем перемещение по XZ дл€ перерисовки сетки
        Vector3 camPosXZ = new Vector3(mainCamera.transform.position.x, 0, mainCamera.transform.position.z);
        Vector3 lastCamPosXZ = new Vector3(lastCameraPos.x, 0, lastCameraPos.z);

        if (Vector3.Distance(camPosXZ, lastCamPosXZ) > cellSize)
        {
            UpdateVisibleGrid();
            lastCameraPos = mainCamera.transform.position;
        }
    }

    
    void InitializeGrid()
    {
        meshFilter = GetComponent<MeshFilter>();
        if (meshFilter == null)
            meshFilter = gameObject.AddComponent<MeshFilter>();

        MeshRenderer mr = GetComponent<MeshRenderer>();
        if (mr == null)
            mr = gameObject.AddComponent<MeshRenderer>();

        materials = new Material[2];
        materials[0] = new Material(Shader.Find("Unlit/Color"));
        materials[1] = new Material(Shader.Find("Unlit/Color"));

        mr.materials = materials;
        ApplyColors();

        transform.position = Vector3.zero;
        UpdateVisibleGrid();
    }

    void UpdateVisibleGrid()
    {
        if (mainCamera == null || meshFilter == null) return;

        Vector3 camPos = mainCamera.transform.position;

        int centerX = Mathf.FloorToInt(camPos.x / cellSize);
        int centerZ = Mathf.FloorToInt(camPos.z / cellSize);

        Mesh mesh = new Mesh();
        List<Vector3> thinVertices = new List<Vector3>();
        List<Vector3> thickVertices = new List<Vector3>();

        for (int i = -drawDistance; i <= drawDistance; i++)
        {
            bool isThickX = (Mathf.Abs(centerX + i) % 10 == 0);
            bool isThickZ = (Mathf.Abs(centerZ + i) % 10 == 0);

            float worldX = (centerX + i) * cellSize;
            float worldZ = (centerZ + i) * cellSize;

            float minZ = (centerZ - drawDistance) * cellSize;
            float maxZ = (centerZ + drawDistance) * cellSize;
            float minX = (centerX - drawDistance) * cellSize;
            float maxX = (centerX + drawDistance) * cellSize;

            if (isThickX)
            {
                thickVertices.Add(new Vector3(worldX, 0, minZ));
                thickVertices.Add(new Vector3(worldX, 0, maxZ));
            }
            else
            {
                thinVertices.Add(new Vector3(worldX, 0, minZ));
                thinVertices.Add(new Vector3(worldX, 0, maxZ));
            }

            if (isThickZ)
            {
                thickVertices.Add(new Vector3(minX, 0, worldZ));
                thickVertices.Add(new Vector3(maxX, 0, worldZ));
            }
            else
            {
                thinVertices.Add(new Vector3(minX, 0, worldZ));
                thinVertices.Add(new Vector3(maxX, 0, worldZ));
            }
        }

        Vector3[] allVertices = new Vector3[thinVertices.Count + thickVertices.Count];
        thinVertices.CopyTo(allVertices, 0);
        thickVertices.CopyTo(allVertices, thinVertices.Count);

        mesh.vertices = allVertices;
        mesh.subMeshCount = 2;

        int[] thinIndices = new int[thinVertices.Count];
        for (int i = 0; i < thinIndices.Length; i++)
            thinIndices[i] = i;
        mesh.SetIndices(thinIndices, MeshTopology.Lines, 0);

        int[] thickIndices = new int[thickVertices.Count];
        for (int i = 0; i < thickIndices.Length; i++)
            thickIndices[i] = thinVertices.Count + i;
        mesh.SetIndices(thickIndices, MeshTopology.Lines, 1);

        meshFilter.mesh = mesh;
    }

    void ApplyColors()
    {
        if (materials == null || materials.Length < 2) return;

        materials[0].color = thinLineColor;
        materials[1].color = thickLineColor;
    }
}

