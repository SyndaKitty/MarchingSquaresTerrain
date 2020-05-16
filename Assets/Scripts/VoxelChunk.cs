using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEngine;

public class VoxelChunk : MonoBehaviour
{
    public const int Size = 10;

    // Terrain stores a 2D array of points (voxels). Each voxel is considered
    // solid if it is > 0. Every 4 voxels in a square make up a voxel configuration
    // that is used to generate a mesh and collider.
    public float[,] Terrain = new float[Size,Size];
    public Vector2Int ChunkCoord;

    void OnDrawGizmos()
    {
        // Give a representation of the terrain
        for (int y = 0; y < Size; y++)
        {
            for (int x = 0; x < Size; x++)
            {
                Gizmos.color = Terrain[x, y] > 0 ? Color.white : Color.black;
                Gizmos.DrawSphere(new Vector3(x, y, 0) + transform.position, 0.1f);
            }
        }

        Gizmos.color = Color.white;
        for (int i = 0; i < pathPoints.Count - 1; i++)
        {
            Gizmos.DrawLine((Vector3)pathPoints[i] + transform.position, (Vector3)pathPoints[i + 1] + transform.position);
        }

        Gizmos.color = new Color(1, 0, 0, 0.2f);
        foreach (var point in edgePoints.Keys)
        {
            Gizmos.DrawCube(transform.position + (Vector3Int)point, Vector3.one);
        }
    }

    // For looping through adjacent voxels
    static Vector2Int[] offsets =
    {
        new Vector2Int(0, 0),
        new Vector2Int(1, 0),
        new Vector2Int(1, 1),
        new Vector2Int(0, 1),
    };
    
    // The 4 cardinal, 4 diagonal and center points for a given voxel
    // Used when constructing the mesh
    // basePoints[pointIndex] -> point position
    static Vector3[] basePoints = new[]
    {
        new Vector3(0   , 0   , 0),
        new Vector3(1   , 0   , 0),
        new Vector3(0   , 1   , 0),
        new Vector3(1   , 1   , 0),
        new Vector3(0.5f, 0,    0),
        new Vector3(0   , 0.5f, 0),
        new Vector3(1   , 0.5f, 0),
        new Vector3(0.5f, 1,    0),
        new Vector3(0.5f, 0.5f, 0)
    };
    
    // The index of the points for a given voxel configuration
    // pointLookup[configuration,pointNumber] -> pointIndex in basePoints
    static int[,] pointLookup = new[,]
    {
        { -1,-1,-1,-1,-1,-1 },
        {  0, 5, 4,-1,-1,-1 },
        {  1, 4, 6,-1,-1,-1 },
        {  0, 5, 6, 1,-1,-1 },
        {  3, 6, 7,-1,-1,-1 },
        {  0, 5, 7, 3, 6, 4 },
        {  4, 7, 3, 1,-1,-1 },
        {  0, 5, 7, 3, 1,-1 },
        {  5, 2, 7,-1,-1,-1 },
        {  0, 2, 7, 4,-1,-1 },
        {  2, 7, 6, 1, 4, 5 },
        {  2, 7, 6, 1, 0,-1 },
        {  2, 3, 6, 5,-1,-1 },
        {  2, 3, 6, 4, 0,-1 },
        {  2, 3, 1, 4, 5,-1 },
        {  0, 2, 3, 1,-1,-1 }
    };

    // The mesh triangle indices to use for a given voxel configuration
    // Used to tell the mesh how the triangles are constructed
    // indexLookup[configuration, indexNum] -> meshTriangleIndex
    static int[,] indexLookup = new[,]
    {
        { -1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1 },
        {  0, 1, 2,-1,-1,-1,-1,-1,-1,-1,-1,-1 },
        {  0, 1, 2,-1,-1,-1,-1,-1,-1,-1,-1,-1 },
        {  0, 1, 2, 0, 2, 3,-1,-1,-1,-1,-1,-1 },
        {  0, 1, 2,-1,-1,-1,-1,-1,-1,-1,-1,-1 },
        {  0, 1, 2, 0, 2, 3, 0, 3, 4, 0, 4, 5 },
        {  0, 1, 2, 0, 2, 3,-1,-1,-1,-1,-1,-1 },
        {  0, 1, 2, 0, 2, 3, 0, 3, 4,-1,-1,-1 },
        {  0, 1, 2,-1,-1,-1,-1,-1,-1,-1,-1,-1 },
        {  0, 1, 2, 0, 2, 3,-1,-1,-1,-1,-1,-1 },
        {  0, 1, 2, 0, 2, 3, 0, 3, 4, 0, 4, 5 },
        {  0, 1, 2, 0, 2, 3, 0, 3, 4,-1,-1,-1 },
        {  0, 1, 2, 0, 2, 3,-1,-1,-1,-1,-1,-1 },
        {  0, 1, 2, 0, 2, 3, 0, 3, 4,-1,-1,-1 },
        {  0, 1, 2, 0, 2, 3, 0, 3, 4,-1,-1,-1 },
        {  0, 1, 2, 0, 2, 3,-1,-1,-1,-1,-1,-1 }
    };
    
    // Lookup to determine which edge connect to which given a voxel configuration
    // 0: down, 1: left, 2: right, 3: up
    // edgeConnections[chunkEdge, configuration, edgeInput] -> edgeOutput
    static int[,,] edgeConnections = new[,,]
    {
        {
            // Chunk Center 
            { -1,-1,-1,-1 }, {  1, 0,-1,-1 }, {  2,-1, 0,-1 }, { -1, 2, 1,-1 },
            { -1,-1, 3, 2 }, {  2, 3, 0, 1 }, {  3,-1,-1, 0 }, { -1, 3,-1, 1 },
            { -1, 3,-1, 1 }, {  3,-1,-1, 0 }, {  1, 0, 3, 2 }, { -1,-1, 3, 2 },
            { -1, 2, 1,-1 }, {  2,-1, 0,-1 }, {  1, 0,-1,-1 }, { -1,-1,-1,-1 }
        },
        {
            // Chunk Bottom
            { -1,-1,-1,-1 }, { -1,-1,-1,-1 }, { -1,-1,-1,-1 }, { -1,-1,-1,-1 },
            { -1,-1, 3, 2 }, { -1,-1, 3, 2 }, { -1,-1, 3, 2 }, { -1,-1, 3, 2 },
            { -1, 3,-1, 1 }, { -1, 3,-1, 1 }, { -1, 3,-1, 1 }, { -1, 3,-1, 1 },
            { -1, 2, 1,-1 }, { -1, 2, 1,-1 }, { -1, 2, 1,-1 }, { -1, 2, 1,-1 }
        },
        {
            // Chunk Left
            { -1,-1,-1,-1 }, { -1,-1,-1,-1 }, {  2,-1, 0,-1 }, {  2,-1, 0,-1 },
            { -1,-1, 3, 2 }, { -1,-1, 3, 2 }, {  3,-1,-1, 0 }, {  3,-1,-1, 0 },
            { -1,-1,-1,-1 }, { -1,-1,-1,-1 }, {  2,-1, 0,-1 }, {  2,-1, 0,-1 },
            { -1,-1, 3, 2 }, { -1,-1, 3, 2 }, {  3,-1,-1, 0 }, {  3,-1,-1, 0 }
        },
        {
            // Chunk Right
            { -1,-1,-1,-1 }, {  1, 0,-1,-1 }, { -1,-1,-1,-1 }, {  1, 0,-1,-1 },
            { -1,-1,-1,-1 }, {  1, 0,-1,-1 }, { -1,-1,-1,-1 }, {  1, 0,-1,-1 },
            { -1, 3,-1, 1 }, {  3,-1,-1, 0 }, { -1, 3,-1, 1 }, {  3,-1,-1, 0 },
            { -1, 3,-1, 1 }, {  3,-1,-1, 0 }, { -1, 3,-1, 1 }, {  3,-1,-1, 0 }
        },
        {
            // Chunk Up
            { -1,-1,-1,-1 }, {  1, 0,-1,-1 }, {  2,-1, 0,-1 }, { -1, 2, 1,-1 },
            { -1,-1,-1,-1 }, {  1, 0,-1,-1 }, {  2,-1, 0,-1 }, { -1, 2, 1,-1 },
            { -1,-1,-1,-1 }, {  1, 0,-1,-1 }, {  2,-1, 0,-1 }, { -1, 2, 1,-1 },
            { -1,-1,-1,-1 }, {  1, 0,-1,-1 }, {  2,-1, 0,-1 }, { -1, 2, 1,-1 }
        },
        {
            // Chunk Bottom-left
            { -1,-1,-1,-1 }, { -1,-1,-1,-1 }, { -1,-1,-1,-1 }, { -1,-1,-1,-1 },
            { -1,-1, 3, 2 }, { -1,-1, 3, 2 }, { -1,-1, 3, 2 }, { -1,-1, 3, 2 },
            { -1,-1,-1,-1 }, { -1,-1,-1,-1 }, { -1,-1,-1,-1 }, { -1,-1,-1,-1 },
            { -1,-1, 3, 2 }, { -1,-1, 3, 2 }, { -1,-1, 3, 2 }, { -1,-1, 3, 2 }
        },
        {
            // Chunk Bottom-right
            { -1,-1,-1,-1 }, { -1,-1,-1,-1 }, { -1,-1,-1,-1 }, { -1,-1,-1,-1 },
            { -1,-1,-1,-1 }, { -1,-1,-1,-1 }, { -1,-1,-1,-1 }, { -1,-1,-1,-1 },
            {  3,-1,-1, 0 }, {  3,-1,-1, 0 }, {  3,-1,-1, 0 }, {  3,-1,-1, 0 },
            {  3,-1,-1, 0 }, {  3,-1,-1, 0 }, {  3,-1,-1, 0 }, {  3,-1,-1, 0 }
        },
        {
            // Chunk Top-left
            { -1,-1,-1,-1 }, { -1,-1,-1,-1 }, {  2,-1, 0,-1 }, {  2,-1, 0,-1 },
            { -1,-1,-1,-1 }, { -1,-1,-1,-1 }, {  2,-1, 0,-1 }, {  2,-1, 0,-1 },
            { -1,-1,-1,-1 }, { -1,-1,-1,-1 }, {  2,-1, 0,-1 }, {  2,-1, 0,-1 },
            { -1,-1,-1,-1 }, { -1,-1,-1,-1 }, {  2,-1, 0,-1 }, {  2,-1, 0,-1 }
        },
        {
            // Chunk Top-right
            { -1,-1,-1,-1 }, {  1, 0,-1,-1 }, { -1,-1,-1,-1 }, {  1, 0,-1,-1 },
            { -1,-1,-1,-1 }, {  1, 0,-1,-1 }, { -1,-1,-1,-1 }, {  1, 0,-1,-1 },
            { -1,-1,-1,-1 }, {  1, 0,-1,-1 }, { -1,-1,-1,-1 }, {  1, 0,-1,-1 },
            { -1,-1,-1,-1 }, {  1, 0,-1,-1 }, { -1,-1,-1,-1 }, {  1, 0,-1,-1 }
        },
    };

    //  7   4   8
    //   |-----|
    // 2 |  0  | 3 
    //   |-----|
    //  5   1   6
    static int[] ChunkEdgeLookup = new[] { 5, 1, 6, 2, 0, 3, 7, 4, 8};
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    int ChunkEdgeIndex(int x, int y) => ChunkEdgeLookup[
        Mathf.CeilToInt((float)x / (Size - 1)) + 
        Mathf.CeilToInt((float)y / (Size - 1)) * 3];

    // Probably can avoid this lookup table by just adding all chunk edges, then checking for -1 when following edges
    // int[,] ValidEdgeLookup = new[]
    // {
    //     { 0, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 0 }, // center
    //     { }, // bottom
    //     { }, // left
    //     { }, // right
    //     { }, // up
    //     { }, // bottom-left
    //     { }, // bottom-right
    //     { }, // top-left
    //     { }  // top-right
    // };
    
    // Lookup to determine where an edge leads
    // edgeOffset[edge] -> voxel offset
    static Vector2Int[] edgeOffset = new[]
    {
        new Vector2Int(0, -1),
        new Vector2Int(-1, 0),
        new Vector2Int(1, 0),
        new Vector2Int(0, 1),
    };
    
    List<Vector2> pathPoints = new List<Vector2>();
    Dictionary<Vector2Int, int> edgePoints = new Dictionary<Vector2Int, int>();
    public IEnumerator GenerateMesh(VoxelTerrain voxelTerrain)
    {
        MeshFilter meshFilter = GetComponent<MeshFilter>();
        PolygonCollider2D polygonCollider = GetComponent<PolygonCollider2D>();

        // Mesh info
        List<Vector3> points = new List<Vector3>();
        List<int> indices = new List<int>();

        Vector3 offsetPoint = Vector3.zero;
        int indexOffset = 0;
        
        // Track which voxel configurations have edges
        //Dictionary<Vector2Int, int> edgePoints = new Dictionary<Vector2Int, int>();
        // Remember the configuration(lookup) for each voxel square
        int[,] lookupGrid = new int[Size,Size];
        float[,] expandedTerrain = new float[Size, Size];
        int[,] chunkEdgeLookup = new int[Size, Size];

        for (int y = 0; y < Size; y++)
        {
            for (int x = 0; x < Size; x++)
            {
                int lookup = 0;

                if (x < Size - 1 && y < Size - 1)
                {
                    for (int offset = 0; offset < 4; offset++)
                    {
                        var o = offsets[offset];
                        var v = Terrain[x + o.x, y + o.y];
                        expandedTerrain[x, y] = v;
                        lookup += v > 0f ? (1 << offset) : 0;
                    }
                }
                else
                {
                    for (int offset = 0; offset < 4; offset++)
                    {
                        var o = offsets[offset];
                        var v = voxelTerrain.GetVoxel(
                            ChunkCoord.x * Size + x + o.x, 
                            ChunkCoord.y * Size + y + o.y
                        );
                        expandedTerrain[x, y] = v;
                        lookup += v > 0f ? (1 << offset) : 0;
                    }
                }

                lookupGrid[x, y] = lookup;
                chunkEdgeLookup[x, y] = ChunkEdgeIndex(x, y);
            }
        }


        for (int y = 0; y < Size; y++)
        {
            for (int x = 0; x < Size; x++)
            {
                offsetPoint.x = x;
                offsetPoint.y = y;

                int lookup = lookupGrid[x, y];
                int chunkEdge = chunkEdgeLookup[x, y];

                if (x < Size - 1 && y < Size - 1 && (lookup > 0 && lookup < 15 || chunkEdge != 0))
                {
                    edgePoints.Add(new Vector2Int(x, y), chunkEdge == 0 && (lookup == 5 || lookup == 10) ? 2 : 1);
                }

                // Add points
                int tempIndexOffset = 0;
                for (int i = 0; i < 6; i++)
                {
                    int p = pointLookup[lookup, i];
                    if (p < 0) break;
                    points.Add(basePoints[p] + offsetPoint);
                    tempIndexOffset++;
                }

                // Add indices
                for (int i = 0; i < 12; i++)
                {
                    int index = indexLookup[lookup, i];
                    if (index < 0) break;
                    indices.Add(index + indexOffset);
                }
                indexOffset += tempIndexOffset;
            }
        }
        
        var mesh = meshFilter.mesh;
        mesh.Clear();
        mesh.vertices = points.ToArray();
        mesh.triangles = indices.ToArray();
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        
        // Calculate polygon collider
        polygonCollider.pathCount = 0;
        
        while (edgePoints.Any())
        {
            // List<Vector2> pathPoints = new List<Vector2>();
            
            // Starting at the first point in the queue
            var point = edgePoints.Keys.First();
            var startingPoint = point;

            int lookup = lookupGrid[point.x,point.y];
            int nextConnection = -1;

            edgePoints[point] = edgePoints[point] - 1;
            if (edgePoints[point] <= 0) edgePoints.Remove(point);
            
            // Find the first valid edge for the point
            for (int i = 0; i < 4; i++)
            {
                int chunkEdge = ChunkEdgeIndex(point.x, point.y);
                int connection = edgeConnections[chunkEdge, lookup, i];
                // Valid connection that leads to another edge
                if (connection >= 0 && edgePoints.ContainsKey(point + edgeOffset[connection]))
                {
                    pathPoints.Add(basePoints[i + 4] + (Vector3Int)point);
                    nextConnection = connection;
                    break;
                }
            }

            // If no valid connection, ignore this point
            if (nextConnection == -1) continue;
            
            point += edgeOffset[nextConnection];
            
            // Keep following the edges that are connected to our starting point
            // until we reach our starting again
            while (point != startingPoint)
            {
                edgePoints[point] = edgePoints[point] - 1;
                if (edgePoints[point] <= 0) edgePoints.Remove(point);
                
                // The 'up' edge from the voxel below us is our 'down' edge. Same with left/right, etc.
                // This is a nice trick to do this conversion
                int incomingEdge = 3 - nextConnection;
                
                // Add 4 to get the cardinal points instead of diagonals
                pathPoints.Add(basePoints[incomingEdge + 4] + (Vector3Int)point);
                yield return new WaitForSeconds(0.5f);
                
                int chunkEdge = ChunkEdgeIndex(point.x, point.y);
                lookup = lookupGrid[point.x,point.y];
                nextConnection = edgeConnections[chunkEdge, lookup, incomingEdge];
                var nextPoint = point + edgeOffset[nextConnection];

                point = nextPoint;
            }
            
            polygonCollider.SetPath(polygonCollider.pathCount++, pathPoints);
            pathPoints.Clear();
        }
    }
}
