using System;
using System.Collections.Generic;
using UnityEngine;

public class VoxelTerrain : MonoBehaviour
{
    public GameObject ChunkTemplate;
    
    Dictionary<Vector2Int, VoxelChunk> chunks = new Dictionary<Vector2Int, VoxelChunk>();

    void Start()
    {
        List<TerrainSet> sets = new List<TerrainSet>();
        for (int y = -20; y <= 20; y++)
        {
            for (int x = -20; x <= 20; x++)
            {
                 sets.Add(new TerrainSet
                 {
                     X = x,
                     Y = y,
                     Value = 15 - (new Vector2(x, y) - Vector2.zero).magnitude
                 });
            }
        }

        SetTerrain(sets);
    }

    public void SetTerrain(List<TerrainSet> terrainSets)
    {
        HashSet<Vector2Int> alteredChunks = new HashSet<Vector2Int>();
        foreach (var terrainSet in terrainSets)
        {
            var chunkCoord = new Vector2Int(Mathf.FloorToInt((float)terrainSet.X / VoxelChunk.Size), Mathf.FloorToInt((float)terrainSet.Y / VoxelChunk.Size));
            var blockCoord = new Vector2Int(terrainSet.X, terrainSet.Y) - chunkCoord * VoxelChunk.Size;
            var c = GetChunk(chunkCoord);
            c.Terrain[blockCoord.x, blockCoord.y] = terrainSet.Value;
            alteredChunks.Add(chunkCoord);

            // Temp code.. REMOVE
            //if (blockCoord.x == 0)
            //    c.Terrain[blockCoord.x, blockCoord.y] = -1;
            //if (blockCoord.y == 0)
            //    c.Terrain[blockCoord.x, blockCoord.y] = -1;
            //if (blockCoord.x == VoxelChunk.Size - 1)
            //    c.Terrain[blockCoord.x, blockCoord.y] = -1;
            //if (blockCoord.y == VoxelChunk.Size - 1)
            //    c.Terrain[blockCoord.x, blockCoord.y] = -1;


            if (blockCoord.x == 0)
                alteredChunks.Add(chunkCoord + Vector2Int.left);
            if (blockCoord.x == VoxelChunk.Size - 1)
                alteredChunks.Add(chunkCoord + Vector2Int.right);
            if (blockCoord.y == 0)
                alteredChunks.Add(chunkCoord + Vector2Int.down);
            if (blockCoord.y == VoxelChunk.Size - 1)
                alteredChunks.Add(chunkCoord + Vector2Int.up);
        }

        foreach (var alteredChunk in alteredChunks)
        {
            var c = GetChunk(alteredChunk);
            StartCoroutine(c.GenerateMesh(this));
        }
    }

    public float GetVoxel(int voxelX, int voxelY)
    {
        var t = GetChunk(voxelX, voxelY, out var cc).Terrain;
        return t[voxelX - cc.x * VoxelChunk.Size, voxelY - cc.y * VoxelChunk.Size];
    }

    VoxelChunk GetChunk(int voxelX, int voxelY, out Vector2Int cc)
    {
        cc = new Vector2Int(
            Mathf.FloorToInt((float) voxelX / VoxelChunk.Size),
            Mathf.FloorToInt((float) voxelY / VoxelChunk.Size)
        );

        return GetChunk(cc);
    }

    VoxelChunk GetChunk(Vector2Int chunkCoord)
    {
        if (chunks.TryGetValue(chunkCoord, out var chunk))
        {
            return chunk;
        }

        return CreateChunk(chunkCoord);
    }

    VoxelChunk CreateChunk(Vector2Int chunkCoords)
    {
        var chunk = Instantiate(ChunkTemplate);

        chunk.transform.localPosition = (Vector3Int)chunkCoords * VoxelChunk.Size;

        var vc = chunk.GetComponent<VoxelChunk>();
        vc.ChunkCoord = chunkCoords;
        chunks[chunkCoords] = vc;

        return vc;
    }
    
}

public struct TerrainSet
{
    public int X;
    public int Y;
    public float Value;
}
