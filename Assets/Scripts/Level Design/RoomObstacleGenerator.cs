using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections.Generic;

public class RoomObstacleGenerator : MonoBehaviour
{
    [Header("Obstacle Configuration")]
    [SerializeField] private bool includeObstacles = true;
    [SerializeField] private bool useRandomGeneration = true;
    [SerializeField] private int minObstacles = 1;
    [SerializeField] private int maxObstacles = 5;
    
    [Header("Level-Based Obstacle Tiles")]
    [Tooltip("Use different obstacle tiles per level. Index 0 = Level 1, Index 1 = Level 2, etc.")]
    [SerializeField] private bool useLevelBasedTiles = true;
    [SerializeField] private TileBase[] obstaclesTilesByLevel;
    [Tooltip("Fallback obstacle tile if level-based tiles are not configured")]
    [SerializeField] private TileBase fallbackObstacleTile;
    

    
    [Header("Fixed Room Layouts")]
    [SerializeField] private RoomLayout[] fixedLayouts;
    [SerializeField] private bool useFixedLayouts = false;
    
    [System.Serializable]
    public class RoomLayout
    {
        public string layoutName;
        public Vector2Int[] obstaclePositions; // Relative positions from room center
        [TextArea(5,10)]
        public string layoutPreview; // Visual representation for designer
    }
    
    [Header("Obstacle Size")]
    [SerializeField] private int minObstacleWidth = 1;
    [SerializeField] private int maxObstacleWidth = 3;
    [SerializeField] private int minObstacleHeight = 1;
    [SerializeField] private int maxObstacleHeight = 3;
    
    [Header("Placement Rules")]
    [SerializeField] private int borderBuffer = 2; // Tiles from room edge
    [SerializeField] private int centerClearRadius = 2; // Clear area around room center
    [SerializeField] private bool avoidExitPaths = true;
    [SerializeField] private bool generateOnStart = true;
    
    [Header("Tilemap References")]
    [SerializeField] private Tilemap collisionTilemap;
    [SerializeField] private string collisionTilemapName = "Collision TM";
    [SerializeField] private bool preserveExistingWalls = true;
    
    [Header("Breakable Blocks")]
    [SerializeField] private bool includeBreakableBlocks = true;
    [Tooltip("Optional: Assign a specific BreakableTileManager to use its tiles and settings. If null, uses local settings.")]
    [SerializeField] private BreakableTileManager customBreakableTileManager;
    [SerializeField] private int minBreakableBlocks = 2;
    [SerializeField] private int maxBreakableBlocks = 8;
    [SerializeField] private float breakableBlockChance = 0.25f; // Chance per valid position for breakable blocks
    [SerializeField] private string breakableTilemapName = "Breakable TM";
    
    [Header("Level-Based Breakable Tiles")]
    [Tooltip("Use different breakable tiles per level. Index 0 = Level 1, Index 1 = Level 2, etc.")]
    [SerializeField] private bool useLevelBasedBreakableTiles = true;
    [SerializeField] private TileBase[] breakableTilesByLevel;
    [Tooltip("Fallback breakable tile if level-based tiles are not configured")]
    [SerializeField] private TileBase fallbackBreakableTile;
    private Tilemap breakableTilemap;
    
    private Room room;
    private Grid grid;
    private DungeonGenerator dungeonGenerator;
    
    void Start()
    {
        if (generateOnStart)
        {
            GenerateObstacles();
        }
    }
    
    /// <summary>
    /// Generate random obstacles in the room
    /// </summary>
    [ContextMenu("Generate Obstacles")]
    public void GenerateObstacles()
    {
        // Quick check for Room component first
        if (room == null)
        {
            room = GetComponent<Room>();
        }
        
        if (room == null)
        {
            // ...removed debug log...
            return;
        }
        
        if (!SetupReferences())
        {
            // ...removed debug log...
            return;
        }
        
        ClearExistingObstacles();
        
        // Step 1: Place regular obstacles first
        if (includeObstacles)
        {
            // ...removed debug log...
            PlaceObstacles();
            // ...removed debug log...
        }
        else
        {
            // ...removed debug log...
        }
        
        // Step 2: Place breakable blocks in remaining valid spaces (avoiding regular obstacles)
        if (includeBreakableBlocks)
        {
            // ...removed debug log...
            PlaceRandomBreakableBlocks();
            // ...removed debug log...
        }

    }
    
    /// <summary>
    /// Clear all existing obstacles in the room
    /// </summary>
    [ContextMenu("Clear Obstacles")]
    public void ClearObstacles()
    {
        if (SetupReferences())
        {
            ClearExistingObstacles();
        }
    }
    
    /// <summary>
    /// Switch to random obstacle generation
    /// </summary>
    [ContextMenu("Use Random Generation")]
    public void UseRandomGeneration()
    {
        useRandomGeneration = true;
        useFixedLayouts = false;
        // ...removed debug log...
    }
    
    /// <summary>
    /// Switch to fixed layout patterns
    /// </summary>
    [ContextMenu("Use Fixed Layouts")]
    public void UseFixedLayouts()
    {
        useRandomGeneration = false;
        useFixedLayouts = true;
        // ...removed debug log...
    }
    
    /// <summary>
    /// Create breakable tilemap in the scene
    /// </summary>
    [ContextMenu("Create Breakable Tilemap")]
    public void CreateBreakableTilemap()
    {
        // Check if breakable tilemap already exists
        GameObject existingBreakable = GameObject.Find(breakableTilemapName);
        if (existingBreakable != null)
        {
            // ...removed debug log...
            return;
        }
        
        // Find grid
        Grid sceneGrid = FindFirstObjectByType<Grid>();
        if (sceneGrid == null)
        {
            // ...removed debug log...
            return;
        }
        
        // Create breakable tilemap GameObject
        GameObject breakableMapObj = new GameObject(breakableTilemapName);
        breakableMapObj.transform.SetParent(sceneGrid.transform);
        
        // Add components
        Tilemap tilemap = breakableMapObj.AddComponent<Tilemap>();
        TilemapRenderer renderer = breakableMapObj.AddComponent<TilemapRenderer>();
        TilemapCollider2D collider = breakableMapObj.AddComponent<TilemapCollider2D>();
        
        // Set sorting order (above collision, below walls)
        renderer.sortingOrder = 2;
        
        // Set collider as trigger so projectiles can detect breakable blocks
        collider.isTrigger = true;
        
        // Assign reference
        breakableTilemap = tilemap;
        
        // ...removed debug log...
    }
    
    /// <summary>
    /// Place breakable blocks randomly throughout the room, independent of regular obstacles
    /// </summary>
    private void PlaceRandomBreakableBlocks()
    {
        // Get tile settings from custom manager if available
        TileBase tileToUse = GetBreakableTileToUse();
        

        
        if (breakableTilemap == null || tileToUse == null)
        {
            // ...removed debug log...
            return;
        }
        
        Vector3 roomCenter = transform.position;
        Vector2Int roomSize = room.interiorSize;
        
        // Calculate usable area (excluding borders)
        int usableWidth = roomSize.x - (borderBuffer * 2);
        int usableHeight = roomSize.y - (borderBuffer * 2);
        
        if (usableWidth <= 0 || usableHeight <= 0) return;
        
        // Generate breakable blocks
        int breakableCount = Random.Range(minBreakableBlocks, maxBreakableBlocks + 1);
        int placedBreakable = 0;
        int maxAttempts = breakableCount * 15; // More attempts since we're placing single tiles
        
        for (int attempt = 0; attempt < maxAttempts && placedBreakable < breakableCount; attempt++)
        {
            // Generate random position for single tile
            int tileX = Random.Range(-usableWidth / 2, usableWidth / 2 + 1);
            int tileY = Random.Range(-usableHeight / 2, usableHeight / 2 + 1);
            
            Vector3 tileWorldPos = roomCenter + new Vector3(tileX * grid.cellSize.x, tileY * grid.cellSize.y, 0);
            Vector3Int tilePos = breakableTilemap.WorldToCell(tileWorldPos);
            
            // Check if position is valid for breakable block
            if (IsValidBreakablePosition(tilePos))
            {
                // Random chance to place breakable block at this position
                if (Random.value <= breakableBlockChance)
                {
                    PlaceBreakableBlock(tilePos, tileToUse);
                    placedBreakable++;
                }
            }
        }
        
            // ...removed debug log...
        
        // Summary message for easy visibility
        // ...removed debug log...
    }
    
    /// <summary>
    /// Check if position is valid for placing a breakable block
    /// </summary>
    /// <param name="tilePosition">Tile position to check</param>
    /// <returns>True if position is valid</returns>
    private bool IsValidBreakablePosition(Vector3Int tilePosition)
    {
        // Check if collision tilemap already has something here (wall or obstacle)
        if (collisionTilemap.GetTile(tilePosition) != null) 
        {
            // Optional: uncomment for detailed debugging
            // $"Position {tilePosition} blocked by collision tile");
            return false;
        }
        
        // Check if breakable tilemap already has something here
        if (breakableTilemap.GetTile(tilePosition) != null) 
        {
            // Optional: uncomment for detailed debugging  
            // $"Position {tilePosition} already has breakable block");
            return false;
        }
        
        return true;
    }
    
    /// <summary>
    /// <summary>
    /// Get the obstacle tile to use for the current level
    /// </summary>
    /// <returns>TileBase to use for regular obstacles</returns>
    private TileBase GetObstacleTileForCurrentLevel()
    {
        // If level-based tiles are disabled, always use fallback
        if (!useLevelBasedTiles)
        {

            return fallbackObstacleTile;
        }
        
        int currentLevel = GetCurrentLevel();
        
        // Convert to 0-based index (Level 1 = index 0)
        int levelIndex = currentLevel - 1;
        
        // Check if we have tiles configured for this level
        if (obstaclesTilesByLevel != null && levelIndex >= 0 && levelIndex < obstaclesTilesByLevel.Length)
        {
            TileBase levelTile = obstaclesTilesByLevel[levelIndex];
            if (levelTile != null)
            {
                return levelTile;
            }
        }
        
        // Fallback to default tile with better logging
        if (fallbackObstacleTile != null)
        {
            return fallbackObstacleTile;
        }
        else
        {
            return null;
        }
    }
    
    /// Get the breakable tile to use - prioritizes custom manager's tiles over local setting
    /// </summary>
    /// <returns>TileBase to use for breakable blocks</returns>
    private TileBase GetBreakableTileToUse()
    {
        // If we have a custom manager, try to get a tile from it first
        if (customBreakableTileManager != null)
        {
            TileBase[] managerTiles = customBreakableTileManager.GetBreakableTiles();
            if (managerTiles != null && managerTiles.Length > 0)
            {
                // Use first available tile from the manager
                return managerTiles[0];
            }
        }
        
        // Use level-based breakable tiles if enabled
        if (useLevelBasedBreakableTiles)
        {
            int currentLevel = GetCurrentLevel();
            int levelIndex = currentLevel - 1; // Convert to 0-based index
            
            if (breakableTilesByLevel != null && levelIndex >= 0 && levelIndex < breakableTilesByLevel.Length)
            {
                TileBase levelTile = breakableTilesByLevel[levelIndex];
                if (levelTile != null)
                {
                    return levelTile;
                }
            }
            

        }

        // Fall back to local breakable tile
        return fallbackBreakableTile;
    }
    
    /// <summary>
    /// Get the current level from DungeonGenerator
    /// </summary>
    /// <returns>Current level number (1-based)</returns>
    private int GetCurrentLevel()
    {
        // Cache DungeonGenerator reference
        if (dungeonGenerator == null)
        {
            dungeonGenerator = FindFirstObjectByType<DungeonGenerator>();
        }
        
        if (dungeonGenerator != null)
        {
            // Use the current level index from DungeonGenerator and convert to 1-based level number
            int currentLevelIndex = dungeonGenerator.GetCurrentLevelIndex();
            int levelNumber = currentLevelIndex + 1; // Convert 0-based index to 1-based level number
            
            return levelNumber;
        }
        
        // Default to level 1 if we can't get the current level
        return 1;
    }
    
    /// <summary>
    /// Place a single breakable block
    /// </summary>
    /// <param name="tilePosition">Position to place the block</param>
    /// <param name="tile">Tile to place (optional, uses level-appropriate tile if null)</param>
    private void PlaceBreakableBlock(Vector3Int tilePosition, TileBase tile = null)
    {
        // Use provided tile or fall back to level-appropriate tile
        TileBase tileToPlace = tile ?? GetBreakableTileToUse();
        
        // Place on breakable tilemap only (no collision - they're destructible)
        breakableTilemap.SetTile(tilePosition, tileToPlace);
    }
    
    /// <summary>
    /// Generate only breakable blocks for testing
    /// </summary>
    [ContextMenu("Generate Breakable Blocks Only")]
    public void GenerateBreakableBlocksOnly()
    {
        if (SetupReferences() && includeBreakableBlocks)
        {
            PlaceRandomBreakableBlocks();
        }
    }
    
    /// <summary>
    /// Debug method to check breakable block setup
    /// </summary>
    [ContextMenu("Debug Breakable Block Setup")]
    public void DebugBreakableBlockSetup()
    {
        // Try to find the tilemap in scene
        GameObject breakableObj = GameObject.Find(breakableTilemapName);
        if (breakableObj != null)
        {
            Tilemap foundTilemap = breakableObj.GetComponent<Tilemap>();
        }
        else
        {
            Debug.LogWarning($"Could not find '{breakableTilemapName}' GameObject in scene");
        }
    }
    
    private bool SetupReferences()
    {
        // Get room component
        room = GetComponent<Room>();
        if (room == null)
        {
            Debug.LogError($"RoomObstacleGenerator on {gameObject.name}: No Room component found");
            return false;
        }
        
        // Find grid
        grid = FindFirstObjectByType<Grid>();
        if (grid == null)
        {
            Debug.LogError($"RoomObstacleGenerator: No Grid found in scene");
            return false;
        }
        
        // Find collision tilemap
        if (collisionTilemap == null)
        {
            GameObject collisionMapObj = GameObject.Find(collisionTilemapName);
            if (collisionMapObj != null)
            {
                collisionTilemap = collisionMapObj.GetComponent<Tilemap>();
            }
            else
            {
                Debug.LogWarning($"RoomObstacleGenerator: {collisionTilemapName} not found. Cannot place obstacles.");
                return false;
            }
        }
        
        // Find breakable tilemap if using breakable blocks
        if (includeBreakableBlocks && breakableTilemap == null)
        {
            // First check if we have a custom BreakableTileManager assigned
            if (customBreakableTileManager != null && customBreakableTileManager.GetBreakableTilemap() != null)
            {
                breakableTilemap = customBreakableTileManager.GetBreakableTilemap();
            }
            else
            {
                // Fall back to finding by name
                GameObject breakableMapObj = GameObject.Find(breakableTilemapName);
                if (breakableMapObj != null)
                {
                    breakableTilemap = breakableMapObj.GetComponent<Tilemap>();
                }
                else
                {
                    Debug.LogWarning($"RoomObstacleGenerator: {breakableTilemapName} not found. Breakable blocks will be disabled.");
                    includeBreakableBlocks = false;
                }
            }
        }
        
        // Double check breakable tilemap assignment
        if (includeBreakableBlocks && breakableTilemap == null)
        {
            Debug.LogError($"RoomObstacleGenerator: Breakable tilemap is still null after setup attempt!");
            includeBreakableBlocks = false;
        }
        
        // Validate breakable block chance (should be 0-1, not 0-100)
        if (breakableBlockChance > 1.0f)
        {
            Debug.LogWarning($"RoomObstacleGenerator: breakableBlockChance is {breakableBlockChance}, converting from percentage to decimal");
            breakableBlockChance = breakableBlockChance / 100f;
        }
        
        // Check if we have obstacle tiles configured
        TileBase currentObstacleTile = GetObstacleTileForCurrentLevel();
        if (currentObstacleTile == null)
        {
            Debug.LogError($"RoomObstacleGenerator on {gameObject.name}: No obstacle tile available for current level {GetCurrentLevel()}");
            return false;
        }
        
        return true;
    }
    
    private void ClearExistingObstacles()
    {
        if (collisionTilemap == null) return;
        
        Vector3 roomCenter = transform.position;
        Vector2Int roomSize = room.interiorSize;
        
        // Calculate interior room bounds (not including walls)
        Vector3Int minTile = collisionTilemap.WorldToCell(roomCenter - new Vector3((roomSize.x-2) * grid.cellSize.x / 2, (roomSize.y-2) * grid.cellSize.y / 2, 0));
        Vector3Int maxTile = collisionTilemap.WorldToCell(roomCenter + new Vector3((roomSize.x-2) * grid.cellSize.x / 2, (roomSize.y-2) * grid.cellSize.y / 2, 0));
        
        // Only clear interior tiles if preserving walls, otherwise clear all
        if (preserveExistingWalls)
        {
            // Store existing wall positions before clearing
            List<Vector3Int> wallPositions = new List<Vector3Int>();
            for (int x = minTile.x - 1; x <= maxTile.x + 1; x++)
            {
                for (int y = minTile.y - 1; y <= maxTile.y + 1; y++)
                {
                    Vector3Int pos = new Vector3Int(x, y, 0);
                    if (collisionTilemap.GetTile(pos) != null)
                    {
                        // Check if this is a wall position (room border)
                        if (x <= minTile.x || x >= maxTile.x || y <= minTile.y || y >= maxTile.y)
                        {
                            wallPositions.Add(pos);
                        }
                    }
                }
            }
            
            // Clear only interior area from both tilemaps
            for (int x = minTile.x; x <= maxTile.x; x++)
            {
                for (int y = minTile.y; y <= maxTile.y; y++)
                {
                    Vector3Int pos = new Vector3Int(x, y, 0);
                    // Don't clear if it's a wall position
                    if (!wallPositions.Contains(pos))
                    {
                        collisionTilemap.SetTile(pos, null);
                        if (breakableTilemap != null)
                        {
                            breakableTilemap.SetTile(pos, null);
                        }
                    }
                }
            }
        }
        else
        {
            // Clear everything in room area from both tilemaps
            for (int x = minTile.x; x <= maxTile.x; x++)
            {
                for (int y = minTile.y; y <= maxTile.y; y++)
                {
                    Vector3Int pos = new Vector3Int(x, y, 0);
                    collisionTilemap.SetTile(pos, null);
                    if (breakableTilemap != null)
                    {
                        breakableTilemap.SetTile(pos, null);
                    }
                }
            }
        }
    }
    
    private void PlaceObstacles()
    {
        if (useFixedLayouts && fixedLayouts != null && fixedLayouts.Length > 0)
        {
            PlaceFixedLayout();
        }
        else if (useRandomGeneration)
        {
            PlaceRandomObstacles();
        }
    }
    
    private void PlaceFixedLayout()
    {
        // Choose random layout
        RoomLayout layout = fixedLayouts[Random.Range(0, fixedLayouts.Length)];
        Vector3 roomCenter = transform.position;
        TileBase tileToUse = GetObstacleTileForCurrentLevel();
        
        if (tileToUse == null)
        {
            Debug.LogWarning("RoomObstacleGenerator: No obstacle tile available for fixed layout!");
            return;
        }
        
        
        foreach (Vector2Int relativePos in layout.obstaclePositions)
        {
            Vector3 worldPos = roomCenter + new Vector3(relativePos.x * grid.cellSize.x, relativePos.y * grid.cellSize.y, 0);
            Vector3Int tilePos = collisionTilemap.WorldToCell(worldPos);
            
            // Check if position is valid (not already occupied by walls)
            if (collisionTilemap.GetTile(tilePos) == null)
            {
                // Place regular obstacle
                collisionTilemap.SetTile(tilePos, tileToUse);
            }
        }
    }
    
    private void PlaceRandomObstacles()
    {
        Vector3 roomCenter = transform.position;
        Vector2Int roomSize = room.interiorSize;
        
        // Calculate usable area (excluding borders)
        int usableWidth = roomSize.x - (borderBuffer * 2);
        int usableHeight = roomSize.y - (borderBuffer * 2);
        
        if (usableWidth <= 0 || usableHeight <= 0)
        {
            Debug.LogWarning($"RoomObstacleGenerator on {gameObject.name}: Room too small for obstacles with current border buffer");
            return;
        }
        
        // Generate obstacles
        int obstacleCount = Random.Range(minObstacles, maxObstacles + 1);
        int placedObstacles = 0;
        int maxAttempts = obstacleCount * 10; // Prevent infinite loops
        
        for (int attempt = 0; attempt < maxAttempts && placedObstacles < obstacleCount; attempt++)
        {
            // Generate random obstacle size
            int obstacleWidth = Random.Range(minObstacleWidth, maxObstacleWidth + 1);
            int obstacleHeight = Random.Range(minObstacleHeight, maxObstacleHeight + 1);
            
            // Generate random position within usable area
            int startX = Random.Range(-usableWidth / 2, usableWidth / 2 - obstacleWidth + 1);
            int startY = Random.Range(-usableHeight / 2, usableHeight / 2 - obstacleHeight + 1);
            
            Vector3 obstacleWorldPos = roomCenter + new Vector3(startX * grid.cellSize.x, startY * grid.cellSize.y, 0);
            
            // Check if position is valid
            if (IsValidObstaclePosition(obstacleWorldPos, obstacleWidth, obstacleHeight))
            {
                PlaceObstacle(obstacleWorldPos, obstacleWidth, obstacleHeight);
                placedObstacles++;
            }
        }
        
    }
    
    private bool IsValidObstaclePosition(Vector3 worldPos, int width, int height)
    {
        Vector3 roomCenter = transform.position;
        
        // Check center clear radius
        float distanceFromCenter = Vector3.Distance(worldPos, roomCenter);
        if (distanceFromCenter < centerClearRadius * grid.cellSize.x)
        {
            return false;
        }
        
        // Check if obstacle would overlap with exit paths
        if (avoidExitPaths && WouldBlockExitPath(worldPos, width, height))
        {
            return false;
        }
        
        // Check if position already has tiles (walls or obstacles)
        Vector3Int startTile = collisionTilemap.WorldToCell(worldPos);
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Vector3Int checkTile = startTile + new Vector3Int(x, y, 0);
                if (collisionTilemap.GetTile(checkTile) != null)
                {
                    return false; // Position already occupied by wall or obstacle
                }
            }
        }
        
        return true;
    }
    
    private bool WouldBlockExitPath(Vector3 worldPos, int width, int height)
    {
        Vector3 roomCenter = transform.position;
        Vector2Int roomSize = room.interiorSize;
        
        // Convert to local room coordinates
        Vector3 localPos = worldPos - roomCenter;
        float halfRoomWidth = roomSize.x * grid.cellSize.x / 2;
        float halfRoomHeight = roomSize.y * grid.cellSize.y / 2;
        
        // Check if obstacle would block main pathways
        // Horizontal pathway (for north/south exits)
        if (Mathf.Abs(localPos.y) < grid.cellSize.y && 
            localPos.x > -halfRoomWidth + grid.cellSize.x && 
            localPos.x < halfRoomWidth - grid.cellSize.x)
        {
            return true;
        }
        
        // Vertical pathway (for east/west exits)
        if (Mathf.Abs(localPos.x) < grid.cellSize.x && 
            localPos.y > -halfRoomHeight + grid.cellSize.y && 
            localPos.y < halfRoomHeight - grid.cellSize.y)
        {
            return true;
        }
        
        return false;
    }
    
    private void PlaceObstacle(Vector3 worldPos, int width, int height)
    {
        Vector3Int startTile = collisionTilemap.WorldToCell(worldPos);
        TileBase tileToUse = GetObstacleTileForCurrentLevel();
        
        if (tileToUse == null)
        {
            Debug.LogWarning("RoomObstacleGenerator: No obstacle tile available for current level!");
            return;
        }
        
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Vector3Int tilePos = startTile + new Vector3Int(x, y, 0);
                // Place regular obstacle on collision tilemap
                collisionTilemap.SetTile(tilePos, tileToUse);
            }
        }
    }
    
    /// <summary>
    /// Create predefined room layouts for common patterns
    /// </summary>
    [ContextMenu("Create Default Layouts")]
    public void CreateDefaultLayouts()
    {
        fixedLayouts = new RoomLayout[]
        {
            new RoomLayout
            {
                layoutName = "Cross Pattern",
                obstaclePositions = new Vector2Int[]
                {
                    new Vector2Int(-2, 0), new Vector2Int(2, 0),
                    new Vector2Int(0, -2), new Vector2Int(0, 2)
                },
                layoutPreview = "  #\n # # \n  #"
            },
            new RoomLayout
            {
                layoutName = "Corner Blocks",
                obstaclePositions = new Vector2Int[]
                {
                    new Vector2Int(-3, -2), new Vector2Int(-2, -2),
                    new Vector2Int(2, -2), new Vector2Int(3, -2),
                    new Vector2Int(-3, 2), new Vector2Int(-2, 2),
                    new Vector2Int(2, 2), new Vector2Int(3, 2)
                },
                layoutPreview = "## ##\n     \n## ##"
            },
            new RoomLayout
            {
                layoutName = "Center Column",
                obstaclePositions = new Vector2Int[]
                {
                    new Vector2Int(0, -2), new Vector2Int(0, -1),
                    new Vector2Int(0, 0), new Vector2Int(0, 1), new Vector2Int(0, 2)
                },
                layoutPreview = "  #\n  #\n  #\n  #\n  #"
            },
            new RoomLayout
            {
                layoutName = "L-Shapes",
                obstaclePositions = new Vector2Int[]
                {
                    new Vector2Int(-3, -2), new Vector2Int(-3, -1), new Vector2Int(-2, -1),
                    new Vector2Int(2, 1), new Vector2Int(3, 1), new Vector2Int(3, 2)
                },
                layoutPreview = "##   \n#    \n   ##\n    #"
            },
            new RoomLayout
            {
                layoutName = "Diamond Pattern",
                obstaclePositions = new Vector2Int[]
                {
                    new Vector2Int(0, -3), new Vector2Int(-1, -1), new Vector2Int(1, -1),
                    new Vector2Int(-2, 0), new Vector2Int(2, 0),
                    new Vector2Int(-1, 1), new Vector2Int(1, 1), new Vector2Int(0, 3)
                },
                layoutPreview = "  #  \n # # \n#   #\n # # \n  #  "
            },
            new RoomLayout
            {
                layoutName = "Maze Walls",
                obstaclePositions = new Vector2Int[]
                {
                    new Vector2Int(-2, -2), new Vector2Int(-2, -1), new Vector2Int(-2, 0),
                    new Vector2Int(0, -2), new Vector2Int(0, 0), new Vector2Int(0, 2),
                    new Vector2Int(2, -1), new Vector2Int(2, 0), new Vector2Int(2, 1)
                },
                layoutPreview = "# # #\n#   #\n# # #"
            },
            new RoomLayout
            {
                layoutName = "U-Shape",
                obstaclePositions = new Vector2Int[]
                {
                    new Vector2Int(-3, -2), new Vector2Int(-3, -1), new Vector2Int(-3, 0),
                    new Vector2Int(-3, 1), new Vector2Int(-2, 1), new Vector2Int(-1, 1),
                    new Vector2Int(1, 1), new Vector2Int(2, 1), new Vector2Int(3, 1),
                    new Vector2Int(3, 0), new Vector2Int(3, -1), new Vector2Int(3, -2)
                },
                layoutPreview = "#     #\n#     #\n#######"
            },
            new RoomLayout
            {
                layoutName = "Checkered",
                obstaclePositions = new Vector2Int[]
                {
                    new Vector2Int(-2, -2), new Vector2Int(0, -2), new Vector2Int(2, -2),
                    new Vector2Int(-1, 0), new Vector2Int(1, 0),
                    new Vector2Int(-2, 2), new Vector2Int(0, 2), new Vector2Int(2, 2)
                },
                layoutPreview = "# # #\n # # \n# # #"
            },
            new RoomLayout
            {
                layoutName = "Arrow Pattern",
                obstaclePositions = new Vector2Int[]
                {
                    new Vector2Int(0, 2), new Vector2Int(-1, 1), new Vector2Int(1, 1),
                    new Vector2Int(-2, 0), new Vector2Int(2, 0), new Vector2Int(0, 0),
                    new Vector2Int(0, -1), new Vector2Int(0, -2)
                },
                layoutPreview = "  #  \n # # \n# # #\n  #  \n  #  "
            },
            new RoomLayout
            {
                layoutName = "Ring Formation",
                obstaclePositions = new Vector2Int[]
                {
                    new Vector2Int(-1, 2), new Vector2Int(0, 2), new Vector2Int(1, 2),
                    new Vector2Int(-2, 1), new Vector2Int(2, 1),
                    new Vector2Int(-2, -1), new Vector2Int(2, -1),
                    new Vector2Int(-1, -2), new Vector2Int(0, -2), new Vector2Int(1, -2)
                },
                layoutPreview = " ### \n#   #\n     \n#   #\n ### "
            }
        };
    }
    
    /// <summary>
    /// Generate obstacles for all rooms in the scene
    /// </summary>
    [ContextMenu("Generate Obstacles for All Rooms")]
    public static void GenerateObstaclesForAllRooms()
    {
        RoomObstacleGenerator[] generators = FindObjectsByType<RoomObstacleGenerator>(FindObjectsSortMode.None);
        int successfulGenerations = 0;
        
        foreach (RoomObstacleGenerator generator in generators)
        {
            // Check if this generator has a valid Room component before trying to generate
            Room room = generator.GetComponent<Room>();
            if (room != null)
            {
                generator.GenerateObstacles();
                successfulGenerations++;
            }
            else
            {
                Debug.LogWarning($"RoomObstacleGenerator on {generator.gameObject.name}: No Room component found, skipping obstacle generation");
            }
        }
        
    }
    
    /// <summary>
    /// Debug all RoomObstacleGenerator components in scene
    /// </summary>
    [ContextMenu("Debug All Generators")]
    public static void DebugAllGenerators()
    {
        RoomObstacleGenerator[] generators = FindObjectsByType<RoomObstacleGenerator>(FindObjectsSortMode.None);
        
        for (int i = 0; i < generators.Length; i++)
        {
            RoomObstacleGenerator gen = generators[i];
            Room room = gen.GetComponent<Room>();
        }
    }
    
    /// <summary>
    /// Force setup breakable tilemap reference for this generator
    /// </summary>
    [ContextMenu("Force Setup Breakable Tilemap")]
    public void ForceSetupBreakableTilemap()
    {
        GameObject breakableMapObj = GameObject.Find(breakableTilemapName);
        if (breakableMapObj != null)
        {
            breakableTilemap = breakableMapObj.GetComponent<Tilemap>();
        }
        else
        {
            Debug.LogError($"Could not find GameObject with name '{breakableTilemapName}'");
        }
    }
    
    /// <summary>
    /// Debug current level and tile configuration
    /// </summary>
    [ContextMenu("Debug Level-Based Tiles")]
    public void DebugLevelBasedTiles()
    {
        int currentLevel = GetCurrentLevel();

        // Debug obstacle tiles
        if (obstaclesTilesByLevel != null)
        {
            for (int i = 0; i < obstaclesTilesByLevel.Length; i++)
            {
                int levelNum = i + 1;
                string tileName = obstaclesTilesByLevel[i]?.name ?? "NULL";
                string marker = (levelNum == currentLevel) ? " ← CURRENT" : "";
            }
        }
        
        // Debug breakable tiles
        if (breakableTilesByLevel != null)
        {
            for (int i = 0; i < breakableTilesByLevel.Length; i++)
            {
                int levelNum = i + 1;
                string tileName = breakableTilesByLevel[i]?.name ?? "NULL";
                string marker = (levelNum == currentLevel) ? " ← CURRENT" : "";
            }
        }
        
        // Show which tiles would be used now
        TileBase currentObstacleTile = GetObstacleTileForCurrentLevel();
        TileBase currentBreakableTile = GetBreakableTileToUse();
    }
    
    /// <summary>
    /// Test level-based tile selection for a specific level
    /// </summary>
    [ContextMenu("Test Level 1 Tiles")]
    public void TestLevel1Tiles()
    {
        TestTilesForLevel(1);
    }
    
    /// <summary>
    /// Test level-based tile selection for a specific level
    /// </summary>
    [ContextMenu("Test Level 2 Tiles")]
    public void TestLevel2Tiles()
    {
        TestTilesForLevel(2);
    }
    
    /// <summary>
    /// Test level-based tile selection for a specific level
    /// </summary>
    [ContextMenu("Test Level 3 Tiles")]
    public void TestLevel3Tiles()
    {
        TestTilesForLevel(3);
    }
    
    /// <summary>
    /// Test tile selection for a specific level (for debugging)
    /// </summary>
    /// <param name="level">Level number to test (1-based)</param>
    private void TestTilesForLevel(int level)
    {
        
        int levelIndex = level - 1;
        
        // Test obstacle tile
        TileBase obstacleTile = null;
        if (useLevelBasedTiles && obstaclesTilesByLevel != null && levelIndex >= 0 && levelIndex < obstaclesTilesByLevel.Length)
        {
            obstacleTile = obstaclesTilesByLevel[levelIndex];
        }
        else
        {
            obstacleTile = fallbackObstacleTile;
        }
        
        
        // Test breakable tile
        TileBase breakableTile = null;
        if (useLevelBasedBreakableTiles && breakableTilesByLevel != null && levelIndex >= 0 && levelIndex < breakableTilesByLevel.Length)
        {
            breakableTile = breakableTilesByLevel[levelIndex];
        }
        else
        {
            breakableTile = fallbackBreakableTile;
        }
        
    }
    
    /// <summary>
    /// Initialize default level-based tiles (for testing)
    /// </summary>
    [ContextMenu("Setup Default Level Tiles")]
    public void SetupDefaultLevelTiles()
    {
        // This is a helper method for setting up some default values
        // In practice, you'll want to assign your actual tile assets in the inspector

        if (obstaclesTilesByLevel == null || obstaclesTilesByLevel.Length == 0)
        {
            obstaclesTilesByLevel = new TileBase[3]; // Initialize for 3 levels

        }
        
        if (breakableTilesByLevel == null || breakableTilesByLevel.Length == 0)
        {
            breakableTilesByLevel = new TileBase[3]; // Initialize for 3 levels
        }
        
        useLevelBasedTiles = true;
        useLevelBasedBreakableTiles = true;

    }
}