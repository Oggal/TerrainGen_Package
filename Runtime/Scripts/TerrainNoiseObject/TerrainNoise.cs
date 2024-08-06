using System.Collections;
using System.Collections.Generic;
using System;


public interface ITerrainNoise
{
    float getHeight(float x, float y);
}

public class ConstantNoise : ITerrainNoise
{
    float height;
    public ConstantNoise(float Height)
    {
        height = Height;
    }
    public float getHeight(float x, float y)
    {
        return height;
    }
}

public class TerrainNoise : ITerrainNoise
{
    int Seed;
    public int SideSize = 5;
    Dictionary<int, double> VectorLib = new Dictionary<int, double>();

    public TerrainNoise(int i, int Size = 5)
    {
        Seed = i;
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
            //Need to remove dependance on system.random as it may
            //not preform ideally on all machines
            System.Random R1 = new System.Random(index);
            double O = R1.NextDouble();
            VectorLib.Add(index, O);
            return O;
        }
    }

    public float getHeight(float x, float y)
    {
        return getBaseHeight(x, y);
    }


    private float getBaseHeight(float x, float y)
    {
        //Find four corner points of the grid
        int rightX = getIndexedPoint(x);
        int bottomY = getIndexedPoint(y);
        int leftX = rightX + SideSize;
        int topY = bottomY + SideSize;

        //Assign Vectors to each point on the grid
        double bottomRight_VecRadian, topRight_VecRadian, bottomLeft_VecRadian, topLeft_VecRadian;
        bottomRight_VecRadian = getVector(rightX, bottomY);
        topRight_VecRadian = getVector(rightX, topY);
        bottomLeft_VecRadian = getVector(leftX, bottomY);
        topLeft_VecRadian = getVector(leftX, topY);
        //Calculate dot products between the corner's vector and relative position of the inquried point.
        //Since my vectors are stored as radians,convert to positions via sin and cos for the dot product.
        float d1, d2, d3, d4;
        d1 = DotProduct(Math.Cos(bottomRight_VecRadian * 2 * Math.PI), Math.Sin(bottomRight_VecRadian * 2 * Math.PI), rightX - x, bottomY - y);
        d2 = DotProduct(Math.Cos(topRight_VecRadian * 2 * Math.PI), Math.Sin(topRight_VecRadian * 2 * Math.PI), rightX - x, topY - y);
        d3 = DotProduct(Math.Cos(bottomLeft_VecRadian * 2 * Math.PI), Math.Sin(bottomLeft_VecRadian * 2 * Math.PI), leftX - x, bottomY - y);
        d4 = DotProduct(Math.Cos(topLeft_VecRadian * 2 * Math.PI), Math.Sin(topLeft_VecRadian * 2 * Math.PI), leftX - x, topY - y);

        //weighted adverage of the 4 dot products
        return weightedAverage(                     //Between the X's across Y
            weightedAverage(d1, d3, x - rightX),    //Across Bottom X's
            weightedAverage(d2, d4, x - rightX),    //Across Top X's
             y - bottomY);                      //Distance Across y
    }

    private int getIndexedPoint(float x)
    {
        int outPoint = (((int)x) / SideSize);
        outPoint = (x < 0 ? (outPoint - 1) : outPoint) * SideSize;
        return outPoint;
    }

    private int getID(int x, int y)
    {
        return (x >> 5) + y | x;

    }

    private float DotProduct(double x, double y, float xi, float yi)
    {
        return (float)((x * xi) + (y * yi));
    }

    private float weightedAverage(float a1, float a2, float w)
    {
        w = w / SideSize;

        return (1 - w) * a1 + w * a2;
    }


}
