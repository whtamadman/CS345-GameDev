using UnityEngine;

public class ItemRoom : Room
{
    [Header("Item Room Configuration")]
    public Transform itemSpawnPoint; // Where to spawn the item
    public bool spawnOnRoomClear = true; // Spawn item when room is cleared
    public bool spawnOnEntry = false; // Spawn item when player enters
    
    // Note: itemPrefabs, itemSpawned, itemCollected, and spawnedItem are inherited from base Room class
    
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
        Debug.Log($"ItemRoom {gameObject.name}: SpawnItem called - itemSpawned={itemSpawned}, spawnOnRoomClear={spawnOnRoomClear}");
        
        if (itemSpawned)
        {
            Debug.LogWarning($"ItemRoom {gameObject.name}: Item already spawned in this room! Existing item: {(currentItem != null ? currentItem.name : "null")}");
            return;
        }
        
        Debug.Log($"ItemRoom {gameObject.name}: Checking item prefabs - itemPrefabs is {(itemPrefabs == null ? "null" : $"array with {itemPrefabs.Length} items")}");
        
        if (itemPrefabs == null || itemPrefabs.Length == 0)
        {
            Debug.LogError($"ItemRoom {gameObject.name}: No item prefabs assigned! ItemPrefabs array is {(itemPrefabs == null ? "null" : $"empty (length: {itemPrefabs.Length})")}");
            
            // Check if manually placed items exist in the room
            Debug.Log($"ItemRoom {gameObject.name}: Searching for manually placed PowerUp items...");
            var existingItems = GetComponentsInChildren<PowerUp>();
            Debug.Log($"ItemRoom {gameObject.name}: Found {existingItems.Length} PowerUp components in children");
            
            if (existingItems.Length > 0)
            {
                Debug.Log($"ItemRoom {gameObject.name}: Found {existingItems.Length} manually placed items in room, marking as spawned");
                itemSpawned = true;
                currentItem = existingItems[0].gameObject;
            }
            else
            {
                Debug.LogWarning($"ItemRoom {gameObject.name}: No manually placed items found either!");
            }
            return;
        }
        
        // Choose random item from available prefabs
        int randomIndex = Random.Range(0, itemPrefabs.Length);
        GameObject itemToSpawn = itemPrefabs[randomIndex];
        Debug.Log($"ItemRoom {gameObject.name}: Selected item prefab at index {randomIndex}: {(itemToSpawn != null ? itemToSpawn.name : "null")}");
        
        if (itemToSpawn == null)
        {
            Debug.LogError($"ItemRoom {gameObject.name}: Selected item prefab at index {randomIndex} is null!");
            return;
        }
        
        // Spawn the item
        Vector3 spawnPosition = itemSpawnPoint.position;
        Debug.Log($"ItemRoom {gameObject.name}: Spawning item {itemToSpawn.name} at position {spawnPosition}");
        currentItem = Instantiate(itemToSpawn, spawnPosition, Quaternion.identity, transform);
        
        itemSpawned = true;
        Debug.Log($"ItemRoom {gameObject.name}: Item spawning completed successfully! Item: {currentItem.name}");

    }
    

    

    
    // Override room clear condition for item rooms
    protected override void CheckRoomClearCondition()
    {
        base.CheckRoomClearCondition();
        
        // Item rooms might have additional clear conditions
        // For example, requiring both enemies defeated AND item collected
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
        currentItem = Instantiate(itemToSpawn, spawnPosition, Quaternion.identity, transform);
        
        itemSpawned = true;

    }
    
    protected override void OnDestroy()
    {
        base.OnDestroy();
        
        // Clean up item reference
        if (currentItem != null)
        {
            // Item cleanup handled by PowerUp component itself
        }
    }
    
    // Debug methods
    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    public void DebugSpawnItem()
    {
        ForceSpawnItem();
    }
}

