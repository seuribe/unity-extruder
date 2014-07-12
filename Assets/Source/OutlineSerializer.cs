using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

[System.Serializable]
class OutlineSerializer
{
    [SerializeField]
    private Outline outline;

    [SerializeField]
    public Outline Outline
    {
        get
        {
            return outline;
        }
        set
        {
            this.outline = value;
        }
    }


}
