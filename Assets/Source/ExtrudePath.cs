using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

[System.Serializable]
public abstract class ExtrudePath : MonoBehaviour
{
    /// <summary>
    /// Initialize the values of the path
    /// </summary>
    virtual public void Init()
    {

    }

    public abstract List<Transform> Steps
    {
        get;
    }
}
