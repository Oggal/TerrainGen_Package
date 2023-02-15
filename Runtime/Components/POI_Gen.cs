using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(WorldGen))]
public class POI_Gen : MonoBehaviour
{
    public ulong worldRadius = 5000000;
    public uint worldResolution = 100;
    public uint cityCount = 5;
    
    [SerializeField]
    WorldGen generator;
    
    // Start is called before the first frame update
    void OnEnable()
    {
        if (generator == null)
            generator = GetComponent<WorldGen>();
        generator.WorldGenPreInit.AddListener(this.Gen_POIs);
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void Gen_POIs(int seed)
    {
        List<TerrainNoiseModifier> NewMods = new List<TerrainNoiseModifier>();
        TerrainNoiseModifier mod = null;
        System.Random rand = new System.Random(seed);
        for(int i = 0; i < cityCount; i++)
        {
            Debug.Log("QUICK BATMAN! Make a place!");
            mod  = ScriptableObject.CreateInstance<TerrainNoiseModifier>();
            float x,y;
            x = (float)rand.NextDouble() * 2 * worldResolution;
            y = (float)rand.NextDouble() * 2 * worldResolution;
            mod.Position = new Vector3(x,generator.GetHeight(x,y),y);
            mod.innerRadius = 5;
            NewMods.Add(mod);
        }
        generator.ProcMods.AddRange(NewMods);
    }

}


