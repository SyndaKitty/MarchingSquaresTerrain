using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class VoxelTerrain : MonoBehaviour
{
    MeshFilter meshFilter;
    PolygonCollider2D collider;

    const int TerrainSize = 20;

    // Terrain stores a 2D array of points (voxels). Each voxel is considered
    // solid if it is > 0. Every 4 voxels in a square make up a voxel configuration
    // that is used to generate a mesh and collider.
    float[,] terrain = new float[TerrainSize,TerrainSize];

    // For looping through adjacent voxels
    Vector2Int[] offsets =
    {
        new Vector2Int(0, 0),
        new Vector2Int(1, 0),
        new Vector2Int(1, 1),
        new Vector2Int(0, 1),
    };
    
    // The 4 cardinal and 4 diagonal points for a given voxel
    // Used when constructing the mesh
    // basePoints[pointIndex] -> point position
    Vector3[] basePoints = new[]
    {
        new Vector3(0   , 0   , 0),
        new Vector3(1   , 0   , 0),
        new Vector3(0   , 1   , 0),
        new Vector3(1   , 1   , 0),
        new Vector3(0.5f, 0,    0),
        new Vector3(0   , 0.5f, 0),
        new Vector3(1   , 0.5f, 0),
        new Vector3(0.5f, 1, 0)
    };
    
    // The index of the points for a given voxel configuration
    // pointLookup[configuration,pointNumber] -> pointIndex in basePoints
    int[,] pointLookup = new[,]
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
    int[,] indexLookup = new[,]
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
    // edgeConnections[configuration, edgeInput] -> edgeOutput
    int[,] edgeConnections = new[,]
    {
        { -1,-1,-1,-1 },
        {  1, 0,-1,-1 },
        {  2,-1, 0,-1 },
        { -1, 2, 1,-1 },
        { -1,-1, 3, 2 },
        {  2, 3, 0, 1 },
        {  3,-1,-1, 0 },
        { -1, 3,-1, 1 },
        { -1, 3,-1, 1 },
        {  3,-1,-1, 0 },
        {  1, 0, 3, 2 },
        { -1,-1, 3, 2 },
        { -1, 2, 1,-1 },
        {  2,-1, 0,-1 },
        {  1, 0,-1,-1 },
        { -1,-1,-1,-1 }
    };

    // Lookup to determine where an edge leads
    // edgeOffset[edge] -> voxel offset
    Vector2Int[] edgeOffset = new[]
    {
        new Vector2Int(0, -1),
        new Vector2Int(-1, 0),
        new Vector2Int(1, 0),
        new Vector2Int(0, 1),
    };
    
    void Start()
    {
        meshFilter = GetComponent<MeshFilter>();
        collider = GetComponent<PolygonCollider2D>();

        // Generate random terrain
        for (int x = 0; x < TerrainSize; x++)
        {
            for (int y = 0; y < TerrainSize; y++)
            {
                if (x == 0 || y == 0 || x == TerrainSize - 1 || y == TerrainSize - 1)
                {
                    terrain[x, y] = -1;
                }
                else terrain[x, y] = Random.Range(-1f, 0.7f);
            }
        }

        CalculateMesh();
    }

    void OnDrawGizmosSelected()
    {
        // Give a representation of the terrain
        for (int y = 0; y < TerrainSize; y++)
        {
            for (int x = 0; x < TerrainSize; x++)
            {
                if (terrain[x, y] > 0)
                {
                    Gizmos.color = Color.white;
                }
                else
                {
                    Gizmos.color = Color.black;
                }
                Gizmos.DrawSphere(new Vector3(x, y, 0), 0.1f);
            }
        }
    }

    void CalculateMesh()
    {
        // Mesh info
        List<Vector3> points = new List<Vector3>();
        List<int> indices = new List<int>();

        Vector3 offsetPoint = Vector3.zero;
        int indexOffset = 0;
        
        // Track which voxel configurations have edges
        Dictionary<Vector2Int, int> edgePoints = new Dictionary<Vector2Int, int>();
        // Remember the configuration(lookup) for each voxel square
        int[,] lookupGrid = new int[TerrainSize,TerrainSize];

        for (int y = 0; y < TerrainSize - 1; y++)
        {
            for (int x = 0; x < TerrainSize - 1; x++)
            {
                offsetPoint.x = x;
                offsetPoint.y = y;
                
                int lookup = 0;
                // Calculate lookup
                for (int offset = 0; offset < 4; offset++)
                {
                    var o = offsets[offset];
                    lookup += terrain[x + o.x, y + o.y] > 0f ? (1 << offset) : 0;
                }

                lookupGrid[x, y] = lookup;
                if (lookup > 0 && lookup < 15)
                {
                    edgePoints.Add(new Vector2Int(x, y), lookup == 5 || lookup == 10 ? 2 : 1);
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
        collider.pathCount = 0;
        
        while (edgePoints.Any())
        {
            List<Vector2> pathPoints = new List<Vector2>();
            
            // Starting at the first point in the queue
            var point = edgePoints.Keys.First();
            
            int lookup = lookupGrid[point.x, point.y];
            int nextConnection = -1;

            edgePoints[point] = edgePoints[point] - 1;
            if (edgePoints[point] <= 0) edgePoints.Remove(point);
            
            // Find the first valid edge for the point
            for (int i = 0; i < 4; i++)
            {
                int connection = edgeConnections[lookup, i];
                // Valid connection that leads to another edge
                if (connection >= 0 && edgePoints.ContainsKey(point + edgeOffset[connection]))
                {
                    pathPoints.Add(basePoints[i + 4] + (Vector3Int)point);
                    nextConnection = connection;
                    break;
                }
            }

            point += edgeOffset[nextConnection];
            
            // Keep following the edges that are connected to our starting point
            // until we reach our starting again
            while (edgePoints.ContainsKey(point))
            {
                edgePoints[point] = edgePoints[point] - 1;
                if (edgePoints[point] <= 0) edgePoints.Remove(point);
                
                // The 'up' edge from the voxel below us is our 'down' edge. Same with left/right, etc.
                // This is a nice trick to do this conversion
                int incomingEdge = 3 - nextConnection;
                
                // Add 4 to get the cardinal points instead of diagonals
                pathPoints.Add(basePoints[incomingEdge + 4] + (Vector3Int)point);
                
                lookup = lookupGrid[point.x, point.y];
                nextConnection = edgeConnections[lookup, incomingEdge];
                var nextPoint = point + edgeOffset[nextConnection];

                point = nextPoint;
            }
            
            collider.pathCount++;
            collider.SetPath(collider.pathCount - 1, pathPoints);
            pathPoints.Clear();
        }
    }
}
