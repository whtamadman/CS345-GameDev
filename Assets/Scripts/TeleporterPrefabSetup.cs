using UnityEngine;

/// <summary>
/// Helper script to quickly configure teleporter prefabs in the editor
/// Attach this to a GameObject to quickly set it up as a teleporter
/// </summary>
[System.Serializable]
public class TeleporterPrefabSetup : MonoBehaviour
{
    [Header("Prefab Setup Instructions")]
    [TextArea(3, 5)]
    public string instructions = "1. Add Collider2D (IsTrigger = true)\n2. Add SpriteRenderer with teleporter sprite\n3. Configure teleporter settings below\n4. Add popup and canvas references\n5. Save as prefab";
    
    [Header("Required Components Check")]
    [SerializeField] private bool hasCollider = false;
    [SerializeField] private bool hasSpriteRenderer = false;
    [SerializeField] private bool hasTeleporter = false;
    
    [Header("Quick Setup")]
    [Tooltip("Sprite to use for the teleporter visual")]
    public Sprite teleporterSprite;
    [Tooltip("Size of the trigger collider")]
    public Vector2 triggerSize = Vector2.one;
    
    void Start()
    {
        // This component is only for editor setup, remove it at runtime
        if (Application.isPlaying)
        {
            Destroy(this);
        }
    }
    
    void OnValidate()
    {
        // Check for required components
        hasCollider = GetComponent<Collider2D>() != null;
        hasSpriteRenderer = GetComponent<SpriteRenderer>() != null;
        hasTeleporter = GetComponent<Teleporter>() != null;
    }
    
    [ContextMenu("Auto Setup Teleporter")]
    public void AutoSetupTeleporter()
    {
        // Add Collider2D if missing
        Collider2D col = GetComponent<Collider2D>();
        if (col == null)
        {
            col = gameObject.AddComponent<BoxCollider2D>();
        }
        col.isTrigger = true;
        
        // Set collider size
        if (col is BoxCollider2D boxCol)
        {
            boxCol.size = triggerSize;
        }
        
        // Add SpriteRenderer if missing
        SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
        {
            spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
        }
        
        // Set sprite if provided
        if (teleporterSprite != null)
        {
            spriteRenderer.sprite = teleporterSprite;
        }
        
        // Add Teleporter component if missing
        if (GetComponent<Teleporter>() == null)
        {
            gameObject.AddComponent<Teleporter>();
        }
        
        // Set appropriate layer and tag
        gameObject.layer = LayerMask.NameToLayer("Default");
        if (!gameObject.CompareTag("Untagged"))
        {
            gameObject.tag = "Untagged";
        }
        
        Debug.Log($"Teleporter setup complete for {gameObject.name}");
        
        // Refresh component check
        OnValidate();
    }
    
    [ContextMenu("Check Component Status")]
    public void CheckComponentStatus()
    {
        OnValidate();
        
        string status = "Teleporter Component Status:\n";
        status += $"✓ Collider2D (Trigger): {hasCollider}\n";
        status += $"✓ SpriteRenderer: {hasSpriteRenderer}\n";
        status += $"✓ Teleporter Script: {hasTeleporter}\n";
        
        if (hasCollider && hasSpriteRenderer && hasTeleporter)
        {
            status += "\n✅ Teleporter is ready to use!";
        }
        else
        {
            status += "\n❌ Missing required components. Use 'Auto Setup Teleporter' to fix.";
        }
        
        Debug.Log(status);
    }
}