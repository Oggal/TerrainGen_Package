using System.Collections;
using System.Collections.Generic;
using System;



public class TerrainNoise
{
	int Seed;
	public int SideSize = 5;
	Dictionary<int, double> VectorLib = new Dictionary<int, double>();

	public TerrainNoise(object i, int Size = 5)
	{
		Seed = i.GetHashCode();
		SideSize = Size;
	}


	private double getVector(int x, int y)
	{
		int index = getID(x, y) * Seed;
		if (VectorLib.ContainsKey(index))
		{
			double O;
			VectorLib.TryGetValue(index, out O);
			return O;
		}
		else
		{

			System.Random R1 = new System.Random(index);
			double O = R1.NextDouble();
			VectorLib.Add(index, O);
			return O;
		}
	}

	public float getHeight(float x, float y, float o = 1)
	{
		return getBaseHeight(x, y);
	}


	private float getBaseHeight(float x, float y)
	{
		int x1 = (((int)x) / SideSize);
		int y1 = (((int)y) / SideSize);
		x1 = (x < 0 ? (x1 - 1) * SideSize : x1 * SideSize);
		y1 = (y < 0 ? (y1 - 1) * SideSize : y1 * SideSize);
		int x2 = x1 + SideSize;
		int y2 = y1 + SideSize;

		double v1, v2, v3, v4;
		v1 = getVector(x1, y1);
		v2 = getVector(x1, y2);
		v3 = getVector(x2, y1);
		v4 = getVector(x2, y2);



		float d1, d2, d3, d4;
		d1 = DotProduct(Math.Cos(v1 * 2 * Math.PI), Math.Sin(v1 * 2 * Math.PI), x1 - x, y1 - y);
		d2 = DotProduct(Math.Cos(v2 * 2 * Math.PI), Math.Sin(v2 * 2 * Math.PI), x1 - x, y2 - y);
		d3 = DotProduct(Math.Cos(v3 * 2 * Math.PI), Math.Sin(v3 * 2 * Math.PI), x2 - x, y1 - y);
		d4 = DotProduct(Math.Cos(v4 * 2 * Math.PI), Math.Sin(v4 * 2 * Math.PI), x2 - x, y2 - y);


		return wAdv(wAdv(d1, d3, x - x1), wAdv(d2, d4, x - x1), y - y1);
	}

	private int getID(int x, int y)
	{
		return (x >> 5) + y | x;

	}

	private float DotProduct(double x, double y, float xi, float yi)
	{
		return (float)((x * xi) + (y * yi));
	}

	private float wAdv(float a1, float a2, float w)
	{
		w = w / SideSize;

		return (1 - w) * a1 + w * a2;
	}


}
