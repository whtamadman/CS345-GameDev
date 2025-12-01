using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections.Generic;

[System.Serializable]
public class LevelData
{
    [Header("Level Info")]
    public string levelName = "Level 1";
    public int levelNumber = 1;
    
    [Header("Room Prefabs")]
    public GameObject universalRoomPrefab;
    
    [Header("Boss Settings")]
    public GameObject bossPrefab;
    [Tooltip("Optional: Custom boss room prefab. If null, uses default room layout with boss spawning.")]
    public GameObject bossRoomPrefab; // Optional custom boss room
    
    [Header("Item Settings")]
    public GameObject[] itemPrefabs;
    
    [Header("Level-Specific Settings")]
    public Color levelThemeColor = Color.white;
    public int difficultyModifier = 0;
}

public class DungeonGenerator : MonoBehaviour
{
    [Header("Grid Settings")]
    public int gridRows = 3;
    public int gridCols = 4;
    public Vector2Int roomSizeInTiles = new Vector2Int(16, 12);
    public float globalGridCellSize = 0.4f;
    
    [Header("Multi-Level System")]
    public List<LevelData> levels = new List<LevelData>();
    [SerializeField] private int currentLevelIndex = 0;
    
    [Header("Fallback Prefabs (if level data missing)")]
    public GameObject fallbackRoomPrefab;
    public GameObject fallbackBossPrefab;
    public GameObject[] fallbackItemPrefabs;
    
    [Header("Item Room Configuration")]
    [Tooltip("Common items that can spawn in any item room across all levels")]
    public GameObject[] commonItemPrefabs;
    [Tooltip("Rare items with lower spawn chance")]
    public GameObject[] rareItemPrefabs;
    [Tooltip("Epic items with very low spawn chance")]
    public GameObject[] epicItemPrefabs;
    [Tooltip("Enable weighted item selection based on rarity")]
    public bool useWeightedItemSelection = true;
    [Tooltip("Chance weights: [Common, Rare, Epic] - higher values = more likely")]
    public Vector3 itemRarityWeights = new Vector3(70f, 25f, 5f); // Common, Rare, Epic percentages
    
    [Header("Room Counts")]
    public int fightRoomCount = 6;
    
    // Properties
    public float RoomSpacingX => roomSizeInTiles.x * globalGridCellSize;
    public float RoomSpacingY => roomSizeInTiles.y * globalGridCellSize;
    
    [Header("Generated Rooms")]
    [SerializeField] private Room[,] roomGrid;
    [SerializeField] private Room startRoom;
    [SerializeField] private Room bossRoom;
    [SerializeField] private Room itemRoom;
    [SerializeField] private List<Room> fightRooms = new List<Room>();
    
    private List<Vector2Int> availablePositions = new List<Vector2Int>();
    
    // Context menu methods
    [ContextMenu("Generate Test Dungeon")]
    public void GenerateTestDungeon()
    {
        GenerateDungeon();
    }
    
    [ContextMenu("Clear Dungeon")]
    public void ClearDungeon()
    {
        ClearExistingDungeon();
        ClearAllTilemaps();
    }
    
    [ContextMenu("Clear and Regenerate")]
    public void ClearAndRegenerate()
    {
        ClearExistingDungeon();
        GenerateDungeon();
    }
    
    /// <summary>
    /// Clear only dungeon-specific tilemaps (not all tilemaps in scene)
    /// </summary>
    public void ClearAllTilemaps()
    {
        // Define the specific tilemap names used by the dungeon system
        string[] dungeonTilemapNames = {
            "Collision TM",      // Walls
            "Floor TM",          // Floors  
            "Decal TM",          // Spawn indicators/decals
            "Breakable TM"       // Breakable blocks
        };
        
        int clearedCount = 0;
        int totalTilemaps = dungeonTilemapNames.Length;
        
        foreach (string tilemapName in dungeonTilemapNames)
        {
            GameObject tilemapObj = GameObject.Find(tilemapName);
            if (tilemapObj != null)
            {
                Tilemap tilemap = tilemapObj.GetComponent<Tilemap>();
                if (tilemap != null)
                {
                    try
                    {
                        // Get the bounds of all tiles in the tilemap
                        BoundsInt bounds = tilemap.cellBounds;
                        
                        // If tilemap has content, clear it
                        if (bounds.size.x > 0 && bounds.size.y > 0)
                        {
                            // Use SetTilesBlock with null array
                            int totalTiles = bounds.size.x * bounds.size.y * bounds.size.z;
                            TileBase[] emptyTiles = new TileBase[totalTiles];
                            tilemap.SetTilesBlock(bounds, emptyTiles);
                            
                            Debug.Log($"Cleared dungeon tilemap: {tilemap.name} (bounds: {bounds}, {totalTiles} tiles)");
                            clearedCount++;
                        }
                        else
                        {
                            // Force clear using FloodFill for edge cases
                            tilemap.FloodFill(Vector3Int.zero, null);
                            Debug.Log($"Force cleared dungeon tilemap: {tilemap.name} (empty bounds)");
                            clearedCount++;
                        }
                        
                        // Additional safety - compress bounds to remove empty space
                        tilemap.CompressBounds();
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogError($"Error clearing dungeon tilemap {tilemap.name}: {e.Message}");
                        
                        // Fallback: Try to clear a large area around origin
                        try
                        {
                            BoundsInt largeBounds = new BoundsInt(-100, -100, 0, 200, 200, 1);
                            TileBase[] emptyTiles = new TileBase[largeBounds.size.x * largeBounds.size.y];
                            tilemap.SetTilesBlock(largeBounds, emptyTiles);
                            tilemap.CompressBounds();
                            Debug.Log($"Fallback cleared dungeon tilemap: {tilemap.name}");
                            clearedCount++;
                        }
                        catch (System.Exception fallbackError)
                        {
                            Debug.LogError($"Fallback clearing failed for dungeon tilemap {tilemap.name}: {fallbackError.Message}");
                        }
                    }
                }
                else
                {
                    Debug.LogWarning($"GameObject '{tilemapName}' found but no Tilemap component");
                }
            }
            else
            {
                Debug.LogWarning($"Dungeon tilemap '{tilemapName}' not found in scene");
            }
        }
        
        Debug.Log($"Successfully cleared {clearedCount}/{totalTilemaps} dungeon tilemaps for clean level transition");
    }
    
    [ContextMenu("Next Level")]
    public void GenerateNextLevel()
    {
        if (currentLevelIndex < levels.Count - 1)
        {
            currentLevelIndex++;
            ClearAndRegenerate();
        }
        else
        {
            Debug.Log("Already at the last level!");
        }
    }
    
    [ContextMenu("Previous Level")]
    public void GeneratePreviousLevel()
    {
        if (currentLevelIndex > 0)
        {
            currentLevelIndex--;
            ClearAndRegenerate();
        }
        else
        {
            Debug.Log("Already at the first level!");
        }
    }
    
    /// <summary>
    /// Check if there is a next level available
    /// </summary>
    /// <returns>True if there is a next level, false if at the last level</returns>
    public bool HasNextLevel()
    {
        return currentLevelIndex < levels.Count - 1;
    }
    
    [ContextMenu("Show Room Positions")]
    public void ShowRoomPositions()
    {
        if (roomGrid == null)
        {
            Debug.LogError("No dungeon generated yet!");
            return;
        }
        
        Debug.Log($"=== DUNGEON LAYOUT ({gridRows}×{gridCols}) ===");
        Debug.Log($"Room spacing: {RoomSpacingX:F1} × {RoomSpacingY:F1} world units");
        
        for (int row = 0; row < gridRows; row++)
        {
            for (int col = 0; col < gridCols; col++)
            {
                Room room = roomGrid[row, col];
                if (room != null)
                {
                    Vector3 expectedPos = new Vector3(col * RoomSpacingX, row * RoomSpacingY, 0);
                    string roomType = GetRoomType(room);
                    Debug.Log($"[{row},{col}] {roomType}: Expected({expectedPos.x:F1},{expectedPos.y:F1}) Actual({room.transform.position.x:F1},{room.transform.position.y:F1}) Exits(N:{room.hasNorthExit} S:{room.hasSouthExit} E:{room.hasEastExit} W:{room.hasWestExit})");
                }
            }
        }
    }
    
    private string GetRoomType(Room room)
    {
        if (room == startRoom) return "START";
        if (room == bossRoom) return "BOSS";
        if (room == itemRoom) return "ITEM";
        if (fightRooms.Contains(room)) return "FIGHT";
        return "UNKNOWN";
    }
    
    public void GenerateDungeon()
    {
        ClearExistingDungeon();
        InitializeGrid();
        PlaceRooms();
        ConnectRooms();
    }
    
    private void ClearExistingDungeon()
    {
        // Destroy existing room GameObjects
        if (roomGrid != null)
        {
            for (int row = 0; row < gridRows; row++)
            {
                for (int col = 0; col < gridCols; col++)
                {
                    if (roomGrid[row, col] != null)
                    {
                        DestroyImmediate(roomGrid[row, col].gameObject);
                    }
                }
            }
        }
        
        // Clear references
        roomGrid = null;
        startRoom = null;
        bossRoom = null;
        itemRoom = null;
        fightRooms.Clear();
        availablePositions.Clear();
        
        Debug.Log($"Cleared existing dungeon layout for level regeneration");
    }
    
    public void SetCurrentLevel(int levelIndex)
    {
        if (levelIndex >= 0 && levelIndex < levels.Count)
        {
            currentLevelIndex = levelIndex;
            Debug.Log($"Set current level to: {GetCurrentLevelData().levelName}");
        }
        else
        {
            Debug.LogError($"Invalid level index: {levelIndex}. Valid range: 0-{levels.Count - 1}");
        }
    }
    
    public LevelData GetCurrentLevelData()
    {
        if (levels.Count == 0)
        {
            Debug.LogWarning("No levels configured! Creating default level data.");
            return CreateDefaultLevelData();
        }
        
        if (currentLevelIndex >= 0 && currentLevelIndex < levels.Count)
        {
            return levels[currentLevelIndex];
        }
        
        Debug.LogWarning($"Invalid current level index: {currentLevelIndex}. Using first level.");
        currentLevelIndex = 0;
        return levels[0];
    }
    
    private LevelData CreateDefaultLevelData()
    {
        LevelData defaultLevel = new LevelData();
        defaultLevel.levelName = "Default Level";
        defaultLevel.universalRoomPrefab = fallbackRoomPrefab;
        defaultLevel.bossPrefab = fallbackBossPrefab;
        defaultLevel.itemPrefabs = fallbackItemPrefabs;
        return defaultLevel;
    }
    
    private void InitializeGrid()
    {
        roomGrid = new Room[gridRows, gridCols];
        
        // Populate available positions
        for (int row = 0; row < gridRows; row++)
        {
            for (int col = 0; col < gridCols; col++)
            {
                availablePositions.Add(new Vector2Int(row, col));
            }
        }
    }
    
    private void PlaceRooms()
    {
        GenerateRoomsWithNewAlgorithm();
    }
    
    private void GenerateRoomsWithNewAlgorithm()
    {
        // Step 1: Place starting room at center
        Vector2Int centerPos = new Vector2Int(gridRows / 2, gridCols / 2);
        startRoom = CreateRoom(centerPos, "Start Room");
        availablePositions.Remove(centerPos);
        
        // Set room type to Start
        startRoom.SetRoomType(RoomType.Start);
        
        // Initialize with all exits open for start room
        startRoom.ConfigureExits(true, true, true, true);
        
        // Track all created rooms
        List<Room> allRooms = new List<Room> { startRoom };
        Room currentRoom = startRoom;
        
        // Step 2: Generate rooms using loop algorithm
        int roomsToGenerate = fightRoomCount; // Use existing room count setting
        int roomsGenerated = 0;
        int maxAttempts = roomsToGenerate * 3; // Prevent infinite loops
        int attempts = 0;
        
        while (roomsGenerated < roomsToGenerate && attempts < maxAttempts)
        {
            attempts++;
            
            // Try to spawn a new room connected to currentRoom
            Room newRoom = TrySpawnConnectedRoom(currentRoom, allRooms, roomsGenerated);
            
            if (newRoom != null)
            {
                allRooms.Add(newRoom);
                fightRooms.Add(newRoom);
                roomsGenerated++;
                
                // Randomly decide what to do next with currentRoom
                float randomChoice = Random.Range(0f, 1f);
                
                if (randomChoice < 0.4f)
                {
                    // 40% chance: Set currentRoom to the new room
                    currentRoom = newRoom;
                }
                else if (randomChoice < 0.7f)
                {
                    // 30% chance: Set currentRoom to a random existing room
                    currentRoom = allRooms[Random.Range(0, allRooms.Count)];
                }
                // 30% chance: Leave currentRoom unchanged
            }
            else
            {
                // No available doors in currentRoom, choose a random connected room
                List<Room> connectedRooms = GetConnectedRooms(currentRoom, allRooms);
                if (connectedRooms.Count > 0)
                {
                    currentRoom = connectedRooms[Random.Range(0, connectedRooms.Count)];
                }
                else
                {
                    // Fallback: choose any room
                    currentRoom = allRooms[Random.Range(0, allRooms.Count)];
                }
            }
        }
        
        // Step 3: Place boss room
        PlaceBossRoom(allRooms);
        
        // Step 4: Place special rooms (item room as treasure room)
        PlaceSpecialRooms(allRooms);
    }
    
    private Room TrySpawnConnectedRoom(Room currentRoom, List<Room> allRooms, int roomIndex)
    {
        // Get available doors (directions) from current room
        List<string> availableDoors = GetAvailableDoors(currentRoom);
        
        if (availableDoors.Count == 0)
        {
            return null; // No available doors
        }
        
        // Shuffle doors for randomness
        for (int i = 0; i < availableDoors.Count; i++)
        {
            string temp = availableDoors[i];
            int randomIndex = Random.Range(i, availableDoors.Count);
            availableDoors[i] = availableDoors[randomIndex];
            availableDoors[randomIndex] = temp;
        }
        
        // Try each door until we find a valid position
        foreach (string door in availableDoors)
        {
            Vector2Int newRoomPos = GetPositionInDirection(currentRoom.gridPos, door);
            
            // Check if position is valid and available
            if (IsValidPosition(newRoomPos) && availablePositions.Contains(newRoomPos))
            {
                // Create new room
                Room newRoom = CreateRoom(newRoomPos, $"Room {roomIndex + 1}");
                availablePositions.Remove(newRoomPos);
                
                // Configure bidirectional connection
                EstablishConnection(currentRoom, newRoom, door);
                
                return newRoom;
            }
        }
        
        return null; // Couldn't place room in any direction
    }
    
    private List<string> GetAvailableDoors(Room room)
    {
        List<string> doors = new List<string>();
        Vector2Int pos = room.gridPos;
        
        // Check each direction for availability
        if (pos.x + 1 < gridRows && IsPositionAvailableForConnection(pos, "north")) doors.Add("north");
        if (pos.x - 1 >= 0 && IsPositionAvailableForConnection(pos, "south")) doors.Add("south");
        if (pos.y + 1 < gridCols && IsPositionAvailableForConnection(pos, "east")) doors.Add("east");
        if (pos.y - 1 >= 0 && IsPositionAvailableForConnection(pos, "west")) doors.Add("west");
        
        return doors;
    }
    
    private bool IsPositionAvailableForConnection(Vector2Int currentPos, string direction)
    {
        Vector2Int targetPos = GetPositionInDirection(currentPos, direction);
        return IsValidPosition(targetPos) && availablePositions.Contains(targetPos);
    }
    
    private Vector2Int GetPositionInDirection(Vector2Int currentPos, string direction)
    {
        switch (direction.ToLower())
        {
            case "north": return new Vector2Int(currentPos.x + 1, currentPos.y);
            case "south": return new Vector2Int(currentPos.x - 1, currentPos.y);
            case "east": return new Vector2Int(currentPos.x, currentPos.y + 1);
            case "west": return new Vector2Int(currentPos.x, currentPos.y - 1);
            default: return currentPos;
        }
    }
    
    private bool IsValidPosition(Vector2Int pos)
    {
        return pos.x >= 0 && pos.x < gridRows && pos.y >= 0 && pos.y < gridCols;
    }
    
    private void EstablishConnection(Room room1, Room room2, string direction)
    {
        // Configure room1's exit in the given direction
        bool north1 = room1.hasNorthExit || direction == "north";
        bool south1 = room1.hasSouthExit || direction == "south";
        bool east1 = room1.hasEastExit || direction == "east";
        bool west1 = room1.hasWestExit || direction == "west";
        room1.ConfigureExits(north1, south1, east1, west1);
        
        // Configure room2's exit in the opposite direction
        string oppositeDirection = GetOppositeDirection(direction);
        bool north2 = room2.hasNorthExit || oppositeDirection == "north";
        bool south2 = room2.hasSouthExit || oppositeDirection == "south";
        bool east2 = room2.hasEastExit || oppositeDirection == "east";
        bool west2 = room2.hasWestExit || oppositeDirection == "west";
        room2.ConfigureExits(north2, south2, east2, west2);
    }
    
    private List<Room> GetConnectedRooms(Room room, List<Room> allRooms)
    {
        List<Room> connectedRooms = new List<Room>();
        Vector2Int pos = room.gridPos;
        
        // Check each direction for connected rooms
        Vector2Int[] directions = { 
            new Vector2Int(1, 0),  // north
            new Vector2Int(-1, 0), // south
            new Vector2Int(0, 1),  // east
            new Vector2Int(0, -1)  // west
        };
        
        foreach (Vector2Int direction in directions)
        {
            Vector2Int adjacentPos = pos + direction;
            Room adjacentRoom = GetRoomAt(adjacentPos);
            
            if (adjacentRoom != null && allRooms.Contains(adjacentRoom))
            {
                connectedRooms.Add(adjacentRoom);
            }
        }
        
        return connectedRooms;
    }
    
    private void PlaceBossRoom(List<Room> allRooms)
    {
        Debug.Log($"PlaceBossRoom: Starting with {allRooms.Count} total rooms");
        Debug.Log($"Grid size: {gridRows}x{gridCols}");
        
        // Find rooms at the edges that are farthest from start
        List<Room> edgeRooms = new List<Room>();
        Vector2Int startPos = startRoom.gridPos;
        
        foreach (Room room in allRooms)
        {
            Vector2Int pos = room.gridPos;
            bool isEdge = (pos.x == 0 || pos.x == gridRows - 1 || pos.y == 0 || pos.y == gridCols - 1);
            
            Debug.Log($"Room at {pos}: isEdge={isEdge} (x:{pos.x}, y:{pos.y})");
            
            if (isEdge)
            {
                edgeRooms.Add(room);
            }
        }
        
        Debug.Log($"Found {edgeRooms.Count} edge rooms");
        
        if (edgeRooms.Count > 0)
        {
            // Choose the edge room farthest from start
            Room farthestRoom = edgeRooms[0];
            float maxDistance = Vector2Int.Distance(startPos, farthestRoom.gridPos);
            
            foreach (Room room in edgeRooms)
            {
                float distance = Vector2Int.Distance(startPos, room.gridPos);
                if (distance > maxDistance)
                {
                    maxDistance = distance;
                    farthestRoom = room;
                }
            }
            
            Debug.Log($"Farthest room selected at {farthestRoom.gridPos} with distance {maxDistance}");
            
            // Create boss room - either custom prefab or convert existing room
            LevelData currentLevel = GetCurrentLevelData();
            if (currentLevel.bossRoomPrefab != null)
            {
                Debug.Log("Using custom boss room prefab");
                // Use custom boss room prefab
                bossRoom = CreateCustomBossRoom(farthestRoom.gridPos, currentLevel.bossRoomPrefab);
                // Remove the original room that we're replacing
                roomGrid[farthestRoom.gridPos.x, farthestRoom.gridPos.y] = bossRoom;
                DestroyImmediate(farthestRoom.gameObject);
            }
            else
            {
                Debug.Log("Converting existing room to boss room");
                // Convert existing room to boss room (default behavior)
                bossRoom = farthestRoom;
                bossRoom.name = "Boss Room";
                bossRoom.SetRoomType(RoomType.Boss);
            }
            
            Debug.Log($"Boss room created: {bossRoom.name} at {bossRoom.gridPos}");
            
            fightRooms.Remove(farthestRoom); // Remove from fight rooms list
            
            // Add BossRoom component and configure it
            SetupBossRoomComponent();
            
            // Configure boss room connections
            ConfigureBossRoomConnections();
        }
        else
        {
            Debug.LogWarning("No edge rooms found for boss placement!");
            Debug.LogWarning($"Total rooms: {allRooms.Count}, Grid: {gridRows}x{gridCols}");
            foreach (Room room in allRooms)
            {
                Debug.LogWarning($"Room at {room.gridPos} - not on edge");
            }
            
            // Fallback: Use the room farthest from start (even if not on edge)
            if (allRooms.Count > 1)
            {
                Debug.Log("Using fallback boss placement: farthest room from start");
                Room farthestRoom = allRooms[1]; // Skip start room
                float maxDistance = Vector2Int.Distance(startPos, farthestRoom.gridPos);
                
                foreach (Room room in allRooms)
                {
                    if (room != startRoom) // Don't use start room as boss room
                    {
                        float distance = Vector2Int.Distance(startPos, room.gridPos);
                        if (distance > maxDistance)
                        {
                            maxDistance = distance;
                            farthestRoom = room;
                        }
                    }
                }
                
                Debug.Log($"Fallback boss room selected at {farthestRoom.gridPos} with distance {maxDistance}");
                
                // Convert to boss room
                bossRoom = farthestRoom;
                bossRoom.name = "Boss Room (Fallback)";
                bossRoom.SetRoomType(RoomType.Boss);
                
                fightRooms.Remove(farthestRoom);
                
                // Setup boss room
                SetupBossRoomComponent();
                ConfigureBossRoomConnections();
            }
            else
            {
                Debug.LogError("Cannot create boss room: Not enough rooms generated!");
            }
        }
    }
    
    private void PlaceSpecialRooms(List<Room> allRooms)
    {
        // Convert one random fight room to item room if we have enough rooms
        if (fightRooms.Count > 0)
        {
            Room randomRoom = fightRooms[Random.Range(0, fightRooms.Count)];
            itemRoom = randomRoom;
            itemRoom.name = "Item Room";
            itemRoom.SetRoomType(RoomType.Item);
            fightRooms.Remove(itemRoom);
            
            // Setup ItemRoom component
            SetupItemRoomComponent();
        }
    }
    
    private void ConfigureBossRoomConnections()
    {
        if (bossRoom == null) return;
        
        Vector2Int bossPos = bossRoom.gridPos;
        
        // Find all adjacent rooms
        List<string> possibleEntrances = new List<string>();
        List<Room> adjacentRooms = new List<Room>();
        List<string> adjacentDirections = new List<string>();
        
        // Check north
        Vector2Int northPos = new Vector2Int(bossPos.x + 1, bossPos.y);
        if (northPos.x < gridRows && roomGrid[northPos.x, northPos.y] != null)
        {
            possibleEntrances.Add("north");
            adjacentRooms.Add(roomGrid[northPos.x, northPos.y]);
            adjacentDirections.Add("south"); // Adjacent room needs south exit
        }
        
        // Check south
        Vector2Int southPos = new Vector2Int(bossPos.x - 1, bossPos.y);
        if (southPos.x >= 0 && roomGrid[southPos.x, southPos.y] != null)
        {
            possibleEntrances.Add("south");
            adjacentRooms.Add(roomGrid[southPos.x, southPos.y]);
            adjacentDirections.Add("north"); // Adjacent room needs north exit
        }
        
        // Check east
        Vector2Int eastPos = new Vector2Int(bossPos.x, bossPos.y + 1);
        if (eastPos.y < gridCols && roomGrid[eastPos.x, eastPos.y] != null)
        {
            possibleEntrances.Add("east");
            adjacentRooms.Add(roomGrid[eastPos.x, eastPos.y]);
            adjacentDirections.Add("west"); // Adjacent room needs west exit
        }
        
        // Check west
        Vector2Int westPos = new Vector2Int(bossPos.x, bossPos.y - 1);
        if (westPos.y >= 0 && roomGrid[westPos.x, westPos.y] != null)
        {
            possibleEntrances.Add("west");
            adjacentRooms.Add(roomGrid[westPos.x, westPos.y]);
            adjacentDirections.Add("east"); // Adjacent room needs east exit
        }
        
        if (possibleEntrances.Count == 0)
        {
            Debug.LogError("Boss room has no adjacent rooms!");
            return;
        }
        
        // Choose one entrance randomly
        int chosenIndex = Random.Range(0, possibleEntrances.Count);
        string chosenDirection = possibleEntrances[chosenIndex];
        Room connectingRoom = adjacentRooms[chosenIndex];
        string requiredExit = adjacentDirections[chosenIndex];
        
        // Configure boss room with single entrance
        bossRoom.ConfigureExits(
            chosenDirection == "north",
            chosenDirection == "south",
            chosenDirection == "east",
            chosenDirection == "west"
        );
        
        // Configure all adjacent rooms - only the chosen room gets exit to boss room
        for (int i = 0; i < adjacentRooms.Count; i++)
        {
            Room adjacentRoom = adjacentRooms[i];
            string exitDirection = adjacentDirections[i];
            
            if (i == chosenIndex)
            {
                // This is the chosen connecting room - ensure it has exit to boss room
                bool north = adjacentRoom.hasNorthExit || exitDirection == "north";
                bool south = adjacentRoom.hasSouthExit || exitDirection == "south";
                bool east = adjacentRoom.hasEastExit || exitDirection == "east";
                bool west = adjacentRoom.hasWestExit || exitDirection == "west";
                
                adjacentRoom.ConfigureExits(north, south, east, west);
            }
            else
            {
                // This is NOT the chosen room - disable exit to boss room and add wall tiles
                bool north = adjacentRoom.hasNorthExit && exitDirection != "north";
                bool south = adjacentRoom.hasSouthExit && exitDirection != "south";
                bool east = adjacentRoom.hasEastExit && exitDirection != "east";
                bool west = adjacentRoom.hasWestExit && exitDirection != "west";
                
                adjacentRoom.ConfigureExits(north, south, east, west);
                
                // Add wall tiles to block the potential connection to boss room
                AddWallTilesToBlockBossConnection(adjacentRoom, exitDirection);
            }
        }
    }
    
    private void AddWallTilesToBlockBossConnection(Room room, string blockedDirection)
    {
        if (room == null) return;
        
        // Force the room to regenerate its tiles with the new exit configuration
        // This will ensure wall tiles are placed where the exit was blocked
        room.UpdateExitTiles();
        
        Debug.Log($"Blocked {blockedDirection} exit in room {room.name} to prevent connection to boss room");
    }
    
    private void SetupBossRoomComponent()
    {
        if (bossRoom == null)
        {
            Debug.LogError("Cannot setup boss room: bossRoom is null!");
            return;
        }
        
        LevelData currentLevel = GetCurrentLevelData();
        
        // Boss functionality is now integrated into Room class
        // Just configure the boss prefab directly on the Room
        if (currentLevel.bossPrefab != null)
        {
            // Use the ConfigureBossPrefab method from Room class
            bossRoom.ConfigureBossPrefab(currentLevel.bossPrefab);
            
            // Log different messages based on room type
            if (currentLevel.bossRoomPrefab != null)
            {
                Debug.Log($"Custom boss room prefab '{currentLevel.bossRoomPrefab.name}' configured with boss: {currentLevel.bossPrefab.name}");
            }
            else
            {
                Debug.Log($"Default boss room layout configured with level {currentLevel.levelNumber} boss: {currentLevel.bossPrefab.name}");
            }
        }
        else
        {
            Debug.LogWarning($"No boss prefab assigned for level: {currentLevel.levelName}! Boss room will not spawn a boss.");
        }
    }
    
    private void SetupItemRoomComponent()
    {
        if (itemRoom == null)
        {
            Debug.LogError("Cannot setup ItemRoom component: itemRoom is null!");
            return;
        }
        
        // Check if ItemRoom component already exists
        ItemRoom itemRoomComponent = itemRoom.GetComponent<ItemRoom>();
        if (itemRoomComponent == null)
        {
            // Add ItemRoom component to the item room GameObject
            itemRoomComponent = itemRoom.gameObject.AddComponent<ItemRoom>();
            Debug.Log($"Added ItemRoom component to {itemRoom.name}");
        }
        
        // Configure the item room component with current level's item prefabs
        LevelData currentLevel = GetCurrentLevelData();
        
        // Check if we have ItemRoom component or use base Room class
        if (itemRoomComponent != null)
        {
            ConfigureItemRoomPrefabs(itemRoomComponent, currentLevel.itemPrefabs);
        }
        else
        {
            // Use base Room class configuration if ItemRoom component not found
            ConfigureRoomItemPrefabs(itemRoom, currentLevel.itemPrefabs);
        }
        
        Debug.Log($"Item room configured for level {currentLevel.levelNumber} ({currentLevel.levelName})");
    }
    
    private void ConfigureItemRoomPrefabs(ItemRoom itemRoomComponent, GameObject[] prefabs)
    {
        // Combine level-specific items with global item pools
        List<GameObject> allItemPrefabs = new List<GameObject>();
        
        // Add level-specific items first (if any)
        if (prefabs != null && prefabs.Length > 0)
        {
            allItemPrefabs.AddRange(prefabs);
        }
        
        // Add global item pools based on weighted selection
        if (useWeightedItemSelection)
        {
            allItemPrefabs.AddRange(GetWeightedItemSelection());
        }
        else
        {
            // Add all global items if not using weighted selection
            if (commonItemPrefabs != null) allItemPrefabs.AddRange(commonItemPrefabs);
            if (rareItemPrefabs != null) allItemPrefabs.AddRange(rareItemPrefabs);
            if (epicItemPrefabs != null) allItemPrefabs.AddRange(epicItemPrefabs);
        }
        
        // Use fallback items if nothing else is available
        if (allItemPrefabs.Count == 0 && fallbackItemPrefabs != null)
        {
            allItemPrefabs.AddRange(fallbackItemPrefabs);
        }
        
        // Configure the item room with the final item list
        if (itemRoomComponent != null)
        {
            itemRoomComponent.ConfigureItemPrefabs(allItemPrefabs.ToArray());
        }
        
        Debug.Log($"Item Room configured with {allItemPrefabs.Count} total item prefab(s)");
    }
    
    /// <summary>
    /// Configure item room using base Room class (for non-ItemRoom components)
    /// </summary>
    private void ConfigureRoomItemPrefabs(Room room, GameObject[] levelItemPrefabs)
    {
        if (room == null) return;
        
        // Combine level-specific items with global item pools
        List<GameObject> allItemPrefabs = new List<GameObject>();
        
        // Add level-specific items first (if any)
        if (levelItemPrefabs != null && levelItemPrefabs.Length > 0)
        {
            allItemPrefabs.AddRange(levelItemPrefabs);
        }
        
        // Add global item pools
        if (useWeightedItemSelection)
        {
            allItemPrefabs.AddRange(GetWeightedItemSelection());
        }
        else
        {
            if (commonItemPrefabs != null) allItemPrefabs.AddRange(commonItemPrefabs);
            if (rareItemPrefabs != null) allItemPrefabs.AddRange(rareItemPrefabs);
            if (epicItemPrefabs != null) allItemPrefabs.AddRange(epicItemPrefabs);
        }
        
        // Use fallback items if nothing else is available
        if (allItemPrefabs.Count == 0 && fallbackItemPrefabs != null)
        {
            allItemPrefabs.AddRange(fallbackItemPrefabs);
        }
        
        // Configure the room with the final item list using the new Room class method
        room.ConfigureItemPrefabs(allItemPrefabs.ToArray());
        
        Debug.Log($"Room {room.name} configured with {allItemPrefabs.Count} item prefab(s) using base Room class");
    }
    
    /// <summary>
    /// Get weighted selection of items based on rarity
    /// </summary>
    private List<GameObject> GetWeightedItemSelection()
    {
        List<GameObject> selectedItems = new List<GameObject>();
        
        // Normalize weights to percentages
        float totalWeight = itemRarityWeights.x + itemRarityWeights.y + itemRarityWeights.z;
        if (totalWeight <= 0) return selectedItems;
        
        float commonChance = itemRarityWeights.x / totalWeight;
        float rareChance = itemRarityWeights.y / totalWeight;
        float epicChance = itemRarityWeights.z / totalWeight;
        
        // Add items based on weighted probabilities
        float random = Random.value;
        
        if (random < commonChance && commonItemPrefabs != null && commonItemPrefabs.Length > 0)
        {
            // Select from common items
            selectedItems.AddRange(commonItemPrefabs);
        }
        else if (random < commonChance + rareChance && rareItemPrefabs != null && rareItemPrefabs.Length > 0)
        {
            // Select from rare items
            selectedItems.AddRange(rareItemPrefabs);
        }
        else if (epicItemPrefabs != null && epicItemPrefabs.Length > 0)
        {
            // Select from epic items
            selectedItems.AddRange(epicItemPrefabs);
        }
        else if (commonItemPrefabs != null && commonItemPrefabs.Length > 0)
        {
            // Fallback to common items if rare/epic are empty
            selectedItems.AddRange(commonItemPrefabs);
        }
        
        return selectedItems;
    }
    
    /// <summary>
    /// Test item prefab configuration (Editor only)
    /// </summary>
    [ContextMenu("Test Item Configuration")]
    private void TestItemConfiguration()
    {
        Debug.Log($"=== Item Prefab Configuration Test ===");
        Debug.Log($"Common Items: {(commonItemPrefabs?.Length ?? 0)}");
        Debug.Log($"Rare Items: {(rareItemPrefabs?.Length ?? 0)}");
        Debug.Log($"Epic Items: {(epicItemPrefabs?.Length ?? 0)}");
        Debug.Log($"Fallback Items: {(fallbackItemPrefabs?.Length ?? 0)}");
        Debug.Log($"Use Weighted Selection: {useWeightedItemSelection}");
        Debug.Log($"Rarity Weights: Common={itemRarityWeights.x}%, Rare={itemRarityWeights.y}%, Epic={itemRarityWeights.z}%");
        
        // Test weighted selection
        if (useWeightedItemSelection)
        {
            var testSelection = GetWeightedItemSelection();
            Debug.Log($"Test weighted selection returned {testSelection.Count} items");
        }
    }

    
    private Room CreateRoom(Vector2Int gridPos, string roomName)
    {
        // Get current level's room prefab
        LevelData currentLevel = GetCurrentLevelData();
        GameObject roomPrefab = currentLevel.universalRoomPrefab;
        
        if (roomPrefab == null)
        {
            Debug.LogError($"Room prefab is null for level: {currentLevel.levelName}!");
            return null;
        }
        
        // Calculate world position so that starting room (0,0) center is at world origin (0,0)
        // No offset needed - grid position (0,0) maps directly to world position (0,0)
        Vector3 worldPos = new Vector3(
            gridPos.y * RoomSpacingX, // x = column * room width
            gridPos.x * RoomSpacingY, // y = row * room height
            0
        );
        
        // Instantiate room
        GameObject roomObj = Instantiate(roomPrefab, worldPos, Quaternion.identity, transform);
        Room room = roomObj.GetComponent<Room>();
        
        if (room == null)
        {
            room = roomObj.AddComponent<Room>();
        }
        
        // Set room data
        room.SetGridPosition(gridPos);
        room.SetRoomType(RoomType.Normal); // Default to normal room type
        roomGrid[gridPos.x, gridPos.y] = room;
        roomObj.name = $"{roomName}_{gridPos.x}_{gridPos.y}";
        
        return room;
    }
    
    private Room CreateCustomBossRoom(Vector2Int gridPos, GameObject bossRoomPrefab)
    {
        if (bossRoomPrefab == null)
        {
            Debug.LogError("Custom boss room prefab is null!");
            return null;
        }
        
        // Calculate world position
        Vector3 worldPos = new Vector3(
            gridPos.y * RoomSpacingX, // x = column * room width
            gridPos.x * RoomSpacingY, // y = row * room height
            0
        );
        
        // Instantiate custom boss room
        GameObject bossRoomObj = Instantiate(bossRoomPrefab, worldPos, Quaternion.identity, transform);
        Room bossRoom = bossRoomObj.GetComponent<Room>();
        
        // If the prefab doesn't have a Room component, add one
        if (bossRoom == null)
        {
            bossRoom = bossRoomObj.AddComponent<Room>();
        }
        
        // Configure boss room
        bossRoom.SetGridPosition(gridPos);
        bossRoom.SetRoomType(RoomType.Boss);
        bossRoomObj.name = $"BossRoom_{gridPos.x}_{gridPos.y}";
        
        Debug.Log($"Created custom boss room from prefab: {bossRoomPrefab.name}");
        return bossRoom;
    }
    
    private void ConnectRooms()
    {
        // Connect all adjacent rooms with bidirectional exits, respecting boss room connections
        for (int row = 0; row < gridRows; row++)
        {
            for (int col = 0; col < gridCols; col++)
            {
                Room currentRoom = roomGrid[row, col];
                if (currentRoom == null || currentRoom == bossRoom) continue;
                
                // Declare exit variables
                bool north, south, east, west;
                
                // Special handling for starting room: simple adjacency check only
                if (currentRoom.roomType == RoomType.Start)
                {
                    // For start room, only connect to existing adjacent rooms (no boss room logic)
                    north = (row + 1 < gridRows) && (roomGrid[row + 1, col] != null);
                    south = (row - 1 >= 0) && (roomGrid[row - 1, col] != null);
                    east = (col + 1 < gridCols) && (roomGrid[row, col + 1] != null);
                    west = (col - 1 >= 0) && (roomGrid[row, col - 1] != null);
                    
                    Debug.Log($"Start room at ({row},{col}) - Adjacent rooms: North: {north}, South: {south}, East: {east}, West: {west}");
                }
                else
                {
                    // Check adjacent positions, but respect boss room connections (for non-start rooms)
                    north = (row + 1 < gridRows) && (roomGrid[row + 1, col] != null) && 
                            (roomGrid[row + 1, col] != bossRoom || currentRoom.hasNorthExit);
                    south = (row - 1 >= 0) && (roomGrid[row - 1, col] != null) && 
                            (roomGrid[row - 1, col] != bossRoom || currentRoom.hasSouthExit);
                    east = (col + 1 < gridCols) && (roomGrid[row, col + 1] != null) && 
                           (roomGrid[row, col + 1] != bossRoom || currentRoom.hasEastExit);
                    west = (col - 1 >= 0) && (roomGrid[row, col - 1] != null) && 
                           (roomGrid[row, col - 1] != bossRoom || currentRoom.hasWestExit);
                }
                
                // For non-start rooms, also preserve existing exits (boss room setup logic)
                if (currentRoom.roomType != RoomType.Start && 
                    (currentRoom.hasNorthExit || currentRoom.hasSouthExit || 
                     currentRoom.hasEastExit || currentRoom.hasWestExit))
                {
                    // Room already has some exits configured (probably by boss room setup)
                    // Preserve existing exits and only add new ones for non-boss connections
                    north = currentRoom.hasNorthExit || (north && roomGrid[row + 1, col] != bossRoom);
                    south = currentRoom.hasSouthExit || (south && roomGrid[row - 1, col] != bossRoom);
                    east = currentRoom.hasEastExit || (east && roomGrid[row, col + 1] != bossRoom);
                    west = currentRoom.hasWestExit || (west && roomGrid[row, col - 1] != bossRoom);
                }
                
                currentRoom.ConfigureExits(north, south, east, west);
            }
        }
        
        // Ensure all non-boss rooms remain accessible after boss room connection restrictions
        ValidateAndFixConnectivity();
    }
    
    private void ValidateAndFixConnectivity()
    {
        // Get all non-boss rooms that should be accessible
        List<Room> allNonBossRooms = new List<Room>();
        for (int row = 0; row < gridRows; row++)
        {
            for (int col = 0; col < gridCols; col++)
            {
                Room room = roomGrid[row, col];
                if (room != null && room != bossRoom)
                {
                    allNonBossRooms.Add(room);
                }
            }
        }
        
        if (allNonBossRooms.Count == 0) return;
        
        // Find all rooms reachable from start room (excluding boss room path)
        HashSet<Room> reachableRooms = GetReachableNonBossRooms(startRoom);
        
        // Find unreachable non-boss rooms
        List<Room> unreachableRooms = new List<Room>();
        foreach (Room room in allNonBossRooms)
        {
            if (!reachableRooms.Contains(room))
            {
                unreachableRooms.Add(room);
            }
        }
        
        if (unreachableRooms.Count > 0)
        {
            Debug.LogWarning($"Found {unreachableRooms.Count} unreachable non-boss rooms. Fixing connectivity...");
            ConnectUnreachableRooms(unreachableRooms, reachableRooms);
        }
        else
        {
            Debug.Log("All non-boss rooms are accessible!");
        }
    }
    
    private HashSet<Room> GetReachableNonBossRooms(Room startingRoom)
    {
        HashSet<Room> visited = new HashSet<Room>();
        Queue<Room> toVisit = new Queue<Room>();
        
        toVisit.Enqueue(startingRoom);
        visited.Add(startingRoom);
        
        while (toVisit.Count > 0)
        {
            Room currentRoom = toVisit.Dequeue();
            Vector2Int pos = currentRoom.gridPos;
            
            // Check all four directions for connected non-boss rooms
            CheckNeighborForConnectivity(pos.x + 1, pos.y, currentRoom.hasNorthExit, "south", visited, toVisit);
            CheckNeighborForConnectivity(pos.x - 1, pos.y, currentRoom.hasSouthExit, "north", visited, toVisit);
            CheckNeighborForConnectivity(pos.x, pos.y + 1, currentRoom.hasEastExit, "west", visited, toVisit);
            CheckNeighborForConnectivity(pos.x, pos.y - 1, currentRoom.hasWestExit, "east", visited, toVisit);
        }
        
        return visited;
    }
    
    private void CheckNeighborForConnectivity(int row, int col, bool hasExit, string requiredExit, HashSet<Room> visited, Queue<Room> toVisit)
    {
        if (!hasExit || row < 0 || row >= gridRows || col < 0 || col >= gridCols)
            return;
            
        Room neighbor = roomGrid[row, col];
        if (neighbor == null || neighbor == bossRoom || visited.Contains(neighbor))
            return;
            
        // Check if neighbor has the required exit back to us
        bool neighborHasExit = false;
        switch (requiredExit)
        {
            case "north": neighborHasExit = neighbor.hasNorthExit; break;
            case "south": neighborHasExit = neighbor.hasSouthExit; break;
            case "east": neighborHasExit = neighbor.hasEastExit; break;
            case "west": neighborHasExit = neighbor.hasWestExit; break;
        }
        
        if (neighborHasExit)
        {
            visited.Add(neighbor);
            toVisit.Enqueue(neighbor);
        }
    }

    public Room GetRoomAt(int row, int col)
    {
        if (row >= 0 && row < gridRows && col >= 0 && col < gridCols)
            return roomGrid[row, col];
        return null;
    }
    
    public Room GetRoomAt(Vector2Int gridPos)
    {
        return GetRoomAt(gridPos.x, gridPos.y);
    }
    
    private int GetTotalRoomCount()
    {
        int count = 0;
        if (roomGrid != null)
        {
            for (int row = 0; row < gridRows; row++)
            {
                for (int col = 0; col < gridCols; col++)
                {
                    if (roomGrid[row, col] != null) count++;
                }
            }
        }
        return count;
    }
    
    private HashSet<Room> GetReachableRooms()
    {
        HashSet<Room> visited = new HashSet<Room>();
        Queue<Room> toVisit = new Queue<Room>();
        
        if (startRoom == null) return visited;
        
        toVisit.Enqueue(startRoom);
        visited.Add(startRoom);
        
        while (toVisit.Count > 0)
        {
            Room current = toVisit.Dequeue();
            Vector2Int pos = current.gridPos;
            
            // Check neighbors with matching exits
            CheckAndAddNeighbor(pos.x + 1, pos.y, current.hasNorthExit, "south", visited, toVisit);
            CheckAndAddNeighbor(pos.x - 1, pos.y, current.hasSouthExit, "north", visited, toVisit);
            CheckAndAddNeighbor(pos.x, pos.y + 1, current.hasEastExit, "west", visited, toVisit);
            CheckAndAddNeighbor(pos.x, pos.y - 1, current.hasWestExit, "east", visited, toVisit);
        }
        
        return visited;
    }
    
    private void CheckAndAddNeighbor(int row, int col, bool hasExit, string requiredExit, HashSet<Room> visited, Queue<Room> toVisit)
    {
        if (!hasExit) return;
        if (row < 0 || row >= gridRows || col < 0 || col >= gridCols) return;
        
        Room neighbor = roomGrid[row, col];
        if (neighbor == null || visited.Contains(neighbor)) return;
        
        // Check if neighbor has matching exit
        bool hasMatchingExit = false;
        switch (requiredExit)
        {
            case "north": hasMatchingExit = neighbor.hasNorthExit; break;
            case "south": hasMatchingExit = neighbor.hasSouthExit; break;
            case "east": hasMatchingExit = neighbor.hasEastExit; break;
            case "west": hasMatchingExit = neighbor.hasWestExit; break;
        }
        
        if (hasMatchingExit)
        {
            visited.Add(neighbor);
            toVisit.Enqueue(neighbor);
        }
    }
    
    private List<Room> GetUnreachableRooms(HashSet<Room> reachableRooms)
    {
        List<Room> unreachableRooms = new List<Room>();
        
        for (int row = 0; row < gridRows; row++)
        {
            for (int col = 0; col < gridCols; col++)
            {
                Room room = roomGrid[row, col];
                if (room != null && !reachableRooms.Contains(room))
                {
                    unreachableRooms.Add(room);
                }
            }
        }
        
        return unreachableRooms;
    }
    
    private void ConnectUnreachableRooms(List<Room> unreachableRooms, HashSet<Room> reachableRooms)
    {
        foreach (Room unreachableRoom in unreachableRooms)
        {
            ConnectRoomToNetwork(unreachableRoom, reachableRooms);
        }
    }
    
    private void ConnectRoomToNetwork(Room unreachableRoom, HashSet<Room> reachableRooms)
    {
        Vector2Int pos = unreachableRoom.gridPos;
        
        // Try to connect to adjacent reachable rooms
        Vector2Int[] directions = { new Vector2Int(1, 0), new Vector2Int(-1, 0), new Vector2Int(0, 1), new Vector2Int(0, -1) };
        string[] exitNames = { "north", "south", "east", "west" };
        
        for (int i = 0; i < directions.Length; i++)
        {
            Vector2Int adjacentPos = pos + directions[i];
            
            if (adjacentPos.x >= 0 && adjacentPos.x < gridRows && adjacentPos.y >= 0 && adjacentPos.y < gridCols)
            {
                Room adjacentRoom = roomGrid[adjacentPos.x, adjacentPos.y];
                if (adjacentRoom != null && reachableRooms.Contains(adjacentRoom))
                {
                    // Create bidirectional connection
                    CreateBidirectionalConnection(unreachableRoom, adjacentRoom, exitNames[i]);
                    return;
                }
            }
        }
    }
    
    private void CreateBidirectionalConnection(Room room1, Room room2, string direction)
    {
        // Don't modify boss room exits
        if (room1 == bossRoom || room2 == bossRoom) return;
        
        // Add exit to room1 in the specified direction
        bool north1 = room1.hasNorthExit || direction == "north";
        bool south1 = room1.hasSouthExit || direction == "south";
        bool east1 = room1.hasEastExit || direction == "east";
        bool west1 = room1.hasWestExit || direction == "west";
        
        room1.ConfigureExits(north1, south1, east1, west1);
        
        // Add matching exit to room2 in the opposite direction
        string oppositeDirection = GetOppositeDirection(direction);
        bool north2 = room2.hasNorthExit || oppositeDirection == "north";
        bool south2 = room2.hasSouthExit || oppositeDirection == "south";
        bool east2 = room2.hasEastExit || oppositeDirection == "east";
        bool west2 = room2.hasWestExit || oppositeDirection == "west";
        
        room2.ConfigureExits(north2, south2, east2, west2);
    }
    
    private string GetOppositeDirection(string direction)
    {
        switch (direction.ToLower())
        {
            case "north": return "south";
            case "south": return "north";
            case "east": return "west";
            case "west": return "east";
            default: return "";
        }
    }
    
    private void ConfigureBossRoomExits(Room bossRoom, bool north, bool south, bool east, bool west)
    {
        // Count possible entrances
        List<string> directions = new List<string>();
        if (north) directions.Add("north");
        if (south) directions.Add("south");
        if (east) directions.Add("east");
        if (west) directions.Add("west");
        
        if (directions.Count == 0)
        {
            Debug.LogWarning("Boss room has no adjacent rooms!");
            bossRoom.ConfigureExits(false, false, false, false);
            return;
        }
        
        // Choose one entrance randomly
        string chosenDirection = directions[Random.Range(0, directions.Count)];
        
        bossRoom.ConfigureExits(
            chosenDirection == "north",
            chosenDirection == "south",
            chosenDirection == "east",
            chosenDirection == "west"
        );
        
        // Ensure the adjacent room has the corresponding exit to the boss room
        Vector2Int bossPos = bossRoom.gridPos;
        EnsureBossRoomAccess(bossPos, chosenDirection);
    }
    
    private void EnsureBossRoomAccess(Vector2Int bossPos, string bossEntrance)
    {
        // Find the room that should connect to the boss room and ensure it has the right exit
        Vector2Int adjacentPos = bossPos;
        string requiredExit = "";
        
        switch (bossEntrance)
        {
            case "north":
                adjacentPos = new Vector2Int(bossPos.x + 1, bossPos.y); // Room to the north
                requiredExit = "south"; // That room needs a south exit
                break;
            case "south":
                adjacentPos = new Vector2Int(bossPos.x - 1, bossPos.y); // Room to the south
                requiredExit = "north"; // That room needs a north exit
                break;
            case "east":
                adjacentPos = new Vector2Int(bossPos.x, bossPos.y + 1); // Room to the east
                requiredExit = "west"; // That room needs a west exit
                break;
            case "west":
                adjacentPos = new Vector2Int(bossPos.x, bossPos.y - 1); // Room to the west
                requiredExit = "east"; // That room needs an east exit
                break;
        }
        
        // Ensure the adjacent room has the required exit
        if (adjacentPos.x >= 0 && adjacentPos.x < gridRows && adjacentPos.y >= 0 && adjacentPos.y < gridCols)
        {
            Room adjacentRoom = roomGrid[adjacentPos.x, adjacentPos.y];
            if (adjacentRoom != null)
            {
                // Add the required exit to the adjacent room
                bool north = adjacentRoom.hasNorthExit || requiredExit == "north";
                bool south = adjacentRoom.hasSouthExit || requiredExit == "south";
                bool east = adjacentRoom.hasEastExit || requiredExit == "east";
                bool west = adjacentRoom.hasWestExit || requiredExit == "west";
                
                adjacentRoom.ConfigureExits(north, south, east, west);
            }
        }
    }
    
    private void ValidateConnectivity()
    {
        if (startRoom == null)
        {
            Debug.LogWarning("No start room to validate connectivity from");
            return;
        }
        
        // Simple BFS to check reachability
        HashSet<Room> visited = new HashSet<Room>();
        Queue<Room> toVisit = new Queue<Room>();
        
        toVisit.Enqueue(startRoom);
        visited.Add(startRoom);
        
        while (toVisit.Count > 0)
        {
            Room current = toVisit.Dequeue();
            Vector2Int pos = current.gridPos;
            
            // Check neighbors with matching exits
            CheckNeighbor(pos.x + 1, pos.y, current.hasNorthExit, "south", visited, toVisit);
            CheckNeighbor(pos.x - 1, pos.y, current.hasSouthExit, "north", visited, toVisit);
            CheckNeighbor(pos.x, pos.y + 1, current.hasEastExit, "west", visited, toVisit);
            CheckNeighbor(pos.x, pos.y - 1, current.hasWestExit, "east", visited, toVisit);
        }
        
        int totalRooms = GetTotalRoomCount();
        Debug.Log($"Connectivity check: {visited.Count}/{totalRooms} rooms reachable from start");
        
        if (visited.Count != totalRooms)
        {
            Debug.LogWarning("Some rooms are not reachable from start room!");
        }
    }
    
    private void CheckNeighbor(int row, int col, bool hasExit, string requiredExit, HashSet<Room> visited, Queue<Room> toVisit)
    {
        if (!hasExit) return;
        if (row < 0 || row >= gridRows || col < 0 || col >= gridCols) return;
        
        Room neighbor = roomGrid[row, col];
        if (neighbor == null || visited.Contains(neighbor)) return;
        
        // Check if neighbor has matching exit
        bool hasMatchingExit = false;
        switch (requiredExit)
        {
            case "north": hasMatchingExit = neighbor.hasNorthExit; break;
            case "south": hasMatchingExit = neighbor.hasSouthExit; break;
            case "east": hasMatchingExit = neighbor.hasEastExit; break;
            case "west": hasMatchingExit = neighbor.hasWestExit; break;
        }
        
        if (hasMatchingExit)
        {
            visited.Add(neighbor);
            toVisit.Enqueue(neighbor);
        }
    }

    // Gizmos for visualization
    private void OnDrawGizmos()
    {
        if (roomGrid == null) return;
        
        Gizmos.color = Color.white;
        
        for (int row = 0; row < gridRows; row++)
        {
            for (int col = 0; col < gridCols; col++)
            {
                Vector3 pos = new Vector3(col * RoomSpacingX, row * RoomSpacingY, 0);
                
                if (roomGrid[row, col] != null)
                {
                    // Different colors for different room types
                    if (roomGrid[row, col] == startRoom)
                        Gizmos.color = Color.green;
                    else if (roomGrid[row, col] == bossRoom)
                        Gizmos.color = Color.red;
                    else if (roomGrid[row, col] == itemRoom)
                        Gizmos.color = Color.yellow;
                    else
                        Gizmos.color = Color.white;
                    
                    Gizmos.DrawWireCube(pos, new Vector3(RoomSpacingX * 0.8f, RoomSpacingY * 0.8f, 0.1f));
                }
                else
                {
                    Gizmos.color = Color.gray;
                    Gizmos.DrawWireCube(pos, new Vector3(RoomSpacingX * 0.3f, RoomSpacingY * 0.3f, 0.1f));
                }
            }
        }
    }
    
    // Public getter methods for FloorManager integration
    public Room[,] GetRoomGrid()
    {
        return roomGrid;
    }
    
    public Room GetStartRoom()
    {
        return startRoom;
    }
    
    public Room GetBossRoom()
    {
        return bossRoom;
    }
    
    public Room GetItemRoom()
    {
        return itemRoom;
    }
    
    // === LEVEL MANAGEMENT PUBLIC METHODS ===
    
    /// <summary>
    /// Generate a specific level by index
    /// </summary>
    public void GenerateLevel(int levelIndex)
    {
        SetCurrentLevel(levelIndex);
        ClearAndRegenerate();
    }
    
    /// <summary>
    /// Advance to the next level and regenerate
    /// </summary>
    public void AdvanceToNextLevel()
    {
        GenerateNextLevel();
    }
    
    /// <summary>
    /// Go back to the previous level and regenerate
    /// </summary>
    public void GoToPreviousLevel()
    {
        GeneratePreviousLevel();
    }
    
    /// <summary>
    /// Clear current layout and generate a new one with the same level
    /// </summary>
    public void RegenerateCurrentLevel()
    {
        ClearAndRegenerate();
    }
    
    /// <summary>
    /// Get information about the current level
    /// </summary>
    public string GetCurrentLevelInfo()
    {
        LevelData currentLevel = GetCurrentLevelData();
        return $"Level {currentLevel.levelNumber}: {currentLevel.levelName}";
    }
    
    /// <summary>
    /// Get the total number of available levels
    /// </summary>
    public int GetTotalLevels()
    {
        return levels.Count;
    }
    
    /// <summary>
    /// Get the current level index (0-based)
    /// </summary>
    public int GetCurrentLevelIndex()
    {
        return currentLevelIndex;
    }
    
    /// <summary>
    /// Check if there are previous levels available
    /// </summary>
    public bool HasPreviousLevel()
    {
        return currentLevelIndex > 0;
    }
    
    /// <summary>
    /// Check if current level uses a custom boss room prefab
    /// </summary>
    public bool IsUsingCustomBossRoom()
    {
        LevelData currentLevel = GetCurrentLevelData();
        return currentLevel.bossRoomPrefab != null;
    }
    
    /// <summary>
    /// Get information about current boss room configuration
    /// </summary>
    public string GetBossRoomInfo()
    {
        LevelData currentLevel = GetCurrentLevelData();
        
        string roomType = currentLevel.bossRoomPrefab != null ? 
            $"Custom Room: {currentLevel.bossRoomPrefab.name}" : 
            "Default Layout";
            
        string bossType = currentLevel.bossPrefab != null ? 
            $"Boss: {currentLevel.bossPrefab.name}" : 
            "No Boss";
            
        return $"{roomType} | {bossType}";
    }
    
    public List<Room> GetFightRooms()
    {
        return fightRooms;
    }
}