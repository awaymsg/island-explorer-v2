using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Tilemaps;

public enum EGameState
{
    Idle,
    Paused,
    PreMoving,
    Moving
}

// TODO: this is handling too much logic- should split into separate systems, at least movement
public class CGameManager : MonoBehaviour
{
    private static CGameManager m_Instance;

    [Header("Generation")]
    [SerializeField]
    private bool m_bUseSeed = false;
    [SerializeField]
    private int m_RandomSeed = 0;

    [Header("Character & Refs")]
    [SerializeField]
    private Camera m_Camera;
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
    [SerializeField, Tooltip("Base hunger rate (per tick)")]
    private float m_HungerRatePerTick = 0.5f;
    [SerializeField, Tooltip("Diagonal movement multipler")]
    private float m_DiagonalMovementScalar = 1.4f;

    [Header("Important values")]
    [SerializeField, Tooltip("Maximum stat value")]
    private int m_MaxStatValue = 100;
    [SerializeField, Tooltip("Days on island value to calculate character sanity debuff against (days / threshold)")]
    private float m_DaysOnIslandMaxValue = 1000f;
    [SerializeField, Tooltip("Stat randomization range")]
    private Vector2Int m_StatRandomizationRange;

    [Header("Debug")]
    [SerializeField]
    private bool m_bShowFog = true;
    [SerializeField]
    private bool m_bDebugTileHighlight = false;
    [SerializeField]
    private bool m_bIgnoreTileRules = false;
    [SerializeField]
    private bool m_bPathFindDiagonals = false;

    private CMapGenerator m_MapGenerator;
    private CPartyManager m_PartyManager;

    private CTerrainTile[,] m_TerrainTileMap;
    private Grid m_WorldGrid;
    private Tilemap m_FogMap;
    private Tilemap m_EffectsMap;

    private CCameraManager m_CameraManager;
    private CUIManager m_UIManager;

    private GameObject m_PartyPlayerGameObject;
    private CPartyPlayerCharacter m_PartyPlayerCharacter;

    private static CPathFinder m_PathFinder;

    // Gameplay important vars
    private EGameState m_GameState = EGameState.Idle; // TODO: add proper state machine with defined transitions
    private float m_DayCount = 0;
    private Queue<Vector3Int> m_CurrentPath;
    private Queue<float> m_CurrentTruePathMovementRates;
    private Queue<float> m_CurrentEstimatedPathMovementRates;
    private float m_CurrentAcceptablePathMovementRate = 0f;
    private float m_TotalEstimatedMovementRemaining = 0f;
    private Vector3Int m_TargetCell = Vector3Int.zero;
    private float m_TimeCounter = 0f;
    private float m_CurrentTrueMovementRate = 0f;
    private Vector3 m_MovementPerTick = Vector3.zero;
    private Vector3 m_TargetPositionNextTick = Vector3.zero;
    private float m_OccurredMovement = 0f;

    //-- Events
    public event Action<string> m_OnDayInfoChanged;
    public event Action<string> m_OnWorldInfoChanged;
    public event Action m_OnTick;
    //--

    //-- Getters
    public CPartyPlayerCharacter PartyPlayerCharacter
    {
        get { return m_PartyPlayerCharacter; }
    }

    public float StepsInADay
    {
        get { return m_StepsInADay; }
    }

    public int MaxStatValue
    {
        get { return m_MaxStatValue; }
    }

    public float DaysOnIslandMaxValue
    {
        get { return m_DaysOnIslandMaxValue; }
    }

    public float HungerRatePerTick
    {
        get { return m_HungerRatePerTick; }
    }

    public Vector2Int StatRandomizationRange
    {
        get { return m_StatRandomizationRange; }
    }

    //--

    public static CGameManager Instance
    {
        get
        {
            if (m_Instance == null)
            {
                m_Instance = FindFirstObjectByType<CGameManager>();
            }

            return m_Instance;
        }
    }
    //--

    private void Start()
    {
        if (m_bUseSeed)
        {
            UnityEngine.Random.InitState(m_RandomSeed);
        }

        Debug.Log(string.Format("RandomSeed: {0}", UnityEngine.Random.seed));

        m_WorldGrid = FindFirstObjectByType<Grid>();
        m_MapGenerator = CMapGenerator.Instance;
        m_PartyManager = CPartyManager.Instance;
        m_CameraManager = m_Camera.GetComponent<CCameraManager>();
        m_UIManager = CUIManager.Instance;

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

    private void OnDestroy()
    {
        m_OnDayInfoChanged = null;
        m_OnWorldInfoChanged = null;
    }

    private bool IsIdleState()
    {
        return m_GameState == EGameState.Idle;
    }

    private bool IsPausedState()
    {
        return m_GameState == EGameState.Paused;
    }

    private bool IsMovingState()
    {
        return m_GameState == EGameState.Moving;
    }

    private bool IsPreMovingState()
    {
        return m_GameState == EGameState.PreMoving;
    }

    private bool CanMove()
    {
        return IsMovingState() && !IsPausedState();
    }

    public bool TryEnterGameState(EGameState gameState)
    {
        m_GameState = gameState;
        return true;
    }

    private void Update()
    {
        if (!CanMove() || m_PartyPlayerGameObject == null)
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

        MoveCharacterAsync();
    }

    private void CreatePlayerCharacter()
    {
        if (m_PartyManager.DefaultPartyMembersPool.Length == 0)
        {
            Debug.Log("CreatePlayerCharacter - There are no DefaultPartyMembers in the pool!");
            return;
        }

        // Instantiate and initialize the player
        m_PartyPlayerGameObject = Instantiate(m_PartyPlayerCharacterPrefab);
        m_PartyPlayerCharacter = m_PartyPlayerGameObject.GetComponent<CPartyPlayerCharacter>();

        // TEMP: initialize default party members. later they will be created and selected from a menu
        Queue<CPartyMemberRuntime> partyMembers = new Queue<CPartyMemberRuntime>();
        for (int i = 0; i < 2; ++i)
        {
            int memberindex = UnityEngine.Random.Range(0, m_PartyManager.DefaultPartyMembersPool.Length);
            CPartyMember defaultPartyMember = m_PartyManager.DefaultPartyMembersPool[memberindex];

            CPartyMemberRuntime partyMember = m_PartyManager.CreatePartyMember(defaultPartyMember);

            partyMembers.Enqueue(partyMember);
        }

        m_PartyPlayerCharacter = m_PartyManager.CreatePartyPlayerCharacter(m_PartyPlayerCharacter, partyMembers);
        m_PartyPlayerGameObject.GetComponent<SpriteRenderer>().sprite = m_PartyPlayerCharacter.PartyLeader.OverworldSprite;

        Vector3Int startingLocation = FindStartingLocation();
        m_PartyPlayerCharacter.CurrentLocation = startingLocation;
        MovePlayerToCell(startingLocation);
        MoveCameraToCell(startingLocation);

        // Set camera to target player
        m_CameraManager.TargetPlayer = m_PartyPlayerGameObject;
    }

    private async Task MoveCharacterAsync()
    {
        // If we do not have a target cell, try to obtain one. If there are no cells left, we're done moving!
        if (m_TargetCell == Vector3Int.zero)
        {
            if (m_CurrentPath.Count > 0)
            {
                m_TargetCell = m_CurrentPath.Dequeue();

                // Ignore if tile is starting tile
                if (m_TargetCell == m_PartyPlayerCharacter.CurrentLocation)
                {
                    m_TargetCell = Vector3Int.zero;
                    return;
                }

                // m_CurrentPathMovementRates does not contain starting tile's movement rate 
                m_CurrentTrueMovementRate = m_CurrentTruePathMovementRates.Dequeue();

                // If the estimated tile movement rate is lower than the actual, update estimate
                float estimatedNextMoveTime = m_CurrentEstimatedPathMovementRates.Dequeue();

                // Our next move is above our estimated threshold!
                if (m_CurrentTrueMovementRate > estimatedNextMoveTime && m_CurrentTrueMovementRate > m_CurrentAcceptablePathMovementRate)
                {
                    // Pause and show message to ask to continue moving or not
                    bool continueMoving = await GetMovementConfirmationAsync(m_CurrentTrueMovementRate);

                    if (!continueMoving)
                    {
                        CancelMove();
                        return;
                    }

                    m_CurrentAcceptablePathMovementRate = m_CurrentTrueMovementRate;
                }

                // Update estimated move time remaining, account for 1 extra step
                m_TotalEstimatedMovementRemaining = m_CurrentTrueMovementRate;
                foreach (float estimatedMoveTime in m_CurrentEstimatedPathMovementRates.AsEnumerable())
                {
                    m_TotalEstimatedMovementRemaining += estimatedMoveTime;
                }

                m_OnWorldInfoChanged?.Invoke(string.Format("Estimated Traversal Time: {0}", m_TotalEstimatedMovementRemaining));
            }
            else
            {
                // We are out of paths, so we're done moving! Reset to able to move state!
                m_EffectsMap.ClearAllTiles();
                m_GameState = EGameState.Idle;
                m_CurrentAcceptablePathMovementRate = 0f;

                return;
            }
        }

        CTerrainTile currentTile = m_TerrainTileMap[m_PartyPlayerCharacter.CurrentLocation.x, m_PartyPlayerCharacter.CurrentLocation.y];

        // Find increment to move
        if (m_MovementPerTick == Vector3.zero)
        {
            Vector3 currentPosition = m_WorldGrid.GetCellCenterWorld(m_PartyPlayerCharacter.CurrentLocation);
            Vector3 targetPosition = m_WorldGrid.GetCellCenterWorld(m_TargetCell);
            m_MovementPerTick = (targetPosition - currentPosition) / (m_CurrentTrueMovementRate * m_StepsInADay);

            m_OccurredMovement = 0f;
        }

        // Move player in increments
        if (m_OccurredMovement < m_CurrentTrueMovementRate)
        {
            float dayIncrement = 1f / m_StepsInADay;

            m_TargetPositionNextTick = m_PartyPlayerGameObject.transform.position + m_MovementPerTick;
            m_OccurredMovement += dayIncrement;
            m_OccurredMovement = (float)Math.Round(m_OccurredMovement, 1);

            // We moved, so increment day
            m_DayCount += dayIncrement;
            m_DayCount = (float)Math.Round(m_DayCount, 1);
            m_OnDayInfoChanged?.Invoke(string.Format("Day: {0}", m_DayCount));

            // Decrement estimated move time remaining
            m_TotalEstimatedMovementRemaining -= dayIncrement;
            m_TotalEstimatedMovementRemaining = (float)Math.Round(m_TotalEstimatedMovementRemaining, 1);
            m_OnWorldInfoChanged?.Invoke(string.Format("Estimated Traversal Time: {0}", m_TotalEstimatedMovementRemaining));
            m_OnTick?.Invoke();
        }
        else
        {
            // Player is officially in cell now
            MovePlayerToCell(m_TargetCell);
            m_MovementPerTick = Vector3.zero;
            m_TargetCell = Vector3Int.zero;
            m_CurrentTrueMovementRate = 0f;
            m_TargetPositionNextTick = Vector3.zero;
        }
    }

    private async Task<bool> GetMovementConfirmationAsync(float actual)
    {
        m_GameState = EGameState.Paused;

        TaskCompletionSource<bool> completionSource = new TaskCompletionSource<bool>();

        // UI callbacks should always execute on main thread in unity allegedly
        m_UIManager.PopupsUI.CreateConfirmationDialogue(actual, (response) =>
        {
            completionSource.SetResult(response);
        });

        bool result = await completionSource.Task;

        m_GameState = EGameState.Moving;
        return result;
    }

    private void CancelMove()
    {
        m_EffectsMap.ClearAllTiles();
        m_TargetCell = Vector3Int.zero;
        m_MovementPerTick = Vector3.zero;
        m_TargetPositionNextTick = Vector3.zero;
        m_CurrentTrueMovementRate = 0f;
        m_CurrentAcceptablePathMovementRate = 0f;
        m_GameState = EGameState.Idle;
    }

    // Input
    public void OnClick(InputAction.CallbackContext context)
    {
        if (!context.performed || IsPausedState())
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

        // Deselect if we are in PreMoving state
        if (IsPreMovingState())
        {
            m_EffectsMap.ClearAllTiles();
            m_GameState = EGameState.Idle;
            UpdateTileInfo(cellPosition);

            return;
        }

        CTerrainTile selectedTile = m_TerrainTileMap[cellPosition.x, cellPosition.y];
        if (selectedTile.IsPlayerOccupied)
        {
            m_EffectsMap.ClearAllTiles();
            m_EffectsMap.SetTile(cellPosition, m_PlayerSelectHighlightTile);
            m_GameState = EGameState.PreMoving;
        }
        else
        {
            if (IsPreMovingState() || !m_bDebugTileHighlight)
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

        if (m_bDebugTileHighlight)
        {
            Debug.Log(cellPosition);
        }
    }

    public void OnMouseOverGrid(InputAction.CallbackContext context)
    {
        if (!context.performed || IsMovingState() || IsPausedState())
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

        if (IsPreMovingState())
        {
            CreateVisualPath(cellPosition);
        }
        else
        {
            UpdateTileInfo(cellPosition);
        }
    }

    private void CreateVisualPath(Vector3Int cellPosition)
    {
        m_EffectsMap.ClearAllTiles();

        // Store the current path in case we wish to use it
        m_CurrentPath = m_PathFinder.GetPath(m_PartyPlayerCharacter.CurrentLocation, cellPosition);
        m_CurrentTruePathMovementRates = new Queue<float>();
        m_CurrentEstimatedPathMovementRates = new Queue<float>();

        m_TotalEstimatedMovementRemaining = 0f;
        float totalForage = 0f;

        Vector3Int previous = m_PartyPlayerCharacter.CurrentLocation;

        // Keep the queue intact, we're just previewing 
        foreach (Vector3Int node in m_CurrentPath.AsEnumerable())
        {
            m_EffectsMap.SetTile(node, m_PlayerSelectHighlightTile);

            // Ignore the self tile
            if (node == m_PartyPlayerCharacter.CurrentLocation)
            {
                continue;
            }

            // Set default fog traversal rate as value previous tile
            float fogTraversalRate = m_TerrainTileMap[previous.x, previous.y].TraversalRate;

            bool bIsFogged = m_FogMap.HasTile(node) && m_bShowFog;

            float trueTraversalRate = m_TerrainTileMap[node.x, node.y].TraversalRate;

            float displayTraversalRate = bIsFogged ? fogTraversalRate : m_TerrainTileMap[node.x, node.y].TraversalRate;
            float displayForageAmount = bIsFogged ? 0f : m_TerrainTileMap[node.x, node.y].ForageAmount;

            // If this is a diagonal, multiply movement cost by multiplier
            if (Mathf.Abs(node.x - previous.x) == 1 && Mathf.Abs(node.y - previous.y) == 1)
            {
                m_CurrentTruePathMovementRates.Enqueue((float)Math.Round(trueTraversalRate * m_DiagonalMovementScalar, 1));
                m_CurrentEstimatedPathMovementRates.Enqueue((float)Math.Round(displayTraversalRate * m_DiagonalMovementScalar, 1));
                m_TotalEstimatedMovementRemaining += displayTraversalRate * m_DiagonalMovementScalar;
                totalForage += displayForageAmount * m_DiagonalMovementScalar;
            }
            else
            {
                m_CurrentTruePathMovementRates.Enqueue((float)Math.Round(trueTraversalRate, 1));
                m_CurrentEstimatedPathMovementRates.Enqueue((float)Math.Round(displayTraversalRate, 1));
                m_TotalEstimatedMovementRemaining += displayTraversalRate;
                totalForage += displayForageAmount;
            }

            previous = node;
        }

        m_TotalEstimatedMovementRemaining = (float)Math.Round(m_TotalEstimatedMovementRemaining, 1);
        totalForage = (float)Math.Round(totalForage, 1);
        m_OnWorldInfoChanged?.Invoke(string.Format("Estimated Traversal Time: {0}d\nEstimated Forage Amount: {1}", m_TotalEstimatedMovementRemaining, totalForage));
    }

    private void UpdateTileInfo(Vector3Int cellPosition)
    {
        bool bIsFogged = m_FogMap.HasTile(cellPosition) && m_bShowFog;

        if (bIsFogged)
        {
            m_OnWorldInfoChanged?.Invoke("Biome: ???\nTraversal Time: ???\nBase Forage Amount: ???");

            return;
        }

        CTerrainTile currentTile = m_TerrainTileMap[cellPosition.x, cellPosition.y];

        m_OnWorldInfoChanged?.Invoke(string.Format("Biome: {0}\nTraversal Time: {1}d\nEstimated Forage Amount: {2}", currentTile.BiomeType.ToString(), currentTile.TraversalRate, currentTile.ForageAmount));
    }

    public void OnRightClick(InputAction.CallbackContext context)
    {
        if (!context.performed || IsPausedState())
        {
            return;
        }

        if (!IsPreMovingState())
        {
            m_EffectsMap.ClearAllTiles();
        }
        else if (m_CurrentPath.Count > 0 && !IsMovingState())
        {
            m_GameState = EGameState.Moving;
        }
    }

    private void PlaceObjectOnCell(GameObject objectToPlace, Vector3Int cell)
    {
        cell = ClampCell(cell);

        Vector3 worldPosition = m_WorldGrid.GetCellCenterWorld(cell);
        worldPosition.z = 0;

        objectToPlace.transform.position = worldPosition;
    }

    private void MovePlayerToCell(Vector3Int cell)
    {
        cell = ClampCell(cell);

        // Try change occupied status of previous location
        Vector3Int prevCell = m_PartyPlayerCharacter.CurrentLocation;
        if (IsCellValid(prevCell))
        {
            m_TerrainTileMap[prevCell.x, prevCell.y].IsPlayerOccupied = false;
        }

        Vector3 worldPosition = m_WorldGrid.GetCellCenterWorld(cell);
        worldPosition.z = 0;

        m_PartyPlayerGameObject.transform.position = worldPosition;
        m_PartyPlayerCharacter.CurrentLocation = cell;

        m_TerrainTileMap[cell.x, cell.y].IsPlayerOccupied = true;

        // Update seen tiles (TEMP, no vision stat adjustments yet)
        for (int x = -1; x <= 1; ++x)
        {
            for (int y = -1; y <= 1; ++y)
            {
                int newX = Mathf.Clamp(cell.x + x, 0, m_TerrainTileMap.GetLength(0) - 1);
                int newY = Mathf.Clamp(cell.y + y, 0, m_TerrainTileMap.GetLength(1) - 1);
                Vector3Int newLocation = new Vector3Int(newX, newY, 0);

                m_TerrainTileMap[newX, newY].IsSeen =true;

                m_FogMap.SetTile(newLocation, null);
            }
        }
    }

    private void MoveCameraToCell(Vector3Int cell)
    {
        cell = ClampCell(cell);

        Vector3 worldPosition = m_WorldGrid.GetCellCenterWorld(cell);
        m_CameraManager.MoveCameraToPosition(worldPosition);
    }

    private Vector3Int ClampCell(Vector3Int cell)
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

    private Vector3Int FindStartingLocation()
    {
        if (m_TerrainTileMap == null || m_TerrainTileMap.Length == 0)
        {
            Debug.Log("FindStartingLocation - map is null or empty!");
            return new Vector3Int();
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
                    if (m_TerrainTileMap[i, y].BiomeType == EBiomeType.Beach)
                    {
                        return new Vector3Int(i, y);
                    }
                }
            }
            else
            {
                for (int i = m_MapGenerator.MapSize.y - 1; i >= 0; --i)
                {
                    if (m_TerrainTileMap[i, y].BiomeType == EBiomeType.Beach)
                    {
                        return new Vector3Int(i, y);
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
                    if (m_TerrainTileMap[x, i].BiomeType == EBiomeType.Beach)
                    {
                        return new Vector3Int(x, i);
                    }
                }
            }
            else
            {
                for (int i = m_MapGenerator.MapSize.x - 1; i >= 0; --i)
                {
                    if (m_TerrainTileMap[x, i].BiomeType == EBiomeType.Beach)
                    {
                        return new Vector3Int(x, i);
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
