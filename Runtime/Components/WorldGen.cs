using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

[SelectionBase]
public class WorldGen : MonoBehaviour {
	public int MaxLOD = 0;

   	[Space]
    [Header("World Data")]
    public bool UseSeed = true;			// if false, will generate a new seed for the world on build
    public int Seed = 0; 

    public bool BuildOnStart = true;	//if false, will NOT build a new world when the game runs
    [Space]
    public GameObject Player;			// Gameobject that will be tracked to keep world centered.
    [Range(1,10)]
    public int OctaveCount = 4;			// Levels for Max Detail World
	public int PhysOctaveCount = 4;		// DEV Var to test Generating a collison mesh with less layers.(UN-USED)

	private TerrainNoiseObject FallbackNoiseObject{ get { return ScriptableObject.CreateInstance<ConstantNoiseObject>(); }  }

    public Material[] mats;
	public bool UseManyMats = false;
	[Space]
    public float SmoothingX = 100;
    public float SmoothingZ = 100;


    private int localX = 0;
    private int localZ = 0;
	[Header("Tile Data")]
	public int TileSize = 100;
    [Range(3,200)]
    public int V_VertexCount= 100;
	[Range(3,200)]
	public int P_VertexCount = 50;
    [Range(0, 10),Tooltip("Radius of Tiles beyond center")]
    public int Radius;



	[Header("Events")]
	[Tooltip("Called at Start of Initial World Generation")]
	public UnityEvent WorldGenStart;
	
	[Tooltip("Called When WorldGen Finishes")]
	public UnityEvent WorldGenFinish;

	[Tooltip("Called before World Generation starts")]
	public UnityEvent WorldGenPreInit;

	[Header("World Contents")]
	[Tooltip("Should the generator check for POI Objects")]
	public bool SpawnPOIs= false;

	[Space]
	[Header("Decor Settings")]
	public bool buildDecor = false;
	public float DecorChunkSize = 500;
	[Range(0,1),Tooltip("Chance to beat to place Decor")]
	public float DecorDensity = 1;
	[Min(0) ,Tooltip("Number of attempts to place Decor per DecorChunk")]
	public int DecorAttempts = 100;
	private TerrainNoiseObject DecorChanceMap;
	private Dictionary<Vector2Int, DecorChunk> DecorChunks = new Dictionary<Vector2Int, DecorChunk>();
	[SerializeField]
	public List<DecorTemplate> decorObjects = new List<DecorTemplate>();
	[Space]
	[Header("Experimental")]



    private int TRadius;
    private GameObject[] Tiles;
	
    private int[] Seeds;

    [SerializeField]private TerrainNoiseObject[] Octaves;
    [SerializeField]private TerrainNoiseObject ScaleMap;

	private List<IEnumerator> TilesBuilding = new List<IEnumerator>();

    public void Start()
    {
        if(BuildOnStart)
            BuildWorld();
    }

    // Update is called once per frame
    void Update () 
	{
		CheckWorldCenter();

	}
    



    public void ClearChildren(GameObject g)
    {
        while (g.transform.childCount > 0)
        {
#if !UNITY_EDITOR
			Destroy(g.transform.GetChild(0).gameObject);
#else
			DestroyImmediate(g.transform.GetChild(0).gameObject);
#endif
		}
    }

    public void ClearChildren()
    {
        ClearChildren(gameObject);
    }


#region World Gen

	private void ClearWorldData(){
		ClearChildren();
		DecorChunks = new Dictionary<Vector2Int, DecorChunk>();
		//Center The World
		localX = 0;
		localZ = 0;

		//Get 'True Raduis' or Diameter
        TRadius = Radius * 2 + 1;
		DecorChanceMap = null;
	}

    public void BuildWorld()
    {
        ClearWorldData();

        Init_Seeds();
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

                Tiles[id] = BuildTile(x, y);
                Tiles[id].name += id;
            }
        }
    }

    private void Init_Seeds()
    {
        //If we are not using a preset Seed generate a new one
        if (!UseSeed)
            Seed = Random.Range(int.MinValue, int.MaxValue);
        Seeds = new int[OctaveCount + 2];
        Seeds[0] = Seed;
		//Create the Random object to be used
        System.Random r = new System.Random(Seed);
        Seeds[OctaveCount + 1] = r.Next();

        //Intialize the Octaves
        if (Octaves == null || Octaves.Length != OctaveCount)
            Octaves = new TerrainNoiseObject[OctaveCount];

        //Define the ScaleMap
        PrepareNoise(ref ScaleMap);
        ScaleMap.Intialize(Seed, 3);
        //Construct the Octaves
        for (int OctaveIndex = 0; OctaveIndex < OctaveCount; OctaveIndex++)
        {	
			//Get the Seed for Each Octave
            Seeds[OctaveIndex + 1] = r.Next();
            PrepareNoise(ref Octaves[OctaveIndex]);
            Octaves[OctaveIndex].Intialize(Seeds[OctaveIndex], Mathf.RoundToInt(Mathf.Pow(5, OctaveIndex)));//New Terrain Noise (Seed, Grid Size(5^Octave)
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
			UpdateMesh(Tx, Ty, HoldsMesh,true);

			//BuildMeshSingle(Tx, Ty,HoldsMesh);
		}
	}

    private float getRatio(float pX, float pY, out float height)
    {
		height = 0;
		if (SpawnPOIs && GetComponent<POI_Gen>() != null)
		{
			TerrainNoiseModifier[] Mods =GetComponent<POI_Gen>()?.Mods;
			float ratio = 0;
			foreach(TerrainNoiseModifier TNM in Mods)
			{
				if(ratio <TNM.GetRatio(pX, pY))
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
			TerrainNoiseModifier[] Mods =GetComponent<POI_Gen>()?.Mods;
			float ratio = 0;
			foreach (TerrainNoiseModifier TNM in Mods)
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
		
		IEnumerator i = BuildMeshSlow(Tx, Ty, m,RunInstant);
		TilesBuilding.Add(i);
		StartCoroutine(i);
		//Should add the coroutine to a list, when the list is empty call the finished event
    }

    private IEnumerator BuildMeshSlow(int Tx, int Ty, GameObject HoldsM, bool RunInstant = false)
    {
		if(HoldsM){ClearChildren(HoldsM);}
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
		for (int y = 0; y < TSize; y++)
        {
            for (int x = 0; x < TSize; x++)
            {
                int id = y * TSize + x;
                float Xi = (x * VertDis) - (TileSize / 2);
                float Yi = (y * VertDis) - (TileSize / 2);
                verts[id] = new Vector3(
                    Xi,
                    GetHeight(Xi + (lx), Yi + (ly), getRatio(Xi + lx, Yi + ly),MaxLOD),
                    Yi);
#region UV map
                uv[id] = gridUV(x,y);
				normals[id] = GetNormal(Xi + lx, Yi + ly);
#endregion
				// Build Triangles
				AddTriangle(x,y,id,TSize,tri);
				//yield return null;
			}
			if(!RunInstant)
				yield return null;
        }
		
		m.vertices = verts;
        m.uv = uv;
        m.triangles = tri;
		m.normals = normals;
       // yield return null;
        //m.RecalculateNormals();
        m.RecalculateBounds();
        
        HoldsM.GetComponent<MeshFilter>().mesh = m;
       
        HoldsM.GetComponent<MeshRenderer>().enabled = true;

		if (!RunInstant)
			yield return null;

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
			AddTriangle(x,y,id,TSize,tri);
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


		if (buildDecor || SpawnPOIs)
		{
			BuildDecor(Tx, Ty, HoldsM);
		}
		TilesBuilding.RemoveAt(0);
		if (!(TilesBuilding.Count > 0))
		{
			WorldGenFinish.Invoke();
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
		return new Vector2 (Mathf.Abs(u),Mathf.Abs(v));
	}
	
	private Vector2 gridUV(int x, int y)
	{
		return new Vector2((float)x / V_VertexCount, (float)y / V_VertexCount);
	}
	private void AddTriangle(int x, int y, int id, int TSize, int[] tri){
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
   
        BuildMesh(Tx, Ty,tile);
		//tile.AddComponent<MeshCollider>().sharedMesh = tile.GetComponent<MeshFilter>().sharedMesh;
		tile.AddComponent<TiledData>().parent = this;
		tile.transform.localScale = Vector3.one;
        tile.transform.position = Vector3.Scale(new Vector3(Tx * TileSize, 0, Ty * TileSize), transform.localScale);
	
		if (buildDecor || SpawnPOIs)
		{
			BuildDecor(Tx,Ty,tile);
		}
		

		return tile;
    }

	public float GetHeight(float px, float py)
	{
		return GetHeight(px, py, getRatio(px, py));
	}


    private float GetHeight(float px, float py, float ratio = 0.0f, int LOD = 0)
    {

        float output = 0;
        for (int i = OctaveCount-1; i >=LOD ; i--)
        {
            output += Octaves[i].getHeight(new Vector2(px, py));
        }
        output *= Mathf.Abs(ScaleMap.getHeight(new Vector2(px / SmoothingX, py / SmoothingZ)));

		//This really only needs done if we're close enought to something that modifies terrain.
		//We could store a list of modifiers and adjust from there.
        if (GetComponent<POI_Gen>() != null && ratio !=0)
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
		//Check if DecorChanceMap has been generated.
		if(DecorChanceMap == null)
		{	
			PrepareNoise(ref DecorChanceMap);
			DecorChanceMap.Intialize(Seeds[OctaveCount+1], 20);
		}
		//Find the bounds of the Tile
		float minX, minZ;
		minX = ((Tx - 0.5f) * TileSize);
		minZ = ((Ty - 0.5f) * TileSize);

		Rect TileArea =new Rect(minX,minZ,TileSize,TileSize);
		List<GameObject> Decor = new List<GameObject>();
		if (buildDecor)
			Decor.AddRange(GetDecorinRect(TileArea));
		if (GetComponent<POI_Gen>())
			Decor.AddRange(GetComponent<POI_Gen>().GetPOIinRect(TileArea));
		
	
			foreach (GameObject deco in Decor)
			{
				deco.transform.SetParent(tile.transform, true);
			}
		
	}


	private GameObject[] GetDecorinRect(Rect area)
	{
		List<GameObject> AllDecore = new List<GameObject>();

		int xMinIndex, xMaxIndex, zMinIndex, zMaxIndex;

		xMinIndex = Mathf.RoundToInt((area.xMin) / DecorChunkSize);
		xMaxIndex = Mathf.RoundToInt((area.xMax) / DecorChunkSize);
		zMinIndex = Mathf.RoundToInt((area.yMin) / DecorChunkSize);
		zMaxIndex = Mathf.RoundToInt((area.yMax) / DecorChunkSize);
		

		for(int _Z = zMinIndex; _Z <= zMaxIndex; _Z++)
		{
			for(int _X = xMinIndex; _X <= xMaxIndex; _X++)
			{
				Vector2Int chunkIndex = new Vector2Int(_X,_Z);
					DecorChunk dChunk;
					DecorChunks.TryGetValue(chunkIndex, out dChunk);
					
				if (dChunk == null){
					dChunk = new DecorChunk(chunkIndex,
						Mathf.RoundToInt(DecorChanceMap.getHeight(new Vector2(_X,_Z))*chunkIndex.sqrMagnitude)
						,this);
					
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

	private void CheckWorldCenter()
	{

		if (Player != null)
		{
			if (Player.transform.position.x / (TileSize * transform.localScale.x) > localX + ((float)Radius / 2))
			{
				Debug.Log("Player is East!");

				MoveX(true);
			}
			if (Player.transform.position.x / (TileSize * transform.localScale.x) < localX - ((float)Radius / 2))
			{
				Debug.Log("Player is West!");

				MoveX(false);
			}
			if (Player.transform.position.z / (TileSize * transform.localScale.z) > localZ + ((float)Radius / 2))
			{
				Debug.Log("Player is North!");

				MoveY(true);
			}
			if (Player.transform.position.z / (TileSize * transform.localScale.z) < localZ - ((float)Radius / 2))
			{
				Debug.Log("Player is South!");

				MoveY(false);
			}
		}
	}
	public void MoveX(bool t)
    {
        if (t)
        {
			
			//Old Move East Code
            localX++;
            //East
            for (int i = 0; i < TRadius; i++)
            {
                int id = i * TRadius;
               // GameObject ti = Tiles[id];
                Vector3 p = new Vector3((localX + Radius) * TileSize, 0, (localZ + (Radius - i)) * TileSize);
                UpdateMesh(localX + Radius, localZ + (Radius -i), Tiles[id]);
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
        else {
            localX--;
            //West
            for (int i = 0; i < TRadius; i++)
            {
                int id = ((i + 1) * TRadius) - 1;
               // GameObject ti = Tiles[id];
                 Vector3 p = new Vector3((localX - Radius) * TileSize * transform.localScale.x, 0, (localZ + (Radius - i)) * TileSize * transform.localScale.z);
              
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
        if (t)
        {
            //North
            localZ++;
            for (int i = 0; i < TRadius; i++)
            {
                int id = (TRadius * (TRadius - 1)) + i;
                
                Vector3 p = new Vector3((localX + (i - Radius)) * TileSize * transform.localScale.x, 0, (localZ + Radius) * TileSize * transform.localScale.z);
                //  ti.queMove(p);
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
               // ti.queMove(p);
               UpdateMesh((localX + (i - Radius)),(localZ - Radius),Tiles[i]);
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


private void PrepareNoise(ref TerrainNoiseObject noise)
{
	if (noise == null)
		noise = Instantiate(FallbackNoiseObject);
}

}





