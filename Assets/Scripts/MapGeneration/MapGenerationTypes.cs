using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public struct STerrainTile
{
    private Vector2Int Position;
    private float Elevation;
    private bool bExplored;
    private EBiomeType BiomeType;

    public STerrainTile(Vector2Int position, float elevation, bool _bExplored, EBiomeType biomeType)
    {
        Position = position;
        Elevation = elevation;
        bExplored = _bExplored;
        BiomeType = biomeType;
    }

    public float GetElevation()
    {
        return Elevation;
    }

    public bool IsExplored()
    {
        return bExplored;
    }

    public EBiomeType GetBiomeType()
    {
        return BiomeType;
    }
}

[System.Serializable]
public enum EBiomeType : UInt16
{
    Invalid,
    DeepWater,
    Water,
    Beach,
    Grasslands,
    Forest,
    Mountain
}

[System.Serializable]
public struct SElevationLevels
{
    public float MountainLevel;
    public float ForestLevel;
    public float GrasslandLevel;
    public float BeachLevel;
    public float WaterLevel;
    public float DeepWaterLevel;
}

[System.Serializable]
public class CTileRule
{
    public string Name;
    public string SelfBiome;
    public string[] West = new string[] { "*" }; // * means any
    public string[] NorthWest = new string[] { "*" };
    public string[] North = new string[] { "*" };
    public string[] NorthEast = new string[] { "*" };
    public string[] East = new string[] { "*" };
    public string[] SouthEast = new string[] { "*" };
    public string[] South = new string[] { "*" };
    public string[] SouthWest = new string[] { "*" };
    public int Rotations;
    public string Result;

    /*
    [Tooltip("REQUIRES 5 ELEMENTS, first element is self biome, neighbors will be west north east south")]
    public EBiomeType[] Rule;
    [Tooltip("Number of rotations to do on the tile")]
    public int Rotations;
    [Tooltip("The tile")]
    public TileBase Tile;
    */
}

[System.Serializable]
public class CRuleCollection
{
    public List<CTileRule> Rules = new List<CTileRule>();
}

[System.Serializable]
public struct SDefaultTile
{
    public EBiomeType BiomeType;
    public TileBase Tile;
}

[System.Serializable]
public struct STileMapping
{
    public TileBase Tile;
    public string Name;
}

[System.Serializable]
public struct SEnumMapping
{
    public EBiomeType BiomeType;
    public string Name;
}

public struct SRuleResult
{
    public SRuleResult(string result, int rotations)
    {
        Result = result;
        Rotations = rotations;
    }

    public string Result;
    public int Rotations;
}
