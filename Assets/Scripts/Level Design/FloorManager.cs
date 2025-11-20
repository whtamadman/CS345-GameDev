using UnityEngine;

public class FloorManager : MonoBehaviour
{
    [Header("Floor Configuration")]
    public int currentFloor = 1;
    public int maxFloors = 10;
    
    [Header("Components")]
    public DungeonGenerator dungeonGenerator;
    public CameraRoomFollow cameraController;
    
    [Header("Player")]
    public Transform player;
    
    // Events
    public System.Action<int> OnFloorChanged;
    public System.Action<int> OnNewFloorGenerated;
    
    // Current dungeon data
    private Room[,] currentDungeon;
    private Room currentPlayerRoom;
    
    public Room GetCurrentPlayerRoom() => currentPlayerRoom;
    public int GetCurrentFloor() => currentFloor;
    public Room[,] GetCurrentDungeon() => currentDungeon;
    
    private void Awake()
    {
        // Get components if not assigned
        if (dungeonGenerator == null)
            dungeonGenerator = GetComponent<DungeonGenerator>();
        
        if (cameraController == null)
            cameraController = FindFirstObjectByType<CameraRoomFollow>();
        
        if (player == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
                player = playerObj.transform;
        }
    }
    
    private void Start()
    {
        // Generate the first dungeon floor
        GenerateNewFloor(currentFloor);
    }
    
    public void GenerateNewFloor(int floorNumber)
    {
        currentFloor = floorNumber;

        
        // Generate the dungeon
        if (dungeonGenerator != null)
        {
            dungeonGenerator.GenerateDungeon();
            currentDungeon = dungeonGenerator.GetRoomGrid();
            
            // Subscribe to room events
            SubscribeToRoomEvents();
            
            // Move player to start room
            MovePlayerToStart();
            
            OnNewFloorGenerated?.Invoke(currentFloor);
        }
        else
        {
            Debug.LogError("DungeonGenerator not found!");
        }
    }
    
    public void NextFloor()
    {
        if (currentFloor >= maxFloors)
        {
            Debug.Log("Maximum floor reached! Game complete!");
            OnGameComplete();
            return;
        }
        
        currentFloor++;
        OnFloorChanged?.Invoke(currentFloor);
        
        // Generate new floor
        GenerateNewFloor(currentFloor);
        
        Debug.Log($"Advanced to floor {currentFloor}");
    }
    
    public void RecreateLevel()
    {
        Debug.Log("Recreating level at origin (0,0)...");
        
        // Move dungeon generator to origin
        if (dungeonGenerator != null)
        {
            dungeonGenerator.transform.position = Vector3.zero;
        }
        
        // Clear existing dungeon and generate new one at current floor
        GenerateNewFloor(currentFloor);

    }
    
    public void MovePlayerToStart()
    {
        if (player == null || dungeonGenerator == null)
        {
            Debug.LogError("Player or DungeonGenerator not found!");
            return;
        }
        
        Room startRoom = dungeonGenerator.GetStartRoom();
        if (startRoom != null)
        {
            // Use the room's transform position as the center instead of GetCenter()
            // GetCenter() appears to be calculating an incorrect offset
            Vector3 startPosition = startRoom.transform.position;
            
            Debug.Log($"Using room transform position: {startPosition}");
            
            player.position = startPosition;
            
            // Set camera to follow start room
            if (cameraController != null)
            {
                cameraController.SetRoomCenter(startPosition);
            }
            
            // Force immediate camera update
            Camera mainCamera = Camera.main;
            if (mainCamera != null)
            {
                Vector3 cameraPosition = new Vector3(startPosition.x, startPosition.y, mainCamera.transform.position.z);
                mainCamera.transform.position = cameraPosition;
            }
        }
        else
        {
            Debug.LogError("Start room not found!");
        }
    }
    
    private void SubscribeToRoomEvents()
    {
        if (currentDungeon == null) return;
        
        // Subscribe to all room events
        for (int row = 0; row < currentDungeon.GetLength(0); row++)
        {
            for (int col = 0; col < currentDungeon.GetLength(1); col++)
            {
                Room room = currentDungeon[row, col];
                if (room != null)
                {
                    room.OnPlayerEntered += OnPlayerEnteredRoom;
                    room.OnPlayerExited += OnPlayerExitedRoom;
                    room.OnRoomCleared += OnRoomCleared;
                }
            }
        }
    }
    
    private void UnsubscribeFromRoomEvents()
    {
        if (currentDungeon == null) return;
        
        // Unsubscribe from all room events
        for (int row = 0; row < currentDungeon.GetLength(0); row++)
        {
            for (int col = 0; col < currentDungeon.GetLength(1); col++)
            {
                Room room = currentDungeon[row, col];
                if (room != null)
                {
                    room.OnPlayerEntered -= OnPlayerEnteredRoom;
                    room.OnPlayerExited -= OnPlayerExitedRoom;
                    room.OnRoomCleared -= OnRoomCleared;
                }
            }
        }
    }
    
    private void OnPlayerEnteredRoom(Room room)
    {
        Room previousRoom = currentPlayerRoom;
        currentPlayerRoom = room;
        
        // Update camera to follow the room (use transform position instead of GetCenter)
        if (cameraController != null)
        {
            Debug.Log($"   Updating camera to room position: {room.transform.position}");
            cameraController.SetRoomCenter(room.transform.position);
        }
        else
        {
            Debug.LogWarning($"   No camera controller found! Using fallback camera movement.");
            // Fallback: Move main camera directly if no camera controller
            Camera mainCamera = Camera.main;
            if (mainCamera != null)
            {
                Vector3 roomPosition = room.transform.position;
                Vector3 cameraPosition = new Vector3(roomPosition.x, roomPosition.y, mainCamera.transform.position.z);
                mainCamera.transform.position = cameraPosition;
                Debug.Log($"   Camera moved directly to: {cameraPosition}");
            }
        }
        
        // Show room exits for debugging
        Debug.Log($"   Room exits: N:{room.hasNorthExit} S:{room.hasSouthExit} E:{room.hasEastExit} W:{room.hasWestExit}");
    }
    
    private void OnPlayerExitedRoom(Room room)
    {
        Debug.Log($"🚪 ROOM EXIT: Player exited room at {room.gridPos} ('{room.name}') on floor {currentFloor}");
        Debug.Log($"   Player position: {player.position}");
    }
    
    private void OnRoomCleared(Room room)
    {
        Debug.Log($"Room at {room.gridPos} cleared on floor {currentFloor}");
        
        // Special handling for boss room
        BossRoom bossRoom = room as BossRoom;
        if (bossRoom != null)
        {
            Debug.Log("Boss room cleared! Floor complete!");
        }
    }
    
    protected virtual void OnGameComplete()
    {
        Debug.Log("Congratulations! You have completed all floors!");
        // Handle game completion logic here
        // Could show victory screen, credits, etc.
    }
    
    // Helper methods
    public Room GetRoomAt(Vector2Int gridPos)
    {
        if (dungeonGenerator != null)
        {
            return dungeonGenerator.GetRoomAt(gridPos);
        }
        return null;
    }
    
    public bool IsFloorComplete()
    {
        if (dungeonGenerator == null) return false;
        
        Room bossRoom = dungeonGenerator.GetBossRoom();
        return bossRoom != null && bossRoom.isCleared;
    }
    
    // Save/Load methods (for future implementation)
    public void SaveFloorProgress()
    {
        // TODO: Implement save system
        PlayerPrefs.SetInt("CurrentFloor", currentFloor);
        PlayerPrefs.Save();
    }
    
    public void LoadFloorProgress()
    {
        // TODO: Implement load system
        int savedFloor = PlayerPrefs.GetInt("CurrentFloor", 1);
        if (savedFloor != currentFloor)
        {
            GenerateNewFloor(savedFloor);
        }
    }
    
    private void OnDestroy()
    {
        // Unsubscribe from room events to prevent memory leaks
        UnsubscribeFromRoomEvents();
    }
    
    // Debug methods
    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    public void DebugNextFloor()
    {
        NextFloor();
    }
    
    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    public void DebugRegenerateFloor()
    {
        GenerateNewFloor(currentFloor);
    }
    
    [ContextMenu("Debug Room Triggers")]
    public void DebugRoomTriggers()
    {
        if (currentDungeon == null)
        {
            Debug.LogError("No dungeon generated yet!");
            return;
        }
        
        Debug.Log("=== ROOM TRIGGER DEBUG ===");
        
        for (int row = 0; row < currentDungeon.GetLength(0); row++)
        {
            for (int col = 0; col < currentDungeon.GetLength(1); col++)
            {
                Room room = currentDungeon[row, col];
                if (room != null)
                {
                    // Check if room has trigger collider
                    BoxCollider2D trigger = room.GetComponent<BoxCollider2D>();
                    if (trigger != null)
                    {
                        Debug.Log($"Room [{row},{col}] '{room.name}': Trigger OK - Size:{trigger.size}, Offset:{trigger.offset}, IsTrigger:{trigger.isTrigger}");
                    }
                    else
                    {
                        Debug.LogError($"Room [{row},{col}] '{room.name}': NO TRIGGER COLLIDER!");
                    }
                    
                    // Check room position
                    Debug.Log($"   Position: {room.transform.position}, Grid: {room.gridPos}");
                }
            }
        }
        
        // Check player setup
        if (player != null)
        {
            Debug.Log($"Player position: {player.position}");
            Debug.Log($"Player tag: {player.tag}");
            
            // Check if player has collider
            Collider2D playerCollider = player.GetComponent<Collider2D>();
            if (playerCollider != null)
            {
                Debug.Log($"Player collider: {playerCollider.GetType().Name}, IsTrigger: {playerCollider.isTrigger}");
            }
            else
            {
                Debug.LogError("Player has no Collider2D component!");
            }
        }
        else
        {
            Debug.LogError("Player not found!");
        }
    }
    
    [ContextMenu("Debug Current Room")]
    public void DebugCurrentRoom()
    {
        if (currentPlayerRoom != null)
        {
            Debug.Log($"Current room: {currentPlayerRoom.gridPos} ('{currentPlayerRoom.name}')");
            Debug.Log($"Room position: {currentPlayerRoom.transform.position}");
            Debug.Log($"Player in room: {currentPlayerRoom.playerInRoom}");
            Debug.Log($"Room cleared: {currentPlayerRoom.isCleared}");
        }
        else
        {
            Debug.Log("No current room set");
        }
        
        if (player != null)
        {
            Debug.Log($"Player position: {player.position}");
        }
    }
    
    [ContextMenu("Force Room Check")]
    public void ForceRoomCheck()
    {
        if (player == null || currentDungeon == null)
        {
            Debug.LogError("Player or dungeon not found!");
            return;
        }
        
        Vector3 playerPos = player.position;
        Debug.Log($"Checking which room contains player at {playerPos}");
        
        for (int row = 0; row < currentDungeon.GetLength(0); row++)
        {
            for (int col = 0; col < currentDungeon.GetLength(1); col++)
            {
                Room room = currentDungeon[row, col];
                if (room != null)
                {
                    BoxCollider2D trigger = room.GetComponent<BoxCollider2D>();
                    if (trigger != null)
                    {
                        Bounds bounds = trigger.bounds;
                        if (bounds.Contains(playerPos))
                        {
                            Debug.Log($"Player should be in room [{row},{col}] '{room.name}'");
                            Debug.Log($"Room bounds: {bounds}");
                            
                            // Manually trigger room entry
                            OnPlayerEnteredRoom(room);
                            return;
                        }
                    }
                }
            }
        }
        
        Debug.LogWarning("Player is not inside any room trigger!");
    }
}