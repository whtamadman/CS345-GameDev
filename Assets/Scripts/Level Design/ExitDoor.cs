using UnityEngine;

public class ExitDoor : MonoBehaviour
{
    [Header("Door Configuration")]
    public bool isLocked = false;
    
    [Header("Visual Components")]
    public GameObject doorVisual; // Visual representation of the door
    public Collider2D doorCollider; // Collider that blocks player movement
    
    [Header("Animation")]
    public Animator doorAnimator; // Optional animator for door open/close animations
    
    private void Awake()
    {
        // Get components if not assigned
        if (doorCollider == null)
            doorCollider = GetComponent<Collider2D>();
        
        if (doorVisual == null)
            doorVisual = gameObject;
            
        if (doorAnimator == null)
            doorAnimator = GetComponent<Animator>();
    }
    
    private void Start()
    {
        // Initialize door state
        if (isLocked)
        {
            Lock();
        }
        else
        {
            Unlock();
        }
    }
    
    public void Lock()
    {
        isLocked = true;
        
        // Enable collider to block player movement
        if (doorCollider != null)
        {
            doorCollider.enabled = true;
            doorCollider.isTrigger = false; // Make it solid
        }
        
        // Show door visual
        if (doorVisual != null)
        {
            doorVisual.SetActive(true);
        }
        
        // Play close animation
        if (doorAnimator != null)
        {
            doorAnimator.SetBool("IsOpen", false);
        }

    }
    
    public void Unlock()
    {
        isLocked = false;
        
        // Disable collider to allow player movement
        if (doorCollider != null)
        {
            doorCollider.enabled = false;
        }
        
        // Hide door visual or show it as open
        if (doorVisual != null)
        {
            doorVisual.SetActive(false); // Or keep active but show open state
        }
        
        // Play open animation
        if (doorAnimator != null)
        {
            doorAnimator.SetBool("IsOpen", true);
        }

    }
    
    public void Toggle()
    {
        if (isLocked)
        {
            Unlock();
        }
        else
        {
            Lock();
        }
    }
    
    // For debugging in editor
    private void OnDrawGizmos()
    {
        if (doorCollider != null)
        {
            Gizmos.color = isLocked ? Color.red : Color.green;
            Gizmos.DrawWireCube(transform.position, doorCollider.bounds.size);
        }
    }
    
    // Optional: Handle player interaction attempts when door is locked
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player") && isLocked)
        {
            // Door is locked, player cannot pass
            Debug.Log("Door is locked!");
            // Could play a sound effect or show UI feedback here
        }
    }
}