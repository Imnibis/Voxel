using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class Chunk
{
    static int chunkSize = World.instance.chunkSize;
    static int chunkHeight = World.instance.chunkHeight;

    public Vector2Int position;

    int vertexIndex = 0;
    List<Vector3> vertices = new List<Vector3>();
    List<int> triangles = new List<int>();
    List<int> tintedTriangles = new List<int>();
    List<Vector2> uvs = new List<Vector2>();

    public Block[,,] region = new Block[chunkSize, chunkHeight, chunkSize];

    public bool isLoaded = false;

    public GameObject chunkObject;

    public Chunk(Vector2Int position)
    {
        this.position = position;
    }

    public bool CheckVoxel(Vector3 pos)
    {
        int x = Mathf.FloorToInt(pos.x);
        int y = Mathf.FloorToInt(pos.y);
        int z = Mathf.FloorToInt(pos.z);

        if (y < 0 || y >= chunkHeight)
            return false;

        Block block = (x < 0 || z < 0 || x >= chunkSize || z >= chunkSize) ? World.instance.GetBlock(new Vector3Int(position.x * chunkSize + x, y, position.y * chunkSize + z)) : region[x, y, z];

        if (block.blockID != "air")
            return true;
        else return false;
    }

    public void UpdateMesh()
    {
        isLoaded = true;
        vertexIndex = 0;
        vertices.Clear();
        triangles.Clear();
        tintedTriangles.Clear();
        uvs.Clear();

        for(int x = 0; x < chunkSize; x++)
        {
            for (int y = 0; y < chunkHeight; y++) 
            {
                for (int z = 0; z < chunkSize; z++)
                {
                    AddVoxelToChunk(new Vector3Int(x, y, z));
                }
            }
        }

        UpdateChunkObject(CreateMesh(vertices, triangles, uvs));
    }

    void AddVoxelToChunk(Vector3Int pos)
    {
        foreach (Model model in region[pos.x, pos.y, pos.z].models)
        {
            for (int p = 0; p < 6; p++)
            {
                if (model.faceChecks[p] == new Vector3(2, 2, 2) || !CheckVoxel(pos + model.faceChecks[p]))
                {
                    for (int i = 0; i < 6; i++)
                    {
                        int triangleIndex = model.voxelTris[p, i];
                        vertices.Add(model.voxelVerts[triangleIndex] + pos);
                        if (model.tintedFaces.Contains(p))
                            tintedTriangles.Add(vertexIndex);
                        else
                            triangles.Add(vertexIndex);

                        uvs.Add(model.voxelUvs[p, i]);

                        vertexIndex++;
                    }
                }
            }
        }
    }

    Mesh CreateMesh(List<Vector3> vertices, List<int> triangles, List<Vector2> uvs)
    {
        Mesh mesh = new Mesh();
        mesh.vertices = vertices.ToArray();
        mesh.subMeshCount = 2;
        mesh.SetTriangles(triangles.ToArray(), 0);
        mesh.SetTriangles(tintedTriangles.ToArray(), 1);
        mesh.uv = uvs.ToArray();

        mesh.RecalculateNormals();

        return mesh;
    }

    void UpdateChunkObject(Mesh mesh)
    {
        if(chunkObject == null)
        {
            chunkObject = new GameObject("Chunk ("+ position.x +"; "+ position.y +")");
            chunkObject.transform.position = new Vector3(position.x * chunkSize, 0, position.y * chunkSize);
            chunkObject.AddComponent<MeshFilter>().mesh = mesh;
            MeshRenderer meshRenderer = chunkObject.AddComponent<MeshRenderer>();
            meshRenderer.materials = new Material[2] { World.instance.terrainMaterial, World.instance.tintedMaterial };
        }
        else
        {
            chunkObject.GetComponent<MeshFilter>().mesh = mesh;
        }
    }
}
