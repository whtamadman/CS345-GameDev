using UnityEngine;
using Random = UnityEngine.Random;
using System.Collections;
using System.Linq;

public class Enemy : MonoBehaviour  {
    [Header("Spawn Delay")]
    [SerializeField] protected float spawnAttackDelay = 1.5f; // Delay after spawn before attacking
    protected float spawnTime = 0f;

    protected Rigidbody2D rigidBody;
    protected SpriteRenderer spriteRenderer;
    public int health;
    [SerializeField] protected int maxHealth = 5;
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

    [Header("Advanced AI")]
    [SerializeField] private bool enableStrafing = true;
    [SerializeField] private float strafeDistance = 5f;
    [SerializeField] private float strafeSpeedMultiplier = 0.85f;
    [SerializeField] private float strafeSwitchInterval = 1.5f;
    [SerializeField] private bool enableSeparation = true;
    [SerializeField] private float separationRadius = 1.5f;
    [SerializeField] private float separationForce = 2.5f;
    [SerializeField] private bool enablePredictiveAim = true;
    [SerializeField] private float predictiveLeadTime = 0.25f;
    [SerializeField] private bool enableRetreat = true;
    [SerializeField] [Range(0f,1f)] private float lowHealthRetreatThreshold = 0.25f;
    [SerializeField] private float retreatDistance = 6f;
    [SerializeField] private float retreatSpeedMultiplier = 1.2f;

    [Header("Pathfinding")]
    [SerializeField] private bool enableLocalPathfinding = true;
    [SerializeField] private int pathSamples = 7; // must be odd to sample both sides evenly
    [SerializeField] private float pathSampleAngle = 15f;
    [SerializeField] private float pathSampleDistance = 2f;
    [SerializeField] private float pathRepathInterval = 0.25f;

    [Header("Item Drop Chance")]
    public GameObject powerUpPrefab;
    [SerializeField] private float foodMisc = 0.06f;
    [SerializeField] private float foodHealth = 0.17f;
    [SerializeField] private float foodMelee = 0.09f;
    [SerializeField] private float foodRange = 0.09f;

    
    // Wall avoidance tracking
    private Vector2 lastPosition;
    private float stuckTimer = 0f;
    private bool isAvoidingWall = false;
    private Vector2 avoidanceDirection = Vector2.zero;
    private float avoidanceTimer = 0f;
    private float avoidanceDuration = 1.5f;
    private float lastStrafeSwitchTime = 0f;
    private int strafeDirectionSign = 1;
    private Vector2 cachedPathDirection = Vector2.zero;
    private float lastPathTime = 0f;
    
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

    void Start(){
        spawnTime = Time.time;
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
            // Lock rotation to Z-axis only (2D rotation) - prevent camera-facing
            rigidBody.constraints = RigidbodyConstraints2D.FreezeRotation;
        }
        
        // Lock transform rotation to Z-axis only (2D) - prevent any camera-facing
        transform.rotation = Quaternion.identity;
        
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
            rigidBody.linearVelocity = Vector2.Lerp(rigidBody.linearVelocity, Vector2.zero, friction * Time.fixedDeltaTime);
            return;
        }
        
        // Handle charge attack movement
        if (isCharging)
        {
            Vector2 targetVelocity = chargeDirection * chargeSpeed;
            rigidBody.linearVelocity = Vector2.MoveTowards(rigidBody.linearVelocity, targetVelocity, moveSpeed * 10f * Time.fixedDeltaTime);
        }
        else if (isChargingUp)
        {
            // Smoothly stop while charging up
            rigidBody.linearVelocity = Vector2.Lerp(rigidBody.linearVelocity, Vector2.zero, friction * 2f * Time.fixedDeltaTime);
        }
        else
        {
            // Smooth acceleration/deceleration for normal movement
            Vector2 targetVelocity = moveDirection * moveSpeed;
            float acceleration = moveDirection.magnitude > 0.01f ? moveSpeed * 8f : friction * 10f;
            rigidBody.linearVelocity = Vector2.MoveTowards(rigidBody.linearVelocity, targetVelocity, acceleration * Time.fixedDeltaTime);
        }
        
        // No rotation - enemies stay facing their original direction
    }
    void Update()
    {
        var player = GameObject.FindWithTag("Player");
        if (player == null) return; // No player found, can't target

        // Prevent attacking for a delay after spawn
        if (Time.time - spawnTime < spawnAttackDelay)
        {
            // Still in spawn delay, skip attack logic
            return;
        }

        // Use simple distance calculation for room-based gameplay
        float distanceToPlayer = Vector3.Distance(transform.position, player.transform.position);

        if (type == EnemyType.Ranged && distanceToPlayer <= shootDist)
        {
            // Check line of sight before shooting
            if (HasLineOfSight(player.transform))
            {
                // Don't stop movement completely - let them continue smoothly
                // This prevents stuttering when entering/exiting shoot range
                if(CanAttackNow()){
                    StartCoroutine(Shoot(moveDirection,shootForce));
                }
                // Continue with normal movement calculation below instead of returning
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
                
            // Retreat if low health
            if (enableRetreat && health > 0 && health <= maxHealth * lowHealthRetreatThreshold)
            {
                Vector2 retreatDir = (transform.position - player.transform.position).normalized;
                targetPosition = transform.position + (Vector3)retreatDir * retreatDistance;
            }
            
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

        // Ranged: strafe around player when in shoot range
        if (type == EnemyType.Ranged && enableStrafing && currentState == State.Shoot && player != null)
        {
            desiredDirection = GetStrafeDirection(player.transform.position);
            desiredDirection *= strafeSpeedMultiplier;
        }
        
        // Ranged: maintain preferred distance
        if (type == EnemyType.Ranged && player != null)
        {
            float dist = Vector2.Distance(transform.position, player.transform.position);
            // Move closer if too far, back off if too close
            if (dist > shootDist * 1.1f)
            {
                desiredDirection = (player.transform.position - transform.position).normalized;
            }
            else if (dist < strafeDistance * 0.8f)
            {
                desiredDirection = (transform.position - player.transform.position).normalized;
            }
        }
        
        // Separation to reduce clustering
        if (enableSeparation)
        {
            desiredDirection = ApplySeparation(desiredDirection);
        }

        // Local pathfinding to pick a clear nearby direction
        desiredDirection = FindPathDirection(desiredDirection);
        
        // Apply wall avoidance if in chase mode and wall avoidance is enabled
        if (currentState == State.Chase && enableWallAvoidance && enableMovement)
        {
            desiredDirection = HandleWallAvoidance(desiredDirection);
        }
        else
        {
            // Reset avoidance when not chasing or movement disabled
            if (isAvoidingWall)
            {
                isAvoidingWall = false;
                stuckTimer = 0f;
            }
        }
        
        // Smooth direction changes to avoid stutter - apply AFTER all calculations
        if (desiredDirection.magnitude < 0.01f)
        {
            // Smoothly decelerate when no direction
            moveDirection = Vector2.Lerp(moveDirection, Vector2.zero, friction * Time.deltaTime);
        }
        else
        {
            // Smoothly interpolate to new direction (higher value = faster response, but still smooth)
            float smoothSpeed = 8f * Time.deltaTime;
            moveDirection = Vector2.Lerp(moveDirection, desiredDirection, smoothSpeed);
            
            // Normalize to maintain consistent speed
            if (moveDirection.magnitude > 0.01f)
            {
                moveDirection = moveDirection.normalized;
            }
        }
    }
    
    /// <summary>
    /// Try to find a nearby clear direction around obstacles (local pathfinding)
    /// </summary>
    private Vector2 FindPathDirection(Vector2 desiredDirection)
    {
        if (!enableLocalPathfinding || desiredDirection.magnitude < 0.01f) return desiredDirection;
        
        // Reuse recent path if still valid
        bool blocked = Physics2D.Raycast(transform.position, desiredDirection, pathSampleDistance, wallLayers);
        if (!blocked && Time.time - lastPathTime < pathRepathInterval && cachedPathDirection != Vector2.zero)
        {
            return cachedPathDirection;
        }
        
        // Sample directions: desired, then alternating left/right by angle steps
        int samples = Mathf.Max(3, pathSamples | 1); // ensure odd
        float halfSamples = (samples - 1) * 0.5f;
        Vector2 bestDir = desiredDirection;
        
        for (int i = 0; i < samples; i++)
        {
            int step = i - (int)halfSamples;
            float angle = step * pathSampleAngle;
            Vector2 candidate = Quaternion.Euler(0, 0, angle) * desiredDirection;
            
            bool hit = Physics2D.Raycast(transform.position, candidate, pathSampleDistance, wallLayers);
            if (!hit)
            {
                bestDir = candidate.normalized;
                cachedPathDirection = bestDir;
                lastPathTime = Time.time;
                return bestDir;
            }
        }
        
        // No clear path found, fall back to original direction
        cachedPathDirection = desiredDirection.normalized;
        lastPathTime = Time.time;
        return cachedPathDirection;
    }
    
    /// <summary>
    /// Calculate a strafe direction around the player
    /// </summary>
    private Vector2 GetStrafeDirection(Vector3 playerPos)
    {
        Vector2 toPlayer = (playerPos - transform.position).normalized;
        // Switch strafe direction periodically
        if (Time.time - lastStrafeSwitchTime >= strafeSwitchInterval)
        {
            strafeDirectionSign = Random.value > 0.5f ? 1 : -1;
            lastStrafeSwitchTime = Time.time;
        }
        Vector2 perpendicular = new Vector2(-toPlayer.y, toPlayer.x) * strafeDirectionSign;
        return perpendicular.normalized;
    }
    
    /// <summary>
    /// Push away from nearby enemies to reduce clustering
    /// </summary>
    private Vector2 ApplySeparation(Vector2 desiredDirection)
    {
        Enemy[] enemies = FindObjectsByType<Enemy>(FindObjectsSortMode.None);
        Vector2 separationVector = Vector2.zero;
        int count = 0;
        
        foreach (Enemy other in enemies)
        {
            if (other == null || other == this) continue;
            float dist = Vector2.Distance(transform.position, other.transform.position);
            if (dist > 0.001f && dist < separationRadius)
            {
                Vector2 away = (Vector2)(transform.position - other.transform.position);
                separationVector += away.normalized * (1f / dist);
                count++;
            }
        }
        
        if (count > 0)
        {
            separationVector /= count;
            Vector2 combined = desiredDirection + separationVector * separationForce;
            if (combined.magnitude > 1f)
            {
                combined = combined.normalized;
            }
            return combined;
        }
        
        return desiredDirection;
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
                return;
            }
        }
        
        // If all directions blocked, move backward
        avoidanceDirection = -blockedDirection.normalized;
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
                FireProjectileWithPrediction();
                
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
            FireProjectileWithPrediction();
            
            // Use normal reload time
            yield return new WaitForSeconds(reloadTime);
        }
        
        canShoot = true;
    }

    /// <summary>
    /// Fire a projectile aiming with optional prediction toward the player
    /// </summary>
    private void FireProjectileWithPrediction()
    {
        int rand = Random.Range(0, projectiles.Length);
        Projectile newBullet = Instantiate(projectiles[rand], transform.position, Quaternion.identity);
        newBullet.gameObject.tag = "Enemy"; // Set enemy tag for collision detection
        
        GameObject playerObj = GameObject.FindWithTag("Player");
        if (playerObj != null && enablePredictiveAim)
        {
            Vector3 targetPos = playerObj.transform.position;
            
            // Predict future position based on player velocity
            Rigidbody2D playerRb = playerObj.GetComponent<Rigidbody2D>();
            if (playerRb != null)
            {
                targetPos += (Vector3)(playerRb.linearVelocity * predictiveLeadTime);
            }
            
            GameObject tempTarget = new GameObject("TempPredictedTarget");
            tempTarget.transform.position = targetPos;
            tempTarget.tag = playerObj.tag; // Preserve target tag for collision checks
            newBullet.SetTarget(tempTarget, this.gameObject);
            Destroy(tempTarget, 0.1f);
        }
        else
        {
            newBullet.SetTarget(playerObj, this.gameObject);
        }
    }


    private Coroutine flashCoroutine;
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
            return;
        }

        if (spriteRenderer != null)
        {
            // Stop any existing flash coroutine before starting a new one
            if (flashCoroutine != null)
            {
                StopCoroutine(flashCoroutine);
            }
            flashCoroutine = StartCoroutine(FlashColor(Color.red, 0.1f));
        }
    }

    IEnumerator FlashColor(Color color, float duration)
    {
        if (spriteRenderer == null) yield break;

        Color original = originalColor;
        spriteRenderer.color = color;
        yield return new WaitForSeconds(duration);
        spriteRenderer.color = original;
        flashCoroutine = null;
    }

    private void SpawnFruit() {
        PowerUpEffect[] food = Resources.LoadAll<PowerUpEffect>("PowerUps/Fruits");
        int randomFoodIndex;
        randomFoodIndex = Random.Range(0, food.Length);
        PowerUpEffect foodChosen = food[randomFoodIndex];
        GameObject foodGO = Instantiate(powerUpPrefab, new Vector2(transform.position.x, transform.position.y), Quaternion.identity);
        PowerUp foodPower = foodGO.GetComponent<PowerUp>();
        foodPower.effect = foodChosen;
    }

    private void SpawnSweet() {
        PowerUpEffect[] food = Resources.LoadAll<PowerUpEffect>("PowerUps/Meat");
        int randomFoodIndex;
        randomFoodIndex = Random.Range(0, food.Length);
        PowerUpEffect foodChosen = food[randomFoodIndex];
        GameObject foodGO = Instantiate(powerUpPrefab, new Vector2(transform.position.x, transform.position.y), Quaternion.identity);
        PowerUp foodPower = foodGO.GetComponent<PowerUp>();
        foodPower.effect = foodChosen;
    }

    private void SpawnMeat() {
        PowerUpEffect[] food = Resources.LoadAll<PowerUpEffect>("PowerUps/Sweets");
        int randomFoodIndex;
        randomFoodIndex = Random.Range(0, food.Length);
        PowerUpEffect foodChosen = food[randomFoodIndex];
        GameObject foodGO = Instantiate(powerUpPrefab, new Vector2(transform.position.x, transform.position.y), Quaternion.identity);
        PowerUp foodPower = foodGO.GetComponent<PowerUp>();
        foodPower.effect = foodChosen;
    }
    
    private void SpawnMisc() {
        PowerUpEffect[] food = Resources.LoadAll<PowerUpEffect>("PowerUps/Misc");
        int randomFoodIndex;
        randomFoodIndex = Random.Range(0, food.Length);
        PowerUpEffect foodChosen = food[randomFoodIndex];
        GameObject foodGO = Instantiate(powerUpPrefab, new Vector2(transform.position.x, transform.position.y), Quaternion.identity);
        PowerUp foodPower = foodGO.GetComponent<PowerUp>();
        foodPower.effect = foodChosen;
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

        if (dropsGold && Random.value <= foodMisc) {
            SpawnMisc();
        } else if (dropsGold && Random.value <= Player.Instance.itemHealthFindMultiplier*foodHealth) {
            SpawnSweet();
        } else if (dropsGold && Random.value <= foodMelee) {
            SpawnMeat();
        } else if (dropsGold && Random.value <= foodRange) {
            SpawnFruit();
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
            }
            
            // End charge attack after hitting player
            EndChargeAttack();
        }
        else
        {
            // Hit wall or obstacle - end charge attack
            EndChargeAttack();
        }
    }
    
    private void OnDestroy()
    {
        // Clean up event subscriptions
        OnDeath = null;
    }
}