using UnityEngine;

/// <summary>
/// Helper component for automatically setting up teleporters in boss rooms
/// Attach this to a Room with RoomType.Boss to automatically spawn a teleporter when the boss is defeated
/// </summary>
public class BossRoomTeleporter : MonoBehaviour
{
    [Header("Teleporter Configuration")]
    public GameObject teleporterPrefab; // The teleporter prefab to spawn
    public Vector3 teleporterOffset = Vector3.zero; // Offset from room center
    public bool spawnOnBossDefeat = true; // Spawn teleporter when boss is defeated
    
    [Header("Teleporter Settings")]
    public Vector3 teleportDestination = Vector3.zero;
    public bool useLocalOffset = false;
    public bool advanceToNextLevel = true;
    public bool clearCurrentDungeon = true;
    
    [Header("Level Transition Settings")]
    [Tooltip("Delay before level transition starts (gives time for boss defeat effects)")]
    public float levelTransitionDelay = 2.0f;
    [Tooltip("Clear all tilemaps before generating new level (ensures clean transition)")]
    public bool forceCompleteClearing = true;
    [Tooltip("How to position player after level advancement")]
    public PlayerPositionMode playerPositioning = PlayerPositionMode.StartRoom;
    [Tooltip("Custom position for player (used when playerPositioning is CustomPosition)")]
    public Vector3 customPlayerPosition = Vector3.zero;
    
    [Header("Camera Control Settings")]
    [Tooltip("Switch camera to fixed mode when teleporting out of dungeon")]
    public bool switchCameraToFixed = true;
    [Tooltip("Fixed camera position for temporary area (while dungeon regenerates)")]
    public Vector3 tempAreaCameraPosition = new Vector3(0, 0, -10);
    [Tooltip("Switch camera back to room follow when entering new level")]
    public bool returnCameraToRoomFollow = true;
    [Tooltip("Snap camera immediately to new position (no smooth transition)")]
    public bool snapCameraImmediately = true;
    
    [Header("Ending Configuration")]
    [Tooltip("Location to teleport to when no next level exists")]
    public Vector3 endingLocation = new Vector3(0, 0, 0);
    [Tooltip("Use ending location if no next level available")]
    public bool useEndingLocation = true;
    
    [Header("Cleanup Settings")]
    [Tooltip("Destroy this teleporter prefab after teleporting")]
    public bool destroyAfterTeleport = false;
    
    [Header("UI Configuration")]
    public GameObject popupTextPrefab; // Reference to popup prefab
    public Transform hudCanvas; // Reference to HUD canvas
    
    private Room room;
    private bool teleporterSpawned = false;
    
    void Start()
    {
        room = GetComponent<Room>();
        
        if (room == null)
        {
            Debug.LogError($"BossRoomTeleporter: No Room component found on {gameObject.name}!");
            return;
        }
        
        if (room.roomType != RoomType.Boss)
        {
            Debug.LogWarning($"BossRoomTeleporter: Room {gameObject.name} is not a boss room!");
            return;
        }
        
        // Subscribe to room cleared event
        if (room.OnRoomCleared != null)
        {
            room.OnRoomCleared += OnBossRoomCleared;
        }
        
        // If teleporter should be present from start, spawn it
        if (!spawnOnBossDefeat)
        {
            SpawnTeleporter();
        }
    }
    
    /// <summary>
    /// Called when the boss room is cleared
    /// </summary>
    private void OnBossRoomCleared(Room clearedRoom)
    {
        if (clearedRoom == room && spawnOnBossDefeat && !teleporterSpawned)
        {
            Debug.Log($"BossRoomTeleporter: Boss defeated in {room.name}, spawning teleporter");
            SpawnTeleporter();
        }
    }
    
    /// <summary>
    /// Spawn the teleporter in the boss room
    /// </summary>
    public void SpawnTeleporter()
    {
        if (teleporterPrefab == null)
        {
            Debug.LogError("BossRoomTeleporter: No teleporter prefab assigned!");
            return;
        }
        
        if (teleporterSpawned)
        {
            Debug.LogWarning("BossRoomTeleporter: Teleporter already spawned!");
            return;
        }
        
        // Calculate spawn position - always center of room regardless of offset
        Vector3 roomCenter = transform.position; // Room transform position is already the center
        Vector3 spawnPosition = roomCenter; // Place teleporter directly in room center
        
        // Spawn the teleporter
        GameObject teleporterObj = Instantiate(teleporterPrefab, spawnPosition, Quaternion.identity);
        Teleporter teleporter = teleporterObj.GetComponent<Teleporter>();
        
        if (teleporter != null)
        {
            // Configure the teleporter
            if (advanceToNextLevel)
            {
                // For level progression, configure positioning mode
                teleporter.ConfigureLevelProgression(true, clearCurrentDungeon);
                teleporter.teleportDelay = levelTransitionDelay; // Use custom delay for level transitions
                teleporter.positionMode = playerPositioning; // Set positioning mode
                teleporter.customLevelPosition = customPlayerPosition; // Set custom position if needed
                teleporter.teleporterName = "Level Exit";
                teleporter.interactionText = "Press E to advance to next level";
                
                // Configure camera control for level progression
                teleporter.switchCameraToFixed = switchCameraToFixed;
                teleporter.fixedCameraPosition = tempAreaCameraPosition;
                teleporter.returnCameraToRoomFollow = returnCameraToRoomFollow;
                teleporter.snapCameraImmediately = snapCameraImmediately;
                
                // Configure ending location and cleanup
                teleporter.endingLocation = endingLocation;
                teleporter.useEndingLocation = useEndingLocation;
                teleporter.destroyAfterTeleport = destroyAfterTeleport;
            }
            else
            {
                // For regular teleportation, use the specified destination
                teleporter.SetTeleportDestination(teleportDestination, useLocalOffset);
                teleporter.ConfigureLevelProgression(false, clearCurrentDungeon);
                teleporter.teleporterName = "Teleporter";
                teleporter.interactionText = "Press E to teleport";
                
                // Configure camera control for regular teleportation
                teleporter.switchCameraToFixed = switchCameraToFixed;
                teleporter.fixedCameraPosition = tempAreaCameraPosition;
                teleporter.snapCameraImmediately = snapCameraImmediately;
                
                // Configure cleanup
                teleporter.destroyAfterTeleport = destroyAfterTeleport;
            }
            
            // Set UI references if available
            if (popupTextPrefab != null)
            {
                teleporter.popupTextPrefab = popupTextPrefab;
            }
            
            if (hudCanvas != null)
            {
                teleporter.hudCanvas = hudCanvas;
            }
            
            Debug.Log($"BossRoomTeleporter: Spawned {(advanceToNextLevel ? "level progression" : "standard")} teleporter at {spawnPosition}");
        }
        else
        {
            Debug.LogError("BossRoomTeleporter: Teleporter prefab does not have Teleporter component!");
        }
        
        teleporterSpawned = true;
    }
    
    /// <summary>
    /// Manually spawn teleporter (for testing)
    /// </summary>
    [ContextMenu("Spawn Teleporter")]
    public void ManualSpawnTeleporter()
    {
        SpawnTeleporter();
    }
    
    /// <summary>
    /// Reset teleporter spawning state
    /// </summary>
    [ContextMenu("Reset Teleporter State")]
    public void ResetTeleporterState()
    {
        teleporterSpawned = false;
        
        // Find and destroy existing teleporter
        Teleporter existingTeleporter = FindFirstObjectByType<Teleporter>();
        if (existingTeleporter != null)
        {
            DestroyImmediate(existingTeleporter.gameObject);
            Debug.Log("BossRoomTeleporter: Removed existing teleporter");
        }
    }
    
    void OnDestroy()
    {
        // Unsubscribe from events
        if (room != null && room.OnRoomCleared != null)
        {
            room.OnRoomCleared -= OnBossRoomCleared;
        }
    }
    
    /// <summary>
    /// Draw gizmo to show teleporter spawn position
    /// </summary>
    private void OnDrawGizmosSelected()
    {
        Vector3 spawnPos = transform.position; // Always center of room
        
        // Draw teleporter spawn position
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireCube(spawnPos, Vector3.one * 0.5f);
        
        // Draw line to teleport destination
        Gizmos.color = Color.cyan;
        Vector3 destPos = useLocalOffset ? spawnPos + teleportDestination : teleportDestination;
        Gizmos.DrawLine(spawnPos, destPos);
        Gizmos.DrawWireSphere(destPos, 0.3f);
    }
}