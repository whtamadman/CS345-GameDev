using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class Projectile : MonoBehaviour
{
    [SerializeField] private float lifeTime = 2f;
    [SerializeField] private float speed = 10f;
    [SerializeField] private bool isBoomerang = false;
    [SerializeField] private float boomerangReturnDelay = 0.5f; // Time before returning
    [SerializeField] private float boomerangReturnSpeed = 12f; // Speed when returning

    private Transform target;
    private Rigidbody2D rb;
    private GameObject shooter;
    private bool isReturning = false;
    
    // Static tracking for boomerangs (max 1 per shooter at a time)
    private static Dictionary<GameObject, Projectile> activeBoomerangs = new Dictionary<GameObject, Projectile>();

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        StartCoroutine(LifeCountdown());
        
        // Boomerang check will happen in SetTarget after shooter is set
        // Only start coroutine for enemy boomerangs here (player boomerangs checked in SetTarget)
        if (isBoomerang && shooter == null)
        {
            // Shooter not set yet, will be checked in SetTarget
        }
        else if (isBoomerang)
        {
            // Enemy boomerangs don't have restrictions, start return coroutine
            StartCoroutine(BoomerangReturn());
        }
    }

    public void SetTarget(GameObject targetObject, GameObject shooterObject = null)
    {
        target = targetObject.transform;
        shooter = shooterObject;

        Vector2 direction = (target.position - transform.position).normalized;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f;
        transform.rotation = Quaternion.Euler(0, 0, angle);
        
        // If this is a boomerang, check if the shooter already has one active (max 1 per shooter)
        if (isBoomerang && shooter != null)
        {
            // Clean up any destroyed boomerangs from the dictionary
            var keysToRemove = new List<GameObject>();
            foreach (var kvp in activeBoomerangs)
            {
                if (kvp.Value == null)
                {
                    keysToRemove.Add(kvp.Key);
                }
            }
            foreach (var key in keysToRemove)
            {
                activeBoomerangs.Remove(key);
            }
            
            // If the shooter already has an active boomerang, destroy this one
            if (activeBoomerangs.ContainsKey(shooter) && activeBoomerangs[shooter] != null)
            {
                Destroy(gameObject);
                return;
            }
            
            // Add this boomerang to the active dictionary
            activeBoomerangs[shooter] = this;
            StartCoroutine(BoomerangReturn());
        }
    }

    public void SetBoomerang(bool boomerang)
    {
        isBoomerang = boomerang;
        if (isBoomerang && gameObject.activeInHierarchy)
        {
            StartCoroutine(BoomerangReturn());
        }
    }

    IEnumerator BoomerangReturn()
    {
        yield return new WaitForSeconds(boomerangReturnDelay);
        isReturning = true;
    }

    IEnumerator LifeCountdown()
    {
        yield return new WaitForSeconds(lifeTime);
        Destroy(gameObject);
    }

    void FixedUpdate()
    {
        if (rb != null)
        {
            Vector2 currentVelocity;
            
            if (isReturning && shooter != null)
            {
                // Return to shooter
                Vector2 direction = (shooter.transform.position - transform.position).normalized;
                float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f;
                transform.rotation = Quaternion.Euler(0, 0, angle);
                currentVelocity = direction * boomerangReturnSpeed;
                rb.linearVelocity = currentVelocity;

                // Check if reached shooter
                if (Vector2.Distance(transform.position, shooter.transform.position) < 0.5f)
                {
                    Destroy(gameObject);
                    return;
                }
            }
            else
            {
                currentVelocity = transform.up * speed;
                rb.linearVelocity = currentVelocity;
            }
            
            // Check for wall collision along movement path
            CheckWallCollision(currentVelocity);
        }
    }
    
    private void CheckWallCollision(Vector2 velocity)
    {
        if (velocity.magnitude < 0.1f) return; // Skip if not moving
        
        // Cast a ray from current position in the direction of movement
        Vector2 rayDirection = velocity.normalized;
        float rayDistance = velocity.magnitude * Time.fixedDeltaTime + 0.2f; // Add buffer
        
        // Use RaycastAll to get ALL colliders in the path (not just the first one)
        RaycastHit2D[] hits = Physics2D.RaycastAll(transform.position, rayDirection, rayDistance);
        
        foreach (RaycastHit2D hit in hits)
        {
            // Skip if it's the projectile itself or the shooter
            if (hit.collider.gameObject == gameObject) continue;
            if (shooter != null && (hit.collider.gameObject == shooter || hit.collider.transform.IsChildOf(shooter.transform))) continue;
            
            // Check if this is a wall
            if (IsWallCollider(hit.collider))
            {
                // Hit a wall - move to hit point and destroy
                transform.position = hit.point;
                if (isBoomerang && shooter != null)
                {
                    if (activeBoomerangs.ContainsKey(shooter) && activeBoomerangs[shooter] == this)
                    {
                        activeBoomerangs.Remove(shooter);
                    }
                }
                Destroy(gameObject);
                return;
            }
        }
    }
    
    private bool IsWallCollider(Collider2D collider)
    {
        // Skip trigger colliders (those are for entities, not walls)
        if (collider.isTrigger) return false;
        
        // Skip the projectile itself
        if (collider.gameObject == gameObject) return false;
        
        // Check for tilemap collision
        if (collider.GetComponent<TilemapCollider2D>() != null) return true;
        
        // Check for collision tilemap by name
        if (collider.gameObject.name.ToLower().Contains("collision")) return true;
        
        // Check for walls layer (if you have one)
        int wallsLayer = LayerMask.NameToLayer("Walls");
        if (wallsLayer != -1 && collider.gameObject.layer == wallsLayer) return true;
        
        // Any non-trigger collider that's not an entity is likely a wall
        // (This catches any solid obstacles)
        return true;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        HandleCollision(other);
    }
    
    private void HandleCollision(Collider2D other)
    {
        // Check for breakable tiles first
        BreakableTileManager tileManager = FindFirstObjectByType<BreakableTileManager>();
        if (tileManager != null)
        {
            // Try to break tile at collision point, passing shooter info for permission check
            if (tileManager.TryBreakTile(transform.position, shooter))
            {
                // Tile was broken, destroy projectile
                if (isBoomerang && shooter != null)
                {
                    if (activeBoomerangs.ContainsKey(shooter) && activeBoomerangs[shooter] == this)
                    {
                        activeBoomerangs.Remove(shooter);
                    }
                }
                Destroy(gameObject);
                return;
            }
        }
        
        // Don't collide with shooter or any of its colliders
        if (shooter != null)
        {
            // Check if the collider belongs to the shooter or any of its children
            if (other.gameObject == shooter || other.transform.IsChildOf(shooter.transform))
            {
                // If boomerang is returning and reaches shooter, destroy it
                if (isBoomerang && isReturning)
                {
                    // Remove from active dictionary
                    if (activeBoomerangs.ContainsKey(shooter) && activeBoomerangs[shooter] == this)
                    {
                        activeBoomerangs.Remove(shooter);
                    }
                    Destroy(gameObject);
                }
                return;
            }
            
            // Also check if it's the same Enemy component (in case of multiple colliders)
            Enemy shooterEnemy = shooter.GetComponent<Enemy>();
            Enemy hitEnemy = other.GetComponent<Enemy>();
            if (shooterEnemy != null && hitEnemy != null && shooterEnemy == hitEnemy)
            {
                return;
            }
        }

        // If boomerang mode, don't destroy on target hit - let it return
        if (isBoomerang && !isReturning)
        {
            if (target != null && other.tag == target.tag)
            {
                other.GetComponent<Player>()?.takeDamage();
            }
            return;
        }

        // Normal projectile behavior - only hit the target tag
        if (target != null && other.tag == target.tag)
        {
            other.GetComponent<Player>()?.takeDamage();
            Destroy(gameObject);
        }
    }
    
    private void OnDestroy()
    {
        // Remove from active dictionary when destroyed
        if (isBoomerang && shooter != null)
        {
            if (activeBoomerangs.ContainsKey(shooter) && activeBoomerangs[shooter] == this)
            {
                activeBoomerangs.Remove(shooter);
            }
        }
    }
}