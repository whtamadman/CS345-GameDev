using UnityEngine;
using System.Collections;

public abstract class Person : MonoBehaviour
{
    protected Rigidbody2D rigidBody;
    protected SpriteRenderer spriteRenderer;
    public int health;
    public int maxHealth;
    public float moveSpeed, attackRange;
    [SerializeField]protected float friction;
    protected Vector2 moveDirection;
    public string opponentTag;

    void Start() {
        spriteRenderer = GetComponent<SpriteRenderer>();
        rigidBody = GetComponent<Rigidbody2D>();
        CustomStart();
    }

    public void takeDamage(){
        health = health - 1;
        if (gameObject.name=="Player") {
            if(health<=0) {
                Time.timeScale = 0;
            }
        }
        // if(health<=0 && gameObject.name!="Player"){
        //     Destroy(gameObject);
        //     OnShipDestroyed?.Invoke(this);
        // }
    }
    abstract protected void CustomStart();
    abstract protected void Move();
    void FixedUpdate(){Move();}

}
