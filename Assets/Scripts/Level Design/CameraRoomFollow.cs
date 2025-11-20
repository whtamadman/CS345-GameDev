using UnityEngine;

public class CameraRoomFollow : MonoBehaviour
{
    [Header("Camera Configuration")]
    public float panSpeed = 2f;
    public Vector2 roomOffset = Vector2.zero; // Offset for UI elements
    public bool smoothPanning = true;
    
    [Header("Camera Bounds")]
    public bool constrainToRoom = true;
    public Vector2 roomSize = new Vector2(14, 10); // Interior room size
    
    private Camera cameraComponent;
    private Vector3 targetPosition;
    private Vector3 currentRoomCenter;
    private bool isPanning = false;
    
    // Events
    public System.Action OnPanStarted;
    public System.Action OnPanCompleted;
    
    private void Awake()
    {
        cameraComponent = GetComponent<Camera>();
        if (cameraComponent == null)
        {
            Debug.LogError("CameraRoomFollow requires a Camera component!");
        }
        
        // Initialize target position to current position
        targetPosition = transform.position;
        currentRoomCenter = transform.position;
    }
    
    private void Start()
    {
        // Subscribe to floor manager events if available
        FloorManager floorManager = FindFirstObjectByType<FloorManager>();
        if (floorManager != null)
        {
            // Camera will be updated through room events via FloorManager
        }
    }
    
    private void Update()
    {
        if (smoothPanning)
        {
            UpdateCameraPosition();
        }
    }
    
    private void UpdateCameraPosition()
    {
        Vector3 currentPosition = transform.position;
        
        // Check if we need to pan
        if (Vector3.Distance(currentPosition, targetPosition) > 0.01f)
        {
            if (!isPanning)
            {
                isPanning = true;
                OnPanStarted?.Invoke();
            }
            
            // Smoothly move towards target
            Vector3 newPosition = Vector3.MoveTowards(currentPosition, targetPosition, panSpeed * Time.deltaTime);
            transform.position = newPosition;
        }
        else if (isPanning)
        {
            // Panning completed
            isPanning = false;
            transform.position = targetPosition; // Ensure exact positioning
            OnPanCompleted?.Invoke();
        }
    }
    
    public void SetRoomCenter(Vector3 roomCenter)
    {
        currentRoomCenter = roomCenter;
        
        // Calculate target position with offset
        Vector3 newTarget = roomCenter + new Vector3(roomOffset.x, roomOffset.y, 0);
        
        // Keep the camera's Z position
        newTarget.z = transform.position.z;
        
        // Apply room constraints if enabled
        if (constrainToRoom)
        {
            newTarget = ConstrainToRoomBounds(newTarget, roomCenter);
        }
        
        targetPosition = newTarget;
        
        // If smooth panning is disabled, move immediately
        if (!smoothPanning)
        {
            transform.position = targetPosition;
        }

    }
    
    private Vector3 ConstrainToRoomBounds(Vector3 desiredPosition, Vector3 roomCenter)
    {
        // Calculate camera bounds based on room size and camera settings
        float cameraHeight = cameraComponent.orthographicSize * 2f;
        float cameraWidth = cameraHeight * cameraComponent.aspect;
        
        // Room boundaries
        float roomLeft = roomCenter.x - roomSize.x / 2f;
        float roomRight = roomCenter.x + roomSize.x / 2f;
        float roomBottom = roomCenter.y - roomSize.y / 2f;
        float roomTop = roomCenter.y + roomSize.y / 2f;
        
        // Camera boundaries (edges of what camera can see)
        float cameraLeft = desiredPosition.x - cameraWidth / 2f;
        float cameraRight = desiredPosition.x + cameraWidth / 2f;
        float cameraBottom = desiredPosition.y - cameraHeight / 2f;
        float cameraTop = desiredPosition.y + cameraHeight / 2f;
        
        // Constrain camera position so it doesn't show outside room bounds
        if (cameraWidth < roomSize.x)
        {
            // Camera is smaller than room, keep it within room bounds
            if (cameraLeft < roomLeft)
                desiredPosition.x = roomLeft + cameraWidth / 2f;
            else if (cameraRight > roomRight)
                desiredPosition.x = roomRight - cameraWidth / 2f;
        }
        else
        {
            // Camera is larger than room, center it on room
            desiredPosition.x = roomCenter.x;
        }
        
        if (cameraHeight < roomSize.y)
        {
            // Camera is smaller than room, keep it within room bounds
            if (cameraBottom < roomBottom)
                desiredPosition.y = roomBottom + cameraHeight / 2f;
            else if (cameraTop > roomTop)
                desiredPosition.y = roomTop - cameraHeight / 2f;
        }
        else
        {
            // Camera is larger than room, center it on room
            desiredPosition.y = roomCenter.y;
        }
        
        return desiredPosition;
    }
    
    public void SetPanSpeed(float newSpeed)
    {
        panSpeed = Mathf.Max(0.1f, newSpeed);
    }
    
    public void SetRoomOffset(Vector2 newOffset)
    {
        roomOffset = newOffset;
        
        // Update target position with new offset
        SetRoomCenter(currentRoomCenter);
    }
    
    public void EnableSmoothPanning(bool enable)
    {
        smoothPanning = enable;
        
        if (!enable && isPanning)
        {
            // Jump to target immediately
            transform.position = targetPosition;
            isPanning = false;
            OnPanCompleted?.Invoke();
        }
    }
    
    public void EnableRoomConstraints(bool enable)
    {
        constrainToRoom = enable;
        
        // Recalculate position with new constraints
        SetRoomCenter(currentRoomCenter);
    }
    
    public void SetRoomSize(Vector2 newRoomSize)
    {
        roomSize = newRoomSize;
        
        // Recalculate position with new room size
        if (constrainToRoom)
        {
            SetRoomCenter(currentRoomCenter);
        }
    }
    
    public bool IsPanning()
    {
        return isPanning;
    }
    
    public Vector3 GetTargetPosition()
    {
        return targetPosition;
    }
    
    public Vector3 GetCurrentRoomCenter()
    {
        return currentRoomCenter;
    }
    
    // Manual camera control methods
    public void MoveToPositionImmediate(Vector3 position)
    {
        position.z = transform.position.z;
        transform.position = position;
        targetPosition = position;
        currentRoomCenter = position - new Vector3(roomOffset.x, roomOffset.y, 0);
        isPanning = false;
    }
    
    public void MoveToPosition(Vector3 position)
    {
        SetRoomCenter(position);
    }
    
    // Debug methods
    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    public void DebugSetRoomCenter(Vector3 center)
    {
        SetRoomCenter(center);
    }
    
    private void OnDrawGizmosSelected()
    {
        // Draw current room bounds
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(currentRoomCenter, new Vector3(roomSize.x, roomSize.y, 0));
        
        // Draw target position
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(targetPosition, 0.5f);
        
        // Draw camera view bounds
        if (cameraComponent != null)
        {
            Gizmos.color = Color.yellow;
            float height = cameraComponent.orthographicSize * 2f;
            float width = height * cameraComponent.aspect;
            Gizmos.DrawWireCube(transform.position, new Vector3(width, height, 0));
        }
    }
}