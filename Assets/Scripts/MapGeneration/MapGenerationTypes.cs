using System;
using System.Collections.Generic;
using UnityEngine.Tilemaps;

public class CTerrainTile
{
    private float m_Elevation;
    private bool m_bSeen;
    private EBiomeType m_BiomeType;
    private float m_TraversalRate;
    private float m_ForageAmount;
    private List<CLocalEvent> m_LocalEvents;
    private bool m_bPlayerOccupied;

    public CTerrainTile(float elevation, bool _bExplored, EBiomeType biomeType, float traversalRate, float forageAmount, List<CLocalEvent> localEvents, bool bPlayerOccupied = false)
    {
        m_Elevation = elevation;
        m_bSeen = _bExplored;
        m_BiomeType = biomeType;
        m_TraversalRate = traversalRate;
        m_ForageAmount = forageAmount;
        m_LocalEvents = localEvents;
        m_bPlayerOccupied = bPlayerOccupied;
    }

    public float Elevation
    {
        get { return m_Elevation; }
    }

    public bool IsSeen
    {
        get { return m_bSeen; }
        set { m_bSeen = value; }
    }

    public EBiomeType BiomeType
    {
        get { return m_BiomeType; }
    }

    public float TraversalRate
    {
        get { return m_TraversalRate; }
    }

    public bool IsPlayerOccupied
    {
        get { return m_bPlayerOccupied; }
        set { m_bPlayerOccupied = value; }
    }

    public float ForageAmount
    {
        get { return m_ForageAmount; }
    }

    public CLocalEvent GetLocalEvent()
    {
        if (m_LocalEvents.Count == 0)
        {
            return null;
        }

        int index = UnityEngine.Random.Range(0, m_LocalEvents.Count);
        CLocalEvent localEvent = m_LocalEvents[index];
        m_LocalEvents.RemoveAt(index);

        return localEvent;
    }
}

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
    public float ForageAmount;
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
