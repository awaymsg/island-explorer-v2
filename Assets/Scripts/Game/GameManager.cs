using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    [SerializeField]
    private bool m_bUseRandomSeed = false;
    [SerializeField]
    private int m_RandomSeed = 0;

    private CMapGenerator m_MapGenerator;
    private CPartyManager m_PartyManager;

    private CPartyPlayerCharacter m_PartyPlayerCharacter;
    private STerrainTile[,] m_Map;
    private Grid m_WorldGrid;

    void Start()
    {
        if (m_bUseRandomSeed)
        {
            UnityEngine.Random.InitState(m_RandomSeed);
        }

        m_WorldGrid = FindFirstObjectByType<Grid>();
        m_MapGenerator = FindFirstObjectByType<CMapGenerator>();
        m_PartyManager = FindFirstObjectByType<CPartyManager>();

        m_Map = m_MapGenerator.GenerateMap();
    }

    private void CreatePlayerCharacter()
    {
        int leaderIndex = UnityEngine.Random.Range(0, m_PartyManager.DefaultPartyLeadersPool.Length);
        CPartyLeader partyLeader = m_PartyManager.DefaultPartyLeadersPool[leaderIndex];

        int memberindex = UnityEngine.Random.Range(0, m_PartyManager.DefaultPartyMembersPool.Length);
        CPartyMember partyMember = m_PartyManager.DefaultPartyMembersPool[memberindex];

        List<CPartyMember> partyMembers = new List<CPartyMember>();
        partyMembers.Add(partyMember);

        m_PartyPlayerCharacter = new CPartyPlayerCharacter();

        m_PartyManager.CreatePartyPlayerCharacter(m_PartyPlayerCharacter, partyLeader, partyMembers);
    }

    private Vector2Int FindStartingLocation()
    {
        if (m_Map == null || m_Map.Length == 0)
        {
            Debug.Log("FindStartingLocation - map is null or empty!");
            return new Vector2Int();
        }

        int direction = UnityEngine.Random.Range(0, 2);        

        bool bNearSide = true;
        int side = UnityEngine.Random.Range(0, 2);

        if (side == 1)
        {
            bNearSide = false;
        }

        if (direction == 0)
        {
            int y = UnityEngine.Random.Range(0, m_MapGenerator.MapSize.y);

            if (bNearSide)
            {
                for (int i = 0; i < m_MapGenerator.MapSize.x; ++i)
                {
                    if (m_Map[i, y].GetBiomeType() == EBiomeType.Beach)
                    {
                        return new Vector2Int(i, y);
                    }
                }
            }
            else
            {
                for (int i = m_MapGenerator.MapSize.y - 1; i >= 0; --i)
                {
                    if (m_Map[i, y].GetBiomeType() == EBiomeType.Beach)
                    {
                        return new Vector2Int(i, y);
                    }
                }
            }
        }
        else
        {
            int x = UnityEngine.Random.Range(0, m_MapGenerator.MapSize.x);

            if (bNearSide)
            {
                for (int i = 0; i < m_MapGenerator.MapSize.x; ++i)
                {
                    if (m_Map[x, i].GetBiomeType() == EBiomeType.Beach)
                    {
                        return new Vector2Int(x, i);
                    }
                }
            }
            else
            {
                for (int i = m_MapGenerator.MapSize.x - 1; i >= 0; --i)
                {
                    if (m_Map[x, i].GetBiomeType() == EBiomeType.Beach)
                    {
                        return new Vector2Int(x, i);
                    }
                }
            }
        }

        Debug.Log("FindStartingLocation - could not find a starting location!");
        return new Vector2Int();
    }
}
