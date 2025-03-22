using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenuAttribute(fileName = " New Oggal Noise", menuName = "Terrain Noise/Oggal Noise")]
public class OggalNoise : TerrainNoiseObject
{
    [SerializeField] float setScale = 0;
    [SerializeField] float GridScale = 1;
    public TerrainNoise myNoise;
    public override bool isValid { get => (myNoise != null); }

    public override ITerrainNoise Intialize(int Seed, float Scale)
    {
        if (setScale != 0)
            Scale = setScale;
        myNoise = new TerrainNoise(Seed, Mathf.RoundToInt(Scale), GridScale);
        return myNoise;
    }


}
