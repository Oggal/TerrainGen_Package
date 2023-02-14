using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;


[CreateAssetMenuAttribute(fileName = "New POI", menuName ="Terrain Noise/Modifiers/POI")]
public class TerrainNoiseModifier : ScriptableObject
{
    [SerializeField] protected Vector3 Position;
    [SerializeField] protected GameObject Prefab;
    [SerializeField] protected float innerRadius, outerRadius;


    public virtual float GetRatio(float pointX, float pointY)
    {
        float distance = Vector2.Distance(new Vector2(Position.x, Position.z), new Vector2(pointX, pointY));
        if (distance > outerRadius + innerRadius)
        {
            return 0;
        }
        else if(distance < innerRadius)
        {
            return 1;
        }
        return 1 - (distance - innerRadius) / (outerRadius);
    }
    public virtual float GetTargetHeight(float pointX, float pointY)
    {
        return Position.y;
    }
    public virtual GameObject BuildGameObject()
    {
        if(Prefab == null)
            return null;
        GameObject GO = Instantiate(Prefab);
        GO.transform.position = Position;
        return (GO);
    }

    public virtual Vector3 GetPosition()
    {
        return Position;
    }

    public virtual void Pregeneration() { }

    public UnityEvent OnSpawned;
    public UnityEvent OnDespawned;

}
