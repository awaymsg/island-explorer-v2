using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using Unity.Hierarchy;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Tilemaps;

public class CGameManager : MonoBehaviour
{
    [Header("Generation")]
    [SerializeField]
    private bool m_bUseSeed = false;
    [SerializeField]
    private int m_RandomSeed = 0;

    [Header("Character & Refs")]
    [SerializeField, Tooltip("Player party character prefab")]
    private GameObject m_PartyPlayerCharacterPrefab;
    [SerializeField, Tooltip("Effect tile when a location is selected")]
    private TileBase m_LocationSelectHighlightTile;
    [SerializeField, Tooltip("Effect tile when a player is selected")]
    private TileBase m_PlayerSelectHighlightTile;

    [Header("Passage of time")]
    [SerializeField, Tooltip("Tick speed in seconds")]
    private float m_TickSpeed = 0.1f;
    [SerializeField, Tooltip("Division of days into steps")]
    private int m_StepsInADay = 10;

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
    private CUIManager m_UIManager;

    private GameObject m_PartyPlayerGameObject;
    private CPartyPlayerCharacter m_PartyPlayerCharacter;

    private CPathFinder m_PathFinder;

    // Gameplay important vars
    private float m_DayCount = 0;
    private bool m_bPlayerSelected = false;
    private Vector3Int m_CurrentPlayerCell;
    private Queue<Vector3Int> m_CurrentPath;
    private Queue<float> m_CurrentTruePathMovementRates;
    private Queue<float> m_CurrentEstimatedPathMovementRates;
    private float m_TotalEstimatedMovementRemaining = 0f;
    private bool m_bStartMove = false;
    private Vector3Int m_TargetCell = Vector3Int.zero;
    private float m_TimeCounter = 0f;
    private float m_CurrentTrueMovementRate = 0f;
    private Vector3 m_MovementPerTick = Vector3.zero;
    private Vector3 m_TargetPositionNextTick = Vector3.zero;
    private float m_OccurredMovement = 0f;

    public CPartyPlayerCharacter PartyPlayerCharacter
    {
        get { return m_PartyPlayerCharacter; }
    }

    private void Start()
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
        m_UIManager = FindFirstObjectByType<CUIManager>();

        m_MapGenerator.IgnoreTileRules = m_bIgnoreTileRules;
        m_TerrainTileMap = m_MapGenerator.GenerateMap();
        m_MapGenerator.CreateFog();

        m_FogMap = m_MapGenerator.FogMap;
        m_FogMap.GetComponent<TilemapRenderer>().enabled = m_bShowFog;
        m_EffectsMap = m_MapGenerator.EffectsMap;

        m_PathFinder = new CPathFinder(m_bPathFindDiagonals, m_TerrainTileMap, m_bShowFog);

        m_UIManager.InitializeWorldInfoPanel();

        CreatePlayerCharacter();
    }

    private void Update()
    {
        if (m_bStartMove == false && m_PartyPlayerGameObject != null)
        {
            return;
        }

        // Only tick at set speed
        if (m_TimeCounter < m_TickSpeed)
        {
            m_TimeCounter += Time.deltaTime;

            if (m_TargetPositionNextTick != Vector3.zero)
            {
                float t = Mathf.Clamp01(m_TimeCounter / m_TickSpeed);
                t = Mathf.SmoothStep(0f, 1f, t);

                m_PartyPlayerGameObject.transform.position = Vector3.Lerp(m_PartyPlayerGameObject.transform.position, m_TargetPositionNextTick, t);
            }

            return;
        }

        // Reset timer
        m_TimeCounter = 0f;

        MoveCharacter();
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

    private void MoveCharacter()
    {
        // If we do not have a target cell, try to obtain one. If there are no cells left, we're done moving!
        if (m_TargetCell == Vector3Int.zero)
        {
            if (m_CurrentPath.Count > 0)
            {
                m_TargetCell = m_CurrentPath.Dequeue();

                // Ignore if tile is starting tile
                if (m_TargetCell == m_CurrentPlayerCell)
                {
                    m_TargetCell = Vector3Int.zero;
                    return;
                }

                // m_CurrentPathMovementRates does not contain starting tile's movement rate 
                m_CurrentTrueMovementRate = m_CurrentTruePathMovementRates.Dequeue();

                // If the estimated tile movement rate is lower than the actual, update estimate
                float estimatedNextMoveTime = m_CurrentEstimatedPathMovementRates.Dequeue();

                if (m_CurrentTrueMovementRate > estimatedNextMoveTime)
                {
                    // TODO: show message to ask to continue moving or not
                }

                // Update estimated move time remaining, account for 1 extra step
                m_TotalEstimatedMovementRemaining = m_CurrentTrueMovementRate;
                foreach (float estimatedMoveTime in m_CurrentEstimatedPathMovementRates.AsEnumerable())
                {
                    m_TotalEstimatedMovementRemaining += estimatedMoveTime;
                }

                m_UIManager.UpdateWorldInfo(string.Format("Estimated Traversal Time: {0}", m_TotalEstimatedMovementRemaining));
            }
            else
            {
                // We are out of paths, so we're done moving! Reset to able to move state!
                m_EffectsMap.ClearAllTiles();
                m_bPlayerSelected = false;
                m_bStartMove = false;

                return;
            }
        }

        STerrainTile currentTile = m_TerrainTileMap[m_CurrentPlayerCell.x, m_CurrentPlayerCell.y];

        // Find increment to move
        if (m_MovementPerTick == Vector3.zero)
        {
            Vector3 currentPosition = m_WorldGrid.GetCellCenterWorld(m_CurrentPlayerCell);
            Vector3 targetPosition = m_WorldGrid.GetCellCenterWorld(m_TargetCell);
            m_MovementPerTick = (targetPosition - currentPosition) / (m_CurrentTrueMovementRate * m_StepsInADay);

            m_OccurredMovement = 0f;
        }

        // Move player in increments
        if (m_OccurredMovement < m_CurrentTrueMovementRate)
        {
            m_TargetPositionNextTick = m_PartyPlayerGameObject.transform.position + m_MovementPerTick;
            m_OccurredMovement += 1f / m_StepsInADay;
            m_OccurredMovement = (float)System.Math.Round(m_OccurredMovement, 1);

            // We moved, so increment day
            m_DayCount += 1f / m_StepsInADay;
            m_DayCount = (float)System.Math.Round(m_DayCount, 1);
            m_UIManager.UpdateDayInfo(string.Format("Day: {0}", m_DayCount));

            // Decrement estimated move time remaining
            m_TotalEstimatedMovementRemaining -= 1f / m_StepsInADay;
            m_TotalEstimatedMovementRemaining = (float)System.Math.Round(m_TotalEstimatedMovementRemaining, 1);
            m_UIManager.UpdateWorldInfo(string.Format("Estimated Traversal Time: {0}", m_TotalEstimatedMovementRemaining));
        }
        else
        {
            // Player is officially in cell now
            MovePlayerToCell(new Vector2Int(m_TargetCell.x, m_TargetCell.y));
            m_MovementPerTick = Vector3.zero;
            m_TargetCell = Vector3Int.zero;
            m_CurrentTrueMovementRate = 0f;
            m_TargetPositionNextTick = Vector3.zero;
        }
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

        // Deselect if we have player selected
        if (m_bPlayerSelected)
        {
            m_EffectsMap.ClearAllTiles();
            m_bPlayerSelected = false;

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
        if (!context.performed || m_bStartMove)
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

        if (m_bPlayerSelected)
        {
            m_EffectsMap.ClearAllTiles();

            // Store the current path in case we wish to use it
            m_CurrentPath = m_PathFinder.GetPath(m_CurrentPlayerCell, cellPosition);
            m_CurrentTruePathMovementRates = new Queue<float>();
            m_CurrentEstimatedPathMovementRates = new Queue<float>();

            m_TotalEstimatedMovementRemaining = 0f;
            float totalDanger = 0f;

            Vector3Int previous = m_CurrentPlayerCell;

            // Keep the queue intact, we're just previewing 
            foreach (Vector3Int node in m_CurrentPath.AsEnumerable())
            {
                m_EffectsMap.SetTile(node, m_PlayerSelectHighlightTile);

                // Ignore the self tile
                if (node == m_CurrentPlayerCell)
                {
                    continue;
                }

                bool bIsFogged = m_FogMap.HasTile(node) && m_bShowFog;

                float trueTraversalRate = m_TerrainTileMap[node.x, node.y].GetTraversalRate();

                float displayTraversalRate = bIsFogged ? 1f : m_TerrainTileMap[node.x, node.y].GetTraversalRate();
                float displayDangerAmount = bIsFogged ? 0f : m_TerrainTileMap[node.x, node.y].GetDangerAmount();

                // If this is a diagonal, multiply movement cost by 1.4
                if (Mathf.Abs(node.x - previous.x) == 1 && Mathf.Abs(node.y - previous.y) == 1)
                {
                    m_CurrentTruePathMovementRates.Enqueue((float)System.Math.Round(trueTraversalRate * 1.4f, 1));
                    m_CurrentEstimatedPathMovementRates.Enqueue((float)System.Math.Round(displayTraversalRate * 1.4f, 1));
                    m_TotalEstimatedMovementRemaining += displayTraversalRate * 1.4f;
                    totalDanger += displayDangerAmount * 1.4f;
                }
                else
                {
                    m_CurrentTruePathMovementRates.Enqueue((float)System.Math.Round(trueTraversalRate, 1));
                    m_CurrentEstimatedPathMovementRates.Enqueue((float)System.Math.Round(displayTraversalRate, 1));
                    m_TotalEstimatedMovementRemaining += displayTraversalRate;
                    totalDanger += displayDangerAmount;
                }

                previous = node;
            }

            m_TotalEstimatedMovementRemaining = (float)System.Math.Round(m_TotalEstimatedMovementRemaining, 1);
            totalDanger = (float)System.Math.Round(totalDanger, 1);
            m_UIManager.UpdateWorldInfo(string.Format("Estimated Traversal Time: {0}d\nEstimated Danger Level: {1}", m_TotalEstimatedMovementRemaining, totalDanger));
        }
        else
        {
            bool bIsFogged = m_FogMap.HasTile(cellPosition) && m_bShowFog;

            if (bIsFogged)
            {
                m_UIManager.UpdateWorldInfo("Biome: ???\nTraversal Time: ???\nDanger Level: ???");

                return;
            }

            STerrainTile currentTile = m_TerrainTileMap[cellPosition.x, cellPosition.y];

            m_UIManager.UpdateWorldInfo(string.Format("Biome: {0}\nTraversal Time: {1}d\nDanger Level: {2}", currentTile.GetBiomeType().ToString(), currentTile.GetTraversalRate(), currentTile.GetDangerAmount()));
        }
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
        else if (m_CurrentPath.Count > 0 && !m_bStartMove)
        {
            m_bStartMove = true;
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
        m_CurrentPlayerCell = cellVector3;

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

                if (m_PathFinder != null)
                {
                    m_PathFinder.SetHasFog(true);
                }
            }
            else
            {
                m_FogMap.GetComponent<TilemapRenderer>().enabled = false;

                if (m_PathFinder != null)
                {
                    m_PathFinder.SetHasFog(false);
                }
            }
        }
    }
}
