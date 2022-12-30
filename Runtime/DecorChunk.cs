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
        Populate();
    }

  /*
        So do we ant to return an array of DecorTemplate or do we want to return a diffrent data object array?
        I know we want to pool out decor objects so I'd like to avoid instanciating a bunch of game objects here...
        That being said, gameObjects mean we can start testing quick.
    */
    public GameObject[] getDecor(Rect _worldSpace){
        List<GameObject> decorOnTile = new List<GameObject>();
        foreach(KeyValuePair<Vector2,DecorTemplate> decor in decorDict){
            if(_worldSpace.Contains((decor.Key+((Vector2)tileIndex))*worldRef.DecorChunkSize)){
                decorOnTile.Add(
                    decor.Value.BuildDecorObject(1,
                        (decor.Key+(Vector2)tileIndex)*worldRef.DecorChunkSize,
                        worldRef));
            }
        }
        if (worldRef.SpawnPOIs)
            decorOnTile.AddRange(getPOI(_worldSpace));

        return decorOnTile.ToArray();
    }


    void Populate(){
        //I would love to assume that the random hasn't been used. but we cant.
        rand = new System.Random(seed);
        //Fill a square
        for(int i = 0;i<worldRef.DecorAttempts;i++){
            Vector2 pos = new Vector2((float)rand.NextDouble(),(float)rand.NextDouble());     //Vector2 ([-0.5,0.5],[-0.5,0.5])
            int decorSeed = Mathf.RoundToInt(Mathf.Floor((float)rand.NextDouble() * worldRef.decorObjects.Count));
            if(rand.NextDouble()>worldRef.DecorDensity){
                decorDict.Add(pos,worldRef.decorObjects[decorSeed]);
            }
        }
    }

    public GameObject[] getPOI(Rect _worldSpace)
    {
        List<GameObject> poi_OnTile = new List<GameObject>();
        foreach(TerrainNoiseModifier tMod in worldRef.Mods)
        {
            if(tMod is PrefabPOI poi)
            {
                Vector3 poiPos = poi.GetPosition();
                GameObject obj = null;
                if (_worldSpace.Contains(new Vector2(poiPos.x, poiPos.z)))
                    obj = poi.BuildGameObject();
                if(obj != null)
                    poi_OnTile.Add(obj);
            }
        }

        return poi_OnTile.ToArray();
    }
}
