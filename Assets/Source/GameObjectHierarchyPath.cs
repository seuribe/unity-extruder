using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

[ExecuteInEditMode]
public class GameObjectHierarchyPath : ExtrudePath
{
    [SerializeField]
    private GameObject pathRoot;
    public GameObject PathRoot
    {
        get
        {
            return pathRoot;
        }
        set
        {
            this.pathRoot = value;
            Prepare();
        }
    }

    private List<Transform> steps = new List<Transform>();

    public void OnValidate()
    {
        Prepare();
    }

    public void Prepare()
    {
        steps = new List<Transform>();
        if (pathRoot == null)
        {
            steps.Add(gameObject.transform);
            return;
        }
        var next = pathRoot;
        do
        {
            steps.Add(next.transform);
            if (next.transform.childCount == 0)
            {
                break;
            }
            next = next.transform.GetChild(0).gameObject;
        } while (true);
    }

    override public List<Transform> Steps
    {
        get
        {
            return steps;
        }
    }
}
