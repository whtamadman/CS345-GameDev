using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// Handles breakable blocks on tilemaps that can be destroyed by projectiles or player interaction
/// </summary>
public class BreakableBlock : MonoBehaviour
{
    [Header("Breakable Block Settings")]
    [SerializeField] private bool breakOnProjectileHit = true;
    [SerializeField] private bool breakOnPlayerContact = false;
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private string projectileTag = "Enemy"; // Projectiles from enemies
    
    [Header("Tilemap References")]
    [SerializeField] private Tilemap targetTilemap;
    [SerializeField] private string tilemapName = "Collision TM";
    [SerializeField] private TileBase breakableTile; // The tile type that can be broken
    
    [Header("Effects")]
    [SerializeField] private GameObject breakEffect; // Particle effect when breaking
    [SerializeField] private AudioClip breakSound; // Sound when breaking
    [SerializeField] private bool spawnDrops = false;
    [SerializeField] private GameObject[] dropPrefabs;
    [SerializeField] private float dropChance = 0.3f;
    
    private AudioSource audioSource;
    
    void Start()
    {
        SetupReferences();
    }
    
    private void SetupReferences()
    {
        // Find tilemap if not assigned
        if (targetTilemap == null)
        {
            GameObject tilemapObj = GameObject.Find(tilemapName);
            if (tilemapObj != null)
            {
                targetTilemap = tilemapObj.GetComponent<Tilemap>();
            }
            else
            {
                Debug.LogWarning($"BreakableBlock: Could not find tilemap named '{tilemapName}'");
            }
        }
        
        // Setup audio source
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null && breakSound != null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
    }
    
    /// <summary>
    /// Break a specific tile at world position
    /// </summary>
    /// <param name="worldPosition">World position where the break occurred</param>
    public void BreakTileAtPosition(Vector3 worldPosition)
    {
        if (targetTilemap == null) return;
        
        // Convert world position to tile position
        Vector3Int tilePosition = targetTilemap.WorldToCell(worldPosition);
        
        // Check if there's a breakable tile at this position
        TileBase currentTile = targetTilemap.GetTile(tilePosition);
        if (currentTile == null) return;
        
        // If we have a specific breakable tile type, check for it
        if (breakableTile != null && currentTile != breakableTile) return;
        
        // Break the tile
        BreakTile(tilePosition, worldPosition);
    }
    
    /// <summary>
    /// Break tile at specific tile coordinates
    /// </summary>
    /// <param name="tilePosition">Tile coordinates</param>
    /// <param name="worldPosition">World position for effects</param>
    private void BreakTile(Vector3Int tilePosition, Vector3 worldPosition)
    {
        // Remove tile from tilemap
        targetTilemap.SetTile(tilePosition, null);
        
        // Play break effects
        PlayBreakEffects(worldPosition);
        
        // Spawn drops
        if (spawnDrops)
        {
            SpawnDrops(worldPosition);
        }
        
        // Refresh tilemap collider
        RefreshTilemapCollider();
        
        Debug.Log($"BreakableBlock: Broke tile at {tilePosition}");
    }
    
    /// <summary>
    /// Handle projectile collision
    /// </summary>
    /// <param name="collision">Collision information</param>
    void OnCollisionEnter2D(Collision2D collision)
    {
        if (!breakOnProjectileHit) return;
        
        // Check if it's a projectile
        if (collision.gameObject.CompareTag(projectileTag) || collision.gameObject.GetComponent<Projectile>() != null)
        {
            Vector3 hitPoint = collision.contacts[0].point;
            BreakTileAtPosition(hitPoint);
        }
    }
    
    /// <summary>
    /// Handle trigger collision (for projectiles set as triggers)
    /// </summary>
    /// <param name="other">Collider that entered trigger</param>
    void OnTriggerEnter2D(Collider2D other)
    {
        if (!breakOnProjectileHit) return;
        
        // Check if it's a projectile
        if (other.CompareTag(projectileTag) || other.GetComponent<Projectile>() != null)
        {
            BreakTileAtPosition(other.transform.position);
        }
        
        // Check if it's player contact breaking
        if (breakOnPlayerContact && other.CompareTag(playerTag))
        {
            BreakTileAtPosition(other.transform.position);
        }
    }
    
    /// <summary>
    /// Play visual and audio effects
    /// </summary>
    /// <param name="position">Position to spawn effects</param>
    private void PlayBreakEffects(Vector3 position)
    {
        // Spawn particle effect
        if (breakEffect != null)
        {
            GameObject effect = Instantiate(breakEffect, position, Quaternion.identity);
            // Auto-destroy particle effect after 2 seconds
            Destroy(effect, 2f);
        }
        
        // Play sound effect
        if (audioSource != null && breakSound != null)
        {
            audioSource.PlayOneShot(breakSound);
        }
    }
    
    /// <summary>
    /// Spawn item drops
    /// </summary>
    /// <param name="position">Position to spawn drops</param>
    private void SpawnDrops(Vector3 position)
    {
        if (dropPrefabs == null || dropPrefabs.Length == 0) return;
        
        if (Random.value <= dropChance)
        {
            GameObject dropPrefab = dropPrefabs[Random.Range(0, dropPrefabs.Length)];
            Instantiate(dropPrefab, position, Quaternion.identity);
        }
    }
    
    /// <summary>
    /// Refresh the tilemap collider to update physics
    /// </summary>
    private void RefreshTilemapCollider()
    {
        if (targetTilemap == null) return;
        
        TilemapCollider2D collider = targetTilemap.GetComponent<TilemapCollider2D>();
        if (collider != null)
        {
            // Force collider refresh
            collider.enabled = false;
            collider.enabled = true;
        }
    }
    
    /// <summary>
    /// Break multiple tiles in a radius (for explosive effects)
    /// </summary>
    /// <param name="centerPosition">Center of explosion</param>
    /// <param name="radius">Radius in tiles</param>
    public void BreakTilesInRadius(Vector3 centerPosition, int radius)
    {
        if (targetTilemap == null) return;
        
        Vector3Int centerTile = targetTilemap.WorldToCell(centerPosition);
        
        for (int x = -radius; x <= radius; x++)
        {
            for (int y = -radius; y <= radius; y++)
            {
                Vector3Int tilePos = centerTile + new Vector3Int(x, y, 0);
                TileBase currentTile = targetTilemap.GetTile(tilePos);
                
                if (currentTile != null)
                {
                    // Check if within circular radius
                    float distance = Vector2.Distance(Vector2.zero, new Vector2(x, y));
                    if (distance <= radius)
                    {
                        Vector3 worldPos = targetTilemap.CellToWorld(tilePos);
                        BreakTile(tilePos, worldPos);
                    }
                }
            }
        }
    }
    
    /// <summary>
    /// Context menu to test breaking tiles
    /// </summary>
    [ContextMenu("Break Tile At Position")]
    public void TestBreakTile()
    {
        BreakTileAtPosition(transform.position);
    }
    
    /// <summary>
    /// Context menu to test explosive breaking
    /// </summary>
    [ContextMenu("Break Tiles In Radius")]
    public void TestExplosiveBreak()
    {
        BreakTilesInRadius(transform.position, 2);
    }
}