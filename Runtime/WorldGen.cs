using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.Events;

public class WorldGen : MonoBehaviour {
	public int MaxLOD = 0;

    public bool drawRadius = true;       // if true, Draw a Wireframe grid where the World will be generated.
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
	

	[Header("World Contents")]
	public bool UseCityGrid = false;


	[Space]
	[Header("Experimental")]
	public bool buildTrees = false;
	public float TreeChunkSize = 500;
	public float TreeDensity = 1;
	private TerrainNoise TreeChanceMap;
	private Dictionary<Vector2Int, TreeData[]> TreeChunks = new Dictionary<Vector2Int, TreeData[]>();

	[SerializeField]
	internal List<TreeData.TreeInfo> Trees;

	[SerializeField]
	List<TerrainNoiseModifier> Modifiers;


    private int TRadius;
    private GameObject[] Tiles;
	
    private int[] Seeds;

    private TerrainNoise[] Octaves;
    private TerrainNoise ScaleMap;

	private int TilesBuilding = 0;

    public void Start()
    {
        if(BuildOnStart)
            BuildWorld();
    }

    // Update is called once per frame
    void Update () {

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
    
    private void OnDrawGizmosSelected()
    {

		if (drawRadius)
		{
			Vector3[] verts = { new Vector3(TileSize * -0.5f, 0, TileSize * -0.5f), new Vector3(TileSize * 0.5f, 0, TileSize * -0.5f), new Vector3(TileSize * -0.5f, 0, TileSize * 0.5f), new Vector3(TileSize * 0.5f, 0, TileSize * 0.5f) };
			Vector2[] uv = { new Vector2(0, 0), new Vector2(1, 0), new Vector2(0, 1), new Vector2(1, 1) };
			int[] tri = { 1, 0, 2, 1, 2, 3 };

			Mesh m = new Mesh
			{
				vertices = verts,
				uv = uv,
				triangles = tri
			};
			m.RecalculateNormals();
			Gizmos.color = Color.yellow;
			for (int y = Radius * -1; y <= Radius; y++)
			{
				for (int x = Radius * -1; x <= Radius; x++)
				{

					Gizmos.DrawWireMesh(m, Vector3.Scale(transform.localScale, new Vector3(x * TileSize, 0, y * TileSize)), Quaternion.identity, transform.localScale);
				}
			}

		}

		if (false)
		{
			//PANIC?
		}
        
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

	//TODO Document This Function
    public void BuildWorld()
    {
		if(Trees == null) { Trees = new List<TreeData.TreeInfo>(); }
		WorldGenStart.Invoke();
			
		//Get 'True Raduis' or Diameter
        TRadius = Radius * 2 + 1;
		
		//Cener The World
        localX = 0;
        localZ = 0;
		
		//If we are not using a preset Seed generate a new one
        if (!UseSeed)
            Seed = Random.Range(int.MinValue,int.MaxValue);
		Seeds = new int[OctaveCount + 2];
		Seeds[0] = Seed;
			
		System.Random r = new System.Random(Seed);  //Create the Random object to be used
		Seeds[OctaveCount + 1] = r.Next();

		//Intialize the Octaves

		Octaves = new TerrainNoise[OctaveCount];
		
		
		//Define the ScaleMap
        ScaleMap = new TerrainNoise(Seed, 3);
		
		
		//Construct the Octaves
        for (int OctaveIndex = 0; OctaveIndex < OctaveCount; OctaveIndex++)
        {
            Seeds[OctaveIndex+1] = r.Next();//Get the Seed for Each Octave
            Octaves[OctaveIndex] = new TerrainNoise(Seeds[OctaveIndex], Mathf.RoundToInt(Mathf.Pow(5, OctaveIndex)));//New Terrain Noise (Seed, Grid Size(5^Octave)
        }
		

		//Build the Tile Grid
		Tiles = new GameObject[TRadius * TRadius];  //Define The tiles in code



		ClearChildren();                //Clear Old Tiles and Trees
		TreeChanceMap = null;           //Clear Tree Data
		TreeChunks  = new Dictionary<Vector2Int, TreeData[]>();

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
		//WorldGenFinish.Invoke();

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


    private float getRatio(float pX,float pY,out float height)
    {
		height = 0;
		if (UseCityGrid)
		{
			float ratio = 0;
			foreach(TerrainNoiseModifier TNM in Modifiers)
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
		
		if (UseCityGrid)
		{
			float ratio = 0;
			foreach (TerrainNoiseModifier TNM in Modifiers)
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


	private void UpdateMesh(int Tx, int Ty, GameObject m,bool RunInstant = false)
    {
		TilesBuilding++;
		IEnumerator i = BuildMeshSlow(Tx, Ty, m,RunInstant);
		ClearChildren(m);
        StartCoroutine(i);
    }

    private IEnumerator BuildMeshSlow(int Tx, int Ty, GameObject HoldsM,bool RunInstant = false)
    {
		
        HoldsM.GetComponent<MeshRenderer>().enabled = false;
        Mesh m = HoldsM.GetComponent<MeshFilter>().sharedMesh;
        if (m == null)
        {
            m = new Mesh();
        }
        int TSize = V_VertexCount + 1;
        Vector3[] verts = new Vector3[TSize * TSize];
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
                float Xi = (x*VertDis) - (TileSize / 2);
                float Yi = (y*VertDis) - (TileSize / 2);
                verts[id] = new Vector3(
                    Xi,
                    GetHeight(Xi + (lx), Yi + (ly), getRatio(Xi + lx, Yi + ly),MaxLOD),
                    Yi);
#region UV map
				int xu = x % 4;
                int yu = y % 4;
                float u;
                if (xu > 2)
                {
                    xu = (xu - 2) % 3;
                }
                else
                {
                    xu = xu % 3;
                }
                u = xu / 2.0f;
                float v;
                if (yu > 2)
                {
                    yu = (yu - 2) % 3;
                }
                else
                {
                    yu = yu % 3;
                }
                v = yu / 2.0f;

                uv[id] = new Vector2(Mathf.Abs(u), Mathf.Abs(v));
#endregion
				// Build Triangles
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
				//yield return null;
			}
			if(!RunInstant)
				yield return null;
        }
		
		m.vertices = verts;
        m.uv = uv;
        m.triangles = tri;
       // yield return null;
        m.RecalculateNormals();
        m.RecalculateBounds();
        
        HoldsM.GetComponent<MeshFilter>().mesh = m;
       
        HoldsM.GetComponent<MeshRenderer>().enabled = true;

		if (!RunInstant)
			yield return null;

	
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
					GetHeight(Xi + (lx), Yi + (ly), getRatio(Xi + lx, Yi + ly),PhysOctaveCount),
					Yi);

				// Build Triangles
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


		if (buildTrees)
		{
			BuildTrees(Tx, Ty, HoldsM);
		}

		if (--TilesBuilding == 0)
		{
			WorldGenFinish.Invoke();
		}

	}

	private GameObject BuildTile(int Tx, int Ty)
    {
		
		GameObject tile = new GameObject("TerrainTile_");
        tile.transform.parent = transform;


        return BuildTile(Tx, Ty, tile);
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
	
		if (buildTrees)
		{
			BuildTrees(Tx,Ty,tile);
		}
		

		return tile;
    }

	public float GetHeight(float px, float py)
	{
		return GetHeight(px, py, getRatio(px, py));
	}


    private float GetHeight(float px, float py, float ratio = 0.0f,int LOD = 0)
    {

		
		
        float output = 0;
        for (int i = OctaveCount-1; i >=LOD ; i--)
        {
            output += Octaves[i].getHeight(px, py);
        }
        output *= Mathf.Abs(ScaleMap.getHeight(px / SmoothingX, py / SmoothingZ));

		//This really only needs done if we're close enought to something that modifies terrain.
		//We could store a list of modifiers and adjust from there.
        if (Modifiers.Count!=0 && ratio !=0)
        {
			getRatio(px, py, out float height);

            output = (output * (1 - ratio)) + (height * ratio);
        }
		
        return output;
    }


	/* * * * * * * * * * * *
	 * 
	 *		Tree Generation and Placement
	 * 
	 * * * * * * * * * * * */
#region Trees
	public void BuildTrees(int Tx, int Ty, GameObject tile)
	{
		ClearChildren(tile);
		if(TreeChanceMap == null)
		{
			TreeChanceMap = new TerrainNoise(Seeds[OctaveCount+1], 20);
		}
		//Find the bounds of the Tile
		float minX, minZ;
		minX = TileSize * -0.5f + (Tx * TileSize);
		minZ = TileSize * -0.5f + (Ty * TileSize);



		//Get ALL trees in the Tree Chunks that intercept our bounds
		TreeData[] Trees = GetTreesinRect(new Rect(minX, minZ, TileSize, TileSize));

		foreach (TreeData t in Trees)
		{
			//t.BuildTree(tile);
			TreeData.TreeInfo _T = t.GetTree();
			GameObject.Instantiate(_T.tree,
				new Vector3(t.location.x,	GetHeight(t.location.x, t.location.y, getRatio(t.location.x, t.location.y)),	t.location.y),
				Quaternion.Euler(_T.RotateAll? t.Rot:new Vector3(0,t.Rot.y,0)),
				tile.transform);
		}
		//Spawn Remaining Trees
	}

	private TreeData[] GetTreesinRect(Rect area)
	{

		List<TreeData> AllTrees = new List<TreeData>();
		int xMinIndex, xMaxIndex, zMinIndex, zMaxIndex;
		xMinIndex = Mathf.RoundToInt((area.xMin))/Mathf.RoundToInt(TreeChunkSize);
		if (area.xMin < 0) { xMinIndex--; }
		xMaxIndex = Mathf.RoundToInt((area.xMax)) / Mathf.RoundToInt(TreeChunkSize);
		if(area.xMax < 0) { xMaxIndex--; }
		zMinIndex = Mathf.RoundToInt((area.yMin)) / Mathf.RoundToInt(TreeChunkSize);
		if (area.yMin < 0) { zMinIndex--; }
		zMaxIndex = Mathf.RoundToInt((area.yMax)) / Mathf.RoundToInt(TreeChunkSize);
		if (area.yMax < 0) { zMaxIndex--; }

		for(int loopZ = zMinIndex; loopZ <= zMaxIndex; loopZ++)
		{
			for(int loopX = xMinIndex; loopX <= xMaxIndex; loopX++)
			{
				
					TreeData[] Chunk;
					TreeChunks.TryGetValue(new Vector2Int(loopX, loopZ), out Chunk);

				if (Chunk == null)
				{
					List<TreeData> newChunk = new List<TreeData>();
					float seed = TreeChanceMap.getHeight(loopX +TreeChunkSize*localX, loopZ+TreeChunkSize * localZ, 0.5f);
					System.Random TreeChunk = new System.Random(Mathf.RoundToInt(seed*seed*100));
					for (int treeIndex = 0; treeIndex < TreeChunkSize * TreeDensity; treeIndex++)
					{
							//Debug.Log("New Tree Made!");
						newChunk.Add(new TreeData(
							new Vector2((float)(TreeChunk.NextDouble() * (TreeChunkSize + 1)) + (loopX * TreeChunkSize), (float)(TreeChunk.NextDouble() * (TreeChunkSize + 1)) + (loopZ * TreeChunkSize)),
							(TreeChunk.Next()),
							this
							));
					}
					Chunk = newChunk.ToArray();
					TreeChunks.Add(new Vector2Int(loopX, loopZ), Chunk);
				}

				AllTrees.AddRange(Chunk);
				
				
			}
		}

		List<TreeData> FixedTrees = new List<TreeData>();
		foreach(TreeData t in AllTrees)
		{
			if (area.Contains(t.location))
				FixedTrees.Add(t);
		}

		return FixedTrees.ToArray();

	}

	public void AddNewTree(GameObject t, bool r)
	{
		
		if(Trees == null) { Trees = new List<TreeData.TreeInfo>(); }
		TreeData.TreeInfo NTree = new TreeData.TreeInfo
		{
			tree = t,
			RotateAll = r
		};
		Trees.Add(NTree);

	}


#endregion

#endregion

#region World Control
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


#if UNITY_EDITOR
	private GameObject NewTree;
	private bool RotateAll;
	private bool sober;
	public void TreeMenu()
	{
		// SerializedObject tree = NewTree;
		NewTree = EditorGUILayout.ObjectField(new GUIContent("Tree"), NewTree, typeof(GameObject),false) as GameObject;
		RotateAll = EditorGUILayout.Toggle(new GUIContent("Rotate On All Axis"), RotateAll);
		if (NewTree != null && GUILayout.Button(new GUIContent("Add Tree To List")))
		{
			AddNewTree(NewTree, RotateAll);
		}
		foreach(TreeData.TreeInfo g in Trees)
		{
			GUILayout.Label(new GUIContent(g.tree.name+ " "+ g.RotateAll));
		}
		sober = (GUILayout.Toggle(sober,new GUIContent("ARE YOU SOBER???")));
		if (sober) {
			if (GUILayout.Button(new GUIContent("Clear Trees")))
			{
				Trees = new List<TreeData.TreeInfo>();
			}
		}

	}

#endif


}





