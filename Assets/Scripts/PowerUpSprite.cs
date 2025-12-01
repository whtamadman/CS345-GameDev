using UnityEngine;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class PowerUpSprite : MonoBehaviour {

    public string name, description;
    public GameObject popupTextPrefab;
    private Transform hudCanvas;
    private RectTransform rectTransform;
    GameObject hoverPopUp;

    void Start() {
        hudCanvas = GameObject.Find("HUD").transform;
        transform.SetAsLastSibling();
        rectTransform = GetComponent<RectTransform>();
    }

    void Update() {

        Vector2 mousePos = Input.mousePosition;
        if (RectTransformUtility.RectangleContainsScreenPoint(rectTransform, mousePos)) {
            if (hoverPopUp == null) {
                hoverPopUp = Instantiate(popupTextPrefab);
                hoverPopUp.transform.SetParent(transform.root, false);
                var textComponent = hoverPopUp.GetComponentInChildren<TMP_Text>();
                if (textComponent != null) {
                    textComponent.text = name + "\n" + description;
                }
            }
        }
        else {
            if (hoverPopUp != null) {
                Destroy(hoverPopUp);
            }
        }
    }

    public void SetData(string powerUpName, string desc) {
        name = powerUpName;
        description = desc;
    }
}