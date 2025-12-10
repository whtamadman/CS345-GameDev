using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections.Generic;
using UnityEngine.EventSystems;

public class Ending : MonoBehaviour
{
    [SerializeField] private GameObject fadeCanvasPrefab;
    private Image fadeImage;
    [SerializeField] private float fadeDuration = 1f;
    private bool onChair;
    public GameObject popupTextPrefab;
    public Transform hudCanvas;
    private bool popUpExist;
    private GameObject popup;

    void Start() {
        popUpExist = false;
    }

    void Update() {
        if(onChair && Input.GetKeyDown(KeyCode.E)) {
            GameObject canvasInstance = Instantiate(fadeCanvasPrefab);
            fadeImage = canvasInstance.GetComponentInChildren<Image>();
            StartCoroutine(FadeToBlack());
        }
    }

    private IEnumerator FadeToBlack()
    {

        AudioManager.Instance.StopMusic();

        yield return StartCoroutine(Fade(0f, 1f));

        yield return new WaitForSeconds(1.5f);

        // 3. Play sound
        AudioManager.Instance.PlayTileBreak();

        yield return new WaitForSeconds(1.5f);

        AudioManager.Instance.PlayCoinPickup();

        // 4. Wait another 3 seconds
        yield return new WaitForSeconds(1.5f);

        // 5. Load main menu
        SceneManager.LoadScene("Main Menu");
    }

    private IEnumerator Fade(float startAlpha, float endAlpha) {
        float elapsed = 0f;
        Color c = fadeImage.color;

        while (elapsed < fadeDuration) {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Lerp(startAlpha, endAlpha, elapsed / fadeDuration);
            fadeImage.color = new Color(c.r, c.g, c.b, alpha);
            yield return null;
        }

        fadeImage.color = new Color(c.r, c.g, c.b, endAlpha);
    }

        void OnTriggerEnter2D(Collider2D other) {
            onChair = true;
            if (!popUpExist) {
                popUpExist = true;
                ShowPowerUpText();
            }
        }

        void OnTriggerExit2D(Collider2D other) {
            onChair = false;
            popUpExist = false;
            Destroy(popup);
        }

        private IEnumerator Delay(float delay) {
        yield return new WaitForSeconds(delay);  // Wait 2 seconds
    }

    public void ShowPowerUpText() {
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
        textComponent.text = "Press E to Start!";
    }
}