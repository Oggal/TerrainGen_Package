using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class TreeData
{
		[System.Serializable]
		public struct TreeInfo
		{
			public GameObject tree;
			public bool RotateAll;
			
		}

		public Vector2 location;
		int Seed;
		public Vector3 Rot;
		System.Random treeRand;
		private WorldGen World;
		private TreeInfo thisTree;

		public TreeData(Vector2 loc, int _seed,WorldGen wg)
		{
			location = loc;
			Seed = _seed;
			treeRand = new System.Random(Seed);
		Rot = new Vector3((float)treeRand.NextDouble()*360, (float)treeRand.NextDouble()*360, (float)treeRand.NextDouble()*360);
		World = wg;
		thisTree = World.Trees[treeRand.Next(0, World.Trees.Count)];
	}
	/*
		public void BuildTree(GameObject Tile)
		{
			TreeInfo NTree = World.Trees[treeRand.Next(0, World.Trees.Count)];
			GameObject.Instantiate(NTree.tree, location, Quaternion.Euler(NTree.RotateAll ? Rot : Vector3.Scale(Rot, Vector3.up)), Tile.transform);
		}
	*/
		public TreeInfo GetTree()
		{
		return thisTree;
		}
}

