﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Xml;
using System.IO;

[System.Serializable]
public class SVGOutline : Outline {

	private const string MOVE_TO = "M";
	private const string LINE_TO = "L";
	private const string HORIZONTAL_TO = "H";
	private const string VERTICAL_TO = "V";
	private const string CURVE_TO = "C";
	private const string SMOOTH_CURVE_TO = "S";
	private const string QUAD_TO = "Q";
	private const string SMOOTH_QUAD_TO = "T";
	private const string CLOSE = "Z";

    public bool normalize = true;
    public float scale = 10;
    public int curveSegments = 8;

    private Vector2 centerPoint = new Vector2(0,0);
    public string pathId;

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

	private List<Vector2> points;
	
	public TextAsset svgFile;
	
    override public List<Vector2> Points
	{
		get
		{
			return points;
		}
	}

	override public void Init()
	{
		points = GetVerticesFromSVG(svgFile.text);
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
                    if (pathId != null && !pathId.Equals(string.Empty))
                    {
                        var id = reader.GetAttribute("id");
                        if (id != pathId)
                        {
                            continue;
                        }
                    }
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
        var size = max - min;
        var mid = size / 2;
        var scale = normalize ? this.scale / Mathf.Max(mid.x, mid.y) : this.scale;
//        centerPoint = mid * scale;
		for (int i = 0; i < vertexList.Count; i++)
		{
			vertexList[i] = (vertexList[i] - min - mid) * scale;
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
				relative = System.Char.IsLower(cmd[0]);
				mode = DrawMode(cmd);
			}
			switch (mode)
			{
                case SVGPathMode.CurveTo:
                {
                    Vector2 last = lastVertex;
                    Vector2 cpStart = DequeueVertex(commands);
                    Vector2 cpEnd = DequeueVertex(commands);
                    Vector2 dest = DequeueVertex(commands);

                    float step = (float)1 / curveSegments;
                    float t = step;
                    for (int i = 0; i < curveSegments; i++)
                    {
                        float rem = (1 - t);
                        var p = last * (rem * rem * rem) +
                            cpStart * (3 * rem * rem * t) +
                            cpEnd * (3 * rem * t * t) +
                            dest * (t * t * t);

                        if (relative)
                        {
                            p += lastVertex;
                        }

                        vertexList.Add(p);
                        t += step;
                    }
                    lastVertex = dest;

                } break;
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

    override public Vector2 Center
    {
        get
        {
            return centerPoint;
        }
    }

}
