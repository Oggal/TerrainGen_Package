using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TiledData : MonoBehaviour {

    public WorldGen parent;
    public int x, z;
    public void ClearChildren()
    {
        if(parent != null)
        {
            parent.ClearChildren(gameObject);
        }
    }


}
