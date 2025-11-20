using UnityEngine;
using System.Collections;

public class Player : MonoBehaviour {

    private float defaultHealth;
    public static Player Instance;
    public Animator animator;
    public bool invincibility;
    private bool canAttack;
    public GameObject meleeHitbox;
    protected Rigidbody2D rigidBody;
    protected SpriteRenderer spriteRenderer;
    public int health;
    public int maxHealth;
    public float moveSpeed, attackRange, damage;
    [SerializeField]protected float friction;
    protected Vector2 moveDirection;
    public float invinceTimer;

    void Start() {
        spriteRenderer = GetComponent<SpriteRenderer>();
        rigidBody = GetComponent<Rigidbody2D>();
        defaultHealth=health;
        invincibility = false;
        canAttack = true;
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
        if(Input.GetMouseButton(0) && canAttack) {
            StartCoroutine(MeleeAttack());
        }
    }

    protected IEnumerator MeleeAttack() {
        canAttack = false;
        Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Vector3 direction = (mousePos - transform.position).normalized;
        float hitboxDirection = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        Vector3 offset = new Vector3(Mathf.Cos(hitboxDirection * Mathf.Deg2Rad), Mathf.Sin(hitboxDirection * Mathf.Deg2Rad) + 1f,  0) * 0.2f;
        Vector3 spawnPos = transform.position + offset;
        GameObject hitbox = Instantiate(meleeHitbox, spawnPos, Quaternion.Euler(0, 0, hitboxDirection - 90f));
        hitbox.transform.SetParent(transform);
        yield return new WaitForSeconds(0.2f);
        Destroy(hitbox);
        yield return new WaitForSeconds(0.3f); 
        canAttack = true;
    }

    public void takeDamage(){
        if (!invincibility) {
            health = health - 1;
            if(health<=0) {
                Time.timeScale = 0;
            }
            StartCoroutine(iFrames(invinceTimer));
        }
    }

    protected IEnumerator iFrames(float time) {
        invincibility = true;
        Debug.Log("Invince True");
        yield return new WaitForSeconds(time);
        Debug.Log("Invince False");
        invincibility = false;
    }

}
