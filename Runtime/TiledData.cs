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

    public void SpawnTrees()
    {

    }

	public void OnDrawGizmos()
	{
		if (parent.drawRadius)
		{
			Gizmos.color = Color.red;
			Gizmos.DrawMesh(GetComponent<MeshCollider>().sharedMesh, transform.position);
		}
		
	}

}
