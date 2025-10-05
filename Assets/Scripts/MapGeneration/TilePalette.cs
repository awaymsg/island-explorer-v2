using UnityEngine;
using UnityEngine.Tilemaps;

[CreateAssetMenu(fileName = "TilePalette", menuName = "Scriptable Objects/TilePalette")]
public class TilePalette : ScriptableObject
{
    public TileBase[] m_MountainTiles = new TileBase[8];
    public TileBase[] m_ForestTiles = new TileBase[8];
    public TileBase[] m_PlainsTiles = new TileBase[8];
    public TileBase[] m_WaterTiles = new TileBase[8];

    private ElevationLevels m_ELevationLevels;

    public TileBase GetTile(float elevation)
    {
        if (elevation < m_ELevationLevels.m_WaterLevel)
        {
            return m_WaterTiles[0];
        }
        else if (elevation < m_ELevationLevels.m_PlainsLevel)
        {
            return m_PlainsTiles[0];
        }
        else if (elevation < m_ELevationLevels.m_ForestLevel)
        {
            return m_ForestTiles[0];
        }

        return m_MountainTiles[0];
    }

    public EBiomeType GetBiomeType(float elevation)
    {
        if (elevation < m_ELevationLevels.m_WaterLevel)
        {
            return EBiomeType.Water;
        }
        else if (elevation < m_ELevationLevels.m_PlainsLevel)
        {
            return EBiomeType.Plains;
        }
        else if (elevation < m_ELevationLevels.m_ForestLevel)
        {
            return EBiomeType.Forest;
        }

        return EBiomeType.Mountain;
    }

    public void SetLevels(ElevationLevels elevationLevels)
    {
        m_ELevationLevels = elevationLevels;
    }
}
