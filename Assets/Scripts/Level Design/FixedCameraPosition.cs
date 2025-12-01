using UnityEngine;

/// <summary>
/// Camera controller that maintains a fixed position instead of following rooms or player
/// Useful for overview shots, fixed perspective gameplay, or static camera angles
/// </summary>
public class FixedCameraPosition : MonoBehaviour
{
    [Header("Fixed Position Settings")]
    [Tooltip("The fixed world position where the camera should be positioned")]
    public Vector3 fixedPosition = new Vector3(0, 0, -10);
    
    [Tooltip("The fixed rotation for the camera (in euler angles)")]
    public Vector3 fixedRotation = new Vector3(0, 0, 0);
    
    [Tooltip("Apply the fixed position and rotation on Start")]
    public bool applyOnStart = true;
    
    [Tooltip("Continuously enforce the fixed position (prevents other scripts from moving camera)")]
    public bool enforcePosition = false;
    
    [Header("Camera Settings")]
    [Tooltip("Camera size (for orthographic cameras)")]
    public float cameraSize = 5f;
    
    [Tooltip("Set camera to orthographic mode")]
    public bool useOrthographic = true;
    
    [Header("Debug")]
    [Tooltip("Show the fixed position in scene view")]
    public bool showGizmos = true;
    
    [Tooltip("Color of the position gizmo")]
    public Color gizmoColor = Color.yellow;
    
    private Camera cameraComponent;
    private Vector3 initialPosition;
    private Vector3 initialRotation;
    
    void Start()
    {
        cameraComponent = GetComponent<Camera>();
        
        if (cameraComponent == null)
        {
            Debug.LogError("FixedCameraPosition: No Camera component found!");
            return;
        }
        
        // Store initial values
        initialPosition = transform.position;
        initialRotation = transform.eulerAngles;
        
        if (applyOnStart)
        {
            ApplyFixedPosition();
        }
        
        Debug.Log($"FixedCameraPosition: Camera set to fixed position {fixedPosition} with rotation {fixedRotation}");
    }
    
    void Update()
    {
        if (enforcePosition && cameraComponent != null)
        {
            // Continuously enforce the fixed position
            if (Vector3.Distance(transform.position, fixedPosition) > 0.01f ||
                Vector3.Distance(transform.eulerAngles, fixedRotation) > 0.01f)
            {
                ApplyFixedPosition();
            }
        }
    }
    
    /// <summary>
    /// Apply the fixed position and rotation to the camera
    /// </summary>
    public void ApplyFixedPosition()
    {
        if (cameraComponent == null) return;
        
        // Set position and rotation
        transform.position = fixedPosition;
        transform.eulerAngles = fixedRotation;
        
        // Configure camera settings
        if (useOrthographic)
        {
            cameraComponent.orthographic = true;
            cameraComponent.orthographicSize = cameraSize;
        }
        else
        {
            cameraComponent.orthographic = false;
            cameraComponent.fieldOfView = cameraSize; // Use cameraSize as FOV for perspective
        }
        
        Debug.Log($"FixedCameraPosition: Applied fixed position {fixedPosition}");
    }
    
    /// <summary>
    /// Set a new fixed position
    /// </summary>
    /// <param name="newPosition">New world position for the camera</param>
    public void SetFixedPosition(Vector3 newPosition)
    {
        fixedPosition = newPosition;
        if (applyOnStart)
        {
            ApplyFixedPosition();
        }
    }
    
    /// <summary>
    /// Set a new fixed rotation
    /// </summary>
    /// <param name="newRotation">New euler angles for the camera</param>
    public void SetFixedRotation(Vector3 newRotation)
    {
        fixedRotation = newRotation;
        if (applyOnStart)
        {
            ApplyFixedPosition();
        }
    }
    
    /// <summary>
    /// Set both position and rotation at once
    /// </summary>
    /// <param name="newPosition">New world position</param>
    /// <param name="newRotation">New euler angles</param>
    public void SetFixedTransform(Vector3 newPosition, Vector3 newRotation)
    {
        fixedPosition = newPosition;
        fixedRotation = newRotation;
        if (applyOnStart)
        {
            ApplyFixedPosition();
        }
    }
    
    /// <summary>
    /// Reset camera to its initial position and rotation
    /// </summary>
    public void ResetToInitial()
    {
        transform.position = initialPosition;
        transform.eulerAngles = initialRotation;
        Debug.Log("FixedCameraPosition: Reset to initial transform");
    }
    
    /// <summary>
    /// Use current camera position as the new fixed position
    /// </summary>
    [ContextMenu("Use Current Position as Fixed")]
    public void UseCurrentAsFixed()
    {
        fixedPosition = transform.position;
        fixedRotation = transform.eulerAngles;
        Debug.Log($"FixedCameraPosition: Set current transform as fixed - Position: {fixedPosition}, Rotation: {fixedRotation}");
    }
    
    /// <summary>
    /// Apply the fixed transform (for testing in editor)
    /// </summary>
    [ContextMenu("Apply Fixed Transform")]
    public void ApplyFixedTransformManual()
    {
        ApplyFixedPosition();
    }
    
    /// <summary>
    /// Center the camera on the dungeon (useful for overview shots)
    /// </summary>
    [ContextMenu("Center on Dungeon")]
    public void CenterOnDungeon()
    {
        DungeonGenerator dungeonGen = FindFirstObjectByType<DungeonGenerator>();
        if (dungeonGen != null)
        {
            // Calculate center of the dungeon grid
            float centerX = (dungeonGen.gridCols - 1) * dungeonGen.RoomSpacingX / 2f;
            float centerY = (dungeonGen.gridRows - 1) * dungeonGen.RoomSpacingY / 2f;
            
            Vector3 dungeonCenter = new Vector3(centerX, centerY, fixedPosition.z);
            SetFixedPosition(dungeonCenter);
            
            Debug.Log($"FixedCameraPosition: Centered on dungeon at {dungeonCenter}");
        }
        else
        {
            Debug.LogWarning("FixedCameraPosition: No DungeonGenerator found to center on");
        }
    }
    
    void OnDrawGizmosSelected()
    {
        if (!showGizmos) return;
        
        // Draw the fixed position
        Gizmos.color = gizmoColor;
        Gizmos.DrawWireCube(fixedPosition, Vector3.one * 0.5f);
        
        // Draw camera bounds for orthographic cameras
        if (useOrthographic && cameraComponent != null)
        {
            float aspect = cameraComponent.aspect;
            float height = cameraSize * 2f;
            float width = height * aspect;
            
            Vector3 size = new Vector3(width, height, 0.1f);
            Gizmos.color = Color.Lerp(gizmoColor, Color.white, 0.5f);
            Gizmos.DrawWireCube(fixedPosition, size);
        }
        
        // Draw direction arrow
        Gizmos.color = Color.red;
        Vector3 forward = Quaternion.Euler(fixedRotation) * Vector3.forward;
        Gizmos.DrawRay(fixedPosition, forward * 2f);
    }
}