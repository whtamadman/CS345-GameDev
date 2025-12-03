using UnityEngine;
using System.Collections;

public class Player : MonoBehaviour {

    public static Player Instance;
    private Animator animator;
    private bool canAttack, invincibility, inEnemy;
    public GameObject meleeHitbox;
    protected Rigidbody2D rigidBody;
    protected SpriteRenderer spriteRenderer;
    public int health, maxHealth, maxMaxHealth;
    public int coins = 0; // Player's gold/currency
    [SerializeField]protected float friction;
    protected Vector2 moveDirection;
    public float moveSpeed, damage, hitboxFrames, meleeCooldown, invinceTimer, hitboxRange;
    
    [Header("Visual Effects")]
    [SerializeField] private Color invisibilityColor = Color.blue;
    [SerializeField] private Color normalColor = Color.white;
    private Coroutine invisibilityVisualCoroutine;
    
    [Header("Collision Settings")]
    [SerializeField] private bool ignoreCollisionsDuringInvis = true;
    private int originalLayer;
    private Collider2D[] playerColliders;

    void Start() {
        spriteRenderer = GetComponent<SpriteRenderer>();
        rigidBody = GetComponent<Rigidbody2D>();
        maxMaxHealth = 10;
        maxHealth = 5;
        health = maxHealth;
        invincibility = false;
        canAttack = true;
        Health.Instance.InitHealthSprites();
        animator = GetComponent<Animator>();
        inEnemy = false;
        
        // Store original layer and get all colliders
        originalLayer = gameObject.layer;
        playerColliders = GetComponentsInChildren<Collider2D>();
    }
    void Awake(){
        if(Instance == null){
            Instance = this;
        }
    }

    void Update(){
        moveDirection = new Vector2(Input.GetAxis("Horizontal"),Input.GetAxis("Vertical")).normalized;
        if(moveDirection.magnitude > 0){
            rigidBody.linearVelocity = moveDirection * moveSpeed;

        }else{
            rigidBody.linearVelocity -= rigidBody.linearVelocity * friction;
        }
        animator.SetFloat("X", moveDirection.x);
        animator.SetFloat("Y", moveDirection.y);
        if (moveDirection != Vector2.zero) {
            animator.SetFloat("LastX", moveDirection.x);
            animator.SetFloat("LastY", moveDirection.y);
        }
        Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Vector2 aimDirection = (mouseWorldPos - transform.position).normalized;
        if(Input.GetMouseButton(0) && canAttack) {
            animator.SetTrigger("Attack");
            StartCoroutine(MeleeAttack());
        }
    }

    protected IEnumerator MeleeAttack() {
        canAttack = false;
        Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Vector3 direction = (mousePos - transform.position).normalized;
        float hitboxDirection = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        Vector3 offset = new Vector3(Mathf.Cos(hitboxDirection * Mathf.Deg2Rad), Mathf.Sin(hitboxDirection * Mathf.Deg2Rad),  0) * 0.2f;
        Vector3 spawnPos = transform.position + offset;
        GameObject hitbox = Instantiate(meleeHitbox, spawnPos, Quaternion.Euler(0, 0, hitboxDirection - 90f));
        hitbox.transform.localScale *= hitboxRange;
        hitbox.transform.SetParent(transform);
        animator.SetFloat("MouseX", direction.x);
        animator.SetFloat("MouseY", direction.y);
        Debug.Log(direction);
        animator.SetTrigger("Attack");
        //How long the attack stays out for
        yield return new WaitForSeconds(hitboxFrames);
        Destroy(hitbox);
       // animator.SetBool("attacking" = false);
        //Melee Cooldown
        yield return new WaitForSeconds(meleeCooldown); 
        canAttack = true;
    }

    public void takeDamage(int damage = 1){
        if (!invincibility) {
            health = health - damage;
            if(health<=0) {
                Time.timeScale = 0;
            }
            Health.Instance.UpdateHealthSprites();
            StartCoroutine(iFrames(invinceTimer));
        }
        else
        {
            Debug.Log($"Player damage blocked by invincibility at position {transform.position}");
        }
    }

    protected IEnumerator iFrames(float time) {
        invincibility = true;
        Debug.Log("Invince True");
        
        // Disable collisions with enemies and projectiles during invisibility
        if (ignoreCollisionsDuringInvis)
        {
            SetPlayerPhysicsIgnore(true);
        }
        
        // Start invisibility visual effect
        if (invisibilityVisualCoroutine != null)
        {
            StopCoroutine(invisibilityVisualCoroutine);
        }
        invisibilityVisualCoroutine = StartCoroutine(InvisibilityVisualEffect(time));
        
        yield return new WaitForSeconds(time);
        
        // Re-enable collisions
        if (ignoreCollisionsDuringInvis)
        {
            SetPlayerPhysicsIgnore(false);
        }
        
        Debug.Log("Invince False");
        invincibility = false;
    }

    private void OnTriggerEnter2D(Collider2D other) {
        if (other.CompareTag("Enemy") && !inEnemy) {
            takeDamage();
            inEnemy = true;
        }
    }

        private void OnTriggerExit2D(Collider2D other) {
        if (other.CompareTag("Enemy") && inEnemy) {
            inEnemy = false;
        }
    }
    
    /// <summary>
    /// Handle visual effect for invisibility - blue tint that fades back to white
    /// </summary>
    protected IEnumerator InvisibilityVisualEffect(float duration)
    {
        if (spriteRenderer == null) yield break;
        
        // Immediately set to blue when invisibility starts
        spriteRenderer.color = invisibilityColor;
        
        float elapsedTime = 0f;
        
        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            
            // Calculate fade progress (0 = blue, 1 = white)
            float fadeProgress = elapsedTime / duration;
            
            // Lerp from invisibility color back to normal color
            Color currentColor = Color.Lerp(invisibilityColor, normalColor, fadeProgress);
            spriteRenderer.color = currentColor;
            
            yield return null;
        }
        
        // Ensure we end with normal color
        spriteRenderer.color = normalColor;
        invisibilityVisualCoroutine = null;
    }
    
    /// <summary>
    /// Enable or disable physics collisions with enemies and projectiles
    /// </summary>
    private void SetPlayerPhysicsIgnore(bool ignore)
    {
        if (playerColliders == null) return;
        
        // Find all enemies and their colliders
        Enemy[] enemies = FindObjectsByType<Enemy>(FindObjectsSortMode.None);
        Projectile[] projectiles = FindObjectsByType<Projectile>(FindObjectsSortMode.None);
        
        foreach (Collider2D playerCol in playerColliders)
        {
            if (playerCol == null) continue;
            
            // Ignore collision with all enemy colliders
            foreach (Enemy enemy in enemies)
            {
                if (enemy != null)
                {
                    Collider2D[] enemyColliders = enemy.GetComponentsInChildren<Collider2D>();
                    foreach (Collider2D enemyCol in enemyColliders)
                    {
                        if (enemyCol != null)
                        {
                            Physics2D.IgnoreCollision(playerCol, enemyCol, ignore);
                        }
                    }
                }
            }
            
            Debug.Log($"Player: {(ignore ? "Disabled" : "Enabled")} collisions with {enemies.Length} enemies");
            
            // Ignore collision with all projectile colliders
            foreach (Projectile projectile in projectiles)
            {
                if (projectile != null)
                {
                    Collider2D projectileCol = projectile.GetComponent<Collider2D>();
                    if (projectileCol != null)
                    {
                        Physics2D.IgnoreCollision(playerCol, projectileCol, ignore);
                    }
                }
            }
        }
        
        Debug.Log($"Player collision ignoring set to: {ignore}");
    }
    
    /// <summary>
    /// Give gold/coins to the player
    /// </summary>
    /// <param name="amount">Amount of coins to give</param>
    public void GiveGold(int amount)
    {
        if (amount > 0)
        {
            coins += amount;
            Debug.Log($"Player received {amount} coins! Total: {coins}");
        }
    }
    
    /// <summary>
    /// Spend gold/coins if player has enough
    /// </summary>
    /// <param name="amount">Amount of coins to spend</param>
    /// <returns>True if transaction successful, false if not enough coins</returns>
    public bool SpendGold(int amount)
    {
        if (amount > 0 && coins >= amount)
        {
            coins -= amount;
            Debug.Log($"Player spent {amount} coins! Remaining: {coins}");
            return true;
        }
        else
        {
            Debug.Log($"Not enough coins! Need {amount}, have {coins}");
            return false;
        }
    }
    
    /// <summary>
    /// Get current coin count
    /// </summary>
    /// <returns>Current number of coins</returns>
    public int GetCoins()
    {
        return coins;
    }
    
    /// <summary>
    /// Check if player has enough coins for a purchase
    /// </summary>
    /// <param name="cost">Cost to check</param>
    /// <returns>True if player can afford it</returns>
    public bool CanAfford(int cost)
    {
        return coins >= cost;
    }
}
