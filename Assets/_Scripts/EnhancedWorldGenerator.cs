using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class EnhancedWorldGenerator : MonoBehaviour
{
    public WorldRenderer worldRenderer;
    public BlockData blockData;

    public NoiseDataSO heightMapNoiseData, stoneNoiseData, caveNoiseData, oreNoiseData, tunnelNoiseData;

    // Dimensions del mapa
    public int mapWidth = 1000;       // Amplada del mapa
    public int mapDepth = 500;        // Profunditat total del mapa (soluciona el problema de la profunditat limitada)
    public int chunkSize = 100;       // Mida dels chunks per a una millor gestió de memòria
    public int oceanLevel = 0;        // Nivell d'aigua del mar (tots els oceans s'anivellaran a aquesta altura)
    public int oceanDepth = 1;       // Profunditat dels oceans

    // Llindars de biomes
    private readonly float[] biomeThresholds = { 0.5f, 0.7f, 0.9f, 1.0f }; // Planures, Desert, Neu, Oceà

    // Generació de coves i túnels
    public float caveThreshold = 0.45f;
    public float tunnelThreshold = 0.35f;  // Llindar per a la generació de túnels que connecten coves
    public int minCaveDepth = 5;
    public int maxCaveSize = 15;
    public float caveConnectivity = 0.3f;  // Determina la freqüència de connexions entre coves

    // Paràmetres de generació de minerals
    [System.Serializable]
    public class OreParameters
    {
        public TileBase oreTile;
        public TileBase oreAltTile;
        public int minDepth;
        public int maxDepth;
        public float peakDepth; // Profunditat on el mineral és més comú
        public float rarity;    // Més alt = més rar (1.0 = molt comú, 10.0 = molt rar)
    }

    public List<OreParameters> oreTypes = new List<OreParameters>();

    // Paràmetres de lava
    public int lavaStartDepth = 80;
    public float lavaChance = 0.05f;

    private System.Random random;

    // Inicialització
    private void Awake()
    {
        random = new System.Random();
        
        // Inicialitza paràmetres de minerals per defecte si no s'han establert
        if (oreTypes.Count == 0)
        {
            // Carbó (comú, poc profund)
            oreTypes.Add(new OreParameters
            {
                oreTile = blockData.stoneCoal,
                oreAltTile = blockData.stoneCoalAlt,
                minDepth = 5,
                maxDepth = 80,
                peakDepth = 20,
                rarity = 2.0f
            });
            
            // Ferro (mig, profunditat mitjana)
            oreTypes.Add(new OreParameters
            {
                oreTile = blockData.stoneIron,
                oreAltTile = blockData.stoneIronAlt,
                minDepth = 10,
                maxDepth = 70,
                peakDepth = 30,
                rarity = 3.0f
            });
            
            // Plata (poc comú, mitjana-profunda)
            oreTypes.Add(new OreParameters
            {
                oreTile = blockData.stoneSilver,
                oreAltTile = blockData.stoneSilverAlt,
                minDepth = 20,
                maxDepth = 60,
                peakDepth = 40,
                rarity = 5.0f
            });
            
            // Or (rar, profund)
            oreTypes.Add(new OreParameters
            {
                oreTile = blockData.stoneGold,
                oreAltTile = blockData.stoneGoldAlt,
                minDepth = 30,
                maxDepth = 90,
                peakDepth = 60,
                rarity = 7.0f
            });
            
            // Diamant (molt rar, molt profund)
            oreTypes.Add(new OreParameters
            {
                oreTile = blockData.stoneDiamond,
                oreAltTile = blockData.stoneDiamondAlt,
                minDepth = 50,
                maxDepth = 95,
                peakDepth = 70,
                rarity = 9.0f
            });
            
            // Rubí (extremadament rar, extens)
            oreTypes.Add(new OreParameters
            {
                oreTile = blockData.rubyStone,
                oreAltTile = blockData.rubyStoneAlt,
                minDepth = 40,
                maxDepth = 100,
                peakDepth = 80,
                rarity = 10.0f
            });
        }
    }

    // Funció principal de generació del món
    public void GenerateWorld()
    {
        worldRenderer.ClearGroundTilemap();
        worldRenderer.ClearPerlind2DTilemap();
        
        // Genera la capa de base (sempre hi haurà 100 blocs de profunditat)
        GenerateBaseLayer();
        
        // Genera chunks
        int numChunks = mapWidth / chunkSize;
        
        for (int chunkIndex = 0; chunkIndex < numChunks; chunkIndex++)
        {
            int chunkStartX = chunkIndex * chunkSize - (mapWidth / 2);
            int chunkEndX = chunkStartX + chunkSize;
            
            // Determina el tipus de bioma per aquest chunk
            float biomeValue = (float)random.NextDouble();
            BiomeType biomeType = DetermineBiomeType(biomeValue);
            
            GenerateChunk(chunkStartX, chunkEndX, biomeType);
        }
        
        // Genera túnels que connecten les coves
        GenerateTunnels();
    }

    // Genera la capa base del món (100 blocs de profunditat sempre presents)
    private void GenerateBaseLayer()
    {
        int baseLayerHeight = 100;
        
        for (int x = -mapWidth / 2; x < mapWidth / 2; x++)
        {
            for (int y = -baseLayerHeight; y < 0; y++)
            {
                // Determina si és una cova
                bool isCave = IsDeepCave(x, y);
                
                if (isCave)
                {
                    // Per a les coves, només estableix la tesel·la de fons
                    worldRenderer.SetBackgroundTile(x, y, blockData.stoneDark);
                    
                    // Comprova la presència de lava al fons de les coves
                    if (y < -baseLayerHeight + 20 && Random.value < lavaChance * 2)
                    {
                        worldRenderer.SetGroundTile(x, y, blockData.lavaTile);
                    }
                    continue;
                }
                
                // Estableix la tesel·la de fons
                worldRenderer.SetBackgroundTile(x, y, blockData.stoneDark);
                
                // Determina la tesel·la del primer pla
                TileBase foregroundTile;
                
                // Comprova la generació de minerals
                foregroundTile = GetOreTile(x, y + mapDepth);
                
                // Si no s'ha generat cap mineral, utilitza pedra o lava
                if (foregroundTile == null)
                {
                    if (y < -baseLayerHeight + 20 && Random.value < lavaChance * 1.5f)
                    {
                        foregroundTile = blockData.lavaTile;
                    }
                    else
                    {
                        foregroundTile = blockData.stoneTile;
                    }
                }
                
                worldRenderer.SetGroundTile(x, y, foregroundTile);
            }
        }
    }

    // Determina el tipus de bioma basat en un valor aleatori
    private BiomeType DetermineBiomeType(float value)
    {
        if (value < biomeThresholds[0]) return BiomeType.Plains;
        if (value < biomeThresholds[1]) return BiomeType.Desert;
        if (value < biomeThresholds[2]) return BiomeType.Snow;
        return BiomeType.Ocean;
    }

    // Genera un chunk del món
    private void GenerateChunk(int startX, int endX, BiomeType biomeType)
    {
        // Pre-calcula les altures de superfície per tot el chunk
        Dictionary<int, int> surfaceHeights = new Dictionary<int, int>();
        
        // Primera passada: calcula les altures
        for (int x = startX; x < endX; x++)
        {
            // Genera l'altura de la superfície per aquesta columna
            float noise = SumNoise((int)(heightMapNoiseData.offset.x + x), 1, heightMapNoiseData);
            float noiseInRange = RangeMap(noise, 0, 1, heightMapNoiseData.noiseRangeMin, heightMapNoiseData.noiseRangeMax);
            int surfaceHeight = Mathf.FloorToInt(noiseInRange);
            
            // Ajusta l'altura de la superfície segons el bioma
            surfaceHeight = AdjustSurfaceHeightForBiome(surfaceHeight, biomeType);
            
            // Si és oceà, assegura't que la superfície estigui al nivell del mar
            if (biomeType == BiomeType.Ocean)
            {
                surfaceHeight = oceanLevel; // Assegurem que els oceans estiguin anivellats
            }
            
            surfaceHeights[x] = surfaceHeight;
        }
        
        // Segona passada: genera les columnes
        for (int x = startX; x < endX; x++)
        {
            int surfaceHeight = surfaceHeights[x];
            
            // Genera columna des del fons fins a la superfície
            for (int y = 0; y < mapDepth; y++)
            {
                // Comprova primer si hi ha coves
                bool isCave = IsCave(x, y, surfaceHeight);
                
                if (isCave)
                {
                    // Per a les coves, només estableix la tesel·la de fons
                    TileBase backgroundTile = GetBackgroundTileForBiome(biomeType, y, surfaceHeight);
                    worldRenderer.SetBackgroundTile(x, y, backgroundTile);
                    
                    // Comprova si hi ha lava al fons de les coves
                    if (y > lavaStartDepth && Random.value < lavaChance)
                    {
                        worldRenderer.SetGroundTile(x, y, blockData.lavaTile);
                    }
                    continue;
                }
                
                // Estableix la tesel·la de fons
                TileBase backTile = GetBackgroundTileForBiome(biomeType, y, surfaceHeight);
                worldRenderer.SetBackgroundTile(x, y, backTile);
                
                // Determina la tesel·la del primer pla
                TileBase foregroundTile;
                
                // Comprova les tesel·les de superfície
                if (y == surfaceHeight)
                {
                    // La tesel·la de superfície canvia segons el bioma
                    foregroundTile = GetSurfaceTileForBiome(biomeType);
                }
                // Tesel·les subterrànies
                else if (y < surfaceHeight)
                {
                    // La capa superior sota la superfície és terra/sorra/etc. segons el bioma
                    if (surfaceHeight - y <= 3)
                    {
                        foregroundTile = GetSubsurfaceTileForBiome(biomeType);
                    }
                    // A sota hi ha pedra amb minerals ocasionals
                    else
                    {
                        // Comprova la generació de minerals
                        foregroundTile = GetOreTile(x, y);
                        
                        // Si no s'ha generat cap mineral, utilitza pedra o lava
                        if (foregroundTile == null)
                        {
                            if (y > lavaStartDepth && Random.value < lavaChance)
                            {
                                foregroundTile = blockData.lavaTile;
                            }
                            else
                            {
                                foregroundTile = blockData.stoneTile;
                            }
                        }
                    }
                }
                // Aigua i aire sobre el nivell de superfície
                else 
                {
                    // Per als biomes oceànics, omple amb aigua
                    if (biomeType == BiomeType.Ocean && y <= surfaceHeight + oceanDepth)
                    {
                        foregroundTile = blockData.waterTile;
                    }
                    else
                    {
                        // Genera alguna decoració a la superfície
                        if (y == surfaceHeight + 1 && biomeType == BiomeType.Plains && Random.value < 0.15f)
                        {
                            // Tipus d'herba aleatori
                            int grassType = Random.Range(1, 5);
                            switch (grassType)
                            {
                                case 1: foregroundTile = blockData.grass1Tile; break;
                                case 2: foregroundTile = blockData.grass2Tile; break;
                                case 3: foregroundTile = blockData.grass3Tile; break;
                                default: foregroundTile = blockData.grass4Tile; break;
                            }
                        }
                        else if (y == surfaceHeight + 1 && biomeType == BiomeType.Plains && Random.value < 0.05f)
                        {
                            // Roques
                            foregroundTile = blockData.rockTile;
                        }
                        else
                        {
                            // Aire (sense tesel·la)
                            foregroundTile = null;
                        }
                    }
                }
                
                // Estableix la tesel·la si no és nul·la
                if (foregroundTile != null)
                {
                    worldRenderer.SetGroundTile(x, y, foregroundTile);
                }
            }
            
            // Genera arbres en biomes de planures ocasionalment
            if (biomeType == BiomeType.Plains && Random.value < 0.05f)
            {
                GenerateTree(x, surfaceHeight + 1);
            }
        }
    }

    // Genera túnels que connecten coves
    private void GenerateTunnels()
    {
        for (int x = -mapWidth / 2; x < mapWidth / 2; x++)
        {
            for (int y = 0; y < mapDepth; y++)
            {
                // Només crea túnels a una certa profunditat
                if (y < minCaveDepth * 2)
                    continue;
                    
                // Utilitza un soroll diferent per als túnels per evitar superposicions
                float tunnelNoise = SumNoise(x, y + 1000, tunnelNoiseData);
                
                // Augmenta la probabilitat de túnels quan estàs més profund
                float depthFactor = Mathf.Min(1.0f, (float)y / 50.0f);
                float adjustedThreshold = tunnelThreshold - (0.1f * depthFactor);
                
                // Genera túnels horitzontals i verticals
                if (tunnelNoise > adjustedThreshold && Random.value < caveConnectivity)
                {
                    // Verifica si hi ha coves properes per connectar
                    bool hasCaveNearby = HasCaveNearby(x, y, 10);
                    
                    if (hasCaveNearby)
                    {
                        // Crea un túnel
                        int tunnelLength = Random.Range(3, 15);
                        int tunnelDirection = Random.Range(0, 4); // 0-horitzontal, 1-vertical, 2-diagonal dreta, 3-diagonal esquerra
                        
                        CreateTunnel(x, y, tunnelLength, tunnelDirection);
                    }
                }
            }
        }
    }

    // Comprova si hi ha coves properes
    private bool HasCaveNearby(int x, int y, int radius)
    {
        for (int dx = -radius; dx <= radius; dx++)
        {
            for (int dy = -radius; dy <= radius; dy++)
            {
                if (dx*dx + dy*dy <= radius*radius) // Dins del cercle
                {
                    if (IsCave(x + dx, y + dy, 100)) // L'altura no importa per a aquesta comprovació
                    {
                        return true;
                    }
                }
            }
        }
        return false;
    }

    // Crea un túnel
    private void CreateTunnel(int startX, int startY, int length, int direction)
    {
        int dx = 0, dy = 0;
        
        // Estableix la direcció del túnel
        switch (direction)
        {
            case 0: dx = 1; dy = 0; break; // Horitzontal
            case 1: dx = 0; dy = 1; break; // Vertical
            case 2: dx = 1; dy = 1; break; // Diagonal dreta
            case 3: dx = 1; dy = -1; break; // Diagonal esquerra
        }
        
        // Crea el túnel
        for (int i = 0; i < length; i++)
        {
            int x = startX + i * dx;
            int y = startY + i * dy;
            
            // Crea una petita cavitat al voltant de cada punt del túnel
            int cavitySize = Random.Range(1, 3);
            
            for (int cx = -cavitySize; cx <= cavitySize; cx++)
            {
                for (int cy = -cavitySize; cy <= cavitySize; cy++)
                {
                    if (cx*cx + cy*cy <= cavitySize*cavitySize) // Forma circular
                    {
                        // Elimina el bloc i afegeix un fons fosc
                        worldRenderer.SetGroundTile(x + cx, y + cy, null);
                        worldRenderer.SetBackgroundTile(x + cx, y + cy, blockData.stoneDark);
                    }
                }
            }
        }
    }

    // Ajusta l'altura de la superfície segons el bioma
    private int AdjustSurfaceHeightForBiome(int baseHeight, BiomeType biomeType)
    {
        switch (biomeType)
        {
            case BiomeType.Plains:
                return baseHeight;
            case BiomeType.Desert:
                return baseHeight - 2; // Els deserts són lleugerament més baixos
            case BiomeType.Snow:
                return baseHeight + 3; // Els biomes de neu són més alts
            case BiomeType.Ocean:
                return oceanLevel; // Els oceans sempre s'anivellen a aquesta altura
            default:
                return baseHeight;
        }
    }

    // Obté la tesel·la de superfície segons el bioma
    private TileBase GetSurfaceTileForBiome(BiomeType biomeType)
    {
        switch (biomeType)
        {
            case BiomeType.Plains:
                return blockData.dirtGrass;
            case BiomeType.Desert:
                return blockData.dirtSand;
            case BiomeType.Snow:
                return blockData.dirstSnow;
            case BiomeType.Ocean:
                return blockData.sandTile;
            default:
                return blockData.dirtGrass;
        }
    }

    // Obté la tesel·la sota la superfície segons el bioma
    private TileBase GetSubsurfaceTileForBiome(BiomeType biomeType)
    {
        switch (biomeType)
        {
            case BiomeType.Plains:
                return blockData.dirtTile;
            case BiomeType.Desert:
                return blockData.sandTile;
            case BiomeType.Snow:
                return blockData.dirtTile;
            case BiomeType.Ocean:
                return blockData.sandTile;
            default:
                return blockData.dirtTile;
        }
    }

    // Obté la tesel·la de fons segons el bioma
    private TileBase GetBackgroundTileForBiome(BiomeType biomeType, int y, int surfaceHeight)
    {
        // El fons subterrani sempre és pedra fosca o terra
        if (y < surfaceHeight - 3)
        {
            return blockData.stoneDark;
        }
        else if (y <= surfaceHeight)
        {
            switch (biomeType)
            {
                case BiomeType.Desert:
                    return blockData.sandDark;
                default:
                    return blockData.dirtDark;
            }
        }
        
        // Per sobre del terreny no hi ha fons
        return null;
    }

    // Obté la tesel·la de mineral
    private TileBase GetOreTile(int x, int y)
    {
        // Comprova cada tipus de mineral
        foreach (var ore in oreTypes)
        {
            if (y >= ore.minDepth && y <= ore.maxDepth)
            {
                // Calcula la probabilitat basada en la profunditat i la raresa
                // Els minerals són més comuns prop de la seva profunditat de pic
                float depthFactor = 1.0f - Mathf.Abs(y - ore.peakDepth) / (float)(ore.maxDepth - ore.minDepth);
                float probability = depthFactor / ore.rarity;
                
                // Afegeix una mica de soroll per fer vetes de mineral
                float noiseValue = SumNoise(x, y, oreNoiseData);
                probability *= noiseValue * 2;
                
                // Comprova si hem de generar aquest mineral
                if (Random.value < probability)
                {
                    // Utilitza la textura alternativa de vegades
                    return Random.value < 0.5f ? ore.oreTile : ore.oreAltTile;
                }
            }
        }
        
        return null; // No s'ha generat cap mineral
    }

    // Comprova si hi ha coves
    private bool IsCave(int x, int y, int surfaceHeight)
    {
        // No hi ha coves prop de la superfície
        if (y > surfaceHeight - minCaveDepth)
        {
            return false;
        }
        
        // Utilitza soroll de Perlin 3D per a la generació de coves
        float caveNoise = SumNoise(x, y, caveNoiseData);
        
        // Més coves com més profund estàs
        float depthFactor = Mathf.Min(1.0f, (float)y / 30.0f);
        float threshold = caveThreshold + (0.1f * depthFactor);
        
        // Crea coves més obertes, similar al generador bàsic
        if (caveNoise > threshold)
        {
            // Fes coves més grans i connectades
            int caveSize = Mathf.FloorToInt(caveNoise * maxCaveSize); 
            
            // Fes coves més freqüents i més grans en profunditat
            return true;
        }
        
        return false;
    }

    // Comprova si hi ha coves profundes (per a la capa base)
    private bool IsDeepCave(int x, int y)
    {
        // Utilitza un offset diferent per coves profundes
        float caveNoise = SumNoise(x, y + 5000, caveNoiseData);
        
        // Ajusta la probabilitat de coves segons la profunditat
        float depthFactor = Mathf.Abs(y) / 50.0f;
        float threshold = caveThreshold - (0.1f * depthFactor);
        
        return caveNoise > threshold;
    }

    // Genera un arbre
    private void GenerateTree(int x, int y)
    {
        // Generació simple d'arbres
        int trunkHeight = Random.Range(3, 6);
        
        // Part inferior del tronc
        worldRenderer.SetGroundTile(x, y, blockData.trunkBottomTile);
        
        // Parts mitjanes del tronc
        for (int i = 1; i < trunkHeight - 1; i++)
        {
            worldRenderer.SetGroundTile(x, y + i, blockData.trunkMidTile);
        }
        
        // Fulles
        int leavesWidth = 3;
        int leavesHeight = 3;
        
        for (int lx = -leavesWidth / 2; lx <= leavesWidth / 2; lx++)
        {
            for (int ly = 0; ly < leavesHeight; ly++)
            {
                // Salta les posicions del tronc
                if (lx == 0 && ly == 0)
                    continue;
                    
                worldRenderer.SetGroundTile(x + lx, y + trunkHeight - 1 + ly, blockData.leavesTransparentTile);
            }
        }
    }

    // Suma soroll
    public float SumNoise(int x, int y, NoiseDataSO noiseSettings)
    {
        float amplitude = 1;
        float frequency = noiseSettings.startFrequency;
        float noiseSum = 0;
        float amplitudeSum = 0;
        
        for (int i = 0; i < noiseSettings.octaves; i++)
        {
            noiseSum += amplitude * Mathf.PerlinNoise(x * frequency, y * frequency);
            amplitudeSum += amplitude;
            amplitude *= noiseSettings.persistance;
            frequency *= noiseSettings.frequencyModifier;
        }
        
        return noiseSum / amplitudeSum; // normalitza [0-1]
    }

    // Mapeja un valor d'un rang a un altre
    private float RangeMap(float inputValue, float inMin, float inMax, float outMin, float outMax)
    {
        return outMin + (inputValue - inMin) * (outMax - outMin) / (inMax - inMin);
    }

    // Tipus de biomes
    public enum BiomeType
    {
        Plains,
        Desert,
        Snow,
        Ocean
    }
}