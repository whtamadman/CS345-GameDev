using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections.Generic;

public class Room : MonoBehaviour
{
    [Header("Room Configuration")]
    public Vector2Int interiorSize = new Vector2Int(14, 10); // Walkable area
    public Vector2Int gridPos; // Position in dungeon grid
    
    [Header("Global Tilemap Components")]
    [SerializeField] private Grid grid;                  // Global grid (assigned at runtime)
    [SerializeField] private Tilemap wallTilemap;        // Global wall tilemap (assigned at runtime)
    [SerializeField] private Tilemap floorTilemap;       // Global floor tilemap (assigned at runtime)
    
    [Header("Auto-Find Settings")]
    public bool autoFindGlobalTilemaps = true;           // Automatically find global tilemaps at runtime
    public bool enableDebugLogging = true;              // Enable verbose debug logging (disable to reduce memory allocations)
    public string globalGridName = "Global_Grid";        // Name of global grid to find
    public string wallTilemapName = "Collision TM";      // Name of wall tilemap to find
    public string floorTilemapName = "Floor TM";    // Name of floor tilemap to find
    
    [Header("Tile Assets")]
    public TileBase floorTile;
    public TileBase wallTile;
    public TileBase doorTile; // Tile block 2 for doors
    
    [Header("Exits")]
    public bool hasNorthExit = true;   // Default: all exits open
    public bool hasSouthExit = true;   // Default: all exits open
    public bool hasEastExit = true;    // Default: all exits open
    public bool hasWestExit = true;    // Default: all exits open
    
    [Header("Room Variant Info")]
    [SerializeField] private string roomVariantName = "";  // Auto-generated based on exits
    [SerializeField] private int exitCount = 0;            // Number of exits this room has
    
    [Header("Door State")]
    public bool doorsLocked = false;
    
    [Header("Room State")]
    public bool isCleared = false;
    public bool playerInRoom = false;
    
    // Events
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
        
        // Configuration is valid
        
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
        // Count exits
        exitCount = 0;
        if (hasNorthExit) exitCount++;
        if (hasSouthExit) exitCount++;
        if (hasEastExit) exitCount++;
        if (hasWestExit) exitCount++;
        
        // Generate variant name
        List<string> exits = new List<string>();
        if (hasNorthExit) exits.Add("N");
        if (hasSouthExit) exits.Add("S");
        if (hasEastExit) exits.Add("E");
        if (hasWestExit) exits.Add("W");
        
        roomVariantName = exits.Count > 0 ? string.Join("", exits) : "NoExits";
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
        if (string.IsNullOrEmpty(roomVariantName))
        {
            UpdateRoomVariantInfo();
        }
        return roomVariantName;
    }
    
    // Get exit count
    public int GetExitCount()
    {
        return exitCount;
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
        
        // If auto-find failed, show helpful message
        if (grid == null || wallTilemap == null || floorTilemap == null)
        {
            Debug.LogWarning($"Room {gameObject.name}: Could not auto-find all global tilemaps. Check names: Grid='{globalGridName}', Wall='{wallTilemapName}', Floor='{floorTilemapName}'");
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
        
        // Lock exits if room is not cleared
        if (!isCleared)
        {
            LockExits();
        }
        
        // Notify systems that player entered
        OnPlayerEntered?.Invoke(this);
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
        
        // Notify systems that room is cleared
        OnRoomCleared?.Invoke(this);
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
        doorsLocked = false;
        
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
        // Room is cleared when all enemies are defeated
        if (enemiesInRoom.Count == 0 && !isCleared)
        {
            MarkCleared();
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
    
    protected virtual void OnDestroy()
    {
        // Unsubscribe from enemy events
        foreach (Enemy enemy in enemiesInRoom)
        {
            if (enemy != null)
            {
                enemy.OnDeath -= OnEnemyDeath;
            }
        }
    }
}