using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class TerrainNoiseObject : ScriptableObject
{
    public abstract bool isValid{get;}
    public abstract float getHeight(Vector2 pos);

    public abstract void Intialize(int Seed, float Scale);
}
