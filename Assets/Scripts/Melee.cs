using UnityEngine;
using Random = UnityEngine.Random;
using System.Collections;

public class Melee : MonoBehaviour {

    private void OnTriggerEnter2D(Collider2D other) {
        if (other.CompareTag("Enemy")) {
            other.GetComponent<Enemy>().TakeDamage((int)Player.Instance.baseMeleeDamage + (int)Player.Instance.damageMeleeModifier);
        }
    }
}
