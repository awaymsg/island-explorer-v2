using System;
using System.Linq;
using UnityEngine;
using UnityEngine.Tilemaps;

public class MapGenerator : MonoBehaviour
{
    [Header("TilemapRefs")]
    [SerializeField]
    private Tilemap m_TerrainMap;

    [Header("TilePalette")]
    [SerializeField]
    private TilePalette m_TerrainPalette;

    [Header("MapSize")]
    [SerializeField]
    private Vector2Int m_MapSize;

    [Header("Generation")]
    [SerializeField]
    private bool m_bUseRandomSeed = false;
    [SerializeField]
    private int m_RandomSeed = 0;
    [SerializeField]
    private float m_GlobalAmplitudeScalar = 1.0f;
    [SerializeField]
    private float m_FalloffFactor = 1.0f;
    [SerializeField]
    private bool m_bUseFractalNoise = false;

    [System.Serializable]
    private struct WaveFunctions
    {
        public int m_Octaves;
        public float m_Persistence;
        public float m_Lacunarity;
        public float m_Scale;
        public float m_AmplitudeScalar;
    }

    [SerializeField]
    private WaveFunctions[] m_WaveFunctions;

    [SerializeField]
    private ElevationLevels m_ElevationLevels;

    private static int[] m_Permutation;
    private static bool m_bIsInitialized = false;

    // Gradient vectors for 2D noise
    private static Vector2[] m_Gradients2D = {
        new Vector2(1, 1), new Vector2(-1, 1), new Vector2(1, -1), new Vector2(-1, -1),
        new Vector2(1, 0), new Vector2(-1, 0), new Vector2(0, 1), new Vector2(0, -1)
    };

    void Start()
    {
        GenerateMap();
    }

    public TerrainTile[,] GenerateMap()
    {
        if (m_bUseRandomSeed)
        {
            UnityEngine.Random.InitState(m_RandomSeed);
        }

        InitializeTilePalette();

        InitializePermutationTable();
        TerrainTile[,] terrainTiles = GenerateHeightMap();

        // consider using SetTiles
        for (int x = 0; x < m_MapSize.x; ++x)
        {
            for (int y = 0; y < m_MapSize.y; ++y)
            {
                float elevation = terrainTiles[x, y].GetElevation();
                TileBase tile = terrainTiles[x, y].GetTile();
                m_TerrainMap.SetTile(new Vector3Int(x, y, 0), tile);
            }
        }

        return terrainTiles;
    }

    private TerrainTile[,] GenerateHeightMap()
    {
        TerrainTile[,] terrainTiles = new TerrainTile[m_MapSize.x, m_MapSize.y];

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
                float weightedNoise = noiseValue * m_GlobalAmplitudeScalar * falloff + (falloff * m_ElevationLevels.m_WaterLevel);

                terrainTiles[x, y] = new TerrainTile(new Vector2Int(x, y), weightedNoise, /*bExplored*/ false, m_TerrainPalette.GetBiomeType(weightedNoise), m_TerrainPalette.GetTile(weightedNoise));
            }
        }

        return terrainTiles;
    }

    private Vector2Int GetCenterPoint()
    {
        return new Vector2Int((int)(m_MapSize.x * 0.5f), (int)(m_MapSize.y * 0.5f));
    }

    private void InitializeTilePalette()
    {
        m_TerrainPalette.SetLevels(m_ElevationLevels);
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
            // Swap
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

        m_bIsInitialized = true;
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
        if (!m_bIsInitialized)
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
        if (!m_bIsInitialized)
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
}
