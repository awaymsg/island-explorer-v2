using UnityEngine;
using UnityEngine.Tilemaps;

[CreateAssetMenu(fileName = "TilePalette", menuName = "Scriptable Objects/TilePalette")]
public class TilePalette : ScriptableObject
{
    // todo: make this more data driven
    [SerializeField]
    private TileBase[] m_MountainTiles;
    [SerializeField]
    private TileBase[] m_ForestTiles;
    [SerializeField]
    private TileBase[] m_GrasslandTiles;
    [SerializeField]
    private TileBase[] m_BeachTiles;
    [SerializeField]
    private TileBase[] m_WaterTiles;

    private ElevationLevels m_ELevationLevels;

    public TileBase GetTile(float elevation)
    {
        if (elevation < m_ELevationLevels.m_WaterLevel)
        {
            return m_WaterTiles[0];
        }
        else if (elevation < m_ELevationLevels.m_PlainsLevel)
        {
            return m_BeachTiles[0];
        }
        else if (elevation < m_ELevationLevels.m_GrasslandLevel)
        {
            return m_GrasslandTiles[0];
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
        else if (elevation < m_ELevationLevels.m_GrasslandLevel)
        {
            return EBiomeType.Grasslands;
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
