using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class Health : MonoBehaviour {

    public Sprite heart;
    private int health, maxHealth;
    public static Health Instance;
    public GameObject HUD;

    void Awake(){
        if(Instance == null){
            Instance = this;
        }
    }
    
    public void InitHealthSprites() {
        maxHealth = Player.Instance.maxHealth;
        for (int i = 1; i <= maxHealth && i <= 10; i++) {
            GameObject heartObject = new GameObject("blackheart" + i);
            heartObject.transform.SetParent(HUD.transform, false);
            Image heartSprite = heartObject.AddComponent<Image>();
            heartSprite.sprite = heart;
            heartSprite.color = Color.black;
            RectTransform rt = heartObject.GetComponent<RectTransform>();
            rt.anchoredPosition = new Vector2(-412f + i * 28, 214.6f);
            rt.localScale = new Vector3(0.26f, 0.26f, 0.26f);
        }
        health = Player.Instance.health;
        for (int i = 1; i <= health && i <= 10; i++) {
            GameObject heartObject = new GameObject("heart" + i);
            heartObject.transform.SetParent(HUD.transform, false);
            Image heartSprite = heartObject.AddComponent<Image>();
            heartSprite.sprite = heart;
            RectTransform rt = heartObject.GetComponent<RectTransform>();
            rt.anchoredPosition = new Vector2(-412f + i * 28, 214.6f);
            rt.localScale = new Vector3(0.26f, 0.26f, 0.26f);
        }
    }

    public void UpdateHealthSprites() {
        health = Player.Instance.health;
        maxHealth = Player.Instance.maxHealth;
        for (int i = 1; i <= 10; i++) {
            if ((!GameObject.Find("blackheart"+i)) && (maxHealth >= i)) {
                GameObject heartObject = new GameObject("blackheart" + i);
                heartObject.transform.SetParent(HUD.transform, false);
                Image heartSprite = heartObject.AddComponent<Image>();
                heartSprite.sprite = heart;
                heartSprite.color = Color.black;
                RectTransform rt = heartObject.GetComponent<RectTransform>();
                rt.anchoredPosition = new Vector2(-412f  + i * 28, 214.6f);
                rt.localScale = new Vector3(0.26f, 0.26f, 0.26f);
            }
        }
        for (int i = 1; i <= 10; i++) {
            if ((!GameObject.Find("heart"+i)) && (health >= i)) {
                GameObject heartObject = new GameObject("heart" + i);
                heartObject.transform.SetParent(HUD.transform, false);
                Image heartSprite = heartObject.AddComponent<Image>();
                heartSprite.sprite = heart;
                RectTransform rt = heartObject.GetComponent<RectTransform>();
                rt.anchoredPosition = new Vector2(-412f  + i * 28, 214.6f);
                rt.localScale = new Vector3(0.26f, 0.26f, 0.26f);
            } else if ((GameObject.Find("heart"+i)) && (health < i)) {
                GameObject destroyHeart = GameObject.Find("heart"+i);
                GameObject.Destroy(destroyHeart);
            }
        }
    }
}