using UnityEngine;
using System.Collections.Generic;

public class DungeonGenerator : MonoBehaviour
{
    [Header("Grid Settings")]
    public int gridRows = 3;
    public int gridCols = 4;
    public Vector2Int roomSizeInTiles = new Vector2Int(16, 12);
    public float globalGridCellSize = 0.4f;
    
    [Header("Room Prefab")]
    public GameObject universalRoomPrefab;
    
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
        // Find rooms at the edges that are farthest from start
        List<Room> edgeRooms = new List<Room>();
        Vector2Int startPos = startRoom.gridPos;
        
        foreach (Room room in allRooms)
        {
            Vector2Int pos = room.gridPos;
            bool isEdge = (pos.x == 0 || pos.x == gridRows - 1 || pos.y == 0 || pos.y == gridCols - 1);
            
            if (isEdge)
            {
                edgeRooms.Add(room);
            }
        }
        
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
            
            // Convert the farthest room to boss room
            bossRoom = farthestRoom;
            bossRoom.name = "Boss Room";
            fightRooms.Remove(bossRoom);
            
            // Configure boss room connections
            ConfigureBossRoomConnections();
        }
        else
        {
            Debug.LogWarning("No edge rooms found for boss placement!");
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
            fightRooms.Remove(itemRoom);
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
    

    
    private Room CreateRoom(Vector2Int gridPos, string roomName)
    {
        if (universalRoomPrefab == null)
        {
            Debug.LogError("Universal room prefab is null!");
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
        GameObject roomObj = Instantiate(universalRoomPrefab, worldPos, Quaternion.identity, transform);
        Room room = roomObj.GetComponent<Room>();
        
        if (room == null)
        {
            room = roomObj.AddComponent<Room>();
        }
        
        // Set room data
        room.SetGridPosition(gridPos);
        roomGrid[gridPos.x, gridPos.y] = room;
        roomObj.name = $"{roomName}_{gridPos.x}_{gridPos.y}";
        
        return room;
    }
    
    private void ConnectRooms()
    {
        // Connect all adjacent rooms with bidirectional exits
        for (int row = 0; row < gridRows; row++)
        {
            for (int col = 0; col < gridCols; col++)
            {
                Room currentRoom = roomGrid[row, col];
                if (currentRoom == null || currentRoom == bossRoom) continue;
                
                // Check adjacent positions
                bool north = (row + 1 < gridRows) && (roomGrid[row + 1, col] != null);
                bool south = (row - 1 >= 0) && (roomGrid[row - 1, col] != null);
                bool east = (col + 1 < gridCols) && (roomGrid[row, col + 1] != null);
                bool west = (col - 1 >= 0) && (roomGrid[row, col - 1] != null);
                
                currentRoom.ConfigureExits(north, south, east, west);
            }
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
    
    public List<Room> GetFightRooms()
    {
        return fightRooms;
    }
}