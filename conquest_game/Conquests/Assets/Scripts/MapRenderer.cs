using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MapRenderer : MonoBehaviour
{
    public Transform lines;
    public Material lineMaterial;
    public int height;
    public int width;
    MapInfo info;
    public Dictionary<string,GameObject> areasUI;



    void GenerateLine(List<List<int>> coords, string name = "") 
    {
        GameObject line = new GameObject("Line " + name);
        line.layer = LayerMask.NameToLayer("UI");
        line.transform.SetParent(lines);
        LineRenderer lr = line.AddComponent<LineRenderer>();
        lr.loop = true;
        lr.widthMultiplier = 3.0f;
        lr.sharedMaterial = lineMaterial;

        List<Vector3> v3s = new List<Vector3>();
        foreach (List<int> coord in coords)
        {
            v3s.Add(new Vector3(coord[0], height - coord[1], 0));
        }

        lr.positionCount = v3s.Count;
        lr.SetPositions(v3s.ToArray());
    }
}

    // public Dictionary<string,GameObject> GenerateAreas(MapInfo mapInfo)
    // {
    //     info = mapInfo;
    //     areasUI = new Dictionary<string,GameObject>();

    //     //Generated mapInfo contains two master lists
    //     //The first level are contiguous hulls, some areas may have more than one (e.g. bunch of islands)
    //     //The second level is the list of coordinates/triangles
    //     //The third level are the size 2 list of coordinate values (xy, origin is top left) 
    //     foreach (string id in info.ID.Values) 
    //     {

    //         //Create the new game object that will hold the mesh
    //         GameObject area = new GameObject(info.name[id]);
    //         areasUI[id] = area;
    //         area.layer = LayerMask.NameToLayer("UI");
    //         area.transform.SetParent(areas);
    //         MeshFilter mf = area.AddComponent<MeshFilter>();
    //         MeshRenderer mr = area.AddComponent<MeshRenderer>();
    //         MeshCollider mc = area.AddComponent<MeshCollider>();
    //         mc.convex = false;
    //         mr.sharedMaterial = areaMaterial;
    //         mf.mesh = new Mesh();

    //         //For each hull, generate the mesh using the coordinate and triangle arrays
    //         //Then combine them using CombineMeshes (to handle cases with multiple hulls)
    //         List<List<List<int>>> perimeterList = info.meshEdges[id];
    //         List<List<List<int>>> triangleSuperList = info.meshTriangles[id];
    //         CombineInstance[] combine = new CombineInstance[perimeterList.Count];
    //         for (int i = 0; i < perimeterList.Count; i++) 
    //         {
    //             List<List<int>> perimeter = perimeterList[i];
    //             List<List<int>> triangleList = triangleSuperList[i];

    //             Mesh mesh = GenerateMesh(perimeter, triangleList);
    //             combine[i].mesh = mesh;
    //             combine[i].transform = mf.transform.localToWorldMatrix;
    //         }
    //         mf.mesh.CombineMeshes(combine);

    //         //Repeat above for the collider mesh
    //         List<List<List<int>>> colPerimeterList = info.colliderEdges[id];
    //         List<List<List<int>>> colTriangleSuperList = info.colliderTriangles[id];
    //         CombineInstance[] colCombine = new CombineInstance[colPerimeterList.Count];
    //         for (int i = 0; i < colPerimeterList.Count; i++) 
    //         {
    //             List<List<int>> perimeter = colPerimeterList[i];
    //             List<List<int>> triangleList = colTriangleSuperList[i];

    //             Mesh mesh = GenerateMesh(perimeter, triangleList);
    //             colCombine[i].mesh = mesh;
    //             colCombine[i].transform = mc.transform.localToWorldMatrix;
    //         }
    //         mc.sharedMesh = new Mesh();
    //         mc.sharedMesh.CombineMeshes(colCombine);
    //         // mf.mesh = new Mesh();
    //         // mf.mesh.CombineMeshes(colCombine);
            
    //         mr.enabled = false;
    //         //For some reason to get the colliders to work, have to turn them off and on again...
    //         mc.enabled = false;
    //         mc.enabled = true;

    //         //Generate lines for the edges
    //         List<List<List<int>>> edgeList = info.meshEdges[id];
    //         foreach (List<List<int>> perimeter in edgeList)
    //         {
    //             GenerateLine(perimeter);
    //         }
    //     }

    //     return areasUI;
    // }

    // Mesh GenerateMesh(List<List<int>> perimeter, List<List<int>> triangleList) 
    // {
    //     //perimeter from map_info comes as list of coordinates
    //     Mesh mesh = new Mesh();
    //     List<Vector3> verticesList = new List<Vector3>();

    //     //Coordinate system has origin at top-left, need to translate into Unity coords (bottom-left)
    //     foreach (List<int> coord in perimeter)
    //     {
    //         verticesList.Add(new Vector3(coord[0], height - coord[1], 0));
    //     }
    //     mesh.vertices = verticesList.ToArray();

    //     //triangleList from map_info comes as list of triples of integers, need to flatten into a 1d list
    //     List<int> triangles = new List<int>();
    //     foreach (List<int> triangleTrio in triangleList)
    //     {
    //         foreach (int triangleInt in triangleTrio)
    //         {
    //             triangles.Add(triangleInt);
    //         }
    //     }
    //     mesh.triangles = triangles.ToArray();
    //     mesh.RecalculateNormals();

    //     return mesh;
    // }