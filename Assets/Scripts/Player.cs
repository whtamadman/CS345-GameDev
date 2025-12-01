using UnityEngine;
using System.Collections;

public class Player : MonoBehaviour {

    public static Player Instance;
    private Animator animator;
    private bool canAttack, invincibility;
    public GameObject meleeHitbox;
    protected Rigidbody2D rigidBody;
    protected SpriteRenderer spriteRenderer;
    public int health, maxHealth;
    public float moveSpeed, attackRange, damage, hitboxFrames, meleeCooldown, invinceTimer, hitboxRange;
    [SerializeField]protected float friction;
    protected Vector2 moveDirection;


    void Start() {
        spriteRenderer = GetComponent<SpriteRenderer>();
        rigidBody = GetComponent<Rigidbody2D>();
        maxHealth=health;
        invincibility = false;
        canAttack = true;
        Health.Instance.InitHealthSprites();
        animator = GetComponent<Animator>();
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
        Vector3 offset = new Vector3(Mathf.Cos(hitboxDirection * Mathf.Deg2Rad), Mathf.Sin(hitboxDirection * Mathf.Deg2Rad) + 1f,  0) * 0.2f;
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
    }

    protected IEnumerator iFrames(float time) {
        invincibility = true;
        Debug.Log("Invince True");
        yield return new WaitForSeconds(time);
        Debug.Log("Invince False");
        invincibility = false;
    }
}
