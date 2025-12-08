using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class VirusBoss : MonoBehaviour
{
    [Header("Boss Health")]
    [SerializeField] private int maxHealth = 100;
    [SerializeField] private int currentHealth;
    
    [Header("Boss Configuration")]
    [SerializeField] private string opponentTag = "Player";
    [SerializeField] private bool isStatic = true; // Boss doesn't move
    
    [Header("Contact Damage")]
    [SerializeField] private bool enableContactDamage = true;
    [SerializeField] private int contactDamage = 2;
    [SerializeField] private float contactDamageCooldown = 1.0f;
    private float lastContactDamageTime = -999f; // Initialize to allow immediate contact damage
    
    [Header("Gold Drop Settings")]
    [SerializeField] private bool dropsGold = true;
    [SerializeField] [Range(0f, 1f)] private float goldDropChance = 1.0f; // Boss always drops gold
    [SerializeField] private int minGoldDrop = 50;
    [SerializeField] private int maxGoldDrop = 100;
    
    [Header("Attack Settings")]
    [SerializeField] private float attackCooldown = 3.0f;
    private float lastAttackTime = -999f; // Initialize to allow immediate attacks
    [SerializeField] private float attackRange = 15f; // Range to detect player for attacks
    
    [Header("Attack Telegraph System")]
    [SerializeField] private bool showAttackTelegraph = true; // Visual telegraph enabled
    [SerializeField] private float burstfireTelegraphDuration = 1.2f; // Burstfire attack warning time
    [SerializeField] private float minionTelegraphDuration = 1.5f; // Minion spawn warning time (longer for powerful attack)
    [SerializeField] private float teleportTelegraphDuration = 1.5f; // Teleport attack warning time
    [SerializeField] private Color burstfireTelegraphColor = Color.red; // Burstfire projectile telegraph color
    [SerializeField] private Color minionTelegraphColor = Color.green; // Minion spawn telegraph color
    [SerializeField] private Color teleportTelegraphColor = Color.magenta; // Teleport attack telegraph color
    
    [Header("Burstfire Projectile Attack")]
    [SerializeField] private GameObject burstfireProjectilePrefab;
    [SerializeField] private int minBurstfireProjectileCount = 2;
    [SerializeField] private int maxBurstfireProjectileCount = 5;
    [SerializeField] private float burstfireProjectileDelay = 0.3f;
    [SerializeField] private float burstfireProjectileSpeed = 8f;
    [SerializeField] private float burstfireAttackCooldown = 2.0f;
    private float lastBurstfireAttackTime = -999f; // Initialize to allow immediate attacks
    
    [Header("Minion Spawn Attack")]
    [SerializeField] private GameObject[] phase1MinionPrefabs;
    [SerializeField] private GameObject[] phase2MinionPrefabs;
    [SerializeField] private GameObject[] phase3MinionPrefabs;
    [SerializeField] private int minionsPerSpawn = 2;
    [SerializeField] private float minionSpawnRadius = 3f;
    [SerializeField] private int maxMinionsAlive = 6;
    [SerializeField] private float minionSpawnCooldown = 4.0f;
    private float lastMinionSpawnTime = -999f; // Initialize to allow immediate attacks
    private List<GameObject> aliveMinions = new List<GameObject>();
    
    [Header("Teleportation Attack")]
    [SerializeField] private int teleportCount = 3; // Number of teleports per attack
    [SerializeField] private float teleportDelay = 0.8f; // Time between teleports
    [SerializeField] private float teleportContactDamage = 2f; // Damage when appearing on player
    [SerializeField] private float teleportRange = 1.5f; // How close to player to teleport
    [SerializeField] private float teleportAttackCooldown = 2f;
    private float lastTeleportAttackTime = -999f; // Initialize to allow immediate attacks
    
    [Header("Boss Phases")]
    [SerializeField] private float phase2HealthThreshold = 0.66f; // 66% health
    [SerializeField] private float phase3HealthThreshold = 0.33f; // 33% health
    
    [Header("Phase Scaling")]
    [SerializeField] private float phase2CooldownMultiplier = 0.8f; // 20% faster attacks
    [SerializeField] private float phase3CooldownMultiplier = 0.6f; // 40% faster attacks
    [SerializeField] private float phase2SpeedMultiplier = 1.2f; // 20% faster projectiles
    [SerializeField] private float phase3SpeedMultiplier = 1.5f; // 50% faster projectiles
    
    [Header("Attack Testing Toggles")]
    [SerializeField] private bool enableBurstfireAttack = true;
    [SerializeField] private bool enableMinionSpawnAttack = true;
    [SerializeField] private bool enableTeleportAttack = true;
    
    // Room reference for boundary checking
    private Room bossRoom;
    
    // Components
    private SpriteRenderer spriteRenderer;
    private Rigidbody2D rigidBody;
    
    // Boss state
    private bool isAttacking = false;
    private bool isDead = false;
    
    // Phase tracking
    public enum BossPhase
    {
        Phase1,
        Phase2,
        Phase3
    }
    
    private BossPhase currentPhase = BossPhase.Phase1;
    private int attackCounter = 0;
    private Color originalColor = Color.white;
    
    // Events
    public System.Action<VirusBoss> OnBossDeath;
    
    void Start()
    {
        // Initialize components
        spriteRenderer = GetComponent<SpriteRenderer>();
        rigidBody = GetComponent<Rigidbody2D>();
        
        // Initialize boss
        currentHealth = maxHealth;
        
        // Store original color for visual effects
        if (spriteRenderer != null)
        {
            originalColor = spriteRenderer.color;
        }
        
        // Static boss setup
        if (isStatic && rigidBody != null)
        {
            rigidBody.bodyType = RigidbodyType2D.Kinematic;
            rigidBody.linearVelocity = Vector2.zero;
        }
        
        Debug.Log($"Virus Boss (Independent) initialized with {maxHealth} health");
        Debug.Log($"Prefab assignments - Burstfire: {(burstfireProjectilePrefab != null ? "OK" : "MISSING")}, Phase1 Minions: {(phase1MinionPrefabs?.Length > 0 ? "OK" : "MISSING")}, Phase2 Minions: {(phase2MinionPrefabs?.Length > 0 ? "OK" : "MISSING")}, Phase3 Minions: {(phase3MinionPrefabs?.Length > 0 ? "OK" : "MISSING")}");
        Debug.Log($"Attack settings - Range: {attackRange}, Cooldown: {attackCooldown}");
        Debug.Log($"Boss GameObject tag: {gameObject.tag} (should be 'Enemy' for projectile damage)");
    }
    
    void Update()
    {
        if (isDead) return;
        
        // Face the player if not static
        if (!isStatic)
        {
            FacePlayer();
        }
        
        // Check for phase transitions
        CheckPhaseTransitions();
        
        // Handle boss attacks
        if (CanAttack() && Time.time - lastAttackTime >= GetCurrentAttackCooldown())
        {
            StartCoroutine(PerformBossAttack());
        }
        
        // Clean up dead minions
        CleanUpMinionsList();
    }
    
    /// <summary>
    /// Make boss face the player
    /// </summary>
    private void FacePlayer()
    {
        GameObject player = GameObject.FindWithTag(opponentTag);
        if (player != null)
        {
            Vector3 direction = player.transform.position - transform.position;
            if (direction.magnitude > 0.1f)
            {
                transform.up = Vector3.Lerp(transform.up, direction.normalized, Time.deltaTime * 2f);
            }
        }
    }
    
    /// <summary>
    /// Check if boss can attack
    /// </summary>
    private bool CanAttack()
    {
        if (isAttacking || isDead) 
        {
            return false;
        }
        
        GameObject player = GameObject.FindWithTag(opponentTag);
        if (player == null) 
        {
            Debug.Log($"No player found with tag '{opponentTag}'");
            return false;
        }
        
        // Check if player is within attack range
        float distanceToPlayer = Vector3.Distance(transform.position, player.transform.position);
        bool inRange = distanceToPlayer <= attackRange;
        return inRange;
    }
    
    /// <summary>
    /// Check for phase transitions based on health
    /// </summary>
    private void CheckPhaseTransitions()
    {
        float healthPercent = (float)currentHealth / maxHealth;
        
        if (currentPhase == BossPhase.Phase1 && healthPercent <= phase2HealthThreshold)
        {
            TransitionToPhase(BossPhase.Phase2);
        }
        else if (currentPhase == BossPhase.Phase2 && healthPercent <= phase3HealthThreshold)
        {
            TransitionToPhase(BossPhase.Phase3);
        }
    }
    
    /// <summary>
    /// Transition to new phase
    /// </summary>
    private void TransitionToPhase(BossPhase newPhase)
    {
        currentPhase = newPhase;
        
        // Visual effect for phase transition
        if (spriteRenderer != null)
        {
            StartCoroutine(PhaseTransitionEffect());
        }
        
        // Play phase transition sound
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayEnemyCharge();
        }
        
        Debug.Log($"Virus Boss entered {newPhase}!");
    }
    
    /// <summary>
    /// Visual effect for phase transitions
    /// </summary>
    private IEnumerator PhaseTransitionEffect()
    {
        // Phase transition without color changes
        // Just wait a brief moment for audio effect
        yield return new WaitForSeconds(0.3f);
    }
    
    /// <summary>
    /// Get attack cooldown based on current phase
    /// </summary>
    private float GetCurrentAttackCooldown()
    {
        switch (currentPhase)
        {
            case BossPhase.Phase1:
                return attackCooldown;
            case BossPhase.Phase2:
                return attackCooldown * phase2CooldownMultiplier;
            case BossPhase.Phase3:
                return attackCooldown * phase3CooldownMultiplier;
            default:
                return attackCooldown;
        }
    }
    
    /// <summary>
    /// Get list of available attacks based on toggle settings
    /// </summary>
    private List<int> GetAvailableAttacks()
    {
        List<int> availableAttacks = new List<int>();
        
        if (enableBurstfireAttack)
            availableAttacks.Add(0);
        if (enableMinionSpawnAttack)
            availableAttacks.Add(1);
        if (enableTeleportAttack)
            availableAttacks.Add(2);
            
        return availableAttacks;
    }
    
    /// <summary>
    /// Get projectile speed multiplier for current phase
    /// </summary>
    private float GetCurrentProjectileSpeedMultiplier()
    {
        switch (currentPhase)
        {
            case BossPhase.Phase1:
                return 1f;
            case BossPhase.Phase2:
                return phase2SpeedMultiplier;
            case BossPhase.Phase3:
                return phase3SpeedMultiplier;
            default:
                return 1f;
        }
    }
    
    /// <summary>
    /// Get minion prefabs for current phase
    /// </summary>
    private GameObject[] GetCurrentPhaseMinionPrefabs()
    {
        switch (currentPhase)
        {
            case BossPhase.Phase1:
                return phase1MinionPrefabs;
            case BossPhase.Phase2:
                return phase2MinionPrefabs;
            case BossPhase.Phase3:
                return phase3MinionPrefabs;
            default:
                return phase1MinionPrefabs;
        }
    }
    
    /// <summary>
    /// Perform boss attack
    /// </summary>
    private IEnumerator PerformBossAttack()
    {
        isAttacking = true;
        lastAttackTime = Time.time;
        
        yield return StartCoroutine(ChooseAndExecuteAttack());
        
        attackCounter++;
        isAttacking = false;
    }
    
    /// <summary>
    /// Choose attack based on available toggles (ignores phases for testing)
    /// </summary>
    private IEnumerator ChooseAndExecuteAttack()
    {
        // Get available attacks based on toggles
        List<int> availableAttacks = GetAvailableAttacks();
        
        if (availableAttacks.Count == 0)
        {
            Debug.LogWarning("No attacks are enabled! Skipping attack.");
            yield break;
        }
        
        // Choose attack by priority: Teleport > Spawn > Homing
        int selectedAttack = -1;
        
        // Priority 1: Teleport Attack (if available and not on cooldown)
        if (enableTeleportAttack && Time.time - lastTeleportAttackTime >= teleportAttackCooldown)
        {
            selectedAttack = 2;
        }
        // Priority 2: Minion Spawn Attack (if available and not on cooldown)
        else if (enableMinionSpawnAttack && Time.time - lastMinionSpawnTime >= minionSpawnCooldown)
        {
            selectedAttack = 1;
        }
        // Priority 3: Burstfire Attack (if available and not on cooldown)
        else if (enableBurstfireAttack && Time.time - lastBurstfireAttackTime >= burstfireAttackCooldown)
        {
            selectedAttack = 0;
        }
        
        Debug.Log($"Selected priority attack {selectedAttack} (2=Teleport, 1=Spawn, 0=Burstfire)");
        
        switch (selectedAttack)
        {
            case 0:
                yield return StartCoroutine(BurstfireProjectileAttack());
                break;
            case 1:
                yield return StartCoroutine(MinionSpawnAttack());
                break;
            case 2:
                yield return StartCoroutine(TeleportAttack());
                break;
            default:
                Debug.Log("No attacks available due to cooldowns");
                yield break;
        }
    }
    
    /// <summary>
    /// Phase 1 attack pattern
    /// </summary>
    private IEnumerator Phase1AttackPattern()
    {
        int attackType = attackCounter % 3;
        
        switch (attackType)
        {
            case 0:
                yield return StartCoroutine(BurstfireProjectileAttack());
                break;
            case 1:
                yield return StartCoroutine(MinionSpawnAttack());
                break;
            case 2:
                yield return StartCoroutine(TeleportAttack());
                break;
        }
    }
    
    /// <summary>
    /// Phase 2 attack pattern - combination attacks
    /// </summary>
    private IEnumerator Phase2AttackPattern()
    {
        int attackType = attackCounter % 4;
        
        switch (attackType)
        {
            case 0:
                // Burstfire + Teleport combo
                yield return StartCoroutine(BurstfireProjectileAttack());
                yield return new WaitForSeconds(0.5f);
                yield return StartCoroutine(TeleportAttack());
                break;
            case 1:
                yield return StartCoroutine(MinionSpawnAttack());
                break;
            case 2:
                // Double burstfire
                yield return StartCoroutine(BurstfireProjectileAttack());
                yield return new WaitForSeconds(0.3f);
                yield return StartCoroutine(BurstfireProjectileAttack());
                break;
            case 3:
                yield return StartCoroutine(TeleportAttack());
                break;
        }
    }
    
    /// <summary>
    /// Phase 3 attack pattern - intense combinations
    /// </summary>
    private IEnumerator Phase3AttackPattern()
    {
        int attackType = attackCounter % 3;
        
        switch (attackType)
        {
            case 0:
                // Triple threat
                yield return StartCoroutine(BurstfireProjectileAttack());
                yield return new WaitForSeconds(0.3f);
                yield return StartCoroutine(TeleportAttack());
                yield return new WaitForSeconds(0.3f);
                yield return StartCoroutine(MinionSpawnAttack());
                break;
            case 1:
                // Double teleport
                yield return StartCoroutine(TeleportAttack());
                yield return new WaitForSeconds(0.4f);
                yield return StartCoroutine(TeleportAttack());
                break;
            case 2:
                // Burstfire spam
                for (int i = 0; i < 3; i++)
                {
                    yield return StartCoroutine(BurstfireProjectileAttack());
                    yield return new WaitForSeconds(0.2f);
                }
                break;
        }
    }
    
    /// <summary>
    /// Attack 1: Burstfire Projectiles
    /// </summary>
    private IEnumerator BurstfireProjectileAttack()
    {
        // Check individual cooldown for this attack
        if (Time.time - lastBurstfireAttackTime < burstfireAttackCooldown)
        {
            Debug.Log("Burstfire attack on cooldown, skipping");
            yield break;
        }
        
        Debug.Log("Virus Boss: Preparing Burstfire Projectile Attack!");
        lastBurstfireAttackTime = Time.time;
        
        // Attack telegraph - warn the player
        if (showAttackTelegraph)
        {
            yield return StartCoroutine(AttackTelegraph("BURSTFIRE MISSILES INCOMING!", burstfireTelegraphColor, burstfireTelegraphDuration));
        }
        
        Debug.Log("Virus Boss: Executing Burstfire Projectile Attack!");
        
        GameObject player = GameObject.FindWithTag(opponentTag);
        if (player == null || burstfireProjectilePrefab == null) yield break;
        
        // Play attack sound
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayEnemyShoot();
        }
        
        // Determine random number of projectiles to fire
        int projectileCount = Random.Range(minBurstfireProjectileCount, maxBurstfireProjectileCount + 1);
        Debug.Log($"Firing {projectileCount} burstfire projectiles");
        
        for (int i = 0; i < projectileCount; i++)
        {
            // Create burstfire projectile
            Vector3 spawnPos = transform.position;
            Debug.Log($"Creating burstfire projectile {i+1} at {spawnPos}");
            
            GameObject burstfireProjectile = Instantiate(burstfireProjectilePrefab, spawnPos, Quaternion.identity);
            
            if (burstfireProjectile == null)
            {
                Debug.LogError("Failed to instantiate burstfire projectile!");
                continue;
            }
            
            // Tag as enemy projectile
            burstfireProjectile.tag = "Enemy";
            
            // Configure projectile like enemies do
            Projectile projectileComponent = burstfireProjectile.GetComponent<Projectile>();
            if (projectileComponent != null)
            {
                // Set speed with phase scaling
                projectileComponent.speed = burstfireProjectileSpeed * GetCurrentProjectileSpeedMultiplier();
                
                // Use SetTarget like enemies do - pass player GameObject and boss as source
                projectileComponent.SetTarget(player, gameObject);
                
                Debug.Log($"Burstfire projectile {i+1} configured with Projectile component - Speed: {projectileComponent.speed}");
            }
            else
            {
                // Fallback: Add homing behavior if no Projectile component
                HomingProjectileBehavior homingBehavior = burstfireProjectile.GetComponent<HomingProjectileBehavior>();
                if (homingBehavior == null)
                {
                    homingBehavior = burstfireProjectile.AddComponent<HomingProjectileBehavior>();
                }
                homingBehavior.Initialize(player.transform, burstfireProjectileSpeed * GetCurrentProjectileSpeedMultiplier());
                
                Debug.Log($"Burstfire projectile {i+1} using fallback HomingProjectileBehavior");
            }
            
            Debug.Log($"Burstfire projectile {i+1} created successfully");
            yield return new WaitForSeconds(burstfireProjectileDelay);
        }
    }
    
    /// <summary>
    /// Attack 2: Spawn Minions
    /// </summary>
    private IEnumerator MinionSpawnAttack()
    {
        // Check individual cooldown for this attack
        if (Time.time - lastMinionSpawnTime < minionSpawnCooldown)
        {
            Debug.Log("Minion spawn attack on cooldown, skipping");
            yield break;
        }
        
        Debug.Log("Virus Boss: Preparing Minion Spawn Attack!");
        
        // Get current phase minion prefabs
        GameObject[] currentPhasePrefabs = GetCurrentPhaseMinionPrefabs();
        if (currentPhasePrefabs == null || currentPhasePrefabs.Length == 0) 
        {
            Debug.LogWarning($"No minion prefabs available for {currentPhase}!");
            yield break;
        }
        
        // Attack telegraph - warn the player
        if (showAttackTelegraph)
        {
            yield return StartCoroutine(AttackTelegraph("SUMMONING MINIONS!", minionTelegraphColor, minionTelegraphDuration));
        }
        
        Debug.Log("Virus Boss: Executing Minion Spawn Attack!");
        
        // Clean up dead minions first
        CleanUpMinionsList();
        
        // Strict enforcement: Don't spawn if at or above max minions
        if (aliveMinions.Count >= maxMinionsAlive)
        {
            Debug.Log($"Max minions reached ({aliveMinions.Count}/{maxMinionsAlive}), skipping spawn");
            yield break;
        }
        
        // Calculate how many minions we can actually spawn
        int availableSlots = maxMinionsAlive - aliveMinions.Count;
        int actualSpawnCount = Mathf.Min(minionsPerSpawn, availableSlots);
        
        if (actualSpawnCount <= 0)
        {
            Debug.Log("No available minion slots, skipping spawn");
            yield break;
        }
        
        Debug.Log($"Spawning {actualSpawnCount} minions (available slots: {availableSlots})");
        
        lastMinionSpawnTime = Time.time;
        
        // Play spawn sound
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayEnemyCharge();
        }
        
        // Visual effect
        if (spriteRenderer != null)
        {
            Color originalColor = spriteRenderer.color;
            spriteRenderer.color = Color.green;
            yield return new WaitForSeconds(0.5f);
            spriteRenderer.color = originalColor;
        }
        
        // Use simple circular spawning for reliability (constrained to boss room)
        Debug.Log($"Spawning {actualSpawnCount} minions around boss using fallback method");
        
        Room bossRoom = FindRoomContainingBoss();
        float maxRadius = bossRoom != null ? Mathf.Min(bossRoom.interiorSize.x, bossRoom.interiorSize.y) * 0.15f : minionSpawnRadius;
        maxRadius = Mathf.Max(maxRadius, 2f); // Minimum radius
        maxRadius = Mathf.Min(maxRadius, 5f); // Maximum radius
        
        for (int i = 0; i < actualSpawnCount; i++)
        {
            float angle = (360f / actualSpawnCount) * i * Mathf.Deg2Rad;
            Vector3 spawnPosition = transform.position + new Vector3(
                Mathf.Cos(angle) * maxRadius,
                Mathf.Sin(angle) * maxRadius,
                0
            );
            
            // If boss is in a room, ensure spawn position stays in that room
            if (bossRoom != null && !IsPositionInRoom(spawnPosition, bossRoom))
            {
                // Try closer to boss
                spawnPosition = transform.position + new Vector3(
                    Mathf.Cos(angle) * (maxRadius * 0.5f),
                    Mathf.Sin(angle) * (maxRadius * 0.5f),
                    0
                );
            }
            
            // Select random minion prefab from current phase
            GameObject selectedPrefab = currentPhasePrefabs[Random.Range(0, currentPhasePrefabs.Length)];
            Debug.Log($"Spawning minion {i+1} (Type: {selectedPrefab.name}) at position {spawnPosition}");
            GameObject minion = Instantiate(selectedPrefab, spawnPosition, Quaternion.identity);
            
            if (minion == null)
            {
                Debug.LogError($"Failed to instantiate minion {i+1}!");
                continue;
            }
            
            aliveMinions.Add(minion);
            
            // Set up minion death tracking
            Enemy minionEnemy = minion.GetComponent<Enemy>();
            if (minionEnemy != null)
            {
                minionEnemy.OnDeath += OnMinionDeath;
                Debug.Log($"Minion {i+1} death tracking set up");
            }
            else
            {
                Debug.LogWarning($"Minion {minion.name} doesn't have Enemy component!");
            }
            
            yield return new WaitForSeconds(0.2f);
        }
        
        Debug.Log($"Minion spawn complete. Total minions alive: {aliveMinions.Count}");
    }
    
    /// <summary>
    /// Helper method to clamp position within room bounds
    /// </summary>
    private Vector3 ClampPositionToRoom(Vector3 position, Room room)
    {
        if (room == null) return position;
        
        // Simple boundary clamping - you may need to adjust based on your Room class implementation
        Vector3 roomCenter = room.transform.position;
        float roomWidth = 10f; // Adjust based on your room size
        float roomHeight = 10f; // Adjust based on your room size
        
        float clampedX = Mathf.Clamp(position.x, roomCenter.x - roomWidth/2, roomCenter.x + roomWidth/2);
        float clampedY = Mathf.Clamp(position.y, roomCenter.y - roomHeight/2, roomCenter.y + roomHeight/2);
        
        return new Vector3(clampedX, clampedY, position.z);
    }
    
    /// <summary>
    /// Attack 3: Teleportation Strike - Teleport directly to player for contact damage
    /// </summary>
    private IEnumerator TeleportAttack()
    {
        // Check individual cooldown for this attack
        if (Time.time - lastTeleportAttackTime < teleportAttackCooldown)
        {
            Debug.Log("Teleport attack on cooldown, skipping");
            yield break;
        }
        
        Debug.Log("Virus Boss: Preparing Teleportation Attack!");
        lastTeleportAttackTime = Time.time;
        
        Player player = Player.Instance;
        if (player == null) 
        {
            Debug.LogWarning("Player not found for teleport attack!");
            yield break;
        }
        
        // Attack telegraph - warn the player
        if (showAttackTelegraph)
        {
            yield return StartCoroutine(AttackTelegraph("TELEPORT STRIKE!", teleportTelegraphColor, teleportTelegraphDuration));
        }
        
        Debug.Log("Virus Boss: Executing Teleportation Attack!");
        
        // Store original position for returning
        Vector3 originalPosition = transform.position;
        
        // Temporarily disable regular contact damage during teleport attack
        bool originalContactDamage = enableContactDamage;
        enableContactDamage = false;
        
        // Perform multiple teleports
        for (int i = 0; i < teleportCount; i++)
        {
            // Calculate teleport position directly on player
            Vector3 playerPos = player.transform.position;
            Vector3 teleportPos = playerPos; // Teleport directly on player position
            
            // Ensure teleport position is within room bounds if boss has a room
            if (bossRoom != null)
            {
                teleportPos = ClampPositionToRoom(teleportPos, bossRoom);
            }
            
            // Visual effect before teleport
            if (spriteRenderer != null)
            {
                spriteRenderer.color = Color.clear; // Fade out
            }
            
            // Play teleport sound
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayEnemyCharge();
            }
            
            yield return new WaitForSeconds(0.2f); // Brief invisibility
            
            // Teleport to new position
            transform.position = teleportPos;
            
            // Visual effect after teleport - keep teleport color
            if (spriteRenderer != null)
            {
                spriteRenderer.color = teleportTelegraphColor; // Keep teleport color while teleporting
            }
            
            Debug.Log($"Boss teleported to {teleportPos} near player (attempt {i+1}/{teleportCount})");
            
            // Check if we're close enough to deal damage
            float distanceToPlayer = Vector3.Distance(transform.position, player.transform.position);
            if (distanceToPlayer <= teleportRange) 
            {
                // Deal teleport contact damage (single hit with specific damage amount)
                player.takeDamage((int)teleportContactDamage);
                Debug.Log($"Teleport attack dealt {teleportContactDamage} damage to player!");
                
                // Visual damage effect
                if (spriteRenderer != null)
                {
                    spriteRenderer.color = Color.red;
                    yield return new WaitForSeconds(0.1f);
                    spriteRenderer.color = teleportTelegraphColor; // Return to teleport color
                }
            }
            
            // Wait between teleports (except for the last one)
            if (i < teleportCount - 1)
            {
                yield return new WaitForSeconds(teleportDelay);
            }
        }
        
        // Brief pause before finishing
        yield return new WaitForSeconds(0.5f);
        
        // Re-enable regular contact damage
        enableContactDamage = originalContactDamage;
        
        // Final visual effect - return to normal color but stay at current position
        if (spriteRenderer != null)
        {
            spriteRenderer.color = originalColor; // Return to normal color
        }
        
        Debug.Log("Teleportation attack completed - boss stays at new position");
    }
    

    
    /// <summary>
    /// Handle minion death
    /// </summary>
    private void OnMinionDeath(Enemy minion)
    {
        if (aliveMinions.Contains(minion.gameObject))
        {
            aliveMinions.Remove(minion.gameObject);
        }
    }
    
    /// <summary>
    /// Clean up dead minions from list
    /// </summary>
    private void CleanUpMinionsList()
    {
        for (int i = aliveMinions.Count - 1; i >= 0; i--)
        {
            if (aliveMinions[i] == null)
            {
                aliveMinions.RemoveAt(i);
            }
        }
    }
    
    /// <summary>
    /// Take damage - PUBLIC method that can be called by weapons/projectiles
    /// </summary>
    public void TakeDamage(int damage)
    {
        if (isDead) 
        {
            Debug.Log("Boss is already dead, ignoring damage");
            return;
        }
        
        Debug.Log($"*** VIRUS BOSS TAKING DAMAGE *** {damage} damage. Health: {currentHealth}/{maxHealth} -> {currentHealth - damage}/{maxHealth}");
        
        currentHealth -= damage;
        
        // Clamp health
        if (currentHealth < 0)
        {
            currentHealth = 0;
        }
        
        // Play hit sound
        if (AudioManager.Instance != null)
        {
            if (currentHealth <= 0)
            {
                AudioManager.Instance.PlayEnemyDeath();
            }
            else
            {
                AudioManager.Instance.PlayEnemyHit();
            }
        }
        
        // Visual damage feedback
        if (spriteRenderer != null)
        {
            StartCoroutine(DamageFlash());
        }
        
        Debug.Log($"Boss health after damage: {currentHealth}/{maxHealth}");
        
        // Check for death
        if (currentHealth <= 0)
        {
            Debug.Log("Boss health reached 0, calling Die()");
            Die();
        }
    }
    
    /// <summary>
    /// Visual damage flash
    /// </summary>
    private IEnumerator DamageFlash()
    {
        spriteRenderer.color = Color.white;
        yield return new WaitForSeconds(0.1f);
        spriteRenderer.color = originalColor;
    }
    
    /// <summary>
    /// Attack telegraph system - warns player before attacks
    /// </summary>
    private IEnumerator AttackTelegraph(string attackName, Color telegraphColor, float duration)
    {
        Debug.Log($"*** {attackName} *** (Telegraph Duration: {duration}s)");
        
        if (spriteRenderer != null)
        {
            Color originalColor = spriteRenderer.color;
            
            // Pulse effect during telegraph
            float telegraphTimer = 0f;
            while (telegraphTimer < duration)
            {
                float pulse = Mathf.Sin(telegraphTimer * 10f) * 0.5f + 0.5f; // Pulse between 0 and 1
                spriteRenderer.color = Color.Lerp(originalColor, telegraphColor, pulse * 0.7f);
                
                telegraphTimer += Time.deltaTime;
                yield return null;
            }
            
            // Restore original color
            spriteRenderer.color = originalColor;
        }
        else
        {
            // Fallback timing if no sprite renderer
            yield return new WaitForSeconds(duration);
        }
    }
    
    /// <summary>
    /// Boss death
    /// </summary>
    private void Die()
    {
        if (isDead) return;
        
        isDead = true;
        Debug.Log("Virus Boss defeated!");
        
        StartCoroutine(DeathSequence());
    }
    
    /// <summary>
    /// Death sequence with effects
    /// </summary>
    private IEnumerator DeathSequence()
    {
        // Stop all attacks
        isAttacking = true;
        
        // Death effect
        if (spriteRenderer != null)
        {
            for (int i = 0; i < 5; i++)
            {
                spriteRenderer.color = Color.red;
                yield return new WaitForSeconds(0.1f);
                spriteRenderer.color = Color.white;
                yield return new WaitForSeconds(0.1f);
            }
        }
        
        // Drop gold
        if (dropsGold && Random.value <= goldDropChance)
        {
            int goldAmount = Random.Range(minGoldDrop, maxGoldDrop + 1);
            if (Player.Instance != null)
            {
                Player.Instance.GiveGold(goldAmount);
                Debug.Log($"Virus Boss dropped {goldAmount} gold!");
            }
        }
        
        // Destroy minions
        foreach (GameObject minion in aliveMinions)
        {
            if (minion != null)
            {
                Destroy(minion);
            }
        }
        aliveMinions.Clear();
        
        // Notify death
        OnBossDeath?.Invoke(this);
        
        // Destroy boss
        Destroy(gameObject);
    }
    
    /// <summary>
    /// Handle contact damage and incoming damage from player weapons
    /// </summary>
    void OnTriggerEnter2D(Collider2D other)
    {
        if (isDead) return;
        
        Debug.Log($"Boss trigger hit by: {other.name}, Tag: {other.tag}, Components: {string.Join(", ", other.GetComponents<Component>().Select(c => c.GetType().Name))}");
        
        // Handle player contact (boss damages player)
        if (enableContactDamage && other.CompareTag(opponentTag))
        {
            // Check cooldown
            if (Time.time - lastContactDamageTime >= contactDamageCooldown)
            {
                Player player = other.GetComponent<Player>();
                if (player == null)
                {
                    player = Player.Instance;
                }
                
                if (player != null)
                {
                    // Deal contact damage (single hit with specific damage amount)
                    player.takeDamage(contactDamage);
                    lastContactDamageTime = Time.time;
                    Debug.Log($"Virus Boss dealt {contactDamage} contact damage to player!");
                }
            }
        }
        
        // Handle incoming damage from player projectiles
        // Note: Player projectiles check for "Enemy" tag, so boss must be tagged as "Enemy"
        if (other.GetComponent<Projectile>() != null)
        {
            // This is a projectile hitting us
            Projectile projectile = other.GetComponent<Projectile>();
            Debug.Log($"Boss hit by projectile: {other.name}, Tag: {other.tag}");
            
            // Only take damage from player projectiles, not enemy projectiles
            // Player projectiles are NOT tagged as "Enemy" and don't have Enemy component
            bool isEnemyProjectile = other.CompareTag("Enemy") || other.GetComponent<Enemy>() != null;
            
            if (!isEnemyProjectile)
            {
                Debug.Log("Taking damage from player projectile");
                TakeDamage((int)Player.Instance.baseRangeDamage + (int)Player.Instance.baseRangeModifier);
                
                // Destroy the projectile
                Destroy(other.gameObject);
            }
            else
            {
                Debug.Log("Ignoring damage from enemy projectile");
            }
        }
        
        // Handle incoming damage from melee weapons (only from player)
        if (other.name.Contains("Melee") || other.GetComponent<Melee>() != null)
        {
            // Check if this is a player melee attack, not an enemy one
            bool isPlayerMelee = other.transform.IsChildOf(Player.Instance.transform) || 
                                other.GetComponent<Player>() != null ||
                                !other.GetComponent<Enemy>();
            
            if (isPlayerMelee)
            {
                Debug.Log($"Boss hit by player melee weapon: {other.name}");
                TakeDamage((int)Player.Instance.baseMeleeDamage + (int)Player.Instance.damageMeleeModifier);
            }
            else
            {
                Debug.Log($"Ignoring enemy melee attack: {other.name}");
            }
        }
    }
    
    /// <summary>
    /// Get health percentage
    /// </summary>
    public float GetHealthPercentage()
    {
        return (float)currentHealth / maxHealth;
    }
    
    /// <summary>
    /// Get current phase
    /// </summary>
    public BossPhase GetCurrentPhase()
    {
        return currentPhase;
    }
    
    /// <summary>
    /// Handle collision damage (for non-trigger colliders)
    /// </summary>
    void OnCollisionEnter2D(Collision2D collision)
    {
        if (isDead) return;
        
        Collider2D other = collision.collider;
        
        // Handle incoming damage from player projectiles
        if (other.GetComponent<Projectile>() != null)
        {
            // This is a projectile hitting us
            Debug.Log($"Boss collision with projectile: {other.name}, Tag: {other.tag}");
            
            // Only take damage from player projectiles, not enemy projectiles
            bool isEnemyProjectile = other.CompareTag("Enemy") || other.GetComponent<Enemy>() != null;
            
            if (!isEnemyProjectile)
            {
                Debug.Log("Taking collision damage from player projectile");
                TakeDamage((int)Player.Instance.baseRangeDamage + (int)Player.Instance.baseRangeModifier);
                
                // Destroy the projectile
                Destroy(other.gameObject);
            }
            else
            {
                Debug.Log("Ignoring collision damage from enemy projectile");
            }
        }
        
        // Handle incoming damage from melee weapons (only from player)
        if (other.name.Contains("Melee") || other.GetComponent<Melee>() != null)
        {
            // Check if this is a player melee attack, not an enemy one
            bool isPlayerMelee = other.transform.IsChildOf(Player.Instance.transform) || 
                                other.GetComponent<Player>() != null ||
                                !other.GetComponent<Enemy>();
            
            if (isPlayerMelee)
            {
                Debug.Log($"Boss collision with player melee weapon: {other.name}");
                TakeDamage((int)Player.Instance.baseMeleeDamage + (int)Player.Instance.damageMeleeModifier);
            }
            else
            {
                Debug.Log($"Ignoring enemy melee collision: {other.name}");
            }
        }
    }
    
    /// <summary>
    /// Check if boss is dead
    /// </summary>
    public bool IsDead()
    {
        return isDead;
    }
    
    /// <summary>
    /// Generate spawn positions using Room's spawning logic
    /// </summary>
    private List<Vector3> GenerateRoomBasedSpawnPositions(int count)
    {
        List<Vector3> positions = new List<Vector3>();
        
        // Find the room the boss is actually in
        Room currentRoom = FindRoomContainingBoss();
        
        if (currentRoom == null)
        {
            Debug.LogWarning("Boss not in any room, cannot use room-based spawning");
            return positions; // Return empty list to trigger fallback
        }
        
        Debug.Log($"Boss found in room: {currentRoom.name} at {currentRoom.transform.position}");
        
        // Get room properties
        Vector3 roomCenter = currentRoom.transform.position;
        Vector2Int interiorSize = currentRoom.interiorSize;
        
        // Calculate spawn area boundaries (much smaller area around boss)
        float maxSpawnDistance = Mathf.Min(interiorSize.x, interiorSize.y) * 0.2f; // 20% of room size
        maxSpawnDistance = Mathf.Max(maxSpawnDistance, 3f); // Minimum 3 units
        maxSpawnDistance = Mathf.Min(maxSpawnDistance, 6f); // Maximum 6 units
        
        // Player position (for distance checking)
        Vector3 playerPos = GameObject.FindWithTag(opponentTag)?.transform.position ?? roomCenter;
        
        int attempts = 0;
        int maxAttempts = count * 30; // More attempts for tighter constraints
        
        while (positions.Count < count && attempts < maxAttempts)
        {
            attempts++;
            
            // Generate random position in a circle around the boss
            float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
            float distance = Random.Range(2f, maxSpawnDistance);
            
            Vector3 spawnPos = transform.position + new Vector3(
                Mathf.Cos(angle) * distance,
                Mathf.Sin(angle) * distance,
                0
            );
            
            // Ensure spawn position is within the room bounds
            if (IsPositionInRoom(spawnPos, currentRoom) && 
                IsValidMinionSpawnPosition(spawnPos, playerPos, positions))
            {
                positions.Add(spawnPos);
            }
        }
        
        Debug.Log($"Boss generated {positions.Count} room-constrained spawn positions out of {count} requested after {attempts} attempts");
        return positions;
    }
    
    /// <summary>
    /// Find which room contains the boss
    /// </summary>
    private Room FindRoomContainingBoss()
    {
        Room[] allRooms = FindObjectsOfType<Room>();
        Vector3 bossPosition = transform.position;
        
        foreach (Room room in allRooms)
        {
            if (IsPositionInRoom(bossPosition, room))
            {
                return room;
            }
        }
        
        return null;
    }
    
    /// <summary>
    /// Check if a position is within a specific room's boundaries
    /// </summary>
    private bool IsPositionInRoom(Vector3 position, Room room)
    {
        Vector3 roomCenter = room.transform.position;
        Vector2Int interiorSize = room.interiorSize;
        
        Grid grid = FindObjectOfType<Grid>();
        float cellSize = grid != null ? grid.cellSize.x : 0.4f;
        
        // Calculate room boundaries in world coordinates
        float halfWidth = (interiorSize.x * cellSize) / 2f;
        float halfHeight = (interiorSize.y * cellSize) / 2f;
        
        // Add small margin for edge cases
        halfWidth -= cellSize;
        halfHeight -= cellSize;
        
        return (position.x >= roomCenter.x - halfWidth && 
                position.x <= roomCenter.x + halfWidth &&
                position.y >= roomCenter.y - halfHeight && 
                position.y <= roomCenter.y + halfHeight);
    }
    
    /// <summary>
    /// Convert tile coordinates to world position (similar to Room.cs)
    /// </summary>
    private Vector3 GetTileCenterWorldPosition(Vector3 roomCenter, int tileOffsetX, int tileOffsetY)
    {
        Grid grid = FindObjectOfType<Grid>();
        
        if (grid != null)
        {
            float worldOffsetX = tileOffsetX * grid.cellSize.x + grid.cellSize.x * 0.5f;
            float worldOffsetY = tileOffsetY * grid.cellSize.y + grid.cellSize.y * 0.5f;
            
            return new Vector3(
                roomCenter.x + worldOffsetX,
                roomCenter.y + worldOffsetY,
                roomCenter.z
            );
        }
        else
        {
            // Fallback with default cell size
            float cellSize = 0.4f;
            return new Vector3(
                roomCenter.x + (tileOffsetX + 0.5f) * cellSize,
                roomCenter.y + (tileOffsetY + 0.5f) * cellSize,
                roomCenter.z
            );
        }
    }
    
    /// <summary>
    /// Check if position is valid for minion spawning
    /// </summary>
    private bool IsValidMinionSpawnPosition(Vector3 position, Vector3 playerPos, List<Vector3> existingPositions)
    {
        Grid grid = FindObjectOfType<Grid>();
        float cellSize = grid != null ? grid.cellSize.x : 0.4f;
        
        // Check distance from player (at least 2 tiles away)
        float minPlayerDistance = cellSize * 2f;
        if (Vector3.Distance(position, playerPos) < minPlayerDistance)
        {
            return false;
        }
        
        // Check distance from boss (at least 1.5 tiles away)
        float minBossDistance = cellSize * 1.5f;
        if (Vector3.Distance(position, transform.position) < minBossDistance)
        {
            return false;
        }
        
        // Check distance from other spawn positions (at least 1 tile apart)
        float minSpawnDistance = cellSize;
        foreach (Vector3 existingPos in existingPositions)
        {
            if (Vector3.Distance(position, existingPos) < minSpawnDistance)
            {
                return false;
            }
        }
        
        // Check for walls/obstacles using multiple detection methods
        if (IsPositionBlocked(position))
        {
            return false;
        }
        
        return true;
    }
    
    /// <summary>
    /// Check if position is blocked by walls or obstacles
    /// </summary>
    private bool IsPositionBlocked(Vector3 position)
    {
        // Method 1: Check for colliders at position
        Collider2D hit = Physics2D.OverlapCircle(position, 0.15f);
        if (hit != null && !hit.isTrigger)
        {
            // Check if it's a wall, tilemap, or obstacle
            if (hit.name.ToLower().Contains("collision") || 
                hit.name.ToLower().Contains("wall") ||
                hit.GetComponent<UnityEngine.Tilemaps.TilemapCollider2D>() != null)
            {
                return true;
            }
        }
        
        // Method 2: Raycast from above to check for floor
        RaycastHit2D floorCheck = Physics2D.Raycast(position + Vector3.up, Vector3.down, 2f);
        if (floorCheck.collider != null)
        {
            // If we hit something that's not a floor, position might be invalid
            if (floorCheck.collider.name.ToLower().Contains("collision"))
            {
                return true;
            }
        }
        
        // Method 3: Check tilemap directly if available
        Grid grid = FindObjectOfType<Grid>();
        if (grid != null)
        {
            // Find tilemaps
            UnityEngine.Tilemaps.Tilemap[] tilemaps = FindObjectsOfType<UnityEngine.Tilemaps.Tilemap>();
            foreach (var tilemap in tilemaps)
            {
                if (tilemap.name.ToLower().Contains("collision") || tilemap.name.ToLower().Contains("wall"))
                {
                    Vector3Int cellPosition = tilemap.WorldToCell(position);
                    if (tilemap.HasTile(cellPosition))
                    {
                        return true; // Position has a wall/collision tile
                    }
                }
            }
        }
        
        return false; // Position is clear
    }
    
    /// <summary>
    /// Fallback minion spawning if room system not available
    /// </summary>
    private IEnumerator FallbackMinionSpawn()
    {
        Debug.Log("Using fallback circular spawning pattern");
        
        // Get current phase minion prefabs
        GameObject[] currentPhasePrefabs = GetCurrentPhaseMinionPrefabs();
        if (currentPhasePrefabs == null || currentPhasePrefabs.Length == 0) 
        {
            Debug.LogWarning($"No minion prefabs available for {currentPhase} in fallback spawn!");
            yield break;
        }
        
        // Spawn minions in a circle around the boss (original method)
        for (int i = 0; i < minionsPerSpawn; i++)
        {
            float angle = (360f / minionsPerSpawn) * i * Mathf.Deg2Rad;
            Vector3 spawnPosition = transform.position + new Vector3(
                Mathf.Cos(angle) * minionSpawnRadius,
                Mathf.Sin(angle) * minionSpawnRadius,
                0
            );
            
            // Basic collision check for fallback
            Collider2D obstacle = Physics2D.OverlapCircle(spawnPosition, 0.2f);
            if (obstacle != null && !obstacle.isTrigger)
            {
                // Try alternative position slightly further out
                spawnPosition = transform.position + new Vector3(
                    Mathf.Cos(angle) * (minionSpawnRadius + 1f),
                    Mathf.Sin(angle) * (minionSpawnRadius + 1f),
                    0
                );
            }
            
            // Select random minion prefab from current phase
            GameObject selectedPrefab = currentPhasePrefabs[Random.Range(0, currentPhasePrefabs.Length)];
            GameObject minion = Instantiate(selectedPrefab, spawnPosition, Quaternion.identity);
            aliveMinions.Add(minion);
            
            // Set up minion death tracking
            Enemy minionEnemy = minion.GetComponent<Enemy>();
            if (minionEnemy != null)
            {
                minionEnemy.OnDeath += OnMinionDeath;
            }
            
            yield return new WaitForSeconds(0.1f);
        }
    }
}

/// <summary>
/// Independent homing projectile behavior
/// </summary>
public class HomingProjectileBehavior : MonoBehaviour
{
    private Transform target;
    private float speed;
    private Rigidbody2D rb;
    private float homingStrength = 2f;
    private float lifeTime = 5f;
    
    public void Initialize(Transform targetTransform, float projectileSpeed)
    {
        target = targetTransform;
        speed = projectileSpeed;
        rb = GetComponent<Rigidbody2D>();
        
        // Auto-destroy after lifetime
        Destroy(gameObject, lifeTime);
    }
    
    void FixedUpdate()
    {
        if (target == null || rb == null) return;
        
        // Calculate homing direction
        Vector2 directionToTarget = (target.position - transform.position).normalized;
        
        // Apply homing
        Vector2 currentVelocity = rb.linearVelocity;
        Vector2 desiredVelocity = directionToTarget * speed;
        Vector2 steering = Vector2.Lerp(currentVelocity, desiredVelocity, homingStrength * Time.fixedDeltaTime);
        
        rb.linearVelocity = steering;
        
        // Rotate to face movement
        if (steering.magnitude > 0.1f)
        {
            float angle = Mathf.Atan2(steering.y, steering.x) * Mathf.Rad2Deg - 90f;
            transform.rotation = Quaternion.Euler(0, 0, angle);
        }
    }
}

/// <summary>
/// Simple projectile destroyer for projectiles without Projectile component
/// </summary>
public class SimpleProjectileDestroy : MonoBehaviour
{
    public float lifeTime = 5f;
    
    void Start()
    {
        Destroy(gameObject, lifeTime);
    }
    

    
    void OnCollisionEnter2D(Collision2D collision)
    {
        // Backup collision detection in case trigger doesn't work
        Collider2D other = collision.collider;
        Debug.Log($"SimpleProjectileDestroy physics collision with: {other.name}, Tag: {other.tag}");
        
        bool isPlayer = other.CompareTag("Player") || other.GetComponent<Player>() != null;
        
        if (isPlayer)
        {
            Player player = other.GetComponent<Player>() ?? Player.Instance;
            if (player != null)
            {
                Debug.Log($"Circle projectile dealing 1 damage to player via collision!");
                player.takeDamage(1);
            }
            
            Destroy(gameObject);
        }
    }
}

/// <summary>
/// Enforces constant movement for circle projectiles
/// </summary>
public class CircleProjectileMovement : MonoBehaviour
{
    public Vector2 velocity;
    private Rigidbody2D rb;
    
    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.linearVelocity = velocity;
            Debug.Log($"CircleProjectileMovement started with velocity: {velocity}");
        }
    }
    
    void FixedUpdate()
    {
        if (rb != null)
        {
            // Maintain constant velocity
            rb.linearVelocity = velocity;
        }
    }
}