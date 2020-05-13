using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class VoxelTerrain : MonoBehaviour
{
    MeshFilter meshFilter;
    PolygonCollider2D collider;

    const int MeshSize = 20;
    float[,] terrain = new float[MeshSize,MeshSize];

    Vector2Int[] offsets =
    {
        new Vector2Int(0, 0),
        new Vector2Int(1, 0),
        new Vector2Int(1, 1),
        new Vector2Int(0, 1),
    };
    
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

    int[] firstEdgeConnection = new[]
    {
        -1,
        0,
        0,
        1,
        2,
        0,
        0,
        1,
        1,
        0,
        0,
        2,
        1,
        0,
        0,
        -1
    };
    
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

        for (int x = 0; x < MeshSize; x++)
        {
            for (int y = 0; y < MeshSize; y++)
            {
                if (x == 0 || y == 0 || x == MeshSize - 1 || y == MeshSize - 1)
                {
                    terrain[x, y] = -1;
                }
                else terrain[x, y] = Random.Range(-1f, 0.7f);
            }
        }

        terrain[0, 0] = 0;
        terrain[1, 0] = 0;
        terrain[2, 0] = 0;
        terrain[3, 0] = 0;
        terrain[4, 0] = 0;
        terrain[5, 0] = 0;
        terrain[6, 0] = 0;

        terrain[0, 1] = 0;
        terrain[1, 1] = 1;
        terrain[2, 1] = 0;
        terrain[3, 1] = 1;
        terrain[4, 1] = 1;
        terrain[5, 1] = 0;
        terrain[6, 1] = 0;

        terrain[0, 2] = 0;
        terrain[1, 2] = 1;
        terrain[2, 2] = 1;
        terrain[3, 2] = 1;
        terrain[4, 2] = 0;
        terrain[5, 2] = 1;
        terrain[6, 2] = 0;

        terrain[0, 3] = 0;
        terrain[1, 3] = 0;
        terrain[2, 3] = 1;
        terrain[3, 3] = 1;
        terrain[4, 3] = 1;
        terrain[5, 3] = 0;
        terrain[6, 3] = 0;

        terrain[0, 4] = 0;
        terrain[1, 4] = 0;
        terrain[2, 4] = 0;
        terrain[3, 4] = 1;
        terrain[4, 4] = 1;
        terrain[5, 4] = 0;
        terrain[6, 4] = 0;

        terrain[0, 5] = 0;
        terrain[1, 5] = 1;
        terrain[2, 5] = 0;
        terrain[3, 5] = 0;
        terrain[4, 5] = 0;
        terrain[5, 5] = 1;
        terrain[6, 5] = 0;

        terrain[0, 6] = 0;
        terrain[1, 6] = 0;
        terrain[2, 6] = 0;
        terrain[3, 6] = 0;
        terrain[4, 6] = 0;
        terrain[5, 6] = 0;
        terrain[6, 6] = 0;

        //terrain[2, 1] = 1;
        //terrain[1, 2] = 1;
        //terrain[3, 2] = 1;
        //terrain[2, 3] = 1;
        
        StartCoroutine(CalculateMesh());
    }
    
    void Update()
    {
        
    }

    void OnDrawGizmos()
    {
        for (int y = 0; y < MeshSize; y++)
        {
            for (int x = 0; x < MeshSize; x++)
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

        Gizmos.color = Color.white;
        for (int i = 0; i < pathPoints.Count - 1; i++)
        {
            Gizmos.DrawLine(pathPoints[i], pathPoints[i + 1]);
        }
        
        foreach (var key in edgePoints.Keys)
        {
            Gizmos.color = new Color(1, 0.2f, 0.2f, edgePoints[key] * 0.2f);
            Gizmos.DrawCube((Vector3Int)key + Vector3.one * 0.5f, Vector3.one);
        }
    }

    List<Vector2> pathPoints = new List<Vector2>();
    Dictionary<Vector2Int, int> edgePoints = new Dictionary<Vector2Int, int>();
    
    IEnumerator CalculateMesh()
    {
        List<Vector3> points = new List<Vector3>();
        List<int> indices = new List<int>();

        Vector3 offsetPoint = Vector3.zero;
        int indexOffset = 0;
        
        // Dictionary<Vector2Int, int> edgePoints = new Dictionary<Vector2Int, int>();
        int[,] lookupGrid = new int[MeshSize,MeshSize];

        Mesh mesh;

        for (int y = 0; y < MeshSize - 1; y++)
        {
            for (int x = 0; x < MeshSize - 1; x++)
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

                mesh = meshFilter.mesh;
                mesh.Clear();
                mesh.vertices = points.ToArray();
                mesh.triangles = indices.ToArray();
                mesh.RecalculateNormals();
                mesh.RecalculateBounds();
                yield return new WaitForSeconds(.01f);
            }
        }
        
        mesh = meshFilter.mesh;
        mesh.Clear();
        mesh.vertices = points.ToArray();
        mesh.triangles = indices.ToArray();
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        
        
        // Calculate polygon collider
        collider.pathCount = 0;
        
        while (edgePoints.Any())
        {
            // List<Vector2> pathPoints = new List<Vector2>();
            
            // Starting at the first point in the queue
            var point = edgePoints.Keys.First();
            
            int lookup = lookupGrid[point.x, point.y];
            int nextConnection = -1;

            edgePoints[point] = edgePoints[point] - 1;
            if (edgePoints[point] <= 0) edgePoints.Remove(point);
            
            // Find the first valid edge for the point
            int fromEdge = firstEdgeConnection[lookup];
            
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
            
            while (edgePoints.ContainsKey(point))
            {
                edgePoints[point] = edgePoints[point] - 1;
                if (edgePoints[point] <= 0) edgePoints.Remove(point);
                
                int incomingEdge = 3 - nextConnection;
                
                pathPoints.Add(basePoints[incomingEdge + 4] + (Vector3Int)point);
                
                lookup = lookupGrid[point.x, point.y];
                nextConnection = edgeConnections[lookup, incomingEdge];
                var nextPoint = point + edgeOffset[nextConnection];

                if (!edgePoints.ContainsKey(point))
                {
                    pathPoints.Add(basePoints[nextConnection + 4] + (Vector3Int)point);
                }

                point = nextPoint;

                yield return new WaitForSeconds(0.03f);
            }
            
            collider.pathCount++;
            collider.SetPath(collider.pathCount - 1, pathPoints);
            pathPoints.Clear();
        }
    }
}
