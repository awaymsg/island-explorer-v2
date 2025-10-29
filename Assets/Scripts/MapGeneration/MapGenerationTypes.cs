using JetBrains.Annotations;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public struct STerrainTile
{
    private float Elevation;
    private bool bSeen;
    private EBiomeType BiomeType;
    private float TraversalRate;
    private float DangerAmount;
    private bool bPlayerOccupied;

    public STerrainTile(float elevation, bool _bExplored, EBiomeType biomeType, float traversalRate, float dangerAmount, bool _bPlayerOccupied = false)
    {
        Elevation = elevation;
        bSeen = _bExplored;
        BiomeType = biomeType;
        TraversalRate = traversalRate;
        DangerAmount = dangerAmount;
        bPlayerOccupied = _bPlayerOccupied;
    }

    public float GetElevation()
    {
        return Elevation;
    }

    public bool IsSeen()
    {
        return bSeen;
    }

    public void SetIsSeen(bool bIsSeen)
    {
        bSeen = bIsSeen;
    }

    public EBiomeType GetBiomeType()
    {
        return BiomeType;
    }

    public float GetTraversalRate()
    {
        return TraversalRate;
    }

    public bool IsPlayerOccupied()
    {
        return bPlayerOccupied;
    }

    public void SetPlayerOccupied(bool _bPlayerOccupied)
    {
        bPlayerOccupied = _bPlayerOccupied;
    }

    public float GetDangerAmount()
    {
        return DangerAmount;
    }
}

[Serializable]
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

[Serializable]
public struct SElevationLevels
{
    public float MountainLevel;
    public float ForestLevel;
    public float GrasslandLevel;
    public float BeachLevel;
    public float WaterLevel;
    public float DeepWaterLevel;
}

[Serializable]
public struct SBiomeSettings
{
    public EBiomeType BiomeType;
    public float MovementTime;
    public float DangerAmount;
}

[Serializable]
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
}

[Serializable]
public class CRuleCollection
{
    public List<CTileRule> Rules = new List<CTileRule>();
}

[Serializable]
public struct SDefaultTile
{
    public EBiomeType BiomeType;
    public TileBase Tile;
}

[Serializable]
public struct STileMapping
{
    public TileBase Tile;
    public string Name;
}

[Serializable]
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

[Serializable]
public struct SPOISettings
{
    public string Name;
    public TileBase Tile;
    public float Likelihood;

    // todo: add POI event(s)
}

[Serializable]
public struct SPOIMapping
{
    public EBiomeType BiomeType;
    public SPOISettings[] POISettings;
}
