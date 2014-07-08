using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Xml;
using System.Xml.XPath;
using System.IO;
using System.Xml.Linq;
using System;
using System.Linq;

[ExecuteInEditMode]
public class Extruder : MonoBehaviour {

    public Material material;
    public TextAsset svgFile;
    public bool invertTop = false;
    public bool invertBottom = false;
    public bool invertSides = false;

    public bool generateCollider = false;
    public bool generateOnEditor = false;

//    public Vector3 extrude = new Vector3(0, 1, 0);

    private List<Vector3> baseOutlineVertices;
    private List<List<Vector3>> stepsSideVertexList;
    private List<List<Vector3>> stepsOutlineVertexList;

    public Mesh mesh;
    private List<Vector3> allVertices;
    private List<int> allIndices;

    public List<Transform> extrudePoints;
/*
    public Transform extrude;

    public Transform Extrude
    {
        get
        {
            return extrude;
        }
        set
        {
            if (!extrude.Equals(value))
            {
                this.extrude = value;
                Prepare();
            }
        }
    }
    */

    public bool IsPrepared
    {
        get
        {
            return baseOutlineVertices != null;
        }
    }

    public TextAsset SvgFile
    {
        get
        {
            return svgFile;
        }
        set
        {
            if (svgFile != value)
            {
                this.svgFile = value;
                Prepare();
            }
        }
    }

    // For editor
    public void OnValidate()
    {
        Debug.Log("OnValidate");
        Prepare();
    }

    // For play sessions
    void Start()
    {
        Debug.Log("Start");
        Prepare();
        if (Application.isPlaying)
        {
            Regenerate();
        }
    }

    // Used for updating the mesh according to the transform position
    void LateUpdate()
    {
        if (!Application.isPlaying)
        {
            Prepare();
        }
    }

    void Regenerate() {
        if (mesh != null)
        {
            DestroyImmediate(mesh);
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


        Debug.Log("mesh set in meshfilter");
    }

    void Prepare() {
        Debug.Log("prepare");
        baseOutlineVertices = null;
        stepsSideVertexList = null;
        stepsOutlineVertexList = null;
        allVertices = null;
        allIndices = null;
        if (svgFile == null)
        {
            Debug.Log("no SVG file!");
            return;
        }

        List<Vector2> flatVertices = GetVerticesFromSVG(svgFile.text);

        // degenerate case: 1 or 2 vertices will make no closed path
        if (flatVertices.Count < 3)
        {
            return;
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
        foreach (var t in extrudePoints)
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

        Debug.Log("prepare finished");
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

    private const string MOVE_TO = "M";
    private const string LINE_TO = "L";
    private const string HORIZONTAL_TO = "H";
    private const string VERTICAL_TO = "V";
    private const string CURVE_TO = "C";
    private const string SMOOTH_CURVE_TO = "S";
    private const string QUAD_TO = "Q";
    private const string SMOOTH_QUAD_TO = "T";
    private const string CLOSE = "Z";

    private enum SVGPathMode
    {
        MoveTo, // (x y)+ , if more than 1 pair, next are line to.
        LineTo, // (x y)+
        HorizontalTo, // x+
        VerticalTo, // y+
        CurveTo, // (x1 y1 x2 y2 x y)+
        SmoothCurveTo, // (x2 y2 x y)+
        QuadTo, // (x1 y1 x y)+
        SmoothQuadTo, // (x y)+
        Close // ()
    }

    /// <summary>
    /// Quick hack implementation, using Inkscape generated paths as a reference
    /// </summary>
    /// <param name="svgText"></param>
    /// <returns></returns>
    private List<Vector2> GetVerticesFromSVG(string svgText)
    {
        var vertexList = new List<Vector2>();

        XmlTextReader reader = new XmlTextReader(new StringReader(svgText));
        while (reader.Read())
        {
            switch (reader.NodeType)
            {
                case XmlNodeType.Element: // The node is an element.
                    var name = reader.Name;
                    if (name.Equals("path"))
                    {
                        vertexList = ReadVertexList(reader.GetAttribute("d"));
                    }
                    break;
            }
        }

        // move polygon to min x and min y
        Vector2 min = new Vector2(float.MaxValue, float.MaxValue);
        Vector2 max = new Vector2(float.MinValue, float.MinValue);
          
        foreach (var v in vertexList) {
            if (v.x < min.x)
            {
                min.x = v.x;
            }
            else if (v.x > max.x)
            {
                max.x = v.x;
            }
            if (v.y < min.y)
            {
                min.y = v.y;
            }
            else if (v.y > max.y)
            {
                max.y = v.y;
            }
        }
        var mid = (max - min) / 2;
        for (int i = 0; i < vertexList.Count; i++)
        {
            vertexList[i] = vertexList[i] - min - mid;
        }
        reader.Close();
        return vertexList;
	}

    private SVGPathMode DrawMode(string cmd)
    {
        switch (cmd.ToUpper())
        {
            case MOVE_TO:
                return SVGPathMode.MoveTo;
            case LINE_TO:
                return SVGPathMode.LineTo;
            case HORIZONTAL_TO:
                return SVGPathMode.HorizontalTo;
            case VERTICAL_TO:
                return SVGPathMode.VerticalTo;
            case CURVE_TO:
                return SVGPathMode.CurveTo;
            case SMOOTH_CURVE_TO:
                return SVGPathMode.SmoothCurveTo;
            case QUAD_TO:
                return SVGPathMode.QuadTo;
            case SMOOTH_QUAD_TO:
                return SVGPathMode.SmoothQuadTo;
            case CLOSE:
                return SVGPathMode.Close;
        }
        return SVGPathMode.LineTo;
    }

    private Vector2 DequeueVertex(Queue<string> commands)
    {
        var x = commands.Dequeue();
        var y = commands.Dequeue();
        return new Vector2(float.Parse(x), float.Parse(y));
    }

    private List<Vector2> ReadVertexList(string pathData) {
        var pdElements = pathData.Split(new char[] { ' ', ',' });
        List<Vector2> vertexList = new List<Vector2>();

        Vector2 lastVertex = Vector2.zero;
        SVGPathMode mode = SVGPathMode.MoveTo;

        Queue<string> commands = new Queue<string>(pdElements);
        if (commands.Count == 0) {
            return vertexList;
        }

        bool relative = false;
        do
        {
            string cmd = commands.Peek();
            float val;
            if (!float.TryParse(cmd, out val))
            {
                commands.Dequeue();
                relative = Char.IsLower(cmd[0]);
                mode = DrawMode(cmd);
            }
            switch (mode)
            {
                case SVGPathMode.MoveTo:
                    {
                        Vector2 newVertex = DequeueVertex(commands);
                        if (relative)
                        {
                            newVertex += lastVertex;
                        }
                        vertexList.Add(newVertex);
                        lastVertex = newVertex;
                        mode = SVGPathMode.LineTo;
                    } break;
                case SVGPathMode.LineTo:
                    {
                        Vector2 newVertex = DequeueVertex(commands);
                        if (relative)
                        {
                            newVertex += lastVertex;
                        }
                        vertexList.Add(newVertex);
                        lastVertex = newVertex;
                    } break;

            }
        } while (mode != SVGPathMode.Close);

        return vertexList;
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