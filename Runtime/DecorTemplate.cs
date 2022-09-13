using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DecorTemplate : ScriptableObject
{
    [SerializeField]Mesh visual;
    
    [SerializeField]bool collides = false;
}
