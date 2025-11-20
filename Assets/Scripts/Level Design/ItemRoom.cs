using UnityEngine;

public class ItemRoom : Room
{
    [Header("Item Room Configuration")]
    public GameObject[] itemPrefabs; // Array of possible items to spawn
    public Transform itemSpawnPoint; // Where to spawn the item
    public bool spawnOnRoomClear = true; // Spawn item when room is cleared
    public bool spawnOnEntry = false; // Spawn item when player enters
    
    [Header("Item State")]
    public bool itemSpawned = false;
    public bool itemCollected = false;
    
    private GameObject spawnedItem;
    
    protected override void Awake()
    {
        base.Awake();
        
        // Set room type to Item
        roomType = RoomType.Item;
        
        // Set default spawn point if not assigned
        if (itemSpawnPoint == null)
        {
            itemSpawnPoint = transform;
        }
    }
    
    protected override void Start()
    {
        base.Start();
        
        // If room should spawn item on entry, spawn it now if no enemies
        if (spawnOnEntry && !spawnOnRoomClear)
        {
            SpawnItem();
        }
    }
    
    public override void EnterRoom()
    {
        base.EnterRoom();
        
        // Spawn item on entry if configured to do so
        if (spawnOnEntry && !itemSpawned)
        {
            SpawnItem();
        }
    }
    
    public override void MarkCleared()
    {
        base.MarkCleared();
        
        // Spawn item when room is cleared if configured to do so
        if (spawnOnRoomClear && !itemSpawned)
        {
            SpawnItem();
        }
    }
    
    private void SpawnItem()
    {
        if (itemSpawned)
        {
            Debug.LogWarning("Item already spawned in this room!");
            return;
        }
        
        if (itemPrefabs == null || itemPrefabs.Length == 0)
        {
            Debug.LogError("No item prefabs assigned to ItemRoom!");
            return;
        }
        
        // Choose random item from available prefabs
        GameObject itemToSpawn = itemPrefabs[Random.Range(0, itemPrefabs.Length)];
        
        if (itemToSpawn == null)
        {
            Debug.LogError("Selected item prefab is null!");
            return;
        }
        
        // Spawn the item
        Vector3 spawnPosition = itemSpawnPoint.position;
        spawnedItem = Instantiate(itemToSpawn, spawnPosition, Quaternion.identity, transform);
        
        // Setup item collection detection
        SetupItemCollection();
        
        itemSpawned = true;

    }
    
    private void SetupItemCollection()
    {
        if (spawnedItem == null) return;
        
        // Try to get existing item collection component
        ItemCollectable itemCollectable = spawnedItem.GetComponent<ItemCollectable>();
        
        if (itemCollectable == null)
        {
            // Add item collection component if it doesn't exist
            itemCollectable = spawnedItem.AddComponent<ItemCollectable>();
        }
        
        // Subscribe to collection event
        itemCollectable.OnItemCollected += OnItemCollected;
        
        // Ensure item has a trigger collider
        Collider2D itemCollider = spawnedItem.GetComponent<Collider2D>();
        if (itemCollider == null)
        {
            itemCollider = spawnedItem.AddComponent<BoxCollider2D>();
        }
        itemCollider.isTrigger = true;
    }
    
    private void OnItemCollected(ItemCollectable item)
    {
        itemCollected = true;

        
        // Optionally mark room as "truly cleared" only when item is collected
        // This could affect completion percentage or achievements
    }
    
    // Override room clear condition for item rooms
    protected override void CheckRoomClearCondition()
    {
        base.CheckRoomClearCondition();
        
        // Item rooms might have additional clear conditions
        // For example, requiring both enemies defeated AND item collected
    }
    
    public GameObject GetSpawnedItem()
    {
        return spawnedItem;
    }
    
    public bool IsItemSpawned()
    {
        return itemSpawned;
    }
    
    public bool IsItemCollected()
    {
        return itemCollected;
    }
    
    // Method to manually spawn item (for testing or special conditions)
    public void ForceSpawnItem()
    {
        if (!itemSpawned)
        {
            SpawnItem();
        }
    }
    
    // Method to manually spawn specific item by index
    public void SpawnSpecificItem(int itemIndex)
    {
        if (itemSpawned)
        {
            Debug.LogWarning("Item already spawned in this room!");
            return;
        }
        
        if (itemPrefabs == null || itemIndex < 0 || itemIndex >= itemPrefabs.Length)
        {
            Debug.LogError($"Invalid item index: {itemIndex}");
            return;
        }
        
        GameObject itemToSpawn = itemPrefabs[itemIndex];
        if (itemToSpawn == null)
        {
            Debug.LogError($"Item prefab at index {itemIndex} is null!");
            return;
        }
        
        Vector3 spawnPosition = itemSpawnPoint.position;
        spawnedItem = Instantiate(itemToSpawn, spawnPosition, Quaternion.identity, transform);
        
        SetupItemCollection();
        itemSpawned = true;

    }
    
    protected override void OnDestroy()
    {
        base.OnDestroy();
        
        // Clean up item reference
        if (spawnedItem != null)
        {
            ItemCollectable itemCollectable = spawnedItem.GetComponent<ItemCollectable>();
            if (itemCollectable != null)
            {
                itemCollectable.OnItemCollected -= OnItemCollected;
            }
        }
    }
    
    // Debug methods
    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    public void DebugSpawnItem()
    {
        ForceSpawnItem();
    }
}

// Simple item collection component
public class ItemCollectable : MonoBehaviour
{
    [Header("Item Properties")]
    public string itemName = "Item";
    public string itemDescription = "A useful item";
    public Sprite itemIcon;
    public int value = 1;
    
    // Events
    public System.Action<ItemCollectable> OnItemCollected;
    
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            CollectItem();
        }
    }
    
    private void CollectItem()
    {
        // Notify listeners
        OnItemCollected?.Invoke(this);
        
        // Add item to player inventory (if inventory system exists)
        // PlayerInventory.Instance?.AddItem(this);
        
        // Play collection effects
        PlayCollectionEffects();
        
        // Destroy the item
        Destroy(gameObject);
    }
    
    private void PlayCollectionEffects()
    {
        // Play sound effect
        // AudioManager.Instance?.PlaySFX("ItemCollected");
        
        // Spawn particle effect
        // EffectsManager.Instance?.SpawnEffect("ItemCollected", transform.position);
    }
}