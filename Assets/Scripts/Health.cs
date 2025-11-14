using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class Health : MonoBehaviour {

    public static Health Instance;
    public float width, height, health, maxHealth;
    [SerializeField] private RectTransform healthBar;

    void Start() {
        deathScreen.SetActive(false);
        effectsAudioSource = gameObject.AddComponent<AudioSource>();
    }

    void Awake() {
        Instance = this;
    }
    
    public void setHealth() {
        health = (float)PlayerShip.Instance.health;
        if (health < 0.01) {
            deathScreen.SetActive(true);
            effectsAudioSource.PlayOneShot(death);
        }
        maxHealth = (float)PlayerShip.Instance.maxHealth;
        float newWidth = (health/maxHealth) * width;
        Debug.Log(newWidth);
        healthBar.sizeDelta = new Vector2(newWidth, height);
    }
}