using System.Runtime.CompilerServices;
using System;

namespace ClangenReborn.Noise;

public interface INoiseGenerator
{
    public uint Seed { get; }

    public float GetNoise(float X, float Y);
    public float[,] GetNoiseMap(int Width, int Height, float OffsetX, float OffsetY, float Scale = 1)
    {
        float[,] Map = new float[Width, Height];
        float MaxNoiseHeight = float.MinValue;
        float MinNoiseHeight = float.MaxValue;

        for (int Y = 0; Y < Height; Y++)
        {
            for (int X = 0; X < Width; X++)
            {
                float NoiseHeight = GetNoise((X + OffsetX) / Scale, (Y + OffsetY) / Scale);

                if (NoiseHeight > MaxNoiseHeight)
                    MaxNoiseHeight = NoiseHeight;
                else if (NoiseHeight < MinNoiseHeight)
                    MinNoiseHeight = NoiseHeight;

                Map[X, Y] = NoiseHeight;
            }
        }


        for (int Y = 0; Y < Height; Y++)
            for (int X = 0; X < Width; X++)
                Map[X, Y] = MathC.LerpInv(MinNoiseHeight, MaxNoiseHeight, Map[X, Y]);

        return Map;
    }
}

public interface IGradientNoiseGenerator : INoiseGenerator
{
    public uint Octaves { get; set; }
    public uint Persistence { get; set; }
    public uint Lacunarity { get; set; }

    float[,] INoiseGenerator.GetNoiseMap(int Width, int Height, float OffsetX, float OffsetY, float Scale) => GetNoiseMap(Width, Height, OffsetX, OffsetY, Scale);

    public new float[,] GetNoiseMap(int Width, int Height, float OffsetX, float OffsetY, float Scale = 1)
    {
        float[,] Map = new float[Width, Height];
        float MaxNoiseHeight = float.MinValue;
        float MinNoiseHeight = float.MaxValue;

        for (int Y = 0; Y < Height; Y++)
        {
            for (int X = 0; X < Width; X++)
            {
                float Amplitude = 1;
                float Frequency = 1;
                float NoiseHeight = 0;

                for (int I = 0; I < this.Octaves; I++)
                {
                    NoiseHeight += GetNoise((X + OffsetX) / Scale * Frequency, (Y + OffsetY) / Scale * Frequency) * Amplitude;
                    Amplitude *= this.Persistence;
                    Frequency *= this.Lacunarity;
                }

                if (NoiseHeight > MaxNoiseHeight)
                    MaxNoiseHeight = NoiseHeight;
                else if (NoiseHeight < MinNoiseHeight)
                    MinNoiseHeight = NoiseHeight;

                Map[X, Y] = NoiseHeight;
            }
        }


        for (int Y = 0; Y < Height; Y++)
            for (int X = 0; X < Width; X++)
                Map[X, Y] = MathC.LerpInv(MinNoiseHeight, MaxNoiseHeight, Map[X, Y]);

        return Map;
    }
}



public class OpenSimplex2 : IGradientNoiseGenerator
{
    private static readonly float[] GTable;

    public uint Seed { get; set; }
    public uint Octaves { get; set; } = 1;
    public uint Persistence { get; set; } = 1;
    public uint Lacunarity { get; set; } = 1;

    

    static OpenSimplex2()
    {
        XorRandom RNG = new(17);
        GTable = new float[64];

        for (int I = 0; I < GTable.Length; I++)
            GTable[I] = (2 * RNG.NextSingle()) - 1;
    }

    public float GetNoise(float X, float Y) // TODO Implement
    {
        // Coordinate Skewing
        const double F = 0.36602540378443864676372317075294d;
        double XSkew = X + (X + Y) * F;
        double YSkew = Y + (X + Y) * F;
        double X_I = XSkew - MathC.Floor(XSkew);
        double Y_I = YSkew - MathC.Floor(YSkew);

        // Simplical subdivision

        // Gradient selection
        double XGrad;
        double YGrad;

        // Kernel summation
        const double G = 0.21132486540518711774542560974902d;

        return 0;
    }
}