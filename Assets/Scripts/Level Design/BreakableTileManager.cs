using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// Manager for breakable tiles that can be placed on tilemaps and broken by various interactions
/// </summary>
public class BreakableTileManager : MonoBehaviour
{
    [Header("Tilemap Setup")]
    [SerializeField] private Tilemap breakableTilemap;
    [SerializeField] private Tilemap collisionTilemap;
    [SerializeField] private string breakableTilemapName = "Breakable TM";
    [SerializeField] private string collisionTilemapName = "Collision TM";
    
    [Header("Breakable Tile Types")]
    [SerializeField] private TileBase[] breakableTiles;
    [SerializeField] private int defaultTileHealth = 1;
    
    [Header("Break Settings")]
    [SerializeField] private bool projectilesBreakTiles = true;
    [SerializeField] private bool playerCanBreakTiles = false;
    [Tooltip("Allow enemy projectiles to break tiles (useful for testing)")]
    [SerializeField] private bool enemyProjectilesBreakTiles = false;
    [SerializeField] private LayerMask projectileLayerMask = -1;
    
    [Header("Effects")]
    [SerializeField] private GameObject breakParticleEffect;
    [SerializeField] private AudioClip breakSound;
    [SerializeField] private bool spawnDrops = false;
    [SerializeField] private GameObject[] possibleDrops;
    [SerializeField] private float dropChance = 0.2f;
    
    [Header("Replacement Tiles")]
    [SerializeField] private bool replaceWithFloorTile = true;
    [SerializeField] private TileBase floorReplacementTile;
    
    private AudioSource audioSource;
    private Grid grid;
    
    // Tile health tracking
    private System.Collections.Generic.Dictionary<Vector3Int, int> tileHealthMap = new System.Collections.Generic.Dictionary<Vector3Int, int>();
    
    void Start()
    {
        SetupReferences();
        InitializeBreakableTiles();
    }
    
    private void SetupReferences()
    {
        // Find grid
        grid = FindFirstObjectByType<Grid>();
        
        // Find tilemaps if not assigned
        if (breakableTilemap == null)
        {
            GameObject breakableObj = GameObject.Find(breakableTilemapName);
            if (breakableObj != null)
            {
                breakableTilemap = breakableObj.GetComponent<Tilemap>();
            }
        }
        
        if (collisionTilemap == null)
        {
            GameObject collisionObj = GameObject.Find(collisionTilemapName);
            if (collisionObj != null)
            {
                collisionTilemap = collisionObj.GetComponent<Tilemap>();
            }
        }
        
        // Setup audio
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null && breakSound != null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
    }
    
    /// <summary>
    /// Get the breakable tilemap managed by this component
    /// </summary>
    /// <returns>The breakable tilemap, or null if not assigned</returns>
    public Tilemap GetBreakableTilemap()
    {
        return breakableTilemap;
    }
    
    /// <summary>
    /// Get the array of breakable tiles managed by this component
    /// </summary>
    /// <returns>Array of breakable tiles, or null if not assigned</returns>
    public TileBase[] GetBreakableTiles()
    {
        return breakableTiles;
    }
    
    /// <summary>
    /// Get whether enemy projectiles are allowed to break tiles
    /// </summary>
    /// <returns>True if enemy projectiles can break tiles</returns>
    public bool GetEnemyProjectilesBreakTiles()
    {
        return enemyProjectilesBreakTiles;
    }
    
    private void InitializeBreakableTiles()
    {
        if (breakableTilemap == null) return;
        
        // Scan tilemap for breakable tiles and initialize their health
        BoundsInt bounds = breakableTilemap.cellBounds;
        TileBase[] allTiles = breakableTilemap.GetTilesBlock(bounds);
        
        for (int x = 0; x < bounds.size.x; x++)
        {
            for (int y = 0; y < bounds.size.y; y++)
            {
                TileBase tile = allTiles[x + y * bounds.size.x];
                if (tile != null && IsBreakableTile(tile))
                {
                    Vector3Int position = new Vector3Int(bounds.x + x, bounds.y + y, bounds.z);
                    tileHealthMap[position] = defaultTileHealth;
                }
            }
        }
        
        Debug.Log($"BreakableTileManager: Initialized {tileHealthMap.Count} breakable tiles");
    }
    
    private bool IsBreakableTile(TileBase tile)
    {
        if (breakableTiles == null || breakableTiles.Length == 0) return true;
        
        foreach (TileBase breakableTile in breakableTiles)
        {
            if (tile == breakableTile) return true;
        }
        return false;
    }
    
    /// <summary>
    /// Check if a shooter is allowed to break tiles based on settings
    /// </summary>
    /// <param name="shooter">GameObject that fired the projectile</param>
    /// <returns>True if this shooter can break tiles</returns>
    private bool CanShooterBreakTiles(GameObject shooter)
    {
        if (shooter == null) return projectilesBreakTiles;
        
        // Check if shooter is a player
        if (shooter.CompareTag("Player"))
        {
            return projectilesBreakTiles;
        }
        
        // Check if shooter has Enemy component (safer than tag check)
        Enemy enemyComponent = shooter.GetComponent<Enemy>();
        if (enemyComponent != null)
        {
            return enemyProjectilesBreakTiles;
        }
        
        // Default to main projectiles setting for unknown shooters
        return projectilesBreakTiles;
    }
    
    /// <summary>
    /// Attempt to break a tile at the given world position
    /// </summary>
    /// <param name="worldPosition">World position of the hit</param>
    /// <param name="damage">Damage to apply to the tile</param>
    /// <returns>True if tile was broken</returns>
    public bool TryBreakTile(Vector3 worldPosition, int damage = 1)
    {
        if (breakableTilemap == null) return false;
        
        Vector3Int tilePosition = breakableTilemap.WorldToCell(worldPosition);
        return TryBreakTileAt(tilePosition, damage);
    }
    
    /// <summary>
    /// Attempt to break a tile at the given world position with shooter permission check
    /// </summary>
    /// <param name="worldPosition">World position of the hit</param>
    /// <param name="shooter">GameObject that fired the projectile (for permission check)</param>
    /// <param name="damage">Damage to apply to the tile</param>
    /// <returns>True if tile was broken</returns>
    public bool TryBreakTile(Vector3 worldPosition, GameObject shooter, int damage = 1)
    {
        if (breakableTilemap == null) return false;
        
        // Check if this shooter is allowed to break tiles
        if (!CanShooterBreakTiles(shooter))
        {
            return false;
        }
        
        Vector3Int tilePosition = breakableTilemap.WorldToCell(worldPosition);
        return TryBreakTileAt(tilePosition, damage);
    }
    
    /// <summary>
    /// Attempt to break a tile at specific tile coordinates
    /// </summary>
    /// <param name="tilePosition">Tile coordinates</param>
    /// <param name="damage">Damage to apply</param>
    /// <returns>True if tile was broken</returns>
    public bool TryBreakTileAt(Vector3Int tilePosition, int damage = 1)
    {
        // Check if tile exists and is breakable
        TileBase tile = breakableTilemap.GetTile(tilePosition);
        if (tile == null || !IsBreakableTile(tile)) return false;
        
        // Initialize health if not tracked
        if (!tileHealthMap.ContainsKey(tilePosition))
        {
            tileHealthMap[tilePosition] = defaultTileHealth;
        }
        
        // Apply damage
        tileHealthMap[tilePosition] -= damage;
        
        // Check if tile should break
        if (tileHealthMap[tilePosition] <= 0)
        {
            BreakTile(tilePosition);
            return true;
        }
        
        return false;
    }
    
    private void BreakTile(Vector3Int tilePosition)
    {
        // Remove from breakable tilemap
        breakableTilemap.SetTile(tilePosition, null);
        
        // Replace with floor tile if enabled
        if (replaceWithFloorTile && floorReplacementTile != null && collisionTilemap != null)
        {
            collisionTilemap.SetTile(tilePosition, null); // Remove collision
        }
        
        // Remove from health tracking
        tileHealthMap.Remove(tilePosition);
        
        // Get world position for effects
        Vector3 worldPosition = breakableTilemap.CellToWorld(tilePosition) + breakableTilemap.cellSize / 2;
        
        // Play effects
        PlayBreakEffects(worldPosition);
        
        // Spawn drops
        if (spawnDrops)
        {
            TrySpawnDrop(worldPosition);
        }
        
        // Refresh colliders
        RefreshColliders();
        
        Debug.Log($"BreakableTileManager: Broke tile at {tilePosition}");
    }
    
    /// <summary>
    /// Break tiles in a circular area (for explosions)
    /// </summary>
    /// <param name="centerPosition">Center of explosion in world coordinates</param>
    /// <param name="radius">Radius in world units</param>
    /// <param name="damage">Damage per tile</param>
    public void BreakTilesInRadius(Vector3 centerPosition, float radius, int damage = 999)
    {
        if (breakableTilemap == null) return;
        
        // Convert to tile coordinates
        Vector3Int centerTile = breakableTilemap.WorldToCell(centerPosition);
        int tileRadius = Mathf.CeilToInt(radius / breakableTilemap.cellSize.x);
        
        for (int x = -tileRadius; x <= tileRadius; x++)
        {
            for (int y = -tileRadius; y <= tileRadius; y++)
            {
                Vector3Int tilePos = centerTile + new Vector3Int(x, y, 0);
                Vector3 tileWorldPos = breakableTilemap.CellToWorld(tilePos) + breakableTilemap.cellSize / 2;
                
                // Check if within radius
                if (Vector3.Distance(centerPosition, tileWorldPos) <= radius)
                {
                    TryBreakTileAt(tilePos, damage);
                }
            }
        }
    }
    
    private void PlayBreakEffects(Vector3 position)
    {
        // Spawn particle effect
        if (breakParticleEffect != null)
        {
            GameObject effect = Instantiate(breakParticleEffect, position, Quaternion.identity);
            Destroy(effect, 2f);
        }
        
        // Play sound
        if (audioSource != null && breakSound != null)
        {
            audioSource.PlayOneShot(breakSound);
        }
    }
    
    private void TrySpawnDrop(Vector3 position)
    {
        if (possibleDrops == null || possibleDrops.Length == 0) return;
        
        if (Random.value <= dropChance)
        {
            GameObject drop = possibleDrops[Random.Range(0, possibleDrops.Length)];
            Instantiate(drop, position, Quaternion.identity);
        }
    }
    
    private void RefreshColliders()
    {
        // Refresh breakable tilemap collider
        if (breakableTilemap != null)
        {
            TilemapCollider2D breakableCollider = breakableTilemap.GetComponent<TilemapCollider2D>();
            if (breakableCollider != null)
            {
                breakableCollider.enabled = false;
                breakableCollider.enabled = true;
            }
        }
        
        // Refresh collision tilemap collider
        if (collisionTilemap != null)
        {
            TilemapCollider2D collisionCollider = collisionTilemap.GetComponent<TilemapCollider2D>();
            if (collisionCollider != null)
            {
                collisionCollider.enabled = false;
                collisionCollider.enabled = true;
            }
        }
    }
    
    /// <summary>
    /// Handle projectile hits via collision detection
    /// </summary>
    void OnCollisionEnter2D(Collision2D collision)
    {
        if (!projectilesBreakTiles) return;
        
        // Check if it's a projectile
        if (IsProjectile(collision.gameObject))
        {
            Vector3 hitPoint = collision.contacts[0].point;
            TryBreakTile(hitPoint);
        }
    }
    
    /// <summary>
    /// Handle projectile hits via trigger detection
    /// </summary>
    void OnTriggerEnter2D(Collider2D other)
    {
        if (!projectilesBreakTiles) return;
        
        if (IsProjectile(other.gameObject))
        {
            TryBreakTile(other.transform.position);
        }
        
        // Handle player interaction
        if (playerCanBreakTiles && other.CompareTag("Player"))
        {
            TryBreakTile(other.transform.position);
        }
    }
    
    private bool IsProjectile(GameObject obj)
    {
        // Check layer mask
        if (((1 << obj.layer) & projectileLayerMask) == 0) return false;
        
        // Check for projectile component or tag
        return obj.GetComponent<Projectile>() != null || obj.CompareTag("Enemy");
    }
    
    /// <summary>
    /// Context menu helpers for testing
    /// </summary>
    [ContextMenu("Break Tile Here")]
    public void TestBreakTile()
    {
        TryBreakTile(transform.position);
    }
    
    [ContextMenu("Explosion Test")]
    public void TestExplosion()
    {
        BreakTilesInRadius(transform.position, 2f);
    }
    
    [ContextMenu("Reinitialize Tiles")]
    public void ReinitializeTiles()
    {
        tileHealthMap.Clear();
        InitializeBreakableTiles();
    }
}