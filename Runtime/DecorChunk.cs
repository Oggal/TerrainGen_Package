using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DecorChunk
{
    Dictionary<Vector2Int,DecorTemplate> DecorDict;
    Vector2Int TileIndex;
    private int Seed;
    private System.Random rand;
    private WorldGen worldRef;

    public DecorChunk( Vector2Int _Index, int _Seed, WorldGen _world){
        rand = new System.Random(_Seed);
        Seed =_Seed;
        DecorDict = new Dictionary<Vector2Int, DecorTemplate>();
        TileIndex = _Index;
        worldRef = _world;
    }

    //TODO: This will return a list of templates with no location Data, Will Need that location data.
    /*
        So do we ant to return an array of DecorTemplate or do we want to return a diffrent data object array?
        I know we want to pool out decor objects so I'd like to avoid instanciating a bunch of game objects here...
    */
    public DecorTemplate[] getDecor(Rect _worldSpace){
        List<DecorTemplate> decorOnTile = new List<DecorTemplate>();
        foreach(KeyValuePair<Vector2Int,DecorTemplate> decor in DecorDict){
            if(_worldSpace.Contains(decor.Key+(TileIndex*worldRef.TileSize))){
                decorOnTile.Add(decor.Value);
            }
        }
        return decorOnTile.ToArray();
    }

}
