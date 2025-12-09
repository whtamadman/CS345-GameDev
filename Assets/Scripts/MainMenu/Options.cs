using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor;
using UnityEngine.UI;
 
public class Options : MonoBehaviour
{

    public GameObject pauseScreen;
    public GameObject confirmationWindow;
    private bool pauseScreenActive;
    public Slider musicSlider;
    public Slider sfxSlider;

    void Start() {
        pauseScreen.SetActive(false);
        pauseScreenActive = false;
        confirmationWindow.SetActive(false);
        musicSlider.value = AudioManager.Instance.GetMusicVolume();
        sfxSlider.value = AudioManager.Instance.GetSFXVolume();
    }

    void Update() {
        if (!pauseScreenActive && Input.GetKeyDown(KeyCode.Escape)) {
            AudioManager.Instance.ButtonClick();
            pauseScreenActive = true;
            pauseScreen.SetActive(true);
            // Time.timeScale = 0.000001f;
            Debug.Log("Game Paused");
        } else if (pauseScreenActive && Input.GetKeyDown(KeyCode.Escape)) {
            pauseScreenActive = false;
            pauseScreen.SetActive(false);
            Time.timeScale = 1f;
        }
    }

    public void Back() {
        Debug.Log("Trying to UnPause");
        AudioManager.Instance.ButtonClick();
        pauseScreen.SetActive(false);
        pauseScreenActive = false;
        Time.timeScale = 1f;
    }

    public void Exit() {
        AudioManager.Instance.ButtonClick();
        confirmationWindow.SetActive(true);
        pauseScreen.SetActive(false);
    }

    public void confirmBack() {
        AudioManager.Instance.ButtonClick();
        confirmationWindow.SetActive(false);
        pauseScreen.SetActive(true);
    }

    public void confirmExit() {
        AudioManager.Instance.ButtonClick();
        SceneManager.LoadScene("Main Menu");
    }

}