using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using UnityEngine;
using UnityEngine.Tilemaps;
using static Unity.Collections.Unicode;
using static UnityEngine.EventSystems.EventTrigger;

public class CMapGenerator : MonoBehaviour
{
    [Header("TilemapRefs")]
    [SerializeField]
    private Tilemap m_TerrainMap;
    [SerializeField]
    private Tilemap m_FlairMap;
    [SerializeField]
    private Tilemap m_POIMap;
    [SerializeField]
    private Tilemap m_EffectsMap;
    [SerializeField]
    private Tilemap m_FogMap;
    [SerializeField]
    private Tile m_FogTile;

    [Header("Tile mappings")]
    [SerializeField, Tooltip("This maps strings to tiles for json file")]
    private STileMapping[] m_TileMappings;
    [SerializeField, Tooltip("This maps biomes to strings for json file")]
    private SEnumMapping[] m_EnumMappings;

    [SerializeField, Tooltip("Tile placement rules json file")]
    private TextAsset m_RulesJson;

    [Header("Default Tiles")]
    [SerializeField, Tooltip("Default tiles for no rules")]
    private SDefaultTile[] m_DefaultTiles;

    [Header("MapSize")]
    [SerializeField]
    private Vector2Int m_MapSize;

    [Header("Generation")]
    [SerializeField, Tooltip("Global scalar for noise amplitude")]
    private float m_GlobalAmplitudeScalar = 1.0f;
    [SerializeField, Tooltip("Fall off factor, the lower the number, the faster the noise amplitude falloff from the center.")]
    private float m_FalloffFactor = 1.0f;
    [SerializeField]
    private bool m_bUseFractalNoise = false;

    [Serializable, Tooltip("Required for Fractal Noise! If fractal noise is selected, the first WaveFunction will be used for fractal noise. Otherwise each will be applied as layers")]
    private struct SWaveFunctions
    {
        public int m_Octaves;
        public float m_Persistence;
        public float m_Lacunarity;
        public float m_Scale;
        public float m_AmplitudeScalar;
    }

    [SerializeField]
    private SWaveFunctions[] m_WaveFunctions;

    [SerializeField, Tooltip("Maximum threshold for each biome type")]
    private SElevationLevels m_ElevationLevels;

    [SerializeField, Tooltip("Movement rate by biome")]
    private SBiomeMovement[] m_MovementRates;

    [SerializeField]
    private SPOIMapping[] m_POIMappings;

    private static int[] m_Permutation;

    private static bool m_bIsTileBookInitialized = false;
    private static bool m_bIsPermutionTableInitialized = false;
    private static bool m_bIsBiomeTypesInitialized = false;

    // gradient vectors for 2D noise
    private static Vector2[] m_Gradients2D = {
        new Vector2(1, 1), new Vector2(-1, 1), new Vector2(1, -1), new Vector2(-1, -1),
        new Vector2(1, 0), new Vector2(-1, 0), new Vector2(0, 1), new Vector2(0, -1)
    };

    private static List<CTileRule> m_TileRules = new List<CTileRule>();
    private static Dictionary<string, TileBase> m_TileBook;
    private static STerrainTile[,] m_TerrainTiles;

    // quick lookup
    private static readonly Matrix4x4[] RotationTable = new Matrix4x4[]
    {
        Matrix4x4.identity, // 0 degrees
        Matrix4x4.TRS(Vector3.zero, Quaternion.Euler(0, 0, -90f), Vector3.one),
        Matrix4x4.TRS(Vector3.zero, Quaternion.Euler(0, 0, -180f), Vector3.one),
        Matrix4x4.TRS(Vector3.zero, Quaternion.Euler(0, 0, -270f), Vector3.one)
    };

    private bool m_bIgnoreTileRules = false;

    //-- getters
    public Vector2Int MapSize
    {
        get { return m_MapSize; }
    }

    public Tilemap TerrainMap
    {
        get { return m_TerrainMap; }
    }

    public Tilemap FogMap
    {
        get { return m_FogMap; }
    }

    public Tilemap EffectsMap
    {
        get { return m_EffectsMap; }
    }

    public bool IgnoreTileRules
    {
        set { m_bIgnoreTileRules = value; }
    }
    //--

    private void LoadRulesFromJson()
    {
        if (m_RulesJson != null)
        {
            CRuleCollection collection = JsonUtility.FromJson<CRuleCollection>(m_RulesJson.text);
            m_TileRules = collection.Rules;
        }
    }

    public SRuleResult GetRule(string self, string west, string northwest, string north, string northeast, string east, string southeast, string south, string southwest)
    {
        if (m_bIgnoreTileRules)
        {
            return new SRuleResult("Empty", 0);
        }

        foreach (var rule in m_TileRules)
        {
            if (MatchesRule(self, west, northwest, north, northeast, east, southeast, south, southwest, rule))
            {
                return new SRuleResult(rule.Result, rule.Rotations);
            }
        }
        return new SRuleResult("Empty", 0);
    }

    private bool MatchesRule(string self, string west, string northwest, string north, string northeast, string east, string southeast, string south, string southwest, CTileRule rule)
    {
        return self == rule.SelfBiome &&
               Matches(west, rule.West) &&
               Matches(northwest, rule.NorthWest) &&
               Matches(north, rule.North) &&
               Matches(northeast, rule.NorthEast) &&
               Matches(east, rule.East) &&
               Matches(southeast, rule.SouthEast) &&
               Matches(south, rule.South) &&
               Matches(southwest, rule.SouthWest);
    }

    private bool Matches(string actual, string[] allowed)
    {
        return allowed.Contains("*") || allowed.Contains(actual);
    }

    public void InitializeTileBook()
    {
        m_TileBook = new Dictionary<string, TileBase>();

        foreach (STileMapping tilemapping in m_TileMappings)
        {
            m_TileBook[tilemapping.Name] = tilemapping.Tile;
        }

        m_bIsTileBookInitialized = true;
    }

    public STerrainTile[,] GenerateMap()
    {
        LoadRulesFromJson();
        InitializeTileBook();

        InitializePermutationTable();
        GenerateHeightMap();

        for (int x = 0; x < m_MapSize.x; ++x)
        {
            for (int y = 0; y < m_MapSize.y; ++y)
            {
                EBiomeType currentBiome = m_TerrainTiles[x, y].GetBiomeType();
                SRuleResult result = GetTile(currentBiome, x, y);
                TileBase tile;

                // if result is "empty", just place the base biome tile for now- rules may not yet be set up
                if (result.Result == "Empty" || string.IsNullOrEmpty(result.Result))
                {
                    tile = Array.Find<SDefaultTile>(m_DefaultTiles, p => p.BiomeType == currentBiome).Tile;
                }
                else
                {
                    if (m_TileBook.ContainsKey(result.Result))
                    {
                        tile = m_TileBook[result.Result];
                    }
                    else
                    {
                        Debug.Log(string.Format("Missing tile from result: {1}", result.Result));
                        tile = Array.Find<SDefaultTile>(m_DefaultTiles, p => p.BiomeType == currentBiome).Tile;
                    }
                }

                if (tile == null)
                {
                    Debug.Log("GenerateMap - null tile!");
                    continue;
                }

                Vector3Int position = new Vector3Int(x, y, 0);

                TileBase poiTile = AddPOI(m_TerrainTiles[x, y]);

                m_TerrainMap.SetTile(position, tile);
                m_TerrainMap.SetTransformMatrix(position, RotationTable[result.Rotations]);

                if (poiTile != null)
                {
                    m_POIMap.SetTile(position, poiTile);
                }
            }
        }

        return m_TerrainTiles;
    }

    private TileBase AddPOI(STerrainTile terrainTile)
    {
        TileBase selectedPOITile = null;

        if (terrainTile.GetBiomeType() != EBiomeType.Invalid)
        {
            SPOIMapping poiMapping = Array.Find(m_POIMappings, p => p.BiomeType == terrainTile.GetBiomeType());

            float minLikelihood = 100f;

            if (poiMapping.BiomeType != EBiomeType.Invalid)
            {
                float random = UnityEngine.Random.Range(0f, 100f);

                foreach (SPOISettings poiSetting in poiMapping.POISettings)
                {
                    if (random < poiSetting.Likelihood)
                    {
                        // TODO: set POI on tile
                        if (poiSetting.Likelihood < minLikelihood)
                        {
                            minLikelihood = poiSetting.Likelihood;
                            selectedPOITile = poiSetting.Tile;
                        }
                        else if (poiSetting.Likelihood == minLikelihood)
                        {
                            // If chances are the same, roll for stomping
                            if (UnityEngine.Random.Range(0, 1) > 0.5f)
                            {
                                selectedPOITile = poiSetting.Tile;
                            }
                        }
                    }
                }
            }
        }

        return selectedPOITile;
    }

    private void GenerateHeightMap()
    {
        m_TerrainTiles = new STerrainTile[m_MapSize.x, m_MapSize.y];

        Vector2Int center = GetCenterPoint();
        float maxDistance = center.magnitude;

        // create heightmap
        for (int x = 0; x < m_MapSize.x; ++x)
        {
            for (int y = 0; y < m_MapSize.y; ++y)
            {
                float noiseValue = 0f;

                // generate initial wave
                if (m_bUseFractalNoise && m_WaveFunctions.Count() > 0)
                {
                    noiseValue = Mathf.Abs(FractalNoise(
                        x * m_WaveFunctions[0].m_Scale,
                        y * m_WaveFunctions[0].m_Scale,
                        m_WaveFunctions[0].m_Octaves,
                        m_WaveFunctions[0].m_Persistence,
                        m_WaveFunctions[0].m_Lacunarity)) * m_WaveFunctions[0].m_AmplitudeScalar * m_GlobalAmplitudeScalar;
                }
                else
                {
                    noiseValue = Mathf.Abs(Noise(x, y)) * m_GlobalAmplitudeScalar;
                }

                // add wave functions
                int startingIndex = m_bUseFractalNoise ? 1 : 0;
                if (m_WaveFunctions.Count() > startingIndex)
                {
                    int arrayCount = m_WaveFunctions.Count();

                    for (int i = startingIndex; i < arrayCount; ++i)
                    {
                        noiseValue += Mathf.Abs(FractalNoise(
                            x * m_WaveFunctions[i].m_Scale,
                            y * m_WaveFunctions[i].m_Scale,
                            m_WaveFunctions[i].m_Octaves,
                            m_WaveFunctions[i].m_Persistence,
                            m_WaveFunctions[i].m_Lacunarity)) * m_WaveFunctions[i].m_AmplitudeScalar;
                    }
                }

                // calculate distance from center (normalized 0-1)
                float distance = Vector2.Distance(new Vector2(x, y), center) / maxDistance;

                // create falloff - 1 in center, 0 at edges, to keep moutains in the middle
                float falloff = 1f - Mathf.Pow(distance, m_FalloffFactor);

                // apply weighting - stronger noise in center and ensure the middle is never water
                float weightedNoise = noiseValue * m_GlobalAmplitudeScalar * falloff + (falloff * m_ElevationLevels.WaterLevel);

                EBiomeType biomeType = GetBiomeType(weightedNoise);

                SBiomeMovement biomeMovement = Array.Find(m_MovementRates, p => p.BiomeType == biomeType);
                m_TerrainTiles[x, y] = new STerrainTile(weightedNoise, /*bExplored*/ false, biomeType, (biomeMovement.BiomeType == EBiomeType.Invalid) ? 1f : biomeMovement.MovementTime);
            }
        }

        m_bIsBiomeTypesInitialized = true;
    }

    private Vector2Int GetCenterPoint()
    {
        return new Vector2Int((int)(m_MapSize.x * 0.5f), (int)(m_MapSize.y * 0.5f));
    }

    private static void InitializePermutationTable()
    {
        // create base permutation
        m_Permutation = new int[512];
        int[] basePerm = new int[256];

        // fill with 0-255
        for (int i = 0; i < 256; i++)
        {
            basePerm[i] = i;
        }

        // shuffle
        for (int i = 255; i > 0; i--)
        {
            int j = UnityEngine.Random.Range(0, i + 1);
            // swap
            int temp = basePerm[i];
            basePerm[i] = basePerm[j];
            basePerm[j] = temp;
        }

        // duplicate for performance
        for (int i = 0; i < 256; i++)
        {
            m_Permutation[i] = basePerm[i];
            m_Permutation[i + 256] = basePerm[i];
        }

        m_bIsPermutionTableInitialized = true;
    }

    // smooth interpolation curve
    private static float Fade(float t)
    {
        return t * t * t * (t * (t * 6f - 15f) + 10f);
    }

    // linear interpolation
    private static float Lerp(float a, float b, float t)
    {
        return a + t * (b - a);
    }

    // get gradient vector for a grid point
    private static Vector2 GetGradient(int x, int y)
    {
        int hash = m_Permutation[(x + m_Permutation[y & 255]) & 255];
        return m_Gradients2D[hash & 7];
    }

    // compute dot product between distance and gradient vectors
    private static float DotGridGradient(int ix, int iy, float x, float y)
    {
        Vector2 gradient = GetGradient(ix, iy);
        float dx = x - ix;
        float dy = y - iy;
        return dx * gradient.x + dy * gradient.y;
    }

    private static float Noise(float x, float y)
    {
        if (!m_bIsPermutionTableInitialized)
        {
            Debug.Log("MapGenerator::Noise - error: permutation table not initialized!");
            return 0f;
        }

        // find grid cell coordinates
        int x0 = Mathf.FloorToInt(x);
        int y0 = Mathf.FloorToInt(y);
        int x1 = x0 + 1;
        int y1 = y0 + 1;

        // get fractional parts
        float sx = x - x0;
        float sy = y - y0;

        // compute dot products with all four corners
        float n00 = DotGridGradient(x0, y0, x, y);
        float n01 = DotGridGradient(x0, y1, x, y);
        float n10 = DotGridGradient(x1, y0, x, y);
        float n11 = DotGridGradient(x1, y1, x, y);

        // fade the interpolation weights
        float sxFaded = Fade(sx);
        float syFaded = Fade(sy);

        // interpolate
        float nx0 = Lerp(n00, n10, sxFaded);
        float nx1 = Lerp(n01, n11, sxFaded);
        return Lerp(nx0, nx1, syFaded);
    }

    private static float FractalNoise(float x, float y, int octaves, float persistence, float lacunarity)
    {
        if (!m_bIsPermutionTableInitialized)
        {
            Debug.Log("MapGenerator::FractalNoise - error: permutation table not initialized!");
            return 0f;
        }

        float total = 0f;
        float frequency = 1f;
        float amplitude = 1f;
        float maxValue = 0f;

        // reapply noise function on itself
        for (int i = 0; i < octaves; i++)
        {
            total += Noise(x * frequency, y * frequency) * amplitude;
            maxValue += amplitude;
            amplitude *= persistence;
            frequency *= lacunarity;
        }

        return total / maxValue;
    }

    private EBiomeType GetBiomeType(float elevation)
    {
        // todo: make this data driven
        if (elevation < m_ElevationLevels.DeepWaterLevel)
        {
            return EBiomeType.DeepWater;
        }
        if (elevation < m_ElevationLevels.WaterLevel)
        {
            return EBiomeType.Water;
        }
        else if (elevation < m_ElevationLevels.BeachLevel)
        {
            return EBiomeType.Beach;
        }
        else if (elevation < m_ElevationLevels.GrasslandLevel)
        {
            return EBiomeType.Grasslands;
        }
        else if (elevation < m_ElevationLevels.ForestLevel)
        {
            return EBiomeType.Forest;
        }
        else if (elevation < m_ElevationLevels.MountainLevel)
        {
            return EBiomeType.Mountain;
        }

        return EBiomeType.Invalid;
    }

    private string GetBiomeString(EBiomeType biomeType)
    {
        SEnumMapping mapping = Array.Find<SEnumMapping>(m_EnumMappings, p => p.BiomeType == biomeType);

        if (string.IsNullOrEmpty(mapping.Name))
        {
            return "Empty";
        }

        return mapping.Name;
    }

    private string GetNeighborBiome(int x, int y)
    {
        int clampedX = Mathf.Clamp(x, 0, m_MapSize.x - 1);
        int clampedY = Mathf.Clamp(y, 0, m_MapSize.y - 1);

        return GetBiomeString(m_TerrainTiles[clampedX, clampedY].GetBiomeType());
    }

    private SRuleResult GetTile(EBiomeType biomeType, int x, int y)
    {
        if (!m_bIsBiomeTypesInitialized)
        {
            Debug.Log("GetTile - BiomeTypes have not been initialized!");
            return new SRuleResult("Empty", 0);
        }

        if (!m_bIsTileBookInitialized)
        {
            Debug.Log("GetTile - TileBook has not been initialized!");
            return new SRuleResult("Empty", 0);
        }

        // get strings to check rules
        string self = GetBiomeString(biomeType);
        string west = GetNeighborBiome(x - 1, y);
        string northwest = GetNeighborBiome(x - 1, y + 1);
        string north = GetNeighborBiome(x, y + 1);
        string northeast = GetNeighborBiome(x + 1, y + 1);
        string east = GetNeighborBiome(x + 1, y);
        string southeast = GetNeighborBiome(x + 1, y - 1);
        string south = GetNeighborBiome(x, y - 1);
        string southwest = GetNeighborBiome(x - 1, y - 1);

        return GetRule(self, west, northwest, north, northeast, east, southeast, south, southwest);
    }

    public void CreateFog()
    {
        for (int x = 0; x < m_MapSize.x; ++x)
        {
            for (int y = 0; y < m_MapSize.y; ++y)
            {
                if (!m_TerrainTiles[x, y].IsSeen())
                {
                    m_FogMap.SetTile(new Vector3Int(x, y), m_FogTile);
                }
            }
        }
    }
}
