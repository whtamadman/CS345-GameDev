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
    
    [Header("Enemy Level Scaling")]
    [SerializeField] private bool enableEnemyLevelScaling = true;
    [SerializeField] private float healthScalePerLevel = 0.2f; // 20% health increase per level
    [SerializeField] private float moveSpeedScalePerLevel = 0.1f; // 10% speed increase per level
    [SerializeField] private float reloadTimeScalePerLevel = -0.05f; // 5% faster reload per level (negative = faster)
    [SerializeField] private int maxScalingLevel = 10; // Cap scaling at this level
    
    private DungeonGenerator dungeonGenerator;
    private FloorManager floorManager;
    
    void Start()
    {
        dungeonGenerator = FindFirstObjectByType<DungeonGenerator>();
        floorManager = FindFirstObjectByType<FloorManager>();
        
        // Subscribe to new floor generation events
        if (floorManager != null)
        {
            floorManager.OnNewFloorGenerated += OnNewFloorGenerated;
            Debug.Log("PostGenerationObstacleProcessor: Subscribed to floor generation events");
        }
        else
        {
            Debug.LogWarning("PostGenerationObstacleProcessor: FloorManager not found! Will not auto-process obstacles on new levels.");
        }
        
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
        
        // Scale enemy stats based on current level
        if (enableEnemyLevelScaling)
        {
            ScaleEnemyStatsForCurrentLevel();
        }
        
        // Initialize breakable tiles after obstacles are placed
        InitializeBreakableTiles();
    }
    
    /// <summary>
    /// Add RoomObstacleGenerator components to all rooms that don't have them
    /// </summary>
    private void AddObstacleGeneratorsToRooms()
    {
        Room[] allRooms = FindObjectsByType<Room>(FindObjectsSortMode.None);
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
        if (room.roomType == RoomType.Boss)
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
        BreakableTileManager manager = FindFirstObjectByType<BreakableTileManager>();
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
        RoomObstacleGenerator[] generators = FindObjectsByType<RoomObstacleGenerator>(FindObjectsSortMode.None);
        foreach (RoomObstacleGenerator generator in generators)
        {
            generator.ClearObstacles();
        }
        
        Debug.Log($"Cleared obstacles from {generators.Length} rooms");
    }
    
    /// <summary>
    /// Scale enemy stats based on current level to increase difficulty
    /// </summary>
    private void ScaleEnemyStatsForCurrentLevel()
    {
        if (floorManager == null) return;
        
        int currentLevel = floorManager.GetCurrentFloor();
        int scalingLevel = Mathf.Min(currentLevel, maxScalingLevel); // Cap scaling
        
        Enemy[] allEnemies = FindObjectsByType<Enemy>(FindObjectsSortMode.None);
        int scaledEnemies = 0;
        
        foreach (Enemy enemy in allEnemies)
        {
            ScaleEnemyStats(enemy, scalingLevel);
            scaledEnemies++;
        }
        
        if (scaledEnemies > 0)
        {
            Debug.Log($"PostGenerationObstacleProcessor: Scaled stats for {scaledEnemies} enemies based on level {currentLevel}");
        }
    }
    
    /// <summary>
    /// Apply stat scaling to individual enemy
    /// </summary>
    private void ScaleEnemyStats(Enemy enemy, int level)
    {
        if (level <= 1) return; // No scaling for level 1
        
        int levelsToScale = level - 1; // Scale from level 2 onwards
        
        // Scale health (multiplicative)
        int originalHealth = enemy.health;
        float healthMultiplier = 1f + (healthScalePerLevel * levelsToScale);
        enemy.health = Mathf.RoundToInt(originalHealth * healthMultiplier);
        
        // Scale movement speed using reflection to access protected field
        var moveSpeedField = typeof(Enemy).GetField("moveSpeed", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (moveSpeedField != null)
        {
            float originalMoveSpeed = (float)moveSpeedField.GetValue(enemy);
            float speedMultiplier = 1f + (moveSpeedScalePerLevel * levelsToScale);
            moveSpeedField.SetValue(enemy, originalMoveSpeed * speedMultiplier);
        }
        
        // Scale reload time (faster shooting)
        var reloadTimeField = typeof(Enemy).GetField("reloadTime", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (reloadTimeField != null)
        {
            float originalReloadTime = (float)reloadTimeField.GetValue(enemy);
            float reloadMultiplier = 1f + (reloadTimeScalePerLevel * levelsToScale);
            float newReloadTime = Mathf.Max(0.1f, originalReloadTime * reloadMultiplier); // Min 0.1s reload
            reloadTimeField.SetValue(enemy, newReloadTime);
        }
    }
    
    /// <summary>
    /// Event handler for when a new floor is generated
    /// </summary>
    private void OnNewFloorGenerated(int floorNumber)
    {
        Debug.Log($"PostGenerationObstacleProcessor: New floor {floorNumber} generated, processing obstacles...");
        
        // Use invoke with delay to ensure dungeon generation is completely finished
        Invoke(nameof(ProcessObstacles), delayAfterGeneration);
    }
    
    /// <summary>
    /// Clean up event subscriptions when destroyed
    /// </summary>
    private void OnDestroy()
    {
        if (floorManager != null)
        {
            floorManager.OnNewFloorGenerated -= OnNewFloorGenerated;
        }
    }
}