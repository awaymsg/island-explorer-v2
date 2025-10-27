using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Tilemaps;

public class CGameManager : MonoBehaviour
{
    [SerializeField]
    private bool m_bUseSeed = false;
    [SerializeField]
    private int m_RandomSeed = 0;

    [SerializeField, Tooltip("Player party character prefab")]
    private GameObject m_PartyPlayerCharacterPrefab;

    private CCameraManager m_CameraManager;

    [Header("Debug")]
    [SerializeField]
    private bool m_bShowFog = true;
    [SerializeField]
    private bool m_bIgnoreTileRules = false;

    private CMapGenerator m_MapGenerator;
    private CPartyManager m_PartyManager;

    private STerrainTile[,] m_Map;
    private Grid m_WorldGrid;
    private Tilemap m_FogMap;

    private GameObject m_PartyPlayerGameObject;
    private CPartyPlayerCharacter m_PartyPlayerCharacter;

    private bool m_MoveConsumed = false;

    public CPartyPlayerCharacter PartyPlayerCharacter
    {
        get { return m_PartyPlayerCharacter; }
    }

    void Start()
    {
        if (m_bUseSeed)
        {
            Random.InitState(m_RandomSeed);
        }

        m_WorldGrid = FindFirstObjectByType<Grid>();
        m_MapGenerator = FindFirstObjectByType<CMapGenerator>();
        m_PartyManager = FindFirstObjectByType<CPartyManager>();
        Camera camera = FindFirstObjectByType<Camera>();
        m_CameraManager = camera.GetComponent<CCameraManager>();

        m_MapGenerator.IgnoreTileRules = m_bIgnoreTileRules;
        m_Map = m_MapGenerator.GenerateMap();
        m_MapGenerator.CreateFog();

        m_FogMap = m_MapGenerator.FogMap;
        m_FogMap.GetComponent<TilemapRenderer>().enabled = m_bShowFog;

        CreatePlayerCharacter();
    }

    private void CreatePlayerCharacter()
    {
        if (m_PartyManager.DefaultPartyLeadersPool.Length == 0)
        {
            Debug.Log("CreatePlayerCharacter - There are no DefaultPartyLeaders in the pool!");
            return;
        }

        if (m_PartyManager.DefaultPartyMembersPool.Length == 0)
        {
            Debug.Log("CreatePlayerCharacter - There are no DefaultPartyMembers in the pool!");
            return;
        }

        int leaderIndex = Random.Range(0, m_PartyManager.DefaultPartyLeadersPool.Length);
        CPartyLeader defaultPartyLeader = m_PartyManager.DefaultPartyLeadersPool[leaderIndex];

        int memberindex = Random.Range(0, m_PartyManager.DefaultPartyMembersPool.Length);
        CPartyMember defaultPartyMember = m_PartyManager.DefaultPartyMembersPool[memberindex];

        // instantiate and initialize the player
        m_PartyPlayerGameObject = Instantiate(m_PartyPlayerCharacterPrefab);
        m_PartyPlayerCharacter = m_PartyPlayerGameObject.GetComponent<CPartyPlayerCharacter>();

        // TEMP: initialize default party members. later they will be created and selected from a menu
        CPartyLeaderRuntime partyLeader = m_PartyManager.CreatePartyLeader(defaultPartyLeader);
        CPartyMemberRuntime partyMember = m_PartyManager.CreatePartyMember(defaultPartyMember);

        List<CPartyMemberRuntime> partyMembers = new List<CPartyMemberRuntime>();
        partyMembers.Add(partyMember);

        m_PartyPlayerCharacter = m_PartyManager.CreatePartyPlayerCharacter(m_PartyPlayerCharacter, partyLeader, partyMembers);
        m_PartyPlayerGameObject.GetComponent<SpriteRenderer>().sprite = defaultPartyLeader.m_OverworldSprite;

        Vector2Int startingLocation = FindStartingLocation();
        m_PartyPlayerCharacter.CurrentLocation = startingLocation;
        MovePlayerToCell(startingLocation);
        MoveCameraToCell(startingLocation);
    }

    // input
    public void OnMove(InputAction.CallbackContext context)
    {
        if (!CCameraManager.IsCameraMapMovementEnabled)
        {
            Vector2Int movement;

            Vector2 moveInput = context.ReadValue<Vector2>();

            if (!m_MoveConsumed && moveInput != Vector2.zero)
            {
                if (moveInput.y != 0)
                {
                    movement = new Vector2Int((int)Mathf.Sign(moveInput.y), 0);
                }
                else
                {
                    movement = new Vector2Int(0, -(int)Mathf.Sign(moveInput.x));
                }

                MovePlayerToCell(m_PartyPlayerCharacter.CurrentLocation + movement);

                m_MoveConsumed = true;
            }

            if (context.canceled)
            {
                m_MoveConsumed = false;
            }
        }
    }

    private void PlaceObjectOnCell(GameObject objectToPlace, Vector2Int cell)
    {
        cell = ClampCell(cell);
        Vector3Int cellVector3 = new Vector3Int(cell.x, cell.y);

        Vector3 worldPosition = m_WorldGrid.GetCellCenterWorld(cellVector3);
        worldPosition.z = 0;

        objectToPlace.transform.position = worldPosition;
    }

    private void MovePlayerToCell(Vector2Int cell)
    {
        cell = ClampCell(cell);
        Vector3Int cellVector3 = new Vector3Int(cell.x, cell.y);

        Vector3 worldPosition = m_WorldGrid.GetCellCenterWorld(cellVector3);
        worldPosition.z = 0;

        m_PartyPlayerGameObject.transform.position = worldPosition;
        m_PartyPlayerCharacter.CurrentLocation = cell;

        STerrainTile terrainTile = m_Map[cell.x, cell.y];

        // update seen tiles (TEMP, no vision stat adjustments yet)
        for (int x = -1; x <= 1; ++x)
        {
            for (int y = -1; y <= 1; ++y)
            {
                int newX = Mathf.Clamp(cell.x + x, 0, m_Map.GetLength(0) - 1);
                int newY = Mathf.Clamp(cell.y + y, 0, m_Map.GetLength(1) - 1);
                Vector3Int newLocation = new Vector3Int(newX, newY, 0);

                m_Map[newX, newY].SetIsSeen(true);

                m_FogMap.SetTile(newLocation, null);
            }
        }
    }

    private void MoveCameraToCell(Vector2Int cell)
    {
        cell = ClampCell(cell);
        Vector3Int cellVector3 = new Vector3Int(cell.x, cell.y);

        Vector3 worldPosition = m_WorldGrid.GetCellCenterWorld(cellVector3);
        m_CameraManager.MoveCameraToPosition(worldPosition);
    }

    private Vector2Int ClampCell(Vector2Int cell)
    {
        cell.x = Mathf.Clamp(cell.x, 0, m_Map.GetLength(0) - 1);
        cell.y = Mathf.Clamp(cell.y, 0, m_Map.GetLength(1) - 1);

        return cell;
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

        Debug.Log("FindStartingLocation - could not find a starting location! Trying again!");
        return FindStartingLocation();
    }

    private void OnValidate()
    {
        if (m_FogMap != null)
        {
            if (m_bShowFog)
            {
                m_FogMap.GetComponent<TilemapRenderer>().enabled = true;
            }
            else
            {
                m_FogMap.GetComponent<TilemapRenderer>().enabled = false;
            }
        }
    }
}
