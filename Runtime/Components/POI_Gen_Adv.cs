using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(WorldGen))]
public class POI_Gen_Adv : MonoBehaviour
{
    public ulong worldRadius = 5000000;
    public uint worldResolution = 100;
    public uint[] cityCounts;
    
    [SerializeField]
    TerrainNoiseModifier[] modifier_templates;
    [SerializeField]
    WorldGen generator;
    
    // Start is called before the first frame update
    void OnEnable()
    {
        if (generator == null)
            generator = GetComponent<WorldGen>();
        generator.WorldGenPreInit.AddListener(this.Gen_POIs);
    }

    void OnValidate()
    {
        if(cityCounts.Length < modifier_templates.Length){
            cityCounts = new uint[modifier_templates.Length];
        }
    }


    public void Gen_POIs()
    {
        int seed = generator.Seed;
        List<TerrainNoiseModifier> NewMods = new List<TerrainNoiseModifier>();
        TerrainNoiseModifier mod = null;
        System.Random rand = new System.Random(seed);
        for(int j = 0; j < modifier_templates.Length; j++)
        {
            TerrainNoiseModifier modifier_template = modifier_templates[j];
            if(modifier_template == null) continue;
            for(int i = 0; i < cityCounts[j]; i++)
            {
                mod  = ScriptableObject.CreateInstance<TerrainNoiseModifier>();
                float x,y;
                x = ((float)rand.NextDouble() * 2 * worldResolution) - worldResolution;
                y = ((float)rand.NextDouble() * 2 * worldResolution) - worldResolution;
                mod.Position = new Vector3(x,generator.GetHeight(x,y),y);
                mod.innerRadius = 50f;
                mod.outerRadius = 25f;

                mod.Prefab = modifier_template.Prefab;
                NewMods.Add(mod);
            }
        }
        generator.ProcMods.AddRange(NewMods);
    }

}


