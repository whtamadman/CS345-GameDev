using UnityEngine;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class PowerUpSprite : MonoBehaviour {

    public string localName, description;
    public GameObject popupTextPrefab;
    private Transform hudCanvas;
    private RectTransform rectTransform;
    GameObject hoverPopUp;
    private bool playedOnce;

    void Start() {
        hudCanvas = GameObject.Find("HUD").transform;
        transform.SetAsLastSibling();
        rectTransform = GetComponent<RectTransform>();
        playedOnce = false;
    }

    void Update() {
        Vector2 mousePos = Input.mousePosition;
        if (RectTransformUtility.RectangleContainsScreenPoint(rectTransform, mousePos)) {
            if (!playedOnce) {
                AudioManager.Instance.ButtonHover();
                playedOnce = true;
            }
            if (hoverPopUp == null) {
                hoverPopUp = Instantiate(popupTextPrefab);
                hoverPopUp.transform.SetParent(hudCanvas.GetComponent<RectTransform>(), false);
                RectTransform popupRT = hoverPopUp.GetComponent<RectTransform>();
                popupRT.anchorMin = new Vector2(1, 1);
                popupRT.anchorMax = new Vector2(1, 1);
                popupRT.pivot = new Vector2(1, 1);
                popupRT.anchoredPosition = new Vector2(-170, -10); // offset from corner
                var textComponent = hoverPopUp.GetComponentInChildren<TMP_Text>();
                if (textComponent != null) {
                    textComponent.text = localName + "\n" + description;
                }
            }
        }
        else {
            if (hoverPopUp != null) {
                Destroy(hoverPopUp);
                playedOnce = false;
            }
        }
    }

    public void SetData(string powerUpName, string desc) {
        localName = powerUpName;
        description = desc;
    }
}