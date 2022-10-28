﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenuAttribute(fileName = " New Constant Noise", menuName ="Terrain Noise/Constant Noise")]
public class ConstantNoiseObject : TerrainNoiseObject
{
    
    [SerializeField]
    private float height = 2;
    
    public override bool isValid {get => true;}

    public override float getHeight(Vector2 pos){

        return height;
    }

    public override void Intialize(int Seed, float Scale)
    {
        ;
    }


}
