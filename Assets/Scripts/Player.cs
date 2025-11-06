using UnityEngine;

public class Player : Person
{
    private float defaultHealth;
    public static Player Instance;
    public Animator animator;
    public AudioClip gun;
    private AudioSource effectsAudioSource;

    void Awake(){
        if(Instance == null){
            Instance = this;}
    }

    protected override void CustomStart(){
        defaultHealth=health;
        effectsAudioSource = gameObject.AddComponent<AudioSource>();
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
    
}
