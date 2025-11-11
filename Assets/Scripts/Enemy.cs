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
    protected Vector2 moveDirection;
    protected bool canShoot = true;
    Vector3 targetPosition;
    OutOfBounds outOfBounds;
    public float rangedDistance;

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
        if(moveDirection.magnitude > 0) {
            rigidBody.linearVelocity = moveDirection * moveSpeed;
        }
        else
        {
            rigidBody.linearVelocity -= rigidBody.linearVelocity * friction;
        }
        transform.up += ((Vector3)moveDirection-transform.up)/5;
    }
    void Update()
    {
        var player = GameObject.FindWithTag("Player");
        if (type == EnemyType.Ranged && outOfBounds.getDistance(transform.position, player.transform.position) <= rangedDistance)
        {
            rigidBody.linearVelocity = moveDirection * 0;
            moveDirection = Vector2.zero;
            if(canShoot){
                StartCoroutine(Shoot(moveDirection,shootForce));
            }
            return;
        }
        if (currentState == State.Roam){
            if(outOfBounds.getDistance(transform.position,targetPosition)<1f){
                targetPosition = (Vector2)transform.position + new
                    Vector2(Random.Range(-roamDist,roamDist),Random.Range(-roamDist,roamDist));
            }
            if(outOfBounds.getDistance(transform.position,player.transform.position)<chaseDist){
                currentState = State.Chase;
            }
        }else if(currentState == State.Chase){
            targetPosition = player.transform.position;
            if(outOfBounds.getDistance(transform.position,player.transform.position)<shootDist){
                currentState = State.Shoot;
            }else if(outOfBounds.getDistance(transform.position,player.transform.position)>chaseDist*1.2f){
                currentState = State.Roam;
            }
        }else{
            targetPosition = player.transform.position;
            if(outOfBounds.getDistance(transform.position,player.transform.position)>shootDist){
                currentState = State.Chase;
            }
            if(canShoot){
                StartCoroutine(Shoot(moveDirection,shootForce));
            }
        }
        targetPosition = outOfBounds.getCoords(targetPosition);
        moveDirection = -outOfBounds.getDirection(transform.position,targetPosition).normalized;
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

    private void OnDestroy()
    {
    }
}