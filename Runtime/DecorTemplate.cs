using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu()]
public class DecorTemplate : ScriptableObject
{
    [SerializeField]Mesh visual;
    [SerializeField]Material material;
    
    [SerializeField]bool collides = false;


    public GameObject BuildDecorObject(int _seed, Vector2 _worldPos, WorldGen _wg ){
        //Ideally we would fetch a fresh Object from a Pool.
        GameObject Decorbase =  new GameObject();
        Decorbase.AddComponent<MeshCollider>().enabled = collides;
        Decorbase.AddComponent<MeshFilter>().sharedMesh = visual;
        Decorbase.AddComponent<MeshRenderer>().material = material;
        Decorbase.transform.SetPositionAndRotation(
            new Vector3(_worldPos.x,_wg.GetHeight(_worldPos.x,_worldPos.y),_worldPos.y),
            Quaternion.identity);
        Decorbase.name = name + _worldPos;
        return Decorbase;
    }
}
