using UnityEngine;
using Random = UnityEngine.Random;
using System.Collections;

public class Melee : MonoBehaviour {

    private void OnTriggerEnter2D(Collider2D other) {
        if (other.tag == "CS" || other.tag == "Math" || other.tag == "Nurse") {
            other.GetComponent<Enemy>().TakeDamage((int)Player.Instance.damage);
        }
    }
}
