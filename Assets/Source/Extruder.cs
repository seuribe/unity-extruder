using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using System.Linq;

[ExecuteInEditMode]
public class Extruder : MonoBehaviour {

    [SerializeField]
    private Outline outline;
    public Outline Outline
    {
        get
        {
            return outline;
        }
        set
        {
            outline = value;
            PrepareVertices();
        }
    }

    [SerializeField]
    private ExtrudePath path;
    public ExtrudePath ExtrudePath
    {
        get
        {
            return path;
        }
        set
        {
            this.path = value;
            PrepareVertices();
        }
    }
    public Material material;
    public bool invertTop = false;
    public bool invertBottom = false;
    public bool invertSides = false;

    public bool generateCollider = false;
    public bool generateOnEditor = false;

    // Mainly for making the mesh available elsewhere
    private Mesh mesh;
    public Mesh Mesh
    {
        get
        {
            return mesh;
        }
    }

    private List<Vector3> baseOutlineVertices;
    private List<List<Vector3>> stepsSideVertexList;
    private List<List<Vector3>> stepsOutlineVertexList;

    private List<Vector3> allVertices;
    private List<int> allIndices;

    public bool IsPrepared
    {
        get
        {
            return allVertices != null;
        }
    }

    // For editor
    public void OnValidate()
    {
        Debug.Log("OnValidate");
        PrepareVertices();
    }

    // For play sessions
    void Start()
    {
        Debug.Log("Start");
        PrepareVertices();
        if (Application.isPlaying)
        {
            RegenerateMesh();
        }
    }

    // Used for updating the mesh according to the transform position
    void LateUpdate()
    {
        if (!Application.isPlaying)
        {
            PrepareVertices();
        }
    }

    void RegenerateMesh() {
        if (mesh != null)
        {
            DestroyImmediate(mesh);
        }
        if (!IsPrepared && !PrepareVertices())
        {
            return;
        }

        // Create the mesh
        mesh = new Mesh();
        mesh.name = "(extruded mesh)";
        mesh.vertices = allVertices.ToArray();
        mesh.triangles = allIndices.ToArray();
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        Debug.Log("new mesh created");

        // Set up game object with mesh;
        MeshRenderer renderer = gameObject.GetComponent<MeshRenderer>() as MeshRenderer;
        if (renderer == null) 
            renderer = gameObject.AddComponent(typeof(MeshRenderer)) as MeshRenderer;

        renderer.material = material;

        MeshFilter filter = gameObject.GetComponent<MeshFilter>() as MeshFilter;
        if (filter == null)
        {
            filter = gameObject.AddComponent(typeof(MeshFilter)) as MeshFilter;
        }
        filter.mesh = mesh;

        if (generateCollider && gameObject.collider == null)
        {
            MeshCollider collider = gameObject.AddComponent<MeshCollider>() as MeshCollider;
            collider.sharedMesh = mesh;
        }
    }

    private bool PrepareVertices() {
        baseOutlineVertices = null;
        stepsSideVertexList = null;
        stepsOutlineVertexList = null;
        allVertices = null;
        allIndices = null;

        if (outline == null || path == null)
        {
            return false;
        }

		outline.Init();
        List<Vector2> flatVertices = outline.Points;

        // degenerate case: 1 or 2 vertices will make no closed path
        if (flatVertices.Count < 3)
        {
            return false;
        }

        // 1. triangulate to create top side
        Triangulator ttor = new Triangulator(flatVertices);
        var topIndices = ttor.Triangulate();

        baseOutlineVertices = flatVertices.Select(v => new Vector3(v.x, 0, -v.y)).ToList();

        // 2. duplicate and invert indices to create bottom side.
        //    Also, displace indices to match duplicated vertices index

        var bottomIndices = new int[topIndices.Length];
        Array.Copy(topIndices, bottomIndices, topIndices.Length);
        Array.Reverse(bottomIndices);

        // 3. create triangle strip around the sides
        stepsSideVertexList = new List<List<Vector3>>();
        stepsOutlineVertexList = new List<List<Vector3>>();
        var stepIndicesList = new List<int[]>();
        var lastVertexList = new List<Vector3>(baseOutlineVertices);

        foreach (var t in path.Steps)
        {
            int[] stepIndices;
            List<Vector3> stepVertices = lastVertexList.Select(v => t.TransformPoint(v)).ToList();
            List<Vector3> stepStripVertices = null;
            CreateSideTriangles(lastVertexList, stepVertices, out stepStripVertices, out stepIndices);
            stepsSideVertexList.Add(stepStripVertices);
            stepIndicesList.Add(stepIndices);
            stepsOutlineVertexList.Add(stepVertices);

            lastVertexList = new List<Vector3>(stepVertices);
        }

        // bottom vertex list


        // 4. put everything together and generate final mesh
        if (invertTop)
        {
            Array.Reverse(topIndices);
        }
        if (invertBottom)
        {
            Array.Reverse(bottomIndices);
        }
        if (invertSides)
        {
            foreach (var stepIndices in stepIndicesList)
            {
                Array.Reverse(stepIndices);
            }
        }

        allVertices = new List<Vector3>();
        allIndices = new List<int>();
        AddMeshVertices(allVertices, allIndices, baseOutlineVertices, topIndices);
        AddMeshVertices(allVertices, allIndices, lastVertexList, bottomIndices);

        for (int i = 0; i < stepsSideVertexList.Count; i++)
        {
            AddMeshVertices(allVertices, allIndices, stepsSideVertexList[i], stepIndicesList[i]);
        }
        return true;
    }

    private void CreateSideTriangles(List<Vector3> verticesA, List<Vector3> verticesB, out List<Vector3> sideVertexList, out int[] sideIndices)
    {
        sideVertexList = new List<Vector3>();
        for (int i = 0; i < verticesA.Count; i++)
        {
            var topNext = verticesA[(i + 1) % verticesA.Count];
            var bottomNext = verticesB[(i + 1) % verticesB.Count];
            sideVertexList.Add(verticesA[i]);
            sideVertexList.Add(topNext);
            sideVertexList.Add(verticesB[i]);

            sideVertexList.Add(bottomNext);
            sideVertexList.Add(verticesB[i]);
            sideVertexList.Add(topNext);
        }
        sideIndices = new int[sideVertexList.Count];
        for (int i = 0; i < sideIndices.Length; i++)
        {
            sideIndices[i] = i;
        }
    }

    void AddMeshVertices(List<Vector3> destVertices, List<int> destIndices, List<Vector3> newVertices, int[] newIndices)
    {
        int currentSize = destVertices.Count;
        destVertices.AddRange(newVertices);
        for (int i = 0; i < newIndices.Length; i++)
        {
            destIndices.Add(newIndices[i] + currentSize);
        }
    }
	
	private void DrawOutlineGizmos(List<Vector3> outlineVertices)
	{
		int n = outlineVertices.Count;
		for (int i = 0; i < n; i++)
		{
			var current = transform.TransformPoint(outlineVertices[i]);
			var next = transform.TransformPoint(outlineVertices[(i + 1) % n]);
			Gizmos.DrawLine(current, next);
		}
	}

    void OnDrawGizmos()
    {
        if (IsPrepared)
        {
            var lastVertices = baseOutlineVertices;
            DrawOutlineGizmos(lastVertices);
            int n = baseOutlineVertices.Count;
            foreach (var stepOutline in stepsOutlineVertexList)
            {
                var nextVertices = stepOutline;

                for (int i = 0; i < n; i++)
                {
                    var currentTop = transform.TransformPoint(lastVertices[i]);
                    var currentBottom = transform.TransformPoint(nextVertices[i]);

                    Gizmos.DrawLine(currentTop, currentBottom);
                }

                lastVertices = nextVertices;
                DrawOutlineGizmos(lastVertices);
            }
        }
    }
}