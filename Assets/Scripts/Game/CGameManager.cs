using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
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
    [SerializeField, Tooltip("Effect tile when a location is selected")]
    private TileBase m_LocationSelectHighlightTile;
    [SerializeField, Tooltip("Effect tile when a player is selected")]
    private TileBase m_PlayerSelectHighlightTile;

    [Header("Debug")]
    [SerializeField]
    private bool m_bShowFog = true;
    [SerializeField]
    private bool m_bIgnoreTileRules = false;
    [SerializeField]
    private bool m_bPathFindDiagonals = false;

    private CMapGenerator m_MapGenerator;
    private CPartyManager m_PartyManager;

    private STerrainTile[,] m_TerrainTileMap;
    private Grid m_WorldGrid;
    private Tilemap m_FogMap;
    private Tilemap m_EffectsMap;

    private Camera m_Camera;
    private CCameraManager m_CameraManager;

    private GameObject m_PartyPlayerGameObject;
    private CPartyPlayerCharacter m_PartyPlayerCharacter;

    private bool m_bPlayerSelected = false;
    private Vector3Int m_CurrentPlayerPosition;
    private Queue<Vector3Int> m_CurrentPath;

    private CPathFinder m_PathFinder;

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

        Debug.Log(string.Format("RandomSeed: {0}", Random.seed));

        m_WorldGrid = FindFirstObjectByType<Grid>();
        m_MapGenerator = FindFirstObjectByType<CMapGenerator>();
        m_PartyManager = FindFirstObjectByType<CPartyManager>();
        m_Camera = FindFirstObjectByType<Camera>();
        m_CameraManager = m_Camera.GetComponent<CCameraManager>();

        m_MapGenerator.IgnoreTileRules = m_bIgnoreTileRules;
        m_TerrainTileMap = m_MapGenerator.GenerateMap();
        m_MapGenerator.CreateFog();

        m_FogMap = m_MapGenerator.FogMap;
        m_FogMap.GetComponent<TilemapRenderer>().enabled = m_bShowFog;

        m_EffectsMap = m_MapGenerator.EffectsMap;

        m_PathFinder = new CPathFinder(m_bPathFindDiagonals, m_TerrainTileMap);

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

        // Instantiate and initialize the player
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

        // Set camera to target player
        m_CameraManager.TargetPlayer = m_PartyPlayerGameObject;
    }

    // Input
    public void OnClick(InputAction.CallbackContext context)
    {
        if (!context.performed)
        {
            return;
        }

        Vector2 mousePosition = Mouse.current.position.ReadValue();
        Vector3 worldPosition = m_Camera.ScreenToWorldPoint(mousePosition);
        Vector3Int cellPosition = m_WorldGrid.WorldToCell(worldPosition);

        if (!IsCellValid(cellPosition))
        {
            return;
        }

        STerrainTile selectedTile = m_TerrainTileMap[cellPosition.x, cellPosition.y];
        if (selectedTile.IsPlayerOccupied())
        {
            if (m_EffectsMap.HasTile(cellPosition))
            {
                m_EffectsMap.ClearAllTiles();
                m_bPlayerSelected = false;
            }
            else
            {
                m_EffectsMap.ClearAllTiles();
                m_EffectsMap.SetTile(cellPosition, m_PlayerSelectHighlightTile);
                m_bPlayerSelected = true;
            }
        }
        else
        {
            if (m_bPlayerSelected)
            {
                return;
            }

            if (m_EffectsMap.HasTile(cellPosition))
            {
                m_EffectsMap.SetTile(cellPosition, null);
            }
            else
            {
                m_EffectsMap.SetTile(cellPosition, m_LocationSelectHighlightTile);
            }
        }
    }

    public void OnMouseOverGrid(InputAction.CallbackContext context)
    {
        if (!m_bPlayerSelected || !context.performed)
        {
            return;
        }

        m_EffectsMap.ClearAllTiles();

        Vector2 mousePosition = Mouse.current.position.ReadValue();
        Vector3 worldPosition = m_Camera.ScreenToWorldPoint(mousePosition);
        Vector3Int cellPosition = m_WorldGrid.WorldToCell(worldPosition);

        // Store the current path in case we wish to use it
        m_CurrentPath = m_PathFinder.GetPath(m_CurrentPlayerPosition, cellPosition);
        
        float totalMovement = 0f;
        Vector3Int previous = m_CurrentPlayerPosition;

        // Keep the queue intact, we're just previewing 
        foreach (Vector3Int node in m_CurrentPath.AsEnumerable())
        {
            m_EffectsMap.SetTile(node, m_PlayerSelectHighlightTile);

            // Ignore the self tile
            if (node == m_CurrentPlayerPosition)
            {
                continue;
            }

            // If this is a diagonal, multiply movement cost by 1.4
            if (Mathf.Abs(node.x - previous.x) == 1 && Mathf.Abs(node.y - previous.y) == 1)
            {
                totalMovement += m_TerrainTileMap[node.x, node.y].GetTraversalRate() * 1.4f;
            }
            else
            {
                totalMovement += m_TerrainTileMap[node.x, node.y].GetTraversalRate();
            }

            previous = node;
        }

        Debug.Log(string.Format("Total Movement: {0}", totalMovement));
    }

    public void OnRightClick(InputAction.CallbackContext context)
    {
        if (!context.performed)
        {
            return;
        }

        if (!m_bPlayerSelected)
        {
            m_EffectsMap.ClearAllTiles();
        }
        else if (m_CurrentPath.Count > 0)
        {
            while (m_CurrentPath.Count > 0)
            {
                Vector3Int node = m_CurrentPath.Dequeue();  
                MovePlayerToCell(new Vector2Int(node.x, node.y));
            }

            m_EffectsMap.ClearAllTiles();
            m_bPlayerSelected = false;
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

        // Try change occupied status of previous location
        Vector3Int prevCell = m_WorldGrid.WorldToCell(m_PartyPlayerGameObject.transform.position);
        if (IsCellValid(prevCell))
        {
            m_TerrainTileMap[prevCell.x, prevCell.y].SetPlayerOccupied(false);
        }

        Vector3 worldPosition = m_WorldGrid.GetCellCenterWorld(cellVector3);
        worldPosition.z = 0;

        m_PartyPlayerGameObject.transform.position = worldPosition;
        m_PartyPlayerCharacter.CurrentLocation = cell;

        m_TerrainTileMap[cell.x, cell.y].SetPlayerOccupied(true);
        m_CurrentPlayerPosition = cellVector3;

        // Update seen tiles (TEMP, no vision stat adjustments yet)
        for (int x = -1; x <= 1; ++x)
        {
            for (int y = -1; y <= 1; ++y)
            {
                int newX = Mathf.Clamp(cell.x + x, 0, m_TerrainTileMap.GetLength(0) - 1);
                int newY = Mathf.Clamp(cell.y + y, 0, m_TerrainTileMap.GetLength(1) - 1);
                Vector3Int newLocation = new Vector3Int(newX, newY, 0);

                m_TerrainTileMap[newX, newY].SetIsSeen(true);

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
        cell.x = Mathf.Clamp(cell.x, 0, m_TerrainTileMap.GetLength(0) - 1);
        cell.y = Mathf.Clamp(cell.y, 0, m_TerrainTileMap.GetLength(1) - 1);

        return cell;
    }

    private bool IsCellValid(Vector3Int cell)
    {
        if (cell.x >= 0 && cell.x < m_TerrainTileMap.GetLength(0))
        {
            if (cell.y >= 0 && cell.y < m_TerrainTileMap.GetLength(1))
            {
                return true;
            }
        }

        return false;
    }

    private Vector2Int FindStartingLocation()
    {
        if (m_TerrainTileMap == null || m_TerrainTileMap.Length == 0)
        {
            Debug.Log("FindStartingLocation - map is null or empty!");
            return new Vector2Int();
        }

        int direction = Random.Range(0, 2);        

        bool bNearSide = true;
        int side = Random.Range(0, 2);

        if (side == 1)
        {
            bNearSide = false;
        }

        if (direction == 0)
        {
            int y = Random.Range(0, m_MapGenerator.MapSize.y);

            if (bNearSide)
            {
                for (int i = 0; i < m_MapGenerator.MapSize.x; ++i)
                {
                    if (m_TerrainTileMap[i, y].GetBiomeType() == EBiomeType.Beach)
                    {
                        return new Vector2Int(i, y);
                    }
                }
            }
            else
            {
                for (int i = m_MapGenerator.MapSize.y - 1; i >= 0; --i)
                {
                    if (m_TerrainTileMap[i, y].GetBiomeType() == EBiomeType.Beach)
                    {
                        return new Vector2Int(i, y);
                    }
                }
            }
        }
        else
        {
            int x = Random.Range(0, m_MapGenerator.MapSize.x);

            if (bNearSide)
            {
                for (int i = 0; i < m_MapGenerator.MapSize.x; ++i)
                {
                    if (m_TerrainTileMap[x, i].GetBiomeType() == EBiomeType.Beach)
                    {
                        return new Vector2Int(x, i);
                    }
                }
            }
            else
            {
                for (int i = m_MapGenerator.MapSize.x - 1; i >= 0; --i)
                {
                    if (m_TerrainTileMap[x, i].GetBiomeType() == EBiomeType.Beach)
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
