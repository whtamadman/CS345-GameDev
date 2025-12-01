using UnityEngine;

/// <summary>
/// Camera manager that can switch between room-following mode and fixed position mode
/// Used for dungeon gameplay (room follow) and temporary areas (fixed position)
/// </summary>
public class CameraManager : MonoBehaviour
{
    [Header("Camera Mode Settings")]
    [Tooltip("Current camera mode")]
    public CameraMode currentMode = CameraMode.RoomFollow;
    
    [Header("Fixed Position Settings")]
    [Tooltip("Fixed position for temporary areas (when not following rooms)")]
    public Vector3 fixedPosition = new Vector3(0, 0, -10);
    
    [Tooltip("Fixed rotation for temporary areas")]
    public Vector3 fixedRotation = new Vector3(0, 0, 0);
    
    [Tooltip("Speed of camera transitions between modes")]
    public float transitionSpeed = 2f;
    
    [Header("Component References")]
    [Tooltip("Reference to the CameraRoomFollow component")]
    public CameraRoomFollow roomFollowCamera;
    
    private Camera cameraComponent;
    private bool isTransitioning = false;
    
    public enum CameraMode
    {
        RoomFollow,     // Follow rooms in dungeon
        Fixed           // Fixed position for temporary areas
    }
    
    void Start()
    {
        cameraComponent = GetComponent<Camera>();
        
        if (cameraComponent == null)
        {
            Debug.LogError("CameraManager: No Camera component found!");
            return;
        }
        
        // Find room follow component if not assigned
        if (roomFollowCamera == null)
        {
            roomFollowCamera = GetComponent<CameraRoomFollow>();
        }
        
        // Set initial mode
        ApplyCameraMode(currentMode, false);
        
        Debug.Log($"CameraManager: Initialized in {currentMode} mode");
    }
    
    /// <summary>
    /// Switch to room following mode (for dungeon gameplay)
    /// </summary>
    public void SwitchToRoomFollow()
    {
        if (currentMode == CameraMode.RoomFollow) return;
        
        Debug.Log("CameraManager: Switching to Room Follow mode");
        currentMode = CameraMode.RoomFollow;
        ApplyCameraMode(CameraMode.RoomFollow, true);
    }
    
    /// <summary>
    /// Switch to fixed position mode (for temporary areas)
    /// </summary>
    public void SwitchToFixed()
    {
        if (currentMode == CameraMode.Fixed) return;
        
        Debug.Log("CameraManager: Switching to Fixed Position mode");
        currentMode = CameraMode.Fixed;
        ApplyCameraMode(CameraMode.Fixed, true);
    }
    
    /// <summary>
    /// Switch to fixed position with custom location
    /// </summary>
    /// <param name="position">Custom fixed position</param>
    /// <param name="rotation">Custom fixed rotation</param>
    public void SwitchToFixed(Vector3 position, Vector3 rotation)
    {
        fixedPosition = position;
        fixedRotation = rotation;
        SwitchToFixed();
    }
    
    /// <summary>
    /// Switch to fixed position with custom location and snap option
    /// </summary>
    /// <param name="position">Custom fixed position</param>
    /// <param name="rotation">Custom fixed rotation</param>
    /// <param name="snapImmediately">If true, snap immediately without transition</param>
    public void SwitchToFixed(Vector3 position, Vector3 rotation, bool snapImmediately)
    {
        fixedPosition = position;
        fixedRotation = rotation;
        
        if (currentMode == CameraMode.Fixed) return;
        
        Debug.Log("CameraManager: Switching to Fixed Position mode" + (snapImmediately ? " (immediate snap)" : ""));
        currentMode = CameraMode.Fixed;
        ApplyCameraMode(CameraMode.Fixed, !snapImmediately);
    }
    
    /// <summary>
    /// Apply the specified camera mode
    /// </summary>
    /// <param name="mode">Camera mode to apply</param>
    /// <param name="useTransition">Whether to use smooth transition</param>
    private void ApplyCameraMode(CameraMode mode, bool useTransition)
    {
        if (cameraComponent == null) return;
        
        switch (mode)
        {
            case CameraMode.RoomFollow:
                // Enable room following
                if (roomFollowCamera != null)
                {
                    roomFollowCamera.enabled = true;
                }
                
                Debug.Log("CameraManager: Room follow mode activated");
                break;
                
            case CameraMode.Fixed:
                // Disable room following
                if (roomFollowCamera != null)
                {
                    roomFollowCamera.enabled = false;
                }
                
                // Apply fixed position and settings
                if (useTransition)
                {
                    StartCoroutine(TransitionToFixed());
                }
                else
                {
                    transform.position = fixedPosition;
                    transform.eulerAngles = fixedRotation;
                }
                
                Debug.Log($"CameraManager: Fixed position mode activated at {fixedPosition}");
                break;
        }
    }
    
    /// <summary>
    /// Smooth transition to fixed position
    /// </summary>
    private System.Collections.IEnumerator TransitionToFixed()
    {
        isTransitioning = true;
        
        Vector3 startPos = transform.position;
        Vector3 startRot = transform.eulerAngles;
        
        float elapsedTime = 0f;
        float duration = 1f / transitionSpeed;
        
        while (elapsedTime < duration)
        {
            float t = elapsedTime / duration;
            t = Mathf.SmoothStep(0f, 1f, t); // Smooth curve
            
            // Interpolate position and rotation
            transform.position = Vector3.Lerp(startPos, fixedPosition, t);
            transform.eulerAngles = Vector3.Lerp(startRot, fixedRotation, t);
            
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        
        // Ensure final values are exact
        transform.position = fixedPosition;
        transform.eulerAngles = fixedRotation;
        
        isTransitioning = false;
    }
    

    
    /// <summary>
    /// Set the fixed position (useful for teleporter positioning)
    /// </summary>
    /// <param name="position">New fixed position</param>
    public void SetFixedPosition(Vector3 position)
    {
        fixedPosition = position;
        if (currentMode == CameraMode.Fixed && !isTransitioning)
        {
            transform.position = fixedPosition;
        }
    }
    
    /// <summary>
    /// Get current camera mode
    /// </summary>
    /// <returns>Current camera mode</returns>
    public CameraMode GetCurrentMode()
    {
        return currentMode;
    }
    
    /// <summary>
    /// Check if camera is currently transitioning
    /// </summary>
    /// <returns>True if transitioning between modes</returns>
    public bool IsTransitioning()
    {
        return isTransitioning;
    }
    
    /// <summary>
    /// Manual mode switch (for testing)
    /// </summary>
    [ContextMenu("Switch to Room Follow")]
    public void ManualSwitchToRoomFollow()
    {
        SwitchToRoomFollow();
    }
    
    /// <summary>
    /// Manual mode switch (for testing)
    /// </summary>
    [ContextMenu("Switch to Fixed")]
    public void ManualSwitchToFixed()
    {
        SwitchToFixed();
    }
    
    /// <summary>
    /// Use current position as fixed position
    /// </summary>
    [ContextMenu("Set Current as Fixed Position")]
    public void SetCurrentAsFixedPosition()
    {
        fixedPosition = transform.position;
        fixedRotation = transform.eulerAngles;
        Debug.Log($"CameraManager: Set current position as fixed: {fixedPosition}");
    }
    
    void OnDrawGizmosSelected()
    {
        // Draw fixed position
        Gizmos.color = currentMode == CameraMode.Fixed ? Color.green : Color.yellow;
        Gizmos.DrawWireCube(fixedPosition, Vector3.one * 0.5f);
    }
}