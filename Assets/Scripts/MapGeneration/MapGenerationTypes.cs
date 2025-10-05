using System;
using UnityEngine;
using UnityEngine.Tilemaps;

public struct TerrainTile
{
    private Vector2Int m_Position;
    private float m_Elevation;
    private bool m_bExplored;
    private EBiomeType m_BiomeType;
    private TileBase m_Tile;

    public TerrainTile(Vector2Int position, float elevation, bool bExplored, EBiomeType biomeType, TileBase tile)
    {
        m_Position = position;
        m_Elevation = elevation;
        m_bExplored = bExplored;
        m_BiomeType = biomeType;
        m_Tile = tile;
    }

    public float GetElevation()
    {
        return m_Elevation;
    }

    public bool IsExplored()
    {
        return m_bExplored;
    }

    public EBiomeType GetBiomeType()
    {
        return m_BiomeType;
    }

    public TileBase GetTile()
    {
        return m_Tile;
    }
}

public enum EBiomeType : UInt16
{
    Mountain,
    Forest,
    Plains,
    Water
}

[System.Serializable]
public struct ElevationLevels
{
    public float m_MountainLevel;
    public float m_ForestLevel;
    public float m_PlainsLevel;
    public float m_WaterLevel;
}
