using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(WorldGen))]
public class POI_Gen : MonoBehaviour
{
    public uint worldResolution = 100;

    public uint[] cityCounts;

    [SerializeField]
    POI_Object[] modifier_templates;

    [SerializeField]
    WorldGen generator;

    [SerializeField]
    private POI_Object[] StaticModifiers;

    private readonly List<POI_Object> ProcMods = new List<POI_Object>();
    public POI_Object[] Mods
    {
        get
        {
            List<POI_Object> temp = new List<POI_Object>(ProcMods.Count + StaticModifiers.Length);
            temp.AddRange(ProcMods);
            temp.AddRange(StaticModifiers);
            return temp.ToArray();
        }
    }

    // OnEnable is called when the script is enabled
    void OnEnable()
    {
        SetUp();
    }

    // OnDisable is called when the script is disabled
    void OnDisable()
    {
        Clear();
    }

    [ContextMenu("SetUp")]
    public void SetUp()
    {
        if (generator == null)
            generator = GetComponent<WorldGen>();
        else
            generator.WorldGenPreInit.RemoveListener(this.Gen_POIs);
        generator.WorldGenPreInit.AddListener(this.Gen_POIs);
    }

    [ContextMenu("Clear")]
    public void Clear()
    {
        ProcMods.Clear();
        if (generator)
            generator.WorldGenPreInit.RemoveListener(this.Gen_POIs);
    }

    void OnValidate()
    {
        if (cityCounts.Length < modifier_templates.Length)
        {
            cityCounts = new uint[modifier_templates.Length];
        }
    }


    public void Gen_POIs()
    {
        ProcMods.Clear();
        int seed = generator.Seed;
        List<POI_Object> NewMods = new List<POI_Object>();
        POI_Object mod = null;
        System.Random rand = new System.Random(seed);
        for (int j = 0; j < modifier_templates.Length; j++)
        {
            POI_Object modifier_template = modifier_templates[j];
            if (modifier_template == null) continue;
            for (int i = 0; i < cityCounts[j]; i++)
            {
                mod = ScriptableObject.CreateInstance<POI_Object>();
                float x, y;
                x = ((float)rand.NextDouble() * 2 * worldResolution) - worldResolution;
                y = ((float)rand.NextDouble() * 2 * worldResolution) - worldResolution;
                mod.Position = new Vector3(x, generator.GetHeight(x, y), y);

                mod.Prefab = modifier_template.Prefab;
                mod.Seed = rand.Next();
                mod.ID = NewMods.Count + 1;
                NewMods.Add(mod);
            }
        }
        ProcMods.AddRange(NewMods);
    }

    public POI_Object[] GetPOIinRect(Rect area)
    {
        var poiOnTile = new List<POI_Object>();
        for (int i = 0; i < Mods.Length; i++)
        {
            var tMod = Mods[i];
            Vector3 poiPos = tMod.GetPosition();
            GameObject obj = null;
            if (area.Contains(new Vector2(poiPos.x, poiPos.z)))
                poiOnTile.Add(tMod);
        }
        return poiOnTile.ToArray();
    }
}


