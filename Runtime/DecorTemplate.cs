using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DecorTemplate : ScriptableObject
{
    [SerializeField]Mesh Visual;
    
    [SerializeField]bool Collides = false;
}
