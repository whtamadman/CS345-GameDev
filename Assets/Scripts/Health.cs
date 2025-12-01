using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class Health : MonoBehaviour {

    public Sprite heart;
    private int health;
    public static Health Instance;
    public GameObject HUD;

    void Awake(){
        if(Instance == null){
            Instance = this;
        }
    }
    
    public void InitHealthSprites() {
        health = Player.Instance.maxHealth;
        for (int i = 1; i <= health && i <= 10; i++) {
            GameObject heartObject = new GameObject("heart" + i);
            heartObject.transform.SetParent(HUD.transform, false);
            Image heartSprite = heartObject.AddComponent<Image>();
            heartSprite.sprite = heart;
            RectTransform rt = heartObject.GetComponent<RectTransform>();
            rt.anchoredPosition = new Vector2(-425 + i * 28, 232);
            rt.localScale = new Vector3(0.26f, 0.26f, 0.26f);
        }
    }

    public void UpdateHealthSprites() {
        health = Player.Instance.health;
        for (int i = 1; i <= 10; i++) {
            if ((!GameObject.Find("heart"+i)) && (health >= i)) {
                GameObject heartObject = new GameObject("heart" + i);
                heartObject.transform.SetParent(HUD.transform, false);
                Image heartSprite = heartObject.AddComponent<Image>();
                heartSprite.sprite = heart;
                RectTransform rt = heartObject.GetComponent<RectTransform>();
                rt.anchoredPosition = new Vector2(-425 + i * 35, 230);
                rt.localScale = new Vector3(0.35f, 0.35f, 0.35f);
            } else if ((GameObject.Find("heart"+i)) && (health < i)) {
                Debug.Log("Trying to destroy heart " + i);
                GameObject destroyHeart = GameObject.Find("heart"+i);
                GameObject.Destroy(destroyHeart);
            }
        }
    }
}