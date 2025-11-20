using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Projectile : MonoBehaviour
{
    [SerializeField] private float lifeTime = 2f;
    [SerializeField] private float speed = 10f;
    [SerializeField] private bool isBoomerang = false;
    [SerializeField] private float boomerangReturnDelay = 0.5f; // Time before returning
    [SerializeField] private float boomerangReturnSpeed = 12f; // Speed when returning
    [SerializeField] private bool isTrackingProjectile = false;
    [SerializeField] private float trackingSpeed = 4f;
    [SerializeField] private float trackingTurnSpeed = 720f;
    [SerializeField] private bool isBeamProjectile = false;
    [SerializeField] private LayerMask beamStopLayers;
    [SerializeField] private bool isStaticProjectile = false;
    [SerializeField] private float staticTravelSpeed = 6f;
    [SerializeField] private float staticArrivalThreshold = 0.2f;
    [SerializeField] private float staticHoldDuration = 2f;

    private Transform target;
    private Rigidbody2D rb;
    private GameObject shooter;
    private bool isReturning = false;
    private Vector3 staticDestination;
    private bool staticDestinationLocked = false;
    private bool staticHasSettled = false;
    private Coroutine staticHoldCoroutine;
    
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
        
        if (isStaticProjectile && target != null)
        {
            staticDestination = target.position;
            staticDestinationLocked = true;
        }
        
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
        if (rb == null)
        {
            return;
        }

        if (isReturning && shooter != null)
        {
            // Return to shooter
            Vector2 direction = (shooter.transform.position - transform.position).normalized;
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f;
            transform.rotation = Quaternion.Euler(0, 0, angle);
            rb.linearVelocity = direction * boomerangReturnSpeed;

            // Check if reached shooter
            if (Vector2.Distance(transform.position, shooter.transform.position) < 0.5f)
            {
                Destroy(gameObject);
            }
            return;
        }

        if (isTrackingProjectile)
        {
            HandleTrackingMovement();
            return;
        }

        if (isStaticProjectile)
        {
            HandleStaticMovement();
            return;
        }

        rb.linearVelocity = transform.up * speed;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
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

        if (isBeamProjectile)
        {
            bool shouldStopBeam = ((1 << other.gameObject.layer) & beamStopLayers) != 0;
            if (shouldStopBeam)
            {
                Destroy(gameObject);
                return;
            }
        }

        // If boomerang mode, don't destroy on hit - let it return
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
        // Don't destroy on collision with walls or other objects that aren't the target
    }
    
    private void HandleTrackingMovement()
    {
        if (rb == null)
        {
            return;
        }

        if (target == null)
        {
            rb.linearVelocity = transform.up * trackingSpeed;
            return;
        }

        Vector2 desiredDirection = ((Vector2)target.position - rb.position).normalized;
        float angle = Mathf.Atan2(desiredDirection.y, desiredDirection.x) * Mathf.Rad2Deg - 90f;
        Quaternion desiredRotation = Quaternion.Euler(0f, 0f, angle);
        transform.rotation = Quaternion.RotateTowards(transform.rotation, desiredRotation, trackingTurnSpeed * Time.fixedDeltaTime);
        rb.linearVelocity = transform.up * trackingSpeed;
    }

    private void HandleStaticMovement()
    {
        if (rb == null)
        {
            return;
        }

        if (!staticDestinationLocked)
        {
            staticDestination = target != null ? target.position : transform.position;
            staticDestinationLocked = true;
        }

        if (staticHasSettled)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        Vector2 toDestination = (Vector2)staticDestination - rb.position;
        if (toDestination.magnitude <= staticArrivalThreshold)
        {
            staticHasSettled = true;
            rb.linearVelocity = Vector2.zero;
            if (staticHoldCoroutine == null)
            {
                staticHoldCoroutine = StartCoroutine(StaticHoldTimer());
            }
            return;
        }

        Vector2 travelDirection = toDestination.normalized;
        rb.linearVelocity = travelDirection * staticTravelSpeed;
        float angle = Mathf.Atan2(travelDirection.y, travelDirection.x) * Mathf.Rad2Deg - 90f;
        transform.rotation = Quaternion.Euler(0, 0, angle);
    }

    private IEnumerator StaticHoldTimer()
    {
        yield return new WaitForSeconds(staticHoldDuration);
        Destroy(gameObject);
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