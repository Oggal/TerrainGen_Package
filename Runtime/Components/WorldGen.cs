using System.Collections;
using System.Collections.Generic;
using System.Xml.Serialization;
using UnityEngine;
using UnityEngine.Events;

[SelectionBase]
public class WorldGen : MonoBehaviour
{
    public int MaxLOD = 0;

    [Space]
    [Header("World Data")]
    public bool UseSeed = true;			// if false, will generate a new seed for the world on build
    public int Seed = 0;

    public bool BuildOnStart = true;	//if false, will NOT build a new world when the game runs
    [Space]
    public GameObject Player;			// Gameobject that will be tracked to keep world centered.
    [Range(1, 10)]
    public int OctaveCount = 4;         // Levels for Max Detail World
    public int PhysOctaveCount = 4;     // DEV Var to test Generating a collison mesh with less layers.(UN-USED)

    private TerrainNoiseObject FallbackNoiseObject { get { return ScriptableObject.CreateInstance<ConstantNoiseObject>(); } }
    private ITerrainNoise FallbackNoiseData { get { return new ConstantNoise(1);}}

    public Material[] mats;
    public bool UseManyMats {get{return mats.Length > 1;}}
    [Space]
    public float smoothing = 100;

    private int localX = 0;
    private int localZ = 0;
    [Header("Tile Data")]
    public int TileSize = 100;
    [Range(3, 200)]
    public int V_VertexCount = 100;
    [Range(3, 200)]
    public int P_VertexCount = 50;
    [Range(0, 10), Tooltip("Radius of Tiles beyond center")]
    public int Radius;


    [Header("World Contents")]
    [Tooltip("Should the generator check for POI Objects")]
    public bool SpawnPOIs = false;

    [Space]
    [Header("Decor Settings")]
    public bool buildDecor = false;
    public float DecorChunkSize = 500;
    [Range(0, 1), Tooltip("Chance to beat to place Decor")]
    public float DecorDensity = 1;
    [Min(0), Tooltip("Number of attempts to place Decor per DecorChunk")]
    public int DecorAttempts = 100;
    private ITerrainNoise DecorChanceMap;
    private Dictionary<Vector2Int, DecorChunk> DecorChunks = new Dictionary<Vector2Int, DecorChunk>();
    [SerializeField]
    public List<DecorTemplate> decorObjects = new List<DecorTemplate>();
    
    [Space]
    [SerializeField] private TerrainNoiseObject[] Octaves;

    [SerializeField] private TerrainNoiseObject ScaleMap;

    [HideInInspector] public ITerrainNoise[] TerrainData;
    
    private ITerrainNoise TerrainScale;

    [Space]

    [Header("Events")]
    [Tooltip("Called at Start of Initial World Generation")]
    public UnityEvent WorldGenStart;

    [Tooltip("Called When WorldGen Finishes")]
    public UnityEvent WorldGenFinish;

    [Tooltip("Called before World Generation starts")]
    public UnityEvent WorldGenPreInit;
    [Space]
    [Tooltip("Called when Tile is flagged to be removed")]
    public TileEvent TileUnloaded;
    [Tooltip("Called after Tile has loaded")]
    public TileEvent TileLoaded;


    private int TRadius;
    private GameObject[] Tiles;

    private int[] Seeds;

    private List<IEnumerator> TilesBuilding = new List<IEnumerator>();

    public void Start()
    {
        if (BuildOnStart)
            BuildWorld();
    }

    // Update is called once per frame
    void Update()
    {
        CheckWorldCenter();

    }

    public void ClearChildren(GameObject g)
    {
        StartCoroutine(_ClearChildren(g));
    }

    IEnumerator _ClearChildren(GameObject g)
    {
        int i = 0;
        while (g!=null && g.transform.childCount > 0)
        {
            if(g.transform.GetChild(0) == null)
            {
                Debug.Log(string.Format("Child 0 of gameobject {0} is null", g.name)); 
                break;
            }
            #if !UNITY_EDITOR
            //Debug.Log("Destroying: " + g.transform.GetChild(0).name);
			Destroy(g.transform.GetChild(0).gameObject);
            #else
            var child = g.transform.GetChild(0);
            foreach (var listener in child.GetComponentsInChildren<POI_Listener>())
            {
                listener.OnDespawned.Invoke();
            }
            DestroyImmediate(child.gameObject);
            #endif
            i++;
            if(i > 100)
            {
                yield return null;
                i = 0;
            }
            
        }
        Debug.Log("Children Cleared From Object: " + g.name);
        yield return null;
    }


    public void ClearChildren()
    {
        if(gameObject!=null)
            ClearChildren(gameObject);
    }


    #region World Gen

    private void ClearWorldData()
    {
        ClearChildren();
        DecorChunks = new Dictionary<Vector2Int, DecorChunk>();
        //Center The World
        localX = 0;
        localZ = 0;

        
        TRadius = Radius * 2 + 1;
        DecorChanceMap = null;
    }

    public void BuildWorld(int x = 0, int z = 0)
    {
        ClearWorldData();
        localX = x;
        localZ = z;
        System.Random r = new System.Random();
        InitSeeds(ref r);
        InitNoiseData();
        WorldGenPreInit.Invoke();

        WorldGenStart.Invoke();
        //Build the Tile Grid
        Init_Tiles();
    }


    private void Init_Tiles()
    {
        Tiles = new GameObject[TRadius * TRadius];  //Define The tiles in code

        //Actually Build the Tiles
        for (int y = Radius * -1; y <= Radius; y++)
        {
            for (int x = Radius * -1; x <= Radius; x++)
            {
                int id = (Radius - y) * TRadius + (x + Radius);

                Tiles[id] = BuildTile(x+ localX, y + localZ);
                Tiles[id].name += id;
            }
        }
    }

    /// <summary>
    /// Ensure TerrainData has been intialized, and is equal length to Octaves
    /// </summary>
    private void ValidateOctaves()
    {
        if (TerrainData == null || TerrainData.Length != OctaveCount +1)
        {
            ITerrainNoise[] temp = new ITerrainNoise[OctaveCount+1];
            //Octaves = new TerrainNoiseObject[OctaveCount];
            for (int i = 0; i < OctaveCount; i++)
            {
                //see if we can reuse the old noise objects
                if (TerrainData != null && i < TerrainData.Length)
                {
                    temp[i] = TerrainData[i];
                }
                else 
                {
                    temp[i] = FallbackNoiseData;
                }
            }
            TerrainData = temp;
        }
    }

    /// <summary>
    /// Intialize TerrainScale from ScaleMap scriptableObject
    /// If ScaleMap is null, use FallbackNoiseObject
    /// </summary>
    private void InitTerrainScale()
    {
        if (ScaleMap == null)
        {
            ScaleMap = FallbackNoiseObject;
        }
        TerrainScale = ScaleMap.Intialize(Seed, 3);
    }

    /// <summary>
    /// Intialize the NoiseData for the World
    /// </summary>
    private void InitNoiseData()
    {
        //Intialize the Octaves
        ValidateOctaves();
        InitTerrainScale();
        //Construct the Octaves
        for (int OctaveIndex = 0; OctaveIndex < OctaveCount; OctaveIndex++)
        {
            TerrainData[OctaveIndex] = PrepareNoise(Octaves[OctaveIndex], Seeds[OctaveIndex + 1], Mathf.Pow(5, OctaveIndex));
        }
    }

    private ITerrainNoise PrepareNoise(TerrainNoiseObject noiseAsset, int seed, float scale)
    {
        if (noiseAsset == null)
        {
            return FallbackNoiseData;
        }
        return noiseAsset.Intialize(seed, scale);
    }

    /// <summary>
    /// Generates Array of Seeds for Terrain Octaves
    /// </summary>
    private void InitSeeds(ref System.Random r)
    {
        //If we are not using a preset Seed generate a new one
        if (!UseSeed)
            Seed = Random.Range(int.MinValue, int.MaxValue);
        //Create the array of seeds
        Seeds = new int[OctaveCount + 2];
        //Set the first seed to the overall world seed
        Seeds[0] = Seed;
        //Create the Seeds to be used
        r = new System.Random(Seed);
        Seeds[OctaveCount + 1] = r.Next();
        for (int OctaveIndex = 0; OctaveIndex < OctaveCount; OctaveIndex++)
        {
            //Get the Seed for Each Octave
            Seeds[OctaveIndex +1] = r.Next();
        }
    
    }

    private void BuildMesh(int Tx, int Ty, GameObject HoldsMesh)
    {
        if (Application.isPlaying)
        {
            UpdateMesh(Tx, Ty, HoldsMesh);
        }
        else
        {
            UpdateMesh(Tx, Ty, HoldsMesh, true);
            //BuildMeshSingle(Tx, Ty,HoldsMesh);
        }
    }

    private float getRatio(float pX, float pY, out float height)
    {
        height = 0;
        if (SpawnPOIs && GetComponent<POI_Gen>() != null)
        {
            POI_Object[] Mods = GetComponent<POI_Gen>()?.Mods;
            float ratio = 0;
            foreach (POI_Object TNM in Mods)
            {
                if (ratio < TNM.GetRatio(pX, pY))
                {
                    ratio = TNM.GetRatio(pX, pY);
                    height = TNM.GetTargetHeight(pX, pY);
                }
            }
            return ratio;
        }
        return 0.0f;
    }

    private float getRatio(float pX, float pY)
    {

        if (SpawnPOIs && GetComponent<POI_Gen>() != null)
        {
            POI_Object[] Mods = GetComponent<POI_Gen>()?.Mods;
            float ratio = 0;
            foreach (POI_Object TNM in Mods)
            {
                if (ratio < TNM.GetRatio(pX, pY))
                {
                    ratio = TNM.GetRatio(pX, pY);
                    //height = TNM.GetTargetHeight(pX, pY);
                }
            }
            return ratio;
        }
        return 0.0f;
    }

    private void UpdateMesh(int Tx, int Ty, GameObject m, bool RunInstant = false)
    {
        IEnumerator i = BuildMeshSlow(Tx, Ty, m, RunInstant);
        TilesBuilding.Add(i);
        StartCoroutine(i);
        //Should add the coroutine to a list, when the list is empty call the finished event
    }

    private IEnumerator BuildMeshSlow(int Tx, int Ty, GameObject HoldsM, bool RunInstant = false)
    {
        if (HoldsM) {
            ClearChildren(HoldsM); 
        }
        Debug.Log("Building Tile: " + Tx + " " + Ty);
        HoldsM.GetComponent<MeshRenderer>().enabled = false;
        Mesh m = HoldsM.GetComponent<MeshFilter>().sharedMesh;
        if (m == null)
        {
            m = new Mesh();
        }
        int TSize = V_VertexCount + 1;
        Vector3[] verts = new Vector3[TSize * TSize];
        Vector3[] normals = new Vector3[TSize * TSize];
        Vector2[] uv = new Vector2[TSize * TSize];
        int[] tri = new int[6 * ((TSize - 1) * (TSize - 1))];
        float VertDis = ((float)TileSize) / V_VertexCount;

        float lx = Tx * TileSize;
        float ly = Ty * TileSize;
        if (!RunInstant)
            yield return null;
        int halfTile = TileSize / 2;
        for (int y = 0; y < TSize; y++)
        {
            float Yi = (y * VertDis) - halfTile;
            for (int x = 0; x < TSize; x++)
            {
                int id = y * TSize + x;
                float Xi = (x * VertDis) - halfTile;
                verts[id] = new Vector3(
                    Xi,
                    GetHeight(Xi + (lx), Yi + (ly), getRatio(Xi + lx, Yi + ly), MaxLOD),
                    Yi);

                uv[id] = gridUV(x, y);
                //normals[id] = GetNormal(Xi + lx, Yi + ly);	// Disabled for now, Being reworked in a sperate branch

                // Build Triangles
                AddTriangle(x, y, id, TSize, tri);
                //yield return null;
            }
            if (!RunInstant)
                yield return null;
        }

        m.vertices = verts;
        m.uv = uv;
        m.triangles = tri;
        m.normals = normals;
        // yield return null;
        m.RecalculateNormals();
        m.RecalculateBounds();

        HoldsM.GetComponent<MeshFilter>().mesh = m;


        if (!RunInstant)
            yield return null;
        Debug.Log("Building Physics Mesh for Tile: " + Tx + " " + Ty);
        // Build Physics Mesh	
        m = new Mesh();
        TSize = P_VertexCount + 1;
        verts = new Vector3[TSize * TSize];
        uv = new Vector2[TSize * TSize];
        tri = new int[6 * ((TSize - 1) * (TSize - 1))];
        VertDis = ((float)TileSize) / P_VertexCount;
        for (int y = 0; y < TSize; y++)
        {
            for (int x = 0; x < TSize; x++)
            {
                int id = y * TSize + x;
                float Xi = (x * VertDis) - (TileSize / 2);
                float Yi = (y * VertDis) - (TileSize / 2);
                verts[id] = new Vector3(
                    Xi,
                    GetHeight(Xi + (lx), Yi + (ly), getRatio(Xi + lx, Yi + ly), PhysOctaveCount),
                    Yi);

                // Build Triangles
                AddTriangle(x, y, id, TSize, tri);
                //yield return null;
            }
            if (!RunInstant)
                yield return null;
        }

        m.vertices = verts;
        m.uv = uv;
        m.triangles = tri;
        // yield return null;
        m.RecalculateNormals();
        m.RecalculateBounds();

        if (!RunInstant)
            yield return null;
        (HoldsM.GetComponent<MeshCollider>() == null ? HoldsM.AddComponent<MeshCollider>() : HoldsM.GetComponent<MeshCollider>()).sharedMesh = m;
        HoldsM.GetComponent<MeshRenderer>().enabled = true;

        
        if (buildDecor || SpawnPOIs)
        {
            Debug.Log("Building Decor for Tile: " + Tx + " " + Ty);
            BuildDecor(Tx, Ty, HoldsM);
            Debug.Log("Tile Decor Built: " + Tx + " " + Ty);
        }
        if (!RunInstant)
        {
            yield return null;
        }
        if (TilesBuilding.Count <= 0)
        {
            TilesBuilding.RemoveAt(0);
        }
        Debug.Log("Tile Built: " + Tx + " " + Ty);
        TileLoaded?.Invoke(Tx, Ty);
        if (!(TilesBuilding.Count > 0))
        {
            WorldGenFinish.Invoke();
            Debug.Log("World Gen Finished");
        }
    }

    Vector3 GetNormal(float x, float z)
    {
        float delta = 5f;
        Vector3 A = new Vector3(x - delta, GetHeight(x - delta, z - delta), z - delta);
        Vector3 B = new Vector3(x + delta, GetHeight(x + delta, z + delta), z + delta);
        Vector3 C = new Vector3(x - delta, GetHeight(x - delta, z + delta), z + delta);
        Vector3 D = new Vector3(x + delta, GetHeight(x + delta, z - delta), z - delta);

        Vector3 normal = Vector3.Cross(D - C, A - B);
        //Debug.Log(normal);
        return normal.normalized;
    }

    private GameObject BuildTile(int Tx, int Ty)
    {

        GameObject tile = new GameObject("TerrainTile_");
        tile.transform.parent = transform;

        return BuildTile(Tx, Ty, tile);
    }

    private Vector2 CalcUV(int x, int y)
    {
        int xu = x % 4;
        int yu = y % 4;
        float u, v;

        if (xu > 2)
        {
            xu = (xu - 2) % 3;
        }
        else
        {
            xu = xu % 3;
        }
        u = xu / 2.0f;

        if (yu > 2)
        {
            yu = (yu - 2) % 3;
        }
        else
        {
            yu = yu % 3;
        }
        v = yu / 2.0f;
        return new Vector2(Mathf.Abs(u), Mathf.Abs(v));
    }

    private Vector2 gridUV(int x, int y)
    {
        return new Vector2((float)x / V_VertexCount, (float)y / V_VertexCount);
    }
    private void AddTriangle(int x, int y, int id, int TSize, int[] tri)
    {
        if (y < TSize - 1 && x < TSize - 1)
        {
            int T_id = (y * (TSize - 1) + x) * 6;
            tri[T_id] = id;
            tri[T_id + 1] = id + TSize;
            tri[T_id + 2] = id + 1;

            tri[T_id + 3] = id + 1;
            tri[T_id + 4] = id + TSize;
            tri[T_id + 5] = id + 1 + TSize;
        }
    }
    //TODO
    private GameObject BuildTile(int Tx, int Ty, GameObject tile)
    {
        tile.transform.parent = gameObject.transform;
        ClearChildren(tile);
        tile.AddComponent<MeshFilter>();
        if (!UseManyMats)
        {
            tile.AddComponent<MeshRenderer>().material = mats[0];
        }
        else
        {
            tile.AddComponent<MeshRenderer>().materials = mats;
        }

        BuildMesh(Tx, Ty, tile);
        //tile.AddComponent<MeshCollider>().sharedMesh = tile.GetComponent<MeshFilter>().sharedMesh;
        tile.AddComponent<TiledData>().parent = this;
        tile.transform.localScale = Vector3.one;
        tile.transform.position = Vector3.Scale(new Vector3(Tx * TileSize, 0, Ty * TileSize), transform.localScale);

        // if (buildDecor || SpawnPOIs)
        // {
        //     BuildDecor(Tx, Ty, tile);
        // }


        return tile;
    }

    public float GetHeight(float px, float py)
    {
        return GetHeight(px, py, getRatio(px, py));
    }

    private float GetHeight(float px, float py, float ratio = 0.0f, int LOD = 0)
    {

        float output = 0;
        for (int i = OctaveCount - 1; i >= LOD; i--)
        {
            output += TerrainData[i].getHeight(px, py);
        }
        output *= Mathf.Abs(TerrainScale.getHeight(px / smoothing, py / smoothing));

        //This really only needs done if we're close enought to something that modifies terrain.
        //We could store a list of modifiers and adjust from there.
        if (ratio != 0 && GetComponent<POI_Gen>() != null)
        {
            getRatio(px, py, out float height);

            output = (output * (1 - ratio)) + (height * ratio);
        }

        return output;
    }


    /* * * * * * * * * * * *
	 * 
	 *		Decor Generation and Placement
	 * 
	 * * * * * * * * * * * */
    #region Decor
    public void BuildDecor(int Tx, int Ty, GameObject tile)
    {
        ClearChildren(tile);

        // Handle Decor
        if (buildDecor)
        {
            BuildTileDecor(Tx, Ty, tile);
        }

        // Handle POIs
        if (SpawnPOIs)
        {
            BuildTilePOIs(Tx, Ty, tile);
        }
    }

    private Rect CalculateTileArea(int Tx, int Ty)
    {
        float minX = (Tx - 0.5f) * TileSize;
        float minZ = (Ty - 0.5f) * TileSize;
        return new Rect(minX, minZ, TileSize, TileSize);
    }

    // Updated usages
    private void BuildTileDecor(int Tx, int Ty, GameObject tile)
    {
        if (DecorChanceMap == null)
        {
            DecorChanceMap = ScriptableObject.CreateInstance<OggalNoise>().Intialize(Seeds[OctaveCount + 1], DecorChunkSize);
        }

        Rect TileArea = CalculateTileArea(Tx, Ty);

        List<GameObject> decorObjects = new List<GameObject>();
        decorObjects.AddRange(GetDecorinRect(TileArea));

        foreach (GameObject decor in decorObjects)
        {
            decor.transform.SetParent(tile.transform, true);
        }
    }

    private void BuildTilePOIs(int Tx, int Ty, GameObject tile)
    {
        var poiGen = GetComponent<POI_Gen>();
        if (poiGen == null)
        {
            return;
        }
        Rect TileArea = CalculateTileArea(Tx, Ty);

        foreach (var poiData in poiGen.GetPOIinRect(TileArea))
        {
            var poi = poiData.GetGameObject();
            foreach (var listener in poi.GetComponentsInChildren<POI_Listener>())
            {
                listener.OnSpawned.Invoke(poiData.ID,poiData.Seed);
            }
            poi.transform.SetParent(tile.transform, true);
        }
    }

    private GameObject[] GetDecorinRect(Rect area)
    {
        List<GameObject> AllDecore = new List<GameObject>();

        int xMinIndex, xMaxIndex, zMinIndex, zMaxIndex;

        xMinIndex = Mathf.RoundToInt((area.xMin) / DecorChunkSize) - 1;
        xMaxIndex = Mathf.RoundToInt((area.xMax) / DecorChunkSize) + 1;
        zMinIndex = Mathf.RoundToInt((area.yMin) / DecorChunkSize) - 1;
        zMaxIndex = Mathf.RoundToInt((area.yMax) / DecorChunkSize) + 1;

        for (int _Z = zMinIndex; _Z <= zMaxIndex; _Z++)
        {
            for (int _X = xMinIndex; _X <= xMaxIndex; _X++)
            {
                Vector2Int chunkIndex = new Vector2Int(_X, _Z);
                DecorChunk dChunk;
                DecorChunks.TryGetValue(chunkIndex, out dChunk);

                if (dChunk == null)
                {
                    dChunk = new DecorChunk(chunkIndex,
                        Mathf.RoundToInt(DecorChanceMap.getHeight(_X, _Z) * chunkIndex.sqrMagnitude) + Seeds[OctaveCount+1]
                        , this);

                    DecorChunks.Add(chunkIndex, dChunk);
                }
                AllDecore.AddRange(dChunk.getDecor(area));
            }
        }


        return AllDecore.ToArray();
    }


    #endregion

    #endregion

    #region World Control
    public Vector2Int getWorldCenter(Vector3 pos)
    {
        return new Vector2Int(Mathf.RoundToInt(pos.x / TileSize), Mathf.RoundToInt(pos.z / TileSize));
    }

    private void CheckWorldCenter()
    {

        if (Player != null)
        {
            if (Player.transform.position.x / (TileSize * transform.localScale.x) > localX + ((float)Radius / 2))
            {
                MoveX(true);
            }
            if (Player.transform.position.x / (TileSize * transform.localScale.x) < localX - ((float)Radius / 2))
            {
                MoveX(false);
            }
            if (Player.transform.position.z / (TileSize * transform.localScale.z) > localZ + ((float)Radius / 2))
            {
                MoveY(true);
            }
            if (Player.transform.position.z / (TileSize * transform.localScale.z) < localZ - ((float)Radius / 2))
            {
                MoveY(false);
            }
        }
    }


    private void MoveX(bool t)
    {
        Debug.Log("Moving X: " + t);
        if (t)
        {
            localX++;
            //East
            for (int i = 0; i < TRadius; i++)
            {
                int id = i * TRadius;
                Vector3 p = new Vector3((localX + Radius) * TileSize, 0, (localZ + (Radius - i)) * TileSize);
                int X = (int)Tiles[id].transform.position.x/TileSize;
                int Y = (int)Tiles[id].transform.position.y / TileSize;

                TileUnloaded?.Invoke(X,Y);
                UpdateMesh(localX + Radius, localZ + (Radius - i), Tiles[id]);
                Tiles[id].transform.position = p;
            }

            for (int y = 0; y < TRadius; y++)
            {
                GameObject tem = Tiles[y * TRadius];
                for (int x = 0; x < TRadius - 1; x++)
                {
                    Tiles[y * TRadius + ((x))] = Tiles[y * TRadius + ((x + 1) % TRadius)];
                }
                Tiles[y * (TRadius) + TRadius - 1] = tem;
            }


        }
        else
        {
            localX--;
            //West
            for (int i = 0; i < TRadius; i++)
            {
                int id = ((i + 1) * TRadius) - 1;
                Vector3 p = new Vector3((localX - Radius) * TileSize * transform.localScale.x, 0, (localZ + (Radius - i)) * TileSize * transform.localScale.z);
                int X = (int)Tiles[id].transform.position.x / TileSize;
                int Y = (int)Tiles[id].transform.position.y / TileSize;

                TileUnloaded?.Invoke(X, Y);
                UpdateMesh(localX - Radius, localZ + (Radius - i), Tiles[id]);
                Tiles[id].transform.position = p;
            }

            for (int y = 0; y < TRadius; y++)
            {
                GameObject tem = Tiles[y * TRadius + TRadius - 1];
                for (int x = TRadius - 1; x > 0; x--)
                {
                    Tiles[y * TRadius + x] = Tiles[y * TRadius + ((x + TRadius - 1) % TRadius)];
                }
                Tiles[y * TRadius] = tem;
            }


        }

    }

    private void MoveY(bool t)
    {
        Debug.Log("Moving Y: " + t);
        if (t)
        {
            //North
            localZ++;
            for (int i = 0; i < TRadius; i++)
            {
                int id = (TRadius * (TRadius - 1)) + i;

                Vector3 p = new Vector3((localX + (i - Radius)) * TileSize * transform.localScale.x, 0, (localZ + Radius) * TileSize * transform.localScale.z);
                int X = (int)Tiles[id].transform.position.x / TileSize;
                int Y = (int)Tiles[id].transform.position.y / TileSize;

                TileUnloaded?.Invoke(X, Y);
                UpdateMesh(localX + (i - Radius), (localZ + Radius), Tiles[id]);
                Tiles[id].transform.position = p;
            }

            for (int x = 0; x < TRadius; x++)
            {
                GameObject tem = Tiles[TRadius * (TRadius - 1) + x];
                for (int y = TRadius - 1; y > 0; y--)
                {
                    Tiles[y * TRadius + x] = Tiles[((y + (TRadius - 1)) % TRadius) * TRadius + x];
                }
                Tiles[x] = tem;
            }

        }
        else
        {
            //South
            localZ--;
            for (int i = 0; i < TRadius; i++)
            {

                Vector3 p = new Vector3((localX + (i - Radius)) * TileSize * transform.localScale.x, 0, (localZ - Radius) * TileSize * transform.localScale.z);
                int X = (int)Tiles[i].transform.position.x / TileSize;
                int Y = (int)Tiles[i].transform.position.y / TileSize;

                TileUnloaded?.Invoke(X, Y);
                UpdateMesh((localX + (i - Radius)), (localZ - Radius), Tiles[i]);
                Tiles[i].transform.position = p;

            }

            for (int x = 0; x < TRadius; x++)
            {
                GameObject tem = Tiles[x];
                for (int y = 0; y < TRadius - 1; y++)
                {
                    Tiles[y * TRadius + x] = Tiles[((y + 1) % TRadius) * TRadius + x];
                }
                Tiles[(TRadius * (TRadius - 1)) + x] = tem;
            }

        }
    }

#endregion

}
