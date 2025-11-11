using System.Collections;
using UnityEngine;

public class Projectile : MonoBehaviour
{
    [SerializeField] private float lifeTime = 2f;
    [SerializeField] private float speed = 10f;

    private Transform target;
    private Rigidbody2D rb;
    private GameObject shooter;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        StartCoroutine(LifeCountdown());
    }

    public void SetTarget(GameObject targetObject, GameObject shooterObject = null)
    {
        target = targetObject.transform;
        shooter = shooterObject; // store shooter reference if provided

        Vector2 direction = (target.position - transform.position).normalized;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f;
        transform.rotation = Quaternion.Euler(0, 0, angle);
    }

    IEnumerator LifeCountdown()
    {
        yield return new WaitForSeconds(lifeTime);
        Destroy(gameObject);
    }

    void FixedUpdate()
    {
        if (rb != null)
        {
            rb.linearVelocity = transform.up * speed;
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (shooter != null && other.gameObject == shooter)
            return;

        if (other.tag == target.tag)
            other.GetComponent<Player>().takeDamage();
        Destroy(gameObject);
    }
}