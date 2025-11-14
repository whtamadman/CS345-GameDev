using UnityEngine;

public class Player : Person
{
    private float defaultHealth;
    public static Player Instance;
    public Animator animator;
    public bool invincibility;

    void Awake(){
        if(Instance == null){
            Instance = this;
        }
    }

    protected override void CustomStart(){
        defaultHealth=health;
        invincibility = false;
    }

    protected override void Move(){
        if(moveDirection.magnitude > 0){
            rigidBody.linearVelocity = moveDirection * moveSpeed;
        }else{
            rigidBody.linearVelocity -= rigidBody.linearVelocity * friction;
        }
    }

    void Update(){
        moveDirection = new Vector2(Input.GetAxis("Horizontal"),Input.GetAxis("Vertical")).normalized;
    }
<<<<<<< HEAD

    public void OnTriggerEnter(Collider other) {
        if(other.gameObject.tag=="GlassBlade") {
            
        } else if (other.gameObject.tag=="DamageUp") {

        } else if (other.gameObject.tag=="AttackRangeUp") {

        } else if ((other.gameObject.tag=="CritChance")) {

        }
    }
=======
>>>>>>> 8f207064568829aa8caf37bf347eea8d1a81fe65
}
