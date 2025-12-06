using UnityEngine;
using Random = UnityEngine.Random;
using System.Collections;

public class Melee : MonoBehaviour {

    private void OnTriggerEnter2D(Collider2D other) {
        Debug.Log("I Am Here " + other.CompareTag("Enemy") + " " + other.TryGetComponent<Projectile>(out _));
        if (other.CompareTag("Enemy") && other.TryGetComponent<Projectile>(out _)) {
            Debug.Log("Destroyed Projectile Hopefully");
            Destroy(other.gameObject);
        } else if (other.CompareTag("Enemy") && !other.TryGetComponent<Projectile>(out _)) {
            Debug.Log("Hit Enemy");
            other.GetComponent<Enemy>().TakeDamage((int)Player.Instance.baseMeleeDamage + (int)Player.Instance.damageMeleeModifier);
        }
    }
}
