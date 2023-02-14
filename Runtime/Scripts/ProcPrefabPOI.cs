using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[CreateAssetMenuAttribute(fileName = "New Procedural POI", menuName ="Terrain Noise/Modifiers/Proc-POI")]
public class ProcPrefabPOI : TerrainNoiseModifier
{

    public override void Pregeneration()
    {
        Debug.Log(" Set Position, Generate Mesh");
    }

}
