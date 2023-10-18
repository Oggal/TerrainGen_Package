using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class TerrainNoiseObject : ScriptableObject
{
    public abstract bool isValid { get; }
    
    [Obsolete("This method has been moved to the initialized object")]
    internal virtual float getHeight(Vector2 pos)
    {
        return 0;
    }

    public abstract ITerrainNoise Intialize(int Seed, float Scale);
}
