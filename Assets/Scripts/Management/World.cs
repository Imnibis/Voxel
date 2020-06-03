using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

public class World : MonoBehaviour
{
    public static World instance
    {
        get
        {
            if (_instance == null)
                _instance = GameObject.FindGameObjectWithTag("World").GetComponent<World>();
            return _instance;
        }
    }

    static World _instance;

    public int seed;

    public Material terrainMaterial;
    public Material tintedMaterial;
    public Material itemMaterial;

    public int chunkSize = 16;
    public int chunkHeight = 256;

    public Camera player;

    [SerializeField] int viewDistance = 16;
    [SerializeField] int lazyLoading = 1;

    [SerializeField] List<Block> blocks = new List<Block>();

    [SerializeField] List<Biome> biomes = new List<Biome>();

    Dictionary<Vector2Int, Chunk> chunks = new Dictionary<Vector2Int, Chunk>();

    List<Vector2Int> chunksToLazyLoad = new List<Vector2Int>();
    List<Vector2Int> chunksToGenerate = new List<Vector2Int>();
    List<Vector2Int> chunksToUnload = new List<Vector2Int>();

    private void Awake()
    {
        foreach(Block block in blocks)
        {
            BlockRegistry.Blocks.Add(block.blockID, block);
        }
        BlockRegistry.InitializeBlocks();
        TextureManager.SetTerrainTexture(terrainMaterial, tintedMaterial);
        blocks.Clear();
    }

    Block[,,] GenerateChunkTerrain(Vector2 chunkPosition)
    {
        Block[,,] region = new Block[chunkSize,chunkHeight,chunkSize];
        for (int x = 0; x < chunkSize; x++)
        {
            for (int y = 0; y < chunkHeight; y++)
            {
                for (int z = 0; z < chunkSize; z++)
                {
                    Vector3 blockPos = new Vector3(chunkPosition.x * chunkSize + x, y, chunkPosition.y * chunkSize + z);
                    Biome biome = GetBiome(blockPos);
                    float perlinNoise = Mathf.PerlinNoise(blockPos.x * biome.perlinScale + seed, blockPos.z * biome.perlinScale + seed);
                    region[x, y, z] = BlockRegistry.Blocks["air"];
                    foreach (BiomeLayer layer in biome.layers)
                    {
                        int bottomPos = Mathf.RoundToInt(layer.bottomMinMax.x + perlinNoise * (layer.bottomMinMax.y - layer.bottomMinMax.x));
                        int topPos = Mathf.RoundToInt(layer.topMinMax.x + perlinNoise * (layer.topMinMax.y - layer.topMinMax.x));
                        if (y >= bottomPos && y <= topPos)
                        {
                            region[x, y, z] = layer.block;
                        }
                    }
                }
            }
        }
        return region;
    }

    public Biome GetBiome(Vector3 pos)
    {
        return biomes[0];
    }

    public Vector2Int GetPlayerChunk()
    {
        return new Vector2Int(Mathf.FloorToInt(player.transform.position.x / chunkSize), Mathf.FloorToInt(player.transform.position.z / chunkSize));
    }

    void GenerateTerrain()
    {
        Vector2Int playerChunk = GetPlayerChunk();
        for (int x = playerChunk.x - (viewDistance + lazyLoading); x < playerChunk.x + viewDistance + lazyLoading; x++)
        {
            for (int z = playerChunk.y - (viewDistance + lazyLoading); z < playerChunk.y + viewDistance + lazyLoading; z++)
            {
                Vector2Int pos = new Vector2Int(x, z);
                if (!chunks.ContainsKey(pos) && x >= playerChunk.x - viewDistance && x <= playerChunk.x + viewDistance && z >= playerChunk.y - viewDistance && z <= playerChunk.y + viewDistance)
                    chunksToGenerate.Add(pos);
                else if (!chunks.ContainsKey(pos))
                    chunksToLazyLoad.Add(pos);
            }
        }
    }

    void LazyLoad(Vector2Int pos)
    {
        if(!chunks.ContainsKey(pos))
        {
            Chunk chunk = new Chunk(pos);
            chunk.region = GenerateChunkTerrain(pos);
            chunks.Add(pos, chunk);
        }
    }

    public Block GetBlock(Vector3Int pos)
    {
        Vector2Int chunkPos = new Vector2Int(Mathf.FloorToInt(pos.x / (float) chunkSize), Mathf.FloorToInt(pos.z / (float) chunkSize));
        Vector3Int blockPos = new Vector3Int(pos.x - chunkPos.x * chunkSize, pos.y, pos.z - chunkPos.y * chunkSize);
        Chunk chunk;
        if (chunks.TryGetValue(chunkPos, out chunk))
            return chunk.region[blockPos.x, blockPos.y, blockPos.z];
        else
            return BlockRegistry.Blocks["air"];
    }

    private void Start()
    {
        GenerateTerrain();
        foreach (Vector2Int pos in chunksToLazyLoad)
        {
            LazyLoad(pos);
        }
        chunksToLazyLoad.Clear();
        foreach(Vector2Int pos in chunksToGenerate)
        {
            LazyLoad(pos);
        }

    }

    void Update()
    {
        Vector2Int playerChunk = GetPlayerChunk();
        foreach (KeyValuePair<Vector2Int, Chunk> pair in chunks)
        {
            if(!(pair.Key.x >= playerChunk.x - (viewDistance + lazyLoading) && pair.Key.x <= playerChunk.x + (viewDistance + lazyLoading) && pair.Key.y >= playerChunk.y - (viewDistance + lazyLoading) && pair.Key.y <= playerChunk.y + (viewDistance + lazyLoading)))
            {
                chunksToUnload.Add(pair.Key);
            }
            else if(!(pair.Key.x >= playerChunk.x - viewDistance && pair.Key.x <= playerChunk.x + viewDistance && pair.Key.y >= playerChunk.y - viewDistance && pair.Key.y <= playerChunk.y + viewDistance) && pair.Value.isLoaded)
            {
                pair.Value.chunkObject.GetComponent<MeshFilter>().mesh = null;
                pair.Value.isLoaded = false;
            }
        }
        foreach (Vector2Int pos in chunksToUnload)
            chunks.Remove(pos);

        chunksToUnload.Clear();

        GenerateTerrain();

        if (chunksToLazyLoad.Count != 0)
        {
            LazyLoad(chunksToLazyLoad[0]);
            chunksToLazyLoad.RemoveAt(0);
        }
        if (chunksToGenerate.Count != 0)
        {
            if (!chunks.ContainsKey(chunksToGenerate[0]))
                LazyLoad(chunksToGenerate[0]);
            chunks[chunksToGenerate[0]].UpdateMesh();
            chunksToGenerate.RemoveAt(0);
        }
    }
}
