using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections;
using System.Collections.Generic;

public enum RoomType
{
    Normal,
    Start,
    Boss,
    Item,
    Shop,
    Secret,
    SuperSecret
}

public class Room : MonoBehaviour
{
    [Header("Room Configuration")]
    public Vector2Int interiorSize = new Vector2Int(14, 10); // Walkable area
    public Vector2Int gridPos; // Position in dungeon grid
    public RoomType roomType = RoomType.Normal; // Type of room
    
    [Header("Global Tilemap Components")]
    [SerializeField] private Grid grid;                  // Global grid (assigned at runtime)
    [SerializeField] private Tilemap wallTilemap;        // Global wall tilemap (assigned at runtime)
    [SerializeField] private Tilemap floorTilemap;       // Global floor tilemap (assigned at runtime)
    [SerializeField] private Tilemap spawnIndicatorTilemap; // Global spawn indicator tilemap (assigned at runtime)
    
    [Header("Auto-Find Settings")]
    public bool autoFindGlobalTilemaps = true;           // Automatically find global tilemaps at runtime
    public string globalGridName = "Global_Grid";        // Name of global grid to find
    public string wallTilemapName = "Collision TM";      // Name of wall tilemap to find
    public string floorTilemapName = "Floor TM";         // Name of floor tilemap to find
    public string spawnIndicatorTilemapName = "Spawn Indicator TM"; // Name of spawn indicator tilemap to find
    
    [Header("Tile Assets")]
    public TileBase floorTile;
    public TileBase wallTile;
    public TileBase doorTile; // Tile block 2 for doors
    
    [Header("Exits")]
    public bool hasNorthExit = true;   // Default: all exits open
    public bool hasSouthExit = true;   // Default: all exits open
    public bool hasEastExit = true;    // Default: all exits open
    public bool hasWestExit = true;    // Default: all exits open
    

    
    [Header("Door State")]
    public bool doorsLocked = false;
    
    [Header("Room State")]
    public bool isCleared = false;
    public bool playerInRoom = false;
    
    /// <summary>
    /// Public property to check if this room has been completed
    /// </summary>
    public bool IsCompleted => isCleared;
    
    [Header("Enemy Spawning")]
    public GameObject[] enemyPrefabs; // List of enemy prefabs to spawn from
    [Header("Spawn Count Range")]
    public int minEnemySpawnCount = 2; // Minimum number of enemies to spawn per wave
    public int maxEnemySpawnCount = 4; // Maximum number of enemies to spawn per wave
    public float spawnDelay = 2.0f; // Delay before spawning first wave (in seconds)
    public float timeBetweenWaves = 5.0f; // Time between waves (in seconds)
    [Header("Wave Count Range")]
    public int minNumberOfWaves = 1; // Minimum number of waves
    public int maxNumberOfWaves = 2; // Maximum number of waves
    public TileBase spawnIndicatorTile; // Tile to show spawn locations
    public float spawnIndicatorDuration = 1.0f; // How long to show spawn indicators
    private bool enemiesSpawned = false; // Track if enemies have been spawned
    private int currentWave = 0; // Current wave number
    private int actualNumberOfWaves = 1; // Actual number of waves for this room (randomly determined)
    private bool allWavesCompleted = false; // Track if all waves are done
    
    [Header("Boss Room Configuration (Boss Rooms Only)")]
    [SerializeField] private GameObject[] bossPrefabs; // Array of possible boss prefabs
    [SerializeField] private bool spawnRandomBoss = true; // If true, picks random from array
    [SerializeField] private int specificBossIndex = 0; // Which boss to spawn if not random
    [SerializeField] private Transform bossSpawnPoint; // Optional specific spawn point
    [SerializeField] private bool spawnBossAtCenter = true; // Spawn at room center if no spawn point
    
    [Header("Boss Defeat Rewards")]
    [SerializeField] private GameObject bossDefeatPrefab; // Prefab to spawn when boss is defeated
    [SerializeField] private bool spawnDefeatPrefabAtCenter = true; // Spawn at room center
    [SerializeField] private Vector3 defeatPrefabOffset = Vector3.zero; // Offset from spawn position
    [SerializeField] private float bossSpawnDelay = 1.0f; // Delay before spawning boss
    [SerializeField] private GameObject[] rewardPrefabs; // Rewards to spawn on boss victory
    [SerializeField] private Transform rewardSpawnPoint; // Where to spawn rewards
    
    [Header("Boss Room State")]
    [SerializeField] private bool bossSpawned = false;
    [SerializeField] private bool bossDefeated = false;
    
    // Boss tracking
    private GameObject currentBoss;
    private Enemy currentBossEnemy;
    
    [Header("Item Room Configuration (Item Rooms Only)")]
    [SerializeField] protected GameObject[] itemPrefabs; // Array of possible items to spawn
    [SerializeField] private bool spawnItemAtCenter = true; // Spawn item at room center
    [SerializeField] private Vector3 itemSpawnOffset = Vector3.zero; // Offset from center position
    [SerializeField] private bool spawnItemOnEntry = false; // Spawn item when player enters
    [SerializeField] private bool spawnItemOnRoomClear = true; // Spawn item when room is cleared

    [Header("Item Room State")]
    [SerializeField] protected bool itemSpawned = false;
    [SerializeField] protected bool itemCollected = false;

    // Item tracking
    protected GameObject currentItem;    // Events
    public System.Action<Room> OnPlayerEntered;
    public System.Action<Room> OnPlayerExited;
    public System.Action<Room> OnRoomCleared;
    
    // Room boundaries (including walls)
    public Vector2Int TotalSize => new Vector2Int(interiorSize.x + 2, interiorSize.y + 2); // 16x12
    
    // Collider for player detection
    private BoxCollider2D roomTrigger;
    
    // Enemies in this room
    private List<Enemy> enemiesInRoom = new List<Enemy>();
    
    protected virtual void Awake()
    {
        // Auto-find global tilemaps if enabled and not assigned
        if (autoFindGlobalTilemaps)
        {
            FindGlobalTilemaps();
        }
        
        SetupTilemapComponents();
        SetupRoomTrigger();
    }
    
    protected virtual void Start()
    {
        // Generate the room layout with current exit configuration
        GenerateRoomTiles();
        
        // Update variant info based on current exits
        UpdateRoomVariantInfo();
        
        // Find all enemies in this room
        FindEnemiesInRoom();
        
        // Subscribe to enemy death events
        foreach (Enemy enemy in enemiesInRoom)
        {
            if (enemy != null)
            {
                enemy.OnDeath += OnEnemyDeath;
            }
        }
        
        // Boss room specific initialization
        if (roomType == RoomType.Boss)
        {
            // Lock doors initially for boss rooms
            doorsLocked = true;
            UpdateExitTiles();
        }
        
        // Item room specific initialization
        if (roomType == RoomType.Item)
        {
            // Spawn item on entry if configured
            if (spawnItemOnEntry)
            {
                SpawnItem();
            }
        }
    }
    
    // Method for manual room generation (testing purposes)
    [ContextMenu("Generate Room")]
    public void GenerateRoomManually()
    {
        SetupTilemapComponents();
        GenerateRoomTiles();
    }
    
    // Method to regenerate room with new settings
    [ContextMenu("Regenerate Room")]
    public void RegenerateRoom()
    {
        // Clear existing tiles first
        ClearRoomTiles();
        
        // Generate new room
        GenerateRoomManually();
    }
    
    // Validate room configuration
    [ContextMenu("Validate Room Setup")]
    public void ValidateRoomSetup()
    {
        bool isValid = true;
        
        if (floorTile == null)
        {
            Debug.LogError($"Room {gameObject.name}: Floor tile is not assigned!");
            isValid = false;
        }
        
        if (wallTile == null)
        {
            Debug.LogError($"Room {gameObject.name}: Wall tile is not assigned!");
            isValid = false;
        }
        
        if (doorTile == null)
        {
            Debug.LogWarning($"Room {gameObject.name}: Door tile is not assigned - doors won't work properly!");
        }
        
        if (interiorSize.x <= 0 || interiorSize.y <= 0)
        {
            Debug.LogError($"Room {gameObject.name}: Interior size must be positive values!");
            isValid = false;
        }
        
        // Report validation result
        if (isValid)
        {
            Debug.Log($"Room {gameObject.name}: Configuration is valid!");
        }
        else
        {
            Debug.LogError($"Room {gameObject.name}: Configuration has errors that need to be fixed!");
        }
        
        // Update and display variant info
        UpdateRoomVariantInfo();
    }
    
    // Test door functionality
    [ContextMenu("Test Lock Doors")]
    public void TestLockDoors()
    {
        if (wallTilemap == null || floorTilemap == null)
        {
            Debug.LogError("Cannot test doors - tilemaps not initialized!");
            return;
        }
        
        LockExits();
    }
    
    [ContextMenu("Test Unlock Doors")]  
    public void TestUnlockDoors()
    {
        if (wallTilemap == null || floorTilemap == null)
        {
            Debug.LogError("Cannot test doors - tilemap not initialized!");
            return;
        }
        
        UnlockExits();
    }
    
    // Debug room tile positions
    [ContextMenu("Show Room Tile Info")]
    public void ShowRoomTileInfo()
    {
        Vector3 worldPos = transform.position;
        Vector3Int offset = GetRoomTileOffset();
        Vector2Int totalSize = TotalSize;
        
        // Room tile information calculated for debugging
        if (grid != null)
        {
            Vector3Int gridPos = grid.WorldToCell(worldPos);
        }
    }
    
    // Toggle all exits for testing
    [ContextMenu("Toggle All Exits")]
    public void ToggleAllExits()
    {
        hasNorthExit = !hasNorthExit;
        hasSouthExit = !hasSouthExit;
        hasEastExit = !hasEastExit;
        hasWestExit = !hasWestExit;
        
        UpdateExitTiles();
        
        // Update variant info after toggling
        UpdateRoomVariantInfo();
    }
    
    // Generate room variant name based on exit configuration
    [ContextMenu("Update Room Variant Info")]
    public void UpdateRoomVariantInfo()
    {
        // Method kept for backward compatibility but no longer stores cached values
        // All values are now calculated on-demand
    }
    
    // Check if this room matches a specific exit pattern
    public bool MatchesExitPattern(bool north, bool south, bool east, bool west)
    {
        return hasNorthExit == north && 
               hasSouthExit == south && 
               hasEastExit == east && 
               hasWestExit == west;
    }
    
    // Get room variant as a readable string
    public string GetRoomVariant()
    {
        List<string> exits = new List<string>();
        if (hasNorthExit) exits.Add("N");
        if (hasSouthExit) exits.Add("S");
        if (hasEastExit) exits.Add("E");
        if (hasWestExit) exits.Add("W");
        
        return exits.Count > 0 ? string.Join("", exits) : "NoExits";
    }
    
    // Get exit count
    public int GetExitCount()
    {
        int count = 0;
        if (hasNorthExit) count++;
        if (hasSouthExit) count++;
        if (hasEastExit) count++;
        if (hasWestExit) count++;
        return count;
    }
    
    // Method for dungeon generator to configure exits dynamically
    public void ConfigureExits(bool north, bool south, bool east, bool west)
    {
        hasNorthExit = north;
        hasSouthExit = south;
        hasEastExit = east;
        hasWestExit = west;
        
        // Update tiles to reflect new exit configuration
        if (wallTilemap != null && floorTilemap != null)
        {
            UpdateExitTiles();
        }
        
        // Update variant info
        UpdateRoomVariantInfo();
    }
    
    // Reset all exits to open (for prefab default state)
    [ContextMenu("Reset All Exits Open")]
    public void ResetAllExitsOpen()
    {
        ConfigureExits(true, true, true, true);
    }
    
    // Manually assign global tilemaps (for testing in editor)
    [ContextMenu("Find Global Tilemaps")]
    public void FindGlobalTilemapsManually()
    {
        FindGlobalTilemaps();
        
        // Check if all tilemaps were found successfully
    }
    
    // Clear room tiles from global tilemaps for testing
    [ContextMenu("Clear Room Tiles")]
    public void ClearRoomTiles()
    {
        if (wallTilemap == null || floorTilemap == null)
        {
            Debug.LogError("Cannot clear tiles - tilemaps not assigned!");
            return;
        }
        
        Vector2Int totalSize = TotalSize;
        Vector3Int offset = GetRoomTileOffset();
        
        // Clear tiles from both tilemaps in the room's area
        BoundsInt bounds = new BoundsInt(offset.x, offset.y, 0, totalSize.x, totalSize.y, 1);
        TileBase[] emptyTiles = new TileBase[totalSize.x * totalSize.y];
        
        wallTilemap.SetTilesBlock(bounds, emptyTiles);
        floorTilemap.SetTilesBlock(bounds, emptyTiles);
    }
    
    // Automatically find global tilemaps in the scene
    private void FindGlobalTilemaps()
    {
        // Find global grid by name
        if (grid == null)
        {
            GameObject gridObj = GameObject.Find(globalGridName);
            if (gridObj != null)
            {
                grid = gridObj.GetComponent<Grid>();
            }
        }
        
        // Find wall tilemap by name
        if (wallTilemap == null)
        {
            GameObject wallObj = GameObject.Find(wallTilemapName);
            if (wallObj != null)
            {
                wallTilemap = wallObj.GetComponent<Tilemap>();
            }
        }
        
        // Find floor tilemap by name
        if (floorTilemap == null)
        {
            GameObject floorObj = GameObject.Find(floorTilemapName);
            if (floorObj != null)
            {
                floorTilemap = floorObj.GetComponent<Tilemap>();
            }
        }
        
        // Find spawn indicator tilemap by name
        if (spawnIndicatorTilemap == null)
        {
            GameObject spawnIndicatorObj = GameObject.Find(spawnIndicatorTilemapName);
            if (spawnIndicatorObj != null)
            {
                spawnIndicatorTilemap = spawnIndicatorObj.GetComponent<Tilemap>();
            }
        }
        
        // If auto-find failed, show helpful message
        if (grid == null || wallTilemap == null || floorTilemap == null)
        {
            Debug.LogWarning($"Room {gameObject.name}: Could not auto-find all global tilemaps. Check names: Grid='{globalGridName}', Wall='{wallTilemapName}', Floor='{floorTilemapName}', SpawnIndicator='{spawnIndicatorTilemapName}'");
        }
        
        // Spawn indicator tilemap is optional, so just log info if missing
        if (spawnIndicatorTilemap == null)
        {
            Debug.Log($"Room {gameObject.name}: Spawn indicator tilemap not found ('{spawnIndicatorTilemapName}') - spawn indicators will be disabled.");
        }
    }
    
    private void SetupTilemapComponents()
    {
        // Validate externally assigned components - do not create any
        if (grid == null)
        {
            Debug.LogWarning($"Room {gameObject.name}: No Grid assigned! Please assign the global Grid component.");
            return;
        }
        
        if (wallTilemap == null)
        {
            Debug.LogWarning($"Room {gameObject.name}: No Wall Tilemap assigned! Please assign the global wall tilemap.");
            return;
        }
        
        if (floorTilemap == null)
        {
            Debug.LogWarning($"Room {gameObject.name}: No Floor Tilemap assigned! Please assign the global floor tilemap.");
            return;
        }
        
        // Spawn indicator tilemap is optional
        if (spawnIndicatorTilemap == null)
        {
            Debug.Log($"Room {gameObject.name}: No Spawn Indicator Tilemap assigned - spawn indicators will be disabled.");
        }
        
        // Global tilemap components validated successfully
    }
    
    // Validate assigned grid settings
    [ContextMenu("Validate Grid Settings")]
    public void ValidateGridSettings()
    {
        if (grid == null)
        {
            Debug.LogError($"Room {gameObject.name}: No Grid assigned! Please assign a global Grid component.");
            return;
        }
        
        // Check if grid has the expected settings for the dungeon system
        Vector3 expectedCellSize = new Vector3(0.4f, 0.4f, 0f);
        if (grid.cellSize != expectedCellSize)
        {
            Debug.LogWarning($"Room {gameObject.name}: Grid cell size is {grid.cellSize}, expected {expectedCellSize}");
        }
        
        // Grid validation complete
    }
    
    // Complete prefab setup - call this before creating prefab
    [ContextMenu("Setup Complete Prefab")]
    public void SetupCompletePrefab()
    {
        // Validate assigned grid first
        if (grid == null)
        {
            Debug.LogError($"Room {gameObject.name}: No Grid assigned! Please assign a global Grid component before setup.");
            return;
        }
        
        // Validate grid settings
        ValidateGridSettings();
        
        // Setup all tilemap components
        SetupTilemapComponents();
        
        // Ensure all components are properly initialized
    }
    

    
    private void SetupRoomTrigger()
    {
        // Create trigger collider for player detection (separate from tilemap collision)
        roomTrigger = gameObject.GetComponent<BoxCollider2D>();
        if (roomTrigger == null)
        {
            roomTrigger = gameObject.AddComponent<BoxCollider2D>();
        }
        
        roomTrigger.isTrigger = true;
        roomTrigger.size = new Vector2(5f, 3.5f);  // Fixed size as requested
        roomTrigger.offset = new Vector2(0f, 0f);  // Fixed offset as requested
        
        Debug.Log($"Room '{gameObject.name}' trigger setup: Size({roomTrigger.size}), Offset({roomTrigger.offset})");
    }
    
    private void FindEnemiesInRoom()
    {
        // Find all enemies that are children of this room
        Enemy[] enemies = GetComponentsInChildren<Enemy>();
        enemiesInRoom.AddRange(enemies);
    }
    
    // Enemy spawning methods
    public void SpawnEnemies()
    {
        if (enemyPrefabs == null || enemyPrefabs.Length == 0)
        {
            Debug.LogWarning($"Room {gameObject.name}: No enemy prefabs assigned!");
            return;
        }
        
        if (maxEnemySpawnCount <= 0)
        {
            Debug.Log($"Room {gameObject.name}: Max enemy spawn count is 0, skipping enemy spawn.");
            return;
        }
        
        // Reset wave tracking and determine actual number of waves
        currentWave = 0;
        actualNumberOfWaves = Random.Range(minNumberOfWaves, maxNumberOfWaves + 1);
        allWavesCompleted = false;
        
        Debug.Log($"Room {gameObject.name}: Will spawn {actualNumberOfWaves} waves (range: {minNumberOfWaves}-{maxNumberOfWaves})");
        
        StartCoroutine(SpawnWaveSystem());
    }
    
    private IEnumerator SpawnWaveSystem()
    {
        // Start with wave 1
        currentWave = 1;
        yield return StartCoroutine(SpawnWave(1));
        
        // Note: Subsequent waves will be triggered by CheckRoomClearCondition()
        // when previous wave enemies are defeated
    }
    
    private IEnumerator SpawnWave(int waveNumber)
    {
        Debug.Log($"Room {gameObject.name}: Starting wave {waveNumber}/{actualNumberOfWaves}");
        
        // Wait for initial spawn delay (first wave) or time between waves
        float delayTime = (waveNumber == 1) ? spawnDelay : timeBetweenWaves;
        yield return new WaitForSeconds(delayTime);
        
        Vector3 roomCenter = transform.position;
        
        // Generate random enemy count within the specified range
        int actualSpawnCount = Random.Range(minEnemySpawnCount, maxEnemySpawnCount + 1);
        Debug.Log($"Room {gameObject.name}: Wave {waveNumber} will spawn {actualSpawnCount} enemies (range: {minEnemySpawnCount}-{maxEnemySpawnCount})");
        
        List<Vector3> spawnPositions = GenerateSpawnPositions(actualSpawnCount, roomCenter);
        
        if (spawnPositions.Count == 0)
        {
            Debug.LogWarning($"Room {gameObject.name}: No valid spawn positions found for wave {waveNumber}");
            
            // If this was the final wave, mark all waves completed
            if (waveNumber >= actualNumberOfWaves)
            {
                allWavesCompleted = true;
                Debug.Log($"Room {gameObject.name}: All {actualNumberOfWaves} waves completed (no enemies spawned in final wave)");
            }
            yield break;
        }
        
        // Show spawn indicators for all enemies in this wave
        List<Vector3Int> indicatorTilePositions = ShowSpawnIndicators(spawnPositions);
        
        // Wait for indicator duration
        yield return new WaitForSeconds(spawnIndicatorDuration);
        
        // Remove spawn indicators
        RemoveSpawnIndicators(indicatorTilePositions);
        
        // Spawn all enemies in this wave simultaneously (no delay between spawns)
        foreach (Vector3 spawnPos in spawnPositions)
        {
            SpawnEnemyAtPosition(spawnPos, waveNumber);
        }
        
        Debug.Log($"Room {gameObject.name}: Wave {waveNumber} spawned {spawnPositions.Count} enemies simultaneously");
        
        // If this was the final wave, mark all waves as completed
        if (waveNumber >= actualNumberOfWaves)
        {
            allWavesCompleted = true;
            Debug.Log($"Room {gameObject.name}: All {actualNumberOfWaves} waves completed");
        }
    }
    
    private List<Vector3> GenerateSpawnPositions(int count, Vector3 roomCenter)
    {
        List<Vector3> positions = new List<Vector3>();
        
        // Calculate spawn area boundaries in tile coordinates (interior of the room)
        int halfTilesWidth = (interiorSize.x - 4) / 2; // Leave 2 tile border
        int halfTilesHeight = (interiorSize.y - 4) / 2; // Leave 2 tile border
        
        // Use room center as player spawn position (where player enters the room)
        Vector3 playerSpawnPos = roomCenter;
        
        int attempts = 0;
        int maxAttempts = count * 20; // Prevent infinite loops
        
        while (positions.Count < count && attempts < maxAttempts)
        {
            attempts++;
            
            // Generate random tile position within room bounds
            int tileX = Random.Range(-halfTilesWidth, halfTilesWidth + 1);
            int tileY = Random.Range(-halfTilesHeight, halfTilesHeight + 1);
            
            // Convert tile position to world position (centered on tile)
            Vector3 spawnPos = GetTileCenterWorldPosition(roomCenter, tileX, tileY);
            
            // Check if position is valid
            if (IsValidSpawnPosition(spawnPos, playerSpawnPos, positions))
            {
                positions.Add(spawnPos);
            }
        }
        
        if (positions.Count < count)
        {
            Debug.LogWarning($"Room {gameObject.name}: Could only find {positions.Count} valid spawn positions out of {count} requested after {attempts} attempts. This may be due to obstacles, breakable blocks, or insufficient space.");
        }
        else
        {
            Debug.Log($"Room {gameObject.name}: Successfully generated {positions.Count} spawn positions, avoiding obstacles and breakable blocks.");
        }
        
        return positions;
    }
    
    private Vector3 GetTileCenterWorldPosition(Vector3 roomCenter, int tileOffsetX, int tileOffsetY)
    {
        if (grid != null)
        {
            // Calculate world position offset based on tile coordinates and grid cell size
            float worldOffsetX = tileOffsetX * grid.cellSize.x;
            float worldOffsetY = tileOffsetY * grid.cellSize.y;
            
            // Add half cell size to center on the tile
            worldOffsetX += grid.cellSize.x * 0.5f;
            worldOffsetY += grid.cellSize.y * 0.5f;
            
            return new Vector3(
                roomCenter.x + worldOffsetX,
                roomCenter.y + worldOffsetY,
                roomCenter.z
            );
        }
        else
        {
            // Fallback if no grid (assume 0.4 cell size based on your setup)
            float cellSize = 0.4f;
            return new Vector3(
                roomCenter.x + (tileOffsetX + 0.5f) * cellSize,
                roomCenter.y + (tileOffsetY + 0.5f) * cellSize,
                roomCenter.z
            );
        }
    }
    
    private bool IsValidSpawnPosition(Vector3 position, Vector3 playerSpawnPos, List<Vector3> existingPositions)
    {
        // Check distance from player spawn position (at least 1 tile away)
        float minPlayerDistance = grid != null ? grid.cellSize.x * 1f : 0.4f; // At least 1 tile away
        if (Vector3.Distance(position, playerSpawnPos) < minPlayerDistance)
        {
            return false;
        }
        
        // Check distance from other spawn positions (at least 1 tile apart)
        float minSpawnDistance = grid != null ? grid.cellSize.x : 0.4f; // At least 1 tile apart
        foreach (Vector3 existingPos in existingPositions)
        {
            if (Vector3.Distance(position, existingPos) < minSpawnDistance)
            {
                return false;
            }
        }
        
        // Check if position is on a walkable tile (not on walls)
        if (IsPositionOnWall(position))
        {
            return false;
        }
        
        // Check if position is on a floor tile (ensure there's actually a floor there)
        if (!IsPositionOnFloor(position))
        {
            return false;
        }
        
        // Check if position has obstacles (collision tilemap)
        if (IsPositionOnObstacle(position))
        {
            // Debug.Log($"Room: Spawn position {position} rejected - obstacle found");
            return false;
        }
        
        // Check if position has breakable blocks
        if (IsPositionOnBreakableBlock(position))
        {
            // Debug.Log($"Room: Spawn position {position} rejected - breakable block found");
            return false;
        }
        
        return true;
    }
    
    private bool IsPositionOnWall(Vector3 worldPosition)
    {
        if (wallTilemap == null || grid == null) return false;
        
        // Convert world position to tile position
        Vector3Int tilePos = grid.WorldToCell(worldPosition);
        
        // Check if there's a wall tile at this position
        TileBase tileAtPosition = wallTilemap.GetTile(tilePos);
        return tileAtPosition != null;
    }
    
    private bool IsPositionOnFloor(Vector3 worldPosition)
    {
        if (floorTilemap == null || grid == null) return true; // Assume valid if can't check
        
        // Convert world position to tile position
        Vector3Int tilePos = grid.WorldToCell(worldPosition);
        
        // Check if there's a floor tile at this position
        TileBase tileAtPosition = floorTilemap.GetTile(tilePos);
        return tileAtPosition != null;
    }
    
    private bool IsPositionOnObstacle(Vector3 worldPosition)
    {
        if (grid == null) return false;
        
        // Convert world position to tile position
        Vector3Int tilePos = grid.WorldToCell(worldPosition);
        
        // Find collision tilemap in scene
        GameObject collisionTilemapObj = GameObject.Find("Collision TM");
        if (collisionTilemapObj != null)
        {
            Tilemap collisionTilemap = collisionTilemapObj.GetComponent<Tilemap>();
            if (collisionTilemap != null)
            {
                TileBase tileAtPosition = collisionTilemap.GetTile(tilePos);
                return tileAtPosition != null; // True if there's an obstacle at this position
            }
        }
        
        return false; // No obstacles found
    }
    
    private bool IsPositionOnBreakableBlock(Vector3 worldPosition)
    {
        if (grid == null) return false;
        
        // Convert world position to tile position
        Vector3Int tilePos = grid.WorldToCell(worldPosition);
        
        // Find breakable tilemap in scene
        GameObject breakableTilemapObj = GameObject.Find("Breakable TM");
        if (breakableTilemapObj != null)
        {
            Tilemap breakableTilemap = breakableTilemapObj.GetComponent<Tilemap>();
            if (breakableTilemap != null)
            {
                TileBase tileAtPosition = breakableTilemap.GetTile(tilePos);
                return tileAtPosition != null; // True if there's a breakable block at this position
            }
        }
        
        return false; // No breakable blocks found
    }
    
    private void SpawnEnemyAtPosition(Vector3 position, int waveOrIndex)
    {
        // Choose random enemy prefab
        GameObject enemyPrefab = enemyPrefabs[Random.Range(0, enemyPrefabs.Length)];
        
        // Instantiate enemy
        GameObject enemyObj = Instantiate(enemyPrefab, position, Quaternion.identity, transform);
        enemyObj.name = $"Enemy_W{currentWave}_{waveOrIndex}_{enemyPrefab.name}";
        
        // Setup proper collision detection
        SetupEnemyCollision(enemyObj);
        
        // Get enemy component and subscribe to death event
        Enemy enemy = enemyObj.GetComponent<Enemy>();
        if (enemy != null)
        {
            enemiesInRoom.Add(enemy);
            enemy.OnDeath += OnEnemyDeath;
            
            // Add spawn protection to prevent immediate contact damage
            StartCoroutine(EnemySpawnProtection(enemy));
        }
        else
        {
            Debug.LogWarning($"Spawned enemy {enemyObj.name} doesn't have Enemy component!");
        }
    }
    
    [ContextMenu("Test Spawn Enemies")]
    public void TestSpawnEnemies()
    {
        SpawnEnemies();
    }
    
    [ContextMenu("Test Spawn Indicators")]
    public void TestSpawnIndicators()
    {
        if (spawnIndicatorTile == null)
        {
            Debug.LogError($"Room {gameObject.name}: No spawn indicator tile assigned!");
            return;
        }
        
        if (spawnIndicatorTilemap == null)
        {
            Debug.LogError($"Room {gameObject.name}: No spawn indicator tilemap found! Check tilemap setup.");
            return;
        }
        
        Vector3 roomCenter = transform.position;
        
        // Use average of range for testing
        int testSpawnCount = (minEnemySpawnCount + maxEnemySpawnCount) / 2;
        List<Vector3> spawnPositions = GenerateSpawnPositions(testSpawnCount, roomCenter);
        List<Vector3Int> indicatorPositions = ShowSpawnIndicators(spawnPositions);
        
        Debug.Log($"Test spawning {indicatorPositions.Count} indicators at positions: {string.Join(", ", indicatorPositions)}");
        
        // Remove indicators after the duration
        StartCoroutine(RemoveIndicatorsAfterDelay(indicatorPositions));
    }
    
    [ContextMenu("Test Spawn Item")]
    public void TestSpawnItem()
    {
        ForceSpawnItem();
    }
    
    private IEnumerator RemoveIndicatorsAfterDelay(List<Vector3Int> indicatorPositions)
    {
        yield return new WaitForSeconds(spawnIndicatorDuration);
        RemoveSpawnIndicators(indicatorPositions);
    }
    
    [ContextMenu("Clear All Enemies")]
    public void ClearAllEnemies()
    {
        // Destroy all existing enemies
        foreach (Enemy enemy in enemiesInRoom)
        {
            if (enemy != null)
            {
                enemy.OnDeath -= OnEnemyDeath;
                DestroyImmediate(enemy.gameObject);
            }
        }
        enemiesInRoom.Clear();
        
        Debug.Log($"Room {gameObject.name}: All enemies cleared");
    }
    
    [ContextMenu("Debug Room Info")]
    public void DebugRoomInfo()
    {
        Debug.Log($"Room {gameObject.name}: Type={roomType}, GridPos={gridPos}, ShouldSkipLocking={ShouldSkipExitLocking()}, IsCleared={isCleared}");
    }
    
    public int GetEnemyCount()
    {
        return enemiesInRoom.Count;
    }
    
    public bool HasEnemies()
    {
        return enemiesInRoom.Count > 0;
    }
    
    public virtual void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            EnterRoom();
        }
    }
    
    public virtual void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            ExitRoom();
        }
    }
    
    public virtual void EnterRoom()
    {
        if (playerInRoom) return;
        
        playerInRoom = true;
        
        // Debug log to show room status when entered
        if (isCleared)
        {
            Debug.Log($"Room {gameObject.name}: Player entered COMPLETED room - no locking or spawning will occur");
        }
        else
        {
            Debug.Log($"Room {gameObject.name}: Player entered room - Type: {roomType}, Will lock: {!ShouldSkipExitLocking()}, Will spawn: {ShouldSpawnEnemies()}");
        }
        
        // Lock exits if room is not cleared, unless it's a starting room or item room
        bool shouldLockExits = !isCleared && !ShouldSkipExitLocking();
        Debug.Log($"Room {gameObject.name}: Door locking check - IsCleared: {isCleared}, ShouldSkipLocking: {ShouldSkipExitLocking()}, WillLock: {shouldLockExits}");
        
        if (shouldLockExits)
        {
            LockExits();
        }
        
        // Handle boss room logic first
        if (roomType == RoomType.Boss && !bossSpawned && !bossDefeated)
        {
            StartCoroutine(SpawnBossAfterDelay());
        }
        // Spawn enemies if it's a room that should have enemies, not already spawned, and not already cleared
        else if (ShouldSpawnEnemies() && !enemiesSpawned && !isCleared && enemyPrefabs != null && enemyPrefabs.Length > 0)
        {
            enemiesSpawned = true; // Mark as spawned to prevent multiple spawns
            SpawnEnemies(); // Use the new wave system
        }
        else if (ShouldSpawnEnemies() && isCleared)
        {
            Debug.Log($"Room {gameObject.name}: Skipping enemy spawn - room already completed");
        }
        // If room shouldn't spawn enemies, check if it should be cleared immediately
        else if (!ShouldSpawnEnemies() && !isCleared)
        {
            CheckRoomClearCondition();
        }
        
        // Notify systems that player entered
        OnPlayerEntered?.Invoke(this);
    }
    
    // Check if this room type should skip exit locking
    protected virtual bool ShouldSkipExitLocking()
    {
        // Starting rooms and item rooms should not lock their exits
        // Boss rooms SHOULD lock their exits (so return false for boss rooms)
        bool skipLocking = roomType == RoomType.Start || roomType == RoomType.Item;
        
        Debug.Log($"Room {gameObject.name}: ShouldSkipExitLocking - RoomType: {roomType}, IsBossRoom: {roomType == RoomType.Boss}, SkipLocking: {skipLocking}");
        
        return skipLocking;
    }
    
    // Check if this room type should spawn enemies
    protected virtual bool ShouldSpawnEnemies()
    {
        // Only normal rooms should spawn enemies via wave system
        // Boss rooms will be handled by separate boss scripts
        // Starting rooms, item rooms, shop rooms, and secret rooms should not spawn enemies
        return roomType == RoomType.Normal;
    }
    
    public virtual void ExitRoom()
    {
        if (!playerInRoom) return;
        
        playerInRoom = false;
        
        // Notify systems that player exited
        OnPlayerExited?.Invoke(this);
    }
    
    public virtual void MarkCleared()
    {
        if (isCleared) return;
        
        isCleared = true;
        UnlockExits();
        
        Debug.Log($"Room {gameObject.name}: Room cleared! Doors unlocked.");
        
        // Item room: spawn item if configured to do so (but only if no ItemRoom component exists)
        if (roomType == RoomType.Item && spawnItemOnRoomClear && !itemSpawned)
        {
            // Check if there's an ItemRoom component that should handle item spawning instead
            ItemRoom itemRoomComponent = GetComponent<ItemRoom>();
            if (itemRoomComponent == null)
            {
                SpawnItem();
            }
            else
            {
                Debug.Log($"Room {gameObject.name}: ItemRoom component will handle item spawning");
            }
        }
        
        // Notify systems that room is cleared
        OnRoomCleared?.Invoke(this);
    }
    
    /// <summary>
    /// Manually mark room as completed (useful for testing or special cases)
    /// </summary>
    [ContextMenu("Mark Room Completed")]
    public void MarkRoomCompleted()
    {
        MarkCleared();
    }
    
    /// <summary>
    /// Reset room to incomplete state (useful for testing)
    /// </summary>
    [ContextMenu("Reset Room Completion")]
    public void ResetRoomCompletion()
    {
        isCleared = false;
        enemiesSpawned = false;
        currentWave = 0;
        allWavesCompleted = false;
        enemiesInRoom.Clear();
        
        Debug.Log($"Room {gameObject.name}: Room completion reset - can spawn enemies again");
    }
    
    /// <summary>
    /// Manually check if room should be cleared (useful when enemies are manually deleted)
    /// </summary>
    [ContextMenu("Force Check Room Clear")]
    public void ForceCheckRoomClear()
    {
        Debug.Log($"Room {gameObject.name}: Manual room clear check triggered");
        CheckRoomClearCondition();
    }
    
    /// <summary>
    /// Force all waves to be marked as completed (useful for testing)
    /// </summary>
    [ContextMenu("Force Complete All Waves")]
    public void ForceCompleteAllWaves()
    {
        allWavesCompleted = true;
        currentWave = actualNumberOfWaves;
        Debug.Log($"Room {gameObject.name}: All waves force-completed, checking room clear condition");
        CheckRoomClearCondition();
    }
    
    /// <summary>
    /// Kill all enemies in the room (useful for testing wave progression and room clearing)
    /// </summary>
    [ContextMenu("Kill All Enemies")]
    public void KillAllEnemies()
    {
        int enemiesKilled = 0;
        
        // Create a copy of the list to avoid modification during iteration
        List<Enemy> enemiesToKill = new List<Enemy>(enemiesInRoom);
        
        foreach (Enemy enemy in enemiesToKill)
        {
            if (enemy != null)
            {
                // Destroy the enemy GameObject
                DestroyImmediate(enemy.gameObject);
                enemiesKilled++;
            }
        }
        
        // Clear the list of null references
        enemiesInRoom.RemoveAll(enemy => enemy == null);
        
        Debug.Log($"Room {gameObject.name}: Killed {enemiesKilled} enemies. Enemies remaining: {enemiesInRoom.Count}");
        
        // Check if room should progress to next wave or be cleared
        CheckRoomClearCondition();
    }
    
    public virtual void LockExits()
    {
        doorsLocked = true;
        
        // Place door tiles (block 2) at exit positions
        if (hasNorthExit) SetExitTile("north", true);
        if (hasSouthExit) SetExitTile("south", true);
        if (hasEastExit) SetExitTile("east", true);
        if (hasWestExit) SetExitTile("west", true);
    }
    
    public virtual void UnlockExits()
    {
        if (!doorsLocked) return; // Already unlocked
        
        doorsLocked = false;
        
        Debug.Log($"Room {gameObject.name}: Unlocking doors - removing door tiles from exits");
        
        // Remove door tiles (place floor tiles) at exit positions
        if (hasNorthExit) SetExitTile("north", false);
        if (hasSouthExit) SetExitTile("south", false);
        if (hasEastExit) SetExitTile("east", false);
        if (hasWestExit) SetExitTile("west", false);
    }
    
    public Vector3 GetCenter()
    {
        // Return the center of the interior walkable area in world coordinates
        if (grid != null)
        {
            Vector3 tileCenter = new Vector3(TotalSize.x / 2f, TotalSize.y / 2f, 0);
            return transform.position + grid.CellToWorld(Vector3Int.FloorToInt(tileCenter));
        }
        return transform.position;
    }
    
    public Vector3 GetWorldPosition()
    {
        return transform.position;
    }
    
    private void OnEnemyDeath(Enemy enemy)
    {
        // Remove enemy from list
        enemiesInRoom.Remove(enemy);
        
        // Check if all enemies are defeated
        CheckRoomClearCondition();
    }
    
    protected virtual void CheckRoomClearCondition()
    {
        // Clean up any null references from manually deleted enemies
        enemiesInRoom.RemoveAll(enemy => enemy == null);
        
        Debug.Log($"Room {gameObject.name}: CheckRoomClearCondition - Enemies remaining: {enemiesInRoom.Count}, Current wave: {currentWave}/{actualNumberOfWaves}, All waves completed: {allWavesCompleted}, Is cleared: {isCleared}");
        
        // Room is cleared when all enemies are defeated and all waves are completed
        if (enemiesInRoom.Count == 0 && !isCleared)
        {
            // For rooms that don't spawn enemies (start rooms, item rooms), clear immediately
            if (!ShouldSpawnEnemies())
            {
                Debug.Log($"Room {gameObject.name}: No enemies to spawn - room cleared immediately!");
                MarkCleared();
            }
            // Check if there are more waves to spawn
            else if (currentWave < actualNumberOfWaves && !allWavesCompleted)
            {
                Debug.Log($"Room {gameObject.name}: Wave {currentWave} cleared! Spawning wave {currentWave + 1}...");
                currentWave++;
                StartCoroutine(SpawnWave(currentWave));
            }
            // For rooms with enemies, check if all waves are completed
            else if (allWavesCompleted)
            {
                Debug.Log($"Room {gameObject.name}: All waves completed and all enemies defeated - room cleared!");
                MarkCleared();
            }
        }
    }
    
    // Helper methods for dungeon generation
    public void SetExits(bool north, bool south, bool east, bool west)
    {
        hasNorthExit = north;
        hasSouthExit = south;
        hasEastExit = east;
        hasWestExit = west;
        
        // Update tilemaps if they exist
        if (wallTilemap != null && floorTilemap != null)
        {
            UpdateExitTiles();
        }
    }
    
    public void SetGridPosition(Vector2Int pos)
    {
        gridPos = pos;
    }
    
    // Set the room type
    public void SetRoomType(RoomType type)
    {
        roomType = type;
        Debug.Log($"Room {gameObject.name} at {gridPos} set to type: {type}");
    }
    
    // Get the room type
    public RoomType GetRoomType()
    {
        return roomType;
    }
    
    // Generate the actual tiles on dual tilemaps
    public void GenerateRoomTiles()
    {
        if (wallTilemap == null || floorTilemap == null || floorTile == null || wallTile == null)
        {
            Debug.LogWarning($"Room {gameObject.name}: Missing tilemap components or tile assets!");
            return;
        }
        
        Vector2Int totalSize = TotalSize;
        Vector3Int offset = GetRoomTileOffset();
        
        // Clear existing tiles on both tilemaps
        BoundsInt bounds = new BoundsInt(offset.x, offset.y, 0, totalSize.x, totalSize.y, 1);
        wallTilemap.SetTilesBlock(bounds, new TileBase[totalSize.x * totalSize.y]);
        floorTilemap.SetTilesBlock(bounds, new TileBase[totalSize.x * totalSize.y]);
        
        // Generate tile layout data
        int[,] tileLayout = GenerateTileLayout();
        
        // Apply tiles to appropriate tilemaps - apply offset to center room at (0,0)
        for (int x = 0; x < totalSize.x; x++)
        {
            for (int y = 0; y < totalSize.y; y++)
            {
                Vector3Int position = new Vector3Int(x + offset.x, y + offset.y, 0);
                
                if (tileLayout[x, y] == 1)
                {
                    // Place wall tile on wall tilemap (with collision)
                    wallTilemap.SetTile(position, wallTile);
                }
                else
                {
                    // Place floor tile on floor tilemap (no collision)
                    floorTilemap.SetTile(position, floorTile);
                }
            }
        }
        
        // Setup collision for walls and exits (but not floors)
        SetupTileCollisions();
    }
    
    // Setup 2D collisions for walls and exits, ensuring floors don't have collision
    private void SetupTileCollisions()
    {
        // Get or add TilemapCollider2D component
        TilemapCollider2D tilemapCollider = GetComponent<TilemapCollider2D>();
        if (tilemapCollider == null)
        {
            tilemapCollider = gameObject.AddComponent<TilemapCollider2D>();
        }
        
        // Enable the tilemap collider
        tilemapCollider.enabled = true;
        
        // The collision behavior depends on the tile assets:
        // - Wall tiles should have "Collider Type" set to "Sprite" in their physics shape
        // - Floor tiles should have "Collider Type" set to "None" in their physics shape
        // - Door tiles (when locked) should have collision enabled
        
        // Tile collisions setup - walls and exits have collision, floors do not
    }
    
    // Force collision refresh when tiles change
    private void RefreshTileCollisions()
    {
        TilemapCollider2D tilemapCollider = GetComponent<TilemapCollider2D>();
        if (tilemapCollider != null)
        {
            // Force regeneration of collision mesh
            tilemapCollider.enabled = false;
            tilemapCollider.enabled = true;
        }
    }
    
    // Get the tile offset to center the room at (0,0) on the global grid
    private Vector3Int GetRoomTileOffset()
    {
        Vector2Int totalSize = TotalSize;
        
        // Convert room's world position to tile position in the global grid
        Vector3 worldPos = transform.position;
        Vector3Int roomTilePos = Vector3Int.zero;
        
        if (grid != null)
        {
            // Convert world position to grid cell position
            roomTilePos = grid.WorldToCell(worldPos);
        }
        
        // Offset to position room tiles relative to the room's grid position
        // Center the room at its grid position by subtracting half the room size
        int offsetX = roomTilePos.x - (totalSize.x / 2);
        int offsetY = roomTilePos.y - (totalSize.y / 2);
        
        return new Vector3Int(offsetX, offsetY, 0);
    }
    
    // Generate tile layout for this room (0 = walkable, 1 = wall)
    public int[,] GenerateTileLayout()
    {
        Vector2Int totalSize = TotalSize;
        int[,] tiles = new int[totalSize.x, totalSize.y];
        
        // Fill with walls
        for (int x = 0; x < totalSize.x; x++)
        {
            for (int y = 0; y < totalSize.y; y++)
            {
                tiles[x, y] = 1; // Wall
            }
        }
        
        // Create interior walkable area
        for (int x = 1; x < totalSize.x - 1; x++)
        {
            for (int y = 1; y < totalSize.y - 1; y++)
            {
                tiles[x, y] = 0; // Walkable floor
            }
        }
        
        // Create exits by carving through walls - 2 tiles wide/tall for symmetry
        int midX = totalSize.x / 2;
        int midY = totalSize.y / 2;
        
        // North exit (2 tiles wide)
        if (hasNorthExit)
        {
            tiles[midX - 1, totalSize.y - 1] = 0;
            tiles[midX, totalSize.y - 1] = 0;
        }
        
        // South exit (2 tiles wide)
        if (hasSouthExit)
        {
            tiles[midX - 1, 0] = 0;
            tiles[midX, 0] = 0;
        }
        
        // East exit (2 tiles tall)
        if (hasEastExit)
        {
            tiles[totalSize.x - 1, midY - 1] = 0;
            tiles[totalSize.x - 1, midY] = 0;
        }
        
        // West exit (2 tiles tall)
        if (hasWestExit)
        {
            tiles[0, midY - 1] = 0;
            tiles[0, midY] = 0;
        }
        
        return tiles;
    }
    
    // Method to update tiles when exits change
    public void UpdateExitTiles()
    {
        if (wallTilemap != null && floorTilemap != null)
        {
            Vector2Int totalSize = TotalSize;
            Vector3Int offset = GetRoomTileOffset();
            int midX = totalSize.x / 2;
            int midY = totalSize.y / 2;
            
            // Update exit tiles - 2 tiles wide/tall for symmetry, apply offset for global grid alignment
            // North exit (top wall center - 2 tiles wide)
            Vector3Int northPos1 = new Vector3Int(midX - 1 + offset.x, totalSize.y - 1 + offset.y, 0);
            Vector3Int northPos2 = new Vector3Int(midX + offset.x, totalSize.y - 1 + offset.y, 0);
            UpdateExitTilePosition(northPos1, hasNorthExit);
            UpdateExitTilePosition(northPos2, hasNorthExit);
            
            // South exit (bottom wall center - 2 tiles wide)
            Vector3Int southPos1 = new Vector3Int(midX - 1 + offset.x, 0 + offset.y, 0);
            Vector3Int southPos2 = new Vector3Int(midX + offset.x, 0 + offset.y, 0);
            UpdateExitTilePosition(southPos1, hasSouthExit);
            UpdateExitTilePosition(southPos2, hasSouthExit);
            
            // East exit (right wall center - 2 tiles tall)
            Vector3Int eastPos1 = new Vector3Int(totalSize.x - 1 + offset.x, midY - 1 + offset.y, 0);
            Vector3Int eastPos2 = new Vector3Int(totalSize.x - 1 + offset.x, midY + offset.y, 0);
            UpdateExitTilePosition(eastPos1, hasEastExit);
            UpdateExitTilePosition(eastPos2, hasEastExit);
            
            // West exit (left wall center - 2 tiles tall)
            Vector3Int westPos1 = new Vector3Int(0 + offset.x, midY - 1 + offset.y, 0);
            Vector3Int westPos2 = new Vector3Int(0 + offset.x, midY + offset.y, 0);
            UpdateExitTilePosition(westPos1, hasWestExit);
            UpdateExitTilePosition(westPos2, hasWestExit);
        }
    }
    
    // Helper method to update a single exit tile position on appropriate tilemap
    private void UpdateExitTilePosition(Vector3Int position, bool isExit)
    {
        if (isExit)
        {
            // Exit is open - remove wall, add floor
            wallTilemap.SetTile(position, null);
            floorTilemap.SetTile(position, floorTile);
        }
        else
        {
            // Exit is closed - add wall, remove floor
            wallTilemap.SetTile(position, wallTile);
            floorTilemap.SetTile(position, null);
        }
    }
    
    // Method to set door tile (block 2) or floor tile at exit positions - 2 tiles for symmetry
    private void SetExitTile(string direction, bool locked)
    {
        if (wallTilemap == null || floorTilemap == null) return;
        
        Vector3Int[] tilePositions = GetDoorTilePositions(direction);
        
        foreach (Vector3Int pos in tilePositions)
        {
            if (locked)
            {
                // Door is locked - place door tile on wall tilemap (with collision)
                wallTilemap.SetTile(pos, doorTile);
                floorTilemap.SetTile(pos, null);
            }
            else
            {
                // Door is unlocked - place floor tile on floor tilemap (no collision)
                wallTilemap.SetTile(pos, null);
                floorTilemap.SetTile(pos, floorTile);
            }
        }
    }
    
    // Get world positions for door placement - aligned to global grid
    public Vector3 GetDoorWorldPosition(string direction)
    {
        if (grid == null) return transform.position;
        
        // Use the offset-adjusted tile position from GetDoorTilePosition
        Vector3Int tilePos = GetDoorTilePosition(direction);
        
        return transform.position + grid.CellToWorld(tilePos) + new Vector3(grid.cellSize.x * 0.5f, grid.cellSize.y * 0.5f, 0);
    }
    
    // Get tile positions for door placement (2 tiles for symmetry) - aligned to global grid
    public Vector3Int[] GetDoorTilePositions(string direction)
    {
        Vector2Int totalSize = TotalSize;
        Vector3Int offset = GetRoomTileOffset();
        int midX = totalSize.x / 2;
        int midY = totalSize.y / 2;
        
        switch (direction.ToLower())
        {
            case "north":
                return new Vector3Int[] {
                    new Vector3Int(midX - 1 + offset.x, totalSize.y - 1 + offset.y, 0),
                    new Vector3Int(midX + offset.x, totalSize.y - 1 + offset.y, 0)
                };
            case "south":
                return new Vector3Int[] {
                    new Vector3Int(midX - 1 + offset.x, 0 + offset.y, 0),
                    new Vector3Int(midX + offset.x, 0 + offset.y, 0)
                };
            case "east":
                return new Vector3Int[] {
                    new Vector3Int(totalSize.x - 1 + offset.x, midY - 1 + offset.y, 0),
                    new Vector3Int(totalSize.x - 1 + offset.x, midY + offset.y, 0)
                };
            case "west":
                return new Vector3Int[] {
                    new Vector3Int(0 + offset.x, midY - 1 + offset.y, 0),
                    new Vector3Int(0 + offset.x, midY + offset.y, 0)
                };
            default:
                return new Vector3Int[] { Vector3Int.zero };
        }
    }
    
    // Get single tile position for door placement (center of 2-tile exit) - aligned to global grid
    public Vector3Int GetDoorTilePosition(string direction)
    {
        Vector2Int totalSize = TotalSize;
        Vector3Int offset = GetRoomTileOffset();
        int midX = totalSize.x / 2;
        int midY = totalSize.y / 2;
        
        switch (direction.ToLower())
        {
            case "north":
                return new Vector3Int(midX + offset.x, totalSize.y - 1 + offset.y, 0);
            case "south":
                return new Vector3Int(midX + offset.x, 0 + offset.y, 0);
            case "east":
                return new Vector3Int(totalSize.x - 1 + offset.x, midY + offset.y, 0);
            case "west":
                return new Vector3Int(0 + offset.x, midY + offset.y, 0);
            default:
                return Vector3Int.zero;
        }
    }
    
    private void SetupEnemyCollision(GameObject enemyObj)
    {
        // Ensure Rigidbody2D has proper collision detection
        Rigidbody2D rb = enemyObj.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous; // Better collision detection
            rb.constraints = RigidbodyConstraints2D.FreezeRotation; // Prevent rotation
        }
        
        // Check if enemy has colliders for physics collision and contact damage
        Collider2D[] colliders = enemyObj.GetComponents<Collider2D>();
        bool hasNonTriggerCollider = false;
        bool hasTriggerCollider = false;
        
        foreach (Collider2D col in colliders)
        {
            if (col.isTrigger)
            {
                hasTriggerCollider = true;
            }
            else
            {
                hasNonTriggerCollider = true;
            }
        }
        
        // Add non-trigger collider for wall collision if missing
        if (!hasNonTriggerCollider)
        {
            CircleCollider2D collisionCol = enemyObj.AddComponent<CircleCollider2D>();
            collisionCol.isTrigger = false;
            collisionCol.radius = 0.4f; // Physics collision with walls
        }
        
        // Add trigger collider for contact damage if missing
        if (!hasTriggerCollider)
        {
            CircleCollider2D triggerCol = enemyObj.AddComponent<CircleCollider2D>();
            triggerCol.isTrigger = true;
            triggerCol.radius = 0.5f; // Slightly larger for contact damage detection
            Debug.Log($"Room {gameObject.name}: Added trigger collider to enemy {enemyObj.name} for contact damage");
        }
    }
    
    /// <summary>
    /// Provide spawn protection to prevent immediate contact damage
    /// </summary>
    private System.Collections.IEnumerator EnemySpawnProtection(Enemy enemy)
    {
        if (enemy == null) yield break;
        
        // Temporarily disable contact damage
        bool originalContactDamage = enemy.GetType().GetField("enableContactDamage", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.GetValue(enemy) as bool? ?? true;
        
        // Disable contact damage for spawn protection period
        var contactDamageField = enemy.GetType().GetField("enableContactDamage", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        contactDamageField?.SetValue(enemy, false);
        
        // Wait for spawn protection period
        yield return new WaitForSeconds(0.5f);
        
        // Re-enable contact damage
        contactDamageField?.SetValue(enemy, originalContactDamage);
        
        Debug.Log($"Enemy {enemy.gameObject.name}: Spawn protection ended, contact damage restored");
    }
    
    private void EnsureEnemyInitialization(GameObject enemyObj)
    {
        // Disable OutOfBounds component if it exists (it interferes with room-based movement)
        OutOfBounds outOfBounds = enemyObj.GetComponent<OutOfBounds>();
        if (outOfBounds != null)
        {
            outOfBounds.enabled = false;
        }
        
        // Ensure Rigidbody2D is properly set up
        Rigidbody2D rb = enemyObj.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.WakeUp(); // Wake up the rigidbody if it's sleeping
            if (rb.bodyType == RigidbodyType2D.Kinematic)
            {
                rb.bodyType = RigidbodyType2D.Dynamic; // Enemies need dynamic physics to move
            }
        }
    }
    
    /// <summary>
    /// Shows spawn indicator tiles at the given positions
    /// </summary>
    /// <param name="spawnPositions">World positions where enemies will spawn</param>
    /// <returns>List of tilemap positions where indicators were placed</returns>
    private List<Vector3Int> ShowSpawnIndicators(List<Vector3> spawnPositions)
    {
        List<Vector3Int> indicatorPositions = new List<Vector3Int>();
        
        if (spawnIndicatorTilemap == null)
        {
            Debug.LogWarning($"Room {gameObject.name}: No spawn indicator tilemap available for showing indicators");
            return indicatorPositions;
        }
        
        if (spawnIndicatorTile == null)
        {
            Debug.LogWarning($"Room {gameObject.name}: No spawn indicator tile assigned");
            return indicatorPositions;
        }
        
        foreach (Vector3 worldPos in spawnPositions)
        {
            // Convert world position to tilemap position
            Vector3Int tilePos = spawnIndicatorTilemap.WorldToCell(worldPos);
            
            // Place the indicator tile
            spawnIndicatorTilemap.SetTile(tilePos, spawnIndicatorTile);
            indicatorPositions.Add(tilePos);
        }
        
        Debug.Log($"Room {gameObject.name}: Showing {indicatorPositions.Count} spawn indicators");
        return indicatorPositions;
    }
    
    /// <summary>
    /// Removes spawn indicator tiles from the given positions
    /// </summary>
    /// <param name="indicatorPositions">Tilemap positions to clear</param>
    private void RemoveSpawnIndicators(List<Vector3Int> indicatorPositions)
    {
        if (spawnIndicatorTilemap == null)
        {
            Debug.LogWarning($"Room {gameObject.name}: No spawn indicator tilemap available for removing indicators");
            return;
        }
        
        foreach (Vector3Int tilePos in indicatorPositions)
        {
            // Remove the tile (set to null)
            spawnIndicatorTilemap.SetTile(tilePos, null);
        }
        
        Debug.Log($"Room {gameObject.name}: Removed {indicatorPositions.Count} spawn indicators");
    }
    
    // === BOSS ROOM FUNCTIONALITY ===
    
    /// <summary>
    /// Spawn the boss with a delay
    /// </summary>
    private System.Collections.IEnumerator SpawnBossAfterDelay()
    {
        yield return new WaitForSeconds(bossSpawnDelay);
        SpawnBoss();
    }
    
    /// <summary>
    /// Spawn the boss in the room
    /// </summary>
    public void SpawnBoss()
    {
        if (roomType != RoomType.Boss)
        {
            Debug.LogWarning($"Room {gameObject.name}: Cannot spawn boss - room type is not Boss!");
            return;
        }
        
        if (bossSpawned || bossPrefabs == null || bossPrefabs.Length == 0) return;
        
        // Select boss prefab
        GameObject bossToSpawn = null;
        if (spawnRandomBoss)
        {
            bossToSpawn = bossPrefabs[Random.Range(0, bossPrefabs.Length)];
        }
        else
        {
            int index = Mathf.Clamp(specificBossIndex, 0, bossPrefabs.Length - 1);
            bossToSpawn = bossPrefabs[index];
        }
        
        if (bossToSpawn == null)
        {
            Debug.LogError($"Boss Room {gameObject.name}: No valid boss prefab to spawn!");
            return;
        }
        
        // Determine spawn position
        Vector3 spawnPos = GetBossSpawnPosition();
        
        // Spawn the boss
        currentBoss = Instantiate(bossToSpawn, spawnPos, Quaternion.identity);
        currentBossEnemy = currentBoss.GetComponent<Enemy>();
        
        if (currentBossEnemy != null)
        {
            // Subscribe to boss death event
            currentBossEnemy.OnDeath += OnBossDefeated;
        }
        else
        {
            Debug.LogWarning($"Boss Room {gameObject.name}: Boss prefab {bossToSpawn.name} doesn't have Enemy component!");
        }
        
        bossSpawned = true;
        
        Debug.Log($"Boss Room {gameObject.name}: Spawned boss {bossToSpawn.name} at {spawnPos}");
    }
    
    /// <summary>
    /// Get the position where the boss should spawn
    /// </summary>
    private Vector3 GetBossSpawnPosition()
    {
        // Use specific spawn point if provided
        if (bossSpawnPoint != null)
        {
            return bossSpawnPoint.position;
        }
        
        // Use room center if enabled
        if (spawnBossAtCenter)
        {
            return transform.position; // Room center
        }
        
        // Fallback to room position
        return transform.position;
    }
    
    /// <summary>
    /// Configure boss prefab from external source (e.g., DungeonGenerator)
    /// </summary>
    /// <param name="prefab">The boss prefab to use</param>
    public void ConfigureBossPrefab(GameObject prefab)
    {
        if (prefab == null)
        {
            Debug.LogWarning($"Room {gameObject.name}: Cannot configure null boss prefab!");
            return;
        }
        
        // Set the boss prefabs array with the provided prefab
        bossPrefabs = new GameObject[] { prefab };
        spawnRandomBoss = false; // Use the specific prefab
        specificBossIndex = 0;
        
        Debug.Log($"Room {gameObject.name}: Configured with boss prefab {prefab.name}");
    }
    
    /// <summary>
    /// Called when the boss is defeated
    /// </summary>
    private void OnBossDefeated(Enemy defeatedEnemy)
    {
        if (defeatedEnemy != currentBossEnemy) return; // Not our boss
        
        bossDefeated = true;
        isCleared = true;
        
        Debug.Log($"Boss Room {gameObject.name}: Boss defeated! Room cleared.");
        
        // Spawn boss defeat prefab
        SpawnBossDefeatPrefab();
        
        // Unsubscribe from death event
        if (currentBossEnemy != null)
        {
            currentBossEnemy.OnDeath -= OnBossDefeated;
        }
        
        // Handle room clearing
        OnBossRoomCleared();
    }
    
    /// <summary>
    /// Spawn boss defeat prefab in the middle of the room
    /// </summary>
    private void SpawnBossDefeatPrefab()
    {
        if (bossDefeatPrefab == null)
        {
            Debug.Log($"Boss Room {gameObject.name}: No boss defeat prefab configured");
            return;
        }
        
        Vector3 spawnPosition;
        
        if (spawnDefeatPrefabAtCenter)
        {
            // Spawn at room center
            spawnPosition = GetCenter() + defeatPrefabOffset;
        }
        else
        {
            // Spawn at room transform position
            spawnPosition = transform.position + defeatPrefabOffset;
        }
        
        // Instantiate the defeat prefab
        GameObject defeatObject = Instantiate(bossDefeatPrefab, spawnPosition, Quaternion.identity);
        
        // Set parent to keep scene organized
        defeatObject.transform.SetParent(transform);
        
        Debug.Log($"Boss Room {gameObject.name}: Spawned boss defeat prefab '{bossDefeatPrefab.name}' at {spawnPosition}");
    }
    
    /// <summary>
    /// Handle boss room clearing logic
    /// </summary>
    private void OnBossRoomCleared()
    {
        // Unlock doors
        doorsLocked = false;
        UpdateExitTiles();
        Debug.Log($"Boss Room {gameObject.name}: Doors unlocked!");
        
        // Spawn rewards
        SpawnRewards();
        
        // Trigger room cleared event
        OnRoomCleared?.Invoke(this);
    }
    
    /// <summary>
    /// Spawn victory rewards
    /// </summary>
    private void SpawnRewards()
    {
        if (rewardPrefabs == null || rewardPrefabs.Length == 0) return;
        
        Vector3 rewardPos = rewardSpawnPoint != null ? rewardSpawnPoint.position : transform.position;
        
        foreach (GameObject rewardPrefab in rewardPrefabs)
        {
            if (rewardPrefab != null)
            {
                Instantiate(rewardPrefab, rewardPos, Quaternion.identity);
                Debug.Log($"Boss Room {gameObject.name}: Spawned reward {rewardPrefab.name}");
                
                // Offset position for next reward
                rewardPos.x += 1.0f;
            }
        }
    }
    
    /// <summary>
    /// Spawn item in the center of the item room
    /// </summary>
    private void SpawnItem()
    {
        if (itemSpawned)
        {
            Debug.LogWarning($"Item Room {gameObject.name}: Item already spawned!");
            return;
        }
        
        if (itemPrefabs == null || itemPrefabs.Length == 0)
        {
            Debug.LogError($"Item Room {gameObject.name}: No item prefabs assigned!");
            return;
        }
        
        // Choose random item from available prefabs
        GameObject itemToSpawn = itemPrefabs[Random.Range(0, itemPrefabs.Length)];
        
        if (itemToSpawn == null)
        {
            Debug.LogError($"Item Room {gameObject.name}: Selected item prefab is null!");
            return;
        }
        
        // Calculate spawn position
        Vector3 spawnPosition;
        if (spawnItemAtCenter)
        {
            spawnPosition = transform.position + itemSpawnOffset; // Room center + offset
        }
        else
        {
            spawnPosition = transform.position + itemSpawnOffset; // Room transform + offset
        }
        
        // Instantiate the item
        currentItem = Instantiate(itemToSpawn, spawnPosition, Quaternion.identity, transform);
        currentItem.name = $"Item_{itemToSpawn.name}";
        
        itemSpawned = true;
        
        Debug.Log($"Item Room {gameObject.name}: Spawned item '{itemToSpawn.name}' at {spawnPosition}");
    }
    
    /// <summary>
    /// Force spawn boss (for testing)
    /// </summary>
    [ContextMenu("Force Spawn Boss")]
    public void ForceSpawnBoss()
    {
        if (roomType != RoomType.Boss)
        {
            Debug.LogWarning($"Room {gameObject.name}: Cannot force spawn boss - room type is not Boss!");
            return;
        }
        
        bossSpawned = false; // Reset flag
        bossDefeated = false; // Reset flag
        SpawnBoss();
    }
    
    /// <summary>
    /// Force clear boss room (for testing)
    /// </summary>
    [ContextMenu("Force Clear Boss Room")]
    public void ForceClearBossRoom()
    {
        if (roomType != RoomType.Boss)
        {
            Debug.LogWarning($"Room {gameObject.name}: Cannot force clear - room type is not Boss!");
            return;
        }
        
        bossDefeated = true;
        OnBossRoomCleared();
    }
    
    /// <summary>
    /// Reset boss room state (for testing)
    /// </summary>
    [ContextMenu("Reset Boss Room")]
    public void ResetBossRoom()
    {
        if (roomType != RoomType.Boss)
        {
            Debug.LogWarning($"Room {gameObject.name}: Cannot reset - room type is not Boss!");
            return;
        }
        
        // Destroy current boss if it exists
        if (currentBoss != null)
        {
            if (currentBossEnemy != null)
            {
                currentBossEnemy.OnDeath -= OnBossDefeated;
            }
            DestroyImmediate(currentBoss);
        }
        
        // Reset states
        bossSpawned = false;
        bossDefeated = false;
        isCleared = false;
        doorsLocked = true;
        
        // Update tiles
        UpdateExitTiles();
        
        Debug.Log($"Boss Room {gameObject.name}: Reset complete");
    }
    
    /// <summary>
    /// Force spawn item (for testing or special conditions)
    /// </summary>
    [ContextMenu("Test Spawn Item")]
    public void ForceSpawnItem()
    {
        if (roomType != RoomType.Item)
        {
            Debug.LogWarning($"Room {gameObject.name}: Cannot spawn item - room type is not Item!");
            return;
        }
        
        if (!itemSpawned)
        {
            SpawnItem();
        }
    }
    
    /// <summary>
    /// Check if item has been spawned in this room
    /// </summary>
    /// <returns>True if item is spawned</returns>
    public bool IsItemSpawned()
    {
        return itemSpawned;
    }
    
    /// <summary>
    /// Check if item has been collected from this room
    /// </summary>
    /// <returns>True if item is collected</returns>
    public bool IsItemCollected()
    {
        return itemCollected;
    }
    
    /// <summary>
    /// Get the spawned item GameObject
    /// </summary>
    /// <returns>The spawned item, or null if none</returns>
    public GameObject GetSpawnedItem()
    {
        return currentItem;
    }
    
    /// <summary>
    /// Configure item prefabs from external source (e.g., DungeonGenerator)
    /// </summary>
    /// <param name="prefabs">Array of item prefabs to use</param>
    public void ConfigureItemPrefabs(GameObject[] prefabs)
    {
        itemPrefabs = prefabs;
        Debug.Log($"Item Room {gameObject.name}: Configured with {prefabs?.Length ?? 0} item prefab(s)");
    }

    protected virtual void OnDestroy()
    {
        // Clear room tiles from global tilemaps when room is destroyed (only if tilemaps are assigned)
        if (wallTilemap != null && floorTilemap != null)
        {
            ClearRoomTiles();
        }
        
        // Unsubscribe from enemy events
        foreach (Enemy enemy in enemiesInRoom)
        {
            if (enemy != null)
            {
                enemy.OnDeath -= OnEnemyDeath;
            }
        }
        
        // Unsubscribe from boss events if this is a boss room
        if (roomType == RoomType.Boss && currentBossEnemy != null)
        {
            currentBossEnemy.OnDeath -= OnBossDefeated;
        }
    }
}