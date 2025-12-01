using UnityEngine;
using Random = UnityEngine.Random;
using System.Collections;

public class Melee : MonoBehaviour {

    private void OnTriggerEnter2D(Collider2D other) {
        if (other.CompareTag("Enemy")) {
            Debug.Log((int)Player.Instance.damage);
            other.GetComponent<Enemy>().TakeDamage((int)Player.Instance.damage);
        }
    }
}
