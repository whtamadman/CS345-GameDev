using UnityEngine;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class PowerUp : MonoBehaviour {

    public GameObject popupTextPrefab;
    public GameObject powerUpSprite;
    public PowerUpEffect effect;
    public Transform hudCanvas;
    private Transform powerUpContainer;
    private Transform meleeContainer;
    private Transform rangeContainer;
    private bool inTrigger;
    private bool popUpExist;
    private bool hasBeenPickedUp = false; // Prevent multiple pickups
    private SpriteRenderer sr;
    public GameObject hoverPopUp;
    public GameObject powerUpPrefab;
    private static GameObject popup;
    public PowerUpEffect startingMelee;
    public PowerUpEffect startingRange;
    

    public void ShowPowerUpText()
    {
        Debug.Log($"PowerUp {gameObject.name}: ShowPowerUpText called");
        
        if (popupTextPrefab == null) {
            Debug.LogError($"PowerUp {gameObject.name}: popupTextPrefab is null!");
            return;
        }
        
        if (hudCanvas == null) {
            Debug.LogError($"PowerUp {gameObject.name}: hudCanvas is null!");
            return;
        }
        
        if (effect == null) {
            Debug.LogError($"PowerUp {gameObject.name}: effect is null!");
            return;
        }
        
        popup = Instantiate(popupTextPrefab);
        Debug.Log($"PowerUp {gameObject.name}: Created popup: {popup.name}");
        
        popup.transform.SetParent(hudCanvas.GetComponent<RectTransform>(), false);
        Debug.Log($"PowerUp {gameObject.name}: Set popup parent to {hudCanvas.name}");
        RectTransform popupRT = popup.GetComponent<RectTransform>();
        popupRT.anchorMin = new Vector2(1, 1);
        popupRT.anchorMax = new Vector2(1, 1);
        popupRT.pivot = new Vector2(1, 1);
        popupRT.anchoredPosition = new Vector2(-170, -10); // offset from corner
        
        var textComponent = popup.GetComponentInChildren<TMP_Text>();
        if (textComponent != null) {
            if (effect.cost <= 0) {
                textComponent.text = effect.powerUpName + "\n" + effect.description;
            } else {
                textComponent.text = effect.powerUpName + "\n" + effect.description + "\nCost: " + effect.cost + " Gold";
            }
            Debug.Log($"PowerUp {gameObject.name}: Set popup text to: '{effect.powerUpName}\\n{effect.description}'");
        } else {
            Debug.LogError($"PowerUp {gameObject.name}: No TMP_Text component found in popup!");
        }
    }

    void Start() {
        popUpExist = false;
        hudCanvas = GameObject.Find("HUD").transform;
        meleeContainer = GameObject.Find("MeleeContainer").transform;
        rangeContainer = GameObject.Find("RangeContainer").transform;
        powerUpContainer = GameObject.Find("PowerUpSprites").transform;
        sr = GetComponent<SpriteRenderer>();
        if ((effect) && (sr)) {
            sr.sprite = effect.itemSprite;
        }
    }

    void Update () {
        if(inTrigger && Input.GetKeyDown(KeyCode.E)) {
            if (Player.Instance.coins >= effect.cost) {
                Debug.Log($"PowerUp {gameObject.name}: E key pressed, applying effect and destroying item");
                hasBeenPickedUp = true;
                effect.Apply(Player.Instance);
                AddPowerUpToSide();
                Player.Instance.coins -= effect.cost;
                // Play pickup sound
                if (AudioManager.Instance != null)
                {
                    AudioManager.Instance.PlayPlayerPickup();
                }
                Destroy(gameObject);
            } else {
                popup = Instantiate(popupTextPrefab);
                popup.transform.SetParent(hudCanvas.GetComponent<RectTransform>(), false);
                var textComponent = popup.GetComponentInChildren<TMP_Text>();
                textComponent.text = "Not enough cash stranger";
                Destroy(popup, 2f);
            }
        }
    }
    
    void OnDestroy() {
        Destroy(popup);
        popUpExist = false;
        Debug.Log($"PowerUp {gameObject.name}: OnDestroy called! HasBeenPickedUp: {hasBeenPickedUp}");
    }

    private void OnTriggerEnter2D(Collider2D other) {
        if(hasBeenPickedUp) return; // Don't process if already picked up
        
        Debug.Log($"PowerUp {gameObject.name}: Trigger entered by {other.gameObject.name} with tag '{other.tag}'");
        
        if(other.CompareTag("Player")) {
            Debug.Log($"PowerUp {gameObject.name}: Player detected, checking popup conditions");
            Debug.Log($"PowerUp {gameObject.name}: popUpExist={popUpExist}, popupTextPrefab={(popupTextPrefab != null ? popupTextPrefab.name : "null")}, hudCanvas={(hudCanvas != null ? hudCanvas.name : "null")}");
            
            if (!popUpExist && popupTextPrefab != null && hudCanvas != null) {
                Debug.Log($"PowerUp {gameObject.name}: All conditions met, showing popup");
                popUpExist = true;
                ShowPowerUpText();
            } else {
                Debug.LogWarning($"PowerUp {gameObject.name}: Popup conditions not met - popUpExist:{popUpExist}, prefab:{popupTextPrefab != null}, canvas:{hudCanvas != null}");
            }
            inTrigger = true;
        }
    }

    private void OnTriggerExit2D(Collider2D other) {
        if(hasBeenPickedUp) return; // Don't process if already picked up
        Destroy(popup, 0.5f);
        popUpExist = false;
        Debug.Log($"PowerUp {gameObject.name}: Trigger exited by {other.gameObject.name} with tag '{other.tag}'");
        
        if(other.CompareTag("Player")) {
            Debug.Log($"PowerUp {gameObject.name}: Player left trigger area");
            inTrigger = false;
        }
    }

    private IEnumerator UpdateResetVar(float delay) {
        yield return new WaitForSeconds(delay);
        popUpExist = false;
    }

    private void AddPowerUpToSide() {
        Transform container;
        if (effect.isMeleeWeapon) {
            if (meleeContainer.childCount > 0)
                Destroy(meleeContainer.GetChild(0).gameObject);
            container = meleeContainer;
        } else if (effect.isRangeWeapon) {
            if (rangeContainer.childCount > 0)
                Destroy(rangeContainer.GetChild(0).gameObject);
            container = rangeContainer;
        } else {
            container = powerUpContainer;
        }
        GameObject sideSprite = Instantiate(powerUpSprite, container);
        sideSprite.GetComponent<Image>().sprite = effect.itemSprite;
        PowerUpSprite iconComponent = sideSprite.GetComponent<PowerUpSprite>();
        iconComponent.SetData((string)effect.powerUpName, (string)effect.description);
    }
}