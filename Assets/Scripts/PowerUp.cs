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

    public void ShowPowerUpText()
    {
        GameObject popup = Instantiate(popupTextPrefab);
        popup.transform.SetParent(hudCanvas, false);
        Debug.Log("2HI\n");
        popup.GetComponentInChildren<TMP_Text>().text = effect.powerUpName + "\n" + effect.description;
        Destroy(popup, 2f);
        StartCoroutine(UpdateResetVar(2f));
    }

    void Start() {
        popUpExist = false;
    }

    void Update () {
        if(inTrigger && Input.GetKeyDown(KeyCode.E)) {
            effect.Apply(Player.Instance);
            Destroy(gameObject);
        }
    }

    private void OnTriggerEnter2D(Collider2D other) {
        if(other.CompareTag("Player")) {
            if (!popUpExist) {
                popUpExist = true;
                ShowPowerUpText();
            }
            inTrigger = true;
        }
    }

    private void OnTriggerExit2D(Collider2D other) {
        if(other.CompareTag("Player")) {
            inTrigger = false;
        }
    }

    private IEnumerator UpdateResetVar(float delay) {
        yield return new WaitForSeconds(delay);
        popUpExist = false;
    }
}