using UnityEngine;
using Random = UnityEngine.Random;
using System.Collections;
using System.Linq;

public class Enemy : MonoBehaviour
{
    protected Rigidbody2D rigidBody;
    protected SpriteRenderer spriteRenderer;
    public int health;
    public Projectile[] projectiles;
    public string opponentTag;
    public static Vector3 initialLocation;
    public EnemyType type;

    [SerializeField]protected float moveSpeed, shootForce, reloadTime;
    [SerializeField]protected float friction;
    [SerializeField]protected bool canAttackWhileMoving = false; // Can this enemy attack while moving?
    [SerializeField]protected float minMovementThreshold = 0.1f; // Minimum velocity to consider "moving"
    [SerializeField]public bool allowShooting = true; // Inspector toggle to enable/disable shooting
    
    [Header("Burst Fire Settings")]
    [SerializeField]protected bool enableBurstFire = false; // Enable 3-projectile burst firing
    [SerializeField]protected int burstCount = 3; // Number of projectiles in a burst
    [SerializeField]protected float burstDelay = 0.2f; // Time between projectiles in a burst
    [SerializeField]protected float longReloadTime = 2.0f; // Extended reload time after burst
    
    [Header("Contact Damage")]
    [SerializeField]protected bool enableContactDamage = true; // Enable damage on player contact
    [SerializeField]protected int contactDamage = 1; // Damage dealt to player on contact
    [SerializeField]protected float contactDamageCooldown = 1.0f; // Cooldown between contact damage
    protected float lastContactDamageTime = 0f; // Track last contact damage time
    
    [Header("Gold Drop Settings")]
    [SerializeField]protected bool dropsGold = true; // Does this enemy drop gold on death?
    [SerializeField][Range(0f, 1f)]protected float goldDropChance = 0.7f; // Chance to drop gold (0-100%)
    [SerializeField]protected int minGoldDrop = 1; // Minimum gold dropped
    [SerializeField]protected int maxGoldDrop = 5; // Maximum gold dropped
    
    [Header("Charge Attack Settings")]
    [SerializeField]protected bool enableChargeAttack = false; // Enable charge attack ability
    [SerializeField]protected float chargeUpTime = 2.0f; // Time to charge up before attacking
    [SerializeField]protected float chargeSpeed = 8.0f; // Speed during charge attack
    [SerializeField]protected float chargeDuration = 1.5f; // How long the charge lasts
    [SerializeField]protected float chargeAttackCooldown = 5.0f; // Cooldown between charge attacks
    [SerializeField]protected int chargeAttackDamage = 2; // Damage dealt during charge attack
    [SerializeField]protected float chargeAttackRange = 10.0f; // Maximum range to initiate charge attack
    protected Vector2 moveDirection;
    protected bool canShoot = true;
    Vector3 targetPosition;
    OutOfBounds outOfBounds;
    public float rangedDistance;
    
    // Charge attack state tracking
    protected bool isChargingUp = false;
    protected bool isCharging = false;
    protected float chargeUpTimer = 0f;
    protected float chargeTimer = 0f;
    protected float lastChargeAttackTime = 0f;
    protected Vector3 chargeDirection = Vector3.zero;
    protected Color originalColor = Color.white;
    
    [Header("Behavior Options")]
    [SerializeField] private bool enableMovement = true; // Can this enemy move?
    [SerializeField] private bool enableChasing = true; // Can this enemy chase the player?
    [SerializeField] private bool enableRoaming = true; // Can this enemy roam when not chasing?
    
    [Header("Line of Sight")]
    [SerializeField] private LayerMask wallLayers = -1; // What layers block line of sight
    [SerializeField] private bool requireLineOfSight = true; // Must see player to chase/shoot
    
    [Header("Wall Avoidance")]
    [SerializeField] private bool enableWallAvoidance = true;
    [SerializeField] private float wallDetectionDistance = 1.5f;
    [SerializeField] private float stuckThreshold = 0.1f; // Velocity threshold to consider "stuck"
    [SerializeField] private float stuckTime = 0.5f; // Time stuck before trying to go around
    
    // Wall avoidance tracking
    private Vector2 lastPosition;
    private float stuckTimer = 0f;
    private bool isAvoidingWall = false;
    private Vector2 avoidanceDirection = Vector2.zero;
    private float avoidanceTimer = 0f;
    private float avoidanceDuration = 1.5f;
    
    // Event for room system integration
    public System.Action<Enemy> OnDeath;

    private enum State
    {
        Roam,
        Chase,
        Shoot,
        ChargingUp,
        Charging
    };
    
    public enum EnemyType
    {
        Melee,
        Ranged,
        Static // Doesn't move, only shoots if player is in range
    };

    [SerializeField] private float chaseDist, roamDist, shootDist;
    State currentState;

    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        rigidBody = GetComponent<Rigidbody2D>();
        outOfBounds = GetComponent<OutOfBounds>();
        
        // Disable OutOfBounds component for room-based gameplay (it's designed for wrap-around worlds)
        if (outOfBounds != null)
        {
            outOfBounds.enabled = false;
        }
        
        // Ensure Rigidbody2D is properly set up
        if (rigidBody != null)
        {
            rigidBody.WakeUp();
            if (rigidBody.bodyType == RigidbodyType2D.Kinematic)
            {
                rigidBody.bodyType = RigidbodyType2D.Dynamic; // Enemies need dynamic physics to move
            }
        }
        
        // Static enemies don't have states, they just shoot
        if (type == EnemyType.Static)
        {
            Debug.Log($"Static Enemy {gameObject.name} initialized (no movement)");
            targetPosition = transform.position; // Stay in place
        }
        else
        {
            currentState = State.Roam;
            Debug.Log($"Enemy {gameObject.name} started in ROAM state");
            targetPosition = (Vector2)transform.position
                             + new Vector2(Random.Range(-roamDist, roamDist), Random.Range(-roamDist, roamDist));
        }
        
        // Initialize wall avoidance
        lastPosition = transform.position;
        
        // Initialize charge attack system
        if (spriteRenderer != null)
        {
            originalColor = spriteRenderer.color;
        }
    }

    void FixedUpdate()
    {
        Move();
    }
    
    void Move()
    {
        if (rigidBody == null) return;
        
        // Check if movement is disabled or enemy is static type
        if (!enableMovement || type == EnemyType.Static)
        {
            rigidBody.linearVelocity = Vector2.zero;
            return;
        }
        
        // Handle charge attack movement
        if (isCharging)
        {
            rigidBody.linearVelocity = chargeDirection * chargeSpeed;
        }
        else if (isChargingUp)
        {
            // Stop moving while charging up
            rigidBody.linearVelocity = Vector2.zero;
        }
        else if(moveDirection.magnitude > 0.01f) {
            rigidBody.linearVelocity = moveDirection * moveSpeed;
        }
        else
        {
            rigidBody.linearVelocity = Vector2.Lerp(rigidBody.linearVelocity, Vector2.zero, friction);
        }
        
        // Only rotate if moving
        if (moveDirection.magnitude > 0.01f)
        {
            transform.up = Vector3.Lerp(transform.up, (Vector3)moveDirection, Time.fixedDeltaTime * 5f);
        }
    }
    void Update()
    {
        var player = GameObject.FindWithTag("Player");
        if (player == null) return; // No player found, can't target
        
        // Use simple distance calculation for room-based gameplay
        float distanceToPlayer = Vector3.Distance(transform.position, player.transform.position);
        
        if (type == EnemyType.Ranged && distanceToPlayer <= shootDist)
        {
            // Check line of sight before shooting
            if (HasLineOfSight(player.transform))
            {
                rigidBody.linearVelocity = Vector2.zero;
                moveDirection = Vector2.zero;
                if(CanAttackNow()){
                    StartCoroutine(Shoot(moveDirection,shootForce));
                }
                return;
            }
            // If no line of sight, continue with normal state behavior below
        }
        
        bool hasLineOfSight = HasLineOfSight(player.transform);
        
        // Static enemies don't change states, they only shoot
        if (type == EnemyType.Static)
        {
            // Static enemies stay in one place but can still shoot
            moveDirection = Vector2.zero;
            return;
        }
        
        // Handle charge attack states
        if (currentState == State.ChargingUp)
        {
            HandleChargingUp();
            return;
        }
        else if (currentState == State.Charging)
        {
            HandleCharging();
            return;
        }
        
        if (currentState == State.Roam){
            // Check if roaming is enabled
            if (!enableRoaming)
            {
                moveDirection = Vector2.zero;
                return;
            }
            
            float distanceToTarget = Vector3.Distance(transform.position, targetPosition);
                
            if(distanceToTarget < 1f){
                targetPosition = (Vector2)transform.position + new
                    Vector2(Random.Range(-roamDist,roamDist),Random.Range(-roamDist,roamDist));
            }
            // Only chase if chasing is enabled and we can see the player
            if(enableChasing && distanceToPlayer < chaseDist && hasLineOfSight){
                currentState = State.Chase;
            }
        }else if(currentState == State.Chase){
            // If chasing is disabled, return to roam
            if (!enableChasing)
            {
                currentState = State.Roam;
                Debug.Log($"Enemy {gameObject.name} entered ROAM state (chasing disabled)");
                targetPosition = (Vector2)transform.position + new
                    Vector2(Random.Range(-roamDist,roamDist),Random.Range(-roamDist,roamDist));
            }
            // If we lose line of sight, return to roaming
            else if (!hasLineOfSight)
            {
                currentState = State.Roam;
                Debug.Log($"Enemy {gameObject.name} entered ROAM state (lost line of sight)");
                // Set new roam target
                targetPosition = (Vector2)transform.position + new
                    Vector2(Random.Range(-roamDist,roamDist),Random.Range(-roamDist,roamDist));
            }
            else
            {
                targetPosition = player.transform.position;
                
                // Check if we should initiate charge attack
                if (CanInitiateChargeAttack(distanceToPlayer))
                {
                    InitiateChargeAttack(player.transform.position);
                }
                else if(distanceToPlayer < shootDist){
                    currentState = State.Shoot;
                }else if(distanceToPlayer > chaseDist*1.2f){
                    currentState = State.Roam;
                    Debug.Log($"Enemy {gameObject.name} entered ROAM state (player too far away)");
                }
            }
        }else{ // Shoot state
            // If we lose line of sight while shooting, return to roaming
            if (!hasLineOfSight)
            {
                currentState = State.Roam;
                Debug.Log($"Enemy {gameObject.name} entered ROAM state (lost line of sight while shooting)");
                // Set new roam target
                targetPosition = (Vector2)transform.position + new
                    Vector2(Random.Range(-roamDist,roamDist),Random.Range(-roamDist,roamDist));
            }
            else
            {
                targetPosition = player.transform.position;
                if(distanceToPlayer > shootDist){
                    currentState = State.Chase;
                    Debug.Log($"Enemy {gameObject.name} entered CHASE state (player moved away from shoot distance)");
                }
                if(CanAttackNow()){
                    StartCoroutine(Shoot(moveDirection,shootForce));
                }
            }
        }
        
        // Calculate direction directly (room-based, no wrap-around)
        Vector3 direction = targetPosition - transform.position;
        Vector2 desiredDirection = Vector2.zero;
        
        // Only calculate movement if movement is enabled
        if (enableMovement && type != EnemyType.Static && direction.magnitude > 0.1f)
        {
            desiredDirection = direction.normalized;
        }
        
        // Apply wall avoidance if in chase mode and wall avoidance is enabled
        if (currentState == State.Chase && enableWallAvoidance && enableMovement)
        {
            moveDirection = HandleWallAvoidance(desiredDirection);
        }
        else
        {
            moveDirection = desiredDirection;
            // Reset avoidance when not chasing or movement disabled
            if (isAvoidingWall)
            {
                isAvoidingWall = false;
                stuckTimer = 0f;
            }
        }
    }
    
    /// <summary>
    /// Checks if the enemy is currently moving
    /// </summary>
    /// <returns>True if the enemy is moving above the minimum threshold</returns>
    protected bool IsMoving()
    {
        if (rigidBody == null) return false;
        return rigidBody.linearVelocity.magnitude > minMovementThreshold;
    }
    
    /// <summary>
    /// Handles wall avoidance when enemy gets stuck
    /// </summary>
    /// <param name="desiredDirection">The direction the enemy wants to move</param>
    /// <returns>Modified direction to avoid walls</returns>
    private Vector2 HandleWallAvoidance(Vector2 desiredDirection)
    {
        if (!enableWallAvoidance) return desiredDirection;
        
        // Check if we're stuck (not moving much despite wanting to move)
        Vector2 currentPos = transform.position;
        float distanceMoved = Vector2.Distance(currentPos, lastPosition);
        
        if (desiredDirection.magnitude > 0.1f && distanceMoved < stuckThreshold)
        {
            stuckTimer += Time.deltaTime;
            
            if (stuckTimer >= stuckTime && !isAvoidingWall)
            {
                // We're stuck, start wall avoidance
                StartWallAvoidance(desiredDirection);
            }
        }
        else
        {
            stuckTimer = 0f;
            if (!isAvoidingWall)
            {
                lastPosition = currentPos;
            }
        }
        
        // Handle active wall avoidance
        if (isAvoidingWall)
        {
            avoidanceTimer -= Time.deltaTime;
            
            // Check if we can now move toward target
            if (avoidanceTimer <= 0f || !IsWallInDirection(desiredDirection))
            {
                isAvoidingWall = false;
                lastPosition = currentPos;
                return desiredDirection;
            }
            
            return avoidanceDirection;
        }
        
        return desiredDirection;
    }
    
    /// <summary>
    /// Starts wall avoidance behavior
    /// </summary>
    /// <param name="blockedDirection">The direction that's blocked</param>
    private void StartWallAvoidance(Vector2 blockedDirection)
    {
        isAvoidingWall = true;
        avoidanceTimer = avoidanceDuration;
        
        // Try to find a clear direction to move
        Vector2[] testDirections = {
            new Vector2(-blockedDirection.y, blockedDirection.x), // Perpendicular left
            new Vector2(blockedDirection.y, -blockedDirection.x), // Perpendicular right
            -blockedDirection, // Opposite
            Vector2.up, Vector2.down, Vector2.left, Vector2.right // Cardinals
        };
        
        foreach (Vector2 testDir in testDirections)
        {
            if (!IsWallInDirection(testDir))
            {
                avoidanceDirection = testDir.normalized;
                Debug.Log($"Enemy {gameObject.name} avoiding wall, moving {avoidanceDirection}");
                return;
            }
        }
        
        // If all directions blocked, move backward
        avoidanceDirection = -blockedDirection.normalized;
        Debug.Log($"Enemy {gameObject.name} all directions blocked, moving backward");
    }
    
    /// <summary>
    /// Check if there's a wall in the given direction
    /// </summary>
    /// <param name="direction">Direction to check</param>
    /// <returns>True if wall detected</returns>
    private bool IsWallInDirection(Vector2 direction)
    {
        RaycastHit2D hit = Physics2D.Raycast(transform.position, direction, wallDetectionDistance, wallLayers);
        return hit.collider != null && IsWallCollider(hit.collider);
    }
    
    /// <summary>
    /// Checks if the enemy has clear line of sight to the target
    /// </summary>
    /// <param name="target">The target to check line of sight to</param>
    /// <returns>True if there are no walls blocking the view</returns>
    protected bool HasLineOfSight(Transform target)
    {
        if (!requireLineOfSight || target == null) return true;
        
        Vector2 directionToTarget = (target.position - transform.position).normalized;
        float distanceToTarget = Vector2.Distance(transform.position, target.position);
        
        // Start raycast from slightly outside enemy's collider to avoid self-collision
        Vector2 rayStart = (Vector2)transform.position + directionToTarget * 0.2f;
        float adjustedDistance = distanceToTarget - 0.2f;
        
        if (adjustedDistance <= 0) return true; // Target too close to check properly
        
        // Cast multiple rays to be more thorough (center, slightly up, slightly down)
        Vector2[] rayDirections = {
            directionToTarget,
            (directionToTarget + Vector2.up * 0.1f).normalized,
            (directionToTarget + Vector2.down * 0.1f).normalized
        };
        
        foreach (Vector2 rayDir in rayDirections)
        {
            RaycastHit2D hit = Physics2D.Raycast(rayStart, rayDir, adjustedDistance, wallLayers);
            
            // If any ray hits a wall before reaching the target, line of sight is blocked
            if (hit.collider != null && IsWallCollider(hit.collider))
            {
                Debug.Log($"Enemy {gameObject.name} line of sight blocked by {hit.collider.gameObject.name} at distance {hit.distance}");
                return false; // Line of sight blocked by wall
            }
        }
        
        return true; // Clear line of sight
    }
    
    /// <summary>
    /// Checks if a collider is a wall that blocks line of sight
    /// </summary>
    /// <param name="collider">The collider to check</param>
    /// <returns>True if this collider blocks line of sight</returns>
    private bool IsWallCollider(Collider2D collider)
    {
        // Skip trigger colliders (those are for entities, not walls)
        if (collider.isTrigger) return false;
        
        // Skip self and other enemies
        if (collider.gameObject == gameObject) return false;
        if (collider.GetComponent<Enemy>() != null) return false;
        
        // Skip player
        if (collider.CompareTag("Player")) return false;
        
        // Definitely walls - tilemap collision
        if (collider.GetComponent<UnityEngine.Tilemaps.TilemapCollider2D>() != null) return true;
        
        // Check for collision tilemap by name
        if (collider.gameObject.name.ToLower().Contains("collision")) return true;
        
        // Check for walls layer specifically
        int wallsLayer = LayerMask.NameToLayer("Walls");
        if (wallsLayer != -1 && collider.gameObject.layer == wallsLayer) return true;
        
        // Be more conservative - only count as wall if it's explicitly a wall-like object
        // Don't assume all non-trigger colliders are walls
        return false;
    }
    
    /// <summary>
    /// Checks if the enemy can attack in its current state
    /// </summary>
    /// <returns>True if the enemy can attack (considering movement restrictions and inspector settings)</returns>
    protected bool CanAttackNow()
    {
        // Check if shooting is enabled in inspector
        if (!allowShooting) return false;
        
        if (!canShoot) return false;
        
        // If enemy can't attack while moving, check if it's currently moving
        if (!canAttackWhileMoving && IsMoving())
        {
            return false;
        }
        
        return true;
    }
    
    protected IEnumerator Shoot(Vector3 shootDirection, float shootForce)
    {
        canShoot = false;
        
        // Play shoot sound
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayEnemyShoot();
        }
        
        if (enableBurstFire)
        {
            // Burst fire mode - shoot multiple projectiles with delay between them
            for (int i = 0; i < burstCount; i++)
            {
                // Fire a projectile
                int rand = Random.Range(0, projectiles.Length);
                Projectile newBullet = Instantiate(projectiles[rand], transform.position, Quaternion.identity);
                newBullet.gameObject.tag = "Enemy"; // Set enemy tag for collision detection
                newBullet.SetTarget(GameObject.FindWithTag("Player"), this.gameObject);
                
                // Wait between burst shots (but not after the last one)
                if (i < burstCount - 1)
                {
                    yield return new WaitForSeconds(burstDelay);
                }
            }
            
            // Use long reload time after burst
            yield return new WaitForSeconds(longReloadTime);
        }
        else
        {
            // Single shot mode (original behavior)
            int rand = Random.Range(0, projectiles.Length);
            Projectile newBullet = Instantiate(projectiles[rand], transform.position, Quaternion.identity);
            newBullet.gameObject.tag = "Enemy"; // Set enemy tag for collision detection
            newBullet.SetTarget(GameObject.FindWithTag("Player"), this.gameObject);
            
            // Use normal reload time
            yield return new WaitForSeconds(reloadTime);
        }
        
        canShoot = true;
    }

    public void TakeDamage(int damage)
    {
        health -= damage;
        
        // Play hit sound
        if (AudioManager.Instance != null)
        {
            if (health <= 0)
            {
                AudioManager.Instance.PlayEnemyDeath();
            }
            else
            {
                AudioManager.Instance.PlayEnemyHit();
            }
        }
        
        if (health <= 0)
        {
            Die();
        }
    }
    
    protected virtual void Die()
    {
        // Drop gold if enabled and chance succeeds
        if (dropsGold && Random.value <= goldDropChance)
        {
            int goldAmount = Random.Range(minGoldDrop, maxGoldDrop + 1);
            if (Player.Instance != null)
            {
                Player.Instance.GiveGold(goldAmount);
                Debug.Log($"Enemy {gameObject.name} dropped {goldAmount} gold!");
            }
        }
        
        // Notify listeners that this enemy has died
        OnDeath?.Invoke(this);
        
        // Destroy the enemy
        Destroy(gameObject);
    }
    
    /// <summary>
    /// Handle collision with player for contact damage
    /// </summary>
    void OnTriggerEnter2D(Collider2D other)
    {
        Debug.Log($"Enemy {gameObject.name}: Trigger entered by {other.gameObject.name} with tag '{other.tag}' at position {transform.position} vs other at {other.transform.position}");
        
        if (!enableContactDamage)
        {
            Debug.Log($"Enemy {gameObject.name}: Contact damage disabled");
            return;
        }
        
        if (other.CompareTag("Player"))
        {
            float distance = Vector3.Distance(transform.position, other.transform.position);
            Debug.Log($"Enemy {gameObject.name}: Player detected at distance {distance:F2}, checking cooldown...");
            
            // Check if enough time has passed since last contact damage
            if (Time.time - lastContactDamageTime >= contactDamageCooldown)
            {
                // Try multiple ways to get the Player component
                Player player = other.GetComponent<Player>();
                if (player == null)
                {
                    // Try using the static Instance
                    player = Player.Instance;
                }
                if (player == null)
                {
                    // Try finding in parent object
                    player = other.GetComponentInParent<Player>();
                }
                
                if (player != null)
                {
                    if (isCharging)
                    {
                        // Deal charge attack damage
                        Debug.Log($"Enemy {gameObject.name}: Dealing charge attack damage to player!");
                        for (int i = 0; i < chargeAttackDamage; i++)
                        {
                            player.takeDamage();
                        }
                        EndChargeAttack();
                    }
                    else
                    {
                        // Deal normal contact damage
                        Debug.Log($"Enemy {gameObject.name}: Dealing contact damage to player!");
                        player.takeDamage();
                        lastContactDamageTime = Time.time;
                    }
                }
                else
                {
                    Debug.LogWarning($"Enemy {gameObject.name}: Player component not found! GameObject: {other.gameObject.name}, Components: {string.Join(", ", other.GetComponents<Component>().Select(c => c.GetType().Name))}");
                }
            }
            else
            {
                float remainingCooldown = contactDamageCooldown - (Time.time - lastContactDamageTime);
                Debug.Log($"Enemy {gameObject.name}: Contact damage on cooldown ({remainingCooldown:F1}s remaining)");
            }
        }
        else
        {
            Debug.Log($"Enemy {gameObject.name}: Not a player (tag: '{other.tag}')");
        }
    }
    
    /// <summary>
    /// Check if enemy can initiate a charge attack
    /// </summary>
    protected bool CanInitiateChargeAttack(float distanceToPlayer)
    {
        if (!enableChargeAttack) return false;
        if (isChargingUp || isCharging) return false;
        if (Time.time - lastChargeAttackTime < chargeAttackCooldown) return false;
        if (distanceToPlayer > chargeAttackRange) return false;
        
        return true;
    }
    
    /// <summary>
    /// Initiate charge attack sequence
    /// </summary>
    protected void InitiateChargeAttack(Vector3 playerPosition)
    {
        currentState = State.ChargingUp;
        isChargingUp = true;
        chargeUpTimer = chargeUpTime;
        chargeDirection = (playerPosition - transform.position).normalized;
        
        // Play charge sound
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayEnemyCharge();
        }
        
        Debug.Log($"Enemy {gameObject.name} initiating charge attack!");
    }
    
    /// <summary>
    /// Handle charging up state - visual effects and countdown
    /// </summary>
    protected void HandleChargingUp()
    {
        chargeUpTimer -= Time.deltaTime;
        
        // Update visual effect - gradually turn red
        if (spriteRenderer != null)
        {
            float chargeProgress = 1f - (chargeUpTimer / chargeUpTime);
            Color currentColor = Color.Lerp(originalColor, Color.red, chargeProgress);
            spriteRenderer.color = currentColor;
        }
        
        // Finish charging up and start charge
        if (chargeUpTimer <= 0f)
        {
            StartCharge();
        }
        
        // Stop all movement during charge up
        moveDirection = Vector2.zero;
    }
    
    /// <summary>
    /// Start the actual charge attack
    /// </summary>
    protected void StartCharge()
    {
        currentState = State.Charging;
        isChargingUp = false;
        isCharging = true;
        chargeTimer = chargeDuration;
        
        // Update charge direction to current player position for more accuracy
        var player = GameObject.FindWithTag("Player");
        if (player != null)
        {
            chargeDirection = (player.transform.position - transform.position).normalized;
        }
        
        Debug.Log($"Enemy {gameObject.name} starting charge attack!");
    }
    
    /// <summary>
    /// Handle charging state - movement and collision
    /// </summary>
    protected void HandleCharging()
    {
        chargeTimer -= Time.deltaTime;
        
        // Finish charge attack
        if (chargeTimer <= 0f)
        {
            EndChargeAttack();
        }
        
        // Keep moving in charge direction (handled in Move method)
        moveDirection = chargeDirection;
    }
    
    /// <summary>
    /// End charge attack and return to normal behavior
    /// </summary>
    protected void EndChargeAttack()
    {
        isCharging = false;
        isChargingUp = false;
        chargeTimer = 0f;
        chargeUpTimer = 0f;
        lastChargeAttackTime = Time.time;
        
        // Restore original color
        if (spriteRenderer != null)
        {
            spriteRenderer.color = originalColor;
        }
        
        // Return to chase state
        currentState = State.Chase;
        
        Debug.Log($"Enemy {gameObject.name} finished charge attack");
    }
    
    /// <summary>
    /// Handle charge attack collision damage
    /// </summary>
    void OnCollisionEnter2D(Collision2D collision)
    {
        if (!isCharging) return;
        
        if (collision.gameObject.CompareTag("Player"))
        {
            // Deal charge attack damage
            Player player = collision.gameObject.GetComponent<Player>();
            if (player == null)
            {
                player = Player.Instance;
            }
            
            if (player != null)
            {
                // Deal multiple damage for charge attack
                for (int i = 0; i < chargeAttackDamage; i++)
                {
                    player.takeDamage();
                }
                Debug.Log($"Enemy {gameObject.name}: Charge attack hit player for {chargeAttackDamage} damage!");
            }
            
            // End charge attack after hitting player
            EndChargeAttack();
        }
        else
        {
            // Hit wall or obstacle - end charge attack
            Debug.Log($"Enemy {gameObject.name}: Charge attack hit obstacle, ending charge");
            EndChargeAttack();
        }
    }
    
    private void OnDestroy()
    {
        // Clean up event subscriptions
        OnDeath = null;
    }
}