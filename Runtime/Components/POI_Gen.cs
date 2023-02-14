using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(WorldGen))]
public class POI_Gen : MonoBehaviour
{
    public ulong worldRadius = 5000000;
    public uint worldResolution = 100;
    public uint cityCount = 5;
    
    WorldGen generator;
    
    // Start is called before the first frame update
    void OnEnabled()
    {
        if (generator == null)
            generator = GetComponent<WorldGen>();
        generator.WorldGenPreInit.AddListener(Gen_POIs);
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void Gen_POIs()
    {
        List<TerrainNoiseModifier> NewMods = new List<TerrainNoiseModifier>();
        for(int i = 0; i < cityCount; i++)
        {
            TerrainNoiseModifier city_obj = new TerrainNoiseModifier();
        }
    }

}


