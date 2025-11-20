using UnityEngine;

/// <summary>
/// Post-generation processor that adds obstacles to rooms after dungeon generation
/// </summary>
public class PostGenerationObstacleProcessor : MonoBehaviour
{
    [Header("Obstacle Processing")]
    [SerializeField] private bool processOnStart = false;
    [SerializeField] private float delayAfterGeneration = 1.0f;
    
    [Header("Auto-Setup")]
    [SerializeField] private bool autoAddGeneratorsToRooms = true;
    [SerializeField] private RoomObstacleGenerator obstacleGeneratorPrefab;
    
    private DungeonGenerator dungeonGenerator;
    
    void Start()
    {
        dungeonGenerator = FindObjectOfType<DungeonGenerator>();
        
        if (processOnStart)
        {
            Invoke(nameof(ProcessObstacles), delayAfterGeneration);
        }
    }
    
    /// <summary>
    /// Process obstacles for all rooms after generation
    /// </summary>
    [ContextMenu("Process Room Obstacles")]
    public void ProcessObstacles()
    {
        SetupBreakableTileManager();
        
        if (autoAddGeneratorsToRooms)
        {
            AddObstacleGeneratorsToRooms();
        }
        
        GenerateObstaclesForAllRooms();
        
        // Initialize breakable tiles after obstacles are placed
        InitializeBreakableTiles();
    }
    
    /// <summary>
    /// Add RoomObstacleGenerator components to all rooms that don't have them
    /// </summary>
    private void AddObstacleGeneratorsToRooms()
    {
        Room[] allRooms = FindObjectsOfType<Room>();
        int addedGenerators = 0;
        
        foreach (Room room in allRooms)
        {
            // Skip if room already has an obstacle generator
            if (room.GetComponent<RoomObstacleGenerator>() != null)
                continue;
            
            // Skip certain room types that shouldn't have obstacles
            if (ShouldSkipObstacles(room))
                continue;
            
            // Add obstacle generator component
            RoomObstacleGenerator generator = room.gameObject.AddComponent<RoomObstacleGenerator>();
            
            // Copy settings from prefab if available
            if (obstacleGeneratorPrefab != null)
            {
                CopyObstacleGeneratorSettings(obstacleGeneratorPrefab, generator);
            }
            
            addedGenerators++;
        }
        
        Debug.Log($"PostGenerationObstacleProcessor: Added obstacle generators to {addedGenerators} rooms");
    }
    
    /// <summary>
    /// Determine if a room should skip obstacle generation
    /// </summary>
    private bool ShouldSkipObstacles(Room room)
    {
        // Skip start rooms and item rooms - they should stay clear
        if (room.roomType == RoomType.Start || room.roomType == RoomType.Item)
            return true;
        
        // Skip boss rooms - they'll have their own layout
        if (room is BossRoom)
            return true;
        
        return false;
    }
    
    /// <summary>
    /// Copy settings from prefab to new generator
    /// </summary>
    private void CopyObstacleGeneratorSettings(RoomObstacleGenerator source, RoomObstacleGenerator target)
    {
        // Use reflection to copy serialized fields
        var fields = typeof(RoomObstacleGenerator).GetFields(
            System.Reflection.BindingFlags.NonPublic | 
            System.Reflection.BindingFlags.Instance);
        
        foreach (var field in fields)
        {
            if (field.GetCustomAttributes(typeof(SerializeField), false).Length > 0)
            {
                object value = field.GetValue(source);
                field.SetValue(target, value);
            }
        }
    }
    
    /// <summary>
    /// Generate obstacles for all rooms with generators
    /// </summary>
    private void GenerateObstaclesForAllRooms()
    {
        RoomObstacleGenerator.GenerateObstaclesForAllRooms();
    }
    
    /// <summary>
    /// Setup BreakableTileManager in the scene
    /// </summary>
    private void SetupBreakableTileManager()
    {
        BreakableTileManager existing = FindFirstObjectByType<BreakableTileManager>();
        if (existing != null)
        {
            Debug.Log("BreakableTileManager already exists in scene");
            return;
        }
        
        // Create BreakableTileManager GameObject
        GameObject managerObj = new GameObject("BreakableTileManager");
        BreakableTileManager manager = managerObj.AddComponent<BreakableTileManager>();
        
        Debug.Log("Created BreakableTileManager for breakable block system");
    }
    
    /// <summary>
    /// Initialize breakable tiles after obstacles are placed
    /// </summary>
    private void InitializeBreakableTiles()
    {
        BreakableTileManager manager = FindObjectOfType<BreakableTileManager>();
        if (manager != null)
        {
            // Force reinitialize tiles to detect newly placed breakable blocks
            manager.ReinitializeTiles();
            Debug.Log("Initialized breakable tile system");
        }
    }
    
    /// <summary>
    /// Clear all obstacles from all rooms
    /// </summary>
    [ContextMenu("Clear All Obstacles")]
    public void ClearAllObstacles()
    {
        RoomObstacleGenerator[] generators = FindObjectsOfType<RoomObstacleGenerator>();
        foreach (RoomObstacleGenerator generator in generators)
        {
            generator.ClearObstacles();
        }
        
        Debug.Log($"Cleared obstacles from {generators.Length} rooms");
    }
}