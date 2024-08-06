using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class TerrainNoiseObject : ScriptableObject
{
    public abstract bool isValid { get; }

    public abstract ITerrainNoise Intialize(int Seed, float Scale);
}
