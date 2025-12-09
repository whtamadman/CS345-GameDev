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
    public GameObject howToPlay;
    public GameObject deathScreen;
    public GameObject player;
    private bool pauseScreenActive;
    public Slider musicSlider;
    public Slider sfxSlider;

    void Start() {
        pauseScreen.SetActive(false);
        pauseScreenActive = false;
        confirmationWindow.SetActive(false);
        deathScreen.SetActive(false);
        howToPlay.SetActive(true);
        Time.timeScale = 0f;
        musicSlider.onValueChanged.AddListener(AudioManager.Instance.SetMusicVolume);
        sfxSlider.onValueChanged.AddListener(AudioManager.Instance.SetSFXVolume);
        musicSlider.value = AudioManager.Instance.GetMusicVolume();
        sfxSlider.value = AudioManager.Instance.GetSFXVolume();
    }

    void Update() {
        if (!pauseScreenActive && Input.GetKeyDown(KeyCode.Escape)) {
            AudioManager.Instance.ButtonClick();
            pauseScreenActive = true;
            pauseScreen.SetActive(true);
            Time.timeScale = 0f;
            Debug.Log("Game Paused");
        } else if (pauseScreenActive && Input.GetKeyDown(KeyCode.Escape)) {
            pauseScreenActive = false;
            pauseScreen.SetActive(false);
            Time.timeScale = 1f;
        }
        if (Player.Instance.death == true) {
            deathScreen.SetActive(true);
            player.SetActive(false);
            Time.timeScale = 0;
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

    public void Close() {
        AudioManager.Instance.ButtonClick();
        howToPlay.SetActive(false);
        Time.timeScale = 1f;
    }

    public void Yes() {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

}