using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DecorChunk
{
    Dictionary<Vector2,DecorTemplate> decorDict;
    Vector2Int tileIndex;
    private int seed;
    private System.Random rand;
    private WorldGen worldRef;

    public DecorChunk( Vector2Int _Index, int _Seed, WorldGen _world){
        rand = new System.Random(_Seed);
        seed =_Seed;
        decorDict = new Dictionary<Vector2, DecorTemplate>();
        tileIndex = _Index;
        worldRef = _world;
    }

    //TODO: This will return a list of templates with no location Data, Will Need that location data.
    /*
        So do we ant to return an array of DecorTemplate or do we want to return a diffrent data object array?
        I know we want to pool out decor objects so I'd like to avoid instanciating a bunch of game objects here...
        That being said, gameObjects mean we can start testing quick.
    */
    public DecorTemplate[] getDecor(Rect _worldSpace){
        List<DecorTemplate> decorOnTile = new List<DecorTemplate>();
        foreach(KeyValuePair<Vector2,DecorTemplate> decor in decorDict){
            if(_worldSpace.Contains(decor.Key+(tileIndex*worldRef.TileSize))){
                decorOnTile.Add(decor.Value);
            }
        }
        return decorOnTile.ToArray();
    }


    void Populate(){
        //I would love to assume that the random hasn't been used. but we cant.
        rand = new System.Random(seed);
        for(int i = 0;i<worldRef.DecorAttempts;i++){
            Vector2 pos = new Vector2((float)rand.NextDouble(),(float)rand.NextDouble());
            if(rand.NextDouble()>worldRef.DecorDensity){
                decorDict.Add(pos,new DecorTemplate());//TODO:Actually select a Decor Object.
            }
        }
    }

}
