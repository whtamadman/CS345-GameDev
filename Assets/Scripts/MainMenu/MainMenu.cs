using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEditor;
 
public class MainMenu : MonoBehaviour
{

    [SerializeField] private GameObject fadeCanvasPrefab;
    private Image fadeImage;
    [SerializeField] private float fadeDuration = 3f;
    public GameObject TitlesAndButtons;
    public GameObject OptionMenu;
    private GameObject canvasInstance;


    void Start() {
        canvasInstance = Instantiate(fadeCanvasPrefab);
        fadeImage = canvasInstance.GetComponentInChildren<Image>();
        TitlesAndButtons.SetActive(false);
        OptionMenu.SetActive(false);
        StartCoroutine(FadeToBlack());
        AudioManager.Instance.PlayMenuMusic();
    }

    public void Play()
    {
        AudioManager.Instance.ButtonClick();
        SceneManager.LoadScene("Main Scene");
    }
 
    public void Quit()
    {
        AudioManager.Instance.ButtonClick();
        Application.Quit();
        EditorApplication.isPlaying = false;
    }

    public void Options() 
    {
        AudioManager.Instance.ButtonClick();
        TitlesAndButtons.SetActive(false);
        OptionMenu.SetActive(true);
    }

    public void Back() {
        AudioManager.Instance.ButtonClick();
        OptionMenu.SetActive(false);
        TitlesAndButtons.SetActive(true);
    }

    private IEnumerator FadeToBlack() {
        if (fadeImage != null) 
            yield return StartCoroutine(Fade(1f, 0f));

        TitlesAndButtons.SetActive(true);
        Destroy(canvasInstance);
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

}