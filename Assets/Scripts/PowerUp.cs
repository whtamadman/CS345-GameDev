using UnityEngine;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class PowerUp : MonoBehaviour {

    public GameObject popupTextPrefab;
    public PowerUpEffect effect;
    public Transform hudCanvas;
    private bool inTrigger;
    private bool popUpExist;
    private bool hasBeenPickedUp = false; // Prevent multiple pickups

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
        
        GameObject popup = Instantiate(popupTextPrefab);
        Debug.Log($"PowerUp {gameObject.name}: Created popup: {popup.name}");
        
        popup.transform.SetParent(hudCanvas, false);
        Debug.Log($"PowerUp {gameObject.name}: Set popup parent to {hudCanvas.name}");
        
        var textComponent = popup.GetComponentInChildren<TMP_Text>();
        if (textComponent != null) {
            textComponent.text = effect.powerUpName + "\n" + effect.description;
            Debug.Log($"PowerUp {gameObject.name}: Set popup text to: '{effect.powerUpName}\\n{effect.description}'");
        } else {
            Debug.LogError($"PowerUp {gameObject.name}: No TMP_Text component found in popup!");
        }
        
        Destroy(popup, 2f);
        StartCoroutine(UpdateResetVar(2f));
        Debug.Log($"PowerUp {gameObject.name}: Popup setup complete, will destroy in 2 seconds");
    }

    void Start() {
        popUpExist = false;
    }

    void Update () {
        if(inTrigger && Input.GetKeyDown(KeyCode.E)) {
            Debug.Log($"PowerUp {gameObject.name}: E key pressed, applying effect and destroying item");
            hasBeenPickedUp = true;
            effect.Apply(Player.Instance);
            Destroy(gameObject);
        }
    }
    
    void OnDestroy() {
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
}