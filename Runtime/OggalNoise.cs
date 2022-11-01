using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenuAttribute(fileName = " New Oggal Noise", menuName ="Terrain Noise/Oggal Noise")]
public class OggalNoise : TerrainNoiseObject
{
    TerrainNoise myNoise;
    public override bool isValid {get => (myNoise != null);}

    public override float getHeight(Vector2 pos){

        return myNoise.getHeight(pos.x,pos.y);
    }

    public override void Intialize(int Seed, float Scale)
    {
        myNoise = new TerrainNoise(Seed,Mathf.RoundToInt(Scale));
    }


}
