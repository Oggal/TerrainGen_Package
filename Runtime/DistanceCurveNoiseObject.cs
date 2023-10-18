using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[CreateAssetMenuAttribute(fileName = " New Curve Noise", menuName = "Terrain Noise/Distance Curve Noise")]
public class DistanceCurveNoiseObject : TerrainNoiseObject
{

    [SerializeField]
    private float scale = 2;
    [SerializeField]
    private AnimationCurve curve;
    public override bool isValid { get => true; }

    public override ITerrainNoise Intialize(int Seed, float Scale)
    {
        return new DistanceCurveNoise(scale, curve);
    }


}

public class DistanceCurveNoise : ITerrainNoise
{
    float scale;
    AnimationCurve curve;
    public DistanceCurveNoise(float Scale, AnimationCurve Curve)
    {
        scale = Scale;
        curve = Curve;
    }
    public float getHeight(float x, float y)
    {
        float dis = Vector2.Distance(new Vector2(x, y), Vector2.zero);
        //Treat scale as the distance that represents one curve.    
        return curve.Evaluate(dis / scale);
    }
}
