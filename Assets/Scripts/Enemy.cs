using UnityEngine;
using Random = UnityEngine.Random;
using System.Collections;

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
    protected Vector2 moveDirection;
    protected bool canShoot = true;
    Vector3 targetPosition;
    OutOfBounds outOfBounds;
    public float rangedDistance;
    
    // Event for room system integration
    public System.Action<Enemy> OnDeath;

    private enum State
    {
        Roam,
        Chase,
        Shoot
    };
    
    public enum EnemyType
    {
        Melee,
        Ranged
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
            if (rigidBody.isKinematic)
            {
                rigidBody.isKinematic = false; // Enemies need dynamic physics to move
            }
        }
        
        currentState = State.Roam;
        targetPosition = (Vector2)transform.position
                         + new Vector2(Random.Range(-roamDist, roamDist), Random.Range(-roamDist, roamDist));
    }

    void FixedUpdate()
    {
        Move();
    }
    
    void Move()
    {
        if (rigidBody == null) return;
        
        if(moveDirection.magnitude > 0.01f) {
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
        
        if (type == EnemyType.Ranged && distanceToPlayer <= rangedDistance)
        {
            rigidBody.linearVelocity = Vector2.zero;
            moveDirection = Vector2.zero;
            if(CanAttackNow()){
                StartCoroutine(Shoot(moveDirection,shootForce));
            }
            return;
        }
        
        if (currentState == State.Roam){
            float distanceToTarget = Vector3.Distance(transform.position, targetPosition);
                
            if(distanceToTarget < 1f){
                targetPosition = (Vector2)transform.position + new
                    Vector2(Random.Range(-roamDist,roamDist),Random.Range(-roamDist,roamDist));
            }
            if(distanceToPlayer < chaseDist){
                currentState = State.Chase;
            }
        }else if(currentState == State.Chase){
            targetPosition = player.transform.position;
            if(distanceToPlayer < shootDist){
                currentState = State.Shoot;
            }else if(distanceToPlayer > chaseDist*1.2f){
                currentState = State.Roam;
            }
        }else{
            targetPosition = player.transform.position;
            if(distanceToPlayer > shootDist){
                currentState = State.Chase;
            }
            if(CanAttackNow()){
                StartCoroutine(Shoot(moveDirection,shootForce));
            }
        }
        
        // Calculate direction directly (room-based, no wrap-around)
        Vector3 direction = targetPosition - transform.position;
        if (direction.magnitude > 0.1f)
        {
            moveDirection = direction.normalized;
        }
        else
        {
            moveDirection = Vector2.zero;
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
        int rand = Random.Range(0, projectiles.Length);
        Projectile newBullet = Instantiate(projectiles[rand], transform.position, Quaternion.identity);
        newBullet.SetTarget(GameObject.FindWithTag(opponentTag), this.gameObject);
        canShoot = false;
        yield return new WaitForSeconds(reloadTime);
        canShoot = true;
    }

    public void TakeDamage(int damage)
    {
        health -= damage;
        if (health <= 0)
        {
            Die();
        }
    }
    
    protected virtual void Die()
    {
        // Notify listeners that this enemy has died
        OnDeath?.Invoke(this);
        
        // Destroy the enemy
        Destroy(gameObject);
    }
    
    private void OnDestroy()
    {
        // Clean up event subscriptions
        OnDeath = null;
    }
}