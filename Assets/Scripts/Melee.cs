using UnityEngine;
using Random = UnityEngine.Random;
using System.Collections;

public class Melee : MonoBehaviour {

    private bool hit;

    void Start() {
        hit = false;
    }

    private void OnTriggerEnter2D(Collider2D other) {
        if (other.CompareTag("Enemy") && other.TryGetComponent<Projectile>(out _)) {
            Destroy(other.gameObject);
        } else if (other.CompareTag("Enemy") && !other.TryGetComponent<Projectile>(out _)) {
            if (!hit) {
                other.GetComponent<Enemy>().TakeDamage((int)Player.Instance.baseMeleeDamage + (int)Player.Instance.damageMeleeModifier);
                hit = true;
            }
        }
    }
}
