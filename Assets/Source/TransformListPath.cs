using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

[ExecuteInEditMode]
public class TransformListPath : ExtrudePath
{
    [SerializeField]
    public List<Transform> steps = new List<Transform>();

    override public List<Transform> Steps
    {
        get
        {
            return steps;
        }
    }

}
