using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

[System.Serializable]
public abstract class Outline : MonoBehaviour
{
    /// <summary>
    /// Initialize the values of the outline
    /// </summary>
    virtual public void Init()
    {

    }

	/// <summary>
	/// After being initialized, the outline points can be obtained from here
	/// </summary>
    abstract public List<Vector2> Points
    {
        get;
    }

    abstract public Vector2 Center
    {
        get;
    }

}
