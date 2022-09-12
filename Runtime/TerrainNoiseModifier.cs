using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class TerrainNoiseModifier : ScriptableObject
{
    public abstract float GetRatio(float pointX, float pointY);
    public abstract float GetTargetHeight(float pointX, float pointY);
}
