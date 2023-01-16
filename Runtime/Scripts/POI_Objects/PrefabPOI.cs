using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenuAttribute(fileName = "New POI", menuName ="Terrain Noise/Modifiers/POI")]
public class PrefabPOI : TerrainNoiseModifier
{
    
    [SerializeField] Vector3 Position;
    [SerializeField] GameObject Prefab;
    [SerializeField] float innerRadius, outerRadius;

    public override float GetRatio(float pointX, float pointY)
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

    public override float GetTargetHeight(float pointX, float pointY)
    {
        return Position.y;
    }

    public override GameObject BuildGameObject()
    {
        GameObject GO = Instantiate(Prefab);
        GO.transform.position = Position;
        return (GO);
    }

    public Vector3 GetPosition()
    {
        return Position;
    }
}
