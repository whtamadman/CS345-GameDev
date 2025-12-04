using UnityEngine;
using TMPro;
using System.Collections;

public enum PlayerPositionMode
{
    StartRoom,          // Position at start room (default behavior)
    SpecificDestination, // Use teleportDestination field
    CustomPosition,     // Use customLevelPosition field
    SameRelativePosition, // Try to maintain relative position in new level
    WorldOrigin         // Position at world origin (0,0,0)
}

public class Teleporter : MonoBehaviour
{
    [Header("Teleporter Configuration")]
    public Vector3 teleportDestination = Vector3.zero; // Where to teleport the player
    public bool useLocalOffset = false; // If true, teleport relative to current position
    
    [Header("Interaction Settings")]
    public GameObject popupTextPrefab; // UI popup prefab (reuse PowerUp popup)
    public Transform hudCanvas; // Canvas for UI elements
    public string teleporterName = "Teleporter";
    public string interactionText = "Press E to teleport";
    
    [Header("Visual Effects")]
    public GameObject teleportEffectPrefab; // Optional teleport effect
    public AudioClip teleportSound; // Optional teleport sound
    public float teleportDelay = 0.5f; // Delay before teleporting
    
    [Header("Level Progression")]
    public bool advanceToNextLevel = false; // If true, triggers level progression
    public bool clearCurrentDungeon = false; // If true, clears current dungeon before teleport
    
    [Header("Level Progression Positioning")]
    public PlayerPositionMode positionMode = PlayerPositionMode.StartRoom;
    public Vector3 customLevelPosition = Vector3.zero; // Used when positionMode is Custom
    
    [Header("Camera Control")]
    public bool switchCameraToFixed = false; // Switch camera to fixed mode when teleporting out of dungeon
    public Vector3 fixedCameraPosition = new Vector3(0, 0, -10); // Fixed camera position for temp areas
    public bool returnCameraToRoomFollow = false; // Switch camera back to room follow when entering new level
    public bool snapCameraImmediately = false; // Snap camera immediately to new position (no smooth transition)
    
    [Header("Ending Configuration")]
    public Vector3 endingLocation = new Vector3(0, 0, 0); // Location to teleport to when no next level exists
    public bool useEndingLocation = false; // Use ending location if no next level available
    
    [Header("Cleanup Settings")]
    public bool destroyAfterTeleport = false; // Destroy this teleporter after teleporting
    
    // Internal state
    private bool inTrigger = false;
    private bool popUpExists = false;
    private bool isTeleporting = false;
    private AudioSource audioSource;
    
    void Start()
    {
        popUpExists = false;
        audioSource = GetComponent<AudioSource>();
        
        // If no audio source exists and we have a sound, add one
        if (audioSource == null && teleportSound != null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
    }
    
    void Update()
    {
        // Handle teleporter interaction
        if (inTrigger && Input.GetKeyDown(KeyCode.E) && !isTeleporting)
        {
            StartCoroutine(HandleTeleportation());
        }
    }
    
    /// <summary>
    /// Handle the teleportation process with effects and delays
    /// </summary>
    private IEnumerator HandleTeleportation()
    {
        isTeleporting = true;
        
        // Play teleport sound if available
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayTeleporter();
        }
        else if (audioSource != null && teleportSound != null)
        {
            // Fallback to local audio source if AudioManager not available
            audioSource.PlayOneShot(teleportSound);
        }
        
        // Spawn teleport effect at current location
        if (teleportEffectPrefab != null)
        {
            Instantiate(teleportEffectPrefab, transform.position, Quaternion.identity);
        }
        
        // Handle level progression if enabled (this will reposition player automatically)
        if (advanceToNextLevel)
        {
            Debug.Log("Teleporter: Starting level progression...");
            HandleLevelProgression();
            
            // Wait for level progression to complete
            yield return new WaitForSeconds(0.5f);
            
            // Skip normal teleportation when advancing levels
            isTeleporting = false;
            Debug.Log("Teleporter: Level progression completed");
            yield break;
        }
        
        // Wait for teleport delay
        yield return new WaitForSeconds(teleportDelay);
        
        // Handle dungeon clearing if enabled (for regular teleports)
        if (clearCurrentDungeon)
        {
            Debug.Log("Teleporter: Clearing current dungeon before teleportation...");
            DungeonGenerator dungeonGen = FindFirstObjectByType<DungeonGenerator>();
            if (dungeonGen != null)
            {
                dungeonGen.ClearDungeon();
            }
            else
            {
                Debug.LogWarning("Teleporter: No DungeonGenerator found for dungeon clearing!");
            }
        }
        
        // Perform the actual teleportation first (move player to temp area)
        PerformTeleportation();
        
        // Switch camera to fixed mode AFTER player is moved (for temp areas outside dungeon)
        if (switchCameraToFixed)
        {
            Debug.Log("Teleporter: Switching camera to fixed position mode at: " + fixedCameraPosition);
            Debug.Log("Teleporter: Current camera position before switch: " + Camera.main.transform.position);
            
            // First, try to disable any room following components
            CameraRoomFollow roomFollow = FindFirstObjectByType<CameraRoomFollow>();
            if (roomFollow != null)
            {
                roomFollow.enabled = false;
                Debug.Log("Teleporter: Disabled CameraRoomFollow component");
            }
            
            // Disable any fixed camera position components
            FixedCameraPosition fixedCameraComp = Camera.main.GetComponent<FixedCameraPosition>();
            if (fixedCameraComp != null)
            {
                fixedCameraComp.enabled = false;
                Debug.Log("Teleporter: Disabled FixedCameraPosition component");
            }
            
            // Try CameraManager if available
            CameraManager cameraManager = FindFirstObjectByType<CameraManager>();
            if (cameraManager != null)
            {
                cameraManager.SwitchToFixed(fixedCameraPosition, Vector3.zero, true);
                Debug.Log("Teleporter: Used CameraManager to switch to fixed mode");
            }
            
            // Force camera position directly as final measure
            Camera.main.transform.position = fixedCameraPosition;
            Camera.main.transform.eulerAngles = Vector3.zero;
            
            Debug.Log("Teleporter: Camera forced to fixed position: " + Camera.main.transform.position);
        }
        
        // Spawn effect at destination
        if (teleportEffectPrefab != null)
        {
            Vector3 effectPos = useLocalOffset ? 
                Player.Instance.transform.position : 
                teleportDestination;
            Instantiate(teleportEffectPrefab, effectPos, Quaternion.identity);
        }
        
        // Destroy teleporter if requested
        if (destroyAfterTeleport)
        {
            Debug.Log("Teleporter: Destroying teleporter after use");
            Destroy(gameObject);
            yield break;
        }
        
        isTeleporting = false;
    }
    
    /// <summary>
    /// Perform the actual teleportation
    /// </summary>
    private void PerformTeleportation()
    {
        if (Player.Instance == null)
        {
            Debug.LogError("Teleporter: Player instance not found!");
            return;
        }
        
        Vector3 targetPosition;
        
        if (useLocalOffset)
        {
            // Teleport relative to current position
            targetPosition = Player.Instance.transform.position + teleportDestination;
        }
        else
        {
            // Teleport to absolute position
            targetPosition = teleportDestination;
        }
        
        // Move the player
        Player.Instance.transform.position = targetPosition;
        
        Debug.Log($"Teleporter: Player teleported to {targetPosition}");
    }
    
    /// <summary>
    /// Handle level progression and dungeon management
    /// </summary>
    private void HandleLevelProgression()
    {
        DungeonGenerator dungeonGen = FindFirstObjectByType<DungeonGenerator>();
        CameraManager cameraManager = FindFirstObjectByType<CameraManager>();
        
        if (dungeonGen == null)
        {
            Debug.LogWarning("Teleporter: No DungeonGenerator found for level progression!");
            return;
        }
        
        if (advanceToNextLevel)
        {
            Debug.Log("Teleporter: Advancing to next level...");
            
            // Check if next level exists
            bool hasNextLevel = dungeonGen.HasNextLevel();
            
            if (!hasNextLevel && useEndingLocation)
            {
                Debug.Log("Teleporter: No next level available, teleporting to ending location");
                
                // Clear dungeon if requested
                if (clearCurrentDungeon)
                {
                    dungeonGen.ClearDungeon();
                }
                
                // Teleport to ending location
                Player.Instance.transform.position = endingLocation;
                
                // Disable room following and switch camera to fixed mode at ending location
                CameraRoomFollow roomFollow = FindFirstObjectByType<CameraRoomFollow>();
                if (roomFollow != null)
                {
                    roomFollow.enabled = false;
                    Debug.Log("Teleporter: Disabled CameraRoomFollow for ending location");
                }
                
                // Calculate camera position for ending location (position camera to show the ending area)
                Vector3 endingCameraPos = endingLocation;
                endingCameraPos.z = -10; // Set appropriate Z position for camera
                
                Debug.Log($"Teleporter: Moving camera from temp area to ending location: {endingCameraPos}");
                
                // Force camera to ending position immediately
                Camera.main.transform.position = endingCameraPos;
                Camera.main.transform.eulerAngles = Vector3.zero;
                
                // Also update CameraManager if available to keep it in sync
                if (cameraManager != null)
                {
                    cameraManager.SwitchToFixed(endingCameraPos, Vector3.zero, true);
                    Debug.Log("Teleporter: Updated CameraManager to ending position");
                }
                
                Debug.Log($"Teleporter: Camera positioned at ending location: {Camera.main.transform.position}");
                
                Debug.Log($"Teleporter: Game completed! Player moved to ending location: {endingLocation}");
            }
            else if (hasNextLevel)
            {
                // Switch camera back to room follow for new level
                if (returnCameraToRoomFollow)
                {
                    Debug.Log("Teleporter: Restoring camera to room follow mode");
                    
                    // Wait for level to be fully generated before setting up camera
                    StartCoroutine(SetupCameraForNewLevel(dungeonGen, cameraManager));
                }
                
                // Ensure complete tilemap clearing before generating new level
                dungeonGen.ClearAllTilemaps();
                
                // AdvanceToNextLevel() automatically clears existing dungeon and generates new one
                dungeonGen.AdvanceToNextLevel();
                
                // Position player at the start room of the new level
                StartCoroutine(PositionPlayerAtStartRoom(dungeonGen));
            }
            else
            {
                Debug.LogWarning("Teleporter: No next level available and no ending location configured!");
            }
        }
        else if (clearCurrentDungeon)
        {
            Debug.Log("Teleporter: Clearing current dungeon...");
            dungeonGen.ClearDungeon();
        }
    }
    
    /// <summary>
    /// Setup camera for new level - ensures it follows the new start room
    /// </summary>
    private System.Collections.IEnumerator SetupCameraForNewLevel(DungeonGenerator dungeonGen, CameraManager cameraManager)
    {
        // Wait for level generation to complete
        yield return new WaitForSeconds(0.2f);
        
        // Find the new start room
        Room newStartRoom = dungeonGen.GetStartRoom();
        if (newStartRoom == null)
        {
            Debug.LogWarning("Teleporter: Could not find new start room for camera setup");
            yield break;
        }
        
        // Re-enable room follow component and set it to track the new start room
        CameraRoomFollow roomFollow = FindFirstObjectByType<CameraRoomFollow>();
        if (roomFollow != null)
        {
            roomFollow.enabled = true;
            
            // Update the room follow to track the new start room center
            roomFollow.SetRoomCenter(newStartRoom.transform.position);
            Debug.Log($"Teleporter: Set CameraRoomFollow to track new start room: {newStartRoom.name} at {newStartRoom.transform.position}");
        }
        
        // Use CameraManager if available
        if (cameraManager != null)
        {
            cameraManager.SwitchToRoomFollow();
        }
        
        // Ensure FloorManager is properly set up for the new level
        FloorManager floorManager = FindFirstObjectByType<FloorManager>();
        if (floorManager != null)
        {
            // Update FloorManager's camera controller reference to ensure it can update camera on room changes
            if (roomFollow != null)
            {
                floorManager.cameraController = roomFollow;
                Debug.Log("Teleporter: Updated FloorManager's camera controller reference");
            }
            
            // Force FloorManager to regenerate and re-subscribe to room events for new level
            // Wait a bit more for the dungeon to be fully generated
            yield return new WaitForSeconds(0.1f);
            
            // Get the new dungeon grid and force FloorManager to update
            Room[,] newDungeonGrid = dungeonGen.GetRoomGrid();
            if (newDungeonGrid != null)
            {
                // Force the FloorManager to recognize the new dungeon
                // This should trigger re-subscription to room events
                floorManager.RecreateLevel();
                Debug.Log("Teleporter: Forced FloorManager to recreate level and re-subscribe to room events");
            }
            
            Debug.Log("Teleporter: FloorManager setup completed for new level");
        }
        else
        {
            Debug.LogWarning("Teleporter: No FloorManager found - room following may not work properly");
        }
        
        // Snap camera to new start room position
        if (snapCameraImmediately && newStartRoom != null)
        {
            Vector3 startRoomPos = newStartRoom.transform.position;
            startRoomPos.z = Camera.main.transform.position.z; // Preserve Z position
            Camera.main.transform.position = startRoomPos;
            Debug.Log($"Teleporter: Snapped camera to new start room position: {startRoomPos}");
        }
    }
    
    /// <summary>
    /// Snap camera immediately to start room position after room follow is enabled
    /// </summary>
    private System.Collections.IEnumerator SnapCameraToStartRoom(DungeonGenerator dungeonGen)
    {
        // Wait one frame for room follow to be enabled
        yield return null;
        
        // Find start room and snap camera there
        Room startRoom = dungeonGen.GetStartRoom();
        if (startRoom != null)
        {
            Vector3 startRoomPos = startRoom.transform.position;
            startRoomPos.z = Camera.main.transform.position.z; // Preserve Z position
            Camera.main.transform.position = startRoomPos;
            Debug.Log("Teleporter: Snapped camera immediately to start room position");
        }
        else
        {
            Debug.LogWarning("Teleporter: Could not find start room for camera snap");
        }
    }
    
    /// <summary>
    /// Position player at start room after level generation
    /// </summary>
    private System.Collections.IEnumerator PositionPlayerAtStartRoom(DungeonGenerator dungeonGen)
    {
        Debug.Log("Teleporter: Waiting for level generation to complete...");
        
        // Wait multiple frames for the new dungeon to be fully generated
        yield return new WaitForSeconds(0.1f);
        
        // Try multiple times to get the start room (in case generation takes longer)
        Room startRoom = null;
        int attempts = 0;
        while (startRoom == null && attempts < 10)
        {
            startRoom = dungeonGen.GetStartRoom();
            if (startRoom == null)
            {
                Debug.Log($"Teleporter: Attempt {attempts + 1} - Start room not ready, waiting...");
                yield return new WaitForSeconds(0.1f);
                attempts++;
            }
        }
        
        // Try to find player
        if (Player.Instance == null)
        {
            Debug.LogWarning("Teleporter: Player.Instance is null, trying to find player in scene...");
            Player.Instance = FindFirstObjectByType<Player>();
        }
        
        if (Player.Instance != null)
        {
            Vector3 targetPosition = Vector3.zero;
            string positionDescription = "";
            
            switch (positionMode)
            {
                case PlayerPositionMode.StartRoom:
                    if (startRoom != null)
                    {
                        // Use room center position (room transform should already be at center)
                        targetPosition = startRoom.transform.position;
                        // Ensure Z position doesn't interfere with 2D gameplay
                        targetPosition.z = Player.Instance.transform.position.z;
                        positionDescription = $"start room center: {targetPosition}";
                    }
                    else
                    {
                        targetPosition = Vector3.zero;
                        positionDescription = "world origin (start room not found)";
                    }
                    break;
                    
                case PlayerPositionMode.SpecificDestination:
                    targetPosition = useLocalOffset ? 
                        Player.Instance.transform.position + teleportDestination : 
                        teleportDestination;
                    positionDescription = $"specific destination: {targetPosition}";
                    break;
                    
                case PlayerPositionMode.CustomPosition:
                    targetPosition = customLevelPosition;
                    positionDescription = $"custom position: {targetPosition}";
                    break;
                    
                case PlayerPositionMode.SameRelativePosition:
                    // Try to maintain relative position (advanced feature)
                    targetPosition = CalculateRelativePosition(dungeonGen);
                    positionDescription = $"relative position: {targetPosition}";
                    break;
                    
                case PlayerPositionMode.WorldOrigin:
                default:
                    targetPosition = Vector3.zero;
                    positionDescription = "world origin";
                    break;
            }
            
            Player.Instance.transform.position = targetPosition;
            Debug.Log($"Teleporter: Successfully positioned player at {positionDescription} after {attempts} attempts");
        }
        else
        {
            Debug.LogError($"Teleporter: Failed to position player after {attempts} attempts - StartRoom: {startRoom != null}, Player: {Player.Instance != null}");
            
            // Fallback: try to find any room and position there
            Room[] allRooms = FindObjectsByType<Room>(FindObjectsSortMode.None);
            if (allRooms.Length > 0 && Player.Instance != null)
            {
                Vector3 fallbackPosition = allRooms[0].transform.position;
                Player.Instance.transform.position = fallbackPosition;
                Debug.Log($"Teleporter: Fallback - positioned player at first available room: {fallbackPosition}");
            }
            else if (Player.Instance != null)
            {
                Player.Instance.transform.position = Vector3.zero;
                Debug.Log("Teleporter: Final fallback - positioned player at world origin");
            }
        }
    }
    
    /// <summary>
    /// Show interaction popup text
    /// </summary>
    public void ShowTeleporterText()
    {
        if (popupTextPrefab == null || hudCanvas == null)
        {
            Debug.LogWarning("Teleporter: Popup prefab or HUD canvas not assigned!");
            return;
        }
        
        GameObject popup = Instantiate(popupTextPrefab);
        popup.transform.SetParent(hudCanvas, false);
        
        TMP_Text textComponent = popup.GetComponentInChildren<TMP_Text>();
        if (textComponent != null)
        {
            textComponent.text = teleporterName + "\n" + interactionText;
        }
        
        Destroy(popup, 2f);
        StartCoroutine(UpdateResetVar(2f));
    }
    
    /// <summary>
    /// Reset popup existence flag after delay
    /// </summary>
    private IEnumerator UpdateResetVar(float delay)
    {
        yield return new WaitForSeconds(delay);
        popUpExists = false;
    }
    
    /// <summary>
    /// Handle player entering teleporter trigger
    /// </summary>
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            if (!popUpExists)
            {
                popUpExists = true;
                ShowTeleporterText();
            }
            inTrigger = true;
        }
    }
    
    /// <summary>
    /// Handle player exiting teleporter trigger
    /// </summary>
    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            inTrigger = false;
        }
    }
    
    /// <summary>
    /// Calculate relative position for maintaining player's relative location in new level
    /// </summary>
    private Vector3 CalculateRelativePosition(DungeonGenerator dungeonGen)
    {
        // This is a simplified version - you could make this more sophisticated
        Room startRoom = dungeonGen.GetStartRoom();
        if (startRoom != null)
        {
            // Position slightly offset from start room
            return startRoom.transform.position + new Vector3(2, 0, 0);
        }
        return Vector3.zero;
    }
    
    /// <summary>
    /// Set teleport destination (for runtime configuration)
    /// </summary>
    public void SetTeleportDestination(Vector3 destination, bool localOffset = false)
    {
        teleportDestination = destination;
        useLocalOffset = localOffset;
    }
    
    /// <summary>
    /// Configure level progression settings
    /// </summary>
    public void ConfigureLevelProgression(bool advanceLevel, bool clearDungeon = true)
    {
        advanceToNextLevel = advanceLevel;
        clearCurrentDungeon = clearDungeon;
    }
    
    /// <summary>
    /// Test teleportation (for debugging)
    /// </summary>
    [ContextMenu("Test Teleport")]
    public void TestTeleport()
    {
        if (Application.isPlaying)
        {
            StartCoroutine(HandleTeleportation());
        }
        else
        {
            Debug.Log($"Teleporter would teleport to: {teleportDestination} (Local offset: {useLocalOffset})");
        }
    }
    
    /// <summary>
    /// Visualize teleport destination in scene view
    /// </summary>
    private void OnDrawGizmosSelected()
    {
        // Draw teleport destination
        Gizmos.color = Color.cyan;
        Vector3 gizmoPos = useLocalOffset ? transform.position + teleportDestination : teleportDestination;
        Gizmos.DrawWireSphere(gizmoPos, 0.5f);
        Gizmos.DrawLine(transform.position, gizmoPos);
        
        // Draw teleporter range
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, 1.0f);
    }
}