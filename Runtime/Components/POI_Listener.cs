using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;



public class POI_Listener : MonoBehaviour
{
    public IntIntUnityEvent OnSpawned = new IntIntUnityEvent();
    public UnityEvent OnDespawned = new UnityEvent();
}

[System.Serializable]
public class IntIntUnityEvent : UnityEvent<int, int> { }
